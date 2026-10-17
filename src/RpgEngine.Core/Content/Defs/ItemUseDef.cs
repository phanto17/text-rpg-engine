namespace RpgEngine.Core.Content.Defs;

public sealed record ItemUseDef
{
    public bool InCombat { get; init; }

    public bool Consume { get; init; } = true;

    public required IReadOnlyList<EffectDef> Effects { get; init; }
}
