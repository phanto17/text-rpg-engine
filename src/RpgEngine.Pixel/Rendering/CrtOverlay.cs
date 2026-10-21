using Raylib_cs;
using RpgEngine.Pixel.Layout;

namespace RpgEngine.Pixel.Rendering;

/// <summary>Scanlines and a vignette drawn over the scaled canvas in window space, for the old-monitor look.</summary>
internal static class CrtOverlay
{
    public static void Draw(PixelRect viewport)
    {
        var scale = Math.Max(1, viewport.Height / (double)PixelLayout.Height);
        var step = Math.Max(2, (int)Math.Round(scale));
        var thickness = Math.Max(1, (int)(scale / 3));
        var line = new Color(0, 0, 0, 70);
        for (var y = viewport.Y + step - thickness; y < viewport.Bottom; y += step)
        {
            Raylib.DrawRectangle(viewport.X, y, viewport.Width, thickness, line);
        }

        // A faint phosphor mask: every third column slightly darker.
        if (scale >= 3)
        {
            var mask = new Color(0, 0, 0, 22);
            for (var x = viewport.X; x < viewport.Right; x += 3)
            {
                Raylib.DrawRectangle(x, viewport.Y, 1, viewport.Height, mask);
            }
        }

        var edge = Math.Max(8, viewport.Width / 12);
        var dark = new Color(0, 0, 0, 150);
        var clear = new Color(0, 0, 0, 0);
        Raylib.DrawRectangleGradientH(viewport.X, viewport.Y, edge, viewport.Height, dark, clear);
        Raylib.DrawRectangleGradientH(viewport.Right - edge, viewport.Y, edge, viewport.Height, clear, dark);
        Raylib.DrawRectangleGradientV(viewport.X, viewport.Y, viewport.Width, edge, dark, clear);
        Raylib.DrawRectangleGradientV(viewport.X, viewport.Bottom - edge, viewport.Width, edge, clear, dark);
    }
}
