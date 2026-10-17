namespace RpgEngine.Core.Content.Defs;

/// <summary><c>{ "mana": n }</c>, <c>{ "usesPerEncounter": n }</c>, or <c>{}</c> for free.</summary>
public sealed record SpellCostDef
{
    public int Mana { get; init; }

    /// <summary>Uses available per combat; recharged automatically when combat starts and ends.</summary>
    public int? UsesPerEncounter { get; init; }
}
