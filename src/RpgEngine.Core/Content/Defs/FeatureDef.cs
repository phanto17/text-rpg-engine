namespace RpgEngine.Core.Content.Defs;

/// <summary>Something in a room that can be inspected and interacted with.</summary>
public sealed record FeatureDef
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<string> Aliases { get; init; } = [];

    public required string Description { get; init; }

    /// <summary>The feature is only visible while these pass (hidden secrets).</summary>
    public IReadOnlyList<ConditionDef> Conditions { get; init; } = [];

    public IReadOnlyList<InteractionDef> Interactions { get; init; } = [];
}
