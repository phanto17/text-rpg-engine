using System.Globalization;

namespace RpgEngine.Tests.Balance;

/// <summary>Aggregated results of many seeded fights for one class, with or without the holy water flask.</summary>
internal sealed record BalanceRow(string ClassId, bool WithFlask, int MaxHp, int Fights, int Wins, int TotalRounds, int TotalHpLeftOnWins)
{
    public const string Header = "Class            Flask  Win rate  Avg rounds  Avg HP left (wins)";

    public double WinRate => (double)Wins / Fights;

    public double AverageRounds => (double)TotalRounds / Fights;

    public double AverageHpLeftOnWins => Wins == 0 ? 0 : (double)TotalHpLeftOnWins / Wins;

    public override string ToString() => string.Format(
        CultureInfo.InvariantCulture,
        "{0,-16} {1,-5} {2,8:P1} {3,11:F2} {4,12:F1} / {5}",
        ClassId,
        WithFlask ? "yes" : "no",
        WinRate,
        AverageRounds,
        AverageHpLeftOnWins,
        MaxHp);
}
