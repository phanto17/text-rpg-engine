using RpgEngine.Core.Characters;
using RpgEngine.Core.Checks;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;
using RpgEngine.Core.World;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.World;

public sealed class CheckRunnerTests
{
    private static readonly PlayerCharacter Hero = TestCharacters.Create(
        abilities: TestCharacters.Scores(cha: 16, str: 12),
        skills: ["persuasion"]);

    [Fact]
    public void Run_SkillCheck_UsesSkillAbilityAndProficiency()
    {
        var game = new TestWorld(player: Hero, dice: [10]);

        var result = game.Checks.Run(new CheckDef { Skill = "persuasion", Dc = 15 }, game.State);

        Assert.Equal(CheckKind.Skill, result.Kind);
        Assert.Equal("Persuasion", result.Label);
        Assert.Equal(15, result.Total);
        Assert.True(result.Success);
    }

    [Fact]
    public void Run_AbilityCheck_UsesRawAbility()
    {
        var game = new TestWorld(player: Hero, dice: [10]);

        var result = game.Checks.Run(new CheckDef { Ability = Ability.Str, Dc = 12 }, game.State);

        Assert.Equal(CheckKind.Ability, result.Kind);
        Assert.Equal(11, result.Total);
        Assert.False(result.Success);
    }

    [Fact]
    public void ModeFor_ExplicitAndConditionalAdvantage()
    {
        var game = new TestWorld(player: Hero);
        var conditionalAdvantage = new CheckDef { Skill = "persuasion", Dc = 10, AdvantageIf = [new FlagCondition { Flag = "charmed" }] };
        var conditionalDisadvantage = new CheckDef { Skill = "persuasion", Dc = 10, DisadvantageIf = [new FlagCondition { Flag = "charmed" }] };

        Assert.Equal(RollMode.Normal, game.Checks.ModeFor(conditionalAdvantage, game.State));
        Assert.Equal(RollMode.Advantage, game.Checks.ModeFor(new CheckDef { Skill = "persuasion", Dc = 10, Advantage = true }, game.State));
        Assert.Equal(RollMode.Disadvantage, game.Checks.ModeFor(new CheckDef { Skill = "persuasion", Dc = 10, Disadvantage = true }, game.State));

        game.World.SetFlag("charmed");

        Assert.Equal(RollMode.Advantage, game.Checks.ModeFor(conditionalAdvantage, game.State));
        Assert.Equal(RollMode.Disadvantage, game.Checks.ModeFor(conditionalDisadvantage, game.State));
        Assert.Equal(RollMode.Normal, game.Checks.ModeFor(conditionalAdvantage with { Disadvantage = true }, game.State));
    }

    [Fact]
    public void Run_WithAdvantage_RollsTwoDice()
    {
        var game = new TestWorld(player: Hero, dice: [4, 17]);

        var result = game.Checks.Run(new CheckDef { Skill = "persuasion", Dc = 15, Advantage = true }, game.State);

        Assert.Equal(17, result.Roll.Natural);
        Assert.Equal(0, game.Random.Remaining);
    }

    [Fact]
    public void Label_UsesSkillOrAbilityName()
    {
        var game = new TestWorld();

        Assert.Equal("Persuasion", game.Checks.Label(new CheckDef { Skill = "persuasion", Dc = 10 }));
        Assert.Equal("Wisdom", game.Checks.Label(new CheckDef { Ability = Ability.Wis, Dc = 10 }));
    }

    [Fact]
    public void SavingThrow_DelegatesToResolver()
    {
        var game = new TestWorld(player: Hero, dice: [9]);

        var result = game.Checks.SavingThrow(Hero, Ability.Cha, 12);

        Assert.Equal(CheckKind.SavingThrow, result.Kind);
        Assert.Equal(12, result.Total);
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var game = new TestWorld();

        Assert.Throws<ArgumentNullException>(() => new CheckRunner(null!, new CheckResolver(game.Dice, true)));
        Assert.Throws<ArgumentNullException>(() => new CheckRunner(game.Content, null!));
        Assert.Throws<ArgumentNullException>(() => game.Checks.Run(null!, game.State));
        Assert.Throws<ArgumentNullException>(() => game.Checks.Run(new CheckDef { Skill = "persuasion", Dc = 10 }, null!));
        Assert.Throws<ArgumentNullException>(() => game.Checks.ModeFor(null!, game.State));
        Assert.Throws<ArgumentNullException>(() => game.Checks.Label(null!));
    }
}
