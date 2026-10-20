using System.Globalization;

namespace RpgEngine.Tests.Balance;

/// <summary>Aggregated results of many seeded fights for one class at one level, carrying the given items beyond the starting kit.</summary>
internal sealed record BalanceRow(string ClassId, int Level, IReadOnlyList<string> ExtraItems, int MaxHp, int Fights, int Wins, int TotalRounds, int TotalHpLeftOnWins)
{
    public const string Header = "Class            Level  Win rate  Avg rounds  Avg HP left (wins)  Extra items";

    public double WinRate => (double)Wins / Fights;

    public double AverageRounds => (double)TotalRounds / Fights;

    public double AverageHpLeftOnWins => Wins == 0 ? 0 : (double)TotalHpLeftOnWins / Wins;

    public override string ToString() => string.Format(
        CultureInfo.InvariantCulture,
        "{0,-16} {1,5} {2,9:P1} {3,11:F2} {4,12:F1} / {5,-3}  {6}",
        ClassId,
        Level,
        WinRate,
        AverageRounds,
        AverageHpLeftOnWins,
        MaxHp,
        ExtraItems.Count == 0 ? "-" : string.Join(", ", ExtraItems));
}
