using RpgEngine.Core.Characters;
using RpgEngine.Core.Combat;
using RpgEngine.Core.Commands;
using RpgEngine.Core.Content;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Engine;
using RpgEngine.Core.Events;
using RpgEngine.Core.Views;
using RpgEngine.Core.World;
using RpgEngine.Tests.Content;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Playthrough;

/// <summary>
/// Plays Act II, the Memory Warrens, through the shipped content with scripted dice. Each run starts
/// as if loaded from a save at the Drowned Tollbridge with Act I behind the hero, crosses into the
/// Warrens, recovers both Triad Seals (the Moon Seal from Prelate Vaelen in the north, the Sun Seal
/// from Commander Silas in the south), and unseals the gate onto the Stair of the Uncarved Skull,
/// where Act III begins. Every room has ambient lines, so each room description consumes one die.
/// <see cref="ActOneScenarioTests"/> plays the Act I runs these saves stand for, and
/// <see cref="ActThreeScenarioTests"/> plays on from the stair.
/// </summary>
public sealed class ActTwoScenarioTests
{
    private static readonly GameContent Content = ContentLoader.LoadFromDirectory(RealDataTests.DataDirectory);

    private static PlayerCharacter Create(string classId, string backgroundId, string[] skills, string[] expertise)
    {
        var cls = Content.Classes[classId];
        var scores = CharacterFactory.AssignByPriority(Content.Rules.StandardArray, cls);
        return new CharacterFactory(Content).Create("Tamsin", classId, backgroundId, scores, skills, expertise);
    }

    // The world as Act I leaves it at the far end of the tollbridge: the lantern in hand, the
    // tollbridge resolved, and the given flags, resonance, and total XP.
    private static GameSession AfterActOne(PlayerCharacter player, int xp, int resonance, IRandomSource dice, params string[] flags)
    {
        Progression.GrantXp(player, Content, xp, new TurnLog());
        player.Inventory.Add("barrowlight");
        var world = WorldState.CreateNew(Content);
        foreach (var flag in flags.Append("tollbridge_resolved").Append("barrow_heart_resolved"))
        {
            world.SetFlag(flag);
        }

        world.SetCounter("lantern_resonance", resonance);
        world.MarkVisited("drowned_tollbridge");
        return GameSession.Restore(Content, new GameState(player, world, "drowned_tollbridge"), dice);
    }

    private static IReadOnlyList<GameEvent> Do(GameSession session, string input) => session.Execute(CommandParser.Parse(input));

    private static IReadOnlyList<GameEvent> Say(GameSession session, string startOfChoice) =>
        session.ChooseDialogue(session.DialogueChoices().Single(c => c.Text.StartsWith(startOfChoice, StringComparison.Ordinal)).Index);

    private static RoomView Described(IReadOnlyList<GameEvent> events) => events.OfType<RoomDescribedEvent>().Single().Room;

    private static IReadOnlyList<GameEvent> Parley(GameSession session, string startOfLabel, RollMode expectedMode)
    {
        var option = session.Combat!.ParleyOptions().Single(o => o.Label.StartsWith(startOfLabel, StringComparison.Ordinal));
        Assert.Equal(expectedMode, option.Mode);
        return session.CombatAct(new ParleyAction(option.Index));
    }

    [Fact]
    public void MercifulRun_ReleasesLira_StandsDownSilas_AndDescendsWithWren()
    {
        var dice = new FixedRandomSource(
            0,                    // the Gate of the Twin Seals
            0, 15, 3,             // the Whispering Stacks | [Investigation DC 12] the index, with the Auditor's advantage
            0, 15,                // the Tallow-Spindle Loom | [Sleight of Hand DC 12] the tension-weight
            0,                    // the Nursery of Unkept Vows
            15, 1, 1, 15, 3,      // Vaelen: initiative, then [Persuasion DC 14] with advantage
            0, 0, 0,              // back through the loom and the stacks to the gate
            0, 15,                // the Scoured Cloister | [Investigation DC 12] the confessional grate
            0, 15,                // the Cistern of Lye and Ash | [Athletics DC 12] the sluice
            0,                    // the Bastion of the Unburdened
            15, 1, 1, 15, 3,      // Silas: initiative, then [Persuasion DC 14] with advantage
            0, 0, 0,              // back through the cistern and the cloister to the gate
            0);                   // the Stair of the Uncarved Skull
        var auditor = Create("liminal_auditor", "ash_deserter", ["persuasion", "insight", "stealth", "perception"], ["persuasion", "stealth"]);

        // The merciful Act I run: Wren trusted, taught the Rite, and followed; every warden spared; the beacon relit.
        var session = AfterActOne(auditor, xp: 675, resonance: 6, dice, "wren_trusts_player", "wren_taught_rite", "wren_joins_descent", "garin_relieved", "beacon_restored");
        Assert.Equal(3, session.State.Player.Level);

        var gate = Do(session, "cross");
        Assert.Equal("warren_crossroads", session.State.CurrentRoomId);
        Assert.Contains(gate.OfType<NarrationEvent>(), e => e.Text.StartsWith("Garin wades over", StringComparison.Ordinal));
        Assert.Contains(Described(gate).Paragraphs, p => p.StartsWith("Both sockets in the great bronze floor-gate stand empty", StringComparison.Ordinal));
        Assert.Equal(10, session.State.World.GetQuestStage("twin_seals"));
        Assert.DoesNotContain(session.ExplorationActions(), a => a.Label.StartsWith("Set both Triad Seals", StringComparison.Ordinal));
        Assert.Single(Do(session, "rest").OfType<RestedEvent>());
        Assert.True(session.State.World.HasFlag("rested_at_crossroads"));

        // North: the Archive of Unkept Vows. Every clue is gathered before Vaelen is faced.
        Do(session, "north");
        var audit = Do(session, "audit index");
        Assert.Equal(RollMode.Advantage, audit.OfType<CheckRolledEvent>().Single().Result.Roll.Mode);
        Do(session, "shine echo");
        Do(session, "talk to caelan");
        Say(session, "Leave him to his cylinders.");
        Do(session, "north");
        Do(session, "sabotage loom");
        Assert.Contains(session.State.Player.Inventory.Entries, e => e.ItemId == "silver_tuning_blade");
        Do(session, "shine echo");
        Do(session, "east");
        Assert.Equal(GameMode.Exploration, session.Mode);
        Do(session, "shine echo");
        Assert.All(
            new[] { "read_vaelen_ledger", "shone_stacks_echo", "slackened_choir_loom", "shone_foundry_echo", "shone_sanctum_echo" },
            flag => Assert.True(session.State.World.HasFlag(flag), flag));

        Do(session, "approach carillon");
        Assert.Equal(GameMode.Combat, session.Mode);
        var vaelen = Parley(session, "Sing the missing final line of Lira's lullaby", RollMode.Advantage);
        Assert.Contains(new CombatEndedEvent(CombatOutcome.Parleyed), vaelen);
        Assert.Contains(vaelen, e => e is LeveledUpEvent { Level: 4 });
        Assert.True(session.State.World.HasFlag("vaelen_reconciled"));
        Do(session, "take seal");
        Assert.Equal(20, session.State.World.GetQuestStage("twin_seals"));

        // South: the Iron Reliquary.
        Do(session, "west");
        Do(session, "south");
        var halfway = Described(Do(session, "south"));
        Assert.Contains(halfway.Paragraphs, p => p.StartsWith("In your pack, the Seal of the Waxing Moon hums", StringComparison.Ordinal));
        Do(session, "south");
        Do(session, "search grate");
        Do(session, "shine echo");
        Do(session, "south");
        Do(session, "quench cistern");
        Assert.True(session.State.World.HasFlag("quenched_lye_cistern"));
        Do(session, "shine echo");
        Do(session, "west");
        Do(session, "shine echo");

        Do(session, "approach furnace");
        Assert.Contains(session.Combat!.ParleyOptions(), o => o.Label == "Speak Sister Wren's remission and the Rite of Rest for the lost garrison");
        var silas = Parley(session, "Ask him to state his campaign objective", RollMode.Advantage);
        Assert.Contains(new CombatEndedEvent(CombatOutcome.Parleyed), silas);
        Assert.True(session.State.World.HasFlag("silas_relieved"));
        Do(session, "take seal");
        Assert.Equal(30, session.State.World.GetQuestStage("twin_seals"));

        // Back at the gate, both seals open the way down into the Cradle.
        Do(session, "east");
        Do(session, "north");
        Do(session, "north");
        var descent = Do(session, "unseal gate");

        Assert.Equal(GameMode.Exploration, session.Mode);
        Assert.Equal("threshold_vestibule", session.State.CurrentRoomId);
        Assert.Contains(Described(descent).Paragraphs, p => p.StartsWith("Beneath the parted bronze of the Twin-Seal Gate", StringComparison.Ordinal));
        Assert.True(session.State.World.HasFlag("unsealed_cradle_gate"));
        Assert.Equal(100, session.State.World.GetQuestStage("twin_seals"));
        Assert.Equal(10, session.State.World.GetQuestStage("cradle_of_ash_quest"));
        Assert.Equal(10, session.State.World.GetCounter("lantern_resonance"));
        Assert.Equal(0, dice.Remaining);
    }

    [Fact]
    public void SteelRun_CutsDownBothBosses_LearnsIronPenance_AndDescendsAlone()
    {
        var dice = new FixedRandomSource(
            0,                    // the Gate of the Twin Seals
            0, 0, 0,              // the stacks, the loom, the nursery
            20, 1, 1,             // initiative against Vaelen and the chorister
            20, 8, 8, 8, 8,       // a critical Ash Brand on Vaelen: (1d8 + 1d8) doubled, +3 = 35 against 22 HP
            0, 1,                 // the chorister picks its garrote and rolls a natural 1
            20, 8, 8,             // a critical strike on the chorister (2d8+3 = 19 against 9 HP)
            0,                    // Vaelen's loot roll: a vial of memory-tallow
            0, 0, 0,              // back through the loom and the stacks to the gate
            0, 0, 0,              // the cloister, the cistern, the bastion
            20, 1, 1,             // initiative against Silas and the halberdier
            20, 8, 8, 8, 8,       // a critical Ash Brand on Silas: 35 damage, more than his HP
            1,                    // the halberdier rolls a natural 1
            20, 8, 8,             // a critical strike on the halberdier: 19 damage, more than its HP
            0,                    // Silas's loot roll: an alkali salve
            0, 0, 0,              // back through the cistern and the cloister to the gate
            0);                   // the Stair of the Uncarved Skull
        var oathEater = Create("oath_eater", "moor_bailiff", ["athletics", "intimidation"], []);

        // The violent Act I run: Wren driven off, Halric and Garin destroyed, the beacon left dark.
        var session = AfterActOne(oathEater, xp: 255, resonance: -2, dice, "wren_fled", "knight_destroyed", "garin_destroyed");
        Assert.Equal(2, session.State.Player.Level);

        Do(session, "cross");
        Do(session, "north");
        Do(session, "north");
        Do(session, "east");
        Do(session, "approach carillon");
        Assert.DoesNotContain(session.Combat!.ParleyOptions(), o => o.Mode == RollMode.Advantage && o.Label.StartsWith("Sing", StringComparison.Ordinal));
        session.CombatAct(new CastAction("ash_brand", "prelate_vaelen#1"));
        session.CombatEnemyTurn();
        var first = session.CombatAct(new AttackAction("tethered_chorister#1"));

        // 255 + 150 + 45 XP crosses 300: the Oath-Eater learns Iron Penance mid-act.
        Assert.Contains(new CombatEndedEvent(CombatOutcome.Victory), first);
        var levelUp = Assert.Single(first.OfType<LeveledUpEvent>());
        Assert.Equal((3, "iron_penance"), (levelUp.Level, Assert.Single(levelUp.NewSpells).Id));
        Assert.True(session.State.World.HasFlag("vaelen_destroyed"));
        Do(session, "take seal");

        Do(session, "west");
        Do(session, "south");
        Do(session, "south");
        Do(session, "south");
        Do(session, "south");
        Do(session, "west");
        Do(session, "approach furnace");
        Assert.Contains(session.Combat!.SpellOptions(), o => o.Spell.Id == "iron_penance" && o.CanCast);
        session.CombatAct(new CastAction("ash_brand", "commander_silas#1"));
        session.CombatEnemyTurn();
        var second = session.CombatAct(new AttackAction("scoured_halberdier#1"));

        Assert.Contains(new CombatEndedEvent(CombatOutcome.Victory), second);
        Assert.True(session.State.World.HasFlag("silas_destroyed"));
        Do(session, "take seal");
        Do(session, "east");
        Do(session, "north");
        Do(session, "north");
        Do(session, "unseal gate");

        Assert.Equal(GameMode.Exploration, session.Mode);
        Assert.Equal("threshold_vestibule", session.State.CurrentRoomId);
        Assert.Equal(-4, session.State.World.GetCounter("lantern_resonance"));
        Assert.Equal(-35, session.State.World.GetReputation("hollow_choir") + session.State.World.GetReputation("ashen_order"));
        Assert.Equal(0, dice.Remaining);
    }
}
