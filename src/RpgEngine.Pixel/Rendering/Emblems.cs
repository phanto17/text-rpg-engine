using System.Numerics;
using Raylib_cs;
using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Pixel.Rendering;

/// <summary>Procedural heraldic devices for monsters and NPCs, drawn from simple shapes so every figure has a face without art assets.</summary>
internal static class Emblems
{
    /// <summary>Draws <paramref name="emblem"/> centered on (<paramref name="cx"/>, <paramref name="cy"/>) inside a <paramref name="size"/> square.</summary>
    public static void Draw(Emblem emblem, int cx, int cy, int size, Color primary, Color accent)
    {
        var dark = Shade(primary, 0.55);
        var light = Shade(primary, 1.35);
        var s = size / 40.0;
        int S(double v) => (int)Math.Round(v * s);

        switch (emblem)
        {
            case Emblem.KnightHelm:
                Raylib.DrawCircle(cx, cy - S(4), S(13), primary);
                Raylib.DrawRectangle(cx - S(13), cy - S(4), S(26), S(16), primary);
                Raylib.DrawRectangle(cx - S(10), cy - S(2), S(20), S(3), dark);
                Raylib.DrawRectangle(cx - S(1), cy - S(2), S(2), S(14), dark);
                Raylib.DrawTriangle(new Vector2(cx - S(2), cy - S(16)), new Vector2(cx + S(2), cy - S(16)), new Vector2(cx + S(8), cy - S(26)), accent);
                Raylib.DrawRectangle(cx - S(13), cy + S(8), S(26), S(2), light);
                break;
            case Emblem.ThrallSkull:
                Raylib.DrawCircle(cx, cy - S(4), S(13), primary);
                Raylib.DrawRectangle(cx - S(8), cy + S(4), S(16), S(9), primary);
                Raylib.DrawCircle(cx - S(5), cy - S(5), S(4), dark);
                Raylib.DrawCircle(cx + S(5), cy - S(5), S(4), dark);
                Raylib.DrawTriangle(new Vector2(cx, cy), new Vector2(cx - S(2), cy + S(4)), new Vector2(cx + S(2), cy + S(4)), dark);
                for (var i = -6; i <= 6; i += 4)
                {
                    Raylib.DrawRectangle(cx + S(i), cy + S(7), S(1), S(5), dark);
                }

                Raylib.DrawPixel(cx - S(5), cy - S(6), accent);
                Raylib.DrawPixel(cx + S(5), cy - S(6), accent);
                break;
            case Emblem.ChoirMask:
                Raylib.DrawEllipse(cx, cy, S(12), S(16), primary);
                Raylib.DrawEllipse(cx, cy, S(10), S(14), light);
                Raylib.DrawTriangle(new Vector2(cx - S(8), cy - S(4)), new Vector2(cx - S(2), cy - S(6)), new Vector2(cx - S(3), cy - S(1)), dark);
                Raylib.DrawTriangle(new Vector2(cx + S(2), cy - S(6)), new Vector2(cx + S(8), cy - S(4)), new Vector2(cx + S(3), cy - S(1)), dark);
                Raylib.DrawEllipse(cx, cy + S(7), S(3), S(5), dark);
                Raylib.DrawLine(cx - S(5), cy, cx - S(6), cy + S(8), accent);
                Raylib.DrawLine(cx + S(5), cy, cx + S(6), cy + S(8), accent);
                break;
            case Emblem.WardenHalberd:
            case Emblem.Halberdier:
                Raylib.DrawRectangle(cx - S(1), cy - S(18), S(3), S(36), dark);
                Raylib.DrawTriangle(new Vector2(cx + S(2), cy - S(16)), new Vector2(cx + S(2), cy - S(2)), new Vector2(cx + S(13), cy - S(9)), light);
                Raylib.DrawTriangle(new Vector2(cx, cy - S(20)), new Vector2(cx - S(2), cy - S(14)), new Vector2(cx + S(3), cy - S(14)), light);
                Raylib.DrawRectangle(cx - S(9), cy - S(4), S(8), S(14), primary);
                Raylib.DrawRectangle(cx - S(8), cy - S(10), S(6), S(6), primary);
                Raylib.DrawRectangle(cx - S(7), cy - S(8), S(4), S(1), accent);
                break;
            case Emblem.CarillonCradle:
                Raylib.DrawRing(new Vector2(cx, cy + S(2)), S(13), S(16), 0, 180, 16, primary);
                Raylib.DrawCircle(cx, cy - S(4), S(7), accent);
                Raylib.DrawRectangle(cx - S(7), cy - S(4), S(14), S(8), accent);
                Raylib.DrawRectangle(cx - S(9), cy + S(3), S(18), S(2), Shade(accent, 0.7));
                Raylib.DrawCircle(cx, cy + S(6), S(2), dark);
                break;
            case Emblem.IronBrazier:
                Raylib.DrawEllipse(cx, cy + S(2), S(13), S(6), primary);
                Raylib.DrawRectangle(cx - S(13), cy - S(2), S(26), S(4), primary);
                Raylib.DrawLine(cx - S(8), cy + S(6), cx - S(12), cy + S(16), dark);
                Raylib.DrawLine(cx + S(8), cy + S(6), cx + S(12), cy + S(16), dark);
                Raylib.DrawLine(cx, cy + S(8), cx, cy + S(16), dark);
                Flame(cx, cy - S(2), S(9), accent);
                break;
            case Emblem.ArchitectAstrolabe:
                Raylib.DrawCircleLines(cx, cy, S(16), primary);
                Raylib.DrawCircleLines(cx, cy, S(15), primary);
                Raylib.DrawCircleLines(cx, cy, S(10), accent);
                Raylib.DrawLine(cx - S(16), cy, cx + S(16), cy, primary);
                Raylib.DrawLine(cx, cy - S(16), cx, cy + S(16), primary);
                Raylib.DrawLine(cx - S(11), cy - S(11), cx + S(11), cy + S(11), accent);
                Raylib.DrawCircle(cx, cy, S(3), accent);
                Raylib.DrawCircle(cx + S(10), cy, S(2), light);
                Raylib.DrawCircle(cx, cy - S(15), S(2), light);
                break;
            case Emblem.LanternNun:
                Hood(cx, cy, s, primary, dark);
                Raylib.DrawRectangle(cx + S(9), cy + S(2), S(6), S(9), accent);
                Raylib.DrawRectangle(cx + S(10), cy, S(4), S(2), dark);
                Raylib.DrawLine(cx + S(12), cy - S(3), cx + S(12), cy, dark);
                Raylib.DrawCircle(cx + S(12), cy + S(6), S(5), Palette.Alpha(accent, 70));
                break;
            case Emblem.Scribe:
                Raylib.DrawRectangle(cx - S(12), cy - S(6), S(24), S(18), light);
                Raylib.DrawRectangle(cx - S(12), cy - S(6), S(24), S(2), dark);
                Raylib.DrawRectangle(cx - S(12), cy + S(10), S(24), S(2), dark);
                for (var i = 0; i < 3; i++)
                {
                    Raylib.DrawRectangle(cx - S(8), cy - S(1) + S(i * 4), S(14), S(1), primary);
                }

                Raylib.DrawLine(cx + S(4), cy + S(8), cx + S(14), cy - S(14), accent);
                Raylib.DrawTriangle(new Vector2(cx + S(14), cy - S(14)), new Vector2(cx + S(8), cy - S(12)), new Vector2(cx + S(13), cy - S(7)), accent);
                break;
            case Emblem.Peddler:
                Raylib.DrawRectangleRounded(new Rectangle(cx - S(12), cy - S(10), S(24), S(24)), 0.3f, 4, primary);
                Raylib.DrawRectangle(cx - S(12), cy - S(2), S(24), S(2), dark);
                Raylib.DrawRectangle(cx - S(8), cy - S(14), S(2), S(6), dark);
                Raylib.DrawRectangle(cx + S(6), cy - S(14), S(2), S(6), dark);
                Raylib.DrawCircle(cx + S(6), cy + S(7), S(4), accent);
                Raylib.DrawCircleLines(cx + S(6), cy + S(7), S(2), dark);
                break;
            case Emblem.Keeper:
                Raylib.DrawRectangle(cx - S(2), cy - S(4), S(4), S(20), primary);
                Raylib.DrawRectangle(cx - S(8), cy + S(14), S(16), S(3), primary);
                Raylib.DrawRectangle(cx - S(7), cy - S(14), S(14), S(12), dark);
                Raylib.DrawRectangle(cx - S(5), cy - S(12), S(10), S(8), accent);
                Raylib.DrawCircle(cx, cy - S(8), S(8), Palette.Alpha(accent, 60));
                break;
            case Emblem.Quartermaster:
                Raylib.DrawLine(cx - S(12), cy - S(12), cx + S(12), cy + S(12), light);
                Raylib.DrawLine(cx + S(12), cy - S(12), cx - S(12), cy + S(12), light);
                Raylib.DrawCircleLines(cx - S(12), cy - S(12), S(4), accent);
                Raylib.DrawCircleLines(cx + S(12), cy - S(12), S(4), accent);
                Raylib.DrawRectangle(cx + S(8), cy + S(8), S(4), S(2), light);
                Raylib.DrawRectangle(cx - S(12), cy + S(8), S(4), S(2), light);
                break;
            case Emblem.Monk:
                Hood(cx, cy, s, primary, dark);
                Raylib.DrawRectangle(cx - S(5), cy + S(8), S(4), S(5), light);
                Raylib.DrawRectangle(cx + S(1), cy + S(8), S(4), S(5), light);
                break;
            case Emblem.Wright:
                Raylib.DrawRectangle(cx - S(2), cy - S(8), S(4), S(24), dark);
                Raylib.DrawRectangle(cx - S(12), cy - S(16), S(24), S(10), primary);
                Raylib.DrawRectangle(cx - S(12), cy - S(16), S(24), S(2), light);
                Raylib.DrawLine(cx + S(6), cy + S(14), cx + S(14), cy - S(2), accent);
                Raylib.DrawTriangle(new Vector2(cx + S(14), cy - S(2)), new Vector2(cx + S(12), cy - S(6)), new Vector2(cx + S(16), cy - S(5)), light);
                break;
            case Emblem.Lancer:
                Raylib.DrawLine(cx - S(14), cy + S(16), cx + S(12), cy - S(16), light);
                Raylib.DrawLine(cx - S(13), cy + S(16), cx + S(13), cy - S(16), light);
                Raylib.DrawTriangle(new Vector2(cx + S(13), cy - S(17)), new Vector2(cx + S(9), cy - S(9)), new Vector2(cx + S(16), cy - S(12)), accent);
                Raylib.DrawTriangle(new Vector2(cx + S(2), cy - S(4)), new Vector2(cx + S(2), cy + S(6)), new Vector2(cx - S(8), cy - S(2)), primary);
                break;
            case Emblem.Mournling:
                Raylib.DrawCircle(cx, cy, S(15), primary);
                Raylib.DrawLine(cx - S(6), cy - S(15), cx - S(2), cy - S(4), dark);
                Raylib.DrawLine(cx - S(2), cy - S(4), cx - S(6), cy + S(6), dark);
                Raylib.DrawLine(cx + S(4), cy + S(2), cx + S(10), cy + S(12), dark);
                Raylib.DrawRectangle(cx - S(8), cy - S(5), S(4), S(3), dark);
                Raylib.DrawRectangle(cx + S(4), cy - S(5), S(4), S(3), dark);
                Raylib.DrawRectangle(cx - S(7), cy - S(2), S(2), S(8), accent);
                Raylib.DrawRectangle(cx + S(5), cy - S(2), S(2), S(10), accent);
                Raylib.DrawEllipse(cx, cy + S(7), S(4), S(2), dark);
                break;
            case Emblem.Chorister:
                Hood(cx - S(4), cy, s, primary, dark);
                Raylib.DrawEllipse(cx - S(4), cy + S(2), S(2), S(3), accent);
                Raylib.DrawRing(new Vector2(cx + S(2), cy + S(2)), S(6), S(7), -40, 40, 8, accent);
                Raylib.DrawRing(new Vector2(cx + S(2), cy + S(2)), S(11), S(12), -40, 40, 8, Palette.Alpha(accent, 160));
                Raylib.DrawRing(new Vector2(cx + S(2), cy + S(2)), S(16), S(17), -40, 40, 8, Palette.Alpha(accent, 90));
                break;
            case Emblem.Prelate:
                Raylib.DrawTriangle(new Vector2(cx - S(9), cy - S(2)), new Vector2(cx + S(9), cy - S(2)), new Vector2(cx, cy - S(20)), accent);
                Raylib.DrawRectangle(cx - S(9), cy - S(3), S(18), S(3), Shade(accent, 0.7));
                Raylib.DrawRectangle(cx - S(1), cy - S(14), S(2), S(10), primary);
                Raylib.DrawRectangle(cx - S(9), cy, S(18), S(18), primary);
                Raylib.DrawRectangle(cx - S(1), cy + S(2), S(2), S(14), accent);
                Raylib.DrawRectangle(cx - S(4), cy + S(6), S(8), S(2), accent);
                Raylib.DrawLine(cx + S(13), cy - S(16), cx + S(13), cy + S(18), light);
                Raylib.DrawCircleLines(cx + S(13), cy - S(16), S(3), light);
                break;
            case Emblem.Commander:
                Raylib.DrawCircle(cx, cy - S(2), S(12), primary);
                Raylib.DrawRectangle(cx - S(12), cy - S(2), S(24), S(12), primary);
                Raylib.DrawRectangle(cx - S(9), cy, S(18), S(3), dark);
                Raylib.DrawRectangle(cx - S(2), cy - S(18), S(4), S(10), accent);
                Raylib.DrawRectangle(cx - S(6), cy - S(16), S(12), S(3), accent);
                Raylib.DrawLine(cx - S(16), cy + S(16), cx - S(4), cy + S(4), light);
                Raylib.DrawLine(cx + S(16), cy + S(16), cx + S(4), cy + S(4), light);
                break;
            default:
                Hood(cx, cy, s, primary, dark);
                Raylib.DrawPixel(cx - S(3), cy - S(1), accent);
                Raylib.DrawPixel(cx + S(3), cy - S(1), accent);
                break;
        }
    }

    public static void Flame(int cx, int baseY, int height, Color accent)
    {
        var core = new Color(255, 240, 180, 255);
        Raylib.DrawTriangle(new Vector2(cx - height / 2, baseY), new Vector2(cx + height / 2, baseY), new Vector2(cx, baseY - height), accent);
        Raylib.DrawTriangle(new Vector2(cx - height / 4, baseY), new Vector2(cx + height / 4, baseY), new Vector2(cx, baseY - height / 2), core);
    }

    public static Color Shade(Color color, double factor) => new(
        (byte)Math.Clamp(color.R * factor, 0, 255),
        (byte)Math.Clamp(color.G * factor, 0, 255),
        (byte)Math.Clamp(color.B * factor, 0, 255),
        color.A);

    private static void Hood(int cx, int cy, double s, Color primary, Color dark)
    {
        int S(double v) => (int)Math.Round(v * s);
        Raylib.DrawCircle(cx, cy - S(4), S(12), primary);
        Raylib.DrawRectangle(cx - S(12), cy - S(4), S(24), S(22), primary);
        Raylib.DrawEllipse(cx, cy - S(2), S(7), S(9), dark);
        Raylib.DrawRectangle(cx - S(14), cy + S(14), S(28), S(4), Shade(primary, 0.8));
    }
}
