using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Core.Views;

/// <summary>An exit as the player sees it. <see cref="KnownDestination"/> is set once the target room has been visited.</summary>
public sealed record ExitView(Direction Direction, string? Description, bool IsBlocked, string? KnownDestination);
