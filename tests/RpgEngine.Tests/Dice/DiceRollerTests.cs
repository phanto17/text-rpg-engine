using RpgEngine.Core.Dice;

namespace RpgEngine.Tests.Dice;

public sealed class DiceRollerTests
{
    [Fact]
    public void Constructor_NullRandom_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DiceRoller(null!));
    }

    [Fact]
    public void Roll_DiceAndModifier_SumsKeptDicePlusModifier()
    {
        var roller = new DiceRoller(new FixedRandomSource(4, 2));

        var result = roller.Roll("2d6+3");

        Assert.Equal(9, result.Total);
        Assert.Equal(3, result.Modifier);
        Assert.Equal(new[] { 4, 2 }, result.Dice.Select(d => d.Value));
        Assert.All(result.Dice, die => Assert.True(die.Kept));
        Assert.All(result.Dice, die => Assert.Equal(6, die.Sides));
        Assert.False(result.Critical);
        Assert.Equal("2d6+3", result.Expression.Text);
    }

    [Fact]
    public void Roll_KeepHighest_DropsLowestDie()
    {
        var roller = new DiceRoller(new FixedRandomSource(1, 5, 3, 6));

        var result = roller.Roll("4d6kh3");

        Assert.Equal(14, result.Total);
        Assert.Equal(new[] { false, true, true, true }, result.Dice.Select(d => d.Kept));
    }

    [Fact]
    public void Roll_KeepHighestWithTies_DropsLaterDiceFirst()
    {
        var roller = new DiceRoller(new FixedRandomSource(2, 2, 2, 2));

        var result = roller.Roll("4d6kh3");

        Assert.Equal(6, result.Total);
        Assert.Equal(new[] { true, true, true, false }, result.Dice.Select(d => d.Kept));
    }

    [Fact]
    public void Roll_KeepLowest_KeepsSmallestDie()
    {
        var roller = new DiceRoller(new FixedRandomSource(15, 4));

        var result = roller.Roll("2d20kl1");

        Assert.Equal(4, result.Total);
        Assert.Equal(new[] { false, true }, result.Dice.Select(d => d.Kept));
    }

    [Fact]
    public void Roll_Critical_DoublesDiceButNotModifier()
    {
        var roller = new DiceRoller(new FixedRandomSource(3, 5));

        var result = roller.Roll("1d8+2", critical: true);

        Assert.Equal(10, result.Total);
        Assert.Equal(2, result.Dice.Count);
        Assert.Equal(2, result.Modifier);
        Assert.True(result.Critical);
    }

    [Fact]
    public void Roll_NegativeDiceTerm_SubtractsDie()
    {
        var roller = new DiceRoller(new FixedRandomSource(3));

        var result = roller.Roll("10-1d4");

        Assert.Equal(7, result.Total);
        Assert.Equal(-1, Assert.Single(result.Dice).Sign);
    }

    [Fact]
    public void Roll_ConstantOnly_RollsNoDice()
    {
        var roller = new DiceRoller(new FixedRandomSource());

        var result = roller.Roll("19");

        Assert.Empty(result.Dice);
        Assert.Equal(19, result.Total);
    }

    [Fact]
    public void Roll_NullExpression_Throws()
    {
        var roller = new DiceRoller(new FixedRandomSource());

        Assert.Throws<ArgumentNullException>(() => roller.Roll((DiceExpression)null!));
    }

    [Fact]
    public void Roll_InvalidText_ThrowsFormatException()
    {
        var roller = new DiceRoller(new FixedRandomSource());

        Assert.Throws<FormatException>(() => roller.Roll("not dice"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-6)]
    public void RollDie_LessThanOneSide_Throws(int sides)
    {
        var roller = new DiceRoller(new FixedRandomSource(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => roller.RollDie(sides));
    }

    [Fact]
    public void Pick_ReturnsValueFromSource()
    {
        var roller = new DiceRoller(new FixedRandomSource(7));

        Assert.Equal(7, roller.Pick(0, 10));
    }

    [Fact]
    public void RollD20_Normal_UsesSingleDie()
    {
        var roller = new DiceRoller(new FixedRandomSource(13));

        var roll = roller.RollD20();

        Assert.Equal(RollMode.Normal, roll.Mode);
        Assert.Equal(13, roll.Natural);
        Assert.Equal(new[] { 13 }, roll.Faces);
    }

    [Fact]
    public void RollD20_WithAdvantage_KeepsHigherDie()
    {
        var roller = new DiceRoller(new FixedRandomSource(5, 17));

        var roll = roller.RollD20(RollMode.Advantage);

        Assert.Equal(17, roll.Natural);
        Assert.Equal(new[] { 5, 17 }, roll.Faces);
    }

    [Fact]
    public void RollD20_WithDisadvantage_KeepsLowerDie()
    {
        var roller = new DiceRoller(new FixedRandomSource(5, 17));

        var roll = roller.RollD20(RollMode.Disadvantage);

        Assert.Equal(5, roll.Natural);
    }

    [Theory]
    [InlineData(20, true, false)]
    [InlineData(1, false, true)]
    [InlineData(10, false, false)]
    public void RollD20_FlagsNaturalResults(int face, bool isNatural20, bool isNatural1)
    {
        var roll = new DiceRoller(new FixedRandomSource(face)).RollD20();

        Assert.Equal(isNatural20, roll.IsNatural20);
        Assert.Equal(isNatural1, roll.IsNatural1);
    }

    [Theory]
    [InlineData(false, false, RollMode.Normal)]
    [InlineData(true, false, RollMode.Advantage)]
    [InlineData(false, true, RollMode.Disadvantage)]
    [InlineData(true, true, RollMode.Normal)]
    public void Combine_AdvantageAndDisadvantage_CancelOut(bool advantage, bool disadvantage, RollMode expected)
    {
        Assert.Equal(expected, RollModes.Combine(advantage, disadvantage));
    }
}
