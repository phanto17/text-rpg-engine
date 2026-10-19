namespace RpgEngine.Core.Persistence;

/// <summary>World state with every collection sorted, so save files diff cleanly.</summary>
public sealed record WorldData
{
    public required IReadOnlyList<string> Flags { get; init; }

    public required IReadOnlyDictionary<string, int> Counters { get; init; }

    public required IReadOnlyDictionary<string, int> Reputation { get; init; }

    public required IReadOnlyDictionary<string, int> QuestStages { get; init; }

    public required IReadOnlyList<string> VisitedRooms { get; init; }

    public required IReadOnlyDictionary<string, IReadOnlyList<string>> RoomItems { get; init; }

    public required IReadOnlyList<string> Used { get; init; }

    // Optional so version 2 saves written before shops existed still load.
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> ShopStock { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.Ordinal);
}
