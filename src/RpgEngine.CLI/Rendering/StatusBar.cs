using RpgEngine.Core.Combat;
using RpgEngine.Core.Engine;
using Spectre.Console;

namespace RpgEngine.CLI.Rendering;

/// <summary>The one-line character header: name, class, HP and MP bars, AC, and gold.</summary>
internal static class StatusBar
{
    public static void Render(IAnsiConsole console, GameSession session)
    {
        var player = session.State.Player;
        var className = session.Content.Classes[player.ClassId].Name;
        var unicode = console.Profile.Capabilities.Unicode;
        var separator = $" [{Theme.Muted}]|[/] ";

        var parts = new List<string>
        {
            $"[{Theme.Title}]{Markup.Escape(player.Name)}[/] [{Theme.Muted}]the {Markup.Escape(className)}[/]",
            $"HP {Bars.Hp(player.CurrentHp, player.MaxHp, unicode, 10)}",
        };

        if (player.MaxMana > 0)
        {
            parts.Add($"MP {Bars.Mana(player.CurrentMana, player.MaxMana, unicode, 6)}");
        }

        parts.Add($"AC {CombatRules.ArmorClass(player, session.Content)}");
        parts.Add($"[{Theme.Gold}]{player.Gold} gp[/]");

        console.WriteLine();
        console.MarkupLine(string.Join(separator, parts));
    }
}
