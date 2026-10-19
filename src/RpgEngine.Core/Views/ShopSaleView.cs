using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Core.Views;

/// <summary>An item the player can sell: what the shop pays and how many can be sold (equipped gear excluded).</summary>
public sealed record ShopSaleView(ItemDef Item, int Price, int Count);
