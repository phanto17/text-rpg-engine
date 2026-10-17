namespace RpgEngine.Core.Content.Defs;

public sealed record NpcDef
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<string> Aliases { get; init; } = [];

    public required string Description { get; init; }

    public required string Dialogue { get; init; }

    /// <summary>The NPC is present only while these pass.</summary>
    public IReadOnlyList<ConditionDef> Conditions { get; init; } = [];
}
