using RpgEngine.CLI.Rendering;
using RpgEngine.Core.Engine;
using Spectre.Console;

namespace RpgEngine.CLI.Screens;

/// <summary>Numbered dialogue choices. Choices with a skill check show a <c>[Skill DC n]</c> badge.</summary>
internal sealed class DialogueScreen(UiContext ui)
{
    public ScreenResult Turn(GameSession session)
    {
        var choices = session.DialogueChoices();
        var labels = choices
            .Select(c => (c.CheckLabel is { } label ? RollText.Badge(label, c.Dc!.Value, c.Mode) + " " : string.Empty) + Theme.Paint(Theme.Dialogue, c.Text))
            .ToList();

        var index = ui.Prompter.Choose(null, labels);
        ui.Renderer.Render(session.ChooseDialogue(choices[index].Index));
        return ScreenResult.Continue;
    }
}
