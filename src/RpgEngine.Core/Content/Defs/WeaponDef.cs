namespace RpgEngine.Core.Content.Defs;

public sealed record WeaponDef
{
    public required string Damage { get; init; }

    public required DamageType DamageType { get; init; }

    /// <summary>May use DEX instead of STR (whichever is higher).</summary>
    public bool Finesse { get; init; }

    /// <summary>Always uses DEX.</summary>
    public bool Ranged { get; init; }
}
