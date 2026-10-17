namespace RpgEngine.Core.Content.Defs;

public sealed record StartingItemDef
{
    public required string Item { get; init; }

    public int Count { get; init; } = 1;

    public bool Equip { get; init; }
}
