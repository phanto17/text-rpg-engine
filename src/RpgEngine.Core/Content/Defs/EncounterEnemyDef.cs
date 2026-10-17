namespace RpgEngine.Core.Content.Defs;

public sealed record EncounterEnemyDef
{
    public required string Monster { get; init; }

    public int Count { get; init; } = 1;
}
