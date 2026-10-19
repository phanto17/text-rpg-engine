using RpgEngine.CLI.Rendering;
using RpgEngine.CLI.Screens;
using RpgEngine.Core.Content;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Engine;
using RpgEngine.Core.Persistence;
using Spectre.Console;

namespace RpgEngine.CLI;

/// <summary>The application loop: title screen, character creation, then the play loop by game mode.</summary>
public sealed class GameApp
{
    private readonly UiContext _ui;
    private readonly Func<ulong, IRandomSource> _randomFactory;
    private readonly TitleScreen _title;
    private readonly CharacterCreationScreen _creation;
    private readonly ExplorationScreen _exploration;
    private readonly DialogueScreen _dialogue;
    private readonly CombatScreen _combat;
    private readonly ShopScreen _shop;
    private readonly EndScreens _end;

    public GameApp(IAnsiConsole console, GameContent content, CliOptions options, Func<ulong, IRandomSource>? randomFactory = null, TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(options);

        _ui = new UiContext(console, content, options, new SaveService(options.SavesDirectory, time ?? TimeProvider.System));
        _randomFactory = randomFactory ?? (seed => new Pcg32RandomSource(seed));
        _title = new TitleScreen(_ui);
        _creation = new CharacterCreationScreen(_ui);
        _exploration = new ExplorationScreen(_ui, new InfoScreens(_ui));
        _dialogue = new DialogueScreen(_ui);
        _combat = new CombatScreen(_ui);
        _shop = new ShopScreen(_ui);
        _end = new EndScreens(_ui);
    }

    /// <summary>Runs until the player quits; returns the process exit code.</summary>
    public int Run()
    {
        while (true)
        {
            GameSession? session = null;
            switch (_title.Show())
            {
                case TitleChoice.NewGame:
                    var random = NewRandom();
                    var player = _creation.Run(new DiceRoller(random));
                    session = GameSession.NewGame(_ui.Content, player, random);
                    break;
                case TitleChoice.Continue:
                    session = Load(null);
                    break;
                default:
                    return 0;
            }

            if (session is not null && Play(session) == ScreenAction.Quit)
            {
                return 0;
            }
        }
    }

    private ScreenAction Play(GameSession session)
    {
        Start(session);
        while (true)
        {
            var result = session.Mode switch
            {
                GameMode.Exploration => _exploration.Turn(session),
                GameMode.Dialogue => _dialogue.Turn(session),
                GameMode.Combat => _combat.Turn(session),
                GameMode.Shop => _shop.Turn(session),
                GameMode.GameOver => _end.GameOver(),
                _ => _end.Ended(),
            };

            switch (result.Action)
            {
                case ScreenAction.Load:
                    if (Load(result.Slot) is { } loaded)
                    {
                        session = loaded;
                        Start(session);
                    }

                    break;
                case ScreenAction.Title:
                case ScreenAction.Quit:
                    return result.Action;
            }
        }
    }

    private void Start(GameSession session)
    {
        _ui.Renderer.PlayerName = session.State.Player.Name;
        _ui.Renderer.Render(session.Begin());
    }

    private GameSession? Load(string? slot)
    {
        if (slot is null)
        {
            var slots = _ui.Saves.ListSlots();
            if (slots.Count == 0)
            {
                _ui.Say(Theme.System, "There are no saved games yet.");
                return null;
            }

            var labels = slots.Select(s =>
            {
                var room = _ui.Content.Rooms.TryGetValue(s.RoomId, out var def) ? def.Name : s.RoomId;
                return $"{Theme.Paint(Theme.Accent, s.Slot)} - {Markup.Escape(s.PlayerName)}, level {s.Level}, {Markup.Escape(room)} [{Theme.Muted}]({s.SavedAtUtc.ToLocalTime():g})[/]";
            }).ToList();

            var index = _ui.Prompter.Choose($"[{Theme.Accent}]Load which game?[/]", labels, allowBack: true);
            if (index < 0)
            {
                return null;
            }

            slot = slots[index].Slot;
        }

        try
        {
            var loaded = _ui.Saves.Load(slot, _ui.Content);
            if (loaded.ContentVersionMismatch)
            {
                _ui.Say(Theme.Warning, $"This save was made with content version {loaded.Save.ContentVersion}; the game is now {_ui.Content.Manifest.ContentVersion}. Some things may have changed.");
            }

            _ui.Say(Theme.Success, $"Loaded '{slot}'.");
            return GameSession.Restore(_ui.Content, loaded.State, NewRandom());
        }
        catch (SaveGameException ex)
        {
            _ui.Say(Theme.Failure, ex.Message);
            return null;
        }
    }

    private IRandomSource NewRandom()
    {
        _ui.Seed = _ui.Options.Seed ?? (ulong)Random.Shared.NextInt64();
        return _randomFactory(_ui.Seed);
    }
}
