using RpgEngine.CLI.Rendering;
using RpgEngine.Core.Combat;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Engine;
using Spectre.Console;

namespace RpgEngine.CLI.Screens;

/// <summary>
/// Wizardry-style combat: a battlefield table, then a numbered action menu (attack, spells and
/// techniques, items, defend, parley, flee) with sub-menus for targets and options.
/// </summary>
internal sealed class CombatScreen(UiContext ui)
{
    public ScreenResult Turn(GameSession session)
    {
        var combat = session.Combat!;
        if (!combat.IsPlayerTurn)
        {
            ui.Animator.Pause(400);
            ui.Renderer.Render(session.CombatEnemyTurn());
            return ScreenResult.Continue;
        }

        RenderBattlefield(combat);
        var menu = BuildMenu(session, combat);
        var index = ui.Prompter.Choose($"[{Theme.Accent}]What do you do?[/]", menu.Select(m => m.Label).ToList());
        var action = menu[index].Build();
        if (action is not null)
        {
            ui.Renderer.Render(session.CombatAct(action));
        }

        return ScreenResult.Continue;
    }

    private void RenderBattlefield(CombatSession combat)
    {
        var table = new Table().Border(TableBorder.Rounded).BorderStyle(Style.Parse(Theme.Muted));
        table.AddColumn("Combatant").AddColumn("HP").AddColumn("AC").AddColumn("Conditions");
        foreach (var view in combat.Snapshot().Where(v => v.IsActive))
        {
            table.AddRow(
                Theme.Paint(view.IsPlayer ? Theme.Accent : Theme.Title, view.Name),
                Bars.Hp(view.Hp, view.MaxHp, ui.Unicode, 10),
                $"{view.ArmorClass}",
                view.Statuses.Count == 0 ? $"[{Theme.Muted}]-[/]" : Theme.Paint(Theme.Warning, string.Join(", ", view.Statuses)));
        }

        ui.Console.Write(table);
    }

    private List<(string Label, Func<PlayerAction?> Build)> BuildMenu(GameSession session, CombatSession combat)
    {
        var menu = new List<(string Label, Func<PlayerAction?> Build)>
        {
            ("Attack", () => ChooseTarget(combat) is { } target ? new AttackAction(target) : null),
        };

        var spells = combat.SpellOptions();
        if (spells.Count > 0)
        {
            menu.Add(("Spells and techniques", () => ChooseSpell(combat)));
        }

        var items = combat.UsableItems();
        if (items.Count > 0)
        {
            menu.Add(("Use an item", () => ChooseItem(session, combat)));
        }

        var defend = session.Content.StatusEffects[session.Content.Rules.DefendStatus];
        menu.Add(($"Defend [{Theme.Muted}]({Markup.Escape(defend.Description)})[/]", () => new DefendAction()));

        var parley = combat.ParleyOptions();
        if (parley.Count > 0)
        {
            menu.Add(("Parley", () => ChooseParley(combat)));
        }

        if (combat.CanFlee)
        {
            var flee = session.Content.Rules.Flee;
            menu.Add(($"Flee {RollText.Badge(session.Checks.Label(flee), flee.Dc, session.Checks.ModeFor(flee, session.State))}", () => new FleeAction()));
        }

        return menu;
    }

    private string? ChooseTarget(CombatSession combat)
    {
        var enemies = combat.ActiveEnemies;
        if (enemies.Count == 1)
        {
            return enemies[0].Id;
        }

        var index = ui.Prompter.Choose(
            $"[{Theme.Accent}]Target:[/]",
            enemies.Select(e => $"{Theme.Paint(Theme.Title, e.Name)} [{Theme.Muted}](HP {e.CurrentHp}/{e.MaxHp}, AC {e.ArmorClass})[/]").ToList(),
            allowBack: true);
        return index < 0 ? null : enemies[index].Id;
    }

    private PlayerAction? ChooseSpell(CombatSession combat)
    {
        var spells = combat.SpellOptions();
        var labels = spells
            .Select(s => s.CanCast
                ? $"{Theme.Paint(Theme.Mana, s.Spell.Name)} [{Theme.Muted}]({Markup.Escape(s.CostText)})[/] - {Markup.Escape(s.Spell.Description)}"
                : $"[{Theme.Muted}]{Markup.Escape(s.Spell.Name)} ({Markup.Escape(s.CostText)}) - not available[/]")
            .ToList();

        var index = ui.Prompter.Choose($"[{Theme.Accent}]Use which?[/]", labels, allowBack: true);
        if (index < 0)
        {
            return null;
        }

        var spell = spells[index];
        if (!spell.CanCast)
        {
            ui.Say(Theme.System, $"You can't use {spell.Spell.Name} right now.");
            return null;
        }

        if (spell.Spell.Target == SpellTarget.Self)
        {
            return new CastAction(spell.Spell.Id, null);
        }

        return ChooseTarget(combat) is { } target ? new CastAction(spell.Spell.Id, target) : null;
    }

    private PlayerAction? ChooseItem(GameSession session, CombatSession combat)
    {
        var items = combat.UsableItems();
        var labels = items
            .Select(i => $"{Theme.Paint(Theme.Gold, i.Name)} x{session.State.Player.Inventory.CountOf(i.Id)} [{Theme.Muted}]- {Markup.Escape(i.Description)}[/]")
            .ToList();
        var index = ui.Prompter.Choose($"[{Theme.Accent}]Use which item?[/]", labels, allowBack: true);
        if (index < 0)
        {
            return null;
        }

        var item = items[index];
        if (item.Use!.Target == SpellTarget.Self)
        {
            return new UseItemAction(item.Id);
        }

        return ChooseTarget(combat) is { } target ? new UseItemAction(item.Id, target) : null;
    }

    private PlayerAction? ChooseParley(CombatSession combat)
    {
        var options = combat.ParleyOptions();
        var labels = options.Select(o => $"{RollText.Badge(o.CheckLabel, o.Dc, o.Mode)} {Theme.Paint(Theme.Dialogue, o.Label)}").ToList();
        var index = ui.Prompter.Choose($"[{Theme.Accent}]What do you say?[/]", labels, allowBack: true);
        return index < 0 ? null : new ParleyAction(options[index].Index);
    }
}
