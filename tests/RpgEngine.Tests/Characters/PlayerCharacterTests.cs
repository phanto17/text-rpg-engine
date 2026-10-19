using RpgEngine.Core.Characters;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Characters;

public sealed class PlayerCharacterTests
{
    [Fact]
    public void Constructor_StartsAtFullHpAndMana()
    {
        var hero = TestCharacters.Create(maxHp: 12, maxMana: 6, name: "  Aria  ");

        Assert.Equal("Aria", hero.Name);
        Assert.Equal(12, hero.CurrentHp);
        Assert.Equal(6, hero.CurrentMana);
        Assert.Equal(1, hero.Level);
        Assert.Equal(2, hero.ProficiencyBonus);
        Assert.True(hero.IsAlive);
    }

    [Fact]
    public void Equipment_StartsEmptyAndCanBeSet()
    {
        var hero = TestCharacters.Create(classId: "rogue");

        Assert.Equal("rogue", hero.ClassId);
        Assert.Null(hero.EquippedWeaponId);
        Assert.Null(hero.EquippedArmorId);

        hero.EquippedWeaponId = "shortsword";
        hero.EquippedArmorId = "leather_armor";

        Assert.Equal(("shortsword", "leather_armor"), (hero.EquippedWeaponId, hero.EquippedArmorId));
    }

    [Fact]
    public void Constructor_ExpertiseWithoutProficiency_Throws()
    {
        Assert.Throws<ArgumentException>(() => TestCharacters.Create(skills: ["stealth"], expertise: ["persuasion"]));
    }

    [Fact]
    public void Constructor_InvalidVitals_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TestCharacters.Create(maxHp: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestCharacters.Create(maxMana: -1));
        Assert.Throws<ArgumentException>(() => TestCharacters.Create(name: " "));
        Assert.Throws<ArgumentException>(() => TestCharacters.Create(backgroundId: " "));
    }

    [Fact]
    public void GetSkillProficiency_ReportsLevel()
    {
        var hero = TestCharacters.Create(skills: ["stealth", "persuasion"], expertise: ["stealth"]);

        Assert.Equal(ProficiencyLevel.Expertise, hero.GetSkillProficiency("stealth"));
        Assert.Equal(ProficiencyLevel.Proficient, hero.GetSkillProficiency("persuasion"));
        Assert.Equal(ProficiencyLevel.None, hero.GetSkillProficiency("athletics"));
        Assert.Contains("persuasion", hero.ProficientSkills);
        Assert.Contains("stealth", hero.ExpertiseSkills);
    }

    [Fact]
    public void IsProficientInSave_UsesClassSaves()
    {
        var hero = TestCharacters.Create(saves: [Ability.Str, Ability.Con]);

        Assert.True(hero.IsProficientInSave(Ability.Con));
        Assert.False(hero.IsProficientInSave(Ability.Wis));
        Assert.Equal(2, hero.SavingThrows.Count);
    }

    [Fact]
    public void TakeDamage_ClampsAtZeroAndReportsLoss()
    {
        var hero = TestCharacters.Create(maxHp: 10);

        Assert.Equal(4, hero.TakeDamage(4));
        Assert.Equal(6, hero.TakeDamage(50));

        Assert.Equal(0, hero.CurrentHp);
        Assert.False(hero.IsAlive);
    }

    [Fact]
    public void Heal_ClampsAtMaxAndReportsGain()
    {
        var hero = TestCharacters.Create(maxHp: 10);
        hero.TakeDamage(3);

        Assert.Equal(3, hero.Heal(8));
        Assert.Equal(10, hero.CurrentHp);
    }

    [Fact]
    public void Mana_SpendAndRestore()
    {
        var hero = TestCharacters.Create(maxMana: 6);

        Assert.True(hero.TrySpendMana(4));
        Assert.False(hero.TrySpendMana(3));
        Assert.Equal(2, hero.CurrentMana);
        Assert.Equal(4, hero.RestoreMana(10));
        Assert.Equal(6, hero.CurrentMana);
    }

    [Fact]
    public void GoldAndXp_Accumulate()
    {
        var hero = TestCharacters.Create();

        hero.AddGold(5);
        hero.AddGold(7);
        hero.AddXp(100);

        Assert.Equal(12, hero.Gold);
        Assert.Equal(100, hero.Xp);
    }

    [Fact]
    public void NegativeAmounts_Throw()
    {
        var hero = TestCharacters.Create();

        Assert.Throws<ArgumentOutOfRangeException>(() => hero.TakeDamage(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => hero.Heal(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => hero.TrySpendMana(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => hero.RestoreMana(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => hero.AddGold(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => hero.AddXp(-1));
    }

    [Fact]
    public void EncounterUses_RechargeAndSpend()
    {
        var hero = TestCharacters.Create(spells: ["second_wind"]);

        Assert.False(hero.TryUseEncounterCharge("second_wind"));

        hero.RechargeEncounterUses(new Dictionary<string, int> { ["second_wind"] = 1 });

        Assert.Equal(1, hero.GetEncounterUses("second_wind"));
        Assert.True(hero.TryUseEncounterCharge("second_wind"));
        Assert.False(hero.TryUseEncounterCharge("second_wind"));

        hero.RechargeEncounterUses(new Dictionary<string, int> { ["second_wind"] = 1 });
        Assert.Equal(1, hero.GetEncounterUses("second_wind"));
        Assert.Equal(new[] { "second_wind" }, hero.SpellIds);
    }

    [Fact]
    public void RestoreState_AppliesSavedValues()
    {
        var hero = TestCharacters.Create(maxHp: 10, maxMana: 4);

        hero.RestoreState(level: 5, xp: 300, currentHp: 7, currentMana: 1, gold: 42);

        Assert.Equal((5, 300, 7, 1, 42), (hero.Level, hero.Xp, hero.CurrentHp, hero.CurrentMana, hero.Gold));
        Assert.Equal(3, hero.ProficiencyBonus);
    }

    [Theory]
    [InlineData(0, 0, 5, 0, 0)]
    [InlineData(1, -1, 5, 0, 0)]
    [InlineData(1, 0, 11, 0, 0)]
    [InlineData(1, 0, -1, 0, 0)]
    [InlineData(1, 0, 5, 5, 0)]
    [InlineData(1, 0, 5, -1, 0)]
    [InlineData(1, 0, 5, 0, -1)]
    public void RestoreState_InvalidValues_Throw(int level, int xp, int hp, int mana, int gold)
    {
        var hero = TestCharacters.Create(maxHp: 10, maxMana: 4);

        Assert.Throws<ArgumentOutOfRangeException>(() => hero.RestoreState(level, xp, hp, mana, gold));
    }
}
