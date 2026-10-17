using RpgEngine.CLI.Rendering;
using RpgEngine.Core.Characters;
using RpgEngine.Core.Combat;
using RpgEngine.Core.Engine;
using Spectre.Console;

namespace RpgEngine.CLI.Screens;

/// <summary>The character sheet, inventory, journal, and help screens.</summary>
internal sealed class InfoScreens(UiContext ui)
{
    public void Stats(GameSession session)
    {
        var player = session.State.Player;
        var content = session.Content;
        var cls = content.Classes[player.ClassId];
        var weapon = CombatRules.Weapon(player, content);

        var vitals = new Grid().AddColumn().AddColumn();
        vitals.AddRow($"[{Theme.Muted}]Hit points[/]", Bars.Hp(player.CurrentHp, player.MaxHp, ui.Unicode));
        if (player.MaxMana > 0)
        {
            vitals.AddRow($"[{Theme.Muted}]Mana[/]", Bars.Mana(player.CurrentMana, player.MaxMana, ui.Unicode));
        }

        vitals.AddRow($"[{Theme.Muted}]Armor class[/]", $"{CombatRules.ArmorClass(player, content)}");
        vitals.AddRow($"[{Theme.Muted}]Proficiency[/]", RollText.Signed(player.ProficiencyBonus));
        vitals.AddRow($"[{Theme.Muted}]Weapon[/]", $"{Markup.Escape(weapon.Name)} {RollText.Signed(weapon.ToHitBonus)} to hit, {Markup.Escape(weapon.Damage.Text)} {weapon.DamageType.ToString().ToLowerInvariant()}");
        vitals.AddRow($"[{Theme.Muted}]Experience[/]", $"{player.Xp} XP");
        vitals.AddRow($"[{Theme.Muted}]Gold[/]", $"[{Theme.Gold}]{player.Gold}[/]");
        vitals.AddRow($"[{Theme.Muted}]Seed[/]", $"{ui.Seed} [{Theme.Muted}](replay with --seed {ui.Seed})[/]");

        ui.Console.Write(new Panel(vitals)
            .Header($" [{Theme.Title}]{Markup.Escape(player.Name)}[/] [{Theme.Muted}]level {player.Level} {Markup.Escape(cls.Name)}[/] ")
            .Border(BoxBorder.Rounded)
            .BorderStyle(Style.Parse(Theme.Accent)));

        var abilities = new Table().Border(TableBorder.Rounded).BorderStyle(Style.Parse(Theme.Muted));
        abilities.AddColumn("Ability").AddColumn("Score").AddColumn("Mod").AddColumn("Save");
        foreach (var ability in AbilityExtensions.All)
        {
            var save = player.Abilities.Modifier(ability) + (player.IsProficientInSave(ability) ? player.ProficiencyBonus : 0);
            abilities.AddRow(
                ability.DisplayName(),
                $"{player.Abilities[ability]}",
                RollText.Signed(player.Abilities.Modifier(ability)),
                player.IsProficientInSave(ability) ? $"[{Theme.Accent}]{RollText.Signed(save)}*[/]" : RollText.Signed(save));
        }

        var skills = new Table().Border(TableBorder.Rounded).BorderStyle(Style.Parse(Theme.Muted));
        skills.AddColumn("Skill").AddColumn("Bonus");
        foreach (var skill in content.SkillList)
        {
            var level = player.GetSkillProficiency(skill.Id);
            var bonus = player.Abilities.Modifier(skill.Ability) + ProficiencyRules.Bonus(level, player.ProficiencyBonus);
            var mark = level switch
            {
                ProficiencyLevel.Expertise => $" [{Theme.Accent}]**[/]",
                ProficiencyLevel.Proficient => $" [{Theme.Accent}]*[/]",
                _ => string.Empty,
            };
            skills.AddRow($"{Markup.Escape(skill.Name)} [{Theme.Muted}]({skill.Ability.Abbreviation()})[/]", RollText.Signed(bonus) + mark);
        }

        ui.Console.Write(new Columns(abilities, skills).Collapse());
        ui.Console.MarkupLine($"[{Theme.Muted}]* proficient   ** expertise[/]");

        if (player.SpellIds.Count > 0)
        {
            var spells = string.Join(", ", player.SpellIds.Select(id => content.Spells[id].Name));
            ui.Console.MarkupLine($"[{Theme.Muted}]Spells and techniques:[/] {Theme.Paint(Theme.Mana, spells)}");
        }
    }

    public void Inventory(GameSession session)
    {
        var player = session.State.Player;
        if (player.Inventory.IsEmpty)
        {
            ui.Say(Theme.System, "You are carrying nothing.");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded).BorderStyle(Style.Parse(Theme.Muted)).Title($"[{Theme.Muted}]Inventory[/]");
        table.AddColumn("Item").AddColumn("Qty").AddColumn("Type").AddColumn("");
        foreach (var entry in player.Inventory.Entries)
        {
            var item = session.Content.Items[entry.ItemId];
            var equipped = item.Id == player.EquippedWeaponId || item.Id == player.EquippedArmorId ? $"[{Theme.Accent}]equipped[/]" : string.Empty;
            table.AddRow(Theme.Paint(Theme.Gold, item.Name), $"{entry.Count}", item.Type.ToString().ToLowerInvariant(), equipped);
        }

        ui.Console.Write(table);
        ui.Console.MarkupLine($"[{Theme.Gold}]{player.Gold} gold[/]");
    }

    public void Journal(GameSession session)
    {
        var journal = session.Journal();
        if (journal.Quests.Count == 0 && journal.Reputation.Count == 0)
        {
            ui.Say(Theme.System, "Your journal is empty.");
            return;
        }

        foreach (var quest in journal.Quests)
        {
            var state = quest.Completed ? $"[{Theme.Success}](completed)[/]" : $"[{Theme.Muted}](in progress)[/]";
            ui.Console.Write(new Panel(new Markup(Theme.Paint(Theme.Narration, quest.Journal)))
                .Header($" [{Theme.Accent}]{Markup.Escape(quest.Name)}[/] {state} ")
                .Border(BoxBorder.Rounded)
                .BorderStyle(Style.Parse(Theme.Muted)));
        }

        foreach (var reputation in journal.Reputation)
        {
            var color = reputation.Value >= 0 ? Theme.Success : Theme.Failure;
            ui.Console.MarkupLine($"[{Theme.Muted}]Reputation:[/] {Markup.Escape(reputation.Faction)} [{color}]{RollText.Signed(reputation.Value)}[/]");
        }
    }

    public void Help()
    {
        var table = new Table().Border(TableBorder.Rounded).BorderStyle(Style.Parse(Theme.Muted)).Title($"[{Theme.Muted}]Commands[/]");
        table.AddColumn("Command").AddColumn("What it does");
        (string Command, string Meaning)[] rows =
        [
            ("1, 2, 3 ...", "Choose a numbered action"),
            ("look, l", "Describe the room again"),
            ("go north, n s e w u d", "Move"),
            ("inspect <thing>, x <thing>", "Examine something or someone"),
            ("<verb> <thing>", "Interact, e.g. 'read inscription', 'press sun'"),
            ("take / drop <item>", "Pick up or drop an item"),
            ("use <item> [on <thing>]", "Use an item"),
            ("equip <item>", "Wield a weapon or wear armor"),
            ("talk to <someone>", "Start a conversation"),
            ("inventory, i", "List what you carry"),
            ("stats, c", "Show your character sheet"),
            ("journal, j", "Show quests and reputation"),
            ("save [slot] / load [slot]", "Save or load (default slot: quicksave)"),
            ("help, ?", "Show this list"),
            ("quit", "Return to the title screen"),
        ];

        foreach (var (command, meaning) in rows)
        {
            table.AddRow(Theme.Paint(Theme.Accent, command), Markup.Escape(meaning));
        }

        ui.Console.Write(table);
    }
}
