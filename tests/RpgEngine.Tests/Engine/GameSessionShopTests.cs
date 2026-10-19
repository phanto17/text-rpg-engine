using RpgEngine.Core.Content;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Engine;
using RpgEngine.Core.Events;
using RpgEngine.Core.Commands;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Engine;

public sealed class GameSessionShopTests
{
    /// <summary>TestContent with a stall feature in the start room whose "trade" verb opens the shop.</summary>
    internal static GameContent ShopContent() => new TestContent()
        .With("Rooms/rooms.json", """
            [ { "id": "start", "name": "Market", "description": "Stalls.",
                "features": [ { "id": "stall", "name": "stall", "description": "A trestle of wares.",
                  "interactions": [ { "verb": "trade", "label": "Trade at the stall", "effects": [ { "type": "startShop", "shop": "stall" } ] } ] } ],
                "exits": [ { "direction": "north", "target": "hall" } ] },
              { "id": "hall", "name": "Hall", "description": "A hall.", "exits": [ { "direction": "south", "target": "start" } ] } ]
            """)
        .Load();

    private static GameSession Begin()
    {
        var content = ShopContent();
        var hero = new CharacterFactory(content).Create("Tess", "fighter", "bailiff", TestCharacters.Scores(), ["athletics", "perception"], []);
        var session = GameSession.NewGame(content, hero, new FixedRandomSource());
        session.Begin();
        return session;
    }

    [Fact]
    public void StartShop_EntersShopModeWithTheKeepersGreeting()
    {
        var session = Begin();

        var events = session.Execute(CommandParser.Parse("trade"));

        Assert.Equal(GameMode.Shop, session.Mode);
        Assert.Equal(new DialogueLineEvent("Mote", "Coin first, Tess."), events[^1]);
        Assert.False(session.CanSave);
        Assert.Equal("stall", session.Shop!.Shop.Id);
        Assert.Equal(new[] { "potion", "sword" }, session.ShopOffers().Select(o => o.Item.Id));
    }

    [Fact]
    public void ShopBuySellLeave_UpdateThePlayerAndReturnToExploration()
    {
        var session = Begin();
        session.Execute(CommandParser.Parse("trade"));

        Assert.Contains(new GoldSpentEvent(10, 5), session.ShopBuy("potion"));
        Assert.Equal(3, session.State.Player.Inventory.CountOf("potion"));
        Assert.Contains(session.ShopSellables(), s => s.Item.Id == "potion");
        Assert.Contains(new GoldGainedEvent(0, 5), session.ShopSell("potion"));

        Assert.Empty(session.ShopLeave());
        Assert.Equal(GameMode.Exploration, session.Mode);
        Assert.Null(session.Shop);
        Assert.True(session.CanSave);
        Assert.Equal(1, session.State.World.GetShopStock("stall", "potion"));
    }

    [Fact]
    public void ShopMethods_OutsideAShop_Throw()
    {
        var session = Begin();

        Assert.Throws<InvalidOperationException>(() => session.ShopOffers());
        Assert.Throws<InvalidOperationException>(() => session.ShopSellables());
        Assert.Throws<InvalidOperationException>(() => session.ShopBuy("potion"));
        Assert.Throws<InvalidOperationException>(() => session.ShopSell("potion"));
        Assert.Throws<InvalidOperationException>(() => session.ShopLeave());

        session.Execute(CommandParser.Parse("trade"));
        Assert.Throws<InvalidOperationException>(() => session.Execute(CommandParser.Parse("look")));
    }
}
