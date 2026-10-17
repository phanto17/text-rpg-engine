namespace RpgEngine.Core.Content.Defs;

public sealed record QuestDef : IContentDef
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required IReadOnlyList<QuestStageDef> Stages { get; init; }
}
