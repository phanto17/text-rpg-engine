using RpgEngine.Core.Commands;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Events;
using RpgEngine.Core.Exploration;
using RpgEngine.Core.Views;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Exploration;

public sealed class ExplorationServiceTests
{
    [Fact]
    public void EnterRoom_FirstVisit_DescribesFullyAndRunsFirstEnterEffects()
    {
        var game = new ExplorationHarness();

        var log = game.Enter("start");

        var described = Assert.IsType<RoomDescribedEvent>(log.Events[0]);
        Assert.True(described.Entered);
        Assert.Equal("Gate", described.Room.Name);
        Assert.Equal("A gate in the rain.", described.Room.Paragraphs[0]);
        Assert.IsType<QuestUpdatedEvent>(log.Events[1]);
        Assert.Equal(1, game.World.World.GetCounter("visits"));
        Assert.True(game.World.World.HasVisited("start"));
        Assert.Empty(ExplorationHarness.Transitions(log));
    }

    [Fact]
    public void EnterRoom_Revisit_UsesShortTextAndSkipsFirstEnterEffects()
    {
        var game = new ExplorationHarness();
        game.Enter("start");

        var log = game.Enter("start");

        Assert.Equal("The gate again.", Assert.IsType<RoomDescribedEvent>(Assert.Single(log.Events)).Room.Paragraphs[0]);
        Assert.Equal(2, game.World.World.GetCounter("visits"));
    }

    [Fact]
    public void EnterRoom_ArmedEncounter_RequestsCombat_UntilResolved()
    {
        var game = new ExplorationHarness(dice: [1, 1]);

        Assert.Equal(new StartCombatTransition("rats"), Assert.Single(ExplorationHarness.Transitions(game.Enter("hall"))));

        game.World.World.SetFlag("rats_dead");
        Assert.Empty(ExplorationHarness.Transitions(game.Enter("hall")));
    }

    [Fact]
    public void Describe_PicksAmbientLineWithDice()
    {
        var game = new ExplorationHarness(dice: [1]);
        game.World.State.CurrentRoomId = "hall";

        Assert.Equal("Wind.", game.Exploration.Describe(game.World.State, full: true).Ambient);
    }

    [Fact]
    public void Describe_ShowsVariantsBlockedExitsAndSecrets()
    {
        var game = new ExplorationHarness();
        var before = game.Exploration.Describe(game.World.State, full: true);

        Assert.Single(before.Paragraphs);
        Assert.Null(before.Ambient);
        Assert.Equal(new[] { Direction.North, Direction.East }, before.Exits.Select(e => e.Direction));
        Assert.True(before.Exits[0].IsBlocked);
        Assert.Equal("A dark hall.", before.Exits[0].Description);
        Assert.Null(before.Exits[1].KnownDestination);
        Assert.Equal(new[] { "bronze door", "loose stone" }, before.Features);
        Assert.Equal(new[] { "Potion of Healing", "Dagger" }, before.Items);
        Assert.Equal(new[] { "Old Sage" }, before.Npcs);

        game.World.World.SetFlag("door_open");
        game.World.World.SetFlag("found_trapdoor");
        game.World.World.MarkVisited("hall");
        var after = game.Exploration.Describe(game.World.State, full: true);

        Assert.Equal("The door stands open.", after.Paragraphs[1]);
        Assert.Equal(new[] { Direction.North, Direction.Down, Direction.East }, after.Exits.Select(e => e.Direction));
        Assert.False(after.Exits[0].IsBlocked);
        Assert.Equal("Hall", after.Exits[2].KnownDestination);
        Assert.Contains("statue", after.Features);
    }

    [Fact]
    public void GetActions_ListsTalkInteractTakeExamineAndMoves()
    {
        var game = new ExplorationHarness();
        game.World.World.MarkVisited("hall");

        var labels = game.Exploration.GetActions(game.World.State).Select(a => a.Label).ToList();

        Assert.Equal(
            new[]
            {
                "Talk to Old Sage",
                "Read the inscription", "Press the sun sigil", "Press the moon sigil", "Use key",
                "Search loose stone",
                "Take Potion of Healing", "Take Dagger",
                "Examine bronze door", "Examine loose stone",
                "Go north", "Go east to Hall",
            },
            labels);
    }

    [Fact]
    public void MenuActions_ExecuteExactly()
    {
        var game = new ExplorationHarness();
        var actions = game.Exploration.GetActions(game.World.State);

        game.Run(actions.Single(a => a.Label == "Press the sun sigil").Command);
        var log = game.Run(actions.Single(a => a.Label == "Press the moon sigil").Command);

        Assert.Contains("The door opens.", ExplorationHarness.Texts(log));
        Assert.True(game.World.World.HasFlag("door_open"));
        Assert.Equal(MenuActionKind.Move, actions[^1].Kind);
    }

    [Fact]
    public void Look_RedescribesRoomFully()
    {
        var game = new ExplorationHarness();
        game.Enter("start");

        var described = Assert.IsType<RoomDescribedEvent>(Assert.Single(game.Run("look").Events));

        Assert.False(described.Entered);
        Assert.Equal("A gate in the rain.", described.Room.Paragraphs[0]);
    }

    [Fact]
    public void Go_OpenExit_RequestsMove()
    {
        var game = new ExplorationHarness();

        Assert.Equal(new MoveToTransition("hall"), Assert.Single(ExplorationHarness.Transitions(game.Run("e"))));
    }

    [Theory]
    [InlineData("n", "The door is sealed.")]
    [InlineData("w", "You can't go that way.")]
    [InlineData("down", "You can't go that way.")]
    [InlineData("go", "Go where?")]
    [InlineData("go home", "You can't go 'home'.")]
    public void Go_Blocked_ExplainsWhy(string input, string expected)
    {
        var game = new ExplorationHarness();

        var log = game.Run(input);

        Assert.Equal(expected, Assert.Single(ExplorationHarness.Texts(log)));
        Assert.Empty(ExplorationHarness.Transitions(log));
    }

    [Fact]
    public void Inspect_Feature_RunsInspectInteractionOnce()
    {
        var game = new ExplorationHarness();

        Assert.Equal(new[] { "A loose stone.", "Scratches mark it." }, ExplorationHarness.Texts(game.Run("examine stone")));
        Assert.Equal(new[] { "A loose stone." }, ExplorationHarness.Texts(game.Run("x loose stone")));
    }

    [Fact]
    public void Inspect_ByObjectId()
    {
        var game = new ExplorationHarness();

        Assert.Equal("Three sigils.", Assert.Single(ExplorationHarness.Texts(game.Run(new ParsedCommand(CommandVerb.Inspect, "examine", ObjectId: "door")))));
        Assert.Equal("You don't see that here.", ExplorationHarness.SystemText(game.Run(new ParsedCommand(CommandVerb.Inspect, "examine", ObjectId: "statue"))));
    }

    [Fact]
    public void Inspect_ItemsShowStats()
    {
        var game = new ExplorationHarness();
        game.World.Player.Inventory.Add("mail");
        game.World.Player.Inventory.Add("sword");

        Assert.Equal(new[] { "Short and sharp.", "Weapon: 1d4 piercing damage, finesse." }, ExplorationHarness.Texts(game.Run("examine dagger")));
        Assert.Equal(new[] { "Heavy rings.", "Armor: AC 16." }, ExplorationHarness.Texts(game.Run("examine chain mail")));
        Assert.Equal(new[] { "Steel.", "Weapon: 1d8 slashing damage." }, ExplorationHarness.Texts(game.Run("examine blade")));
        Assert.Equal(new[] { "Red and warm." }, ExplorationHarness.Texts(game.Run("examine potion")));
    }

    [Fact]
    public void Inspect_ArmorDexCaps()
    {
        var content = new TestContent().With("Items/extra.json", """
            [ { "id": "leather", "name": "Leather", "type": "armor", "description": "Soft.", "armor": { "baseAc": 11 } },
              { "id": "scale", "name": "Scale", "type": "armor", "description": "Scaly.", "armor": { "baseAc": 14, "maxDexBonus": 2 } },
              { "id": "bow", "name": "Bow", "type": "weapon", "description": "Yew.", "weapon": { "damage": "1d6", "damageType": "piercing", "ranged": true } } ]
            """).Load();
        var game = new ExplorationHarness(content);
        game.World.Player.Inventory.Add("leather");
        game.World.Player.Inventory.Add("scale");
        game.World.Player.Inventory.Add("bow");

        Assert.Equal("Armor: AC 11 + DEX.", ExplorationHarness.SystemText(game.Run("examine leather")));
        Assert.Equal("Armor: AC 14 + DEX (max 2).", ExplorationHarness.SystemText(game.Run("examine scale")));
        Assert.Equal("Weapon: 1d6 piercing damage, ranged.", ExplorationHarness.SystemText(game.Run("examine bow")));
    }

    [Theory]
    [InlineData("examine sage", "Ancient eyes.")]
    [InlineData("examine ghost", "You see no ghost here.")]
    [InlineData("examine", "Examine what?")]
    public void Inspect_NpcsAndMissingThings(string input, string expected)
    {
        var game = new ExplorationHarness();

        Assert.Equal(expected, Assert.Single(ExplorationHarness.Texts(game.Run(input))));
    }

    [Fact]
    public void Inspect_Around_DescribesRoom()
    {
        var game = new ExplorationHarness();

        Assert.IsType<RoomDescribedEvent>(Assert.Single(game.Run("look around").Events));
    }

    [Fact]
    public void Inspect_Ambiguous_AsksWhich()
    {
        var content = new TestContent().With("Rooms/rooms.json", """
            [ { "id": "start", "name": "S", "description": "D", "features": [
                { "id": "left", "name": "left door", "description": "L" }, { "id": "right", "name": "right door", "description": "R" } ] } ]
            """).Load();
        var game = new ExplorationHarness(content);

        Assert.Equal("Which do you mean: left door or right door?", ExplorationHarness.SystemText(game.Run("examine door")));
    }

    [Fact]
    public void Take_RoomItem_MovesItToInventory()
    {
        var game = new ExplorationHarness();

        var log = game.Run("take potion");

        Assert.Equal("potion", Assert.IsType<ItemGainedEvent>(Assert.Single(log.Events)).Item.Id);
        Assert.True(game.World.Player.Inventory.Contains("potion"));
        Assert.Equal(new[] { "dagger" }, game.World.World.ItemsIn("start"));
    }

    [Fact]
    public void Take_ByObjectId()
    {
        var game = new ExplorationHarness();

        game.Run(new ParsedCommand(CommandVerb.Take, "take", ObjectId: "dagger"));

        Assert.True(game.World.Player.Inventory.Contains("dagger"));
    }

    [Fact]
    public void Take_FallsBackToTakeInteraction()
    {
        var game = new ExplorationHarness();
        game.World.World.SetFlag("found_trapdoor");

        var log = game.Run("take statue");

        Assert.Contains("The halo comes loose.", ExplorationHarness.Texts(log));
        Assert.True(game.World.Player.Inventory.Contains("relic"));
    }

    [Theory]
    [InlineData("take door", "You can't take the bronze door.")]
    [InlineData("take ghost", "You see no ghost here.")]
    [InlineData("take", "Take what?")]
    public void Take_Refusals(string input, string expected)
    {
        var game = new ExplorationHarness();

        Assert.Equal(expected, ExplorationHarness.SystemText(game.Run(input)));
    }

    private const string BluePotion = """[ { "id": "blue_potion", "name": "Blue Potion", "aliases": ["potion"], "type": "misc", "description": "Blue." } ]""";

    [Fact]
    public void Take_AmbiguousItems_AsksWhich()
    {
        var content = new TestContent()
            .With("Items/extra.json", BluePotion)
            .With("Rooms/rooms.json", """[ { "id": "start", "name": "S", "description": "D", "items": ["potion", "blue_potion"] } ]""")
            .Load();
        var game = new ExplorationHarness(content);

        Assert.Equal("Which do you mean: Potion of Healing or Blue Potion?", ExplorationHarness.SystemText(game.Run("take potion")));
    }

    [Fact]
    public void Drop_AmbiguousItems_AsksWhich()
    {
        var game = new ExplorationHarness(new TestContent().With("Items/extra.json", BluePotion).Load());
        game.World.Player.Inventory.Add("potion");
        game.World.Player.Inventory.Add("blue_potion");

        Assert.Equal("Which do you mean: Potion of Healing or Blue Potion?", ExplorationHarness.SystemText(game.Run("drop potion")));
    }

    [Fact]
    public void Drop_PutsItemInRoomAndUnequips()
    {
        var game = new ExplorationHarness();
        game.World.Player.Inventory.Add("sword");
        game.World.Player.EquippedWeaponId = "sword";

        Assert.Equal("You drop the Longsword.", ExplorationHarness.SystemText(game.Run("drop sword")));

        Assert.Null(game.World.Player.EquippedWeaponId);
        Assert.Contains("sword", game.World.World.ItemsIn("start"));
    }

    [Fact]
    public void Drop_ArmorAndPartialStacks()
    {
        var game = new ExplorationHarness();
        game.World.Player.Inventory.Add("mail");
        game.World.Player.EquippedArmorId = "mail";
        game.World.Player.Inventory.Add("potion", 2);

        game.Run("drop mail");
        game.Run("drop potion");

        Assert.Null(game.World.Player.EquippedArmorId);
        Assert.Equal(1, game.World.Player.Inventory.CountOf("potion"));
    }

    [Theory]
    [InlineData("drop relic", "You'd better hold on to the Relic.")]
    [InlineData("drop ghost", "You don't have any ghost.")]
    [InlineData("drop", "Drop what?")]
    public void Drop_Refusals(string input, string expected)
    {
        var game = new ExplorationHarness();
        game.World.Player.Inventory.Add("relic");

        Assert.Equal(expected, ExplorationHarness.SystemText(game.Run(input)));
    }

    [Fact]
    public void Use_Consumable_AppliesEffectsAndConsumes()
    {
        var game = new ExplorationHarness(dice: [2, 2]);
        game.World.Player.Inventory.Add("potion");
        game.World.Player.TakeDamage(6);

        var log = game.Run("use potion");

        Assert.Contains("You use the Potion of Healing.", ExplorationHarness.Texts(log));
        Assert.Equal(6, Assert.Single(log.Events.OfType<HealedEvent>()).Amount);
        Assert.False(game.World.Player.Inventory.Contains("potion"));
    }

    [Fact]
    public void Use_ItemOnFeature_RunsUseInteraction()
    {
        var game = new ExplorationHarness();
        game.World.Player.Inventory.Add("key");

        Assert.Contains("The key turns.", ExplorationHarness.Texts(game.Run("use iron key on door")));
        Assert.True(game.World.World.HasFlag("door_open"));
    }

    [Theory]
    [InlineData("use key on stone", "Nothing happens.")]
    [InlineData("use key on ghost", "You see no ghost here.")]
    [InlineData("use dagger", "You can't use the Dagger on its own.")]
    [InlineData("use rope", "You don't have any rope.")]
    [InlineData("use", "Use what?")]
    public void Use_Refusals(string input, string expected)
    {
        var game = new ExplorationHarness();
        game.World.Player.Inventory.Add("key");
        game.World.Player.Inventory.Add("dagger");

        Assert.Equal(expected, ExplorationHarness.SystemText(game.Run(input)));
    }

    [Fact]
    public void Use_AmbiguousItemOrFeature_AsksWhich()
    {
        var content = new TestContent()
            .With("Items/extra.json", BluePotion)
            .With("Rooms/rooms.json", """
                [ { "id": "start", "name": "S", "description": "D", "features": [
                    { "id": "left", "name": "left door", "description": "L" }, { "id": "right", "name": "right door", "description": "R" } ] } ]
                """)
            .Load();
        var game = new ExplorationHarness(content);
        game.World.Player.Inventory.Add("potion");
        game.World.Player.Inventory.Add("blue_potion");
        game.World.Player.Inventory.Add("key");

        Assert.Equal("Which do you mean: Potion of Healing or Blue Potion?", ExplorationHarness.SystemText(game.Run("use potion")));
        Assert.Equal("Which do you mean: left door or right door?", ExplorationHarness.SystemText(game.Run("use key on door")));
    }

    [Fact]
    public void Use_FeatureVerbWithoutItem_FallsBackToInteraction()
    {
        var content = new TestContent().With("Rooms/rooms.json", """
            [ { "id": "start", "name": "S", "description": "D", "features": [
                { "id": "lever", "name": "lever", "description": "Rusty.", "interactions": [ { "verb": "use", "text": "Clunk." } ] } ] } ]
            """).Load();
        var game = new ExplorationHarness(content);

        Assert.Equal(new[] { "Clunk." }, ExplorationHarness.Texts(game.Run("use lever")));
    }

    [Theory]
    [InlineData("equip sword", "You ready the Longsword.")]
    [InlineData("wear mail", "You don the Chain Mail.")]
    [InlineData("equip potion", "You can't equip the Potion of Healing.")]
    [InlineData("equip", "Equip what?")]
    [InlineData("equip quarter", "You ready the Quarterstaff.")]
    public void Equip_ChangesGear(string input, string expected)
    {
        var game = new ExplorationHarness();
        foreach (var item in new[] { "sword", "staff", "mail", "potion" })
        {
            game.World.Player.Inventory.Add(item);
        }

        Assert.Equal(expected, ExplorationHarness.SystemText(game.Run(input)));
    }

    [Fact]
    public void Equip_SetsEquippedIds()
    {
        var game = new ExplorationHarness();
        game.World.Player.Inventory.Add("sword");
        game.World.Player.Inventory.Add("mail");

        game.Run("equip sword");
        game.Run("equip mail");

        Assert.Equal(("sword", "mail"), (game.World.Player.EquippedWeaponId, game.World.Player.EquippedArmorId));
    }

    [Theory]
    [InlineData("talk to sage")]
    [InlineData("talk")]
    public void Talk_StartsDialogue(string input)
    {
        var game = new ExplorationHarness();

        Assert.Equal(new StartDialogueTransition("sage"), Assert.Single(ExplorationHarness.Transitions(game.Run(input))));
    }

    [Fact]
    public void Talk_ByObjectId()
    {
        var game = new ExplorationHarness();

        Assert.Single(ExplorationHarness.Transitions(game.Run(new ParsedCommand(CommandVerb.Talk, "talk", ObjectId: "sage"))));
    }

    [Fact]
    public void Talk_NoOneOrUnknownOrAmbiguous()
    {
        var game = new ExplorationHarness();
        Assert.Equal("You see no ghost here.", ExplorationHarness.SystemText(game.Run("talk to ghost")));

        game.World.State.CurrentRoomId = "cellar";
        Assert.Equal("There is no one here to talk to.", ExplorationHarness.SystemText(game.Run("talk")));

        var content = new TestContent()
            .With("Rooms/rooms.json", """
                [ { "id": "start", "name": "S", "description": "D", "npcs": [
                    { "id": "a", "name": "Guard Ann", "aliases": ["guard"], "description": "A", "dialogue": "d" },
                    { "id": "b", "name": "Guard Bo", "aliases": ["guard"], "description": "B", "dialogue": "d" } ] } ]
                """)
            .With("Dialogues/d.json", """[ { "id": "d", "speaker": "G", "entries": [ { "node": "a" } ], "nodes": [ { "id": "a", "text": "Halt." } ] } ]""")
            .Load();
        var crowded = new ExplorationHarness(content);

        Assert.Equal("Talk to whom: Guard Ann or Guard Bo?", ExplorationHarness.SystemText(crowded.Run("talk to guard")));
    }

    [Fact]
    public void Interact_SequencePuzzle_RightOrderOpensDoor()
    {
        var game = new ExplorationHarness();

        game.Run("press sun");
        var log = game.Run("press the moon sigil");

        Assert.Equal(new[] { "The door opens." }, ExplorationHarness.Texts(log));
        Assert.True(game.World.World.HasFlag("door_open"));
    }

    [Fact]
    public void Interact_SequencePuzzle_WrongOrderResets()
    {
        var game = new ExplorationHarness();

        Assert.Equal(new[] { "Wrong!" }, ExplorationHarness.Texts(game.Run("press moon")));
        game.Run("press sun");
        Assert.Equal(new[] { "Wrong!" }, ExplorationHarness.Texts(game.Run("press sun")));

        Assert.Equal(0, game.World.World.GetCounter("seq"));
        Assert.False(game.World.World.HasFlag("door_open"));
    }

    [Theory]
    [InlineData("read", "Sun, then moon.")]
    [InlineData("press", "Press what? (sun, moon)")]
    [InlineData("press door", "Press which: sun, moon?")]
    [InlineData("dance", "I don't understand 'dance'. Type 'help' for commands.")]
    [InlineData("dance stone", "You can't dance that here.")]
    public void Interact_Prompts(string input, string expected)
    {
        var game = new ExplorationHarness();

        Assert.Equal(expected, Assert.Single(ExplorationHarness.Texts(game.Run(input))));
    }

    [Fact]
    public void Interact_AmbiguousTargets_AsksWhich()
    {
        var content = new TestContent().With("Rooms/rooms.json", """
            [ { "id": "start", "name": "S", "description": "D", "features": [
                { "id": "left", "name": "left lever", "description": "L", "interactions": [ { "verb": "pull", "text": "L!" } ] },
                { "id": "right", "name": "right lever", "description": "R", "interactions": [ { "verb": "pull", "text": "R!" } ] } ] } ]
            """).Load();
        var game = new ExplorationHarness(content);

        Assert.Equal("Which do you mean: left lever or right lever?", ExplorationHarness.SystemText(game.Run("pull lever")));
        Assert.Equal(new[] { "L!" }, ExplorationHarness.Texts(game.Run("pull left lever")));
    }

    [Theory]
    [InlineData(15, "A trapdoor!", true)]
    [InlineData(3, "Nothing.", false)]
    public void Interact_WithCheck_BranchesAndIsOneTime(int face, string outcome, bool found)
    {
        var game = new ExplorationHarness(dice: [face]);

        var log = game.Run("search stone");

        Assert.Equal(new[] { "You pry at it.", outcome }, ExplorationHarness.Texts(log));
        Assert.IsType<CheckRolledEvent>(log.Events[1]);
        Assert.Equal(found, game.World.World.HasFlag("found_trapdoor"));
        Assert.Equal("You can't search that here.", ExplorationHarness.SystemText(game.Run("search stone")));
    }

    [Fact]
    public void Interact_ByObjectIdThatIsGone_IsNotHandled()
    {
        var game = new ExplorationHarness();

        var log = game.Run(new ParsedCommand(CommandVerb.Interact, "press", "sun", ObjectId: "stone"));

        Assert.Equal("You can't press that here.", ExplorationHarness.SystemText(log));
    }

    [Fact]
    public void Execute_Empty_DoesNothing()
    {
        Assert.Empty(new ExplorationHarness().Run("").Events);
    }

    [Theory]
    [InlineData("inventory")]
    [InlineData("save")]
    [InlineData("quit")]
    public void Execute_MetaCommand_Throws(string input)
    {
        Assert.Throws<InvalidOperationException>(() => new ExplorationHarness().Run(input));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var game = new ExplorationHarness();
        var w = game.World;

        Assert.Throws<ArgumentNullException>(() => new ExplorationService(null!, w.Dice, w.Checks, w.Effects, w.Text));
        Assert.Throws<ArgumentNullException>(() => new ExplorationService(w.Content, null!, w.Checks, w.Effects, w.Text));
        Assert.Throws<ArgumentNullException>(() => new ExplorationService(w.Content, w.Dice, null!, w.Effects, w.Text));
        Assert.Throws<ArgumentNullException>(() => new ExplorationService(w.Content, w.Dice, w.Checks, null!, w.Text));
        Assert.Throws<ArgumentNullException>(() => new ExplorationService(w.Content, w.Dice, w.Checks, w.Effects, null!));
        Assert.Throws<ArgumentNullException>(() => game.Exploration.Execute(null!, w.State, new TurnLog()));
        Assert.Throws<ArgumentNullException>(() => game.Exploration.Execute(CommandParser.Parse("look"), null!, new TurnLog()));
        Assert.Throws<ArgumentNullException>(() => game.Exploration.Execute(CommandParser.Parse("look"), w.State, null!));
        Assert.Throws<ArgumentNullException>(() => game.Exploration.EnterRoom("start", null!, new TurnLog()));
        Assert.Throws<ArgumentNullException>(() => game.Exploration.EnterRoom("start", w.State, null!));
        Assert.Throws<ArgumentNullException>(() => game.Exploration.Describe(null!, true));
        Assert.Throws<ArgumentNullException>(() => game.Exploration.GetActions(null!));
    }
}
