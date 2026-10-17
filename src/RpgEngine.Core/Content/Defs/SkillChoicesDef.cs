namespace RpgEngine.Core.Content.Defs;

public sealed record SkillChoicesDef
{
    public required int Count { get; init; }

    public required IReadOnlyList<string> From { get; init; }
}
