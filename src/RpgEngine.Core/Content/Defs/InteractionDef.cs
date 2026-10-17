namespace RpgEngine.Core.Content.Defs;

/// <summary>
/// A verb that can be used on a feature, e.g. <c>press sun</c>. Interactions sharing a
/// (verb, target) form a group: the group is offered while any member's conditions pass,
/// and the first passing member runs.
/// </summary>
public sealed record InteractionDef
{
    public required string Verb { get; init; }

    /// <summary>Optional sub-target (e.g. <c>sun</c> on a door with three sigils).</summary>
    public string? Target { get; init; }

    /// <summary>Menu label; defaults to "Verb target".</summary>
    public string? Label { get; init; }

    public IReadOnlyList<ConditionDef> Conditions { get; init; } = [];

    /// <summary>Can only be used once per playthrough.</summary>
    public bool Once { get; init; }

    public string? Text { get; init; }

    public IReadOnlyList<EffectDef> Effects { get; init; } = [];

    public CheckDef? Check { get; init; }

    public OutcomeDef? Success { get; init; }

    public OutcomeDef? Failure { get; init; }
}
