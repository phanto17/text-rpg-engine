using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Pixel.Presentation;

/// <summary>
/// Everything the scene viewport needs to draw a room, with every gap already filled by a default.
/// <see cref="Caption"/> is the room's mood caption, or null for none.
/// </summary>
public sealed record RoomLook(
    SceneStyle Style,
    WeatherKind Weather,
    PixelColor BackgroundTop,
    PixelColor BackgroundBottom,
    PixelColor Architecture,
    PixelColor Accent,
    string? BackdropImage,
    string? Caption = null);
