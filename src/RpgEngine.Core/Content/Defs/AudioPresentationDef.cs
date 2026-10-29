namespace RpgEngine.Core.Content.Defs;

/// <summary>Optional graphical-client sound paths, relative to Data; gameplay never reads them.</summary>
public sealed record AudioPresentationDef
{
    public IReadOnlyDictionary<string, string> Effects { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, string> Ambience { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
