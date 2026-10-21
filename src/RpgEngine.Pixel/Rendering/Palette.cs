using Raylib_cs;
using RpgEngine.Pixel.Presentation;
using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Pixel.Rendering;

/// <summary>Every color the client draws with, in one place. The tones match the terminal theme.</summary>
internal static class Palette
{
    public static readonly Color Title = Hex("#d7af5f");
    public static readonly Color Accent = Hex("#d7af5f");
    public static readonly Color Narration = Hex("#e4dccb");
    public static readonly Color Sensory = Hex("#8a8a8a");
    public static readonly Color Speaker = Hex("#e8c070");
    public static readonly Color Dialogue = Hex("#f0e6d2");
    public static readonly Color System = Hex("#87afd7");
    public static readonly Color Success = Hex("#87d787");
    public static readonly Color Failure = Hex("#d75f5f");
    public static readonly Color Warning = Hex("#d7af00");
    public static readonly Color Critical = Hex("#ff87ff");
    public static readonly Color Muted = Hex("#8c8690");
    public static readonly Color Danger = Hex("#e04848");
    public static readonly Color Mana = Hex("#6f97e7");
    public static readonly Color Gold = Hex("#ffd75f");

    public static readonly Color Letterbox = Hex("#050408");
    public static readonly Color PanelBack = Hex("#0e0b12");
    public static readonly Color PanelEdge = Hex("#3b3344");
    public static readonly Color PanelEdgeLight = Hex("#5a4f66");
    public static readonly Color HudBack = Hex("#140f19");
    public static readonly Color ButtonBack = Hex("#231c2a");
    public static readonly Color ButtonHover = Hex("#3c2f48");
    public static readonly Color ButtonEdge = Hex("#4d4058");
    public static readonly Color InputBack = Hex("#0a080d");
    public static readonly Color HpFill = Hex("#c23b2e");
    public static readonly Color HpBack = Hex("#3a1a1a");
    public static readonly Color ManaFill = Hex("#2f6fd0");
    public static readonly Color ManaBack = Hex("#17203a");
    public static readonly Color XpFill = Hex("#b8860b");
    public static readonly Color XpBack = Hex("#2a2210");
    public static readonly Color Ink = Hex("#0a0a0c");
    public static readonly Color Flash = new(255, 255, 255, 170);

    public static Color For(LogTone tone) => tone switch
    {
        LogTone.Sensory => Sensory,
        LogTone.Dialogue => Dialogue,
        LogTone.Speaker => Speaker,
        LogTone.System => System,
        LogTone.Success => Success,
        LogTone.Failure => Failure,
        LogTone.Warning => Warning,
        LogTone.Critical => Critical,
        LogTone.Muted => Muted,
        LogTone.Mana => Mana,
        LogTone.Gold => Gold,
        LogTone.Accent => Accent,
        LogTone.Title => Title,
        LogTone.Danger => Danger,
        _ => Narration,
    };

    public static Color Hex(string hex) => Of(PixelColor.Parse(hex));

    public static Color Of(PixelColor color) => new(color.R, color.G, color.B, color.A);

    public static Color Alpha(Color color, int alpha) => new(color.R, color.G, color.B, (byte)Math.Clamp(alpha, 0, 255));

    /// <summary>A color from channel values that may have been computed; each is clamped to 0..255.</summary>
    public static Color Rgba(int r, int g, int b, int a) =>
        new((byte)Math.Clamp(r, 0, 255), (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(b, 0, 255), (byte)Math.Clamp(a, 0, 255));
}
