using RpgEngine.Core.Characters;
using RpgEngine.Core.Checks;
using RpgEngine.Core.Dice;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Checks;

public sealed class CheckResolverTests
{
    // CHA 14 (+2), proficiency bonus +2 at level 1.
    private static readonly PlayerCharacter Bard = TestCharacters.Create(
        abilities: TestCharacters.Scores(cha: 14, dex: 16),
        skills: ["persuasion", "stealth"],
        expertise: ["stealth"],
        saves: [Ability.Dex]);

    private static CheckResolver Resolver(bool naturalCrits = true, params int[] faces) =>
        new(new DiceRoller(new FixedRandomSource(faces)), naturalCrits);

    [Fact]
    public void Constructor_NullDice_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CheckResolver(null!, true));
    }

    [Fact]
    public void SkillCheck_Proficient_AddsModifierAndProficiency()
    {
        var result = Resolver(true, 10).SkillCheck(Bard, "persuasion", "Persuasion", Ability.Cha, 14);

        Assert.Equal(CheckKind.Skill, result.Kind);
        Assert.Equal("Persuasion", result.Label);
        Assert.Equal(Ability.Cha, result.Ability);
        Assert.Equal(10, result.Roll.Natural);
        Assert.Equal(2, result.AbilityModifier);
        Assert.Equal(2, result.ProficiencyBonus);
        Assert.Equal(14, result.Total);
        Assert.Equal(14, result.Dc);
        Assert.True(result.Success);
    }

    [Fact]
    public void SkillCheck_MeetingDc_Succeeds_OneBelowFails()
    {
        var meets = Resolver(true, 10).SkillCheck(Bard, "persuasion", "Persuasion", Ability.Cha, 14);
        var oneBelow = Resolver(true, 9).SkillCheck(Bard, "persuasion", "Persuasion", Ability.Cha, 14);

        Assert.True(meets.Success);
        Assert.Equal(13, oneBelow.Total);
        Assert.False(oneBelow.Success);
    }

    [Fact]
    public void SkillCheck_Expertise_DoublesProficiency()
    {
        var result = Resolver(true, 10).SkillCheck(Bard, "stealth", "Stealth", Ability.Dex, 15);

        Assert.Equal(4, result.ProficiencyBonus);
        Assert.Equal(17, result.Total);
    }

    [Fact]
    public void SkillCheck_NotProficient_AddsOnlyModifier()
    {
        var result = Resolver(true, 10).SkillCheck(Bard, "athletics", "Athletics", Ability.Str, 10);

        Assert.Equal(0, result.ProficiencyBonus);
        Assert.Equal(10, result.Total);
        Assert.True(result.Success);
    }

    [Fact]
    public void Natural20_WithCritRule_SucceedsEvenBelowDc()
    {
        var result = Resolver(true, 20).SkillCheck(Bard, "athletics", "Athletics", Ability.Str, 30);

        Assert.True(result.Success);
        Assert.True(result.IsCriticalSuccess);
        Assert.False(result.IsCriticalFailure);
    }

    [Fact]
    public void Natural20_WithoutCritRule_UsesTotal()
    {
        var result = Resolver(false, 20).SkillCheck(Bard, "athletics", "Athletics", Ability.Str, 30);

        Assert.False(result.Success);
        Assert.False(result.IsCriticalSuccess);
    }

    [Fact]
    public void Natural1_WithCritRule_FailsEvenAboveDc()
    {
        var result = Resolver(true, 1).SkillCheck(Bard, "stealth", "Stealth", Ability.Dex, 2);

        Assert.Equal(8, result.Total);
        Assert.False(result.Success);
        Assert.True(result.IsCriticalFailure);
    }

    [Fact]
    public void Natural1_WithoutCritRule_UsesTotal()
    {
        var result = Resolver(false, 1).SkillCheck(Bard, "stealth", "Stealth", Ability.Dex, 2);

        Assert.True(result.Success);
        Assert.False(result.IsCriticalFailure);
    }

    [Fact]
    public void SkillCheck_WithAdvantage_UsesHigherDie()
    {
        var result = Resolver(true, 3, 15).SkillCheck(Bard, "persuasion", "Persuasion", Ability.Cha, 14, RollMode.Advantage);

        Assert.Equal(RollMode.Advantage, result.Roll.Mode);
        Assert.Equal(15, result.Roll.Natural);
        Assert.Equal(19, result.Total);
    }

    [Fact]
    public void SkillCheck_WithDisadvantage_UsesLowerDie()
    {
        var result = Resolver(true, 3, 15).SkillCheck(Bard, "persuasion", "Persuasion", Ability.Cha, 14, RollMode.Disadvantage);

        Assert.Equal(3, result.Roll.Natural);
        Assert.False(result.Success);
    }

    [Fact]
    public void AbilityCheck_NeverAddsProficiency()
    {
        var result = Resolver(true, 12).AbilityCheck(Bard, Ability.Cha, 14);

        Assert.Equal(CheckKind.Ability, result.Kind);
        Assert.Equal("Charisma", result.Label);
        Assert.Equal(0, result.ProficiencyBonus);
        Assert.Equal(14, result.Total);
        Assert.True(result.Success);
    }

    [Fact]
    public void SavingThrow_Proficient_AddsProficiency()
    {
        var result = Resolver(true, 8).SavingThrow(Bard, Ability.Dex, 13);

        Assert.Equal(CheckKind.SavingThrow, result.Kind);
        Assert.Equal("Dexterity save", result.Label);
        Assert.Equal(2, result.ProficiencyBonus);
        Assert.Equal(13, result.Total);
        Assert.True(result.Success);
    }

    [Fact]
    public void SavingThrow_NotProficient_AddsOnlyModifier()
    {
        var result = Resolver(true, 8).SavingThrow(Bard, Ability.Con, 9);

        Assert.Equal(0, result.ProficiencyBonus);
        Assert.Equal(8, result.Total);
        Assert.False(result.Success);
    }

    [Fact]
    public void NullSubject_Throws()
    {
        var resolver = Resolver(true, 10);

        Assert.Throws<ArgumentNullException>(() => resolver.SkillCheck(null!, "x", "X", Ability.Str, 10));
        Assert.Throws<ArgumentNullException>(() => resolver.AbilityCheck(null!, Ability.Str, 10));
        Assert.Throws<ArgumentNullException>(() => resolver.SavingThrow(null!, Ability.Str, 10));
    }

    [Fact]
    public void NaturalCritsOnChecks_IsExposed()
    {
        Assert.True(Resolver(true).NaturalCritsOnChecks);
        Assert.False(Resolver(false).NaturalCritsOnChecks);
    }
}
