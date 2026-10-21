using RpgEngine.Core.Content;
using RpgEngine.Core.Engine;
using RpgEngine.Core.Persistence;
using RpgEngine.Pixel.Game;
using RpgEngine.Pixel.Sequencing;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Pixel;

/// <summary>Drives the whole client without a window, from the title through creation into play.</summary>
public sealed class PixelGameTests : IDisposable
{
    private readonly string _saves = Path.Combine(Path.GetTempPath(), "rpg-pixel-saves-" + Guid.NewGuid().ToString("N"));
    private readonly GameContent _content = SessionFixture.Load();

    public void Dispose()
    {
        if (Directory.Exists(_saves))
        {
            Directory.Delete(_saves, recursive: true);
        }
    }

    private PixelGame NewGame() => new(
        _content,
        new SaveService(_saves, new FixedTimeProvider(new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero))),
        _ => new LuckyRandomSource(),
        seed: 1,
        new EventSequencer(new EventNarrator(), charactersPerSecond: 100000));

    private static string Log(PixelGame game) => string.Join("\n", game.Sequencer.Lines.Select(l => l.Text));

    private static void Settle(PixelGame game)
    {
        for (var i = 0; i < 50 && !(game.Sequencer.IsIdle && (game.Session?.Combat is not { IsPlayerTurn: false })); i++)
        {
            game.Update(10);
        }
    }

    /// <summary>Title, name, Fighter, Orphan, the standard array kept, Athletics and Persuasion, begin.</summary>
    private PixelGame StartedGame()
    {
        var game = NewGame();
        game.Pick(0);
        game.Submit("Tester");
        game.Pick(0);
        game.Pick(0);
        game.Pick(0);
        game.Pick(0);
        game.Pick(0);
        game.Pick(2);
        game.Pick(4);
        game.Pick(0);
        Settle(game);
        return game;
    }

    [Fact]
    public void Title_OffersNewGameContinueAndQuit()
    {
        var game = NewGame();

        Assert.Equal(PixelScreen.Title, game.Screen);
        Assert.Equal(["New game", "Continue", "Quit"], game.Actions.Select(a => a.Label));
        Assert.False(game.Actions[1].Enabled);
        Assert.Equal("Test Adventure", game.Prompt);

        game.Submit("4");
        Assert.False(game.WantsQuit);

        game.Submit("3");
        Assert.True(game.WantsQuit);
    }

    [Fact]
    public void Creation_WalksEveryStep_AndBeginsInTheStartRoom()
    {
        var game = NewGame();

        game.Pick(0);
        Assert.Equal((PixelScreen.Creation, CreationStep.Name), (game.Screen, game.Creation!.Step));
        Assert.True(game.InputIsName);
        Assert.Empty(game.Actions);

        game.Submit("   ");
        Assert.Equal(CreationStep.Name, game.Creation.Step);
        game.Submit("Tester");
        Assert.Equal(CreationStep.Calling, game.Creation.Step);
        Assert.Equal(["Fighter", "Wizard"], game.Actions.Select(a => a.Label));

        game.Submit("wizard");
        Assert.Equal("Wizard", game.Creation.Class!.Name);
        Assert.Equal(["Orphan", "Bailiff"], game.Actions.Select(a => a.Label));

        game.Pick(0);
        Assert.StartsWith("Standard array", game.Actions[0].Label, StringComparison.Ordinal);
        game.Pick(1);
        Settle(game);
        Assert.Equal(CreationStep.ScoreReview, game.Creation.Step);
        Assert.Contains("(6)] = 18", Log(game), StringComparison.Ordinal);
        Assert.Contains("Suggested for a Wizard: STR 18 (+4)", Log(game), StringComparison.Ordinal);

        game.Pick(1);
        Assert.Equal(CreationStep.AssignScores, game.Creation.Step);
        Assert.Equal("Score for Strength:", game.Prompt);
        for (var i = 0; i < 6; i++)
        {
            game.Pick(0);
        }

        Assert.Equal(CreationStep.Skills, game.Creation.Step);
        Assert.Equal(["[ ] Religion (INT)", "[ ] Insight (WIS)", "[ ] Perception (WIS)", "Done"], game.Actions.Select(a => a.Label));
        Assert.False(game.Actions[3].Enabled);

        game.Pick(0);
        game.Pick(2);
        Assert.Equal("[x] Religion (INT)", game.Actions[0].Label);
        Assert.False(game.Actions[1].Enabled);
        game.Pick(0);
        Assert.True(game.Actions[1].Enabled);
        game.Pick(1);
        game.Pick(3);

        Assert.Equal(CreationStep.Expertise, game.Creation.Step);
        Assert.Equal(["[ ] Insight", "[ ] Perception", "Done"], game.Actions.Select(a => a.Label));
        game.Pick(1);
        game.Pick(2);

        Assert.Equal(CreationStep.Review, game.Creation.Step);
        Assert.Equal(["Begin the adventure", "Start over"], game.Actions.Select(a => a.Label));
        Settle(game);
        Assert.Contains("Wizard, Orphan.", Log(game), StringComparison.Ordinal);
        Assert.Contains("Skills: Insight, Perception*, Stealth", Log(game), StringComparison.Ordinal);

        game.Pick(0);
        Settle(game);

        Assert.Equal(PixelScreen.Playing, game.Screen);
        Assert.Null(game.Creation);
        Assert.Equal("wizard", game.Session!.State.Player.ClassId);
        Assert.Equal("start", game.Session.State.CurrentRoomId);
        Assert.Contains("Gate", Log(game), StringComparison.Ordinal);
        Assert.Equal("Look", game.Actions[0].Label);
        Assert.Equal("Save", game.Actions[^1].Label);
        Assert.Equal(1UL, game.Seed);
    }

    [Fact]
    public void StartOver_ReturnsToTheName()
    {
        var game = NewGame();
        game.Pick(0);
        game.Submit("Tester");
        game.Pick(0);
        game.Pick(0);
        game.Pick(0);
        game.Pick(0);
        game.Pick(0);
        game.Pick(2);
        game.Pick(4);

        game.Pick(1);

        Assert.Equal(CreationStep.Name, game.Creation!.Step);
        Assert.Equal(string.Empty, game.Creation.Name);
    }

    [Fact]
    public void TypedCommands_AndNumbers_DriveExploration()
    {
        var game = StartedGame();
        var before = game.Sequencer.Lines.Count;

        game.Submit("look");
        Settle(game);
        Assert.True(game.Sequencer.Lines.Count > before);

        game.Submit("inventory");
        Assert.Equal(HudTab.Inventory, game.Tab);
        game.Submit("journal");
        Assert.Equal(HudTab.Quests, game.Tab);
        game.Submit("stats");
        Assert.Equal(HudTab.Stats, game.Tab);

        game.Submit("help");
        Settle(game);
        Assert.Contains("Commands", Log(game), StringComparison.Ordinal);

        game.Submit("99");
        Settle(game);
        Assert.Contains("There is no option 99.", Log(game), StringComparison.Ordinal);

        var talk = game.Actions.Select((a, i) => (a, i)).First(x => x.a.Label.Contains("Sage", StringComparison.Ordinal)).i;
        game.Submit((talk + 1).ToString());
        Settle(game);

        Assert.Equal(GameMode.Dialogue, game.Session!.Mode);
        Assert.Equal("Sage", game.Prompt);
        Assert.Equal(["Fight me.", "Bless me.", "Bye."], game.Actions.Select(a => a.Label));

        game.Submit("hello");
        Settle(game);
        Assert.Contains("Pick a numbered reply.", Log(game), StringComparison.Ordinal);

        game.Pick(2);
        Settle(game);
        Assert.Equal(GameMode.Exploration, game.Session.Mode);
    }

    [Fact]
    public void Combat_RunsEnemyTurnsOnUpdate_AndTakesTypedActions()
    {
        var game = StartedGame();

        // Every roll is at its maximum, so the rat wins initiative and bites first.
        game.Submit("north");
        Assert.Equal(GameMode.Combat, game.Session!.Mode);
        Assert.False(game.Session.Combat!.IsPlayerTurn);
        Assert.Empty(game.Actions);
        Assert.Equal("Giant Rat acts...", game.Prompt);

        Settle(game);

        Assert.True(game.Session.Combat!.IsPlayerTurn);
        Assert.Contains(game.Sequencer.TakeCues(), c => c is DiceBannerCue { Label: "Bite" });
        Assert.True(game.Session.State.Player.CurrentHp < game.Session.State.Player.MaxHp);
        Assert.Equal("Attack", game.Actions[0].Label);
        Assert.Contains(game.Actions, a => a.Label == "Spells and techniques");
        Assert.Contains(game.Actions, a => a.Label == "Use an item");
        Assert.Contains(game.Actions, a => a.Label == "Defend");
        Assert.Contains(game.Actions, a => a.Label.StartsWith("Flee [", StringComparison.Ordinal));

        game.Submit("use potion");
        Settle(game);
        Assert.Contains("You use the Potion", Log(game), StringComparison.Ordinal);
        Settle(game);

        game.Submit("attack rat");
        Settle(game);

        Assert.Contains("VICTORY", Log(game), StringComparison.Ordinal);
        Assert.Equal(GameMode.Exploration, game.Session.Mode);
        Assert.Equal(25, game.Session.State.Player.Xp);
        Assert.Contains(game.Sequencer.TakeCues(), c => c is FellCue { Name: "Giant Rat" });
    }

    [Fact]
    public void Combat_SubmenusOpenAndGoBack()
    {
        var game = StartedGame();
        game.Submit("north");
        Settle(game);

        game.Pick(1);
        Assert.True(game.InSubmenu);
        Assert.Equal("Use which?", game.Prompt);
        Assert.Equal("Back", game.Actions[^1].Label);
        Assert.Contains(game.Actions, a => a.Label.StartsWith("Second Wind", StringComparison.Ordinal));

        game.Back();
        Assert.False(game.InSubmenu);
        Assert.Equal("Attack", game.Actions[0].Label);

        game.Submit("dance");
        Settle(game);
        Assert.Contains("In combat: attack, defend", Log(game), StringComparison.Ordinal);

        game.Submit("defend");
        Settle(game);
        Assert.Contains("You are now Defending.", Log(game), StringComparison.Ordinal);
    }

    [Fact]
    public void SaveQuitAndContinue_RoundTrip()
    {
        var game = StartedGame();

        game.Submit("save slot-a");
        Settle(game);
        Assert.Contains("Game saved to slot 'slot-a'.", Log(game), StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(_saves, "slot-a.json")));

        game.Submit("quit");
        Assert.True(game.InSubmenu);
        game.Pick(1);
        Assert.Equal(PixelScreen.Playing, game.Screen);
        game.Submit("quit");
        game.Pick(0);
        Assert.Equal(PixelScreen.Title, game.Screen);
        Assert.True(game.Actions[1].Enabled);

        game.Pick(1);
        Assert.Equal("Load which game?", game.Prompt);
        Assert.StartsWith("slot-a: Tester, level 1, Gate", game.Actions[0].Label, StringComparison.Ordinal);
        game.Pick(0);
        Settle(game);

        Assert.Equal(PixelScreen.Playing, game.Screen);
        Assert.Equal("start", game.Session!.State.CurrentRoomId);
        Assert.Contains("Loaded 'slot-a'.", Log(game), StringComparison.Ordinal);

        game.Submit("load nope");
        Settle(game);
        Assert.Contains("There is no save named 'nope'.", Log(game), StringComparison.Ordinal);
    }

    [Fact]
    public void Title_TypedWords_WorkToo()
    {
        var game = NewGame();

        game.Submit("continue");
        game.Update(10);
        Assert.Contains("There are no saved games yet.", Log(game), StringComparison.Ordinal);

        game.Submit("fly");
        game.Update(10);
        Assert.Contains("Type new, continue, or quit.", Log(game), StringComparison.Ordinal);

        game.Submit("new");
        Assert.Equal(PixelScreen.Creation, game.Screen);
    }

    [Fact]
    public void Input_IsEditedCharacterByCharacter()
    {
        var game = NewGame();

        foreach (var c in "ne\tw")
        {
            game.Type(c);
        }

        Assert.Equal("new", game.Input);
        game.Backspace();
        game.Type('w');
        game.SubmitInput();

        Assert.Equal(string.Empty, game.Input);
        Assert.Equal(PixelScreen.Creation, game.Screen);
    }

    [Fact]
    public void EmptyLine_SkipsTheTypewriter()
    {
        var game = new PixelGame(_content, new SaveService(_saves, TimeProvider.System), _ => new LuckyRandomSource(), seed: 1, new EventSequencer(new EventNarrator(), charactersPerSecond: 1));

        Assert.False(game.Sequencer.IsIdle);
        game.Submit(string.Empty);

        Assert.True(game.Sequencer.IsIdle);
    }

    [Fact]
    public void EndingTheGame_OffersTheTitle()
    {
        var game = StartedGame();

        game.Submit("pray altar");
        Settle(game);

        Assert.Equal(GameMode.Ended, game.Session!.Mode);
        Assert.Contains("THE END", Log(game), StringComparison.Ordinal);
        Assert.Equal(["Return to the title"], game.Actions.Select(a => a.Label));

        game.Submit("anything");
        Assert.Equal(PixelScreen.Title, game.Screen);
    }
}
