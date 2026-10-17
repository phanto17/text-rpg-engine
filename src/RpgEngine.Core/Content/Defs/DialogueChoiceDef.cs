namespace RpgEngine.Core.Content.Defs;

/// <summary>
/// A player reply. Exactly one of <see cref="Next"/>, <see cref="End"/>, or <see cref="Check"/>
/// (with success and failure nodes) decides where the conversation goes.
/// </summary>
public sealed record DialogueChoiceDef
{
    public required string Text { get; init; }

    public IReadOnlyList<ConditionDef> Conditions { get; init; } = [];

    public bool Once { get; init; }

    public IReadOnlyList<EffectDef> Effects { get; init; } = [];

    public string? Next { get; init; }

    public bool End { get; init; }

    public CheckDef? Check { get; init; }

    public string? SuccessNode { get; init; }

    public string? FailureNode { get; init; }

    /// <summary>When false, the check can be attempted only once, ever.</summary>
    public bool Retry { get; init; } = true;
}
