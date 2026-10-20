using RpgEngine.Core.Content;
using RpgEngine.Tests.Content;
using Xunit.Abstractions;

namespace RpgEngine.Tests.Balance;

/// <summary>
/// Guards the balance of Act II's two boss fights, Prelate Vaelen (with a tethered chorister) and
/// Commander Silas (with a scoured halberdier). Each Calling plays <see cref="FightsPerVariant"/>
/// seeded fights with <see cref="BalancePolicy"/> at level 2 and level 3, carrying only its starting
/// kit and no flask, so the numbers are a floor: real players arrive with Act I's gear and the
/// memory-echo advantages that make a parley likely. A boss must be neither unwinnable nor trivial
/// for any Calling at either level.
/// <para>
/// Level 3 is where nearly every player meets these bosses (Act I alone is worth well over 300 XP),
/// so its band is the fair one. Level 2 is reachable only by skipping most of Act I, so its band only
/// rules out a fight that cannot be won at all. Measured no-flask win rates (Oath-Eater, Liminal
/// Auditor, Vesper Cantor, Remnant Vessel):
/// </para>
/// <list type="bullet">
/// <item>Vaelen, level 2: 57.5%, 29.2%, 66.8%, 35.7%. Level 3: 71.8%, 55.1%, 81.6%, 54.9%.</item>
/// <item>Silas, level 2: 52.0%, 26.0%, 29.5%, 6.7%. Level 3: 76.5%, 46.4%, 48.4%, 62.6%. The level-2
/// Remnant Vessel has nothing that exploits his weaknesses until Death-Knell Echo (lightning) arrives
/// at level 3; its class parley is the intended way through.</item>
/// </list>
/// When a tuning change is intended, run <c>dotnet test --filter MemoryWarrens --logger "console;verbosity=detailed"</c>,
/// read the tables, and move the bands in the same commit.
/// </summary>
public sealed class MemoryWarrensBalanceTests(ITestOutputHelper output)
{
    private const int FightsPerVariant = 2000;

    private static readonly IReadOnlyDictionary<int, (double Min, double Max)> WinRateBandsByLevel =
        new Dictionary<int, (double Min, double Max)>
        {
            [2] = (0.05, 0.75),
            [3] = (0.40, 0.85),
        };

    [Theory]
    [InlineData("archive_vaelen_boss", "prelate_vaelen")]
    [InlineData("reliquary_silas_boss", "commander_silas")]
    public void Simulate_EachCallingAtLevelsTwoAndThree_WinRateStaysInBand(string encounterId, string bossId)
    {
        var content = ContentLoader.LoadFromDirectory(RealDataTests.DataDirectory);
        var simulator = new BossFightSimulator(content, encounterId, bossId);

        var rows = content.ClassList
            .SelectMany(cls => WinRateBandsByLevel.Keys.Select(level => simulator.Run(cls.Id, level, FightsPerVariant)))
            .ToList();

        output.WriteLine(encounterId);
        output.WriteLine(BalanceRow.Header);
        rows.ForEach(row => output.WriteLine(row.ToString()));

        Assert.Equal(content.ClassList.Count * WinRateBandsByLevel.Count, rows.Count);
        Assert.All(rows, row => Assert.InRange(row.WinRate, WinRateBandsByLevel[row.Level].Min, WinRateBandsByLevel[row.Level].Max));
    }
}
