namespace RpgEngine.Pixel.Presentation;

/// <summary>Whole-pixel poses for a heavy strike, independent of render frame rate.</summary>
public static class AttackMotion
{
    public const double AnticipationSeconds = 0.16;
    public const double ImpactSeconds = 0.24;
    public const double Duration = 0.60;

    public static int Offset(double remainingSeconds)
    {
        var age = Duration - remainingSeconds;
        return age switch
        {
            < 0 or >= Duration => 0,
            < AnticipationSeconds => -2,
            < 0.20 => 2,
            < ImpactSeconds => 5,
            < 0.36 => 8,
            < 0.44 => 5,
            < 0.52 => 2,
            _ => 0,
        };
    }
}
