namespace RpgEngine.Core.Content.Defs;

public sealed record ParleyOptionDef
{
    public required string Label { get; init; }

    public IReadOnlyList<ConditionDef> Conditions { get; init; } = [];

    public required CheckDef Check { get; init; }
}
