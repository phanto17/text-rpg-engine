using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Pixel.Presentation;

/// <summary>
/// How a monster or NPC is drawn: its heraldic emblem and colors, an epithet, an optional picture,
/// and (for NPCs) the name shown under the portrait when it differs from the speaker's.
/// </summary>
public sealed record FigureLook(Emblem Emblem, PixelColor Primary, PixelColor Accent, string Title, string? Image, string? SpeakerName = null);
