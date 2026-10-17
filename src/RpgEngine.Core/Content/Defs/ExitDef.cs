namespace RpgEngine.Core.Content.Defs;

/// <summary>
/// A way out of a room. When <see cref="Conditions"/> fail, the exit is shown as blocked if it has
/// <see cref="BlockedText"/>, and hidden entirely otherwise (a secret passage).
/// </summary>
public sealed record ExitDef
{
    public required Direction Direction { get; init; }

    public required string Target { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<ConditionDef> Conditions { get; init; } = [];

    public string? BlockedText { get; init; }
}
