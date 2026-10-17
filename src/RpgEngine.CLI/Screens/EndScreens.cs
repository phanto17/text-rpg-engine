using RpgEngine.CLI.Rendering;

namespace RpgEngine.CLI.Screens;

/// <summary>The menus shown after death (load, title, quit) and after the epilogue (title, quit).</summary>
internal sealed class EndScreens(UiContext ui)
{
    public ScreenResult GameOver()
    {
        var options = new List<(string Label, ScreenResult Result)>();
        if (ui.Saves.ListSlots().Count > 0)
        {
            options.Add(("Load a saved game", ScreenResult.Load(null)));
        }

        options.Add(("Return to the title screen", ScreenResult.Title));
        options.Add(("Quit", ScreenResult.Quit));
        return options[ui.Prompter.Choose(null, options.Select(o => o.Label).ToList())].Result;
    }

    public ScreenResult Ended()
    {
        ui.Say(Theme.Accent, "Thank you for playing.");
        return ui.Prompter.Choose(null, ["Return to the title screen", "Quit"]) == 0 ? ScreenResult.Title : ScreenResult.Quit;
    }
}
