using RpgEngine.Core.Dice;

namespace RpgEngine.Tests.Dice;

/// <summary>
/// Statistical fairness checks. Each test uses a fixed seed, so the result is deterministic and
/// can never flake; the thresholds are the p = 0.001 chi-square critical values.
/// </summary>
public sealed class DiceProbabilityTests
{
    [Fact]
    public void D20_IsUniform_ChiSquare()
    {
        var roller = new DiceRoller(new Pcg32RandomSource(12345));
        const int rolls = 120_000;
        var counts = new int[21];

        for (var i = 0; i < rolls; i++)
        {
            counts[roller.RollDie(20)]++;
        }

        var expected = Enumerable.Repeat(rolls / 20.0, 20).ToArray();
        var chiSquare = ChiSquare(counts[1..], expected);

        Assert.True(chiSquare < 43.82, $"chi-square {chiSquare:F2} exceeds the df=19 critical value");
    }

    [Fact]
    public void TwoD6_MatchesExactTriangularDistribution_ChiSquare()
    {
        var roller = new DiceRoller(new Pcg32RandomSource(777));
        var expression = DiceExpression.Parse("2d6");
        const int rolls = 72_000;
        var counts = new int[13];

        for (var i = 0; i < rolls; i++)
        {
            counts[roller.Roll(expression).Total]++;
        }

        // Ways to make 2..12 with two six-sided dice: 1,2,3,4,5,6,5,4,3,2,1 out of 36.
        int[] ways = [1, 2, 3, 4, 5, 6, 5, 4, 3, 2, 1];
        var expected = ways.Select(w => rolls * w / 36.0).ToArray();
        var chiSquare = ChiSquare(counts[2..], expected);

        Assert.True(chiSquare < 29.59, $"chi-square {chiSquare:F2} exceeds the df=10 critical value");
    }

    [Theory]
    [InlineData(RollMode.Advantage, 13.825)]
    [InlineData(RollMode.Disadvantage, 7.175)]
    [InlineData(RollMode.Normal, 10.5)]
    public void D20_MeanMatchesTheory(RollMode mode, double expectedMean)
    {
        var roller = new DiceRoller(new Pcg32RandomSource(31337));
        const int rolls = 100_000;

        var mean = Enumerable.Range(0, rolls).Average(_ => roller.RollD20(mode).Natural);

        Assert.InRange(mean, expectedMean - 0.1, expectedMean + 0.1);
    }

    [Fact]
    public void FourD6DropLowest_MeanMatchesExactAverage()
    {
        var roller = new DiceRoller(new Pcg32RandomSource(4));
        var expression = DiceExpression.Parse("4d6kh3");
        const int rolls = 50_000;

        var mean = Enumerable.Range(0, rolls).Average(_ => roller.Roll(expression).Total);

        Assert.InRange(mean, expression.Average - 0.1, expression.Average + 0.1);
    }

    private static double ChiSquare(IReadOnlyList<int> observed, IReadOnlyList<double> expected) =>
        observed.Select((count, i) => Math.Pow(count - expected[i], 2) / expected[i]).Sum();
}
