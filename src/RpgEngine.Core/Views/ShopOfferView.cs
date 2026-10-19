using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Core.Views;

/// <summary>An item a shop has for sale. <see cref="Remaining"/> is null when it never runs out.</summary>
public sealed record ShopOfferView(ItemDef Item, int Price, int? Remaining, bool CanAfford);
