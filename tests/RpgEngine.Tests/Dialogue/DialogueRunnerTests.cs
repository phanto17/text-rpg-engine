using RpgEngine.Core.Dialogue;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Events;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Dialogue;

public sealed class DialogueRunnerTests
{
    private static (ExplorationHarness Game, DialogueSession Session, TurnLog Log) Start(params int[] dice)
    {
        var game = new ExplorationHarness(dice: dice);
        var log = new TurnLog();
        var session = game.Dialogue.Start("sage", game.World.State, log);
        return (game, session, log);
    }

    private static TurnLog Choose(ExplorationHarness game, DialogueSession session, string text)
    {
        var log = new TurnLog();
        var choice = game.Dialogue.GetChoices(session, game.World.State).Single(c => c.Text == text);
        game.Dialogue.Choose(session, choice.Index, game.World.State, log);
        return log;
    }

    private static IReadOnlyList<string> Lines(TurnLog log) => log.Events.OfType<DialogueLineEvent>().Select(e => e.Text).ToList();

    [Fact]
    public void Start_FollowsNextLinksUntilChoices()
    {
        var (game, session, log) = Start();

        Assert.Equal(new[] { "Greetings, Tester.", "What do you seek?" }, Lines(log));
        Assert.All(log.Events.OfType<DialogueLineEvent>(), line => Assert.Equal("Old Sage", line.Speaker));
        Assert.Equal("hub", session.CurrentNode!.Id);
        Assert.False(session.IsOver);
        Assert.True(game.World.World.HasFlag("met"));
        Assert.Equal("sage", session.Dialogue.Id);
    }

    [Fact]
    public void GetChoices_HidesFailedConditionsAndBadgesChecks()
    {
        var (game, session, _) = Start();

        var choices = game.Dialogue.GetChoices(session, game.World.State);

        Assert.Equal(new[] { "Who are you?", "Trust me.", "Read him.", "Fight!", "Goodbye." }, choices.Select(c => c.Text));
        var trust = choices[1];
        Assert.Equal(("Persuasion", 14, RollMode.Normal), (trust.CheckLabel, trust.Dc, trust.Mode));
        Assert.Null(choices[0].CheckLabel);
        Assert.Null(choices[0].Dc);
    }

    [Fact]
    public void OnceChoice_DisappearsAfterUse()
    {
        var (game, session, _) = Start();

        var log = Choose(game, session, "Who are you?");

        Assert.Equal(new[] { "A keeper of secrets.", "What do you seek?" }, Lines(log));
        Assert.DoesNotContain(game.Dialogue.GetChoices(session, game.World.State), c => c.Text == "Who are you?");
    }

    [Fact]
    public void Persuasion_Success_SetsPermanentConsequenceAndChangesGreeting()
    {
        var (game, session, _) = Start(18);

        var log = Choose(game, session, "Trust me.");

        Assert.True(Assert.IsType<CheckRolledEvent>(log.Events[0]).Result.Success);
        Assert.Equal(new[] { "Very well." }, Lines(log));
        Assert.Single(log.Events.OfType<ReputationChangedEvent>());
        Assert.True(session.IsOver);
        Assert.True(game.World.World.HasFlag("sage_friend"));

        var again = new TurnLog();
        var second = game.Dialogue.Start("sage", game.World.State, again);
        Assert.Equal(new[] { "Welcome back, friend." }, Lines(again));
        Assert.Equal("friend", second.CurrentNode!.Id);
    }

    [Fact]
    public void Persuasion_Failure_LocksTheCheckForever()
    {
        var (game, session, _) = Start(2);

        var log = Choose(game, session, "Trust me.");

        Assert.Equal(new[] { "No." }, Lines(log));
        Assert.True(session.IsOver);

        var cold = game.Dialogue.Start("sage", game.World.State, new TurnLog());
        Assert.True(cold.IsOver);

        game.World.World.ClearFlag("sage_angry");
        var retry = game.Dialogue.Start("sage", game.World.State, new TurnLog());
        Assert.DoesNotContain(game.Dialogue.GetChoices(retry, game.World.State), c => c.Text == "Trust me.");
    }

    [Fact]
    public void Insight_Success_GrantsAdvantageOnPersuasion()
    {
        var (game, session, _) = Start(15);

        var log = Choose(game, session, "Read him.");

        Assert.Equal(new[] { "He is afraid.", "What do you seek?" }, Lines(log));
        var trust = game.Dialogue.GetChoices(session, game.World.State).Single(c => c.Text == "Trust me.");
        Assert.Equal(RollMode.Advantage, trust.Mode);
    }

    [Fact]
    public void RetryableCheck_StaysAvailableAfterFailure()
    {
        var (game, session, _) = Start(1);

        Choose(game, session, "Read him.");

        Assert.Contains(game.Dialogue.GetChoices(session, game.World.State), c => c.Text == "Read him.");
        Assert.Equal("hub", session.CurrentNode!.Id);
    }

    [Fact]
    public void ChoiceThatStartsCombat_EndsConversationWithTransition()
    {
        var (game, session, _) = Start();

        var log = Choose(game, session, "Fight!");

        Assert.True(session.IsOver);
        Assert.Equal(new StartCombatTransition("rats"), Assert.Single(ExplorationHarness.Transitions(log)));
    }

    [Fact]
    public void EndChoice_EndsConversation()
    {
        var (game, session, _) = Start();

        Assert.Empty(Choose(game, session, "Goodbye.").Events);
        Assert.True(session.IsOver);
        Assert.Empty(game.Dialogue.GetChoices(session, game.World.State));
    }

    [Fact]
    public void NodeEffectsThatStartCombat_EndConversation()
    {
        var content = new TestContent()
            .With("Rooms/rooms.json", ExplorationFixture.Rooms)
            .With("Dialogues/sage.json", """
                [ { "id": "sage", "speaker": "S", "entries": [ { "node": "a" } ],
                    "nodes": [ { "id": "a", "text": "Die!", "effects": [ { "type": "startCombat", "encounter": "rats" } ], "next": "b" },
                               { "id": "b", "text": "Unreached.", "choices": [ { "text": "x", "end": true } ] } ] } ]
                """)
            .Load();
        var game = new ExplorationHarness(content);
        var log = new TurnLog();

        var session = game.Dialogue.Start("sage", game.World.State, log);

        Assert.True(session.IsOver);
        Assert.Equal(new[] { "Die!" }, Lines(log));
    }

    [Fact]
    public void Choose_UnavailableChoice_Throws()
    {
        var (game, session, _) = Start();

        Assert.Throws<ArgumentOutOfRangeException>(() => game.Dialogue.Choose(session, 4, game.World.State, new TurnLog()));
    }

    [Fact]
    public void Choose_WhenOver_Throws()
    {
        var (game, session, _) = Start();
        Choose(game, session, "Goodbye.");

        Assert.Throws<InvalidOperationException>(() => game.Dialogue.Choose(session, 0, game.World.State, new TurnLog()));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var (game, session, _) = Start();
        var w = game.World;

        Assert.Throws<ArgumentNullException>(() => new DialogueRunner(null!, w.Checks, w.Effects, w.Text));
        Assert.Throws<ArgumentNullException>(() => new DialogueRunner(w.Content, null!, w.Effects, w.Text));
        Assert.Throws<ArgumentNullException>(() => new DialogueRunner(w.Content, w.Checks, null!, w.Text));
        Assert.Throws<ArgumentNullException>(() => new DialogueRunner(w.Content, w.Checks, w.Effects, null!));
        Assert.Throws<ArgumentNullException>(() => game.Dialogue.Start("sage", null!, new TurnLog()));
        Assert.Throws<ArgumentNullException>(() => game.Dialogue.Start("sage", w.State, null!));
        Assert.Throws<ArgumentNullException>(() => game.Dialogue.GetChoices(null!, w.State));
        Assert.Throws<ArgumentNullException>(() => game.Dialogue.GetChoices(session, null!));
        Assert.Throws<ArgumentNullException>(() => game.Dialogue.Choose(null!, 0, w.State, new TurnLog()));
        Assert.Throws<ArgumentNullException>(() => game.Dialogue.Choose(session, 0, null!, new TurnLog()));
        Assert.Throws<ArgumentNullException>(() => game.Dialogue.Choose(session, 0, w.State, null!));
    }
}
