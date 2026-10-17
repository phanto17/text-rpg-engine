namespace RpgEngine.Core.Persistence;

/// <summary>A save file: state only. Content is always reloaded from <c>Data/</c>.</summary>
public sealed record SaveGame
{
    public const int CurrentVersion = 1;

    public required int SaveVersion { get; init; }

    public required string ContentVersion { get; init; }

    public required DateTimeOffset SavedAtUtc { get; init; }

    public required string CurrentRoom { get; init; }

    public required PlayerData Player { get; init; }

    public required WorldData World { get; init; }
}
