using RpgEngine.CLI.Rendering;
using RpgEngine.Core.Events;
using RpgEngine.Tests.TestSupport;
using Spectre.Console.Testing;

namespace RpgEngine.Tests.Cli;

/// <summary>
/// The renderer's switch has no default case, so an event it forgets prints nothing. These tests
/// pin the output of each event added after Milestone 1.
/// </summary>
public sealed class EventRendererTests
{
    private static string Render(params GameEvent[] events)
    {
        var console = new TestConsole();
        console.Profile.Width = 200;
        new EventRenderer(console, new DiceAnimator(console, enabled: false)) { PlayerName = "Tamsin" }.Render(events);
        return console.Output;
    }

    [Fact]
    public void Render_Rested_ShowsWhatWasRestored()
    {
        Assert.Contains("Rested. (+9 HP, +4 MP)", Render(new RestedEvent(9, 4, 12, 12, 6, 6)));
        Assert.Contains("Rested. (+3 HP)", Render(new RestedEvent(3, 0, 10, 10, 0, 0)));
    }

    [Fact]
    public void Render_GoldSpent_ShowsWhatIsLeft()
    {
        Assert.Contains("-10 gold (5 left)", Render(new GoldSpentEvent(10, 5)));
    }

    [Fact]
    public void Render_LeveledUp_ShowsAPanelWithGainsAndNewSpells()
    {
        var spell = new TestContent().Load().Spells["second_wind"];

        var output = Render(new LeveledUpEvent(2, 8, 20, 2, 8, [spell], 2));

        Assert.Contains("Level 2!", output);
        Assert.Contains("+8 (max 20)", output);
        Assert.Contains("+2 (max 8)", output);
        Assert.Contains("Learned", output);
        Assert.Contains("Second Wind", output);
        Assert.DoesNotContain("Mana", Render(new LeveledUpEvent(2, 8, 20, 0, 0, [], 2)));
    }
}
