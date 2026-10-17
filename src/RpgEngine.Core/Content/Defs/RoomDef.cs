namespace RpgEngine.Core.Content.Defs;

public sealed record RoomDef : IContentDef
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Full description, shown on the first visit and by <c>look</c>.</summary>
    public required string Description { get; init; }

    /// <summary>Shorter text for later visits (falls back to <see cref="Description"/>).</summary>
    public string? Revisit { get; init; }

    /// <summary>Extra paragraphs appended when their conditions pass (e.g. reflecting earlier choices).</summary>
    public IReadOnlyList<TextVariantDef> Variants { get; init; } = [];

    /// <summary>Sensory lines; one is picked at random each time the room is shown.</summary>
    public IReadOnlyList<string> Ambient { get; init; } = [];

    public IReadOnlyList<ExitDef> Exits { get; init; } = [];

    public IReadOnlyList<FeatureDef> Features { get; init; } = [];

    /// <summary>Item ids lying here at the start of the game.</summary>
    public IReadOnlyList<string> Items { get; init; } = [];

    public IReadOnlyList<NpcDef> Npcs { get; init; } = [];

    public EncounterDef? Encounter { get; init; }

    public IReadOnlyList<EffectDef> OnFirstEnter { get; init; } = [];

    public IReadOnlyList<EffectDef> OnEnter { get; init; } = [];
}
