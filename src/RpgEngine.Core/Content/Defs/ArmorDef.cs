namespace RpgEngine.Core.Content.Defs;

/// <summary>AC = <see cref="BaseAc"/> + DEX modifier, capped at <see cref="MaxDexBonus"/> (null = no cap).</summary>
public sealed record ArmorDef
{
    public required int BaseAc { get; init; }

    public int? MaxDexBonus { get; init; }
}
