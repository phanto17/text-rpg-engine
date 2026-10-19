namespace RpgEngine.Core.Content.Defs;

/// <summary>A merchant: what they sell, at what price, and how much they pay for the player's goods.</summary>
public sealed record ShopDef : IContentDef
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Who speaks the greeting.</summary>
    public required string Keeper { get; init; }

    public required string Greeting { get; init; }

    public IReadOnlyList<ShopStockDef> Stock { get; init; } = [];

    /// <summary>Fraction of an item's value paid when the player sells it (rounded down).</summary>
    public double BuybackRate { get; init; } = 0.5;
}
