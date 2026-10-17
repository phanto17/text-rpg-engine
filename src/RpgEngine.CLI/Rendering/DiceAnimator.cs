using Spectre.Console;

namespace RpgEngine.CLI.Rendering;

/// <summary>
/// Flickers dice faces before a result is shown. Purely cosmetic: the flicker uses its own
/// randomness and never touches the game's seeded dice. Disabled with <c>--no-anim</c>.
/// </summary>
internal sealed class DiceAnimator(IAnsiConsole console, bool enabled)
{
    private const int Frames = 10;

    public bool Enabled => enabled && console.Profile.Capabilities.Interactive;

    public void Roll(string label, int diceCount, int sides)
    {
        if (!Enabled)
        {
            return;
        }

        console.Live(new Markup(string.Empty)).AutoClear(true).Start(context =>
        {
            for (var frame = 0; frame < Frames; frame++)
            {
                var faces = string.Join("  ", Enumerable.Range(0, Math.Max(1, diceCount)).Select(_ => Random.Shared.Next(1, sides + 1).ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(2)));
                context.UpdateTarget(new Markup($"[{Theme.Muted}]{Markup.Escape(label)}[/]  [bold]{faces}[/]"));
                context.Refresh();
                Thread.Sleep(35 + (frame * 6));
            }
        });
    }

    /// <summary>A short beat between enemy actions so combat can be followed.</summary>
    public void Pause(int milliseconds)
    {
        if (Enabled)
        {
            Thread.Sleep(milliseconds);
        }
    }
}
