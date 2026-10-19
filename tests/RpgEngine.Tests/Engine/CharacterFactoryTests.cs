using RpgEngine.Core.Characters;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Engine;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Engine;

public sealed class CharacterFactoryTests
{
    private static readonly RpgEngine.Core.Content.GameContent Content = new TestContent().Load();
    private static readonly CharacterFactory Factory = new(Content);

    [Fact]
    public void AssignByPriority_GivesHighestScoreToFirstPriority()
    {
        var scores = CharacterFactory.AssignByPriority([8, 10, 12, 13, 14, 15], Content.Classes["fighter"]);

        // Fighter priority: str, con, dex, wis, cha, int.
        Assert.Equal(TestCharacters.Scores(str: 15, con: 14, dex: 13, wis: 12, cha: 10, @int: 8), scores);
    }

    [Fact]
    public void AssignByPriority_WrongCount_Throws()
    {
        Assert.Throws<ArgumentException>(() => CharacterFactory.AssignByPriority([15, 14], Content.Classes["fighter"]));
        Assert.Throws<ArgumentNullException>(() => CharacterFactory.AssignByPriority(null!, Content.Classes["fighter"]));
        Assert.Throws<ArgumentNullException>(() => CharacterFactory.AssignByPriority([1, 2, 3, 4, 5, 6], null!));
    }

    [Fact]
    public void StandardArray_ComesFromRules()
    {
        Assert.Equal(new[] { 15, 14, 13, 12, 10, 8 }, Factory.StandardArray);
    }

    [Fact]
    public void RollAbilityScores_RollsSixTimesFourD6DropLowest()
    {
        var dice = new DiceRoller(new FixedRandomSource(Enumerable.Repeat(new[] { 6, 5, 4, 1 }, 6).SelectMany(x => x)));

        var rolls = Factory.RollAbilityScores(dice);

        Assert.Equal(6, rolls.Count);
        Assert.All(rolls, roll => Assert.Equal(15, roll.Total));
        Assert.Throws<ArgumentNullException>(() => Factory.RollAbilityScores(null!));
    }

    [Theory]
    [InlineData(14, 12)]
    [InlineData(3, 6)]
    public void MaxHp_IsHitDiePlusConstitution(int con, int expected)
    {
        Assert.Equal(expected, CharacterFactory.MaxHp(Content.Classes["fighter"], TestCharacters.Scores(con: con)));
    }

    [Fact]
    public void MaxMana_UsesClassManaAbility()
    {
        Assert.Equal(6, CharacterFactory.MaxMana(Content.Classes["wizard"], TestCharacters.Scores(@int: 14)));
        Assert.Equal(0, CharacterFactory.MaxMana(Content.Classes["wizard"], TestCharacters.Scores(@int: 1)));
        Assert.Equal(0, CharacterFactory.MaxMana(Content.Classes["fighter"], TestCharacters.Scores(@int: 20)));
        Assert.Throws<ArgumentNullException>(() => CharacterFactory.MaxMana(null!, TestCharacters.Scores()));
        Assert.Throws<ArgumentNullException>(() => CharacterFactory.MaxMana(Content.Classes["wizard"], null!));
        Assert.Throws<ArgumentNullException>(() => CharacterFactory.MaxHp(null!, TestCharacters.Scores()));
        Assert.Throws<ArgumentNullException>(() => CharacterFactory.MaxHp(Content.Classes["wizard"], null!));
    }

    [Fact]
    public void Create_BuildsALevelOneCharacterWithGear()
    {
        var scores = TestCharacters.Scores(str: 15, con: 14, dex: 13);

        var hero = Factory.Create("  Bram  ", "fighter", "orphan", scores, ["athletics", "persuasion"], []);

        Assert.Equal(("Bram", "fighter", "orphan", 1, 12, 0, 5), (hero.Name, hero.ClassId, hero.BackgroundId, hero.Level, hero.MaxHp, hero.MaxMana, hero.Gold));
        Assert.Equal(ProficiencyLevel.Proficient, hero.GetSkillProficiency("athletics"));
        Assert.Equal(ProficiencyLevel.Proficient, hero.GetSkillProficiency("stealth"));
        Assert.Equal(new[] { "sword", "mail", "potion", "relic" }, hero.Inventory.Entries.Select(e => e.ItemId));
        Assert.True(hero.IsProficientInSave(Ability.Con));
        Assert.Equal(("sword", "mail"), (hero.EquippedWeaponId, hero.EquippedArmorId));
        Assert.Equal(2, hero.Inventory.CountOf("potion"));
        Assert.Equal(new[] { "second_wind" }, hero.SpellIds);
    }

    [Fact]
    public void Create_WizardWithExpertise()
    {
        var hero = Factory.Create("Ilse", "wizard", "orphan", TestCharacters.Scores(@int: 15), ["religion", "insight"], ["religion"]);

        Assert.Equal(ProficiencyLevel.Expertise, hero.GetSkillProficiency("religion"));
        Assert.Equal(6, hero.MaxMana);
        Assert.Equal("staff", hero.EquippedWeaponId);
        Assert.Null(hero.EquippedArmorId);
    }

    [Fact]
    public void Create_BurdenAddsItsSkillGoldAndKeepsake()
    {
        var hero = Factory.Create("Mara", "fighter", "bailiff", TestCharacters.Scores(), ["athletics", "intimidation"], []);

        Assert.Equal(15, hero.Gold);
        Assert.Equal(new[] { "athletics", "intimidation", "persuasion" }, hero.ProficientSkills.Order(StringComparer.Ordinal));
        Assert.True(hero.Inventory.Contains("key"));
    }

    [Theory]
    [InlineData("", "fighter", "orphan", new[] { "athletics", "persuasion" }, new string[0], "name")]
    [InlineData("A name that is far too long to fit", "fighter", "orphan", new[] { "athletics", "persuasion" }, new string[0], "name")]
    [InlineData("Bram", "bard", "orphan", new[] { "athletics", "persuasion" }, new string[0], "classId")]
    [InlineData("Bram", "fighter", "orphan", new[] { "athletics" }, new string[0], "skills")]
    [InlineData("Bram", "fighter", "orphan", new[] { "athletics", "athletics" }, new string[0], "skills")]
    [InlineData("Bram", "fighter", "orphan", new[] { "athletics", "stealth" }, new string[0], "skills")]
    [InlineData("Bram", "fighter", "orphan", new[] { "athletics", "persuasion" }, new[] { "athletics" }, "expertise")]
    [InlineData("Ilse", "wizard", "orphan", new[] { "religion", "insight" }, new string[0], "expertise")]
    [InlineData("Ilse", "wizard", "orphan", new[] { "religion", "insight" }, new[] { "perception" }, "expertise")]
    [InlineData("Bram", "fighter", "noble", new[] { "athletics", "persuasion" }, new string[0], "backgroundId")]
    [InlineData("Bram", "fighter", "bailiff", new[] { "athletics", "persuasion" }, new string[0], "skills")]
    public void Create_InvalidChoices_Throw(string name, string classId, string backgroundId, string[] skills, string[] expertise, string parameter)
    {
        var exception = Assert.Throws<ArgumentException>(() => Factory.Create(name, classId, backgroundId, TestCharacters.Scores(), skills, expertise));

        Assert.Equal(parameter, exception.ParamName);
    }

    [Fact]
    public void Create_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new CharacterFactory(null!));
        Assert.Throws<ArgumentNullException>(() => Factory.Create("x", "fighter", "orphan", null!, [], []));
        Assert.Throws<ArgumentNullException>(() => Factory.Create("x", "fighter", "orphan", TestCharacters.Scores(), null!, []));
        Assert.Throws<ArgumentNullException>(() => Factory.Create("x", "fighter", "orphan", TestCharacters.Scores(), [], null!));
        Assert.Throws<ArgumentException>(() => Factory.Create(null!, "fighter", "orphan", TestCharacters.Scores(), ["athletics", "persuasion"], []));
    }
}
