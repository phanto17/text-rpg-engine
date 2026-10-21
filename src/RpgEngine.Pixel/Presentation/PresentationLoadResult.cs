using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Pixel.Presentation;

/// <summary>What <see cref="PresentationLoader"/> read. <see cref="Warnings"/> lists every entry it had to ignore.</summary>
public sealed record PresentationLoadResult(PresentationDef Presentation, bool FileFound, IReadOnlyList<string> Warnings);
