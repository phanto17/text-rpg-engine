namespace RpgEngine.Pixel.Sequencing;

/// <summary>A run of text in one tone, so a line can mix a colored roll badge with plain narration.</summary>
public sealed record LogSpan(string Text, LogTone Tone);
