using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Events;
using RpgEngine.Core.World;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.World;

public sealed class TextFormatterAndTurnLogTests
{
    [Fact]
    public void Format_ReplacesTokens()
    {
        var formatter = new TextFormatter(new TestContent().Load());
        var hero = TestCharacters.Create(name: "Bram", classId: "fighter");

        Assert.Equal("Bram the Fighter waits.", formatter.Format("{player.name} the {player.class} waits.", hero));
        Assert.Equal("Once an Orphan.", formatter.Format("Once an {player.background}.", hero));
    }

    [Fact]
    public void Format_UnknownClassOrBackground_FallsBackToId()
    {
        var formatter = new TextFormatter(new TestContent().Load());
        var hero = TestCharacters.Create(classId: "bard", backgroundId: "noble");

        Assert.Equal("A bard.", formatter.Format("A {player.class}.", hero));
        Assert.Equal("A noble.", formatter.Format("A {player.background}.", hero));
    }

    [Fact]
    public void Format_NullArguments_Throw()
    {
        var formatter = new TextFormatter(new TestContent().Load());

        Assert.Throws<ArgumentNullException>(() => new TextFormatter(null!));
        Assert.Throws<ArgumentNullException>(() => formatter.Format(null!, TestCharacters.Create()));
        Assert.Throws<ArgumentNullException>(() => formatter.Format("x", null!));
    }

    [Fact]
    public void TurnLog_CollectsEventsAndTransitions()
    {
        var log = new TurnLog();

        log.Add(new NarrationEvent(TextStyle.System, "Hello"));
        log.Request(new MoveToTransition("hall"));

        Assert.Single(log.Events);
        Assert.True(log.HasPendingTransition);

        log.ClearTransitions();
        Assert.False(log.HasPendingTransition);
        Assert.False(log.TryTakeTransition(out _));
        Assert.Throws<ArgumentNullException>(() => log.Add(null!));
        Assert.Throws<ArgumentNullException>(() => log.Request(null!));
    }
}
