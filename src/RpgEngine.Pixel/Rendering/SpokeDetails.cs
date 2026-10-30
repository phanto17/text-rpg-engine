using Raylib_cs;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Layout;

namespace RpgEngine.Pixel.Rendering;

/// <summary>Live chapel and bridge details shared by painted and procedural scenes.</summary>
internal static class SpokeDetails
{
    public static void Draw(PixelRect area, SceneFrame frame)
    {
        var time = Math.Floor(frame.Time * 4) / 4;
        if (frame.Look.Style == SceneStyle.EchoChapel)
        {
            Jars(area, frame, time);
            if (frame.HasLantern) Echo(area.X + 231, area.Y + 88, frame, seated: true);
        }
        else if (frame.Look.Style == SceneStyle.DrownedBridge)
        {
            Bridge(area, frame, time);
            if (frame.HasLantern) Echo(area.X + 93, area.Y + 89, frame, seated: false);
        }
    }

    private static void Jars(PixelRect area, SceneFrame frame, double time)
    {
        var accent = Palette.Of(frame.Look.Accent);
        var wire = Palette.Of(frame.Look.Architecture);
        for (var i = 0; i < 5; i++)
        {
            var x = area.X + 24 + i * 60;
            var y = area.Y + 20 + i % 2 * 10 + (int)(Math.Sin(time * 0.6 + i) * 2);
            Raylib.DrawLine(x, area.Y, x, y, wire);
            Raylib.DrawRectangleLines(x - 4, y, 9, 10, Emblems.Shade(accent, 0.6));
            if (!frame.SpiritsReleased)
            {
                Raylib.DrawRectangle(x - 2, y + 3, 5, 4, Palette.Alpha(accent, 180));
                Raylib.DrawRectangle(x - 3, y - 1, 7, 2, wire);
            }
            else
            {
                Raylib.DrawRectangle(x + 5, y + 8, 4, 2, wire);
            }
        }
    }

    private static void Bridge(PixelRect area, SceneFrame frame, double time)
    {
        var stone = Palette.Of(frame.Look.Architecture);
        var iron = Emblems.Shade(stone, 0.7);
        // Keep the upper walking span intact while ripples cover the flooded foreground.
        SceneProps.Water(new PixelRect(area.X, area.Y + 116, area.Width, area.Height - 116),
            Emblems.Shade(Palette.Of(frame.Look.BackgroundBottom), 0.8), time);
        var x = area.CenterX;
        var bottom = area.Y + (frame.PassageOpen ? 48 : 96);
        for (var offset = -24; offset <= 24; offset += 8)
        {
            Raylib.DrawRectangle(x + offset, area.Y + 40, 2, bottom - area.Y - 40, iron);
            Raylib.DrawPixel(x + offset, bottom, Palette.Muted);
        }
        Raylib.DrawRectangle(x - 25, bottom - 8, 52, 2, stone);
        for (var side = -1; side <= 1; side += 2)
        {
            var sway = (int)Math.Round(Math.Sin(time * 0.5) * 1);
            for (var y = area.Y + 46; y < area.Y + 80; y += 5)
            {
                Raylib.DrawRectangleLines(x + side * 34 + sway, y, 3, 5, iron);
            }
        }
    }

    private static void Echo(int x, int y, SceneFrame frame, bool seated)
    {
        var color = Palette.Alpha(Palette.Of(frame.Look.Accent), 65);
        Raylib.DrawRectangle(x - 2, y - 16, 5, 5, color);
        Raylib.DrawRectangle(x - 3, y - 10, 7, seated ? 8 : 12, color);
        Raylib.DrawLine(x - 2, y - 2, x - 5, y + 4, color);
        Raylib.DrawLine(x + 2, y - 2, x + 5, y + 4, color);
    }
}
