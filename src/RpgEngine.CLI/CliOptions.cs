using System.Globalization;

namespace RpgEngine.CLI;

/// <summary>
/// Command-line options: <c>--seed &lt;n&gt;</c>, <c>--no-anim</c>, <c>--data &lt;dir&gt;</c>, <c>--saves &lt;dir&gt;</c>, <c>--help</c>,
/// and the contributor-only <c>--export-schemas [dir]</c>, which is left out of the player-facing usage text.
/// </summary>
public sealed record CliOptions
{
    public ulong? Seed { get; init; }

    public bool Animations { get; init; } = true;

    public string DataDirectory { get; init; } = Path.Combine(AppContext.BaseDirectory, "Data");

    public string SavesDirectory { get; init; } = Path.Combine(Environment.CurrentDirectory, "Saves");

    public bool ShowHelp { get; init; }

    /// <summary>Workspace root to write the content JSON Schemas under, or null to play.</summary>
    public string? ExportSchemasRoot { get; init; }

    public const string Usage = """
        Usage: RpgEngine.CLI [options]

          --seed <n>      Use a fixed random seed (reproducible runs)
          --no-anim       Skip dice animations and pauses
          --data <dir>    Load game content from another folder
          --saves <dir>   Store save files in another folder (default: ./Saves)
          --help          Show this help
        """;

    /// <summary>Parses arguments; returns null and sets <paramref name="error"/> when they are invalid.</summary>
    public static CliOptions? Parse(IReadOnlyList<string> args, out string? error)
    {
        ArgumentNullException.ThrowIfNull(args);
        var options = new CliOptions();
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
                case "--no-anim":
                    options = options with { Animations = false };
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
                case "--export-schemas":
                    options = options with { ExportSchemasRoot = TryValue(args, ref i, out var root) ? root : Environment.CurrentDirectory };
                    break;
                case "--help" or "-h" or "-?":
                    options = options with { ShowHelp = true };
                    break;
                default:
                    error = $"Unknown option '{args[i]}'.";
                    return null;
            }
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
