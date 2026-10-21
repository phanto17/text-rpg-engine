using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Pixel.Presentation;

/// <summary>Everything the scene viewport needs to draw a room, with every gap already filled by a default.</summary>
public sealed record RoomLook(
    SceneStyle Style,
    WeatherKind Weather,
    PixelColor BackgroundTop,
    PixelColor BackgroundBottom,
    PixelColor Architecture,
    PixelColor Accent,
    string? BackdropImage);
