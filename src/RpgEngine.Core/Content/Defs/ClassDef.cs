using RpgEngine.Core.Characters;

namespace RpgEngine.Core.Content.Defs;

public sealed record ClassDef : IContentDef
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required int HitDie { get; init; }

    /// <summary>Order in which the standard array (or rolled scores) is auto-assigned.</summary>
    public required IReadOnlyList<Ability> AbilityPriority { get; init; }

    public IReadOnlyList<Ability> SavingThrows { get; init; } = [];

    public required SkillChoicesDef SkillChoices { get; init; }

    public int ExpertiseCount { get; init; }

    /// <summary>Spell points; also defines the casting ability. Null for non-casters.</summary>
    public ManaDef? Mana { get; init; }

    public IReadOnlyList<StartingItemDef> StartingItems { get; init; } = [];

    public IReadOnlyList<string> Spells { get; init; } = [];

    /// <summary>Spells and techniques learned on reaching later levels.</summary>
    public IReadOnlyList<SpellGrantDef> SpellsByLevel { get; init; } = [];
}
