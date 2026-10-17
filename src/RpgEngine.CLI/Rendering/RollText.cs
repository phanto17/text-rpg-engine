using System.Globalization;
using RpgEngine.Core.Characters;
using RpgEngine.Core.Checks;
using RpgEngine.Core.Dice;
using Spectre.Console;

namespace RpgEngine.CLI.Rendering;

/// <summary>Formats check badges and roll breakdowns, e.g. <c>[Persuasion DC 14] d20 13 +3 CHA +2 prof = 18</c>.</summary>
internal static class RollText
{
    public static string Badge(string label, int dc, RollMode mode) =>
        $"[{Theme.Accent}][[{Markup.Escape(label)} DC {dc}]][/]" + ModeBadge(mode);

    public static string ModeBadge(RollMode mode) => mode switch
    {
        RollMode.Advantage => $" [{Theme.Success}](advantage)[/]",
        RollMode.Disadvantage => $" [{Theme.Failure}](disadvantage)[/]",
        _ => string.Empty,
    };

    public static string D20(D20Roll roll) => roll.Faces.Count > 1
        ? $"d20 [bold]{roll.Natural}[/] [{Theme.Muted}](rolled {roll.Faces[0]} and {roll.Faces[1]})[/]"
        : $"d20 [bold]{roll.Natural}[/]";

    public static string Check(CheckResult result)
    {
        var parts = $"{D20(result.Roll)} {Signed(result.AbilityModifier)} {result.Ability.Abbreviation()}";
        if (result.ProficiencyBonus != 0)
        {
            parts += $" {Signed(result.ProficiencyBonus)} prof";
        }

        var outcome = result switch
        {
            { IsCriticalSuccess: true } => $"[{Theme.Critical}]CRITICAL SUCCESS[/]",
            { IsCriticalFailure: true } => $"[{Theme.Failure}]CRITICAL FAILURE[/]",
            { Success: true } => $"[{Theme.Success}]SUCCESS[/]",
            _ => $"[{Theme.Failure}]FAILURE[/]",
        };

        return $"{Badge(result.Label, result.Dc, RollMode.Normal)} {parts} = [bold]{result.Total}[/]  {outcome}";
    }

    public static string Dice(RollResult roll)
    {
        var faces = roll.Dice.Count == 0
            ? string.Empty
            : "[" + string.Join(", ", roll.Dice.Select(d => d.Kept ? d.Value.ToString(CultureInfo.InvariantCulture) : $"({d.Value})")) + "]";
        var modifier = roll.Modifier == 0 || roll.Dice.Count == 0 ? string.Empty : $" {Signed(roll.Modifier)}";
        var shown = faces.Length == 0 ? roll.Total.ToString(CultureInfo.InvariantCulture) : $"{faces}{modifier}";
        return $"{Markup.Escape(roll.Expression.Text)}{(roll.Critical ? " (critical)" : string.Empty)}: {Markup.Escape(shown)} = [bold]{roll.Total}[/]";
    }

    public static string Signed(int value) => value < 0
        ? $"-{(-value).ToString(CultureInfo.InvariantCulture)}"
        : $"+{value.ToString(CultureInfo.InvariantCulture)}";
}
