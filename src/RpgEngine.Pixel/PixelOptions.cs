using System.Globalization;

namespace RpgEngine.Pixel;

/// <summary>
/// Command-line options, the same as the terminal client's where they overlap (<c>--seed</c>,
/// <c>--data</c>, <c>--saves</c>, <c>--help</c>), so a run can be replayed and a save shared between
/// the two clients, plus <c>--crt</c> to start with the scanline overlay on.
/// </summary>
public sealed record PixelOptions
{
    public ulong? Seed { get; init; }

    public string DataDirectory { get; init; } = Path.Combine(AppContext.BaseDirectory, "Data");

    public string SavesDirectory { get; init; } = Path.Combine(Environment.CurrentDirectory, "Saves");

    public bool Crt { get; init; }

    public bool ShowHelp { get; init; }

    /// <summary>Contributor tool: write the canvas to this PNG once the script has played, then exit.</summary>
    public string? Screenshot { get; init; }

    /// <summary>Contributor tool: write native canvas frames at 12 fps to a fresh directory.</summary>
    public string? RecordingDirectory { get; init; }

    /// <summary>Contributor tool: lines typed into the game one after another before the screenshot.</summary>
    public IReadOnlyList<string> Script { get; init; } = [];

    public const string Usage = """
        Usage: RpgEngine.Pixel [options]

          --seed <n>      Use a fixed random seed (reproducible runs)
          --data <dir>    Load game content from another folder
          --saves <dir>   Store save files in another folder (default: ./Saves)
          --crt           Start with the CRT scanline overlay on (F2 toggles it)
          --help          Show this help

        In the window: click a button, press 1-9, or type a command. Tab cycles the HUD tabs,
        Enter skips the typewriter, Esc goes back, F2 toggles the CRT, F3 toggles whole-pixel
        scaling, F11 toggles borderless fullscreen.
        """;

    /// <summary>Parses arguments; returns null and sets <paramref name="error"/> when they are invalid.</summary>
    public static PixelOptions? Parse(IReadOnlyList<string> args, out string? error)
    {
        ArgumentNullException.ThrowIfNull(args);
        var options = new PixelOptions();
        error = null;

        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--seed":
                    if (!TryValue(args, ref i, out var seedText) || !ulong.TryParse(seedText, NumberStyles.None, CultureInfo.InvariantCulture, out var seed))
                    {
                        error = "--seed needs a non-negative whole number.";
                        return null;
                    }

                    options = options with { Seed = seed };
                    break;
                case "--data":
                    if (!TryValue(args, ref i, out var data))
                    {
                        error = "--data needs a folder path.";
                        return null;
                    }

                    options = options with { DataDirectory = data };
                    break;
                case "--saves":
                    if (!TryValue(args, ref i, out var saves))
                    {
                        error = "--saves needs a folder path.";
                        return null;
                    }

                    options = options with { SavesDirectory = saves };
                    break;
                case "--crt":
                    options = options with { Crt = true };
                    break;
                case "--screenshot":
                    if (!TryValue(args, ref i, out var screenshot))
                    {
                        error = "--screenshot needs a file path.";
                        return null;
                    }

                    options = options with { Screenshot = screenshot };
                    break;
                case "--play":
                    if (!TryValue(args, ref i, out var script))
                    {
                        error = "--play needs a semicolon-separated list of lines.";
                        return null;
                    }

                    options = options with { Script = script.Split(';', StringSplitOptions.TrimEntries) };
                    break;
                case "--record":
                    if (!TryValue(args, ref i, out var recording))
                    {
                        error = "--record needs a fresh directory path.";
                        return null;
                    }
                    options = options with { RecordingDirectory = recording };
                    break;
                case "--help" or "-h" or "-?":
                    options = options with { ShowHelp = true };
                    break;
                default:
                    error = $"Unknown option '{args[i]}'.";
                    return null;
            }
        }

        if (options.Screenshot is not null && options.RecordingDirectory is not null)
        {
            error = "Use either --screenshot or --record.";
            return null;
        }
        return options;
    }

    private static bool TryValue(IReadOnlyList<string> args, ref int index, out string value)
    {
        if (index + 1 < args.Count && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = args[++index];
            return true;
        }

        value = string.Empty;
        return false;
    }
}
