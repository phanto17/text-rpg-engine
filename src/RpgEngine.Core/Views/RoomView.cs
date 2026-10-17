namespace RpgEngine.Core.Views;

/// <summary>A snapshot of what the player perceives in a room, ready for the UI to render.</summary>
public sealed record RoomView(
    string RoomId,
    string Name,
    IReadOnlyList<string> Paragraphs,
    string? Ambient,
    IReadOnlyList<ExitView> Exits,
    IReadOnlyList<string> Features,
    IReadOnlyList<string> Items,
    IReadOnlyList<string> Npcs);
