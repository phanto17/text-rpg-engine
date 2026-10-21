using RpgEngine.Core.Content;
using RpgEngine.Core.Persistence;
using RpgEngine.Pixel.Game;
using RpgEngine.Pixel.Presentation;
using RpgEngine.Pixel.Rendering;

namespace RpgEngine.Pixel;

/// <summary>Parses arguments, loads and validates content, and opens the window. Returns the exit code.</summary>
public static class PixelHost
{
    public const int ExitOk = 0;
    public const int ExitContentError = 1;
    public const int ExitUsageError = 2;

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter errors)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(errors);

        var options = PixelOptions.Parse(args, out var error);
        if (options is null)
        {
            errors.WriteLine(error);
            output.WriteLine(PixelOptions.Usage);
            return ExitUsageError;
        }

        if (options.ShowHelp)
        {
            output.WriteLine(PixelOptions.Usage);
            return ExitOk;
        }

        GameContent content;
        try
        {
            content = ContentLoader.LoadFromDirectory(options.DataDirectory);
        }
        catch (ContentValidationException ex)
        {
            errors.WriteLine($"The game content in {options.DataDirectory} has {ex.Errors.Count} problem(s):");
            foreach (var problem in ex.Errors)
            {
                errors.WriteLine($"- {problem}");
            }

            return ExitContentError;
        }

        var presentation = PresentationLoader.Load(options.DataDirectory);
        foreach (var warning in presentation.Warnings)
        {
            errors.WriteLine($"warning: {warning}");
        }

        var game = new PixelGame(content, new SaveService(options.SavesDirectory, TimeProvider.System), seed: options.Seed);
        var catalog = new PresentationCatalog(content, presentation.Presentation);
        using var app = new PixelApp(game, catalog, options.DataDirectory, options.Crt, options.Script, options.Screenshot);
        app.Run();
        return ExitOk;
    }
}
