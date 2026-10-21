using RpgEngine.Core.Characters;
using RpgEngine.Core.Combat;
using RpgEngine.Core.Commands;
using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Engine;
using RpgEngine.Core.Events;
using RpgEngine.Core.Persistence;
using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Pixel.Game;

/// <summary>
/// The whole client without a window: the title screen, character creation, and the play loop by
/// game mode. It owns the <see cref="GameSession"/>, builds the contextual <see cref="Actions"/> the
/// action bar shows, accepts clicks (<see cref="Pick"/>) and typed lines (<see cref="Submit"/>), and
/// feeds every <see cref="GameEvent"/> to the <see cref="Sequencer"/>. The renderer only draws it.
/// </summary>
public sealed class PixelGame
{
    public const string DefaultSlot = "quicksave";
    public const int MaxInputLength = 60;

    private static readonly string[] HelpRows =
    [
        "1, 2, 3 ... or click    choose an action",
        "look, l                 describe the room again",
        "go north, n s e w u d   move",
        "inspect <thing>         examine something or someone",
        "<verb> <thing>          interact: read inscription, press sun, fit splinter",
        "take / drop <item>      pick up or drop an item",
        "use <item> [on <thing>] use an item",
        "equip <item>            wield a weapon or wear armor",
        "talk to <someone>       start a conversation",
        "inventory, stats,       open a HUD tab (Tab cycles them)",
        "journal",
        "save [slot] / load      save or load (default slot: quicksave)",
        "quit                    return to the title screen",
        "In combat: attack, defend, flee, parley, cast <spell>, use <item>. In a shop: buy / sell <item>, leave.",
        "Keys: Enter skips the typewriter, Esc goes back, F2 toggles the CRT, F3 toggles pixel scaling.",
    ];

    private readonly GameContent _content;
    private readonly SaveService _saves;
    private readonly Func<ulong, IRandomSource> _randomFactory;
    private readonly ulong? _fixedSeed;
    private readonly List<ActionChoice> _actions = [];
    private List<ActionChoice>? _submenu;
    private IRandomSource? _random;

    public PixelGame(GameContent content, SaveService saves, Func<ulong, IRandomSource>? randomFactory = null, ulong? seed = null, EventSequencer? sequencer = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(saves);
        _content = content;
        _saves = saves;
        _randomFactory = randomFactory ?? (s => new Pcg32RandomSource(s));
        _fixedSeed = seed;
        Sequencer = sequencer ?? new EventSequencer(new EventNarrator());
        ShowTitle();
    }

    public GameContent Content => _content;

    public EventSequencer Sequencer { get; }

    public PixelScreen Screen { get; private set; }

    public GameSession? Session { get; private set; }

    public CreationFlow? Creation { get; private set; }

    public HudTab Tab { get; set; }

    /// <summary>The line above the action bar: a question, a speaker, or a reminder of what the buttons are.</summary>
    public string Prompt { get; private set; } = string.Empty;

    public IReadOnlyList<ActionChoice> Actions => _submenu ?? _actions;

    public bool InSubmenu => _submenu is not null;

    public bool WantsQuit { get; private set; }

    /// <summary>The seed of the current run, shown on the Stats tab so a run can be replayed.</summary>
    public ulong Seed { get; private set; }

    /// <summary>The command line as typed so far.</summary>
    public string Input { get; private set; } = string.Empty;

    /// <summary>The command line waits for a name during creation; otherwise it takes commands.</summary>
    public bool InputIsName => Screen == PixelScreen.Creation && Creation?.Step == CreationStep.Name;

    public void Type(char character)
    {
        if (character >= ' ' && Input.Length < MaxInputLength)
        {
            Input += character;
        }
    }

    public void Backspace()
    {
        if (Input.Length > 0)
        {
            Input = Input[..^1];
        }
    }

    public void SubmitInput()
    {
        var text = Input;
        Input = string.Empty;
        Submit(text);
    }

    /// <summary>Runs the action at <paramref name="index"/> in <see cref="Actions"/>; out-of-range or disabled picks do nothing.</summary>
    public void Pick(int index)
    {
        var actions = Actions;
        if (index >= 0 && index < actions.Count && actions[index].Enabled)
        {
            actions[index].Run();
        }
    }

    /// <summary>Leaves the current sub-menu, if any.</summary>
    public void Back()
    {
        if (_submenu is not null)
        {
            Refresh();
        }
    }

    /// <summary>
    /// Handles a typed line: a number picks an action, an empty line skips the typewriter, and
    /// anything else is a name (during creation) or a command for the current mode.
    /// </summary>
    public void Submit(string? text)
    {
        var line = text?.Trim() ?? string.Empty;
        if (line.Length == 0)
        {
            if (!Sequencer.IsIdle)
            {
                Sequencer.Skip();
            }

            return;
        }

        if (int.TryParse(line, out var number))
        {
            if (number >= 1 && number <= Actions.Count)
            {
                Pick(number - 1);
            }
            else
            {
                Sequencer.Say($"There is no option {number}.");
            }

            return;
        }

        switch (Screen)
        {
            case PixelScreen.Title:
                TitleCommand(line);
                break;
            case PixelScreen.Creation:
                CreationCommand(line);
                break;
            default:
                PlayCommand(line);
                break;
        }
    }

    /// <summary>Advances the typewriter and, once it is quiet, lets the monsters take their turns.</summary>
    public void Update(double deltaSeconds)
    {
        Sequencer.Advance(deltaSeconds);
        if (Screen == PixelScreen.Playing && Session is { Mode: GameMode.Combat, Combat: { IsPlayerTurn: false } } && Sequencer.IsIdle)
        {
            Run(Session.CombatEnemyTurn());
        }
    }

    // ----- title -----

    private void ShowTitle()
    {
        Screen = PixelScreen.Title;
        Session = null;
        Creation = null;
        _submenu = null;
        Sequencer.Clear();
        Sequencer.Enqueue(Beat.Of(
            LogLine.Of(_content.Manifest.Title, LogTone.Title, LogLineKind.Heading),
            LogLine.Of("A new game, or continue a saved one. Type a number or click.", LogTone.Muted)));
        Refresh();
    }

    private void TitleCommand(string line)
    {
        var words = TextNormalizer.Words(line);
        switch (words.Count > 0 ? words[0] : string.Empty)
        {
            case "new" or "start":
                NewGame();
                break;
            case "continue" or "load" or "restore":
                if (words.Count > 1)
                {
                    Load(line[(line.IndexOf(' ', StringComparison.Ordinal) + 1)..].Trim());
                }
                else
                {
                    LoadMenu();
                }

                break;
            case "quit" or "exit" or "q":
                WantsQuit = true;
                break;
            default:
                Sequencer.Say("Type new, continue, or quit.");
                break;
        }
    }

    private void NewGame()
    {
        _random = NewRandom();
        Creation = new CreationFlow(_content, new DiceRoller(_random));
        Screen = PixelScreen.Creation;
        Sequencer.Clear();
        Sequencer.Enqueue(Beat.Of(LogLine.Of("Create your character", LogTone.Title, LogLineKind.Rule)));
        Refresh();
    }

    // ----- creation -----

    private void CreationCommand(string line)
    {
        var creation = Creation!;
        if (creation.Step == CreationStep.Name)
        {
            if (creation.SetName(line))
            {
                Sequencer.Enqueue(Beat.Of(LogLine.Build().Add("Name: ", LogTone.Muted).Add(creation.Name, LogTone.Accent).Done()));
                Refresh();
            }
            else
            {
                Sequencer.Say($"A name needs 1 to {CharacterFactory.MaxNameLength} characters.");
            }

            return;
        }

        // Callings, burdens, and skills can be typed by name as well as picked.
        var match = Actions.Select((action, index) => (action, index)).FirstOrDefault(x => x.action.Enabled && Matches(x.action.Label, line));
        if (match.action is not null)
        {
            Pick(match.index);
        }
        else
        {
            Sequencer.Say("Choose one of the options.");
        }
    }

    private void RefreshCreation()
    {
        var creation = Creation!;
        switch (creation.Step)
        {
            case CreationStep.Name:
                Prompt = "What is your name?";
                break;
            case CreationStep.Calling:
                Prompt = "Choose a calling:";
                foreach (var cls in _content.ClassList)
                {
                    _actions.Add(new ActionChoice(cls.Name, LogTone.Title, () => Choose(() =>
                    {
                        creation.ChooseClass(cls);
                        Sequencer.Enqueue(Beat.Of(LogLine.Build().Add("Calling: ", LogTone.Muted).Add(cls.Name, LogTone.Title).Add($" (d{cls.HitDie}) ", LogTone.Muted).Add(cls.Description, LogTone.Narration).Done()));
                    }), Hint: cls.Description));
                }

                break;
            case CreationStep.Burden:
                Prompt = "Choose a burden:";
                foreach (var background in _content.BackgroundList)
                {
                    var skill = _content.Skills[background.BonusSkill].Name;
                    var keepsake = _content.Items[background.KeepsakeItem].Name;
                    var gold = background.BonusGold > 0 ? $", +{background.BonusGold} gold" : string.Empty;
                    _actions.Add(new ActionChoice(background.Name, LogTone.Title, () => Choose(() =>
                    {
                        creation.ChooseBackground(background);
                        Sequencer.Enqueue(Beat.Of(LogLine.Build().Add("Burden: ", LogTone.Muted).Add(background.Name, LogTone.Title).Add($" ({skill}, {keepsake}{gold}) ", LogTone.Muted).Add(background.Description).Done()));
                    }), Hint: $"{background.Description} ({skill}, {keepsake}{gold})"));
                }

                break;
            case CreationStep.ScoreMethod:
                Prompt = "How will you determine your ability scores?";
                _actions.Add(new ActionChoice($"Standard array ({string.Join(", ", creation.StandardArray)})", LogTone.Narration, () => Choose(() =>
                {
                    creation.UseStandardArray();
                    ShowScores(creation.Scores!, $"Suggested for a {creation.Class!.Name}");
                })));
                _actions.Add(new ActionChoice("Roll 4d6, drop the lowest, six times", LogTone.Narration, () => Choose(() =>
                {
                    foreach (var roll in creation.RollScores())
                    {
                        Sequencer.Enqueue(Beat.Of(LogLine.Build().Add("4d6 ", LogTone.Muted)
                            .Add($"[{string.Join(", ", roll.Dice.Select(d => d.Kept ? d.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : $"({d.Value})"))}] = ", LogTone.Muted)
                            .Add(roll.Total.ToString(System.Globalization.CultureInfo.InvariantCulture), LogTone.Accent).Done()));
                    }

                    ShowScores(creation.Scores!, $"Suggested for a {creation.Class!.Name}");
                })));
                break;
            case CreationStep.ScoreReview:
                Prompt = "Keep the suggested scores?";
                _actions.Add(new ActionChoice("Keep these scores", LogTone.Success, () => Choose(creation.KeepSuggestedScores)));
                _actions.Add(new ActionChoice("Assign them myself", LogTone.Narration, () => Choose(creation.AssignManually)));
                break;
            case CreationStep.AssignScores:
                Prompt = $"Score for {creation.NextAbility.DisplayName()}:";
                for (var i = 0; i < creation.RemainingValues.Count; i++)
                {
                    var index = i;
                    var value = creation.RemainingValues[i];
                    _actions.Add(new ActionChoice($"{value} ({EventNarrator.Signed(AbilityScores.ModifierFor(value))})", LogTone.Narration, () => Choose(() =>
                    {
                        creation.AssignNext(index);
                        if (creation.Step == CreationStep.Skills)
                        {
                            ShowScores(creation.Scores!, "Your scores");
                        }
                    })));
                }

                break;
            case CreationStep.Skills:
                var granted = _content.Skills[creation.Background!.BonusSkill].Name;
                Prompt = $"Choose {creation.Class!.SkillChoices.Count} skills ({granted} comes from your burden)";
                foreach (var id in creation.SkillOptions)
                {
                    var skill = _content.Skills[id];
                    var picked = creation.Skills.Contains(id);
                    _actions.Add(new ActionChoice($"{(picked ? "[x] " : "[ ] ")}{skill.Name} ({skill.Ability.Abbreviation()})", picked ? LogTone.Success : LogTone.Narration, () => Choose(() => creation.ToggleSkill(id)), Enabled: picked || !creation.SkillsComplete));
                }

                _actions.Add(new ActionChoice("Done", LogTone.Accent, () => Choose(() =>
                {
                    creation.ConfirmSkills();
                    Sequencer.Enqueue(Beat.Of(LogLine.Build().Add("Skills: ", LogTone.Muted).Add(string.Join(", ", creation.Skills.Select(s => _content.Skills[s].Name)), LogTone.Accent).Done()));
                }), Enabled: creation.SkillsComplete));
                break;
            case CreationStep.Expertise:
                Prompt = $"Choose {creation.Class!.ExpertiseCount} of them for expertise (double proficiency)";
                foreach (var id in creation.ExpertiseOptions)
                {
                    var picked = creation.Expertise.Contains(id);
                    _actions.Add(new ActionChoice($"{(picked ? "[x] " : "[ ] ")}{_content.Skills[id].Name}", picked ? LogTone.Success : LogTone.Narration, () => Choose(() => creation.ToggleExpertise(id)), Enabled: picked || !creation.ExpertiseComplete));
                }

                _actions.Add(new ActionChoice("Done", LogTone.Accent, () => Choose(() =>
                {
                    creation.ConfirmExpertise();
                    Sequencer.Enqueue(Beat.Of(LogLine.Build().Add("Expertise: ", LogTone.Muted).Add(string.Join(", ", creation.Expertise.Select(s => _content.Skills[s].Name)), LogTone.Accent).Done()));
                }), Enabled: creation.ExpertiseComplete));
                break;
            case CreationStep.Review:
                Prompt = $"Begin your adventure as {creation.Name}?";
                _actions.Add(new ActionChoice("Begin the adventure", LogTone.Success, () =>
                {
                    var player = creation.Build();
                    Creation = null;
                    Session = GameSession.NewGame(_content, player, _random!);
                    Screen = PixelScreen.Playing;
                    StartSession();
                }));
                _actions.Add(new ActionChoice("Start over", LogTone.Muted, () => Choose(creation.Restart)));
                break;
        }

        if (creation.Step == CreationStep.Review)
        {
            ShowSummary(creation);
        }
    }

    private void ShowScores(AbilityScores scores, string title)
    {
        var line = LogLine.Build().Add($"{title}: ", LogTone.Muted);
        foreach (var ability in AbilityExtensions.All)
        {
            line.Add($"{ability.Abbreviation()} ", LogTone.Accent).Add($"{scores[ability]} ({EventNarrator.Signed(scores.Modifier(ability))})  ");
        }

        Sequencer.Enqueue(Beat.Of(line.Done()));
    }

    private void ShowSummary(CreationFlow creation)
    {
        var player = creation.Build();
        var skills = string.Join(", ", player.ProficientSkills.Select(id => _content.Skills[id].Name + (player.ExpertiseSkills.Contains(id) ? "*" : string.Empty)).Order(StringComparer.Ordinal));
        var gear = string.Join(", ", player.Inventory.Entries.Select(e => _content.Items[e.ItemId].Name + (e.Count > 1 ? $" x{e.Count}" : string.Empty)));
        var mana = player.MaxMana > 0 ? $", MP {player.MaxMana}" : string.Empty;
        Sequencer.Enqueue(Beat.Of(
            LogLine.Of(player.Name, LogTone.Title, LogLineKind.Heading),
            LogLine.Build().Add($"{creation.Class!.Name}, {creation.Background!.Name}. ", LogTone.Narration).Add($"HP {player.MaxHp}{mana}, AC {CombatRules.ArmorClass(player, _content)}, {player.Gold} gold.", LogTone.Muted).Done(),
            LogLine.Build().Add("Skills: ", LogTone.Muted).Add(skills).Done(),
            LogLine.Build().Add("Gear: ", LogTone.Muted).Add(gear, LogTone.Gold).Done()));
    }

    // ----- play -----

    private void StartSession()
    {
        var session = Session!;
        Sequencer.Narrator.PlayerName = session.State.Player.Name;
        Sequencer.Clear();
        Tab = HudTab.Exits;
        Run(session.Begin());
    }

    private void Run(IReadOnlyList<GameEvent> events)
    {
        Sequencer.Enqueue(events);
        Refresh();
    }

    private void PlayCommand(string line)
    {
        var session = Session!;
        switch (session.Mode)
        {
            case GameMode.Exploration:
                ExplorationCommand(session, line);
                break;
            case GameMode.Dialogue:
                Sequencer.Say("Pick a numbered reply.");
                break;
            case GameMode.Combat:
                CombatCommand(session, line);
                break;
            case GameMode.Shop:
                ShopCommand(session, line);
                break;
            default:
                ShowTitle();
                break;
        }
    }

    private void ExplorationCommand(GameSession session, string line)
    {
        var command = CommandParser.Parse(line);
        switch (command.Verb)
        {
            case CommandVerb.Inventory:
                Tab = HudTab.Inventory;
                break;
            case CommandVerb.Stats:
                Tab = HudTab.Stats;
                break;
            case CommandVerb.Journal:
                Tab = HudTab.Quests;
                break;
            case CommandVerb.Help:
                Help();
                break;
            case CommandVerb.Save:
                Save(RawArgument(line) ?? DefaultSlot);
                break;
            case CommandVerb.Load:
                if (RawArgument(line) is { } slot)
                {
                    Load(slot);
                }
                else
                {
                    LoadMenu();
                }

                break;
            case CommandVerb.Quit:
                ConfirmQuit();
                break;
            default:
                Run(session.Execute(command));
                break;
        }
    }

    private void CombatCommand(GameSession session, string line)
    {
        var combat = session.Combat!;
        if (!combat.IsPlayerTurn)
        {
            return;
        }

        var words = TextNormalizer.Words(line);
        var verb = words.Count > 0 ? words[0] : string.Empty;
        var rest = words.Skip(1).ToList();
        var on = rest.FindIndex(w => w is "on" or "at");
        var what = string.Join(' ', on < 0 ? rest : rest.Take(on));
        var targetText = on < 0 ? null : string.Join(' ', rest.Skip(on + 1));

        switch (verb)
        {
            case "attack" or "hit" or "strike" or "a":
                if (Target(combat, what.Length > 0 ? what : targetText) is { } target)
                {
                    Run(session.CombatAct(new AttackAction(target)));
                }

                break;
            case "defend" or "guard" or "d":
                Run(session.CombatAct(new DefendAction()));
                break;
            case "flee" or "run" or "escape":
                if (combat.CanFlee)
                {
                    Run(session.CombatAct(new FleeAction()));
                }
                else
                {
                    Sequencer.Say("There is no escape from this fight.");
                }

                break;
            case "parley" or "talk" or "speak":
                if (combat.ParleyOptions().Count > 0)
                {
                    OpenParley(session, combat);
                }
                else
                {
                    Sequencer.Say("There is no talking to them.");
                }

                break;
            case "cast" or "use" or "throw" or "c" or "u":
                var spell = combat.SpellOptions().FirstOrDefault(s => Matches(s.Spell.Name, what) || s.Spell.Id == what);
                if (spell is not null)
                {
                    CastOrSay(session, combat, spell, targetText);
                    break;
                }

                var item = combat.UsableItems().FirstOrDefault(i => Matches(i.Name, what) || i.Id == what || i.Aliases.Any(a => a == what));
                if (item is not null)
                {
                    UseOrSay(session, combat, item, targetText);
                    break;
                }

                Sequencer.Say(what.Length == 0 ? "Cast or use what?" : $"You have no '{what}' to use here.");
                break;
            default:
                Sequencer.Say("In combat: attack, defend, flee, parley, cast <spell>, use <item>, or a number.");
                break;
        }
    }

    private void ShopCommand(GameSession session, string line)
    {
        var words = TextNormalizer.Words(line);
        var verb = words.Count > 0 ? words[0] : string.Empty;
        var what = string.Join(' ', words.Skip(1));
        switch (verb)
        {
            case "buy":
                var offer = session.ShopOffers().FirstOrDefault(o => Matches(o.Item.Name, what) || o.Item.Id == what || o.Item.Aliases.Contains(what));
                if (offer is null)
                {
                    Sequencer.Say(what.Length == 0 ? "Buy what?" : $"The merchant has no '{what}'.");
                }
                else if (!offer.CanAfford)
                {
                    Sequencer.Say($"{offer.Item.Name} costs {offer.Price} gold; you cannot afford it.");
                }
                else
                {
                    Run(session.ShopBuy(offer.Item.Id));
                }

                break;
            case "sell":
                var sale = session.ShopSellables().FirstOrDefault(s => Matches(s.Item.Name, what) || s.Item.Id == what || s.Item.Aliases.Contains(what));
                if (sale is null)
                {
                    Sequencer.Say(what.Length == 0 ? "Sell what?" : $"The merchant will not buy '{what}'.");
                }
                else
                {
                    Run(session.ShopSell(sale.Item.Id));
                }

                break;
            case "leave" or "exit" or "bye" or "done":
                Run(session.ShopLeave());
                break;
            default:
                Sequencer.Say("In a shop: buy <item>, sell <item>, or leave.");
                break;
        }
    }

    private void Refresh()
    {
        _actions.Clear();
        _submenu = null;
        Prompt = string.Empty;

        switch (Screen)
        {
            case PixelScreen.Title:
                Prompt = _content.Manifest.Title;
                _actions.Add(new ActionChoice("New game", LogTone.Success, NewGame));
                _actions.Add(new ActionChoice("Continue", LogTone.Narration, LoadMenu, Enabled: _saves.ListSlots().Count > 0));
                _actions.Add(new ActionChoice("Quit", LogTone.Muted, () => WantsQuit = true));
                break;
            case PixelScreen.Creation:
                RefreshCreation();
                break;
            default:
                RefreshPlay();
                break;
        }
    }

    private void RefreshPlay()
    {
        var session = Session!;
        switch (session.Mode)
        {
            case GameMode.Exploration:
                Prompt = $"{session.Content.Rooms[session.State.CurrentRoomId].Name}. Type a command or choose:";
                _actions.Add(new ActionChoice("Look", LogTone.Narration, () => Run(session.Execute(CommandParser.Parse("look")))));
                foreach (var action in session.ExplorationActions())
                {
                    var tone = action.Kind switch
                    {
                        Core.Views.MenuActionKind.Talk => LogTone.Speaker,
                        Core.Views.MenuActionKind.Move => LogTone.Accent,
                        Core.Views.MenuActionKind.Take => LogTone.Gold,
                        _ => LogTone.Narration,
                    };
                    _actions.Add(new ActionChoice(action.Label, tone, () => Run(session.Execute(action.Command))));
                }

                _actions.Add(new ActionChoice("Save", LogTone.System, () => Save(DefaultSlot), Hint: "Saves to the quicksave slot; type 'save <slot>' for another."));
                break;
            case GameMode.Dialogue:
                Prompt = session.Dialogue!.Dialogue.Speaker;
                foreach (var choice in session.DialogueChoices())
                {
                    var badge = choice.CheckLabel is { } label ? $"[{label} DC {choice.Dc}{Badge(choice.Mode)}] " : string.Empty;
                    _actions.Add(new ActionChoice(badge + choice.Text, LogTone.Dialogue, () => Run(session.ChooseDialogue(choice.Index))));
                }

                break;
            case GameMode.Combat:
                RefreshCombat(session, session.Combat!);
                break;
            case GameMode.Shop:
                var shop = session.Shop!.Shop;
                Prompt = $"{shop.Name}: what will you do?";
                _actions.Add(new ActionChoice("Buy", LogTone.Gold, () => OpenBuy(session)));
                _actions.Add(new ActionChoice("Sell", LogTone.Gold, () => OpenSell(session)));
                _actions.Add(new ActionChoice("Leave", LogTone.Muted, () => Run(session.ShopLeave())));
                break;
            case GameMode.GameOver:
                Prompt = "Your story ends here.";
                _actions.Add(new ActionChoice("Return to the title", LogTone.Muted, ShowTitle));
                break;
            default:
                Prompt = "The end.";
                _actions.Add(new ActionChoice("Return to the title", LogTone.Muted, ShowTitle));
                break;
        }
    }

    private void RefreshCombat(GameSession session, CombatSession combat)
    {
        if (!combat.IsPlayerTurn)
        {
            Prompt = $"{combat.CurrentActor.Name} acts...";
            return;
        }

        Prompt = $"Round {combat.Round}: what do you do?";
        _actions.Add(new ActionChoice("Attack", LogTone.Failure, () =>
        {
            if (combat.ActiveEnemies.Count == 1)
            {
                Run(session.CombatAct(new AttackAction(combat.ActiveEnemies[0].Id)));
            }
            else
            {
                OpenTargets(combat, "Attack whom?", id => Run(session.CombatAct(new AttackAction(id))));
            }
        }));

        if (combat.SpellOptions().Count > 0)
        {
            _actions.Add(new ActionChoice("Spells and techniques", LogTone.Mana, () => OpenSpells(session, combat)));
        }

        if (combat.UsableItems().Count > 0)
        {
            _actions.Add(new ActionChoice("Use an item", LogTone.Gold, () => OpenItems(session, combat)));
        }

        var defend = _content.StatusEffects[_content.Rules.DefendStatus];
        _actions.Add(new ActionChoice("Defend", LogTone.System, () => Run(session.CombatAct(new DefendAction())), Hint: defend.Description));

        if (combat.ParleyOptions().Count > 0)
        {
            _actions.Add(new ActionChoice("Parley", LogTone.Speaker, () => OpenParley(session, combat)));
        }

        if (combat.CanFlee)
        {
            var flee = _content.Rules.Flee;
            _actions.Add(new ActionChoice($"Flee [{session.Checks.Label(flee)} DC {flee.Dc}{Badge(session.Checks.ModeFor(flee, session.State))}]", LogTone.Warning, () => Run(session.CombatAct(new FleeAction()))));
        }
    }

    private void OpenTargets(CombatSession combat, string prompt, Action<string> pick)
    {
        Open(prompt, combat.ActiveEnemies.Select(enemy =>
            new ActionChoice($"{enemy.Name} (HP {enemy.CurrentHp}/{enemy.MaxHp}, AC {enemy.ArmorClass})", LogTone.Title, () => pick(enemy.Id))).ToList());
    }

    private void OpenSpells(GameSession session, CombatSession combat)
    {
        Open("Use which?", combat.SpellOptions().Select(option =>
            new ActionChoice($"{option.Spell.Name} ({option.CostText})", LogTone.Mana, () => CastOrSay(session, combat, option, null), option.CanCast, option.Spell.Description)).ToList());
    }

    private void CastOrSay(GameSession session, CombatSession combat, Core.Views.SpellOptionView option, string? targetText)
    {
        if (!option.CanCast)
        {
            Sequencer.Say($"You can't use {option.Spell.Name} right now.");
            return;
        }

        if (option.Spell.Target == SpellTarget.Self)
        {
            Run(session.CombatAct(new CastAction(option.Spell.Id, null)));
            return;
        }

        if (targetText is not null)
        {
            if (Target(combat, targetText) is { } target)
            {
                Run(session.CombatAct(new CastAction(option.Spell.Id, target)));
            }

            return;
        }

        if (combat.ActiveEnemies.Count == 1)
        {
            Run(session.CombatAct(new CastAction(option.Spell.Id, combat.ActiveEnemies[0].Id)));
        }
        else
        {
            OpenTargets(combat, $"{option.Spell.Name} at whom?", id => Run(session.CombatAct(new CastAction(option.Spell.Id, id))));
        }
    }

    private void OpenItems(GameSession session, CombatSession combat)
    {
        var player = session.State.Player;
        Open("Use which item?", combat.UsableItems().Select(item =>
            new ActionChoice($"{item.Name} x{player.Inventory.CountOf(item.Id)}", LogTone.Gold, () => UseOrSay(session, combat, item, null), Hint: item.Description)).ToList());
    }

    private void UseOrSay(GameSession session, CombatSession combat, ItemDef item, string? targetText)
    {
        if (item.Use!.Target == SpellTarget.Self)
        {
            Run(session.CombatAct(new UseItemAction(item.Id)));
            return;
        }

        if (targetText is not null)
        {
            if (Target(combat, targetText) is { } target)
            {
                Run(session.CombatAct(new UseItemAction(item.Id, target)));
            }

            return;
        }

        if (combat.ActiveEnemies.Count == 1)
        {
            Run(session.CombatAct(new UseItemAction(item.Id, combat.ActiveEnemies[0].Id)));
        }
        else
        {
            OpenTargets(combat, $"{item.Name} at whom?", id => Run(session.CombatAct(new UseItemAction(item.Id, id))));
        }
    }

    private void OpenParley(GameSession session, CombatSession combat)
    {
        Open("What do you say?", combat.ParleyOptions().Select(option =>
            new ActionChoice($"[{option.CheckLabel} DC {option.Dc}{Badge(option.Mode)}] {option.Label}", LogTone.Dialogue, () => Run(session.CombatAct(new ParleyAction(option.Index))))).ToList());
    }

    private void OpenBuy(GameSession session)
    {
        var offers = session.ShopOffers();
        if (offers.Count == 0)
        {
            Sequencer.Say("There is nothing left to buy.");
            return;
        }

        Open("Buy which?", offers.Select(offer =>
            new ActionChoice($"{offer.Item.Name} ({offer.Price} gp{(offer.Remaining is { } left ? $", {left} left" : string.Empty)})", LogTone.Gold, () => Run(session.ShopBuy(offer.Item.Id)), offer.CanAfford, offer.Item.Description)).ToList());
    }

    private void OpenSell(GameSession session)
    {
        var sellables = session.ShopSellables();
        if (sellables.Count == 0)
        {
            Sequencer.Say("You have nothing the merchant will buy.");
            return;
        }

        Open("Sell which?", sellables.Select(sale =>
            new ActionChoice($"{sale.Item.Name}{(sale.Count > 1 ? $" x{sale.Count}" : string.Empty)} (sells for {sale.Price} gp)", LogTone.Gold, () => Run(session.ShopSell(sale.Item.Id)), Hint: sale.Item.Description)).ToList());
    }

    private string? Target(CombatSession combat, string? text)
    {
        var enemies = combat.ActiveEnemies;
        if (string.IsNullOrWhiteSpace(text))
        {
            if (enemies.Count == 1)
            {
                return enemies[0].Id;
            }

            Sequencer.Say("Attack whom? " + string.Join(", ", enemies.Select(e => e.Name)));
            return null;
        }

        var match = enemies.FirstOrDefault(e => Matches(e.Name, text) || e.Id == text);
        if (match is null)
        {
            Sequencer.Say($"There is no '{text}' to target.");
        }

        return match?.Id;
    }

    private void ConfirmQuit()
    {
        Open("Return to the title screen? Unsaved progress will be lost.",
        [
            new ActionChoice("Yes, return to the title", LogTone.Warning, ShowTitle),
            new ActionChoice("No, keep playing", LogTone.Narration, Back),
        ]);
    }

    private void Open(string prompt, List<ActionChoice> items)
    {
        items.Add(new ActionChoice("Back", LogTone.Muted, Back));
        _submenu = items;
        Prompt = prompt;
    }

    // Runs a creation step and rebuilds the buttons for the next one.
    private void Choose(Action step)
    {
        step();
        Refresh();
    }

    // ----- saves -----

    private void Save(string slot)
    {
        var session = Session!;
        if (!session.CanSave)
        {
            Sequencer.Say("You can only save while exploring.");
            return;
        }

        try
        {
            _saves.Save(session.State, _content, slot);
            Sequencer.Say($"Game saved to slot '{slot}'.", LogTone.Success);
        }
        catch (SaveGameException ex)
        {
            Sequencer.Say(ex.Message, LogTone.Failure);
        }
    }

    private void LoadMenu()
    {
        var slots = _saves.ListSlots();
        if (slots.Count == 0)
        {
            Sequencer.Say("There are no saved games yet.");
            return;
        }

        Open("Load which game?", slots.Select(s =>
        {
            var room = _content.Rooms.TryGetValue(s.RoomId, out var def) ? def.Name : s.RoomId;
            return new ActionChoice($"{s.Slot}: {s.PlayerName}, level {s.Level}, {room}", LogTone.Accent, () => Load(s.Slot), Hint: s.SavedAtUtc.ToLocalTime().ToString("g", System.Globalization.CultureInfo.CurrentCulture));
        }).ToList());
    }

    private void Load(string slot)
    {
        try
        {
            var loaded = _saves.Load(slot, _content);
            var random = NewRandom();
            Session = GameSession.Restore(_content, loaded.State, random);
            Creation = null;
            Screen = PixelScreen.Playing;
            StartSession();
            if (loaded.ContentVersionMismatch)
            {
                Sequencer.Say($"This save was made with content version {loaded.Save.ContentVersion}; the game is now {_content.Manifest.ContentVersion}. Some things may have changed.", LogTone.Warning);
            }

            Sequencer.Say($"Loaded '{slot}'.", LogTone.Success);
        }
        catch (SaveGameException ex)
        {
            Sequencer.Say(ex.Message, LogTone.Failure);
        }
    }

    private void Help()
    {
        Sequencer.Enqueue(new Beat(HelpRows.Select(row => LogLine.Of(row, LogTone.Muted)).Prepend(LogLine.Of("Commands", LogTone.Title, LogLineKind.Heading)).ToList(), []));
    }

    private IRandomSource NewRandom()
    {
        Seed = _fixedSeed ?? (ulong)Random.Shared.NextInt64();
        return _randomFactory(Seed);
    }

    private static string Badge(RollMode mode) => mode switch
    {
        RollMode.Advantage => " adv",
        RollMode.Disadvantage => " dis",
        _ => string.Empty,
    };

    // Slot names keep their exact spelling (e.g. "my-save"), so they bypass the word normalizer.
    private static string? RawArgument(string input)
    {
        var space = input.IndexOf(' ', StringComparison.Ordinal);
        var argument = space < 0 ? string.Empty : input[(space + 1)..].Trim();
        return argument.Length == 0 ? null : argument;
    }

    private static bool Matches(string label, string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        var plain = label.StartsWith('[') && label.Contains("] ", StringComparison.Ordinal) ? label[(label.IndexOf("] ", StringComparison.Ordinal) + 2)..] : label;
        return string.Equals(plain, text, StringComparison.OrdinalIgnoreCase)
            || plain.Contains(text, StringComparison.OrdinalIgnoreCase);
    }
}
