namespace RpgEngine.Core.Content.Defs;

public sealed record MonsterAttackDef
{
    public required string Name { get; init; }

    public required int ToHit { get; init; }

    public required string Damage { get; init; }

    public required DamageType DamageType { get; init; }

    /// <summary>Relative chance of choosing this attack.</summary>
    public int Weight { get; init; } = 1;

    public OnHitDef? OnHit { get; init; }
}
