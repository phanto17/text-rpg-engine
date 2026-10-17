using RpgEngine.Core.Commands;

namespace RpgEngine.Tests.Commands;

public sealed class TargetMatcherTests
{
    [Theory]
    [InlineData("bronze door", 3)]
    [InlineData("the Bronze Door", 3)]
    [InlineData("door", 3)]
    [InlineData("bronze", 2)]
    [InlineData("old bronze door", 2)]
    [InlineData("bro", 1)]
    [InlineData("br", 0)]
    [InlineData("window", 0)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    public void Score_RanksExactSubsetAndPrefix(string? query, int expected)
    {
        Assert.Equal(expected, TargetMatcher.Score(query, ["bronze door", "door", ""]));
    }

    [Fact]
    public void Best_ReturnsSingleTopMatch()
    {
        var best = TargetMatcher.Best("door", ["bronze door", "door", "window"], name => [name]);

        Assert.Equal("door", Assert.Single(best));
    }

    [Fact]
    public void Best_ReturnsAllTiedCandidates()
    {
        var best = TargetMatcher.Best("door", ["left door", "right door", "window"], name => [name]);

        Assert.Equal(new[] { "left door", "right door" }, best);
    }

    [Fact]
    public void Best_NoMatch_ReturnsEmpty()
    {
        Assert.Empty(TargetMatcher.Best("cat", ["left door"], name => [name]));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => TargetMatcher.Score("x", null!));
        Assert.Throws<ArgumentNullException>(() => TargetMatcher.Best<string>("x", null!, name => [name]));
        Assert.Throws<ArgumentNullException>(() => TargetMatcher.Best("x", ["a"], null!));
    }

    [Fact]
    public void Words_StripsPunctuationAndArticles()
    {
        Assert.Equal(new[] { "press", "sun", "sigil" }, TextNormalizer.Words("Press the SUN-sigil!"));
        Assert.Empty(TextNormalizer.Words(null));
    }
}
