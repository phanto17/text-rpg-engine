namespace RpgEngine.Core.Dice;

/// <summary>A d20 roll. With advantage or disadvantage, <see cref="Faces"/> holds both dice and <see cref="Natural"/> the one used.</summary>
public sealed record D20Roll(RollMode Mode, int Natural, IReadOnlyList<int> Faces)
{
    public bool IsNatural20 => Natural == 20;

    public bool IsNatural1 => Natural == 1;
}
