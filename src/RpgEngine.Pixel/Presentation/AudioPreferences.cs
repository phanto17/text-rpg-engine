namespace RpgEngine.Pixel.Presentation;

public sealed record AudioPreferences
{
    public int Version { get; init; } = 1;
    public bool Muted { get; init; }
    public float EffectsVolume { get; init; } = 0.45f;
    public float AmbienceVolume { get; init; } = 0.20f;

    public AudioPreferences Normalize() => this with
    {
        Version = 1,
        EffectsVolume = float.IsFinite(EffectsVolume) ? Math.Clamp(EffectsVolume, 0, 1) : 0.45f,
        AmbienceVolume = float.IsFinite(AmbienceVolume) ? Math.Clamp(AmbienceVolume, 0, 1) : 0.20f,
    };
}
