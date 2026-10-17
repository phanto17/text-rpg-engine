namespace RpgEngine.Core.Content.Defs;

public sealed record EpilogueDef
{
    public IReadOnlyList<ConditionDef> Conditions { get; init; } = [];

    public required string Text { get; init; }
}
