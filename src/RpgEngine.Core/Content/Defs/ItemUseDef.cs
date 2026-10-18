namespace RpgEngine.Core.Content.Defs;

/// <summary>
/// How an item is used. Self-targeted items run <see cref="Effects"/> on the user; enemy-targeted
/// items (thrown flasks, bombs) resolve <see cref="Resolution"/> against a foe, exactly like a spell.
/// </summary>
public sealed record ItemUseDef
{
    public bool InCombat { get; init; }

    public bool Consume { get; init; } = true;

    public SpellTarget Target { get; init; } = SpellTarget.Self;

    public IReadOnlyList<EffectDef> Effects { get; init; } = [];

    public SpellResolutionDef? Resolution { get; init; }
}
