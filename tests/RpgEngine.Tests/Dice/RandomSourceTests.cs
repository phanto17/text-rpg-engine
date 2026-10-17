using RpgEngine.Core.Dice;

namespace RpgEngine.Tests.Dice;

public sealed class RandomSourceTests
{
    [Fact]
    public void Pcg32_Seed42_MatchesReferenceImplementation()
    {
        // First outputs of pcg32_srandom(42, 54) from the PCG reference demo.
        var random = new Pcg32RandomSource(42);

        uint[] expected = [0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e];

        Assert.Equal(expected, Enumerable.Range(0, expected.Length).Select(_ => random.NextUInt32()));
    }

    [Fact]
    public void Pcg32_SameSeed_ProducesSameSequence()
    {
        var first = new Pcg32RandomSource(2024);
        var second = new Pcg32RandomSource(2024);

        Assert.Equal(
            Enumerable.Range(0, 50).Select(_ => first.Next(1, 21)),
            Enumerable.Range(0, 50).Select(_ => second.Next(1, 21)));
    }

    [Fact]
    public void Pcg32_DifferentSeeds_ProduceDifferentSequences()
    {
        var first = new Pcg32RandomSource(1);
        var second = new Pcg32RandomSource(2);

        Assert.NotEqual(
            Enumerable.Range(0, 20).Select(_ => first.Next(1, 21)),
            Enumerable.Range(0, 20).Select(_ => second.Next(1, 21)));
    }

    [Fact]
    public void Pcg32_Next_StaysWithinBoundsAndHitsEveryValue()
    {
        var random = new Pcg32RandomSource(7);

        var values = Enumerable.Range(0, 10_000).Select(_ => random.Next(1, 7)).ToList();

        Assert.All(values, value => Assert.InRange(value, 1, 6));
        Assert.Equal(6, values.Distinct().Count());
    }

    [Fact]
    public void Pcg32_Next_HandlesFullIntRange()
    {
        var random = new Pcg32RandomSource(99);

        var values = Enumerable.Range(0, 1_000).Select(_ => random.Next(int.MinValue, int.MaxValue)).ToList();

        Assert.All(values, value => Assert.InRange(value, int.MinValue, int.MaxValue - 1));
        Assert.Contains(values, value => value < 0);
        Assert.Contains(values, value => value > 0);
    }

    [Fact]
    public void Pcg32_Next_RangeWithHeavyRejection_StaysUnbiasedAndInBounds()
    {
        // A range of 2^31 + 1 rejects roughly half of all raw outputs, exercising the retry loop.
        var random = new Pcg32RandomSource(5);

        var values = Enumerable.Range(0, 200).Select(_ => random.Next(int.MinValue, 1)).ToList();

        Assert.All(values, value => Assert.InRange(value, int.MinValue, 0));
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(5, 4)]
    public void Pcg32_Next_EmptyRange_Throws(int min, int max)
    {
        var random = new Pcg32RandomSource(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => random.Next(min, max));
    }

    [Fact]
    public void Fixed_ReturnsScriptedValuesInOrder()
    {
        var random = new FixedRandomSource(3, 1, 4);

        Assert.Equal(3, random.Remaining);
        Assert.Equal(new[] { 3, 1, 4 }, new[] { random.Next(1, 7), random.Next(1, 7), random.Next(1, 7) });
        Assert.Equal(0, random.Remaining);
    }

    [Fact]
    public void Fixed_RunsOut_Throws()
    {
        var random = new FixedRandomSource(1);
        random.Next(1, 7);

        var exception = Assert.Throws<InvalidOperationException>(() => random.Next(1, 7));
        Assert.Contains("ran out", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Fixed_ValueOutsideRange_Throws(int value)
    {
        var random = new FixedRandomSource(value);

        var exception = Assert.Throws<InvalidOperationException>(() => random.Next(1, 7));
        Assert.Contains("outside", exception.Message);
    }

    [Fact]
    public void Fixed_NullSequence_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FixedRandomSource((IEnumerable<int>)null!));
    }
}
