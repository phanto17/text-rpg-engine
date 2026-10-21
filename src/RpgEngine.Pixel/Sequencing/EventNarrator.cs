using RpgEngine.Core.Checks;
using RpgEngine.Core.Combat;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Events;
using RpgEngine.Core.Views;

namespace RpgEngine.Pixel.Sequencing;

/// <summary>
/// Turns each Core <see cref="GameEvent"/> into log lines and visual cues: the graphical counterpart
/// of the terminal's event renderer. It never touches the window.
/// </summary>
public sealed class EventNarrator
{
    /// <summary>How long a d20 banner plays before its verdict is typed.</summary>
    public const double DiceHoldSeconds = 0.9;

    /// <summary>The player's name, so events about the player read "You ...".</summary>
    public string PlayerName { get; set; } = string.Empty;

    public IReadOnlyList<Beat> Narrate(GameEvent gameEvent)
    {
        ArgumentNullException.ThrowIfNull(gameEvent);
        return gameEvent switch
        {
            NarrationEvent e => [Beat.Of(Paragraphs(e.Text, ToneFor(e.Style)))],
            RoomDescribedEvent e => [Room(e.Room)],
            DialogueLineEvent e => [Beat.Of(Quote(e.Speaker, e.Text), new SpeakerCue(e.Speaker))],
            CheckRolledEvent e => [Check(e.Result)],
            DiceRolledEvent e => [Beat.Of(Dice(e.Label, e.Roll))],
            ItemGainedEvent e => [Beat.Of(LogLine.Of($"+ {e.Item.Name}{Count(e.Count)}", LogTone.Success))],
            ItemLostEvent e => [Beat.Of(LogLine.Of($"- {e.Item.Name}{Count(e.Count)}", LogTone.Muted))],
            GoldGainedEvent e => [Beat.Of(LogLine.Build().Add($"+{e.Amount} gold", LogTone.Gold).Add($" ({e.Total} total)", LogTone.Muted).Done())],
            GoldSpentEvent e => [Beat.Of(LogLine.Build().Add($"-{e.Amount} gold", LogTone.Gold).Add($" ({e.Total} left)", LogTone.Muted).Done())],
            XpGainedEvent e => [Beat.Of(LogLine.Build().Add($"+{e.Amount} XP", LogTone.Accent).Add($" ({e.Total} total)", LogTone.Muted).Done())],
            LeveledUpEvent e => [LevelUp(e)],
            DamageTakenEvent e => [Damage(e)],
            HealedEvent e => [Beat.Of(
                [LogLine.Build().Add(Who(e.Target), LogTone.Accent).Add(IsPlayer(e.Target) ? " recover " : " recovers ").Add($"{e.Amount} HP", LogTone.Success).Add($"  [HP {e.Hp}/{e.MaxHp}]", LogTone.Muted).Done()],
                new FloatingNumberCue(e.Target, e.IsPlayer, $"+{e.Amount}", FloatingKind.Heal))],
            ManaChangedEvent e => [Beat.Of(LogLine.Build().Add($"{Signed(e.Delta)} MP", LogTone.Mana).Add($"  [MP {e.Mana}/{e.MaxMana}]", LogTone.Muted).Done())],
            RestedEvent e => [Beat.Of(LogLine.Build().Add("Rested.", LogTone.Success)
                .Add($" (+{e.HpRestored} HP{(e.MaxMana > 0 ? $", +{e.ManaRestored} MP" : string.Empty)})", LogTone.Muted)
                .Add($"  [HP {e.Hp}/{e.MaxHp}{(e.MaxMana > 0 ? $", MP {e.Mana}/{e.MaxMana}" : string.Empty)}]", LogTone.Muted).Done())],
            ReputationChangedEvent e => [Beat.Of(LogLine.Build().Add("Reputation: ", LogTone.Muted).Add(e.Faction.Name)
                .Add($" {Signed(e.Delta)}", e.Delta >= 0 ? LogTone.Success : LogTone.Failure).Add($" (now {e.Value})", LogTone.Muted).Done())],
            QuestUpdatedEvent e => [new Beat(
                [LogLine.Of($"Journal: {e.Quest.Name}", LogTone.Accent, LogLineKind.Heading), .. Paragraphs(e.Stage.Journal, LogTone.Narration).Select(l => l with { Kind = l.Length == 0 ? LogLineKind.Normal : LogLineKind.Quote })],
                [])],
            CombatStartedEvent e => [CombatStart(e)],
            InitiativeRolledEvent e => [Beat.Of(Initiative(e))],
            TurnStartedEvent e => [Beat.Of(e.IsPlayer
                ? LogLine.Of($"Round {e.Round}: your turn", LogTone.Muted, LogLineKind.Rule)
                : LogLine.Of($"{e.Name}'s turn", LogTone.Muted))],
            AttackRolledEvent e => [Attack(e)],
            SpellCastEvent e => [Beat.Of(LogLine.Build().Add(Who(e.Caster), LogTone.Accent)
                .Add($" {(e.Kind == SpellKind.Spell ? "cast" : "use")}{(IsPlayer(e.Caster) ? string.Empty : "s")} ").Add(e.SpellName, LogTone.Mana).Add(".").Done())],
            StatusAppliedEvent e => [Beat.Of(
                [LogLine.Build().Add(Who(e.Target), LogTone.Accent).Add(IsPlayer(e.Target) ? " are now " : " is now ").Add(e.StatusName, LogTone.Warning).Add(".").Done()],
                new FloatingNumberCue(e.Target, IsPlayer(e.Target), e.StatusName, FloatingKind.Status))],
            StatusExpiredEvent e => [Beat.Of(LogLine.Of($"{e.StatusName} wears off {(IsPlayer(e.Target) ? "you" : e.Target)}.", LogTone.Muted))],
            TurnSkippedEvent e => [Beat.Of(LogLine.Build().Add(Who(e.Name), LogTone.Accent).Add(IsPlayer(e.Name) ? " are " : " is ").Add(e.StatusName, LogTone.Warning)
                .Add(IsPlayer(e.Name) ? " and lose the turn." : " and loses the turn.").Done())],
            CombatantDefeatedEvent e => [Beat.Of(
                [e.IsPlayer ? LogLine.Of("You fall.", LogTone.Danger) : LogLine.Of($"{e.Name} falls!", LogTone.Title)],
                new FellCue(e.Name, e.IsPlayer, Fled: false))],
            CombatantFledEvent e => [Beat.Of(
                [LogLine.Of(e.IsPlayer ? "You break away and flee!" : $"{e.Name} flees!", LogTone.Warning)],
                new FellCue(e.Name, e.IsPlayer, Fled: true))],
            CombatEndedEvent e => CombatEnd(e.Outcome),
            EpilogueEvent e => [new Beat(
                [LogLine.Blank, LogLine.Of("Epilogue", LogTone.Title, LogLineKind.Rule), .. Paragraphs(e.Text, LogTone.Narration), LogLine.Of("THE END", LogTone.Title, LogLineKind.Rule)],
                [])],
            PlayerDefeatedEvent e => [new Beat([LogLine.Blank, LogLine.Of("You have died", LogTone.Danger, LogLineKind.Rule), .. Paragraphs(e.Text, LogTone.Narration)], [])],
            _ => [],
        };
    }

    public static LogTone ToneFor(TextStyle style) => style switch
    {
        TextStyle.Sensory => LogTone.Sensory,
        TextStyle.Dialogue => LogTone.Dialogue,
        TextStyle.System => LogTone.System,
        TextStyle.Success => LogTone.Success,
        TextStyle.Failure => LogTone.Failure,
        TextStyle.Warning => LogTone.Warning,
        _ => LogTone.Narration,
    };

    public static IReadOnlyList<LogLine> Paragraphs(string text, LogTone tone)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lines = new List<LogLine>();
        foreach (var paragraph in text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            lines.Add(LogLine.Of(paragraph.Replace("\n", " ", StringComparison.Ordinal).Trim(), tone));
            lines.Add(LogLine.Blank);
        }

        return lines;
    }

    private Beat Room(RoomView room)
    {
        var lines = new List<LogLine> { LogLine.Blank, LogLine.Of(room.Name, LogTone.Title, LogLineKind.Heading) };
        foreach (var paragraph in room.Paragraphs)
        {
            lines.AddRange(Paragraphs(paragraph, LogTone.Narration));
        }

        if (room.Ambient is { } ambient)
        {
            lines.Add(LogLine.Of(ambient, LogTone.Sensory));
        }

        if (room.Npcs.Count > 0)
        {
            lines.Add(LogLine.Build().Add("Here: ", LogTone.Muted).Add(string.Join(", ", room.Npcs), LogTone.Speaker).Done());
        }

        if (room.Features.Count > 0)
        {
            lines.Add(LogLine.Build().Add("You notice: ", LogTone.Muted).Add(string.Join(", ", room.Features)).Done());
        }

        if (room.Items.Count > 0)
        {
            lines.Add(LogLine.Build().Add("Lying here: ", LogTone.Muted).Add(string.Join(", ", room.Items), LogTone.Gold).Done());
        }

        foreach (var exit in room.Exits)
        {
            var line = LogLine.Build().Add("Exit: ", LogTone.Muted).Add(exit.Direction.ToString().ToLowerInvariant(), LogTone.Accent);
            if (exit.KnownDestination is { } known)
            {
                line.Add($" (to {known})", LogTone.Muted);
            }

            // A blocked exit's description describes it open, so only the blocked marker is shown.
            if (exit.IsBlocked)
            {
                line.Add(" (blocked)", LogTone.Failure);
            }
            else if (exit.Description is { } description)
            {
                line.Add($" - {description}", LogTone.Muted);
            }

            lines.Add(line.Done());
        }

        return Beat.Of(lines, new RoomCue(room.RoomId));
    }

    // A spoken node may hold several paragraphs; the speaker's name opens the first and each is its own quote line.
    private static IReadOnlyList<LogLine> Quote(string speaker, string text)
    {
        var paragraphs = Paragraphs(text, LogTone.Dialogue).Where(l => l.Length > 0).ToList();
        if (paragraphs.Count == 0)
        {
            return [LogLine.Build(LogLineKind.Quote).Add($"{speaker}: ", LogTone.Speaker).Done()];
        }

        var lines = new List<LogLine>
        {
            LogLine.Build(LogLineKind.Quote).Add($"{speaker}: ", LogTone.Speaker).Add(paragraphs[0].Text, LogTone.Dialogue).Done(),
        };
        lines.AddRange(paragraphs.Skip(1).Select(l => l with { Kind = LogLineKind.Quote }));
        return lines;
    }

    private static Beat Check(CheckResult result)
    {
        var outcome = result.IsCriticalSuccess ? DiceOutcome.CriticalSuccess
            : result.IsCriticalFailure ? DiceOutcome.CriticalFailure
            : result.Success ? DiceOutcome.Success
            : DiceOutcome.Failure;
        var bonus = result.AbilityModifier + result.ProficiencyBonus;

        var line = LogLine.Build().Add($"[{result.Label} DC {result.Dc}]", LogTone.Accent).Add(" ").Add(D20(result.Roll), LogTone.Narration);
        line.Add($" {Signed(result.AbilityModifier)} {result.Ability.ToString().ToUpperInvariant()}", LogTone.Muted);
        if (result.ProficiencyBonus != 0)
        {
            line.Add($" {Signed(result.ProficiencyBonus)} prof", LogTone.Muted);
        }

        line.Add($" = {result.Total}").Add($" vs DC {result.Dc}  ", LogTone.Muted).Add(Verdict(outcome), VerdictTone(outcome));
        return Beat.Of([line.Done()], new DiceBannerCue(result.Label, result.Roll, bonus, result.Total, $"DC {result.Dc}", outcome)).Holding(DiceHoldSeconds);
    }

    private Beat Attack(AttackRolledEvent e)
    {
        var outcome = e.Critical ? DiceOutcome.CriticalSuccess : e.Roll.IsNatural1 ? DiceOutcome.CriticalFailure : e.Hit ? DiceOutcome.Success : DiceOutcome.Failure;
        var line = LogLine.Build().Add(Who(e.Attacker), LogTone.Accent).Add(IsPlayer(e.Attacker) ? " attack " : " attacks ").Add(Whom(e.Target), LogTone.Title)
            .Add(" with ").Add(e.AttackName, LogTone.Accent).Add(": ").Add(D20(e.Roll)).Add($" {Signed(e.Bonus)}", LogTone.Muted).Add($" = {e.Total}")
            .Add($" vs AC {e.TargetArmorClass}  ", LogTone.Muted).Add(e.Critical ? "CRITICAL HIT" : e.Hit ? "HIT" : "MISS", VerdictTone(outcome));

        var cues = new List<SequenceCue>
        {
            new DiceBannerCue(e.AttackName, e.Roll, e.Bonus, e.Total, $"AC {e.TargetArmorClass}", outcome),
            new LungeCue(e.Attacker, IsPlayer(e.Attacker), e.Target),
        };
        if (!e.Hit)
        {
            cues.Add(new FloatingNumberCue(e.Target, IsPlayer(e.Target), "MISS", FloatingKind.Miss));
        }

        return new Beat([line.Done()], cues, DiceHoldSeconds);
    }

    private Beat Damage(DamageTakenEvent e)
    {
        var type = e.DamageType is { } damageType ? $" {damageType.ToString().ToLowerInvariant()}" : string.Empty;
        var line = LogLine.Build().Add(Who(e.Target), LogTone.Accent).Add(e.IsPlayer ? " take " : " takes ").Add($"{e.Amount}{type} damage", LogTone.Failure);
        switch (e.Modifier)
        {
            case DamageModifier.Resisted:
                line.Add(" (resisted)", LogTone.Muted);
                break;
            case DamageModifier.Vulnerable:
                line.Add(" (vulnerable!)", LogTone.Warning);
                break;
            case DamageModifier.Immune:
                line.Add(" (immune)", LogTone.Muted);
                break;
        }

        line.Add($"  [HP {e.Hp}/{e.MaxHp}]", LogTone.Muted);
        var number = e.Modifier == DamageModifier.Immune ? "IMMUNE" : $"-{e.Amount}";
        return Beat.Of([line.Done()], new HitFlashCue(e.Target, e.IsPlayer), new FloatingNumberCue(e.Target, e.IsPlayer, number, FloatingKind.Damage));
    }

    private static Beat LevelUp(LeveledUpEvent e)
    {
        var lines = new List<LogLine>
        {
            LogLine.Of($"Level {e.Level}!", LogTone.Title, LogLineKind.Rule),
            LogLine.Build().Add("Hit points ", LogTone.Muted).Add($"+{e.HpGain}", LogTone.Success).Add($" (max {e.MaxHp})", LogTone.Muted).Done(),
        };
        if (e.ManaGain > 0)
        {
            lines.Add(LogLine.Build().Add("Mana ", LogTone.Muted).Add($"+{e.ManaGain}", LogTone.Mana).Add($" (max {e.MaxMana})", LogTone.Muted).Done());
        }

        lines.Add(LogLine.Build().Add("Proficiency ", LogTone.Muted).Add(Signed(e.ProficiencyBonus)).Done());
        foreach (var spell in e.NewSpells)
        {
            lines.Add(LogLine.Build().Add("Learned ", LogTone.Muted).Add(spell.Name, LogTone.Mana).Add($" - {spell.Description}", LogTone.Muted).Done());
        }

        return Beat.Of(lines, new LevelUpCue(e.Level));
    }

    private static Beat CombatStart(CombatStartedEvent e)
    {
        var lines = new List<LogLine> { LogLine.Blank, LogLine.Of("COMBAT", LogTone.Danger, LogLineKind.Rule) };
        if (e.Intro is { } intro)
        {
            lines.AddRange(Paragraphs(intro, LogTone.Narration));
        }

        return Beat.Of(lines, new CombatCue(Started: true));
    }

    private static LogLine Initiative(InitiativeRolledEvent e)
    {
        var line = LogLine.Build().Add("Initiative: ", LogTone.Muted);
        for (var i = 0; i < e.Order.Count; i++)
        {
            var entry = e.Order[i];
            line.Add(entry.Name, entry.IsPlayer ? LogTone.Accent : LogTone.Narration).Add($" {entry.Total}", LogTone.Muted);
            if (i < e.Order.Count - 1)
            {
                line.Add(", ", LogTone.Muted);
            }
        }

        return line.Done();
    }

    private static IReadOnlyList<Beat> CombatEnd(CombatOutcome outcome)
    {
        var line = outcome switch
        {
            CombatOutcome.Victory => LogLine.Of("VICTORY", LogTone.Success, LogLineKind.Rule),
            CombatOutcome.Parleyed => LogLine.Of("The fight is over. Words won where steel did not.", LogTone.Accent, LogLineKind.Rule),
            CombatOutcome.Fled => LogLine.Of("You got away.", LogTone.Warning, LogLineKind.Rule),
            _ => null,
        };

        return [Beat.Of(line is null ? [] : [line], new CombatCue(Started: false))];
    }

    private static LogLine Dice(string label, RollResult roll)
    {
        var line = LogLine.Build().Add($"{label}: ", LogTone.Muted).Add(roll.Expression.Text, LogTone.Narration).Add(" [", LogTone.Muted);
        for (var i = 0; i < roll.Dice.Count; i++)
        {
            var die = roll.Dice[i];
            line.Add(die.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), die.Kept ? LogTone.Narration : LogTone.Muted);
            if (i < roll.Dice.Count - 1)
            {
                line.Add(", ", LogTone.Muted);
            }
        }

        line.Add("]", LogTone.Muted);
        if (roll.Modifier != 0)
        {
            line.Add($" {Signed(roll.Modifier)}", LogTone.Muted);
        }

        line.Add($" = {roll.Total}");
        if (roll.Critical)
        {
            line.Add(" critical!", LogTone.Critical);
        }

        return line.Done();
    }

    private static string D20(D20Roll roll) => roll.Mode switch
    {
        RollMode.Advantage => $"d20 {roll.Natural} (adv {string.Join("/", roll.Faces)})",
        RollMode.Disadvantage => $"d20 {roll.Natural} (dis {string.Join("/", roll.Faces)})",
        _ => $"d20 {roll.Natural}",
    };

    private static string Verdict(DiceOutcome outcome) => outcome switch
    {
        DiceOutcome.CriticalSuccess => "CRITICAL SUCCESS",
        DiceOutcome.CriticalFailure => "CRITICAL FAILURE",
        DiceOutcome.Success => "SUCCESS",
        _ => "FAILURE",
    };

    public static LogTone VerdictTone(DiceOutcome outcome) => outcome switch
    {
        DiceOutcome.CriticalSuccess => LogTone.Critical,
        DiceOutcome.CriticalFailure => LogTone.Danger,
        DiceOutcome.Success => LogTone.Success,
        _ => LogTone.Failure,
    };

    private bool IsPlayer(string name) => name == PlayerName;

    private string Who(string name) => IsPlayer(name) ? "You" : name;

    private string Whom(string name) => IsPlayer(name) ? "you" : name;

    private static string Count(int count) => count > 1 ? $" x{count}" : string.Empty;

    public static string Signed(int value) => value >= 0 ? $"+{value}" : value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
