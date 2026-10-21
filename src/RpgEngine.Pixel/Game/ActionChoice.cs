using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Pixel.Game;

/// <summary>
/// One button on the action bar. <see cref="Run"/> performs it; <see cref="Hint"/> is a longer
/// description shown while the button is hovered; a disabled choice is drawn dim and does nothing.
/// </summary>
public sealed record ActionChoice(string Label, LogTone Tone, Action Run, bool Enabled = true, string? Hint = null);
