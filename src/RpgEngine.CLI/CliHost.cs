using RpgEngine.CLI.Rendering;
using RpgEngine.Core.Content;
using RpgEngine.Core.Dice;
using Spectre.Console;

namespace RpgEngine.CLI;

/// <summary>Parses arguments, loads and validates content, and runs the game. Returns the exit code.</summary>
public static class CliHost
{
    public const int ExitOk = 0;
    public const int ExitContentError = 1;
    public const int ExitUsageError = 2;

    public static int Run(IReadOnlyList<string> args, IAnsiConsole console, Func<ulong, IRandomSource>? randomFactory = null, TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(console);

        var options = CliOptions.Parse(args, out var error);
        if (options is null)
        {
            console.MarkupLine(Theme.Paint(Theme.Failure, error!));
            console.WriteLine(CliOptions.Usage);
            return ExitUsageError;
        }

        if (options.ShowHelp)
        {
            console.WriteLine(CliOptions.Usage);
            return ExitOk;
        }

        GameContent content;
        try
        {
            content = ContentLoader.LoadFromDirectory(options.DataDirectory);
        }
        catch (ContentValidationException ex)
        {
            var lines = new List<Markup>
            {
                new($"The game content in [{Theme.Accent}]{Markup.Escape(options.DataDirectory)}[/] has {ex.Errors.Count} problem(s):"),
            };
            lines.AddRange(ex.Errors.Select(e => new Markup($"[{Theme.Failure}]-[/] {Markup.Escape(e)}")));
            console.Write(new Panel(new Rows(lines))
                .Header($" [{Theme.Danger}]Content errors[/] ")
                .Border(BoxBorder.Rounded)
                .BorderStyle(Style.Parse(Theme.Failure)));
            return ExitContentError;
        }

        return new GameApp(console, content, options, randomFactory, time).Run();
    }
}
