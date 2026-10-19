using RpgEngine.Core.Content;

namespace RpgEngine.Core.World;

/// <summary>
/// Everything the player's choices have changed: flags, counters, faction reputation, quest
/// stages, visited rooms, items lying in rooms, and one-time interactions already used.
/// </summary>
public sealed class WorldState
{
    private readonly HashSet<string> _flags = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _counters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _reputation = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _questStages = new(StringComparer.Ordinal);
    private readonly HashSet<string> _visitedRooms = new(StringComparer.Ordinal);
    private readonly HashSet<string> _used = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _roomItems = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, int>> _shopStock = new(StringComparer.Ordinal);

    public IReadOnlySet<string> Flags => _flags;

    public IReadOnlyDictionary<string, int> Counters => _counters;

    public IReadOnlyDictionary<string, int> Reputation => _reputation;

    public IReadOnlyDictionary<string, int> QuestStages => _questStages;

    public IReadOnlySet<string> VisitedRooms => _visitedRooms;

    public IReadOnlySet<string> Used => _used;

    public IReadOnlyDictionary<string, IReadOnlyList<string>> RoomItems =>
        _roomItems.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)pair.Value, StringComparer.Ordinal);

    /// <summary>Remaining stock of each limited item a shop has sold from, by shop then item.</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> ShopStock =>
        _shopStock.ToDictionary(pair => pair.Key, pair => (IReadOnlyDictionary<string, int>)pair.Value, StringComparer.Ordinal);

    /// <summary>Creates the starting world: every room holds the items its definition lists.</summary>
    public static WorldState CreateNew(GameContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var world = new WorldState();
        foreach (var room in content.RoomList)
        {
            foreach (var item in room.Items)
            {
                world.AddItemToRoom(room.Id, item);
            }
        }

        return world;
    }

    public bool HasFlag(string flag) => _flags.Contains(flag);

    public void SetFlag(string flag) => _flags.Add(flag);

    public void ClearFlag(string flag) => _flags.Remove(flag);

    public int GetCounter(string counter) => _counters.GetValueOrDefault(counter);

    public void SetCounter(string counter, int value) => _counters[counter] = value;

    public int AddCounter(string counter, int amount) => _counters[counter] = GetCounter(counter) + amount;

    public int GetReputation(string faction) => _reputation.GetValueOrDefault(faction);

    public int ChangeReputation(string faction, int amount) => _reputation[faction] = GetReputation(faction) + amount;

    public int GetQuestStage(string quest) => _questStages.GetValueOrDefault(quest);

    /// <summary>Moves a quest to <paramref name="stage"/> if that is further along; returns whether it advanced.</summary>
    public bool AdvanceQuest(string quest, int stage)
    {
        if (stage <= GetQuestStage(quest))
        {
            return false;
        }

        _questStages[quest] = stage;
        return true;
    }

    public bool HasVisited(string roomId) => _visitedRooms.Contains(roomId);

    /// <summary>Marks a room visited; returns true on the first visit.</summary>
    public bool MarkVisited(string roomId) => _visitedRooms.Add(roomId);

    /// <summary>Whether a one-time interaction, dialogue choice, or locked check has been used.</summary>
    public bool IsUsed(string key) => _used.Contains(key);

    public void MarkUsed(string key) => _used.Add(key);

    public IReadOnlyList<string> ItemsIn(string roomId) =>
        _roomItems.TryGetValue(roomId, out var items) ? items : [];

    public void AddItemToRoom(string roomId, string itemId)
    {
        if (!_roomItems.TryGetValue(roomId, out var items))
        {
            items = [];
            _roomItems[roomId] = items;
        }

        items.Add(itemId);
    }

    /// <summary>Remaining stock of a shop item, or null if nothing has been bought yet (use the shop's own stock).</summary>
    public int? GetShopStock(string shopId, string itemId) =>
        _shopStock.TryGetValue(shopId, out var items) && items.TryGetValue(itemId, out var count) ? count : null;

    public void SetShopStock(string shopId, string itemId, int remaining)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(remaining);
        if (!_shopStock.TryGetValue(shopId, out var items))
        {
            items = new Dictionary<string, int>(StringComparer.Ordinal);
            _shopStock[shopId] = items;
        }

        items[itemId] = remaining;
    }

    public bool RemoveItemFromRoom(string roomId, string itemId) =>
        _roomItems.TryGetValue(roomId, out var items) && items.Remove(itemId);
}
