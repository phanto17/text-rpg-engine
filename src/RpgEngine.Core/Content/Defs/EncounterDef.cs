namespace RpgEngine.Core.Content.Defs;

/// <summary>A combat encounter, defined inline in a room and triggered on entry while its conditions pass.</summary>
public sealed record EncounterDef
{
    public required string Id { get; init; }

    public IReadOnlyList<ConditionDef> Conditions { get; init; } = [];

    public string? Intro { get; init; }

    public required IReadOnlyList<EncounterEnemyDef> Enemies { get; init; }

    public bool CanFlee { get; init; } = true;

    /// <summary>Room the player retreats to after fleeing.</summary>
    public string? FleeTo { get; init; }

    public ParleyDef? Parley { get; init; }

    public IReadOnlyList<EffectDef> OnVictory { get; init; } = [];

    /// <summary>Runs after a victory or a successful parley.</summary>
    public IReadOnlyList<EffectDef> OnResolved { get; init; } = [];
}
