namespace RpgEngine.Core.Content.Defs;

public sealed record MonsterDef : IContentDef
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    /// <summary>Hit dice, e.g. <c>3d8+2</c>; see <see cref="RulesDef.MonsterHp"/>.</summary>
    public required string Hp { get; init; }

    public required int Ac { get; init; }

    public required AbilityBlockDef Abilities { get; init; }

    public required IReadOnlyList<MonsterAttackDef> Attacks { get; init; }

    public IReadOnlyList<DamageType> Resistances { get; init; } = [];

    public IReadOnlyList<DamageType> Vulnerabilities { get; init; } = [];

    public IReadOnlyList<DamageType> Immunities { get; init; } = [];

    public int Xp { get; init; }

    public MonsterAiDef Ai { get; init; } = new();

    public IReadOnlyList<LootDef> Loot { get; init; } = [];
}
