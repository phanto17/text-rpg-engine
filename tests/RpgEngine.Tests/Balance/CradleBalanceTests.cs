using RpgEngine.Core.Content;
using RpgEngine.Tests.Content;
using Xunit.Abstractions;

namespace RpgEngine.Tests.Balance;

/// <summary>
/// Guards the balance of Act III's final fight, the First Architect with a calcified warden. Each
/// Calling plays <see cref="FightsPerVariant"/> seeded fights with <see cref="BalancePolicy"/> at
/// level 3 and level 4, once with only its starting kit and no flask, and once also carrying the
/// three ally blessings a merciful journey earns at the Vigil of the Last Watch (Wren's censer,
/// Vaelen's chime, Silas's standard). The blessings must always help.
/// <para>
/// Level 4 is where nearly every player meets the Architect (Act II's bosses and Act III's
/// discoveries carry a hero past 900 XP), so its solo band is the fair one. Level 3 is reachable
/// only by a hero who fought every boss and found little on the way down, so its band only rules out
/// a fight that cannot be won at all. Measured win rates, solo then blessed (Oath-Eater, Liminal
/// Auditor, Vesper Cantor, Remnant Vessel):
/// </para>
/// <list type="bullet">
/// <item>Level 3: 46.7% / 85.5%, 23.0% / 69.6%, 52.0% / 63.4%, 15.5% / 38.8%.</item>
/// <item>Level 4: 78.9% / 98.0%, 43.0% / 83.4%, 80.6% / 87.0%, 82.0% / 82.9%. The Architect is
/// weak to force and radiant, so the Remnant Vessel's Reclaim the Ember and the Vesper Cantor's
/// Requiem hit him for double, and the blessings add little to a fight they already end quickly.</item>
/// </list>
/// When a tuning change is intended, run <c>dotnet test --filter Cradle --logger "console;verbosity=detailed"</c>,
/// read the table, and move the bands in the same commit.
/// </summary>
public sealed class CradleBalanceTests(ITestOutputHelper output)
{
    private const int FightsPerVariant = 2000;

    private static readonly string[] AllyBlessings = ["wrens_ashen_censer", "vaelens_silver_chime", "silas_iron_standard"];

    private static readonly IReadOnlyDictionary<int, (double Min, double Max)> SoloWinRateBandsByLevel =
        new Dictionary<int, (double Min, double Max)>
        {
            [3] = (0.05, 0.75),
            [4] = (0.40, 0.85),
        };

    [Fact]
    public void Simulate_EachCallingAtLevelsThreeAndFour_WinRateStaysInBand_AndTheBlessingsHelp()
    {
        var content = ContentLoader.LoadFromDirectory(RealDataTests.DataDirectory);
        var simulator = new BossFightSimulator(content, "cradle_architect_boss", "first_architect");

        var pairs = content.ClassList
            .SelectMany(cls => SoloWinRateBandsByLevel.Keys.Select(level =>
                (Solo: simulator.Run(cls.Id, level, FightsPerVariant), Blessed: simulator.Run(cls.Id, level, FightsPerVariant, AllyBlessings))))
            .ToList();

        output.WriteLine(BalanceRow.Header);
        pairs.ForEach(pair =>
        {
            output.WriteLine(pair.Solo.ToString());
            output.WriteLine(pair.Blessed.ToString());
        });

        Assert.Equal(content.ClassList.Count * SoloWinRateBandsByLevel.Count, pairs.Count);
        Assert.All(pairs, pair => Assert.InRange(pair.Solo.WinRate, SoloWinRateBandsByLevel[pair.Solo.Level].Min, SoloWinRateBandsByLevel[pair.Solo.Level].Max));
        Assert.All(pairs, pair => Assert.True(pair.Blessed.WinRate > pair.Solo.WinRate, $"{pair.Solo.ClassId} at level {pair.Solo.Level}: the blessings did not help."));
    }
}
