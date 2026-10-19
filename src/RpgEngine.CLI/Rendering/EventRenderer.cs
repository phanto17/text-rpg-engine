using RpgEngine.Core.Combat;
using RpgEngine.Core.Events;
using RpgEngine.Core.Views;
using Spectre.Console;

namespace RpgEngine.CLI.Rendering;

/// <summary>Turns Core <see cref="GameEvent"/>s into styled terminal output, in order.</summary>
internal sealed class EventRenderer(IAnsiConsole console, DiceAnimator animator)
{
    private bool Unicode => console.Profile.Capabilities.Unicode;

    /// <summary>The player's name, so events about the player can read "You ...".</summary>
    public string PlayerName { get; set; } = string.Empty;

    public void Render(IEnumerable<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        foreach (var gameEvent in events)
        {
            Render(gameEvent);
        }
    }

    public void Paragraphs(string style, string text)
    {
        foreach (var paragraph in text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            console.MarkupLine(Theme.Paint(style, paragraph));
            console.WriteLine();
        }
    }

    public void Room(RoomView room)
    {
        console.WriteLine();
        console.Write(new Rule(Theme.Paint(Theme.Title, room.Name)).LeftJustified().RuleStyle(Theme.Muted));
        foreach (var paragraph in room.Paragraphs)
        {
            Paragraphs(Theme.Narration, paragraph);
        }

        if (room.Ambient is { } ambient)
        {
            console.MarkupLine(Theme.Paint(Theme.Sensory, ambient));
        }

        if (room.Npcs.Count > 0)
        {
            console.MarkupLine($"[{Theme.Muted}]Here:[/] {Theme.Paint(Theme.Speaker, string.Join(", ", room.Npcs))}");
        }

        if (room.Features.Count > 0)
        {
            console.MarkupLine($"[{Theme.Muted}]You notice:[/] {Theme.Paint(Theme.Narration, string.Join(", ", room.Features))}");
        }

        if (room.Items.Count > 0)
        {
            console.MarkupLine($"[{Theme.Muted}]Lying here:[/] {Theme.Paint(Theme.Gold, string.Join(", ", room.Items))}");
        }

        foreach (var exit in room.Exits)
        {
            var direction = exit.Direction.ToString().ToLowerInvariant();
            var destination = exit.KnownDestination is { } known ? $" [{Theme.Muted}](to {Markup.Escape(known)})[/]" : string.Empty;
            // A blocked exit's description describes it open, so only the blocked marker is shown.
            var detail = exit.IsBlocked
                ? $" [{Theme.Failure}](blocked)[/]"
                : exit.Description is { } text ? $" [{Theme.Muted}]- {Markup.Escape(text)}[/]" : string.Empty;
            console.MarkupLine($"[{Theme.Muted}]Exit:[/] [{Theme.Accent}]{direction}[/]{destination}{detail}");
        }
    }

    private void Render(GameEvent gameEvent)
    {
        switch (gameEvent)
        {
            case NarrationEvent e:
                Paragraphs(Theme.For(e.Style), e.Text);
                break;
            case RoomDescribedEvent e:
                Room(e.Room);
                break;
            case DialogueLineEvent e:
                console.Write(new Panel(new Markup(Theme.Paint(Theme.Dialogue, e.Text)))
                    .Header($" {Theme.Paint(Theme.Speaker, e.Speaker)} ")
                    .Border(BoxBorder.Rounded)
                    .BorderStyle(Style.Parse(Theme.Muted))
                    .Padding(1, 0));
                break;
            case CheckRolledEvent e:
                animator.Roll(e.Result.Label, e.Result.Roll.Faces.Count, 20);
                console.MarkupLine(RollText.Check(e.Result));
                break;
            case DiceRolledEvent e:
                console.MarkupLine($"[{Theme.Muted}]{Markup.Escape(e.Label)}:[/] {RollText.Dice(e.Roll)}");
                break;
            case ItemGainedEvent e:
                console.MarkupLine($"[{Theme.Success}]+ {Markup.Escape(e.Item.Name)}{Count(e.Count)}[/]");
                break;
            case ItemLostEvent e:
                console.MarkupLine($"[{Theme.Muted}]- {Markup.Escape(e.Item.Name)}{Count(e.Count)}[/]");
                break;
            case GoldGainedEvent e:
                console.MarkupLine($"[{Theme.Gold}]+{e.Amount} gold[/] [{Theme.Muted}]({e.Total} total)[/]");
                break;
            case XpGainedEvent e:
                console.MarkupLine($"[{Theme.Accent}]+{e.Amount} XP[/] [{Theme.Muted}]({e.Total} total)[/]");
                break;
            case LeveledUpEvent e:
                RenderLevelUp(e);
                break;
            case DamageTakenEvent e:
                console.MarkupLine($"{Who(e.Target)} {(IsPlayer(e.Target) ? "take" : "takes")} [{Theme.Failure}]{e.Amount}{DamageType(e)} damage[/]{Modifier(e.Modifier)}  {Bars.Hp(e.Hp, e.MaxHp, Unicode)}");
                break;
            case HealedEvent e:
                console.MarkupLine($"{Who(e.Target)} {(IsPlayer(e.Target) ? "recover" : "recovers")} [{Theme.Success}]{e.Amount} HP[/]  {Bars.Hp(e.Hp, e.MaxHp, Unicode)}");
                break;
            case ManaChangedEvent e:
                console.MarkupLine($"[{Theme.Mana}]{RollText.Signed(e.Delta)} MP[/]  {Bars.Mana(e.Mana, e.MaxMana, Unicode)}");
                break;
            case RestedEvent e:
                var mana = e.MaxMana > 0 ? $"  {Bars.Mana(e.Mana, e.MaxMana, Unicode)}" : string.Empty;
                console.MarkupLine($"[{Theme.Success}]Rested.[/] [{Theme.Muted}](+{e.HpRestored} HP{(e.MaxMana > 0 ? $", +{e.ManaRestored} MP" : string.Empty)})[/]  {Bars.Hp(e.Hp, e.MaxHp, Unicode)}{mana}");
                break;
            case ReputationChangedEvent e:
                var color = e.Delta >= 0 ? Theme.Success : Theme.Failure;
                console.MarkupLine($"[{Theme.Muted}]Reputation:[/] {Markup.Escape(e.Faction.Name)} [{color}]{RollText.Signed(e.Delta)}[/] [{Theme.Muted}](now {e.Value})[/]");
                break;
            case QuestUpdatedEvent e:
                console.Write(new Panel(new Markup(Theme.Paint(Theme.Narration, e.Stage.Journal)))
                    .Header($" [{Theme.Accent}]Journal: {Markup.Escape(e.Quest.Name)}[/] ")
                    .Border(BoxBorder.Rounded)
                    .BorderStyle(Style.Parse(Theme.Accent)));
                break;
            case CombatStartedEvent e:
                console.WriteLine();
                console.Write(new Rule($"[{Theme.Danger}]COMBAT[/]").RuleStyle(Theme.Failure));
                if (e.Intro is { } intro)
                {
                    Paragraphs(Theme.Narration, intro);
                }

                break;
            case InitiativeRolledEvent e:
                var table = new Table().Border(TableBorder.Simple).Title($"[{Theme.Muted}]Initiative[/]");
                table.AddColumn("Combatant").AddColumn("d20").AddColumn("Mod").AddColumn("Total");
                foreach (var entry in e.Order)
                {
                    table.AddRow(Theme.Paint(entry.IsPlayer ? Theme.Accent : Theme.Narration, entry.Name), $"{entry.Natural}", RollText.Signed(entry.Modifier), $"[bold]{entry.Total}[/]");
                }

                console.Write(table);
                break;
            case TurnStartedEvent e:
                if (e.IsPlayer)
                {
                    console.Write(new Rule($"[{Theme.Muted}]Round {e.Round} - your turn[/]").RuleStyle(Theme.Muted));
                }
                else
                {
                    console.MarkupLine($"[{Theme.Muted}]-- {Markup.Escape(e.Name)}'s turn --[/]");
                }

                break;
            case AttackRolledEvent e:
                animator.Roll(e.AttackName, e.Roll.Faces.Count, 20);
                var result = e.Critical ? $"[{Theme.Critical}]CRITICAL HIT[/]" : e.Hit ? $"[{Theme.Success}]HIT[/]" : $"[{Theme.Muted}]MISS[/]";
                console.MarkupLine($"{Who(e.Attacker)} {(IsPlayer(e.Attacker) ? "attack" : "attacks")} {Whom(e.Target)} with {Theme.Paint(Theme.Accent, e.AttackName)}: {RollText.D20(e.Roll)} {RollText.Signed(e.Bonus)} = [bold]{e.Total}[/] vs AC {e.TargetArmorClass}  {result}");
                break;
            case SpellCastEvent e:
                console.MarkupLine($"{Who(e.Caster)} {(e.Kind == Core.Content.Defs.SpellKind.Spell ? "cast" : "use")}{(IsPlayer(e.Caster) ? string.Empty : "s")} {Theme.Paint(Theme.Mana, e.SpellName)}.");
                break;
            case StatusAppliedEvent e:
                console.MarkupLine($"{Who(e.Target)} {(IsPlayer(e.Target) ? "are" : "is")} now affected by {Theme.Paint(Theme.Warning, e.StatusName)}.");
                break;
            case StatusExpiredEvent e:
                console.MarkupLine($"[{Theme.Muted}]{Markup.Escape(e.StatusName)} wears off {Markup.Escape(IsPlayer(e.Target) ? "you" : e.Target)}.[/]");
                break;
            case TurnSkippedEvent e:
                console.MarkupLine($"{Who(e.Name)} {(IsPlayer(e.Name) ? "are" : "is")} {Theme.Paint(Theme.Warning, e.StatusName)} and {(IsPlayer(e.Name) ? "lose" : "loses")} the turn.");
                break;
            case CombatantDefeatedEvent e:
                console.MarkupLine(e.IsPlayer ? $"[{Theme.Danger}]You fall.[/]" : $"[{Theme.Title}]{Markup.Escape(e.Name)} falls![/]");
                break;
            case CombatantFledEvent e:
                console.MarkupLine(e.IsPlayer ? $"[{Theme.Warning}]You break away and flee![/]" : $"[{Theme.Warning}]{Markup.Escape(e.Name)} flees![/]");
                break;
            case CombatEndedEvent e:
                RenderCombatEnd(e.Outcome);
                break;
            case EpilogueEvent e:
                console.WriteLine();
                console.Write(new Rule($"[{Theme.Title}]Epilogue[/]").RuleStyle(Theme.Accent));
                Paragraphs(Theme.Narration, e.Text);
                console.Write(new Rule($"[{Theme.Title}]THE END[/]").RuleStyle(Theme.Accent));
                break;
            case PlayerDefeatedEvent e:
                console.WriteLine();
                console.Write(new Panel(new Markup(Theme.Paint(Theme.Narration, e.Text)))
                    .Header($" [{Theme.Danger}]You have died[/] ")
                    .Border(BoxBorder.Double)
                    .BorderStyle(Style.Parse(Theme.Failure)));
                break;
        }
    }

    private void RenderLevelUp(LeveledUpEvent e)
    {
        var grid = new Grid().AddColumn().AddColumn();
        grid.AddRow($"[{Theme.Muted}]Hit points[/]", $"[{Theme.Success}]+{e.HpGain}[/] [{Theme.Muted}](max {e.MaxHp})[/]");
        if (e.ManaGain > 0)
        {
            grid.AddRow($"[{Theme.Muted}]Mana[/]", $"[{Theme.Mana}]+{e.ManaGain}[/] [{Theme.Muted}](max {e.MaxMana})[/]");
        }

        grid.AddRow($"[{Theme.Muted}]Proficiency[/]", RollText.Signed(e.ProficiencyBonus));
        foreach (var spell in e.NewSpells)
        {
            grid.AddRow($"[{Theme.Muted}]Learned[/]", $"{Theme.Paint(Theme.Mana, spell.Name)} [{Theme.Muted}]- {Markup.Escape(spell.Description)}[/]");
        }

        console.WriteLine();
        console.Write(new Panel(grid)
            .Header($" [{Theme.Title}]Level {e.Level}![/] ")
            .Border(BoxBorder.Double)
            .BorderStyle(Style.Parse(Theme.Accent)));
    }

    private void RenderCombatEnd(CombatOutcome outcome)
    {
        var text = outcome switch
        {
            CombatOutcome.Victory => $"[{Theme.Success}]VICTORY[/]",
            CombatOutcome.Parleyed => $"[{Theme.Accent}]The fight is over. Words won where steel did not.[/]",
            CombatOutcome.Fled => $"[{Theme.Warning}]You got away.[/]",
            _ => string.Empty,
        };

        if (text.Length > 0)
        {
            console.Write(new Rule(text).RuleStyle(Theme.Muted));
        }
    }

    private bool IsPlayer(string name) => name == PlayerName;

    private string Who(string name) => IsPlayer(name) ? $"[{Theme.Accent}]You[/]" : Theme.Paint(Theme.Title, name);

    private string Whom(string name) => IsPlayer(name) ? $"[{Theme.Accent}]you[/]" : Theme.Paint(Theme.Title, name);

    private static string Count(int count) => count > 1 ? $" x{count}" : string.Empty;

    private static string DamageType(DamageTakenEvent e) => e.DamageType is { } type ? $" {type.ToString().ToLowerInvariant()}" : string.Empty;

    private static string Modifier(DamageModifier modifier) => modifier switch
    {
        DamageModifier.Resisted => $" [{Theme.Muted}](resisted)[/]",
        DamageModifier.Vulnerable => $" [{Theme.Warning}](vulnerable!)[/]",
        DamageModifier.Immune => $" [{Theme.Muted}](immune)[/]",
        _ => string.Empty,
    };
}
