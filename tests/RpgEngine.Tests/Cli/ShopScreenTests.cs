using System.Text.RegularExpressions;
using RpgEngine.CLI;
using RpgEngine.Tests.Engine;
using RpgEngine.Tests.TestSupport;
using Spectre.Console.Testing;

namespace RpgEngine.Tests.Cli;

public sealed partial class ShopScreenTests
{
    [Fact]
    public void ShopScreen_BuysSellsAndLeaves()
    {
        var saves = Path.Combine(Path.GetTempPath(), "rpg-shop-saves-" + Guid.NewGuid().ToString("N"));
        using var console = new TestConsole().Interactive();
        console.Profile.Width = 1000;
        string[] inputs =
        [
            "1", "Tess",            // new game, name
            "1", "2",               // Fighter, Bailiff (persuasion, +10 gold: 15 in all)
            "1", "1", "1 2", "y",   // standard array, keep, athletics + perception, begin
            "trade",                // open the stall
            "1", "1",               // buy a potion (10 gp)
            "1", "2",               // try a sword (15 gp, too dear)
            "2", "1",               // sell a potion (worth nothing)
            "2", "0",               // open the sell menu and back out
            "3",                    // leave
            "quit", "y",            // back to the title
            "2",                    // quit
        ];
        foreach (var input in inputs)
        {
            console.Input.PushTextWithEnter(input);
        }

        try
        {
            var app = new GameApp(console, GameSessionShopTests.ShopContent(), new CliOptions { Animations = false, Seed = 7, SavesDirectory = saves }, _ => new LuckyRandomSource());
            Assert.Equal(0, app.Run());
        }
        finally
        {
            if (Directory.Exists(saves))
            {
                Directory.Delete(saves, recursive: true);
            }
        }

        var output = Whitespace().Replace(console.Output, " ");
        Assert.Contains("Mote", output);
        Assert.Contains("Coin first, Tess.", output);
        Assert.Contains("The Stall", output);
        Assert.Contains("-10 gold (5 left)", output);
        Assert.Contains("+ Potion of Healing", output);
        Assert.Contains("Longsword (15 gp) - too dear", output);
        Assert.Contains("You can't afford the Longsword (15 gold).", output);
        Assert.Contains("- Potion of Healing", output);
        Assert.Contains("+0 gold", output);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
