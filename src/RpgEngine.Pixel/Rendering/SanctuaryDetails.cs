using Raylib_cs;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Layout;
using RpgEngine.Pixel.Presentation;

namespace RpgEngine.Pixel.Rendering;

/// <summary>Relief, sanctuary fire and ally tokens shared by both backdrop paths.</summary>
internal static class SanctuaryDetails
{
    public static void Draw(PixelRect area, SceneFrame frame)
    {
        var state = frame.Sanctuary ?? new SanctuaryState();
        if (frame.Look.Style == SceneStyle.UncarvedStair) Relief(area, frame, state);
        else if (frame.Look.Style == SceneStyle.SanctuaryLanding) Landing(area, frame, state);
    }

    private static void Relief(PixelRect area, SceneFrame frame, SanctuaryState state)
    {
        var x = area.X + 12;
        var y = area.Y + 39;
        var bronze = Emblems.Shade(Palette.Gold, 0.28);
        Raylib.DrawRectangle(x, y, 90, 30, bronze);
        Raylib.DrawRectangleLines(x, y, 90, 30, Emblems.Shade(Palette.Gold, 0.65));
        Raylib.DrawCircleLines(x + 17, y + 12, 6, Palette.Gold);
        Raylib.DrawLine(x + 9, y + 12, x + 25, y + 12, Palette.Gold);
        Raylib.DrawLine(x + 17, y + 4, x + 17, y + 20, Palette.Gold);
        Raylib.DrawCircle(x + 44, y + 12, 6, Palette.Muted);
        Raylib.DrawCircle(x + 47, y + 10, 5, bronze);
        // The erased third sigil remains lead-filled even after its verse is understood.
        Raylib.DrawRectangle(x + 67, y + 6, 13, 13, Emblems.Shade(Palette.Muted, 0.8));
        Raylib.DrawLine(x + 68, y + 8, x + 73, y + 14, bronze);
        Raylib.DrawLine(x + 72, y + 8, x + 78, y + 17, bronze);
        var letters = state.FriezeRead ? Palette.Narration : Emblems.Shade(Palette.Gold, 0.4);
        for (var i = 0; i < 10; i++)
        {
            Raylib.DrawRectangle(x + 7 + i * 7, y + 24, 4, 1, letters);
        }
        if (frame.HasLantern)
        {
            var echo = Palette.Alpha(Palette.Of(frame.Look.Accent), 60);
            Raylib.DrawRectangle(area.CenterX + 42, area.Y + 74, 5, 5, echo);
            Raylib.DrawRectangle(area.CenterX + 40, area.Y + 81, 9, 13, echo);
            Raylib.DrawLine(area.CenterX + 48, area.Y + 85, area.CenterX + 55, area.Y + 78, echo);
        }
    }

    private static void Landing(PixelRect area, SceneFrame frame, SanctuaryState state)
    {
        var x = area.CenterX;
        var y = area.Y + 106;
        var stone = Palette.Of(frame.Look.Architecture);
        var accent = Palette.Of(frame.Look.Accent);
        var time = Math.Floor(frame.Time * 4) / 4;
        SceneProps.Brazier(x, y, 20, time, 9, stone, accent);
        if (state.HasChime)
        {
            var post = area.X + 68;
            Raylib.DrawLine(post + 3, area.Y + 68, post + 13, area.Y + 68, Palette.Muted);
            Raylib.DrawLine(post + 13, area.Y + 68, post + 13, area.Y + 84, Palette.Muted);
            Raylib.DrawRectangle(post + 11, area.Y + 82, 5, 8, Palette.Narration);
            Raylib.DrawLine(post + 12, area.Y + 83, post + 12, area.Y + 88, Palette.Muted);
        }
        if (state.HasDispatch)
        {
            Raylib.DrawRectangle(x - 47, y - 11, 13, 17, Palette.PanelEdgeLight);
            for (var i = 0; i < 3; i++)
            {
                Raylib.DrawRectangle(x - 45, y - 8 + i * 4, 8 - i, 1, Palette.Muted);
            }
            Raylib.DrawRectangle(x - 34, y + 1, 13, 5, Emblems.Shade(stone, 0.6));
        }
        var bowlX = area.X + 202;
        var bowlY = area.Y + 116;
        Raylib.DrawEllipse(bowlX, bowlY, 9, 3, Palette.PanelEdgeLight);
        Raylib.DrawEllipse(bowlX, bowlY - 1, 7, 2, Palette.Ink);
        if (state.OfferingMade) Raylib.DrawRectangle(bowlX - 2, bowlY - 2, 4, 2, Palette.Gold);
    }
}
