using RpgEngine.Core.Characters;
using RpgEngine.Core.Combat;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Combat;

public sealed class CombatRulesTests
{
    private static readonly RpgEngine.Core.Content.GameContent Content = CombatFixture.Load();

    private static PlayerCharacter Hero(int str = 10, int dex = 14, string classId = "fighter") =>
        TestCharacters.Create(abilities: TestCharacters.Scores(str: str, dex: dex), classId: classId, spells: ["second_wind", "sneak", "firebolt"]);

    // DEX 18 gives +4.
    [Theory]
    [InlineData(null, 14)]
    [InlineData("leather", 15)]
    [InlineData("scale", 16)]
    [InlineData("mail", 16)]
    [InlineData("sword", 14)]
    public void ArmorClass_UsesArmorAndCappedDexterity(string? armor, int expected)
    {
        var hero = Hero(dex: 18);
        hero.EquippedArmorId = armor;

        Assert.Equal(expected, CombatRules.ArmorClass(hero, Content));
    }

    [Fact]
    public void Weapon_Unarmed_UsesStrengthAndFlatDamage()
    {
        var weapon = CombatRules.Weapon(Hero(str: 14), Content);

        Assert.Equal(("Unarmed strike", Ability.Str, 4, "1+2", DamageType.Bludgeoning), (weapon.Name, weapon.Ability, weapon.ToHitBonus, weapon.Damage.Text, weapon.DamageType));
    }

    [Fact]
    public void Weapon_Finesse_UsesBetterOfStrengthAndDexterity()
    {
        var nimble = Hero(str: 8, dex: 16);
        nimble.EquippedWeaponId = "dagger";
        var strong = Hero(str: 16, dex: 8);
        strong.EquippedWeaponId = "dagger";

        Assert.Equal(Ability.Dex, CombatRules.Weapon(nimble, Content).Ability);
        Assert.Equal("1d4+3", CombatRules.Weapon(nimble, Content).Damage.Text);
        Assert.Equal(Ability.Str, CombatRules.Weapon(strong, Content).Ability);
    }

    [Fact]
    public void Weapon_Ranged_UsesDexterity()
    {
        var archer = Hero(str: 16, dex: 12);
        archer.EquippedWeaponId = "bow";

        var weapon = CombatRules.Weapon(archer, Content);

        Assert.Equal((Ability.Dex, 3), (weapon.Ability, weapon.ToHitBonus));
    }

    [Fact]
    public void Weapon_NonWeaponEquipped_FallsBackToUnarmed()
    {
        var hero = Hero();
        hero.EquippedWeaponId = "potion";

        Assert.Equal("Unarmed strike", CombatRules.Weapon(hero, Content).Name);
    }

    [Fact]
    public void CastingAbility_ComesFromClassMana()
    {
        Assert.Equal(Ability.Int, CombatRules.CastingAbility(Hero(classId: "wizard"), Content));
        Assert.Equal(Ability.Int, CombatRules.CastingAbility(Hero(classId: "fighter"), Content));
    }

    [Fact]
    public void EncounterUses_ListsOnlyTechniquesWithUses()
    {
        var uses = CombatRules.EncounterUses(Hero(), Content);

        Assert.Equal(2, uses.Count);
        Assert.Equal(1, uses["second_wind"]);
        Assert.Equal(2, uses["sneak"]);
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var hero = Hero();

        Assert.Throws<ArgumentNullException>(() => CombatRules.ArmorClass(null!, Content));
        Assert.Throws<ArgumentNullException>(() => CombatRules.ArmorClass(hero, null!));
        Assert.Throws<ArgumentNullException>(() => CombatRules.Weapon(null!, Content));
        Assert.Throws<ArgumentNullException>(() => CombatRules.Weapon(hero, null!));
        Assert.Throws<ArgumentNullException>(() => CombatRules.CastingAbility(null!, Content));
        Assert.Throws<ArgumentNullException>(() => CombatRules.CastingAbility(hero, null!));
        Assert.Throws<ArgumentNullException>(() => CombatRules.EncounterUses(null!, Content));
        Assert.Throws<ArgumentNullException>(() => CombatRules.EncounterUses(hero, null!));
    }

    [Fact]
    public void MonsterCombatant_HealClampsAndRejectsNegatives()
    {
        var combat = new CombatHarness(dice: [15, 2]);
        combat.Begin("brute");
        var brute = combat.Session.ActiveEnemies[0];

        brute.ApplyDamage(5);
        Assert.Equal(5, brute.Heal(10));
        Assert.Throws<ArgumentOutOfRangeException>(() => brute.ApplyDamage(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => brute.Heal(-1));
        Assert.Equal(ProficiencyLevel.None, brute.GetSkillProficiency("athletics"));
        Assert.False(brute.IsProficientInSave(Ability.Str));
        Assert.Equal(0, brute.ProficiencyBonus);
        Assert.Equal(ProficiencyLevel.Proficient, combat.Session.Player.GetSkillProficiency("persuasion"));
        Assert.True(combat.Session.Player.IsProficientInSave(Ability.Con));
        Assert.Equal(2, combat.Session.Player.ProficiencyBonus);
        Assert.Equal(0, combat.Session.Player.Heal(5));
    }
}
