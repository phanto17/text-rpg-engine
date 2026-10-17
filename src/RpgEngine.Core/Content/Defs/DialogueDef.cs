namespace RpgEngine.Core.Content.Defs;

public sealed record DialogueDef : IContentDef
{
    public required string Id { get; init; }

    public required string Speaker { get; init; }

    /// <summary>The first entry whose conditions pass picks the starting node.</summary>
    public required IReadOnlyList<DialogueEntryDef> Entries { get; init; }

    public required IReadOnlyList<DialogueNodeDef> Nodes { get; init; }
}
