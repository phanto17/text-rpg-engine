using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Events;
using RpgEngine.Core.Views;
using RpgEngine.Core.World;

namespace RpgEngine.Core.Shop;

/// <summary>
/// Buying and selling with one merchant. Limited stock is tracked in <see cref="WorldState"/>, so it
/// persists across visits and saves. Quest items (keepsakes, the Barrowlight) are never bought, and
/// equipped gear can only be sold while a spare is carried.
/// </summary>
public sealed class ShopSession
{
    private readonly GameContent _content;
    private readonly GameState _state;

    public ShopSession(GameContent content, ShopDef shop, GameState state)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(shop);
        ArgumentNullException.ThrowIfNull(state);
        _content = content;
        Shop = shop;
        _state = state;
    }

    public ShopDef Shop { get; }

    public bool IsOver { get; private set; }

    /// <summary>Everything still in stock, in the shop's own order.</summary>
    public IReadOnlyList<ShopOfferView> Offers() => Shop.Stock
        .Select(entry => (Entry: entry, Remaining: Remaining(entry)))
        .Where(x => x.Remaining is not 0)
        .Select(x => new ShopOfferView(_content.Items[x.Entry.Item], x.Entry.Price, x.Remaining, _state.Player.Gold >= x.Entry.Price))
        .ToList();

    /// <summary>What the player can sell, in inventory order.</summary>
    public IReadOnlyList<ShopSaleView> Sellables() => _state.Player.Inventory.Entries
        .Select(entry => (Item: _content.Items[entry.ItemId], Count: entry.Count - (IsEquipped(entry.ItemId) ? 1 : 0)))
        .Where(x => x.Item.Type != ItemType.Quest && x.Count > 0)
        .Select(x => new ShopSaleView(x.Item, SalePrice(x.Item), x.Count))
        .ToList();

    public void Buy(string itemId, TurnLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        var offer = Offers().FirstOrDefault(o => o.Item.Id == itemId)
            ?? throw new ArgumentException($"'{itemId}' is not for sale here.", nameof(itemId));

        var player = _state.Player;
        if (!player.TrySpendGold(offer.Price))
        {
            log.Add(new NarrationEvent(TextStyle.System, $"You can't afford the {offer.Item.Name} ({offer.Price} gold)."));
            return;
        }

        player.Inventory.Add(itemId);
        if (offer.Remaining is { } remaining)
        {
            _state.World.SetShopStock(Shop.Id, itemId, remaining - 1);
        }

        log.Add(new GoldSpentEvent(offer.Price, player.Gold));
        log.Add(new ItemGainedEvent(offer.Item, 1));
    }

    public void Sell(string itemId, TurnLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        var sale = Sellables().FirstOrDefault(s => s.Item.Id == itemId)
            ?? throw new ArgumentException($"'{itemId}' cannot be sold here.", nameof(itemId));

        var player = _state.Player;
        player.Inventory.Remove(itemId);
        player.AddGold(sale.Price);
        log.Add(new ItemLostEvent(sale.Item, 1));
        log.Add(new GoldGainedEvent(sale.Price, player.Gold));
    }

    public void Leave() => IsOver = true;

    private int? Remaining(ShopStockDef entry) =>
        entry.Stock is { } initial ? _state.World.GetShopStock(Shop.Id, entry.Item) ?? initial : null;

    private int SalePrice(ItemDef item) => (int)Math.Floor(item.Value * Shop.BuybackRate);

    private bool IsEquipped(string itemId) =>
        _state.Player.EquippedWeaponId == itemId || _state.Player.EquippedArmorId == itemId;
}
