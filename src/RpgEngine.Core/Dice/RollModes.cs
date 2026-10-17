namespace RpgEngine.Core.Dice;

public static class RollModes
{
    /// <summary>Advantage and disadvantage cancel out, no matter how many sources of each apply.</summary>
    public static RollMode Combine(bool hasAdvantage, bool hasDisadvantage) => (hasAdvantage, hasDisadvantage) switch
    {
        (true, false) => RollMode.Advantage,
        (false, true) => RollMode.Disadvantage,
        _ => RollMode.Normal,
    };
}
