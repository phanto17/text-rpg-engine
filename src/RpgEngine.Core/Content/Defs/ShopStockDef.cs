namespace RpgEngine.Core.Content.Defs;

/// <summary>One item a shop sells. <see cref="Stock"/> is how many it has; null means it never runs out.</summary>
public sealed record ShopStockDef
{
    public required string Item { get; init; }

    public required int Price { get; init; }

    public int? Stock { get; init; }
}
