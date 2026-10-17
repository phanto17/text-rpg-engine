namespace RpgEngine.Core.Content.Defs;

public sealed record LootDef
{
    public required string Item { get; init; }

    /// <summary>Drop chance from 0 (never) to 1 (always).</summary>
    public required double Chance { get; init; }
}
