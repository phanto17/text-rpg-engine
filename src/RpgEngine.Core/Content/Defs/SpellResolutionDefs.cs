using System.Text.Json.Serialization;
using RpgEngine.Core.Characters;

namespace RpgEngine.Core.Content.Defs;

// The resolution family is a small closed set of records, kept together in one file.

/// <summary>How a spell or technique resolves, selected by <c>"type"</c>.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AutoHitResolutionDef), "autoHit")]
[JsonDerivedType(typeof(SpellAttackResolutionDef), "spellAttack")]
[JsonDerivedType(typeof(WeaponAttackResolutionDef), "weaponAttack")]
[JsonDerivedType(typeof(SaveResolutionDef), "save")]
[JsonDerivedType(typeof(EffectsResolutionDef), "effects")]
public abstract record SpellResolutionDef;

/// <summary>Always hits: roll <see cref="Damage"/>.</summary>
public sealed record AutoHitResolutionDef : SpellResolutionDef
{
    public required string Damage { get; init; }

    public required DamageType DamageType { get; init; }
}

/// <summary>d20 + proficiency + casting modifier vs AC; may apply a status on hit.</summary>
public sealed record SpellAttackResolutionDef : SpellResolutionDef
{
    public required string Damage { get; init; }

    public required DamageType DamageType { get; init; }

    public string? Status { get; init; }
}

/// <summary>A normal weapon attack with optional bonus damage dice (e.g. Sneak Attack).</summary>
public sealed record WeaponAttackResolutionDef : SpellResolutionDef
{
    public string? BonusDamage { get; init; }
}

/// <summary>Target saves against <see cref="Dc"/> (default 8 + proficiency + casting modifier).</summary>
public sealed record SaveResolutionDef : SpellResolutionDef
{
    public required Ability Ability { get; init; }

    public int? Dc { get; init; }

    public required string Damage { get; init; }

    public required DamageType DamageType { get; init; }

    public bool HalfOnSave { get; init; }

    public string? Status { get; init; }
}

/// <summary>Runs effects on the caster (healing, buffs).</summary>
public sealed record EffectsResolutionDef : SpellResolutionDef
{
    public required IReadOnlyList<EffectDef> Effects { get; init; }
}
