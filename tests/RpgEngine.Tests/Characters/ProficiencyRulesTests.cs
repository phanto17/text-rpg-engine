using RpgEngine.Core.Characters;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Characters;

public sealed class ProficiencyRulesTests
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(9, 3)]
    public void BonusForLevel_UsesTableAndClampsToLastEntry(int level, int expected)
    {
        Assert.Equal(expected, ProficiencyRules.BonusForLevel(level, TestCharacters.ProficiencyTable));
    }

    [Fact]
    public void BonusForLevel_LevelBelowOne_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProficiencyRules.BonusForLevel(0, TestCharacters.ProficiencyTable));
    }

    [Fact]
    public void BonusForLevel_EmptyTable_Throws()
    {
        Assert.Throws<ArgumentException>(() => ProficiencyRules.BonusForLevel(1, []));
    }

    [Fact]
    public void BonusForLevel_NullTable_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ProficiencyRules.BonusForLevel(1, null!));
    }

    [Theory]
    [InlineData(ProficiencyLevel.None, 0)]
    [InlineData(ProficiencyLevel.Proficient, 2)]
    [InlineData(ProficiencyLevel.Expertise, 4)]
    public void Bonus_ScalesWithProficiencyLevel(ProficiencyLevel level, int expected)
    {
        Assert.Equal(expected, ProficiencyRules.Bonus(level, 2));
    }
}
