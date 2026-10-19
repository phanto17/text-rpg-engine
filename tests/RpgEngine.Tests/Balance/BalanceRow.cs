using System.Globalization;

namespace RpgEngine.Tests.Balance;

/// <summary>Aggregated results of many seeded fights for one class at one level, with or without the holy water flask.</summary>
internal sealed record BalanceRow(string ClassId, int Level, bool WithFlask, int MaxHp, int Fights, int Wins, int TotalRounds, int TotalHpLeftOnWins)
{
    public const string Header = "Class            Level  Flask  Win rate  Avg rounds  Avg HP left (wins)";

    public double WinRate => (double)Wins / Fights;

    public double AverageRounds => (double)TotalRounds / Fights;

    public double AverageHpLeftOnWins => Wins == 0 ? 0 : (double)TotalHpLeftOnWins / Wins;

    public override string ToString() => string.Format(
        CultureInfo.InvariantCulture,
        "{0,-16} {1,5}  {2,-5} {3,8:P1} {4,11:F2} {5,12:F1} / {6}",
        ClassId,
        Level,
        WithFlask ? "yes" : "no",
        WinRate,
        AverageRounds,
        AverageHpLeftOnWins,
        MaxHp);
}
