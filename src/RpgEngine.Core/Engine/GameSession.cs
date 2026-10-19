using RpgEngine.Core.Characters;
using RpgEngine.Core.Checks;
using RpgEngine.Core.Combat;
using RpgEngine.Core.Commands;
using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dialogue;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Events;
using RpgEngine.Core.Exploration;
using RpgEngine.Core.Shop;
using RpgEngine.Core.Views;
using RpgEngine.Core.World;

namespace RpgEngine.Core.Engine;

/// <summary>
/// One playthrough. Routes player input to exploration, dialogue, combat, or a shop based on
/// <see cref="Mode"/>, carries out queued transitions, and returns the resulting events.
/// </summary>
public sealed class GameSession
{
    private readonly ExplorationService _exploration;
    private readonly DialogueRunner _dialogue;
    private readonly CombatEngine _combat;
    private readonly TextFormatter _text;
    private bool _begun;

    private GameSession(GameContent content, GameState state, IRandomSource random)
    {
        Content = content;
        State = state;
        var dice = new DiceRoller(random);
        Checks = new CheckRunner(content, new CheckResolver(dice, content.Rules.NaturalCritsOnChecks));
        _text = new TextFormatter(content);
        var effects = new EffectApplier(content, dice, Checks, _text);
        _exploration = new ExplorationService(content, dice, Checks, effects, _text);
        _dialogue = new DialogueRunner(content, Checks, effects, _text);
        _combat = new CombatEngine(content, dice, Checks, effects, _text);
    }

    public GameContent Content { get; }

    public GameState State { get; }

    public CheckRunner Checks { get; }

    public GameMode Mode { get; private set; } = GameMode.Exploration;

    public DialogueSession? Dialogue { get; private set; }

    public CombatSession? Combat { get; private set; }

    public ShopSession? Shop { get; private set; }

    public string? Epilogue { get; private set; }

    public bool CanSave => Mode == GameMode.Exploration;

    public static GameSession NewGame(GameContent content, PlayerCharacter player, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(random);
        return new GameSession(content, new GameState(player, WorldState.CreateNew(content), content.Manifest.StartRoom), random);
    }

    public static GameSession Restore(GameContent content, GameState state, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(random);
        return new GameSession(content, state, random) { _begun = true };
    }

    /// <summary>Starts a new game (entering the start room) or, for a restored game, re-describes the current room.</summary>
    public IReadOnlyList<GameEvent> Begin()
    {
        var log = new TurnLog();
        if (_begun)
        {
            log.Add(new RoomDescribedEvent(_exploration.Describe(State, full: true), Entered: false));
            return log.Events;
        }

        _begun = true;
        log.Add(new NarrationEvent(TextStyle.Narration, _text.Format(Content.Manifest.Intro, State.Player)));
        log.Request(new MoveToTransition(Content.Manifest.StartRoom));
        return Process(log);
    }

    public RoomView DescribeRoom(bool full = false) => _exploration.Describe(State, full);

    public IReadOnlyList<MenuAction> ExplorationActions()
    {
        RequireMode(GameMode.Exploration);
        return _exploration.GetActions(State);
    }

    public IReadOnlyList<GameEvent> Execute(ParsedCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        RequireMode(GameMode.Exploration);
        var log = new TurnLog();
        _exploration.Execute(command, State, log);
        return Process(log);
    }

    public IReadOnlyList<DialogueChoiceView> DialogueChoices()
    {
        RequireMode(GameMode.Dialogue);
        return _dialogue.GetChoices(Dialogue!, State);
    }

    public IReadOnlyList<GameEvent> ChooseDialogue(int choiceIndex)
    {
        RequireMode(GameMode.Dialogue);
        var log = new TurnLog();
        _dialogue.Choose(Dialogue!, choiceIndex, State, log);
        return Process(log);
    }

    public IReadOnlyList<GameEvent> CombatAct(PlayerAction action)
    {
        RequireMode(GameMode.Combat);
        var log = new TurnLog();
        Combat!.Act(action, log);
        return Process(log);
    }

    public IReadOnlyList<GameEvent> CombatEnemyTurn()
    {
        RequireMode(GameMode.Combat);
        var log = new TurnLog();
        Combat!.RunEnemyTurn(log);
        return Process(log);
    }

    public IReadOnlyList<ShopOfferView> ShopOffers()
    {
        RequireMode(GameMode.Shop);
        return Shop!.Offers();
    }

    public IReadOnlyList<ShopSaleView> ShopSellables()
    {
        RequireMode(GameMode.Shop);
        return Shop!.Sellables();
    }

    public IReadOnlyList<GameEvent> ShopBuy(string itemId)
    {
        RequireMode(GameMode.Shop);
        var log = new TurnLog();
        Shop!.Buy(itemId, log);
        return Process(log);
    }

    public IReadOnlyList<GameEvent> ShopSell(string itemId)
    {
        RequireMode(GameMode.Shop);
        var log = new TurnLog();
        Shop!.Sell(itemId, log);
        return Process(log);
    }

    public IReadOnlyList<GameEvent> ShopLeave()
    {
        RequireMode(GameMode.Shop);
        Shop!.Leave();
        return Process(new TurnLog());
    }

    public JournalView Journal()
    {
        var quests = Content.QuestList
            .Where(quest => State.World.GetQuestStage(quest.Id) > 0)
            .Select(quest =>
            {
                var stage = State.World.GetQuestStage(quest.Id);
                var current = quest.Stages.Where(s => s.Stage <= stage).MaxBy(s => s.Stage)!;
                return new QuestLogEntry(quest.Name, stage, _text.Format(current.Journal, State.Player), stage >= quest.Stages.Max(s => s.Stage));
            })
            .ToList();

        var reputation = Content.FactionList
            .Where(faction => State.World.Reputation.ContainsKey(faction.Id))
            .Select(faction => new ReputationEntry(faction.Name, State.World.GetReputation(faction.Id)))
            .ToList();

        return new JournalView(quests, reputation);
    }

    // Closes finished dialogue/combat, then carries out queued transitions until the game settles.
    private IReadOnlyList<GameEvent> Process(TurnLog log)
    {
        if (Mode == GameMode.Dialogue && Dialogue!.IsOver)
        {
            Dialogue = null;
            Mode = GameMode.Exploration;
        }

        if (Mode == GameMode.Combat && Combat!.IsOver)
        {
            Combat = null;
            Mode = GameMode.Exploration;
        }

        if (Mode == GameMode.Shop && Shop!.IsOver)
        {
            Shop = null;
            Mode = GameMode.Exploration;
        }

        while (true)
        {
            if (!State.Player.IsAlive)
            {
                Mode = GameMode.GameOver;
                Dialogue = null;
                Combat = null;
                Shop = null;
                log.ClearTransitions();
                log.Add(new PlayerDefeatedEvent(_text.Format(Content.Manifest.GameOverText, State.Player)));
                break;
            }

            if (!log.TryTakeTransition(out var transition))
            {
                break;
            }

            switch (transition)
            {
                case MoveToTransition move:
                    _exploration.EnterRoom(move.RoomId, State, log);
                    break;
                case StartDialogueTransition talk:
                    var dialogue = _dialogue.Start(talk.DialogueId, State, log);
                    (Dialogue, Mode) = dialogue.IsOver ? (null, GameMode.Exploration) : (dialogue, GameMode.Dialogue);
                    break;
                case StartCombatTransition fight:
                    Combat = _combat.Begin(fight.EncounterId, State, log);
                    Mode = GameMode.Combat;
                    break;
                case StartShopTransition open:
                    var shop = Content.Shops[open.ShopId];
                    Shop = new ShopSession(Content, shop, State);
                    Mode = GameMode.Shop;
                    log.Add(new DialogueLineEvent(shop.Keeper, _text.Format(shop.Greeting, State.Player)));
                    break;
                case EndGameTransition:
                    var epilogue = Content.Manifest.Epilogues.First(e => ConditionEvaluator.AllPass(e.Conditions, State));
                    Epilogue = _text.Format(epilogue.Text, State.Player);
                    Mode = GameMode.Ended;
                    Dialogue = null;
                    Combat = null;
                    Shop = null;
                    log.ClearTransitions();
                    log.Add(new EpilogueEvent(Epilogue));
                    return log.Events;
            }
        }

        return log.Events;
    }

    private void RequireMode(GameMode mode)
    {
        if (Mode != mode)
        {
            throw new InvalidOperationException($"This action needs {mode} mode, but the game is in {Mode} mode.");
        }
    }
}
