using RpgEngine.Core.Content;
using RpgEngine.Tests.Content;
using Xunit.Abstractions;

namespace RpgEngine.Tests.Balance;

/// <summary>
/// Guards the balance of Barrowlight's boss fight against data tuning: each class plays
/// <see cref="FightsPerVariant"/> seeded fights with <see cref="BalancePolicy"/>, with and without the
/// holy water flask. The bands bracket the measured no-flask win rates (Oath-Eater 78.6%, Liminal
/// Auditor 58.0%, Vesper Cantor 56.4%, Remnant Vessel 59.4%), so a change that makes a class
/// unwinnable or trivial fails here. When a tuning change is intended, read the table in the test
/// output and move the band.
/// </summary>
public sealed class BarrowHeartBalanceTests(ITestOutputHelper output)
{
    private const int FightsPerVariant = 2000;

    private static readonly IReadOnlyDictionary<string, (double Min, double Max)> NoFlaskWinRateBands =
        new Dictionary<string, (double Min, double Max)>(StringComparer.Ordinal)
        {
            ["oath_eater"] = (0.65, 0.90),
            ["liminal_auditor"] = (0.45, 0.70),
            ["vesper_cantor"] = (0.45, 0.70),
            ["remnant_vessel"] = (0.45, 0.70),
        };

    [Fact]
    public void Simulate_EachClassWithoutFlask_WinRateStaysInBand()
    {
        var content = ContentLoader.LoadFromDirectory(RealDataTests.DataDirectory);
        var simulator = new BossFightSimulator(content, "barrow_heart", "gravebound_knight");

        var rows = content.ClassList
            .SelectMany(cls => new[] { false, true }.Select(withFlask => simulator.Run(cls.Id, level: 1, withFlask, FightsPerVariant)))
            .ToList();

        output.WriteLine(BalanceRow.Header);
        rows.ForEach(row => output.WriteLine(row.ToString()));

        var noFlask = rows.Where(row => !row.WithFlask).ToList();
        Assert.Equal(NoFlaskWinRateBands.Keys.Order(StringComparer.Ordinal), noFlask.Select(row => row.ClassId).Order(StringComparer.Ordinal));
        Assert.All(noFlask, row => Assert.InRange(row.WinRate, NoFlaskWinRateBands[row.ClassId].Min, NoFlaskWinRateBands[row.ClassId].Max));
    }
}
