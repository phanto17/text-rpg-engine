using RpgEngine.CLI.Rendering;
using RpgEngine.Core.Commands;
using RpgEngine.Core.Engine;
using RpgEngine.Core.Persistence;
using Spectre.Console;

namespace RpgEngine.CLI.Screens;

/// <summary>
/// One exploration turn: status line, numbered actions, then a prompt that accepts either a
/// number or a typed command (<c>look</c>, <c>inspect door</c>, <c>press sun</c>, <c>save</c>...).
/// </summary>
internal sealed class ExplorationScreen(UiContext ui, InfoScreens info)
{
    public const string DefaultSlot = "quicksave";

    public ScreenResult Turn(GameSession session)
    {
        var actions = session.ExplorationActions();
        StatusBar.Render(ui.Console, session);
        ui.Prompter.ShowMenu(actions.Select(a => Markup.Escape(a.Label)).ToList());
        ui.Console.MarkupLine($"[{Theme.Muted}]Type a number or a command ('help' lists them).[/]");

        var input = ui.Prompter.Ask();
        if (input.Length == 0)
        {
            return ScreenResult.Continue;
        }

        if (int.TryParse(input, out var number))
        {
            if (number >= 1 && number <= actions.Count)
            {
                ui.Renderer.Render(session.Execute(actions[number - 1].Command));
            }
            else
            {
                ui.Say(Theme.System, $"There is no option {number}.");
            }

            return ScreenResult.Continue;
        }

        var command = CommandParser.Parse(input);
        switch (command.Verb)
        {
            case CommandVerb.Inventory:
                info.Inventory(session);
                break;
            case CommandVerb.Stats:
                info.Stats(session);
                break;
            case CommandVerb.Journal:
                info.Journal(session);
                break;
            case CommandVerb.Help:
                info.Help();
                break;
            case CommandVerb.Save:
                Save(session, RawArgument(input) ?? DefaultSlot);
                break;
            case CommandVerb.Load:
                return ScreenResult.Load(RawArgument(input));
            case CommandVerb.Quit:
                return ui.Prompter.Confirm($"[{Theme.Warning}]Return to the title screen? Unsaved progress will be lost.[/]")
                    ? ScreenResult.Title
                    : ScreenResult.Continue;
            default:
                ui.Renderer.Render(session.Execute(command));
                break;
        }

        return ScreenResult.Continue;
    }

    // Slot names keep their exact spelling (e.g. "my-save"), so they bypass the word normalizer.
    private static string? RawArgument(string input)
    {
        var space = input.IndexOf(' ', StringComparison.Ordinal);
        var argument = space < 0 ? string.Empty : input[(space + 1)..].Trim();
        return argument.Length == 0 ? null : argument;
    }

    private void Save(GameSession session, string slot)
    {
        try
        {
            ui.Saves.Save(session.State, session.Content, slot);
            ui.Say(Theme.Success, $"Game saved to slot '{slot}'.");
        }
        catch (SaveGameException ex)
        {
            ui.Say(Theme.Failure, ex.Message);
        }
    }
}
