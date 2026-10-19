using RpgEngine.CLI.Rendering;
using RpgEngine.Core.Engine;
using Spectre.Console;

namespace RpgEngine.CLI.Screens;

/// <summary>A merchant's stall: the wares on offer, then Buy, Sell, or Leave (sub-menus can go back with 0).</summary>
internal sealed class ShopScreen(UiContext ui)
{
    public ScreenResult Turn(GameSession session)
    {
        var shop = session.Shop!.Shop;
        StatusBar.Render(ui.Console, session);
        RenderOffers(session);

        var choice = ui.Prompter.Choose($"[{Theme.Accent}]{Markup.Escape(shop.Name)}:[/] [{Theme.Muted}]what will you do?[/]", ["Buy", "Sell", "Leave"]);
        switch (choice)
        {
            case 0:
                Buy(session);
                break;
            case 1:
                Sell(session);
                break;
            default:
                ui.Renderer.Render(session.ShopLeave());
                break;
        }

        return ScreenResult.Continue;
    }

    private void RenderOffers(GameSession session)
    {
        var offers = session.ShopOffers();
        if (offers.Count == 0)
        {
            ui.Say(Theme.Muted, "The shelves are bare.");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded).BorderStyle(Style.Parse(Theme.Muted)).Title($"[{Theme.Title}]{Markup.Escape(session.Shop!.Shop.Name)}[/]");
        table.AddColumn("Item").AddColumn("Price").AddColumn("Stock").AddColumn("Description");
        foreach (var offer in offers)
        {
            table.AddRow(
                Theme.Paint(Theme.Gold, offer.Item.Name),
                $"{offer.Price} gp",
                offer.Remaining is { } remaining ? $"{remaining}" : $"[{Theme.Muted}]plenty[/]",
                Markup.Escape(offer.Item.Description));
        }

        ui.Console.Write(table);
    }

    private void Buy(GameSession session)
    {
        var offers = session.ShopOffers();
        if (offers.Count == 0)
        {
            ui.Say(Theme.System, "There is nothing left to buy.");
            return;
        }

        var labels = offers
            .Select(o => o.CanAfford
                ? $"{Theme.Paint(Theme.Gold, o.Item.Name)} [{Theme.Muted}]({o.Price} gp)[/]"
                : $"[{Theme.Muted}]{Markup.Escape(o.Item.Name)} ({o.Price} gp) - too dear[/]")
            .ToList();
        var index = ui.Prompter.Choose($"[{Theme.Accent}]Buy which?[/]", labels, allowBack: true);
        if (index >= 0)
        {
            ui.Renderer.Render(session.ShopBuy(offers[index].Item.Id));
        }
    }

    private void Sell(GameSession session)
    {
        var sellables = session.ShopSellables();
        if (sellables.Count == 0)
        {
            ui.Say(Theme.System, "You have nothing the merchant will buy.");
            return;
        }

        var labels = sellables
            .Select(s => $"{Theme.Paint(Theme.Gold, s.Item.Name)}{(s.Count > 1 ? $" x{s.Count}" : string.Empty)} [{Theme.Muted}](sells for {s.Price} gp)[/]")
            .ToList();
        var index = ui.Prompter.Choose($"[{Theme.Accent}]Sell which?[/]", labels, allowBack: true);
        if (index >= 0)
        {
            ui.Renderer.Render(session.ShopSell(sellables[index].Item.Id));
        }
    }
}
