using RpgEngine.Core.Characters;

namespace RpgEngine.Core.Persistence;

public sealed record PlayerData
{
    public required string Name { get; init; }

    public required string ClassId { get; init; }

    public required int Level { get; init; }

    public required int Xp { get; init; }

    public required AbilityScores Abilities { get; init; }

    public required IReadOnlyList<string> ProficientSkills { get; init; }

    public required IReadOnlyList<string> ExpertiseSkills { get; init; }

    public required IReadOnlyList<Ability> SavingThrows { get; init; }

    public required IReadOnlyList<string> Spells { get; init; }

    public required int MaxHp { get; init; }

    public required int CurrentHp { get; init; }

    public required int MaxMana { get; init; }

    public required int CurrentMana { get; init; }

    public required int Gold { get; init; }

    public required IReadOnlyList<InventoryEntry> Inventory { get; init; }

    public string? EquippedWeapon { get; init; }

    public string? EquippedArmor { get; init; }
}
