using Raylib_cs;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Layout;
using RpgEngine.Pixel.Presentation;

namespace RpgEngine.Pixel.Rendering;

/// <summary>Bell, bier and brazier remain live over painted and procedural architecture.</summary>
internal static class FinaleDetails
{
    public static void Draw(PixelRect area, SceneFrame frame)
    {
        var ending = frame.Finale?.Ending ?? FinaleEnding.None;
        if (frame.Look.Style == SceneStyle.SunderedBellNave) Bell(area, frame, ending);
        else if (frame.Look.Style == SceneStyle.CradleOfAsh) Cradle(area, frame, ending);
    }

    private static void Bell(PixelRect area, SceneFrame frame, FinaleEnding ending)
    {
        var time = Math.Floor(frame.Time * 4) / 4;
        var x = area.CenterX + (int)Math.Round(Math.Sin(time * 0.7));
        var y = area.Y + 27;
        var bronze = new Color(89, 116, 101, 255);
        for (var cy = area.Y; cy < y + 9; cy += 4)
        {
            Raylib.DrawRectangle(x - 16, cy, 3, 3, Palette.Muted);
            Raylib.DrawRectangle(x + 13, cy, 3, 3, Palette.Muted);
        }
        Raylib.DrawEllipse(x, y + 16, 25, 16, bronze);
        Raylib.DrawRectangle(x - 25, y + 16, 50, 33, bronze);
        Raylib.DrawRectangle(x - 25, y + 23, 7, 24, new Color(60, 69, 84, 255));
        Raylib.DrawRectangle(x + 18, y + 20, 7, 27, new Color(37, 43, 56, 255));
        Raylib.DrawLine(x - 21, y + 35, x + 20, y + 35, Palette.Muted);
        Raylib.DrawLine(x - 22, y + 38, x + 21, y + 38, Palette.Muted);
        for (var rivet = -15; rivet <= 15; rivet += 10)
            Raylib.DrawPixel(x + rivet, y + 41, Palette.Gold);
        Raylib.DrawRectangle(x - 29, y + 47, 58, 6, Palette.PanelEdgeLight);
        Raylib.DrawLine(x - 19, y + 12, x - 19, y + 45, Palette.Narration);
        Raylib.DrawCircle(x, y + 57, 3, Palette.Muted);
        // Empty sockets do not become filled just because the player carries seals.
        Raylib.DrawRectangle(x - 19, y + 48, 6, 4, Palette.Ink);
        Raylib.DrawRectangle(x + 13, y + 48, 6, 4, Palette.Ink);
        if (ending != FinaleEnding.Triad)
        {
            Raylib.DrawLine(x + 3, y + 2, x, y + 24, Palette.Ink);
            Raylib.DrawLine(x, y + 24, x + 5, y + 43, Palette.Ink);
            Raylib.DrawLine(x + 5, y + 43, x + 2, y + 49, Palette.Ink);
            Raylib.DrawLine(x, y + 24, x - 5, y + 30, Palette.Ink);
            // The splinter repairs only the lip notch; the main fracture persists.
            Raylib.DrawTriangle(new(x - 2, y + 53), new(x + 6, y + 53), new(x + 2, y + 46),
                frame.BellMended ? Palette.Gold : Palette.Ink);
        }
    }

    private static void Cradle(PixelRect area, SceneFrame frame, FinaleEnding ending)
    {
        var x = area.CenterX;
        var y = area.Y + 104;
        var time = Math.Floor(frame.Time * 4) / 4;
        var flame = ending == FinaleEnding.Moon ? new Color(170, 184, 197, 255) : Palette.Gold;
        var core = ending == FinaleEnding.Moon ? new Color(162, 155, 196, 255) : Palette.Narration;
        Raylib.DrawEllipse(x, y - 8, 43, 11, Palette.PanelEdgeLight);
        Raylib.DrawRectangle(x - 43, y - 8, 86, 8, Palette.PanelEdgeLight);
        Raylib.DrawEllipse(x, y - 12, 39, 7, Palette.Ink);
        Raylib.DrawRectangle(x - 20, y, 40, 3, Palette.Muted);
        if (frame.BrazierLit || ending != FinaleEnding.None)
        {
            SceneProps.Flame(x, y - 12, 40, time, 10, flame, core);
            SceneProps.Flame(x - 16, y - 12, 24, time, 11, flame, core);
            SceneProps.Flame(x + 16, y - 12, 24, time, 12, flame, core);
            if (ending == FinaleEnding.Sun)
                Raylib.DrawCircleLines(x, y - 33, 25, Palette.Alpha(flame, 110));
        }
        else
        {
            for (var i = 0; i < 7; i++)
                Raylib.DrawRectangle(x - 30 + i * 10, y - 14, 3, 2,
                    Palette.Alpha(Palette.Gold, (int)(120 + 80 * Math.Abs(Math.Sin(time + i)))));
        }
        Bier(area, ending);
    }

    private static void Bier(PixelRect area, FinaleEnding ending)
    {
        var x = area.X + 12;
        var y = area.Y + 90;
        // Repeated on the fallback path so both versions share the same state geometry.
        Raylib.DrawRectangle(x, y, 66, 10, Palette.Muted);
        Raylib.DrawRectangle(x + 3, y - 3, 60, 5, Palette.Narration);
        if (ending == FinaleEnding.Triad) return;
        Raylib.DrawRectangle(x + 9, y - 7, 44, 6, Palette.PanelEdgeLight);
        Raylib.DrawCircle(x + 9, y - 5, 4, Palette.Narration);
        Raylib.DrawRectangle(x + 30, y - 6, 8, 2, Palette.Narration);
        Raylib.DrawRectangle(x + 4, y - 12, 53, 12, Palette.Alpha(Palette.Gold, 95));
        Raylib.DrawRectangleLines(x + 4, y - 12, 53, 12, Palette.Gold);
    }
}
