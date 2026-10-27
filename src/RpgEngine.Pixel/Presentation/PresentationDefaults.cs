using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Pixel.Presentation;

/// <summary>Content-independent fallbacks for missing or incomplete presentation data.</summary>
public static class PresentationDefaults
{
    public static readonly RoomLook GenericRoom = new(SceneStyle.OssuaryVault, WeatherKind.AshMotes,
        PixelColor.Parse("#14110f"), PixelColor.Parse("#2b241e"), PixelColor.Parse("#4a3b2c"), PixelColor.Parse("#c08a3e"), null);

    public static readonly FigureLook GenericMonster = new(Emblem.ThrallSkull,
        PixelColor.Parse("#8a8070"), PixelColor.Parse("#d0c8a0"), string.Empty, null);

    public static readonly FigureLook GenericNpc = new(Emblem.Hooded,
        PixelColor.Parse("#4a4a58"), PixelColor.Parse("#d8c8a0"), string.Empty, null);
}
