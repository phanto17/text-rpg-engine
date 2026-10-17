namespace RpgEngine.Core.Content.Defs;

/// <summary>
/// A combat status. It lasts for <see cref="DurationRounds"/> of the affected creature's own turns
/// (a status applied during its own turn does not count that turn).
/// </summary>
public sealed record StatusEffectDef : IContentDef
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required int DurationRounds { get; init; }

    public StatusModifiersDef Modifiers { get; init; } = new();
}
