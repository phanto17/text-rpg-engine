namespace RpgEngine.Pixel.Presentation;

/// <summary>
/// The lantern glow. Its color temperature warms with the configured resonance counter,
/// from the pale frost of a newly lit lantern to the golden amber of one that has heard every echo.
/// </summary>
public static class LanternLight
{
    /// <summary>Resonance at and beyond which the light is fully amber.</summary>
    public const int FullResonance = 12;

    public static readonly PixelColor Frost = new(176, 214, 255);
    public static readonly PixelColor Candle = new(255, 226, 170);
    public static readonly PixelColor Amber = new(255, 176, 64);

    /// <summary>Frost at 0, candlelight halfway, amber at <see cref="FullResonance"/>.</summary>
    public static PixelColor ColorFor(int resonance)
    {
        var t = Math.Clamp(resonance / (double)FullResonance, 0, 1);
        return t < 0.5 ? Frost.Lerp(Candle, t * 2) : Candle.Lerp(Amber, (t - 0.5) * 2);
    }

    /// <summary>How far the light reaches, in canvas pixels: a little further the warmer it burns.</summary>
    public static int RadiusFor(int resonance) => 54 + (int)(26 * Math.Clamp(resonance / (double)FullResonance, 0, 1));
}
