using Raylib_cs;
using RpgEngine.Pixel.Layout;
using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Pixel.Rendering;

/// <summary>A room's mood caption: fades in along the bottom of the scene on entry, lingers, then fades away.</summary>
internal sealed class MoodCaption
{
    private const double FadeInSeconds = 0.5;
    private const double ShowSeconds = 7;
    private const double FadeOutSeconds = 1.2;

    private string? _text;
    private double _age;

    public void Show(string? text)
    {
        _text = string.IsNullOrWhiteSpace(text) ? null : text;
        _age = 0;
    }

    public void Update(double dt)
    {
        if (_text is null)
        {
            return;
        }

        _age += dt;
        if (_age > ShowSeconds)
        {
            _text = null;
        }
    }

    public void Draw(PixelRect scene, Color accent)
    {
        if (_text is null)
        {
            return;
        }

        var fade = _age < FadeInSeconds ? _age / FadeInSeconds
            : _age > ShowSeconds - FadeOutSeconds ? (ShowSeconds - _age) / FadeOutSeconds
            : 1;
        var alpha = (int)(255 * Math.Clamp(fade, 0, 1));

        var width = scene.Width - 24;
        var rows = TextWrapper.Wrap(LogLine.Of(_text), width, Canvas.Width).Take(2).Select(r => string.Concat(r.Select(s => s.Text)).TrimEnd()).ToList();
        var height = (rows.Count * PixelLayout.LineHeight) + 4;
        var box = new PixelRect(scene.X + 8, scene.Bottom - height - 6, scene.Width - 16, height);
        Canvas.Fill(box, Palette.Alpha(Palette.PanelBack, (int)(alpha * 0.75)));
        Raylib.DrawRectangle(box.X, box.Y, 2, box.Height, Palette.Alpha(accent, alpha));
        for (var i = 0; i < rows.Count; i++)
        {
            var text = i == rows.Count - 1 ? Canvas.Ellipsize(rows[i], width) : rows[i];
            Canvas.Text(box.X + 8, box.Y + 2 + (i * PixelLayout.LineHeight), text, Palette.Alpha(Palette.Sensory, alpha));
        }
    }
}
