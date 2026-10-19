using RpgEngine.Core.Events;
using RpgEngine.Core.Shop;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Shop;

public sealed class ShopSessionTests
{
    // The stall (TestContent.Shops) sells 2 potions at 10 gp and unlimited swords at 15 gp, and buys at half value.
    private static (ShopSession Shop, TestWorld World) Open(int gold = 30)
    {
        var world = new TestWorld();
        world.Player.AddGold(gold);
        return (new ShopSession(world.Content, world.Content.Shops["stall"], world.State), world);
    }

    [Fact]
    public void Offers_ListStockPricesAndAffordability()
    {
        var (shop, _) = Open(gold: 12);

        var offers = shop.Offers();

        Assert.Equal(new[] { ("potion", 10, (int?)2, true), ("sword", 15, (int?)null, false) }, offers.Select(o => (o.Item.Id, o.Price, o.Remaining, o.CanAfford)));
    }

    [Fact]
    public void Buy_SpendsGoldAddsTheItemAndDrawsDownLimitedStock()
    {
        var (shop, world) = Open();
        var log = new TurnLog();

        shop.Buy("potion", log);

        Assert.Equal(new GameEvent[] { new GoldSpentEvent(10, 20), new ItemGainedEvent(world.Content.Items["potion"], 1) }, log.Events);
        Assert.Equal(1, world.Player.Inventory.CountOf("potion"));
        Assert.Equal(1, world.World.GetShopStock("stall", "potion"));
        Assert.Equal(1, shop.Offers()[0].Remaining);
    }

    [Fact]
    public void Buy_LastOfTheStock_RemovesTheOfferForGood()
    {
        var (shop, world) = Open(gold: 100);

        shop.Buy("potion", new TurnLog());
        shop.Buy("potion", new TurnLog());

        Assert.Equal(new[] { "sword" }, shop.Offers().Select(o => o.Item.Id));
        Assert.Equal(0, world.World.GetShopStock("stall", "potion"));
        Assert.Throws<ArgumentException>(() => shop.Buy("potion", new TurnLog()));
    }

    [Fact]
    public void Buy_UnlimitedStock_NeverRunsOut()
    {
        var (shop, world) = Open(gold: 100);

        shop.Buy("sword", new TurnLog());
        shop.Buy("sword", new TurnLog());

        Assert.Equal(2, world.Player.Inventory.CountOf("sword"));
        Assert.Null(world.World.GetShopStock("stall", "sword"));
        Assert.Null(shop.Offers().Single(o => o.Item.Id == "sword").Remaining);
    }

    [Fact]
    public void Buy_TooExpensive_ExplainsAndChangesNothing()
    {
        var (shop, world) = Open(gold: 5);
        var log = new TurnLog();

        shop.Buy("sword", log);

        var message = Assert.IsType<NarrationEvent>(Assert.Single(log.Events));
        Assert.Equal("You can't afford the Longsword (15 gold).", message.Text);
        Assert.Equal(5, world.Player.Gold);
        Assert.False(world.Player.Inventory.Contains("sword"));
    }

    [Fact]
    public void Sell_PaysHalfValueRoundedDown()
    {
        var (shop, world) = Open(gold: 0);
        world.Player.Inventory.Add("sword");
        var log = new TurnLog();

        shop.Sell("sword", log);

        Assert.Equal(new GameEvent[] { new ItemLostEvent(world.Content.Items["sword"], 1), new GoldGainedEvent(7, 7) }, log.Events);
        Assert.False(world.Player.Inventory.Contains("sword"));
    }

    [Fact]
    public void Sellables_SkipQuestItemsAndTheEquippedPieceButAllowSpares()
    {
        var (shop, world) = Open();
        var player = world.Player;
        player.Inventory.Add("relic");
        player.Inventory.Add("mail");
        player.EquippedArmorId = "mail";
        player.Inventory.Add("sword", 2);
        player.EquippedWeaponId = "sword";

        var sellables = shop.Sellables();

        Assert.Equal(new[] { ("sword", 7, 1) }, sellables.Select(s => (s.Item.Id, s.Price, s.Count)));
        Assert.Throws<ArgumentException>(() => shop.Sell("relic", new TurnLog()));
        Assert.Throws<ArgumentException>(() => shop.Sell("mail", new TurnLog()));

        shop.Sell("sword", new TurnLog());
        Assert.Equal("sword", player.EquippedWeaponId);
        Assert.Empty(shop.Sellables());
    }

    [Fact]
    public void Leave_EndsTheSession()
    {
        var (shop, _) = Open();

        Assert.False(shop.IsOver);
        shop.Leave();
        Assert.True(shop.IsOver);
    }

    [Fact]
    public void InvalidArguments_Throw()
    {
        var (shop, world) = Open();

        Assert.Throws<ArgumentException>(() => shop.Buy("dagger", new TurnLog()));
        Assert.Throws<ArgumentNullException>(() => shop.Buy("potion", null!));
        Assert.Throws<ArgumentNullException>(() => shop.Sell("potion", null!));
        Assert.Throws<ArgumentNullException>(() => new ShopSession(null!, world.Content.Shops["stall"], world.State));
        Assert.Throws<ArgumentNullException>(() => new ShopSession(world.Content, null!, world.State));
        Assert.Throws<ArgumentNullException>(() => new ShopSession(world.Content, world.Content.Shops["stall"], null!));
    }
}
