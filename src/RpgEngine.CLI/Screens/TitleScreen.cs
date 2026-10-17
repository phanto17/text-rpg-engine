using RpgEngine.CLI.Rendering;
using Spectre.Console;

namespace RpgEngine.CLI.Screens;

internal enum TitleChoice
{
    NewGame,
    Continue,
    Quit,
}

internal sealed class TitleScreen(UiContext ui)
{
    public TitleChoice Show()
    {
        var console = ui.Console;
        console.WriteLine();
        console.Write(new FigletText(ui.Content.Manifest.Title).Color(Color.FromHex("#d7af5f")));
        console.MarkupLine($"[{Theme.Muted}]content v{Markup.Escape(ui.Content.Manifest.ContentVersion)}[/]");
        console.WriteLine();

        var hasSaves = ui.Saves.ListSlots().Count > 0;
        var choices = new List<(string Label, TitleChoice Choice)> { ("New game", TitleChoice.NewGame) };
        if (hasSaves)
        {
            choices.Add(("Continue a saved game", TitleChoice.Continue));
        }

        choices.Add(("Quit", TitleChoice.Quit));
        var index = ui.Prompter.Choose(null, choices.Select(c => c.Label).ToList());
        return choices[index].Choice;
    }
}
