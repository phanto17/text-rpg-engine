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
/// Plays Act III, the Cradle of the First Architect, through the shipped content with scripted dice.
/// Each run starts as if loaded from a save at the Gate of the Twin Seals with both Triad Seals in
/// hand, unseals the gate, descends through the Stair of the Uncarved Skull, the Vigil of the Last
/// Watch, and the Nave of the Sundered Bell, resolves the First Architect, and ends the adventure at
/// the Great Brazier. Every room has ambient lines, so each room description consumes one die.
/// <see cref="ActTwoScenarioTests"/> plays the Act II runs these saves stand for.
/// </summary>
public sealed class ActThreeScenarioTests
{
    private static readonly GameContent Content = ContentLoader.LoadFromDirectory(RealDataTests.DataDirectory);

    private static readonly string[] Blessings = ["wrens_ashen_censer", "vaelens_silver_chime", "silas_iron_standard"];

    private static PlayerCharacter Create(string classId, string backgroundId, string[] skills, string[] expertise)
    {
        var cls = Content.Classes[classId];
        var scores = CharacterFactory.AssignByPriority(Content.Rules.StandardArray, cls);
        return new CharacterFactory(Content).Create("Tamsin", classId, backgroundId, scores, skills, expertise);
    }

    // The world as Act II leaves it at the Gate of the Twin Seals: the lantern and both Triad Seals in
    // hand, both bosses resolved, and the given flags, resonance, total XP, and gold.
    private static GameSession AfterActTwo(PlayerCharacter player, int xp, int gold, int resonance, IRandomSource dice, params string[] flags)
    {
        Progression.GrantXp(player, Content, xp, new TurnLog());
        player.AddGold(gold);
        player.Inventory.Add("barrowlight");
        player.Inventory.Add("moon_triad_seal");
        player.Inventory.Add("sun_triad_seal");
        var world = WorldState.CreateNew(Content);
        foreach (var flag in flags.Concat(["barrow_heart_resolved", "tollbridge_resolved", "vaelen_resolved", "silas_resolved"]))
        {
            world.SetFlag(flag);
        }

        world.SetCounter("lantern_resonance", resonance);
        world.AdvanceQuest("twin_seals", 30);
        world.MarkVisited("warren_crossroads");
        return GameSession.Restore(Content, new GameState(player, world, "warren_crossroads"), dice);
    }

    private static IReadOnlyList<GameEvent> Do(GameSession session, string input) => session.Execute(CommandParser.Parse(input));

    private static IReadOnlyList<GameEvent> Say(GameSession session, string startOfChoice) =>
        session.ChooseDialogue(session.DialogueChoices().Single(c => c.Text.StartsWith(startOfChoice, StringComparison.Ordinal)).Index);

    private static RoomView Described(IReadOnlyList<GameEvent> events) => events.OfType<RoomDescribedEvent>().Single().Room;

    private static bool Offers(GameSession session, string startOfLabel) =>
        session.ExplorationActions().Any(a => a.Label.StartsWith(startOfLabel, StringComparison.Ordinal));

    [Fact]
    public void MercifulRun_GathersEveryBlessing_FitsTheSplinter_ReconcilesAurelius_AndCompletesTheTriad()
    {
        var dice = new FixedRandomSource(
            0,                    // the Stair of the Uncarved Skull
            15, 3,                // [History DC 13] the frieze, with the Cantor's advantage
            0,                    // the Vigil of the Last Watch
            0,                    // the Nave of the Sundered Bell
            15, 3,                // [Arcana DC 13] the bell, with the Cantor's advantage
            0,                    // the Cradle of the First Architect
            15, 3,                // [Investigation DC 13] the sarcophagus, with the Fever Dreamer's advantage
            15, 1, 1,             // initiative against Aurelius and the warden
            15, 3,                // [Religion DC 10] the combined witness, with advantage
            0);                   // looking around the Cradle once Aurelius kneels
        var cantor = Create("vesper_cantor", "fever_dreamer", ["arcana", "history"], []);

        // The merciful run: Wren followed, Vaelen and Silas spared, the beacon relit.
        var session = AfterActTwo(cantor, xp: 1000, gold: 75, resonance: 8, dice, "wren_joins_descent", "vaelen_reconciled", "silas_relieved", "beacon_restored");
        Assert.Equal(4, session.State.Player.Level);

        var stair = Do(session, "unseal gate");
        Assert.Equal("threshold_vestibule", session.State.CurrentRoomId);
        Assert.Contains(Described(stair).Paragraphs, p => p.StartsWith("Beneath the parted bronze of the Twin-Seal Gate", StringComparison.Ordinal));
        Assert.Equal(10, session.State.World.GetQuestStage("cradle_of_ash_quest"));
        var frieze = Do(session, "study frieze");
        Assert.Equal(RollMode.Advantage, frieze.OfType<CheckRolledEvent>().Single().Result.Roll.Mode);
        Do(session, "shine echo");
        Assert.All(new[] { "read_architect_frieze", "shone_vestibule_echo" }, flag => Assert.True(session.State.World.HasFlag(flag), flag));

        // The Vigil of the Last Watch: every spared ally has left a relic.
        var landing = Described(Do(session, "down"));
        Assert.Contains(landing.Paragraphs, p => p.StartsWith("Sister Wren stands near the southern parapet", StringComparison.Ordinal));
        Do(session, "talk to wren");
        Say(session, "Receive Sister Wren's blessing");
        Assert.Equal(GameMode.Exploration, session.Mode);
        Do(session, "listen chime");
        Do(session, "read tablet");
        Assert.All(Blessings, item => Assert.True(session.State.Player.Inventory.Contains(item), item));
        Assert.All(new[] { "wren_cradle_blessing", "carries_vaelen_testimony", "carries_silas_testimony" }, flag => Assert.True(session.State.World.HasFlag(flag), flag));
        Do(session, "tithe bowl");
        Assert.True(session.State.Player.Inventory.Contains("tallow_draught"));

        // Oakhaven's crates: the last of the surface coin buys the Mantle of the Balanced Triad.
        Do(session, "talk to oakhaven");
        Say(session, "Barter for supplies and endgame armor.");
        Assert.Equal(GameMode.Shop, session.Mode);
        var mantle = session.ShopOffers().Single(o => o.Item.Id == "triad_ward_mantle");
        Assert.Equal((80, (int?)1, true), (mantle.Price, mantle.Remaining, mantle.CanAfford));
        Assert.False(session.ShopOffers().Single(o => o.Item.Id == "architect_scale_mail").CanAfford);
        Assert.Contains(new GoldSpentEvent(80, 5), session.ShopBuy("triad_ward_mantle"));
        Assert.DoesNotContain(session.ShopOffers(), o => o.Item.Id == "triad_ward_mantle" && o.Remaining > 0);
        session.ShopLeave();
        Assert.Equal(GameMode.Exploration, session.Mode);
        Assert.True(session.State.Player.Inventory.Contains("triad_ward_mantle"));

        // The Nave of the Sundered Bell: the Fever Dreamer's splinter fits the bell's lip.
        Do(session, "south");
        Assert.Equal(20, session.State.World.GetQuestStage("cradle_of_ash_quest"));
        Do(session, "study bell");
        Do(session, "fit bell");
        Do(session, "shine echo");
        Assert.All(new[] { "diagnosed_sundered_bell", "fitted_bell_splinter", "shone_nave_echo" }, flag => Assert.True(session.State.World.HasFlag(flag), flag));

        // The Cradle: the fight waits until the hero steps onto the dais.
        var cradle = Described(Do(session, "south"));
        Assert.Contains(cradle.Paragraphs, p => p.StartsWith("Before the octagonal dais, the First Architect stands motionless", StringComparison.Ordinal));
        Assert.Equal(GameMode.Exploration, session.Mode);
        Assert.False(Offers(session, "The Path of"));
        Do(session, "read sarcophagus");
        Do(session, "shine echo");
        Assert.All(new[] { "read_caelia_inscription", "shone_cradle_echo" }, flag => Assert.True(session.State.World.HasFlag(flag), flag));

        Do(session, "approach brazier");
        Assert.Equal(GameMode.Combat, session.Mode);
        var options = session.Combat!.ParleyOptions();
        // The main parley, the Calling's, the Burden's, and the allies' combined witness, all with advantage.
        string[] expected = ["Speak Caelia's scratched words", "Sing the Third Harmonic", "Hold up the bronze bell-splinter", "Deliver the combined witness"];
        Assert.Equal(expected.Length, options.Count);
        Assert.All(expected.Zip(options), x => Assert.StartsWith(x.First, x.Second.Label, StringComparison.Ordinal));
        Assert.All(options, o => Assert.Equal(RollMode.Advantage, o.Mode));
        var witness = options.Single(o => o.Label.StartsWith("Deliver the combined witness", StringComparison.Ordinal));
        var parley = session.CombatAct(new ParleyAction(witness.Index));

        Assert.Contains(new CombatEndedEvent(CombatOutcome.Parleyed), parley);
        Assert.True(session.State.World.HasFlag("architect_reconciled"));
        Assert.Equal(30, session.State.World.GetQuestStage("cradle_of_ash_quest"));
        Assert.Contains(Described(Do(session, "look")).Paragraphs, p => p.StartsWith("The First Architect kneels beside Caelia's quiet sarcophagus", StringComparison.Ordinal));

        Do(session, "talk to aurelius");
        Assert.True(session.State.World.HasFlag("spoke_to_reconciled_architect"));
        Assert.True(Offers(session, "The Path of the Complete Triad"));

        var ending = Do(session, "complete brazier");

        Assert.Equal(GameMode.Ended, session.Mode);
        Assert.IsType<EpilogueEvent>(ending[^1]);
        Assert.StartsWith("The Great Vesper Bell rings once above the Cradle", session.Epilogue);
        Assert.Contains("It was Tamsin, they say", session.Epilogue);
        Assert.True(session.State.World.HasFlag("ending_triad_passage"));
        Assert.Equal(100, session.State.World.GetQuestStage("cradle_of_ash_quest"));
        Assert.Equal(11, session.State.World.GetCounter("lantern_resonance"));
        Assert.Equal(1485, session.State.Player.Xp);
        Assert.Equal(0, dice.Remaining);
    }

    [Theory]
    [InlineData("relight brazier", "ending_sun_stasis", "White fire climbs the roots of the mountain")]
    [InlineData("tune brazier", "ending_moon_vigil", "Silver-blue light runs up through the roots of the mountain")]
    public void ArmedRun_CutsDownAurelius_WithTheCapstoneAndVaelensChime_ThenChoosesAtTheBrazier(string choice, string endingFlag, string expectedStart)
    {
        var dice = new FixedRandomSource(
            0, 0, 0, 0,           // the stair, the vigil, the nave, and the cradle
            20, 1, 1,             // initiative against Aurelius and the warden
            8, 8,                 // Unbroken Vigil heals 2d8+2 and makes the Oath-Eater vigilant
            0, 1,                 // Aurelius picks his pendulum and rolls a natural 1
            1,                    // the warden rolls a natural 1
            6, 6, 6,              // Vaelen's chime: 3d6 force, doubled by his weakness, is 36 against 33 HP
            1,                    // the warden rolls a natural 1 again
            20, 1, 8, 8,          // vigilant advantage: a critical strike on the warden (2d8+3 = 19 against 9 HP)
            0,                    // Aurelius's loot roll: an alkali salve
            0);                   // looking around the Cradle after the fight
        var oathEater = Create("oath_eater", "moor_bailiff", ["athletics", "intimidation"], []);

        // Every ally spared on the way down, but this hero brings steel to the dais.
        var session = AfterActTwo(oathEater, xp: 1000, gold: 0, resonance: 8, dice, "wren_joins_descent", "vaelen_reconciled", "silas_relieved", "beacon_restored");
        Assert.Equal((4, 36), (session.State.Player.Level, session.State.Player.MaxHp));
        session.State.Player.TakeDamage(20);

        Do(session, "unseal gate");
        Do(session, "down");
        Do(session, "talk to wren");
        Say(session, "Receive Sister Wren's blessing");
        Do(session, "listen chime");
        Do(session, "read tablet");
        Do(session, "south");
        Do(session, "south");
        Do(session, "approach brazier");
        Assert.Contains(session.Combat!.SpellOptions(), o => o.Spell.Id == "unbroken_vigil" && o.CanCast);

        var vigil = session.CombatAct(new CastAction("unbroken_vigil", null));
        Assert.Contains(vigil, e => e is HealedEvent { IsPlayer: true, Amount: 18, Hp: 34 });
        Assert.Contains(new StatusAppliedEvent("Tamsin", "Vigilant"), vigil);
        session.CombatEnemyTurn();
        session.CombatEnemyTurn();
        var chime = session.CombatAct(new UseItemAction("vaelens_silver_chime", "first_architect#1"));
        Assert.Contains(chime, e => e is CombatantDefeatedEvent { Name: "The First Architect, Aurelius" });
        Assert.False(session.State.Player.Inventory.Contains("vaelens_silver_chime"));
        session.CombatEnemyTurn();
        var last = session.CombatAct(new AttackAction("calcified_warden#1"));

        Assert.Equal(RollMode.Advantage, last.OfType<AttackRolledEvent>().Single().Roll.Mode);
        Assert.Contains(new CombatEndedEvent(CombatOutcome.Victory), last);
        Assert.All(new[] { "architect_destroyed", "architect_resolved" }, flag => Assert.True(session.State.World.HasFlag(flag), flag));
        Assert.False(session.State.World.HasFlag("architect_reconciled"));
        Assert.Equal(7, session.State.World.GetCounter("lantern_resonance"));
        Assert.Contains(Described(Do(session, "look")).Paragraphs, p => p.StartsWith("The shattered bronze astrolabes", StringComparison.Ordinal));
        Assert.All(new[] { "The Path of the Sun", "The Path of the Moon", "The Path of the Complete Triad" }, label => Assert.True(Offers(session, label), label));

        Do(session, choice);

        Assert.Equal(GameMode.Ended, session.Mode);
        Assert.True(session.State.World.HasFlag(endingFlag));
        Assert.StartsWith(expectedStart, session.Epilogue);
        Assert.Equal(100, session.State.World.GetQuestStage("cradle_of_ash_quest"));
        Assert.Equal(0, dice.Remaining);
    }

    [Theory]
    [InlineData("ending_triad_passage", true, true, true, true, true, 9, "The Great Vesper Bell rings once above the Cradle")]
    [InlineData("ending_triad_passage", true, false, true, true, true, 9, "\"Last the face all faces yield,\" you say")]
    [InlineData("ending_triad_passage", false, true, true, true, true, 9, "The Rite of True Passage was meant to be spoken by the one who broke it.")]
    [InlineData("ending_sun_stasis", true, false, false, false, false, -3, "White fire climbs the roots of the mountain")]
    [InlineData("ending_sun_stasis", false, false, false, true, false, -1, "White fire climbs the roots of the mountain")]
    [InlineData("ending_sun_stasis", false, false, false, false, true, 3, "White fire climbs the roots of the mountain")]
    [InlineData("ending_sun_stasis", false, false, false, false, true, 2, "You lock the Sun Seal into the grate")]
    [InlineData("ending_moon_vigil", false, false, true, false, false, -2, "Silver-blue light runs up through the roots of the mountain")]
    [InlineData("ending_moon_vigil", false, false, false, false, true, -2, "Silver-blue light runs up through the roots of the mountain")]
    [InlineData("ending_moon_vigil", true, true, false, true, false, 4, "You set the Moon Seal into the resonator")]
    [InlineData("", true, true, true, true, true, 9, "The Great Brazier falls quiet behind you")]
    public void Epilogue_FollowsTheChoiceAtTheBrazier_AndTheJourneyThere(
        string ending, bool architectSpared, bool wrenFollowed, bool vaelenSpared, bool silasSpared, bool beaconRelit, int resonance, string expectedStart)
    {
        var world = new TestWorld(Content, Create("oath_eater", "moor_bailiff", ["athletics", "intimidation"], []));
        string[] flags =
        [
            ending,
            architectSpared ? "architect_reconciled" : "architect_destroyed",
            wrenFollowed ? "wren_joins_descent" : "",
            vaelenSpared ? "vaelen_reconciled" : "vaelen_destroyed",
            silasSpared ? "silas_relieved" : "silas_destroyed",
            beaconRelit ? "beacon_restored" : "",
        ];
        foreach (var flag in flags.Where(f => f.Length > 0))
        {
            world.World.SetFlag(flag);
        }

        world.World.SetCounter("lantern_resonance", resonance);

        var epilogue = Content.Manifest.Epilogues.First(e => ConditionEvaluator.AllPass(e.Conditions, world.State));

        Assert.StartsWith(expectedStart, epilogue.Text, StringComparison.Ordinal);
    }
}
