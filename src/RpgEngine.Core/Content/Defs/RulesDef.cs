namespace RpgEngine.Core.Content.Defs;

public sealed record RulesDef
{
    public required IReadOnlyList<int> StandardArray { get; init; }

    public required IReadOnlyList<int> ProficiencyBonusByLevel { get; init; }

    /// <summary>
    /// Total XP needed for each level: index 0 is level 1 (always 0), index 1 is level 2, and so on.
    /// The list length is the maximum level; the default allows no leveling at all.
    /// </summary>
    public IReadOnlyList<int> XpThresholdsByLevel { get; init; } = [0];

    public bool NaturalCritsOnChecks { get; init; } = true;

    public MonsterHpMode MonsterHp { get; init; } = MonsterHpMode.Average;

    /// <summary>The check made to escape combat.</summary>
    public required CheckDef Flee { get; init; }

    /// <summary>Status applied by the Defend combat action.</summary>
    public required string DefendStatus { get; init; }
}
