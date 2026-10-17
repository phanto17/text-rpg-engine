namespace RpgEngine.Core.Content.Defs;

/// <summary>Ways to talk down an encounter. Each option can be tried once per fight; success ends combat.</summary>
public sealed record ParleyDef
{
    public required IReadOnlyList<ParleyOptionDef> Options { get; init; }

    public IReadOnlyList<EffectDef> OnSuccess { get; init; } = [];

    public IReadOnlyList<EffectDef> OnFailure { get; init; } = [];
}
