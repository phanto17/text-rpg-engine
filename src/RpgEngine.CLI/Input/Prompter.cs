using RpgEngine.CLI.Rendering;
using Spectre.Console;

namespace RpgEngine.CLI.Input;

/// <summary>
/// Line-based input: free text, numbered menus, and yes/no questions. Every prompt reads a typed
/// line (no arrow-key widgets), which keeps the retro feel and lets tests script input.
/// </summary>
internal sealed class Prompter(IAnsiConsole console)
{
    public string Ask(string promptMarkup = "[#d7af5f]>[/]") =>
        console.Prompt(new TextPrompt<string>(promptMarkup).AllowEmpty()).Trim();

    public void ShowMenu(IReadOnlyList<string> optionsMarkup, bool allowBack = false)
    {
        for (var i = 0; i < optionsMarkup.Count; i++)
        {
            console.MarkupLine($"  [{Theme.Accent}]{i + 1,2})[/] {optionsMarkup[i]}");
        }

        if (allowBack)
        {
            console.MarkupLine($"  [{Theme.Muted}] 0) Back[/]");
        }
    }

    /// <summary>Shows a numbered menu and returns the chosen index, or -1 for Back.</summary>
    public int Choose(string? titleMarkup, IReadOnlyList<string> optionsMarkup, bool allowBack = false)
    {
        if (titleMarkup is not null)
        {
            console.MarkupLine(titleMarkup);
        }

        ShowMenu(optionsMarkup, allowBack);
        while (true)
        {
            var input = Ask();
            if (TryParseChoice(input, optionsMarkup.Count, out var index))
            {
                return index;
            }

            if (allowBack && input == "0")
            {
                return -1;
            }

            console.MarkupLine($"[{Theme.System}]Enter a number from 1 to {optionsMarkup.Count}{(allowBack ? ", or 0 to go back" : string.Empty)}.[/]");
        }
    }

    public bool Confirm(string questionMarkup)
    {
        while (true)
        {
            var answer = Ask($"{questionMarkup} [{Theme.Muted}](y/n)[/]").ToLowerInvariant();
            if (answer is "y" or "yes")
            {
                return true;
            }

            if (answer is "n" or "no")
            {
                return false;
            }
        }
    }

    public static bool TryParseChoice(string input, int count, out int index)
    {
        index = -1;
        if (int.TryParse(input, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number) && number >= 1 && number <= count)
        {
            index = number - 1;
            return true;
        }

        return false;
    }
}
