using RpgEngine.Core.Content;
using RpgEngine.Core.Events;
using RpgEngine.Core.World;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.World;

public sealed class ProgressionTests
{
    /// <summary>Levels at 100 and 300 XP; the wizard learns Second Wind at level 2 and nothing at level 3.</summary>
    internal static GameContent Leveling() => new TestContent()
        .With("game.json", TestContent.Manifest.Replace("\"naturalCritsOnChecks\": true", "\"xpThresholdsByLevel\": [0, 100, 300], \"naturalCritsOnChecks\": true", StringComparison.Ordinal))
        .With("Rules/classes.json", TestContent.Classes.Replace("\"spells\": [\"firebolt\", \"missile\"]", "\"spells\": [\"firebolt\", \"missile\"], \"spellsByLevel\": [ { \"level\": 2, \"spells\": [\"second_wind\"] } ]", StringComparison.Ordinal))
        .Load();

    private static TurnLog Grant(GameContent content, Core.Characters.PlayerCharacter player, int xp)
    {
        var log = new TurnLog();
        Progression.GrantXp(player, content, xp, log);
        return log;
    }

    [Fact]
    public void GrantXp_CrossingAThreshold_LevelsUpOnce()
    {
        var fighter = TestCharacters.Create(abilities: TestCharacters.Scores(con: 14), maxHp: 12, classId: "fighter");
        fighter.TakeDamage(5);

        var log = Grant(Leveling(), fighter, 120);

        // d10 fighter: 10 / 2 + 1 + CON 2 = 8 HP, no mana.
        Assert.Equal(new XpGainedEvent(120, 120), log.Events[0]);
        var up = Assert.IsType<LeveledUpEvent>(Assert.Single(log.Events.Skip(1)));
        Assert.Equal((2, 8, 20, 0, 0, 2), (up.Level, up.HpGain, up.MaxHp, up.ManaGain, up.MaxMana, up.ProficiencyBonus));
        Assert.Empty(up.NewSpells);
        Assert.Equal((2, 20, 15), (fighter.Level, fighter.MaxHp, fighter.CurrentHp));
    }

    [Fact]
    public void GrantXp_BigAward_LevelsUpOncePerThreshold()
    {
        var fighter = TestCharacters.Create(maxHp: 10, classId: "fighter");

        var log = Grant(Leveling(), fighter, 350);

        Assert.Equal(new[] { 2, 3 }, log.Events.OfType<LeveledUpEvent>().Select(e => e.Level));
        Assert.Equal((3, 22), (fighter.Level, fighter.MaxHp));
        Assert.Null(Progression.NextLevelXp(fighter, Leveling()));
    }

    [Fact]
    public void GrantXp_Caster_GainsManaAndLearnsLevelSpells()
    {
        var wizard = TestCharacters.Create(abilities: TestCharacters.Scores(con: 8), maxHp: 6, maxMana: 6, spells: ["firebolt", "missile"], classId: "wizard");

        var up = Assert.Single(Grant(Leveling(), wizard, 100).Events.OfType<LeveledUpEvent>());

        // d6 wizard with CON 8: 6 / 2 + 1 - 1 = 3 HP; casters gain 2 mana.
        Assert.Equal((3, 9, 2, 8), (up.HpGain, up.MaxHp, up.ManaGain, up.MaxMana));
        Assert.Equal("Second Wind", Assert.Single(up.NewSpells).Name);
        Assert.Equal(new[] { "firebolt", "missile", "second_wind" }, wizard.SpellIds);
        Assert.Equal((8, 8), (wizard.CurrentMana, wizard.MaxMana));
    }

    [Fact]
    public void GrantXp_DefaultThresholds_NeverLevel()
    {
        var fighter = TestCharacters.Create(classId: "fighter");
        var content = new TestContent().Load();

        var log = Grant(content, fighter, 10_000);

        Assert.Single(log.Events);
        Assert.Equal(1, fighter.Level);
        Assert.Null(Progression.NextLevelXp(fighter, content));
    }

    [Fact]
    public void GrantXp_DeadOrUnknownClass_OnlyAddsXp()
    {
        var dead = TestCharacters.Create(classId: "fighter");
        dead.TakeDamage(dead.MaxHp);
        var stranger = TestCharacters.Create(classId: "bard");

        Assert.Single(Grant(Leveling(), dead, 500).Events);
        Assert.Single(Grant(Leveling(), stranger, 500).Events);
        Assert.Equal((1, 500), (dead.Level, dead.Xp));
        Assert.Equal((1, 500), (stranger.Level, stranger.Xp));
    }

    [Fact]
    public void HitPointGain_IsAtLeastOne()
    {
        var wizard = Leveling().Classes["wizard"];

        Assert.Equal(1, Progression.HitPointGain(wizard, TestCharacters.Scores(con: 1)));
        Assert.Equal(4, Progression.HitPointGain(wizard, TestCharacters.Scores(con: 10)));
    }

    [Fact]
    public void NextLevelXp_ReportsTheNextThreshold()
    {
        var fighter = TestCharacters.Create(classId: "fighter");

        Assert.Equal(100, Progression.NextLevelXp(fighter, Leveling()));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var content = Leveling();
        var player = TestCharacters.Create();

        Assert.Throws<ArgumentNullException>(() => Progression.GrantXp(null!, content, 1, new TurnLog()));
        Assert.Throws<ArgumentNullException>(() => Progression.GrantXp(player, null!, 1, new TurnLog()));
        Assert.Throws<ArgumentNullException>(() => Progression.GrantXp(player, content, 1, null!));
        Assert.Throws<ArgumentNullException>(() => Progression.NextLevelXp(null!, content));
        Assert.Throws<ArgumentNullException>(() => Progression.NextLevelXp(player, null!));
        Assert.Throws<ArgumentNullException>(() => Progression.HitPointGain(null!, TestCharacters.Scores()));
        Assert.Throws<ArgumentNullException>(() => Progression.HitPointGain(content.Classes["wizard"], null!));
    }
}
