using RpgEngine.Core.Combat;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Events;
using RpgEngine.Core.World;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Combat;

public sealed class CombatEngineTests
{
    // Dice conventions: the first values are initiative d20s (player first, then monsters in listed order).
    // The hero has DEX 12 (+1); the brute has DEX 8 (-1), so [15, 2] means the hero acts first.

    [Fact]
    public void Begin_RollsInitiativeAndBreaksTiesByDexterity()
    {
        var combat = new CombatHarness(dice: [10, 9, 15]);

        var log = combat.Begin("rats");

        var started = Assert.IsType<CombatStartedEvent>(log.Events[0]);
        Assert.Equal("Squeak, Tester!", started.Intro);
        var initiative = Assert.IsType<InitiativeRolledEvent>(log.Events[1]);
        Assert.Equal(new[] { "Giant Rat B", "Giant Rat A", "Tester" }, initiative.Order.Select(e => e.Name));
        Assert.Equal(new[] { 17, 11, 11 }, initiative.Order.Select(e => e.Total));
        Assert.Equal(new[] { "rat#2", "rat#1", "player" }, combat.Session.TurnOrder.Select(c => c.Id));
        Assert.Equal("Giant Rat B", combat.Session.CurrentActor.Name);
        Assert.False(combat.Session.IsPlayerTurn);
        Assert.Equal(1, combat.Session.Round);
        Assert.All(combat.Session.ActiveEnemies, rat => Assert.Equal(7, rat.MaxHp));
    }

    [Fact]
    public void Begin_TiedInitiativeAndDexterity_FavorsTheParty()
    {
        var combat = new CombatHarness(dice: [10, 10]);

        combat.Begin("ghoul");

        Assert.True(combat.Session.IsPlayerTurn);
    }

    [Fact]
    public void Begin_RollModeHitPoints_RollsHitDice()
    {
        var content = CombatFixture.Content().With("game.json", TestContent.Manifest.Replace("\"average\"", "\"roll\"", StringComparison.Ordinal)).Load();
        var combat = new CombatHarness(content: content, dice: [3, 4, 1, 1, 10, 10, 10]);

        combat.Begin("rats");

        Assert.Equal(new[] { 7, 2 }, combat.Session.ActiveEnemies.OrderBy(r => r.Id).Select(r => r.MaxHp));
    }

    [Fact]
    public void Attack_Hit_DealsWeaponDamagePlusStrength()
    {
        var combat = new CombatHarness(dice: [15, 2, 10, 4]);
        combat.Begin("brute");

        var log = combat.Act(new AttackAction(combat.EnemyId()));

        var attack = Assert.IsType<AttackRolledEvent>(log.Events[0]);
        Assert.Equal(("Longsword", 5, 15, 10, true, false), (attack.AttackName, attack.Bonus, attack.Total, attack.TargetArmorClass, attack.Hit, attack.Critical));
        var damage = Assert.Single(log.Events.OfType<DamageTakenEvent>());
        Assert.Equal((7, 23, 30, DamageModifier.None), (damage.Amount, damage.Hp, damage.MaxHp, damage.Modifier));
        Assert.Equal("Brute", combat.Session.CurrentActor.Name);
        Assert.IsType<TurnStartedEvent>(log.Events[^1]);
    }

    [Fact]
    public void Attack_BelowArmorClass_Misses()
    {
        var combat = new CombatHarness(dice: [15, 2, 4]);
        combat.Begin("brute");

        var log = combat.Act(new AttackAction(combat.EnemyId()));

        Assert.False(Assert.IsType<AttackRolledEvent>(log.Events[0]).Hit);
        Assert.Empty(log.Events.OfType<DamageTakenEvent>());
    }

    [Fact]
    public void Attack_Natural1_AlwaysMisses()
    {
        var mighty = CombatFixture.Hero(abilities: TestCharacters.Scores(str: 30, dex: 12));
        var combat = new CombatHarness(mighty, dice: [15, 2, 1]);
        combat.Begin("brute");

        var attack = Assert.IsType<AttackRolledEvent>(combat.Act(new AttackAction(combat.EnemyId())).Events[0]);

        Assert.True(attack.Total >= attack.TargetArmorClass);
        Assert.False(attack.Hit);
    }

    [Fact]
    public void Attack_Natural20_CritsAndDoublesDiceButNotModifier()
    {
        var combat = new CombatHarness(dice: [15, 2, 20, 3, 5]);
        combat.Begin("brute");

        var log = combat.Act(new AttackAction(combat.EnemyId()));

        Assert.True(Assert.IsType<AttackRolledEvent>(log.Events[0]).Critical);
        Assert.Equal(11, Assert.Single(log.Events.OfType<DamageTakenEvent>()).Amount);
    }

    [Fact]
    public void Damage_ResistanceHalves()
    {
        var combat = new CombatHarness(dice: [15, 2, 15, 5]);
        combat.Begin("ghoul");

        var damage = Assert.Single(combat.Act(new AttackAction(combat.EnemyId())).Events.OfType<DamageTakenEvent>());

        Assert.Equal((4, DamageModifier.Resisted, DamageType.Slashing), (damage.Amount, damage.Modifier, damage.DamageType));
    }

    [Fact]
    public void Damage_VulnerabilityDoubles_ThroughSpellAttack()
    {
        var combat = new CombatHarness(dice: [15, 2, 15, 3]);
        combat.Begin("ghoul");

        var log = combat.Act(new CastAction("firebolt", combat.EnemyId()));

        Assert.Equal(4, Assert.IsType<AttackRolledEvent>(log.Events[1]).Bonus);
        var damage = Assert.Single(log.Events.OfType<DamageTakenEvent>());
        Assert.Equal((6, DamageModifier.Vulnerable), (damage.Amount, damage.Modifier));
        Assert.Equal(SpellKind.Spell, Assert.IsType<SpellCastEvent>(log.Events[0]).Kind);
    }

    [Fact]
    public void Damage_ImmunityNegates_AndManaIsSpent()
    {
        var combat = new CombatHarness(dice: [15, 2, 1, 1, 1]);
        combat.Begin("ghoul");

        var log = combat.Act(new CastAction("missile", combat.EnemyId()));

        Assert.Equal(new ManaChangedEvent(-2, 4, 6), log.Events[0]);
        var damage = Assert.Single(log.Events.OfType<DamageTakenEvent>());
        Assert.Equal((0, DamageModifier.Immune), (damage.Amount, damage.Modifier));
    }

    [Fact]
    public void MonsterOnHit_FailedSave_AppliesStatusThatLastsTwoOwnTurns()
    {
        // init hero 2, ghoul 15 | claw 15, dmg 2, save 3 | hero (disadvantage) 18,4 | claw 1 | hero (disadvantage) 10,12, dmg 1
        var combat = new CombatHarness(dice: [2, 15, 15, 2, 3, 18, 4, 1, 10, 12, 1]);
        combat.Begin("ghoul");

        var ghoulTurn = combat.EnemyTurn();

        Assert.False(Assert.IsType<CheckRolledEvent>(ghoulTurn.Events.Single(e => e is CheckRolledEvent)).Result.Success);
        Assert.Equal(new StatusAppliedEvent("Tester", "Chilled"), ghoulTurn.Events.OfType<StatusAppliedEvent>().Single());
        Assert.Equal(17, combat.World.Player.CurrentHp);
        Assert.Equal(RollMode.Disadvantage, combat.Session.Player.AttackRollMode);

        var firstSwing = Assert.IsType<AttackRolledEvent>(combat.Act(new AttackAction(combat.EnemyId())).Events[0]);
        Assert.Equal((4, false), (firstSwing.Roll.Natural, firstSwing.Hit));
        Assert.Equal(1, Assert.Single(combat.Session.Player.Statuses).RemainingTurns);

        combat.EnemyTurn();
        var secondTurn = combat.Act(new AttackAction(combat.EnemyId()));

        Assert.True(Assert.IsType<AttackRolledEvent>(secondTurn.Events[0]).Hit);
        Assert.Contains(new StatusExpiredEvent("Tester", "Chilled"), secondTurn.Events);
        Assert.Empty(combat.Session.Player.Statuses);
        Assert.Equal(RollMode.Normal, combat.Session.Player.AttackRollMode);
    }

    [Fact]
    public void MonsterOnHit_SuccessfulSave_AppliesNothing()
    {
        var combat = new CombatHarness(dice: [2, 15, 15, 2, 15]);
        combat.Begin("ghoul");

        var log = combat.EnemyTurn();

        Assert.True(log.Events.OfType<CheckRolledEvent>().Single().Result.Success);
        Assert.Empty(log.Events.OfType<StatusAppliedEvent>());
    }

    [Fact]
    public void Defend_RaisesArmorClassUntilEndOfNextOwnTurn()
    {
        // init 15, 2 | defend | brute: attack pick 0 (Slam), d20 12 (17 vs AC 18 = miss) | hero attack 3 (miss)
        var combat = new CombatHarness(dice: [15, 2, 0, 12, 3]);
        combat.Begin("brute");

        combat.Act(new DefendAction());
        Assert.Equal(18, combat.Session.Player.ArmorClass);

        var brute = combat.EnemyTurn();
        var slam = Assert.IsType<AttackRolledEvent>(brute.Events[0]);
        Assert.Equal(("Slam", 17, 18, false), (slam.AttackName, slam.Total, slam.TargetArmorClass, slam.Hit));
        Assert.Equal(18, combat.Session.Player.ArmorClass);

        var log = combat.Act(new AttackAction(combat.EnemyId()));

        Assert.Contains(new StatusExpiredEvent("Tester", "Defending"), log.Events);
        Assert.Equal(16, combat.Session.Player.ArmorClass);
    }

    [Fact]
    public void WeightedAttack_AndDamageOverTime()
    {
        // init hero 2, brute 15 | pick 3 (Torch), d20 15 hit, flat 1 fire, burning | hero turn start: burning 1d4 = 2
        var combat = new CombatHarness(dice: [2, 15, 3, 15, 2]);
        combat.Begin("brute");

        var log = combat.EnemyTurn();

        Assert.Equal("Torch", Assert.IsType<AttackRolledEvent>(log.Events[0]).AttackName);
        Assert.Contains(new StatusAppliedEvent("Tester", "Burning"), log.Events);
        Assert.Equal("Burning", log.Events.OfType<DiceRolledEvent>().Last().Label);
        Assert.Equal(17, combat.World.Player.CurrentHp);
        Assert.True(combat.Session.IsPlayerTurn);
    }

    [Fact]
    public void DamageOverTime_CanDefeatThePlayer()
    {
        var combat = new CombatHarness(CombatFixture.Hero(maxHp: 2), dice: [2, 15, 3, 15, 2]);
        combat.Begin("brute");

        var log = combat.EnemyTurn();

        Assert.Equal(CombatOutcome.Defeat, combat.Session.Outcome);
        Assert.Contains(new CombatantDefeatedEvent("Tester", true), log.Events);
        Assert.Equal(new CombatEndedEvent(CombatOutcome.Defeat), log.Events[^1]);
    }

    [Fact]
    public void SkipTurnStatus_SkipsTheMonstersTurnAndExpires()
    {
        var combat = new CombatHarness(dice: [15, 2, 15]);
        combat.Begin("brute");

        var log = combat.Act(new CastAction("daze", combat.EnemyId()));

        Assert.Contains(new StatusAppliedEvent("Brute", "Stunned"), log.Events);
        Assert.Contains(new TurnSkippedEvent("Brute", "Stunned"), log.Events);
        Assert.Contains(new StatusExpiredEvent("Brute", "Stunned"), log.Events);
        Assert.True(combat.Session.IsPlayerTurn);
        Assert.Equal(2, combat.Session.Round);
    }

    [Fact]
    public void Monster_BelowFleeThreshold_FleesAndTheFightIsWon()
    {
        var combat = new CombatHarness(dice: [15, 2, 10, 3]);
        combat.Begin("kobold");
        combat.Act(new AttackAction(combat.EnemyId()));

        var log = combat.EnemyTurn();

        Assert.Equal(new CombatantFledEvent("Kobold", false), log.Events[0]);
        Assert.Equal(CombatOutcome.Victory, combat.Session.Outcome);
        Assert.Empty(log.Events.OfType<XpGainedEvent>());
    }

    [Fact]
    public void Victory_GrantsXpLootAndRunsEncounterEffects()
    {
        // init hero 20, rats 1, 1 | kill A (15, 4) | rat B bite 1 (miss) | kill B (15, 4) | loot picks 50, 99
        var combat = new CombatHarness(dice: [20, 1, 1, 15, 4, 1, 15, 4, 50, 99]);
        combat.Begin("rats");

        combat.Act(new AttackAction("rat#1"));
        combat.EnemyTurn();
        var log = combat.Act(new AttackAction("rat#2"));

        Assert.Equal(CombatOutcome.Victory, combat.Session.Outcome);
        Assert.True(combat.Session.IsOver);
        Assert.False(combat.Session.IsPlayerTurn);
        Assert.Equal(new XpGainedEvent(50, 50), log.Events.OfType<XpGainedEvent>().Single());
        Assert.Equal(2, log.Events.OfType<ItemGainedEvent>().Count());
        Assert.Equal(2, combat.World.Player.Inventory.CountOf("key"));
        Assert.True(combat.World.World.HasFlag("rats_won"));
        Assert.True(combat.World.World.HasFlag("rats_done"));
    }

    [Fact]
    public void Victory_CrossingAnXpThreshold_LevelsUpAfterTheXp()
    {
        var content = CombatFixture.Content()
            .With("game.json", TestContent.Manifest.Replace("\"naturalCritsOnChecks\": true", "\"xpThresholdsByLevel\": [0, 50], \"naturalCritsOnChecks\": true", StringComparison.Ordinal))
            .Load();
        var combat = new CombatHarness(content: content, dice: [20, 1, 1, 15, 4, 1, 15, 4, 50, 99]);
        combat.Begin("rats");

        combat.Act(new AttackAction("rat#1"));
        combat.EnemyTurn();
        var log = combat.Act(new AttackAction("rat#2"));

        var xp = log.Events.ToList().FindIndex(e => e is XpGainedEvent);
        var level = log.Events.ToList().FindIndex(e => e is LeveledUpEvent);
        Assert.True(xp >= 0 && level == xp + 1);
        Assert.Equal(2, combat.World.Player.Level);
    }

    [Fact]
    public void Defeat_EndsTheFight()
    {
        var combat = new CombatHarness(CombatFixture.Hero(maxHp: 3), dice: [2, 15, 15, 2]);
        combat.Begin("ghoul");

        var log = combat.EnemyTurn();

        Assert.Equal(CombatOutcome.Defeat, combat.Session.Outcome);
        Assert.Empty(log.Events.OfType<CheckRolledEvent>());
        Assert.Throws<InvalidOperationException>(() => combat.EnemyTurn());
    }

    [Fact]
    public void Flee_Success_MovesToFleeRoomAndRechargesAbilities()
    {
        // init 15, 2 | second wind 5 | claw 1 (miss) | flee acrobatics 15
        var combat = new CombatHarness(dice: [15, 2, 5, 1, 15]);
        combat.World.Player.TakeDamage(10);
        combat.Begin("ghoul");

        combat.Act(new CastAction("second_wind", null));
        Assert.Equal(0, combat.World.Player.GetEncounterUses("second_wind"));
        combat.EnemyTurn();
        var log = combat.Act(new FleeAction());

        Assert.Equal(CombatOutcome.Fled, combat.Session.Outcome);
        Assert.Contains(new CombatantFledEvent("Tester", true), log.Events);
        Assert.Equal(new MoveToTransition("start"), Assert.Single(ExplorationHarness.Transitions(log)));
        Assert.Equal(1, combat.World.Player.GetEncounterUses("second_wind"));
        Assert.Equal(16, combat.World.Player.CurrentHp);
    }

    [Fact]
    public void Flee_Failure_EndsTurn()
    {
        var combat = new CombatHarness(dice: [15, 2, 5]);
        combat.Begin("ghoul");

        var log = combat.Act(new FleeAction());

        Assert.Contains(log.Events.OfType<NarrationEvent>(), e => e.Text == "You fail to break away!");
        Assert.Equal(CombatOutcome.Ongoing, combat.Session.Outcome);
        Assert.False(combat.Session.IsPlayerTurn);
    }

    [Fact]
    public void Flee_NotAllowed_Throws()
    {
        var combat = new CombatHarness(dice: [15, 2]);
        combat.Begin("kobold");

        Assert.False(combat.Session.CanFlee);
        Assert.Throws<InvalidOperationException>(() => combat.Act(new FleeAction()));
    }

    [Fact]
    public void Parley_Success_EndsFightAndRunsEffects()
    {
        var combat = new CombatHarness(dice: [15, 2, 12]);
        combat.Begin("talkers");

        var option = Assert.Single(combat.Session.ParleyOptions());
        Assert.Equal(("Plead", "Persuasion", 12, RollMode.Normal), (option.Label, option.CheckLabel, option.Dc, option.Mode));

        var log = combat.Act(new ParleyAction(option.Index));

        Assert.Equal(CombatOutcome.Parleyed, combat.Session.Outcome);
        Assert.Contains(new CombatEndedEvent(CombatOutcome.Parleyed), log.Events);
        Assert.True(combat.World.World.HasFlag("talked_down"));
        Assert.True(combat.World.World.HasFlag("talk_done"));
    }

    [Fact]
    public void Parley_Failure_RunsFailureEffectsAndRemovesOption()
    {
        var combat = new CombatHarness(dice: [15, 2, 2]);
        combat.World.Player.Inventory.Add("key");
        combat.Begin("talkers");

        Assert.Equal(2, combat.Session.ParleyOptions().Count);
        var bribe = combat.Session.ParleyOptions()[1];
        Assert.Equal(("Charisma", RollMode.Advantage), (bribe.CheckLabel, bribe.Mode));

        var log = combat.Act(new ParleyAction(0));

        Assert.Contains(log.Events.OfType<NarrationEvent>(), e => e.Text == "It snarls.");
        Assert.Equal(new[] { "Bribe" }, combat.Session.ParleyOptions().Select(o => o.Label));
        Assert.Equal(CombatOutcome.Ongoing, combat.Session.Outcome);
    }

    [Fact]
    public void UseItem_HealsAndConsumes()
    {
        var combat = new CombatHarness(dice: [15, 2, 3, 3]);
        combat.World.Player.TakeDamage(10);
        combat.World.Player.Inventory.Add("potion");
        combat.Begin("brute");

        Assert.Equal("potion", Assert.Single(combat.Session.UsableItems()).Id);
        var log = combat.Act(new UseItemAction("potion"));

        Assert.Equal(8, log.Events.OfType<HealedEvent>().Single().Amount);
        Assert.False(combat.World.Player.Inventory.Contains("potion"));
    }

    [Fact]
    public void UseItem_EnemyTargeted_DamagesTheTargetAndConsumes()
    {
        // Initiative 15 vs 2, then the firebomb's 2d4 rolls 2 + 1; the ghoul is vulnerable to fire.
        var combat = new CombatHarness(dice: [15, 2, 2, 1]);
        combat.World.Player.Inventory.Add("firebomb");
        combat.Begin("ghoul");

        var log = combat.Act(new UseItemAction("firebomb", combat.EnemyId()));

        Assert.Contains(log.Events.OfType<NarrationEvent>(), e => e.Text == "You use the Firebomb on Ghoul.");
        Assert.Equal("Firebomb damage", log.Events.OfType<DiceRolledEvent>().Single().Label);
        var damage = log.Events.OfType<DamageTakenEvent>().Single();
        Assert.Equal(("Ghoul", 6, DamageType.Fire, DamageModifier.Vulnerable), (damage.Target, damage.Amount, damage.DamageType, damage.Modifier));
        Assert.Equal(4, combat.Session.ActiveEnemies.Single().CurrentHp);
        Assert.False(combat.World.Player.Inventory.Contains("firebomb"));
    }

    [Fact]
    public void UseItem_EnemyTargetedAttack_RollsAgainstArmorClassWithCastingBonus()
    {
        // Proficiency +2 and INT 14 (+2): a natural 8 totals 12 and meets the ghoul's AC 12.
        var combat = new CombatHarness(dice: [15, 2, 8, 4]);
        combat.World.Player.Inventory.Add("acid");
        combat.Begin("ghoul");

        var log = combat.Act(new UseItemAction("acid", combat.EnemyId()));

        var attack = log.Events.OfType<AttackRolledEvent>().Single();
        Assert.Equal(("Acid Flask", 4, 12, true), (attack.AttackName, attack.Bonus, attack.Total, attack.Hit));
        Assert.Equal(4, log.Events.OfType<DamageTakenEvent>().Single().Amount);
    }

    [Fact]
    public void UseItem_EnemyTargetedWithoutValidTarget_ThrowsAndKeepsTheItem()
    {
        var combat = new CombatHarness(dice: [15, 2]);
        combat.World.Player.Inventory.Add("firebomb");
        combat.Begin("ghoul");

        Assert.Throws<ArgumentException>(() => combat.Act(new UseItemAction("firebomb")));
        Assert.Throws<ArgumentException>(() => combat.Act(new UseItemAction("firebomb", "nobody")));

        Assert.True(combat.World.Player.Inventory.Contains("firebomb"));
        Assert.True(combat.Session.IsPlayerTurn);
    }

    [Fact]
    public void Technique_UsesPerEncounter_RunOut()
    {
        var combat = new CombatHarness(dice: [15, 2, 5, 0, 1]);
        combat.Begin("brute");

        combat.Act(new CastAction("second_wind", null));
        combat.EnemyTurn();

        var option = combat.Session.SpellOptions().Single(o => o.Spell.Id == "second_wind");
        Assert.Equal((false, "0/1 uses"), (option.CanCast, option.CostText));
        Assert.Throws<ArgumentException>(() => combat.Act(new CastAction("second_wind", null)));
    }

    [Fact]
    public void Spell_Unaffordable_Throws()
    {
        var combat = new CombatHarness(CombatFixture.Hero(maxMana: 1), dice: [15, 2]);
        combat.Begin("brute");

        var options = combat.Session.SpellOptions();
        Assert.Equal((false, "2 MP"), options.Where(o => o.Spell.Id == "missile").Select(o => (o.CanCast, o.CostText)).Single());
        Assert.Equal("free", options.Single(o => o.Spell.Id == "firebolt").CostText);
        Assert.Throws<ArgumentException>(() => combat.Act(new CastAction("missile", combat.EnemyId())));
    }

    [Fact]
    public void SneakAttack_AddsBonusDamage()
    {
        var combat = new CombatHarness(dice: [15, 2, 15, 4, 5]);
        combat.Begin("brute");

        var log = combat.Act(new CastAction("sneak", combat.EnemyId()));

        Assert.Equal("Sneak Attack", log.Events.OfType<AttackRolledEvent>().Single().AttackName);
        Assert.Equal(12, log.Events.OfType<DamageTakenEvent>().Single().Amount);
        Assert.Equal(1, combat.World.Player.GetEncounterUses("sneak"));
    }

    [Fact]
    public void SneakAttack_Miss_DealsNoDamage()
    {
        var combat = new CombatHarness(dice: [15, 2, 2]);
        combat.Begin("brute");

        Assert.Empty(combat.Act(new CastAction("sneak", combat.EnemyId())).Events.OfType<DamageTakenEvent>());
    }

    [Fact]
    public void SaveSpell_Failure_FullDamageAndStatus()
    {
        var combat = new CombatHarness(dice: [15, 2, 5, 3, 4]);
        combat.Begin("brute");

        var log = combat.Act(new CastAction("frost", combat.EnemyId()));

        var save = log.Events.OfType<CheckRolledEvent>().Single().Result;
        Assert.Equal((12, 4, false), (save.Dc, save.Total, save.Success));
        Assert.Equal(7, log.Events.OfType<DamageTakenEvent>().Single().Amount);
        Assert.Contains(new StatusAppliedEvent("Brute", "Chilled"), log.Events);
    }

    [Fact]
    public void SaveSpell_Success_HalfDamageNoStatus()
    {
        var combat = new CombatHarness(dice: [15, 2, 18, 3, 4]);
        combat.Begin("brute");

        var log = combat.Act(new CastAction("frost", combat.EnemyId()));

        Assert.Equal(3, log.Events.OfType<DamageTakenEvent>().Single().Amount);
        Assert.Empty(log.Events.OfType<StatusAppliedEvent>());
    }

    [Fact]
    public void SaveSpell_SuccessWithoutHalf_DealsNothing()
    {
        var combat = new CombatHarness(dice: [15, 2, 15, 3, 4]);
        combat.Begin("brute");

        var log = combat.Act(new CastAction("blast", combat.EnemyId()));

        Assert.Equal(0, log.Events.OfType<DamageTakenEvent>().Single().Amount);
    }

    [Fact]
    public void Snapshot_DescribesEveryCombatant()
    {
        var combat = new CombatHarness(dice: [15, 2]);
        combat.Begin("brute");

        var views = combat.Session.Snapshot();

        Assert.Equal(new[] { "Tester", "Brute" }, views.Select(v => v.Name));
        Assert.True(views[0].IsPlayer);
        Assert.Equal((30, 30, 10, true), (views[1].Hp, views[1].MaxHp, views[1].ArmorClass, views[1].IsActive));
        Assert.Empty(views[1].Statuses);
    }

    [Fact]
    public void InvalidActions_Throw()
    {
        var combat = new CombatHarness(dice: [15, 2]);
        combat.Begin("brute");

        Assert.Throws<ArgumentException>(() => combat.Act(new AttackAction("nobody")));
        Assert.Throws<ArgumentException>(() => combat.Act(new CastAction("fireball", combat.EnemyId())));
        Assert.Throws<ArgumentException>(() => combat.Act(new CastAction("firebolt", null)));
        Assert.Throws<ArgumentException>(() => combat.Act(new UseItemAction("sword")));
        Assert.Throws<ArgumentException>(() => combat.Act(new ParleyAction(0)));
        Assert.Throws<NotSupportedException>(() => combat.Act(new DanceAction()));
        Assert.Throws<InvalidOperationException>(() => combat.EnemyTurn());
        Assert.Empty(combat.Session.ParleyOptions());
        Assert.Throws<ArgumentNullException>(() => combat.Session.Act(null!, new TurnLog()));
        Assert.Throws<ArgumentNullException>(() => combat.Session.Act(new DefendAction(), null!));
        Assert.Throws<ArgumentNullException>(() => combat.Session.RunEnemyTurn(null!));
    }

    [Fact]
    public void Act_OnEnemyTurn_Throws()
    {
        var combat = new CombatHarness(dice: [2, 15]);
        combat.Begin("brute");

        Assert.Throws<InvalidOperationException>(() => combat.Act(new DefendAction()));
    }

    [Fact]
    public void Engine_NullArguments_Throw()
    {
        var w = new TestWorld(CombatFixture.Load());

        Assert.Throws<ArgumentNullException>(() => new CombatEngine(null!, w.Dice, w.Checks, w.Effects, w.Text));
        Assert.Throws<ArgumentNullException>(() => new CombatEngine(w.Content, null!, w.Checks, w.Effects, w.Text));
        Assert.Throws<ArgumentNullException>(() => new CombatEngine(w.Content, w.Dice, null!, w.Effects, w.Text));
        Assert.Throws<ArgumentNullException>(() => new CombatEngine(w.Content, w.Dice, w.Checks, null!, w.Text));
        Assert.Throws<ArgumentNullException>(() => new CombatEngine(w.Content, w.Dice, w.Checks, w.Effects, null!));
        var engine = new CombatEngine(w.Content, w.Dice, w.Checks, w.Effects, w.Text);
        Assert.Throws<ArgumentNullException>(() => engine.Begin("brute", null!, new TurnLog()));
        Assert.Throws<ArgumentNullException>(() => engine.Begin("brute", w.State, null!));
    }

    private sealed record DanceAction : PlayerAction;
}
