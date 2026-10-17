using RpgEngine.Core.World;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.World;

public sealed class WorldStateTests
{
    [Fact]
    public void Flags_SetAndClear()
    {
        var world = new WorldState();

        world.SetFlag("door_open");
        Assert.True(world.HasFlag("door_open"));
        Assert.Contains("door_open", world.Flags);

        world.ClearFlag("door_open");
        Assert.False(world.HasFlag("door_open"));
    }

    [Fact]
    public void Counters_DefaultToZeroAndAccumulate()
    {
        var world = new WorldState();

        Assert.Equal(0, world.GetCounter("levers"));
        Assert.Equal(2, world.AddCounter("levers", 2));
        Assert.Equal(1, world.AddCounter("levers", -1));

        world.SetCounter("levers", 7);
        Assert.Equal(7, world.Counters["levers"]);
    }

    [Fact]
    public void Reputation_DefaultsToZeroAndChanges()
    {
        var world = new WorldState();

        Assert.Equal(0, world.GetReputation("order"));
        Assert.Equal(10, world.ChangeReputation("order", 10));
        Assert.Equal(-5, world.ChangeReputation("order", -15));
        Assert.Equal(-5, world.Reputation["order"]);
    }

    [Fact]
    public void Quests_OnlyMoveForward()
    {
        var world = new WorldState();

        Assert.True(world.AdvanceQuest("main", 20));
        Assert.False(world.AdvanceQuest("main", 10));
        Assert.False(world.AdvanceQuest("main", 20));
        Assert.True(world.AdvanceQuest("main", 30));

        Assert.Equal(30, world.GetQuestStage("main"));
        Assert.Equal(30, world.QuestStages["main"]);
        Assert.Equal(0, world.GetQuestStage("other"));
    }

    [Fact]
    public void Visited_ReportsFirstVisit()
    {
        var world = new WorldState();

        Assert.False(world.HasVisited("start"));
        Assert.True(world.MarkVisited("start"));
        Assert.False(world.MarkVisited("start"));
        Assert.True(world.HasVisited("start"));
        Assert.Contains("start", world.VisitedRooms);
    }

    [Fact]
    public void Used_TracksOneTimeKeys()
    {
        var world = new WorldState();

        Assert.False(world.IsUsed("search:start:door"));
        world.MarkUsed("search:start:door");
        Assert.True(world.IsUsed("search:start:door"));
        Assert.Single(world.Used);
    }

    [Fact]
    public void RoomItems_AddAndRemove()
    {
        var world = new WorldState();

        Assert.Empty(world.ItemsIn("start"));
        Assert.False(world.RemoveItemFromRoom("start", "potion"));

        world.AddItemToRoom("start", "potion");
        world.AddItemToRoom("start", "potion");

        Assert.Equal(new[] { "potion", "potion" }, world.ItemsIn("start"));
        Assert.True(world.RemoveItemFromRoom("start", "potion"));
        Assert.Equal(new[] { "potion" }, world.RoomItems["start"]);
        Assert.False(world.RemoveItemFromRoom("start", "sword"));
    }

    [Fact]
    public void CreateNew_PlacesRoomItemsFromContent()
    {
        var content = new TestContent().With("Rooms/rooms.json", """
            [ { "id": "start", "name": "S", "description": "D", "items": ["potion", "key"] } ]
            """).Load();

        var world = WorldState.CreateNew(content);

        Assert.Equal(new[] { "potion", "key" }, world.ItemsIn("start"));
    }

    [Fact]
    public void CreateNew_NullContent_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => WorldState.CreateNew(null!));
    }

    [Fact]
    public void GameState_ValidatesArguments()
    {
        var player = TestCharacters.Create();
        var world = new WorldState();

        Assert.Throws<ArgumentNullException>(() => new GameState(null!, world, "start"));
        Assert.Throws<ArgumentNullException>(() => new GameState(player, null!, "start"));
        Assert.Throws<ArgumentException>(() => new GameState(player, world, " "));

        var state = new GameState(player, world, "start") { CurrentRoomId = "hall" };
        Assert.Equal("hall", state.CurrentRoomId);
    }
}
