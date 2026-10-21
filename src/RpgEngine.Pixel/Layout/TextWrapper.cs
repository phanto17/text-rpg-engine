using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Pixel.Layout;

/// <summary>Breaks a toned log line into rows that fit a pixel width, measuring with the renderer's own font.</summary>
public static class TextWrapper
{
    /// <summary>
    /// Rows of spans, each at most <paramref name="maxWidth"/> wide by <paramref name="measure"/>.
    /// Words move whole to the next row; a word wider than a row is split by characters.
    /// A blank line yields one empty row.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<LogSpan>> Wrap(LogLine line, int maxWidth, Func<string, int> measure)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(measure);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxWidth);

        var rows = new List<IReadOnlyList<LogSpan>>();
        var row = new List<LogSpan>();
        var rowText = string.Empty;

        foreach (var span in line.Spans)
        {
            foreach (var token in Tokens(span.Text))
            {
                var candidate = rowText + token;
                if (measure(candidate.TrimEnd()) <= maxWidth)
                {
                    Append(row, token, span.Tone);
                    rowText = candidate;
                    continue;
                }

                if (rowText.Length > 0)
                {
                    rows.Add(row);
                    row = [];
                    rowText = string.Empty;
                }

                var word = token.TrimStart();
                while (measure(word) > maxWidth && word.Length > 1)
                {
                    var cut = word.Length - 1;
                    while (cut > 1 && measure(word[..cut]) > maxWidth)
                    {
                        cut--;
                    }

                    rows.Add([new LogSpan(word[..cut], span.Tone)]);
                    word = word[cut..];
                }

                if (word.Length > 0)
                {
                    Append(row, word, span.Tone);
                    rowText = word;
                }
            }
        }

        if (row.Count > 0 || rows.Count == 0)
        {
            rows.Add(row);
        }

        return rows;
    }

    // Words keep their trailing spaces, so rows rejoin to the original text.
    private static IEnumerable<string> Tokens(string text)
    {
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ' && (i + 1 == text.Length || text[i + 1] != ' '))
            {
                yield return text[start..(i + 1)];
                start = i + 1;
            }
        }

        if (start < text.Length)
        {
            yield return text[start..];
        }
    }

    private static void Append(List<LogSpan> row, string text, LogTone tone)
    {
        if (row.Count > 0 && row[^1].Tone == tone)
        {
            row[^1] = new LogSpan(row[^1].Text + text, tone);
        }
        else
        {
            row.Add(new LogSpan(text, tone));
        }
    }
}
