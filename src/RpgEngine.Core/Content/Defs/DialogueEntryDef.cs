namespace RpgEngine.Core.Content.Defs;

public sealed record DialogueEntryDef
{
    public IReadOnlyList<ConditionDef> Conditions { get; init; } = [];

    public required string Node { get; init; }
}
