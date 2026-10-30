using Raylib_cs;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Layout;
using RpgEngine.Pixel.Presentation;

namespace RpgEngine.Pixel.Rendering;

/// <summary>Archive wires, loom and carillon details above both backdrop paths.</summary>
internal static class ArchiveDetails
{
    public static void Draw(PixelRect area, SceneFrame frame)
    {
        var state = frame.Archive ?? new ArchiveState();
        var time = Math.Floor(frame.Time * 4) / 4;
        switch (frame.Look.Style)
        {
            case SceneStyle.ArchiveStacks:
                for (var i = 0; i < 6; i++)
                {
                    var x = area.CenterX - 60 + i * 24;
                    var y = area.Y + 24 + (int)(Math.Sin(time * 0.6 + i) * 2);
                    var color = Palette.Alpha(Palette.Of(frame.Look.Accent), 85);
                    Raylib.DrawLine(x, area.Y + 4, x + 2, y, color);
                    Raylib.DrawRectangle(x, y, 5, 7, color);
                }
                break;
            case SceneStyle.TallowLoom:
                Loom(area, frame, state, time);
                break;
            case SceneStyle.VowNursery:
                Carillon(area, frame, state, time);
                break;
        }
    }

    private static void Loom(PixelRect area, SceneFrame frame, ArchiveState state, double time)
    {
        var color = Palette.Of(frame.Look.Accent);
        var top = area.Y + 20;
        var bottom = area.Y + 84;
        for (var i = 0; i < 20; i++)
        {
            var x = area.CenterX - 52 + i * 5;
            var wave = state.LoomSlack ? 0 : (int)(Math.Sin(time * 1.5 + i * 0.5) * 2);
            if (state.LoomSlack)
            {
                Raylib.DrawLine(x, top, x + 3, top + 39 + i % 3 * 7, Palette.Alpha(color, 95));
            }
            else
            {
                Raylib.DrawLine(x, top, x + wave, bottom, Palette.Alpha(color, 130));
            }
        }
        for (var side = -1; side <= 1; side += 2)
        {
            var x = area.CenterX + side * 104;
            SceneProps.Flame(x, area.Y + 108, 7, time, side + 7, color, Palette.Narration);
        }
    }

    private static void Carillon(PixelRect area, SceneFrame frame, ArchiveState state, double time)
    {
        var x = area.CenterX;
        var y = area.Y + 72;
        var brass = Palette.Of(frame.Look.Accent);
        var glass = Palette.Alpha(Palette.Muted, 190);
        Raylib.DrawLine(x - 17, area.Y + 4, x - 17, y - 18, glass);
        Raylib.DrawLine(x + 17, area.Y + 4, x + 17, y - 18, glass);
        Raylib.DrawRectangleLines(x - 29, y - 19, 58, 39, brass);
        Raylib.DrawRectangle(x - 32, y + 18, 64, 4, brass);
        Raylib.DrawRectangle(x - 24, y - 16, 48, 31, Palette.Alpha(Palette.Ink, 180));
        for (var side = -1; side <= 1; side += 2)
        {
            Raylib.DrawLine(x + side * 23, y - 15, x + side * 23, y + 14, glass);
        }
        if (state.SpiritReleased)
        {
            Raylib.DrawLine(x - 29, y - 19, x - 17, y - 30, brass);
            Raylib.DrawLine(x - 17, y - 30, x + 29, y - 30, brass);
            return;
        }
        var sway = state.CarillonStopped ? 0 : (int)Math.Round(Math.Sin(time * 1.5));
        var spirit = Palette.Alpha(state.CarillonStopped ? Palette.Muted : Palette.Narration, 170);
        Raylib.DrawCircle(x + sway, y - 7, 3, spirit);
        Raylib.DrawRectangle(x - 3 + sway, y - 2, 7, 12, spirit);
        Raylib.DrawLine(x + sway, y + 4, x + 17, y + 7, glass);
    }
}
