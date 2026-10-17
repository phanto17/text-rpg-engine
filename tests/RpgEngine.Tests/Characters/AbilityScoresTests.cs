using RpgEngine.Core.Characters;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Characters;

public sealed class AbilityScoresTests
{
    [Theory]
    [InlineData(1, -5)]
    [InlineData(2, -4)]
    [InlineData(3, -4)]
    [InlineData(7, -2)]
    [InlineData(8, -1)]
    [InlineData(9, -1)]
    [InlineData(10, 0)]
    [InlineData(11, 0)]
    [InlineData(12, 1)]
    [InlineData(13, 1)]
    [InlineData(15, 2)]
    [InlineData(18, 4)]
    [InlineData(20, 5)]
    [InlineData(29, 9)]
    [InlineData(30, 10)]
    public void ModifierFor_Score_FollowsFloorFormula(int score, int expected)
    {
        Assert.Equal(expected, AbilityScores.ModifierFor(score));
    }

    [Fact]
    public void ModifierFor_EveryScore_MatchesFloorOfHalfDifference()
    {
        for (var score = AbilityScores.MinScore; score <= AbilityScores.MaxScore; score++)
        {
            Assert.Equal((int)Math.Floor((score - 10) / 2.0), AbilityScores.ModifierFor(score));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void ModifierFor_OutOfRange_Throws(int score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AbilityScores.ModifierFor(score));
    }

    [Theory]
    [InlineData(0, 10, 10, 10, 10, 10)]
    [InlineData(10, 31, 10, 10, 10, 10)]
    [InlineData(10, 10, 0, 10, 10, 10)]
    [InlineData(10, 10, 10, -1, 10, 10)]
    [InlineData(10, 10, 10, 10, 99, 10)]
    [InlineData(10, 10, 10, 10, 10, 0)]
    public void Constructor_AnyScoreOutOfRange_Throws(int str, int dex, int con, int @int, int wis, int cha)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AbilityScores(str, dex, con, @int, wis, cha));
    }

    [Fact]
    public void Indexer_ReturnsEachScore()
    {
        var scores = TestCharacters.Scores(str: 15, dex: 14, con: 13, @int: 12, wis: 10, cha: 8);

        Assert.Equal(15, scores[Ability.Str]);
        Assert.Equal(14, scores[Ability.Dex]);
        Assert.Equal(13, scores[Ability.Con]);
        Assert.Equal(12, scores[Ability.Int]);
        Assert.Equal(10, scores[Ability.Wis]);
        Assert.Equal(8, scores[Ability.Cha]);
        Assert.Equal(2, scores.Modifier(Ability.Str));
        Assert.Equal(-1, scores.Modifier(Ability.Cha));
    }

    [Fact]
    public void Indexer_UnknownAbility_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TestCharacters.Scores()[(Ability)99]);
    }

    [Fact]
    public void With_ReplacesOnlyOneScore()
    {
        var original = TestCharacters.Scores();

        foreach (var ability in AbilityExtensions.All)
        {
            var changed = original.With(ability, 17);

            Assert.Equal(17, changed[ability]);
            Assert.All(AbilityExtensions.All.Where(other => other != ability), other => Assert.Equal(10, changed[other]));
        }
    }

    [Fact]
    public void FromMap_AllScores_BuildsScores()
    {
        var map = AbilityExtensions.All.Select((ability, i) => (ability, score: 10 + i)).ToDictionary(x => x.ability, x => x.score);

        var scores = AbilityScores.FromMap(map);

        Assert.Equal(TestCharacters.Scores(10, 11, 12, 13, 14, 15), scores);
    }

    [Fact]
    public void FromMap_MissingScore_Throws()
    {
        var map = new Dictionary<Ability, int> { [Ability.Str] = 10 };

        var exception = Assert.Throws<ArgumentException>(() => AbilityScores.FromMap(map));
        Assert.Contains("DEX", exception.Message);
    }

    [Fact]
    public void FromMap_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AbilityScores.FromMap(null!));
    }

    [Theory]
    [InlineData(Ability.Str, "Strength", "STR")]
    [InlineData(Ability.Dex, "Dexterity", "DEX")]
    [InlineData(Ability.Con, "Constitution", "CON")]
    [InlineData(Ability.Int, "Intelligence", "INT")]
    [InlineData(Ability.Wis, "Wisdom", "WIS")]
    [InlineData(Ability.Cha, "Charisma", "CHA")]
    public void Names_AreStandard(Ability ability, string displayName, string abbreviation)
    {
        Assert.Equal(displayName, ability.DisplayName());
        Assert.Equal(abbreviation, ability.Abbreviation());
    }

    [Fact]
    public void Names_UnknownAbility_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((Ability)99).DisplayName());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((Ability)99).Abbreviation());
    }
}
