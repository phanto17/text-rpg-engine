using Raylib_cs;
using RpgEngine.Pixel.Layout;
using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Pixel.Rendering;

/// <summary>A room's mood caption: fades in along the bottom of the scene on entry, lingers, then fades away.</summary>
internal sealed class MoodCaption
{
    private readonly RpgEngine.Pixel.Presentation.CaptionLifetime _lifetime = new();

    public void Show(string roomId, string? text) => _lifetime.Show(roomId, text);

    public void Complete(string roomId) => _lifetime.Complete(roomId);

    public void Update(double dt) => _lifetime.Update(dt);

    public void Draw(PixelRect scene, Color accent)
    {
        if (_lifetime.Text is not { } text)
        {
            return;
        }

        var alpha = (int)(255 * _lifetime.Opacity);

        var width = scene.Width - 24;
        var rows = TextWrapper.Wrap(LogLine.Of(text), width, Canvas.Width).Take(2).Select(r => string.Concat(r.Select(s => s.Text)).TrimEnd()).ToList();
        var height = (rows.Count * PixelLayout.LineHeight) + 4;
        var box = new PixelRect(scene.X + 8, scene.Bottom - height - 6, scene.Width - 16, height);
        Canvas.Fill(box, Palette.Alpha(Palette.PanelBack, (int)(alpha * 0.75)));
        Raylib.DrawRectangle(box.X, box.Y, 2, box.Height, Palette.Alpha(accent, alpha));
        for (var i = 0; i < rows.Count; i++)
        {
            var rowText = i == rows.Count - 1 ? Canvas.Ellipsize(rows[i], width) : rows[i];
            Canvas.Text(box.X + 8, box.Y + 2 + (i * PixelLayout.LineHeight), rowText, Palette.Alpha(Palette.Sensory, alpha));
        }
    }
}
