namespace RpgEngine.Core.Content.Defs;

public sealed record MonsterAiDef
{
    public AiBehavior Behavior { get; init; } = AiBehavior.Aggressive;

    /// <summary>Flees once HP drops below this percentage of max (0 = never).</summary>
    public int FleeBelowHpPercent { get; init; }
}
