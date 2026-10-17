using RpgEngine.CLI.Rendering;
using RpgEngine.Core.Characters;
using RpgEngine.Core.Combat;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Engine;
using Spectre.Console;

namespace RpgEngine.CLI.Screens;

/// <summary>Name, class, ability scores (standard array or 4d6 drop lowest), skills, expertise, and a final review.</summary>
internal sealed class CharacterCreationScreen(UiContext ui)
{
    private readonly CharacterFactory _factory = new(ui.Content);

    public PlayerCharacter Run(DiceRoller dice)
    {
        while (true)
        {
            ui.Console.WriteLine();
            ui.Console.Write(new Rule($"[{Theme.Title}]Create your character[/]").RuleStyle(Theme.Accent));

            var name = AskName();
            var cls = ChooseClass();
            var scores = ChooseScores(cls, dice);
            var skills = ChooseSkills(cls);
            var expertise = ChooseExpertise(cls, skills);
            var player = _factory.Create(name, cls.Id, scores, skills, expertise);

            ShowSummary(player, cls);
            if (ui.Prompter.Confirm($"[{Theme.Accent}]Begin your adventure as {Markup.Escape(player.Name)}?[/]"))
            {
                return player;
            }
        }
    }

    private string AskName()
    {
        while (true)
        {
            var name = ui.Prompter.Ask($"[{Theme.Accent}]What is your name?[/]");
            if (name.Length is > 0 and <= CharacterFactory.MaxNameLength)
            {
                return name;
            }

            ui.Say(Theme.System, $"A name needs 1 to {CharacterFactory.MaxNameLength} characters.");
        }
    }

    private ClassDef ChooseClass()
    {
        var classes = ui.Content.ClassList;
        var table = new Table().Border(TableBorder.Rounded).BorderStyle(Style.Parse(Theme.Muted));
        table.AddColumn("#").AddColumn("Class").AddColumn("Hit die").AddColumn("Description");
        for (var i = 0; i < classes.Count; i++)
        {
            var cls = classes[i];
            table.AddRow($"[{Theme.Accent}]{i + 1}[/]", Theme.Paint(Theme.Title, cls.Name), $"d{cls.HitDie}", Markup.Escape(cls.Description));
        }

        ui.Console.Write(table);
        while (true)
        {
            var input = ui.Prompter.Ask($"[{Theme.Accent}]Choose a class (1-{classes.Count}):[/]");
            if (Input.Prompter.TryParseChoice(input, classes.Count, out var index))
            {
                return classes[index];
            }

            var byName = classes.FirstOrDefault(c => string.Equals(c.Name, input, StringComparison.OrdinalIgnoreCase) || c.Id == input.ToLowerInvariant());
            if (byName is not null)
            {
                return byName;
            }
        }
    }

    private AbilityScores ChooseScores(ClassDef cls, DiceRoller dice)
    {
        var method = ui.Prompter.Choose(
            $"[{Theme.Accent}]How will you determine your ability scores?[/]",
            [$"Standard array ({string.Join(", ", _factory.StandardArray)})", "Roll 4d6 and drop the lowest die, six times"]);

        IReadOnlyList<int> values;
        if (method == 0)
        {
            values = _factory.StandardArray;
        }
        else
        {
            var rolls = _factory.RollAbilityScores(dice);
            foreach (var roll in rolls)
            {
                ui.Animator.Roll("4d6", 4, 6);
                ui.Console.MarkupLine($"  {RollText.Dice(roll)}");
            }

            values = rolls.Select(r => r.Total).ToList();
        }

        var scores = CharacterFactory.AssignByPriority(values, cls);
        ShowScores(scores, $"Suggested for a {cls.Name}");

        var keep = ui.Prompter.Choose(null, ["Keep these scores", "Assign them myself"]);
        return keep == 0 ? scores : AssignManually(values);
    }

    private AbilityScores AssignManually(IReadOnlyList<int> values)
    {
        var remaining = values.OrderByDescending(v => v).ToList();
        var map = new Dictionary<Ability, int>();
        foreach (var ability in AbilityExtensions.All)
        {
            var index = ui.Prompter.Choose(
                $"[{Theme.Accent}]Score for {ability.DisplayName()}:[/]",
                remaining.Select(v => $"{v} ({RollText.Signed(AbilityScores.ModifierFor(v))})").ToList());
            map[ability] = remaining[index];
            remaining.RemoveAt(index);
        }

        var scores = AbilityScores.FromMap(map);
        ShowScores(scores, "Your scores");
        return scores;
    }

    private void ShowScores(AbilityScores scores, string title)
    {
        var table = new Table().Border(TableBorder.Rounded).BorderStyle(Style.Parse(Theme.Muted)).Title($"[{Theme.Muted}]{Markup.Escape(title)}[/]");
        foreach (var ability in AbilityExtensions.All)
        {
            table.AddColumn(new TableColumn($"[{Theme.Accent}]{ability.Abbreviation()}[/]").Centered());
        }

        table.AddRow(AbilityExtensions.All.Select(a => $"[bold]{scores[a]}[/] ({RollText.Signed(scores.Modifier(a))})").ToArray());
        ui.Console.Write(table);
    }

    private List<string> ChooseSkills(ClassDef cls)
    {
        var options = cls.SkillChoices.From.Select(id => ui.Content.Skills[id]).ToList();
        return PickMany(
            $"[{Theme.Accent}]Choose {cls.SkillChoices.Count} skills you are trained in[/] [{Theme.Muted}](numbers separated by spaces or commas)[/]",
            options.Select(s => $"{Markup.Escape(s.Name)} [{Theme.Muted}]({s.Ability.Abbreviation()})[/]").ToList(),
            cls.SkillChoices.Count)
            .Select(index => options[index].Id)
            .ToList();
    }

    private List<string> ChooseExpertise(ClassDef cls, List<string> skills)
    {
        if (cls.ExpertiseCount == 0)
        {
            return [];
        }

        var ordered = cls.SkillChoices.From.Where(skills.Contains).ToList();
        return PickMany(
            $"[{Theme.Accent}]Choose {cls.ExpertiseCount} of them for expertise[/] [{Theme.Muted}](double proficiency bonus)[/]",
            ordered.Select(id => Markup.Escape(ui.Content.Skills[id].Name)).ToList(),
            cls.ExpertiseCount)
            .Select(index => ordered[index])
            .ToList();
    }

    private List<int> PickMany(string titleMarkup, IReadOnlyList<string> optionsMarkup, int count)
    {
        ui.Console.MarkupLine(titleMarkup);
        ui.Prompter.ShowMenu(optionsMarkup);
        while (true)
        {
            var parts = ui.Prompter.Ask().Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
            var picks = new List<int>();
            foreach (var part in parts)
            {
                if (Input.Prompter.TryParseChoice(part, optionsMarkup.Count, out var index) && !picks.Contains(index))
                {
                    picks.Add(index);
                }
                else
                {
                    picks.Clear();
                    break;
                }
            }

            if (picks.Count == count && parts.Length == count)
            {
                return picks;
            }

            ui.Say(Theme.System, $"Pick exactly {count} different numbers from 1 to {optionsMarkup.Count}.");
        }
    }

    private void ShowSummary(PlayerCharacter player, ClassDef cls)
    {
        var skills = string.Join(", ", player.ProficientSkills
            .Select(id => ui.Content.Skills[id].Name + (player.ExpertiseSkills.Contains(id) ? " (expertise)" : string.Empty))
            .Order(StringComparer.Ordinal));
        var gear = string.Join(", ", player.Inventory.Entries.Select(e => ui.Content.Items[e.ItemId].Name + (e.Count > 1 ? $" x{e.Count}" : string.Empty)));
        var abilities = string.Join("  ", AbilityExtensions.All.Select(a => $"{a.Abbreviation()} {player.Abilities[a]}"));

        var grid = new Grid().AddColumn().AddColumn();
        grid.AddRow($"[{Theme.Muted}]Class[/]", Markup.Escape(cls.Name));
        grid.AddRow($"[{Theme.Muted}]Abilities[/]", abilities);
        grid.AddRow($"[{Theme.Muted}]Hit points[/]", $"{player.MaxHp}");
        if (player.MaxMana > 0)
        {
            grid.AddRow($"[{Theme.Muted}]Mana[/]", $"{player.MaxMana}");
        }

        grid.AddRow($"[{Theme.Muted}]Armor class[/]", $"{CombatRules.ArmorClass(player, ui.Content)}");
        grid.AddRow($"[{Theme.Muted}]Skills[/]", Markup.Escape(skills));
        grid.AddRow($"[{Theme.Muted}]Gear[/]", Markup.Escape(gear));
        grid.AddRow($"[{Theme.Muted}]Gold[/]", $"{player.Gold}");

        ui.Console.Write(new Panel(grid).Header($" [{Theme.Title}]{Markup.Escape(player.Name)}[/] ").Border(BoxBorder.Rounded).BorderStyle(Style.Parse(Theme.Accent)));
    }
}
