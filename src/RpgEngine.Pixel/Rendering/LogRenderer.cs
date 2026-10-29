using Raylib_cs;
using RpgEngine.Pixel.Layout;
using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Pixel.Rendering;

/// <summary>The narrative log: wrapped, toned lines revealed by the typewriter, scrollable with the wheel.</summary>
internal sealed class LogRenderer(PixelRect area)
{
    private readonly Dictionary<LogLine, IReadOnlyList<IReadOnlyList<LogSpan>>> _wrapped = new(ReferenceEqualityComparer.Instance);
    private int _scrollRows;
    private int _seenVersion = -1;

    public PixelRect Area => area;

    /// <summary>Scrolls by whole rows; positive goes back in time. New text snaps the view to the newest line.</summary>
    public void Scroll(int rows) => _scrollRows = Math.Max(0, _scrollRows + rows);

    public void Draw(EventSequencer sequencer, double time)
    {
        if (sequencer.Version != _seenVersion)
        {
            _seenVersion = sequencer.Version;
            _scrollRows = 0;
        }

        Canvas.Panel(area, Palette.PanelBack, Palette.PanelEdge);
        var inner = area.Inset(4, 3);
        var textWidth = inner.Width - 8;
        Canvas.Clip(inner);

        var lines = sequencer.Lines;
        var y = inner.Bottom + (_scrollRows * PixelLayout.LineHeight) - PixelLayout.LineHeight;
        var totalRows = 0;
        for (var i = lines.Count - 1; i >= 0; i--)
        {
            var line = lines[i];
            var rows = Rows(line, textWidth);
            var revealed = i == lines.Count - 1 ? sequencer.RevealedCharacters : int.MaxValue;
            totalRows += rows.Count;
            var top = y - ((rows.Count - 1) * PixelLayout.LineHeight);
            if (top < inner.Bottom)
            {
                DrawLine(line, rows, inner.X, top, textWidth, revealed, time);
            }

            y = top - PixelLayout.LineHeight;
            if (y < inner.Y - PixelLayout.LineHeight && i < lines.Count - 1)
            {
                // Rows above the view are only counted, for the scrollbar.
                totalRows += CountRows(lines, i, textWidth);
                break;
            }
        }

        Canvas.Unclip();

        var visibleRows = inner.Height / PixelLayout.LineHeight;
        if (totalRows > visibleRows)
        {
            var track = new PixelRect(area.Right - 4, area.Y + 2, 2, area.Height - 4);
            Canvas.Fill(track, Palette.PanelEdge);
            var thumbHeight = Math.Max(4, track.Height * visibleRows / totalRows);
            var maxScroll = Math.Max(1, totalRows - visibleRows);
            var thumbY = track.Bottom - thumbHeight - (int)((track.Height - thumbHeight) * Math.Min(1.0, _scrollRows / (double)maxScroll));
            Canvas.Fill(new PixelRect(track.X, thumbY, 2, thumbHeight), Palette.Accent);
        }

        if (!sequencer.IsIdle && (time % 1) < 0.6)
        {
            Canvas.Fill(new PixelRect(area.Right - 10, area.Bottom - 6, 4, 3), Palette.Accent);
        }
    }

    private void DrawLine(LogLine line, IReadOnlyList<IReadOnlyList<LogSpan>> rows, int x, int top, int width, int revealed, double time)
    {
        var shown = 0;
        var indent = line.Kind == LogLineKind.Quote ? 6 : 0;
        if (line.Kind == LogLineKind.Quote && revealed > 0)
        {
            Raylib.DrawRectangle(x, top + 1, 2, (rows.Count * PixelLayout.LineHeight) - 2, Palette.Alpha(Palette.Speaker, 160));
        }

        if (line.Kind == LogLineKind.Rule)
        {
            var text = Canvas.Ellipsize(line.Text, width - 8);
            var visible = text[..Math.Min(text.Length, revealed)];
            var textWidth = Canvas.Width(text);
            var left = x + ((width - textWidth) / 2);
            var color = line.Spans.Count > 0 ? Palette.For(line.Spans[0].Tone) : Palette.Muted;
            Raylib.DrawRectangle(x, top + 5, Math.Max(0, left - x - 4), 1, Palette.PanelEdgeLight);
            Raylib.DrawRectangle(left + textWidth + 4, top + 5, Math.Max(0, x + width - left - textWidth - 4), 1, Palette.PanelEdgeLight);
            Canvas.BoldText(left, top, visible, color);
            return;
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var rx = x + indent;
            var ry = top + (r * PixelLayout.LineHeight);
            foreach (var span in rows[r])
            {
                var remaining = revealed - shown;
                if (remaining <= 0)
                {
                    return;
                }

                var text = span.Text.Length <= remaining ? span.Text : span.Text[..remaining];
                var color = Palette.For(span.Tone);
                if (line.Kind == LogLineKind.Heading)
                {
                    Canvas.BoldText(rx, ry, text, color);
                    Raylib.DrawRectangle(rx, ry + 10, Canvas.Width(text) + 2, 1, Palette.Alpha(color, 120));
                }
                else
                {
                    Canvas.Text(rx, ry, text, color);
                }

                rx += Canvas.Width(text);
                shown += text.Length;
                if (text.Length < span.Text.Length)
                {
                    // The caret sits where the typewriter is.
                    if ((time * 3) % 1 < 0.5)
                    {
                        Raylib.DrawRectangle(rx + 1, ry + 1, 4, 8, Palette.Accent);
                    }

                    return;
                }
            }
        }
    }

    private IReadOnlyList<IReadOnlyList<LogSpan>> Rows(LogLine line, int width)
    {
        if (!_wrapped.TryGetValue(line, out var rows))
        {
            rows = line.Kind == LogLineKind.Rule ? [line.Spans] : TextWrapper.Wrap(line, width - (line.Kind == LogLineKind.Quote ? 6 : 0), Canvas.Width);
            _wrapped[line] = rows;
            if (_wrapped.Count > EventSequencer.MaxLines * 2)
            {
                _wrapped.Clear();
            }
        }

        return rows;
    }

    private int CountRows(IReadOnlyList<LogLine> lines, int before, int width)
    {
        var count = 0;
        for (var i = before - 1; i >= 0; i--)
        {
            count += Rows(lines[i], width).Count;
        }

        return count;
    }
}
