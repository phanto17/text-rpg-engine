namespace RpgEngine.Core.Content.Defs;

public sealed record TextVariantDef
{
    public IReadOnlyList<ConditionDef> Conditions { get; init; } = [];

    public required string Text { get; init; }
}
