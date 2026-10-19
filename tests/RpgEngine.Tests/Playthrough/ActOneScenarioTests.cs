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
/// Plays Act I through the shipped content with scripted dice: from the Lichgate, down the Weeping
/// Stair, through Nocturne and its two spoke locations, and across the Drowned Tollbridge, where the
/// adventure ends. Every room has ambient lines, so each room description consumes one die.
/// </summary>
public sealed class ActOneScenarioTests
{
    private static readonly GameContent Content = ContentLoader.LoadFromDirectory(RealDataTests.DataDirectory);

    private static PlayerCharacter Create(string classId, string backgroundId, string[] skills, string[] expertise)
    {
        var cls = Content.Classes[classId];
        var scores = CharacterFactory.AssignByPriority(Content.Rules.StandardArray, cls);
        return new CharacterFactory(Content).Create("Tamsin", classId, backgroundId, scores, skills, expertise);
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
    public void MercifulRun_FromTheLichgate_RelightsNocturne_AndCrossesTheTollbridge()
    {
        var dice = new FixedRandomSource(
            0, 15, 0, 0, 15, 1, 1, 15, 10,  // prologue: ambient | persuasion | ossuary | crypt | initiative | rite with advantage
            0, 0, 0, 0,                     // back to Wren and down again: lichgate, ossuary, crypt (on the way back down), crypt
            0,                              // the Weeping Stair
            15,                             // [History DC 11] the relief
            15, 3,                          // [Insight DC 11] Orin, with advantage
            0, 0,                           // Nocturne, the Echo Chapel
            15, 1, 1, 15, 3,                // Miriel: initiative, then [Persuasion DC 13] with advantage
            0, 0,                           // Nocturne, the Drowned Tollbridge
            15, 1, 1, 15, 3,                // Garin: initiative, then [Persuasion DC 13] with advantage
            0, 0);                          // Nocturne, the Drowned Tollbridge
        var auditor = Create("liminal_auditor", "ash_deserter", ["persuasion", "insight", "stealth", "perception"], ["persuasion", "stealth"]);
        var session = GameSession.NewGame(Content, auditor, dice);
        session.Begin();

        // Prologue: win Wren's trust, lay Ser Halric to rest, and bring the lantern back to her.
        Do(session, "talk to wren");
        Say(session, "I'm not here to rob the dead");
        Say(session, "I'll bring the light back.");
        Do(session, "down");
        Do(session, "press sun");
        Do(session, "press moon");
        Do(session, "press skull");
        Do(session, "east");
        Parley(session, "Speak the Rite of Rest", RollMode.Advantage);
        Do(session, "take lantern");
        Do(session, "west");
        Do(session, "up");
        Do(session, "talk to wren");
        Say(session, "Then I'll meet you below, Sister.");
        Do(session, "down");
        Do(session, "east");

        // The Weeping Stair.
        var stair = Do(session, "descend");
        Assert.Equal("weeping_stair", session.State.CurrentRoomId);
        Assert.Equal(GameMode.Exploration, session.Mode);
        Assert.Contains(Described(stair).Paragraphs, p => p.StartsWith("Fresh chalk-marks of the Ashen flame", StringComparison.Ordinal));
        Assert.Equal(10, session.State.World.GetQuestStage("nocturne_beacon"));

        Do(session, "study relief");
        Do(session, "shine echo");
        Assert.True(session.State.World.HasFlag("read_stair_relief"));
        Assert.True(session.State.World.HasFlag("saw_stair_echo"));

        Do(session, "talk to orin");
        var ledger = session.DialogueChoices().Single(c => c.CheckLabel == "Insight");
        Assert.Equal((11, RollMode.Advantage), (ledger.Dc, ledger.Mode));
        var steadied = Say(session, "Read the first line of your ledger");
        Assert.Contains(steadied.OfType<ItemGainedEvent>(), e => e.Item.Id == "toll_token");
        Say(session, "I'll see the beacon relit");

        // Nocturne: Wren has kept her word, and the brazier is a full sanctuary rest.
        var square = Described(Do(session, "down"));
        Assert.Contains(square.Paragraphs, p => p.StartsWith("Beneath a broken side-altar, Sister Wren has kindled", StringComparison.Ordinal));
        Assert.Contains(square.Paragraphs, p => p.StartsWith("The Nocturne Beacon at the center of the square stands dark", StringComparison.Ordinal));
        Assert.Equal(20, session.State.World.GetQuestStage("nocturne_beacon"));
        Assert.Single(Do(session, "rest").OfType<RestedEvent>());

        // The Echo Chapel: Miriel waits at the pulpit, so the echo can be shone before the fight.
        Do(session, "east");
        Assert.Equal(GameMode.Exploration, session.Mode);
        Do(session, "release jars");
        Do(session, "shine echo");
        Assert.True(session.State.World.HasFlag("shone_chapel_echo"));
        Do(session, "approach pulpit");
        Assert.Equal(GameMode.Combat, session.Mode);

        var miriel = Parley(session, "Show her that the forced chord", RollMode.Advantage);
        Assert.Contains(new CombatEndedEvent(CombatOutcome.Parleyed), miriel);
        Assert.Contains(miriel, e => e is LeveledUpEvent { Level: 3 });
        Assert.True(session.State.World.HasFlag("miriel_reconciled"));
        Do(session, "take tongue");
        Assert.Equal(30, session.State.World.GetQuestStage("nocturne_beacon"));

        // The Drowned Tollbridge: Garin holds the line until the player steps up to it.
        Do(session, "west");
        var bridge = Do(session, "west");
        Assert.Equal(GameMode.Exploration, session.Mode);
        Assert.DoesNotContain(bridge, e => e is CombatStartedEvent);
        Assert.DoesNotContain(session.ExplorationActions(), a => a.Label == "Cross the tollbridge into the deeper warrens");
        Do(session, "shine echo");
        Assert.True(session.State.World.HasFlag("shone_bridge_echo"));
        Do(session, "approach line");
        Assert.Equal(GameMode.Combat, session.Mode);
        Assert.Contains(session.Combat!.ParleyOptions(), o => o.Label.StartsWith("Present the Ferryman's Obol", StringComparison.Ordinal));

        var garin = Parley(session, "Tell him the flood has passed", RollMode.Advantage);
        Assert.Contains(new CombatEndedEvent(CombatOutcome.Parleyed), garin);
        Assert.True(session.State.World.HasFlag("garin_relieved"));
        Do(session, "take lens");
        Assert.Equal(40, session.State.World.GetQuestStage("nocturne_beacon"));

        // Back in Nocturne, the two pieces relight the beacon.
        Do(session, "east");
        Do(session, "relight beacon");
        Assert.True(session.State.World.HasFlag("beacon_restored"));
        Assert.Equal(100, session.State.World.GetQuestStage("nocturne_beacon"));
        Assert.False(session.State.Player.Inventory.Contains("beacon_lens"));

        // Crossing the tollbridge ends Act I.
        Do(session, "west");
        var ending = Do(session, "cross");

        Assert.Contains(ending.OfType<NarrationEvent>(), e => e.Text.StartsWith("Garin wades over and takes the other spoke", StringComparison.Ordinal));
        Assert.Equal(GameMode.Ended, session.Mode);
        Assert.IsType<EpilogueEvent>(ending[^1]);
        Assert.StartsWith("Behind you, the Nocturne Beacon chimes across the sunken cathedral", session.Epilogue);
        Assert.Contains("Tamsin", session.Epilogue);
        Assert.Equal(6, session.State.World.GetCounter("lantern_resonance"));
        Assert.Equal(3, session.State.Player.Level);
        Assert.Equal(0, dice.Remaining);
    }

    [Fact]
    public void SteelRun_CutsDownGarin_AndCrossesWithNocturneStillDark()
    {
        var dice = new FixedRandomSource(
            0, 15, 0, 0,                    // ambient | [Intimidation] Wren flees | ossuary | crypt
            20, 1, 1, 20, 8, 8, 1, 20, 8, 8, // Barrow Heart: initiative, critical on Halric, the thrall misses, critical on the thrall
            0, 0, 0,                        // the Weeping Stair, Nocturne, the Drowned Tollbridge
            20, 1, 1,                       // initiative against Garin and the lancer
            20, 8, 8,                       // critical on Garin (2d8+3 = 19 against 17 HP)
            1,                              // the lancer misses
            20, 8, 8,                       // critical on the lancer
            0);                             // Garin's loot roll: a healing potion
        var oathEater = Create("oath_eater", "moor_bailiff", ["athletics", "intimidation"], []);
        var session = GameSession.NewGame(Content, oathEater, dice);
        session.Begin();

        Do(session, "talk to wren");
        Say(session, "Step aside");
        Do(session, "down");
        Do(session, "press sun");
        Do(session, "press moon");
        Do(session, "press skull");
        Do(session, "east");
        session.CombatAct(new AttackAction("gravebound_knight#1"));
        session.CombatEnemyTurn();
        session.CombatAct(new AttackAction("barrow_thrall#1"));
        Do(session, "take the barrowlight");
        Do(session, "descend");
        Do(session, "down");
        Do(session, "west");
        Do(session, "approach line");

        session.CombatAct(new AttackAction("tollbridge_warden_garin#1"));
        session.CombatEnemyTurn();
        var victory = session.CombatAct(new AttackAction("drowned_lancer#1"));

        Assert.Contains(new CombatEndedEvent(CombatOutcome.Victory), victory);
        Assert.Contains(victory, e => e is XpGainedEvent { Amount: 130 });
        Assert.True(session.State.World.HasFlag("garin_destroyed"));
        Assert.True(session.State.World.HasFlag("tollbridge_resolved"));
        Assert.Contains(session.ExplorationActions(), a => a.Label == "Cross the tollbridge into the deeper warrens");

        Do(session, "take lens");
        var ending = Do(session, "cross");

        Assert.DoesNotContain(ending.OfType<NarrationEvent>(), e => e.Text.Contains("Garin wades over", StringComparison.Ordinal));
        Assert.Equal(GameMode.Ended, session.Mode);
        Assert.StartsWith("Behind you, Nocturne's beacon is still dark, and the Lantern Market has gone very quiet.", session.Epilogue);
        Assert.Equal(-2, session.State.World.GetCounter("lantern_resonance"));
        Assert.Equal(0, dice.Remaining);
    }

    [Theory]
    [InlineData(true, true, true, 3, "Behind you, the Nocturne Beacon chimes across the sunken cathedral")]
    [InlineData(true, true, true, 2, "The Nocturne Beacon rings out behind you")]
    [InlineData(true, true, false, 1, "The Nocturne Beacon rings out behind you")]
    [InlineData(true, true, true, 0, "The Nocturne Beacon burns again")]
    [InlineData(true, false, false, 1, "Behind you, the Nocturne Beacon still stands dark.")]
    [InlineData(false, false, true, 2, "Behind you, the Nocturne Beacon still stands dark.")]
    [InlineData(true, false, false, -1, "Behind you, Nocturne's beacon is still dark")]
    [InlineData(false, false, false, 0, "The floodgate grinds open")]
    public void Epilogue_ReflectsTheChapel_TheBeacon_Wren_AndTheLantern(bool chapelResolved, bool beaconRestored, bool wrenFollowed, int resonance, string expectedStart)
    {
        var world = new TestWorld(Content, Create("oath_eater", "moor_bailiff", ["athletics", "intimidation"], []));
        world.World.SetFlag("tollbridge_resolved");
        SetFlagIf(world.World, chapelResolved, "echo_chapel_resolved");
        SetFlagIf(world.World, beaconRestored, "beacon_restored");
        SetFlagIf(world.World, wrenFollowed, "wren_joins_descent");
        world.World.SetCounter("lantern_resonance", resonance);

        var epilogue = Content.Manifest.Epilogues.First(e => ConditionEvaluator.AllPass(e.Conditions, world.State));

        Assert.StartsWith(expectedStart, epilogue.Text);
    }

    private static void SetFlagIf(WorldState world, bool set, string flag)
    {
        if (set)
        {
            world.SetFlag(flag);
        }
    }
}
