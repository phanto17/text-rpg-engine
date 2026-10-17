namespace RpgEngine.Core.Content.Defs;

public sealed record StatusModifiersDef
{
    public AttackRollModifier AttackRolls { get; init; } = AttackRollModifier.Normal;

    public int AcBonus { get; init; }

    /// <summary>Dice rolled as damage at the start of each affected turn.</summary>
    public string? DamagePerTurn { get; init; }

    public bool SkipTurn { get; init; }
}
