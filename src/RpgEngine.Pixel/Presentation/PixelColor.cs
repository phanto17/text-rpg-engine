using System.Globalization;

namespace RpgEngine.Pixel.Presentation;

/// <summary>An RGBA color the logic layer can reason about without the renderer's types.</summary>
public readonly record struct PixelColor(byte R, byte G, byte B, byte A = 255)
{
    public static readonly PixelColor White = new(255, 255, 255);
    public static readonly PixelColor Black = new(0, 0, 0);

    /// <summary>Parses <c>#rgb</c>, <c>#rrggbb</c>, or <c>#rrggbbaa</c> (the hash is optional); returns null for anything else.</summary>
    public static PixelColor? TryParse(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return null;
        }

        var digits = hex.Trim().TrimStart('#');
        if (digits.Length == 3)
        {
            digits = string.Concat(digits.Select(c => new string(c, 2)));
        }

        if (digits.Length is not (6 or 8) || !uint.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        if (digits.Length == 6)
        {
            value = (value << 8) | 0xFF;
        }

        return new PixelColor((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
    }

    public static PixelColor Parse(string hex) =>
        TryParse(hex) ?? throw new FormatException($"'{hex}' is not a hex color.");

    public PixelColor WithAlpha(byte alpha) => this with { A = alpha };

    /// <summary>Linear blend toward <paramref name="other"/>; <paramref name="t"/> is clamped to 0..1.</summary>
    public PixelColor Lerp(PixelColor other, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return new PixelColor(Mix(R, other.R, t), Mix(G, other.G, t), Mix(B, other.B, t), Mix(A, other.A, t));
    }

    /// <summary>Multiplies the channels by <paramref name="factor"/> (darker below 1, lighter above), clamped.</summary>
    public PixelColor Scale(double factor) =>
        new(Clamp(R * factor), Clamp(G * factor), Clamp(B * factor), A);

    public override string ToString() => $"#{R:x2}{G:x2}{B:x2}{(A == 255 ? string.Empty : A.ToString("x2", CultureInfo.InvariantCulture))}";

    private static byte Mix(byte a, byte b, double t) => (byte)Math.Round(a + ((b - a) * t));

    private static byte Clamp(double value) => (byte)Math.Clamp(Math.Round(value), 0, 255);
}
