using System.Text.Json;
using RpgEngine.Core.Content;
using RpgEngine.Core.Persistence;
using RpgEngine.Core.World;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Persistence;

public sealed class SaveServiceTests : IDisposable
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "rpg-saves-" + Guid.NewGuid().ToString("N"));
    private readonly FixedTimeProvider _time = new(Noon);
    private readonly GameContent _content = ExplorationFixture.Load();

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private SaveService Service() => new(_directory, _time);

    private GameState BusyState()
    {
        var player = TestCharacters.Create(skills: ["persuasion", "stealth"], expertise: ["stealth"], spells: ["second_wind"], maxHp: 12, maxMana: 4, name: "Aria");
        player.Inventory.Add("sword");
        player.Inventory.Add("potion", 2);
        player.EquippedWeaponId = "sword";
        player.TakeDamage(5);
        player.TrySpendMana(1);
        player.AddGold(17);
        player.AddXp(40);

        var world = WorldState.CreateNew(_content);
        world.SetFlag("door_open");
        world.SetCounter("seq", 2);
        world.ChangeReputation("order", -15);
        world.AdvanceQuest("main", 10);
        world.MarkVisited("start");
        world.MarkVisited("hall");
        world.MarkUsed("interaction:start:stone:0");
        world.RemoveItemFromRoom("start", "potion");
        world.AddItemToRoom("hall", "key");
        world.SetShopStock("stall", "potion", 1);

        return new GameState(player, world, "hall");
    }

    [Fact]
    public void SaveThenLoad_RoundTripsEveryField()
    {
        var service = Service();
        var state = BusyState();

        var path = service.Save(state, _content, "slot1");
        var loaded = service.Load("slot1", _content);

        Assert.Equal(Path.Combine(_directory, "slot1.json"), path);
        Assert.False(loaded.ContentVersionMismatch);
        Assert.Equal(Noon, loaded.Save.SavedAtUtc);
        var expected = JsonSerializer.Serialize(SaveService.Capture(state, _content, Noon), ContentJson.IndentedOptions);
        var actual = JsonSerializer.Serialize(SaveService.Capture(loaded.State, _content, Noon), ContentJson.IndentedOptions);
        Assert.Equal(expected, actual);
        Assert.Equal(expected, File.ReadAllText(path));
    }

    [Fact]
    public void SaveThenLoad_LeveledCharacterAtFullStrength_RestoresLevelMaximumsAndSpells()
    {
        // A level 2 hero at full HP and MP has current values above the level 1 maximums, so the
        // save must carry the leveled maximums and learned spells for the restore to accept them.
        var content = World.ProgressionTests.Leveling();
        var wizard = TestCharacters.Create(maxHp: 6, maxMana: 6, spells: ["firebolt", "missile"], classId: "wizard", name: "Ilse");
        Progression.GrantXp(wizard, content, 150, new Core.Events.TurnLog());
        Assert.Equal((2, 10, 10, 8, 8), (wizard.Level, wizard.MaxHp, wizard.CurrentHp, wizard.MaxMana, wizard.CurrentMana));
        var service = Service();

        service.Save(new GameState(wizard, WorldState.CreateNew(content), "start"), content, "leveled");
        var restored = service.Load("leveled", content).State.Player;

        Assert.Equal((2, 150, 10, 10, 8, 8), (restored.Level, restored.Xp, restored.MaxHp, restored.CurrentHp, restored.MaxMana, restored.CurrentMana));
        Assert.Equal(new[] { "firebolt", "missile", "second_wind" }, restored.SpellIds);
        Assert.Equal(300, Progression.NextLevelXp(restored, content));
    }

    [Fact]
    public void Load_RestoresPlayableState()
    {
        var service = Service();
        service.Save(BusyState(), _content, "slot1");

        var state = service.Load("slot1", _content).State;

        Assert.Equal(("Aria", 7, 3, 17, 40), (state.Player.Name, state.Player.CurrentHp, state.Player.CurrentMana, state.Player.Gold, state.Player.Xp));
        Assert.Equal("orphan", state.Player.BackgroundId);
        Assert.Equal(Core.Characters.ProficiencyLevel.Expertise, state.Player.GetSkillProficiency("stealth"));
        Assert.Equal("sword", state.Player.EquippedWeaponId);
        Assert.Equal(2, state.Player.Inventory.CountOf("potion"));
        Assert.Equal("hall", state.CurrentRoomId);
        Assert.True(state.World.HasFlag("door_open"));
        Assert.Equal(2, state.World.GetCounter("seq"));
        Assert.Equal(-15, state.World.GetReputation("order"));
        Assert.Equal(10, state.World.GetQuestStage("main"));
        Assert.True(state.World.IsUsed("interaction:start:stone:0"));
        Assert.Equal(new[] { "dagger" }, state.World.ItemsIn("start"));
        Assert.Equal(new[] { "key" }, state.World.ItemsIn("hall"));
        Assert.Equal(1, state.World.GetShopStock("stall", "potion"));
    }

    [Fact]
    public void Load_DifferentContentVersion_FlagsMismatch()
    {
        var service = Service();
        service.Save(BusyState(), _content, "slot1");
        var newer = new TestContent()
            .With("game.json", TestContent.Manifest.Replace("0.0.1", "9.9.9", StringComparison.Ordinal))
            .With("Rooms/rooms.json", ExplorationFixture.Rooms)
            .With("Dialogues/sage.json", ExplorationFixture.Dialogue)
            .Load();

        Assert.True(service.Load("slot1", newer).ContentVersionMismatch);
    }

    [Fact]
    public void Load_UnsupportedSaveVersion_Throws()
    {
        var service = Service();
        var path = service.Save(BusyState(), _content, "slot1");
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"saveVersion\": 2", "\"saveVersion\": 99", StringComparison.Ordinal));

        var exception = Assert.Throws<SaveGameException>(() => service.Load("slot1", _content));
        Assert.Contains("version 99", exception.Message);
    }

    [Fact]
    public void Load_VersionOneSaveWithoutBackground_ReportsTheFormatVersion()
    {
        // A version 1 save has no background. It must still parse, so the player is told the save
        // is from an older format rather than that the file is unreadable.
        var service = Service();
        var path = service.Save(BusyState(), _content, "slot1");
        var json = File.ReadAllText(path)
            .Replace("\"saveVersion\": 2", "\"saveVersion\": 1", StringComparison.Ordinal)
            .Replace("\"background\": \"orphan\",", string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("background", json);
        File.WriteAllText(path, json);

        var exception = Assert.Throws<SaveGameException>(() => service.Load("slot1", _content));
        Assert.Contains("version 1", exception.Message);
        Assert.DoesNotContain("could not be read", exception.Message);
    }

    [Fact]
    public void Restore_SaveWithoutBackground_Throws()
    {
        var save = SaveService.Capture(BusyState(), _content, Noon);
        var withoutBackground = save with { Player = save.Player with { Background = null } };

        var exception = Assert.Throws<SaveGameException>(() => SaveService.Restore(withoutBackground, _content));
        Assert.Contains("unknown background '(none)'", exception.Message);
    }

    [Theory]
    [InlineData("\"classId\": \"fighter\"", "\"classId\": \"bard\"", "unknown class 'bard'")]
    [InlineData("\"background\": \"orphan\"", "\"background\": \"noble\"", "unknown background 'noble'")]
    [InlineData("\"currentRoom\": \"hall\"", "\"currentRoom\": \"void\"", "unknown room 'void'")]
    [InlineData("\"stall\": {", "\"bazaar\": {", "unknown shop 'bazaar'")]
    [InlineData("\"potion\": 1", "\"dagger\": 1", "shop 'stall' does not stock 'dagger'")]
    [InlineData("\"potion\": 1", "\"potion\": -1", "shop 'stall' has negative stock of 'potion'")]
    [InlineData("\"itemId\": \"sword\"", "\"itemId\": \"laser\"", "unknown item 'laser'")]
    [InlineData("\"second_wind\"", "\"fireball\"", "unknown spell 'fireball'")]
    [InlineData("\"equippedWeapon\": \"sword\"", "\"equippedWeapon\": \"dagger\"", "equipped weapon")]
    [InlineData("\"currentHp\": 7", "\"currentHp\": 70", "damaged")]
    [InlineData("\"str\": 10", "\"str\": 0", "could not be read")]
    [InlineData("\"name\": \"Aria\"", "\"name\": \"Aria\", \"surprise\": true", "could not be read")]
    public void Load_TamperedSave_Throws(string original, string replacement, string fragment)
    {
        var service = Service();
        var path = service.Save(BusyState(), _content, "slot1");
        var json = File.ReadAllText(path);
        Assert.Contains(original, json);
        File.WriteAllText(path, json.Replace(original, replacement, StringComparison.Ordinal));

        var exception = Assert.Throws<SaveGameException>(() => service.Load("slot1", _content));
        Assert.Contains(fragment, exception.Message);
    }

    [Fact]
    public void Load_EquippedArmorMissingFromInventory_Throws()
    {
        var state = BusyState();
        state.Player.EquippedArmorId = "mail";
        var service = Service();
        service.Save(state, _content, "slot1");

        Assert.Contains("equipped armor", Assert.Throws<SaveGameException>(() => service.Load("slot1", _content)).Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a b")]
    [InlineData("../escape")]
    [InlineData("name.json")]
    [InlineData("abcdefghijabcdefghijabcdefghijabc")]
    [InlineData(null)]
    public void SlotNames_AreValidated(string? slot)
    {
        var service = Service();

        Assert.False(SaveService.IsValidSlotName(slot));
        Assert.False(service.Exists(slot!));
        Assert.Throws<SaveGameException>(() => service.Save(BusyState(), _content, slot!));
        Assert.Throws<SaveGameException>(() => service.Load(slot!, _content));
    }

    [Fact]
    public void Load_MissingOrCorruptOrEmpty_Throws()
    {
        var service = Service();
        Assert.Contains("no save named", Assert.Throws<SaveGameException>(() => service.Load("ghost", _content)).Message);

        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "broken.json"), "{ not json");
        Assert.Contains("could not be read", Assert.Throws<SaveGameException>(() => service.Load("broken", _content)).Message);

        File.WriteAllText(Path.Combine(_directory, "empty.json"), "null");
        Assert.Contains("is empty", Assert.Throws<SaveGameException>(() => service.Load("empty", _content)).Message);
    }

    [Fact]
    public void ListSlots_NewestFirstAndSkipsUnreadable()
    {
        var service = Service();
        Assert.Empty(service.ListSlots());

        service.Save(BusyState(), _content, "older");
        _time.Now = Noon.AddHours(1);
        service.Save(BusyState(), _content, "newer");
        File.WriteAllText(Path.Combine(_directory, "broken.json"), "{");
        File.WriteAllText(Path.Combine(_directory, "bad name.json"), "{}");

        var slots = service.ListSlots();

        Assert.Equal(new[] { "newer", "older" }, slots.Select(s => s.Slot));
        Assert.Equal(("Aria", 1, "hall"), (slots[0].PlayerName, slots[0].Level, slots[0].RoomId));
        Assert.True(service.Exists("older"));
        Assert.False(service.Exists("missing"));
    }

    [Fact]
    public void Save_OverwritesExistingSlot()
    {
        var service = Service();
        var state = BusyState();
        service.Save(state, _content, "slot1");

        state.Player.AddGold(100);
        service.Save(state, _content, "slot1");

        Assert.Equal(117, service.Load("slot1", _content).State.Player.Gold);
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var service = Service();

        Assert.Throws<ArgumentException>(() => new SaveService(" ", _time));
        Assert.Throws<ArgumentNullException>(() => new SaveService(_directory, null!));
        Assert.Throws<ArgumentNullException>(() => service.Save(null!, _content, "x"));
        Assert.Throws<ArgumentNullException>(() => service.Save(BusyState(), null!, "x"));
        Assert.Throws<ArgumentNullException>(() => service.Load("x", null!));
        Assert.Throws<ArgumentNullException>(() => SaveService.Capture(null!, _content, Noon));
        Assert.Throws<ArgumentNullException>(() => SaveService.Capture(BusyState(), null!, Noon));
        Assert.Throws<ArgumentNullException>(() => SaveService.Restore(null!, _content));
        Assert.Throws<ArgumentNullException>(() => SaveService.Restore(SaveService.Capture(BusyState(), _content, Noon), null!));
        Assert.Equal(Path.GetFullPath(_directory), service.Directory);
    }
}
