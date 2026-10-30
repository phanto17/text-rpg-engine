using Raylib_cs;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Layout;
using RpgEngine.Pixel.Presentation;

namespace RpgEngine.Pixel.Rendering;

/// <summary>Live liquid, steam, sluice and furnace over either backdrop path.</summary>
internal static class ReliquaryDetails
{
    public static void Draw(PixelRect area, SceneFrame frame)
    {
        var time = Math.Floor(frame.Time * 4) / 4;
        var state = frame.Reliquary ?? new ReliquaryState();
        if (frame.Look.Style == SceneStyle.LyeCistern) Cistern(area, frame, state, time);
        else if (frame.Look.Style == SceneStyle.IronBastion) Furnace(area, frame, state, time);
    }

    private static void Cistern(PixelRect area, SceneFrame frame, ReliquaryState state, double time)
    {
        var x = area.CenterX;
        var y = area.Y + 98;
        var accent = Palette.Of(frame.Look.Accent);
        var liquid = Emblems.Shade(accent, state.CisternQuenched ? 0.6 : 0.32);
        for (var i = 0; i < 5; i++)
        {
            var drip = (int)((time * 12 + i * 17) % 40);
            Raylib.DrawPixel(area.X + 33 + i * 62, area.Y + 28 + drip, Palette.Alpha(accent, 130));
        }
        Raylib.DrawEllipse(x, y, 72, 14, liquid);
        for (var row = 0; row < 3; row++)
        {
            var shift = (int)Math.Round(Math.Sin(time + row) * 3);
            for (var offset = -40; offset <= 40; offset += 24)
            {
                Raylib.DrawRectangle(x + offset + shift, y - 7 + row * 6, 8, 1, Emblems.Shade(liquid, 1.5));
            }
        }

        var wheelY = area.Y + 55;
        Raylib.DrawCircleLines(x, wheelY, 10, Palette.Muted);
        Raylib.DrawCircle(x, wheelY, 2, Palette.Muted);
        if (state.CisternQuenched)
        {
            Raylib.DrawLine(x - 6, wheelY - 6, x + 6, wheelY + 6, Palette.Muted);
            Raylib.DrawLine(x - 6, wheelY + 6, x + 6, wheelY - 6, Palette.Muted);
            for (var i = -1; i <= 1; i++)
            {
                var shift = (int)((time * 8 + i * 3 + 8) % 5);
                Raylib.DrawLine(x + i * 3, area.Y + 75 + shift, x + i * 3, y - 2, accent);
            }
        }
        else
        {
            Raylib.DrawLine(x - 8, wheelY, x + 8, wheelY, Palette.Muted);
            Raylib.DrawLine(x, wheelY - 8, x, wheelY + 8, Palette.Muted);
            for (var i = 0; i < 5; i++)
            {
                var sx = x - 48 + i * 24;
                var rise = (int)((time * 9 + i * 7) % 24);
                var sway = (int)Math.Round(Math.Sin(time + i) * 2);
                var color = Palette.Alpha(accent, 70);
                Raylib.DrawLine(sx, y - rise, sx + sway, y - rise - 13, color);
                Raylib.DrawLine(sx + sway, y - rise - 13, sx - sway, y - rise - 22, color);
            }
        }
    }

    private static void Furnace(PixelRect area, SceneFrame frame, ReliquaryState state, double time)
    {
        var x = area.CenterX;
        var top = area.Y + 56;
        var bottom = area.Y + 100;
        var accent = Palette.Of(frame.Look.Accent);
        var iron = Palette.Of(frame.Look.Architecture);
        Raylib.DrawRectangle(x - 30, top, 60, 44, Palette.Ink);
        SceneProps.Flame(x - 8, bottom - 3, 20, time, 8, accent, Palette.Narration);
        if (!frame.SunSealSet)
        {
            Raylib.DrawCircle(x + 19, bottom - 13, 5, Palette.Gold);
            Raylib.DrawCircleLines(x + 19, bottom - 13, 7, Palette.Gold);
            Raylib.DrawLine(x + 19, bottom - 22, x + 19, bottom - 4, Palette.Gold);
            Raylib.DrawLine(x + 10, bottom - 13, x + 28, bottom - 13, Palette.Gold);
        }
        var grateBottom = state.FurnaceOpen ? top + 7 : bottom;
        for (var offset = -28; offset <= 28; offset += 8)
        {
            Raylib.DrawRectangle(x + offset, top, 2, grateBottom - top, iron);
        }
        Raylib.DrawRectangle(x - 29, grateBottom - 2, 59, 2, iron);
    }
}
