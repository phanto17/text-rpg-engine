namespace RpgEngine.Core.Content.Defs;

public sealed record QuestStageDef
{
    public required int Stage { get; init; }

    public required string Journal { get; init; }
}
