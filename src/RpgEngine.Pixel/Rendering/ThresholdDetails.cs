using Raylib_cs;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Layout;

namespace RpgEngine.Pixel.Rendering;

/// <summary>The live floor casting and sanctuary fire, over painted or procedural architecture.</summary>
internal static class ThresholdDetails
{
    public static void Draw(PixelRect area, SceneFrame frame)
    {
        if (frame.Look.Style != SceneStyle.TwinSealGate) return;
        var x = area.CenterX;
        var y = area.Y + 96;
        var bronze = Palette.Of(frame.Look.Architecture);
        var edge = Emblems.Shade(bronze, 1.5);
        var gold = Palette.Of(frame.Look.Accent);
        var silver = new Color(170, 200, 255, 255);
        var open = frame.ThresholdOpen;
        Raylib.DrawEllipse(x, y, 66, 17, bronze);
        Raylib.DrawEllipseLines(x, y, 66, 17, edge);
        if (open)
        {
            Raylib.DrawEllipse(x - 67, y, 16, 15, bronze);
            Raylib.DrawEllipse(x + 67, y, 16, 15, bronze);
            Raylib.DrawEllipse(x, y, 53, 14, Palette.Ink);
            for (var i = 0; i < 5; i++)
            {
                Raylib.DrawRectangle(x - 35 + i * 4, y - 9 + i * 4, 70 - i * 8, 2, Emblems.Shade(edge, 1 - i * 0.12));
            }
        }
        else
        {
            Raylib.DrawEllipseLines(x, y, 60, 13, Emblems.Shade(edge, 0.8));
            Raylib.DrawLine(x, y - 15, x, y + 15, Palette.Ink);
            Raylib.DrawRectangle(x - 4, y - 3, 9, 6, edge);
            Raylib.DrawPixel(x - 2, y - 1, Palette.Ink);
            Raylib.DrawPixel(x + 2, y - 1, Palette.Ink);
        }

        // Carried seals resonate with empty sockets; only opening seats them into the parted lids.
        var offset = open ? 67 : 38;
        var moon = frame.MoonSealSet || open ? silver : Emblems.Shade(edge, 0.9);
        var sun = frame.SunSealSet || open ? gold : Emblems.Shade(edge, 0.9);
        Raylib.DrawCircle(x - offset, y - 1, 6, Palette.Ink);
        Raylib.DrawCircle(x + offset, y - 1, 6, Palette.Ink);
        Raylib.DrawCircleLines(x - offset, y - 1, 6, moon);
        if (open) Raylib.DrawCircle(x - offset, y - 1, 5, moon);
        Raylib.DrawCircle(x - offset + 3, y - 3, 5, bronze);
        Raylib.DrawCircleLines(x + offset, y - 1, 6, sun);
        if (open) Raylib.DrawCircle(x + offset, y - 1, 4, sun);
        for (var i = -1; i <= 1; i += 2)
        {
            Raylib.DrawLine(x + offset + i * 8, y - 1, x + offset + i * 10, y - 1, sun);
            Raylib.DrawLine(x + offset, y - 1 + i * 7, x + offset, y - 1 + i * 9, sun);
        }
        var time = Math.Floor(frame.Time * 4) / 4;
        SceneProps.Brazier(area.Right - 25, area.Y + 112, 10, time, 3, bronze, gold);
    }
}
