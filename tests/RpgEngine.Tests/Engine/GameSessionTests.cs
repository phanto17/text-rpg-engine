using RpgEngine.Core.Combat;
using RpgEngine.Core.Commands;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Engine;
using RpgEngine.Core.Events;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Engine;

public sealed class GameSessionTests
{
    private static IReadOnlyList<GameEvent> Run(GameSession session, string input) => session.Execute(CommandParser.Parse(input));

    private static IReadOnlyList<GameEvent> Say(GameSession session, string choiceText) =>
        session.ChooseDialogue(session.DialogueChoices().Single(c => c.Text == choiceText).Index);

    [Fact]
    public void Begin_NewGame_ShowsIntroAndEntersStartRoom()
    {
        var session = SessionFixture.NewSession();

        var events = session.Begin();

        Assert.Equal("You wake in the dark.", Assert.IsType<NarrationEvent>(events[0]).Text);
        Assert.True(Assert.IsType<RoomDescribedEvent>(events[1]).Entered);
        Assert.Equal(GameMode.Exploration, session.Mode);
        Assert.True(session.CanSave);
        Assert.Contains(session.ExplorationActions(), a => a.Label == "Talk to Sage");
        Assert.Equal("Gate", session.DescribeRoom().Name);
    }

    [Fact]
    public void EnteringArmedRoom_StartsCombat_AndVictoryReturnsToExploration()
    {
        // initiative hero 20, rat 1 | attack 15, damage 4 (7 vs rat 7 HP) | loot roll 0
        var session = SessionFixture.NewSession(dice: [20, 1, 15, 4, 0]);
        session.Begin();

        var events = Run(session, "north");

        Assert.Equal(GameMode.Combat, session.Mode);
        Assert.False(session.CanSave);
        Assert.Contains(events, e => e is CombatStartedEvent);
        Assert.Throws<InvalidOperationException>(() => session.ExplorationActions());

        var after = session.CombatAct(new AttackAction(session.Combat!.ActiveEnemies[0].Id));

        Assert.Contains(new CombatEndedEvent(CombatOutcome.Victory), after);
        Assert.Equal(GameMode.Exploration, session.Mode);
        Assert.Null(session.Combat);
        Assert.True(session.State.World.HasFlag("rats_dead"));
    }

    [Fact]
    public void Fleeing_ReturnsToTheFleeRoom()
    {
        var session = SessionFixture.NewSession(dice: [20, 1, 15]);
        session.Begin();
        Run(session, "north");

        var events = session.CombatAct(new FleeAction());

        Assert.Equal(GameMode.Exploration, session.Mode);
        Assert.Equal("start", session.State.CurrentRoomId);
        Assert.Contains(events, e => e is RoomDescribedEvent { Entered: true });
    }

    [Fact]
    public void EnemyTurn_DefeatEndsTheGame()
    {
        // initiative hero 1, rat 20 | bite 15 hits, 1d4+2 = 3 kills a 1-HP hero
        var session = SessionFixture.NewSession(hp: 1, dice: [1, 20, 15, 1]);
        session.Begin();
        Run(session, "north");

        var events = session.CombatEnemyTurn();

        Assert.Equal(GameMode.GameOver, session.Mode);
        Assert.Equal("Rest now, Tester.", Assert.IsType<PlayerDefeatedEvent>(events[^1]).Text);
        Assert.Null(session.Combat);
    }

    [Fact]
    public void LethalTrap_EndsTheGameOutsideCombat()
    {
        var session = SessionFixture.NewSession();
        session.Begin();

        var events = Run(session, "touch needle");

        Assert.Equal(GameMode.GameOver, session.Mode);
        Assert.IsType<PlayerDefeatedEvent>(events[^1]);
    }

    [Fact]
    public void Talking_EntersDialogue_AndEndingReturnsToExploration()
    {
        var session = SessionFixture.NewSession();
        session.Begin();

        var events = Run(session, "talk to sage");

        Assert.Equal(GameMode.Dialogue, session.Mode);
        Assert.Equal("Hello, Tester.", Assert.IsType<DialogueLineEvent>(events[^1]).Text);
        Assert.False(session.CanSave);

        Say(session, "Bye.");

        Assert.Equal(GameMode.Exploration, session.Mode);
        Assert.Null(session.Dialogue);
    }

    [Fact]
    public void DialogueChoice_CanStartCombat()
    {
        var session = SessionFixture.NewSession(dice: [20, 1]);
        session.Begin();
        Run(session, "talk to sage");

        Say(session, "Fight me.");

        Assert.Equal(GameMode.Combat, session.Mode);
        Assert.Null(session.Dialogue);
        Assert.True(session.Combat!.IsPlayerTurn);
    }

    [Fact]
    public void DialogueWithNoChoices_EndsImmediately()
    {
        var session = SessionFixture.NewSession();
        session.Begin();

        var events = Run(session, "talk to monk");

        Assert.Equal("...", Assert.IsType<DialogueLineEvent>(events[^1]).Text);
        Assert.Equal(GameMode.Exploration, session.Mode);
    }

    [Fact]
    public void Journal_ShowsStartedQuestsAndReputation()
    {
        var session = SessionFixture.NewSession();
        session.Begin();
        Assert.Empty(session.Journal().Quests);

        Run(session, "talk sage");
        Say(session, "Bless me.");

        var journal = session.Journal();
        var quest = Assert.Single(journal.Quests);
        Assert.Equal(("The Main Quest", 10, "Begin.", false), (quest.Name, quest.Stage, quest.Journal, quest.Completed));
        Assert.Equal(new[] { ("The Order", 5) }, journal.Reputation.Select(r => (r.Faction, r.Value)));
    }

    [Fact]
    public void EndGame_PicksEpilogueFromWorldState()
    {
        var session = SessionFixture.NewSession();
        session.Begin();

        var events = Run(session, "pray");

        Assert.Equal(GameMode.Ended, session.Mode);
        Assert.Equal("The end, Tester.", session.Epilogue);
        Assert.Equal(new EpilogueEvent("The end, Tester."), events[^1]);
        Assert.True(session.Journal().Quests.Single().Completed);
    }

    [Fact]
    public void EndGame_WithReputation_PicksBlessedEpilogue()
    {
        var session = SessionFixture.NewSession();
        session.Begin();
        Run(session, "talk sage");
        Say(session, "Bless me.");

        Run(session, "pray at altar");

        Assert.Equal("Blessed ending, Tester.", session.Epilogue);
    }

    [Fact]
    public void Restore_BeginRedescribesWithoutIntro()
    {
        var content = SessionFixture.Load();
        var original = GameSession.NewGame(content, SessionFixture.Hero(content), new FixedRandomSource());
        original.Begin();

        var restored = GameSession.Restore(content, original.State, new FixedRandomSource());
        var events = restored.Begin();

        Assert.False(Assert.IsType<RoomDescribedEvent>(Assert.Single(events)).Entered);
    }

    [Fact]
    public void WrongMode_Throws()
    {
        var session = SessionFixture.NewSession();
        session.Begin();

        Assert.Throws<InvalidOperationException>(() => session.DialogueChoices());
        Assert.Throws<InvalidOperationException>(() => session.ChooseDialogue(0));
        Assert.Throws<InvalidOperationException>(() => session.CombatAct(new DefendAction()));
        Assert.Throws<InvalidOperationException>(() => session.CombatEnemyTurn());

        Run(session, "talk sage");
        Assert.Throws<InvalidOperationException>(() => Run(session, "look"));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var content = SessionFixture.Load();
        var hero = SessionFixture.Hero(content);
        var random = new FixedRandomSource();

        Assert.Throws<ArgumentNullException>(() => GameSession.NewGame(null!, hero, random));
        Assert.Throws<ArgumentNullException>(() => GameSession.NewGame(content, null!, random));
        Assert.Throws<ArgumentNullException>(() => GameSession.NewGame(content, hero, null!));
        var state = GameSession.NewGame(content, hero, random).State;
        Assert.Throws<ArgumentNullException>(() => GameSession.Restore(null!, state, random));
        Assert.Throws<ArgumentNullException>(() => GameSession.Restore(content, null!, random));
        Assert.Throws<ArgumentNullException>(() => GameSession.Restore(content, state, null!));
        Assert.Throws<ArgumentNullException>(() => GameSession.NewGame(content, hero, random).Execute(null!));
    }

    [Fact]
    public void Checks_AreExposedForUiBadges()
    {
        var session = SessionFixture.NewSession();

        Assert.Equal("Persuasion", session.Checks.Label(new Core.Content.Defs.CheckDef { Skill = "persuasion", Dc = 10 }));
        Assert.Equal(RollMode.Normal, session.Checks.ModeFor(new Core.Content.Defs.CheckDef { Skill = "persuasion", Dc = 10 }, session.State));
    }
}
