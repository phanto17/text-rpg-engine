namespace RpgEngine.Core.Content.Defs;

/// <summary>A spell or martial technique usable in combat.</summary>
public sealed record SpellDef : IContentDef
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required SpellKind Kind { get; init; }

    public required string Description { get; init; }

    public SpellCostDef Cost { get; init; } = new();

    public required SpellTarget Target { get; init; }

    public required SpellResolutionDef Resolution { get; init; }
}
