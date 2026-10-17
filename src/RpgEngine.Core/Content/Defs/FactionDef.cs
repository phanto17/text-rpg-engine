namespace RpgEngine.Core.Content.Defs;

public sealed record FactionDef : IContentDef
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string Description { get; init; } = string.Empty;
}
