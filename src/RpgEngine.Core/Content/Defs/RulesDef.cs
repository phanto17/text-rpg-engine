namespace RpgEngine.Core.Content.Defs;

public sealed record RulesDef
{
    public required IReadOnlyList<int> StandardArray { get; init; }

    public required IReadOnlyList<int> ProficiencyBonusByLevel { get; init; }

    public bool NaturalCritsOnChecks { get; init; } = true;

    public MonsterHpMode MonsterHp { get; init; } = MonsterHpMode.Average;

    /// <summary>The check made to escape combat.</summary>
    public required CheckDef Flee { get; init; }

    /// <summary>Status applied by the Defend combat action.</summary>
    public required string DefendStatus { get; init; }
}
