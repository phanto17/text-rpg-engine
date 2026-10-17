namespace RpgEngine.Core.Content.Defs;

public sealed record OutcomeDef
{
    public string? Text { get; init; }

    public IReadOnlyList<EffectDef> Effects { get; init; } = [];
}
