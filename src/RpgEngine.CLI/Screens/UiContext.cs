using RpgEngine.CLI.Input;
using RpgEngine.CLI.Rendering;
using RpgEngine.Core.Content;
using RpgEngine.Core.Persistence;
using Spectre.Console;

namespace RpgEngine.CLI.Screens;

/// <summary>Everything a screen needs: the console, input, rendering, content, and saves.</summary>
internal sealed class UiContext
{
    public UiContext(IAnsiConsole console, GameContent content, CliOptions options, SaveService saves)
    {
        Console = console;
        Content = content;
        Options = options;
        Saves = saves;
        Prompter = new Prompter(console);
        Animator = new DiceAnimator(console, options.Animations);
        Renderer = new EventRenderer(console, Animator);
    }

    public IAnsiConsole Console { get; }

    public GameContent Content { get; }

    public CliOptions Options { get; }

    public SaveService Saves { get; }

    public Prompter Prompter { get; }

    public DiceAnimator Animator { get; }

    public EventRenderer Renderer { get; }

    /// <summary>Seed of the current run, shown on the character sheet so a run can be replayed.</summary>
    public ulong Seed { get; set; }

    public bool Unicode => Console.Profile.Capabilities.Unicode;

    public void Say(string style, string text) => Console.MarkupLine(Theme.Paint(style, text));
}
