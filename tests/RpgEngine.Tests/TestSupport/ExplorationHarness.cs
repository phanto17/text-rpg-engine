using RpgEngine.Core.Commands;
using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dialogue;
using RpgEngine.Core.Events;
using RpgEngine.Core.Exploration;

namespace RpgEngine.Tests.TestSupport;

/// <summary>Exploration and dialogue services over <see cref="ExplorationFixture"/> with scripted dice.</summary>
internal sealed class ExplorationHarness
{
    public ExplorationHarness(GameContent? content = null, params int[] dice)
    {
        World = new TestWorld(content ?? ExplorationFixture.Load(), dice: dice);
        Exploration = new ExplorationService(World.Content, World.Dice, World.Checks, World.Effects, World.Text);
        Dialogue = new DialogueRunner(World.Content, World.Checks, World.Effects, World.Text);
    }

    public TestWorld World { get; }

    public ExplorationService Exploration { get; }

    public DialogueRunner Dialogue { get; }

    public TurnLog Run(string input) => Run(CommandParser.Parse(input));

    public TurnLog Run(ParsedCommand command)
    {
        var log = new TurnLog();
        Exploration.Execute(command, World.State, log);
        return log;
    }

    public TurnLog Enter(string roomId)
    {
        var log = new TurnLog();
        Exploration.EnterRoom(roomId, World.State, log);
        return log;
    }

    public static IReadOnlyList<string> Texts(TurnLog log) =>
        log.Events.OfType<NarrationEvent>().Select(e => e.Text).ToList();

    public static string SystemText(TurnLog log) =>
        Assert.Single(log.Events.OfType<NarrationEvent>(), e => e.Style == TextStyle.System).Text;

    public static List<GameTransition> Transitions(TurnLog log)
    {
        var transitions = new List<GameTransition>();
        while (log.TryTakeTransition(out var transition))
        {
            transitions.Add(transition);
        }

        return transitions;
    }
}
