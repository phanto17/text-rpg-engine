using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Pixel.Presentation;

/// <summary>How a monster or NPC is drawn: its heraldic emblem and colors, an epithet, and an optional picture.</summary>
public sealed record FigureLook(Emblem Emblem, PixelColor Primary, PixelColor Accent, string Title, string? Image);
