using RpgEngine.Core.Dice;

namespace RpgEngine.Tests.Dice;

public sealed class DiceExpressionTests
{
    [Theory]
    [InlineData("d20", "1d20", 1, 20)]
    [InlineData("1d20", "1d20", 1, 20)]
    [InlineData("2d6+3", "2d6+3", 5, 15)]
    [InlineData(" 2D6 + 3 ", "2d6+3", 5, 15)]
    [InlineData("3d8-1", "3d8-1", 2, 23)]
    [InlineData("4d6kh3", "4d6kh3", 3, 18)]
    [InlineData("2d20kl1", "2d20kl1", 1, 20)]
    [InlineData("1d8+1d6+2", "1d8+1d6+2", 4, 16)]
    [InlineData("19", "19", 19, 19)]
    [InlineData("+3", "3", 3, 3)]
    [InlineData("-2", "-2", -2, -2)]
    [InlineData("10-1d4", "10-1d4", 6, 9)]
    [InlineData("0", "0", 0, 0)]
    public void Parse_ValidExpression_NormalizesTextAndBounds(string input, string expectedText, int expectedMin, int expectedMax)
    {
        var expression = DiceExpression.Parse(input);

        Assert.Equal(expectedText, expression.Text);
        Assert.Equal(expectedText, expression.ToString());
        Assert.Equal(expectedMin, expression.Min);
        Assert.Equal(expectedMax, expression.Max);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("0d6")]
    [InlineData("d0")]
    [InlineData("d1")]
    [InlineData("1d1001")]
    [InlineData("2d")]
    [InlineData("101d6")]
    [InlineData("99999999999d6")]
    [InlineData("1d99999999999")]
    [InlineData("4d6kh5")]
    [InlineData("4d6kh0")]
    [InlineData("4d6kl99999999999")]
    [InlineData("4d6k3")]
    [InlineData("2d6+")]
    [InlineData("-")]
    [InlineData("2d6 3")]
    [InlineData("2d6d4")]
    [InlineData("10001")]
    [InlineData("99999999999")]
    public void TryParse_InvalidExpression_ReturnsFalseWithError(string? input)
    {
        var parsed = DiceExpression.TryParse(input, out var expression, out var error);

        Assert.False(parsed);
        Assert.Null(expression);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Parse_InvalidExpression_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() => DiceExpression.Parse("banana"));

        Assert.Contains("banana", exception.Message);
    }

    [Fact]
    public void TryParse_MoreThanMaxTerms_ReturnsFalse()
    {
        var input = string.Join('+', Enumerable.Repeat("1", DiceExpression.MaxTerms + 1));

        Assert.False(DiceExpression.TryParse(input, out _, out var error));
        Assert.Contains("terms", error);
    }

    [Fact]
    public void Parse_TwentyTerms_IsAllowed()
    {
        var expression = DiceExpression.Parse(string.Join('+', Enumerable.Repeat("1", DiceExpression.MaxTerms)));

        Assert.Equal(DiceExpression.MaxTerms, expression.Terms.Count);
        Assert.Equal(DiceExpression.MaxTerms, expression.Max);
    }

    [Theory]
    [InlineData("d20", 10.5)]
    [InlineData("2d6+3", 10.0)]
    [InlineData("3d8-1", 12.5)]
    [InlineData("19", 19.0)]
    [InlineData("10-1d4", 7.5)]
    [InlineData("4d6kh4", 14.0)]
    [InlineData("2d20kh1", 13.825)]
    [InlineData("2d20kl1", 7.175)]
    [InlineData("4d6kh3", 12.2446)]
    [InlineData("3d6kl1", 2.0417)]
    public void Average_MatchesExactExpectedValue(string input, double expected)
    {
        var expression = DiceExpression.Parse(input);

        Assert.Equal(expected, expression.Average, precision: 4);
    }

    [Fact]
    public void Average_IsCachedAfterFirstRead()
    {
        var expression = DiceExpression.Parse("4d6kh3");

        var first = expression.Average;
        var second = expression.Average;

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("1d8+2", "2d8+2")]
    [InlineData("4d6kh3", "8d6kh6")]
    [InlineData("19", "19")]
    [InlineData("10-1d4", "10-2d4")]
    public void Doubled_DoublesDiceButNotConstants(string input, string expected)
    {
        var doubled = DiceExpression.Parse(input).Doubled();

        Assert.Equal(expected, doubled.Text);
    }

    [Fact]
    public void Plus_Expression_ConcatenatesTerms()
    {
        var combined = DiceExpression.Parse("1d6+2").Plus(DiceExpression.Parse("1d6"));

        Assert.Equal("1d6+2+1d6", combined.Text);
        Assert.Equal(4, combined.Min);
        Assert.Equal(14, combined.Max);
    }

    [Fact]
    public void Plus_NullExpression_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DiceExpression.Parse("1d6").Plus(null!));
    }

    [Theory]
    [InlineData(3, "1d8+3")]
    [InlineData(-1, "1d8-1")]
    public void Plus_Modifier_AppendsSignedConstant(int modifier, string expected)
    {
        Assert.Equal(expected, DiceExpression.Parse("1d8").Plus(modifier).Text);
    }

    [Fact]
    public void Plus_ZeroModifier_ReturnsSameInstance()
    {
        var expression = DiceExpression.Parse("1d8");

        Assert.Same(expression, expression.Plus(0));
    }

    [Theory]
    [InlineData("2d6+3", true)]
    [InlineData("7", false)]
    public void HasDice_ReflectsDiceTerms(string input, bool expected)
    {
        Assert.Equal(expected, DiceExpression.Parse(input).HasDice);
    }

    [Fact]
    public void Terms_ExposeKeepRules()
    {
        var expression = DiceExpression.Parse("4d6kh3-2d4kl1+5");

        var highest = expression.Terms[0];
        var lowest = expression.Terms[1];
        var constant = expression.Terms[2];

        Assert.Equal((1, 4, 6, KeepRule.Highest, 3), (highest.Sign, highest.Count, highest.Sides, highest.Keep, highest.KeepCount));
        Assert.Equal((-1, 2, 4, KeepRule.Lowest, 1), (lowest.Sign, lowest.Count, lowest.Sides, lowest.Keep, lowest.KeepCount));
        Assert.True(constant.IsConstant);
        Assert.Equal(5, constant.Constant);
    }
}
