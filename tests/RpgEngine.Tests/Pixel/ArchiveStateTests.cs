using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Presentation;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Pixel;

public sealed class ArchiveStateTests
{
    [Fact]
    public void Read_CustomBindings_SeparatesSlackStoppedAndReleased()
    {
        var session = SessionFixture.NewSession();
        var parsed = PresentationLoader.Parse("""{"bindings":{"loomSlackFlag":"slack","carillonStoppedFlag":"stopped","carillonReleasedFlag":"released"}}""");
        Assert.Empty(parsed.Warnings);
        var bindings = parsed.Presentation.Bindings;
        Assert.Equal(new ArchiveState(), ArchiveState.Read(session, bindings));
        session.State.World.SetFlag("unrelated");
        Assert.Equal(new ArchiveState(), ArchiveState.Read(session, bindings));
        session.State.World.SetFlag("slack");
        Assert.Equal(new ArchiveState(true), ArchiveState.Read(session, bindings));
        session.State.World.SetFlag("stopped");
        Assert.Equal(new ArchiveState(true, true), ArchiveState.Read(session, bindings));
        session.State.World.SetFlag("released");
        Assert.Equal(new ArchiveState(true, true, true), SceneState.Read(session, bindings).Archive);
    }

    [Fact]
    public void Read_ReleasedSpirit_StopsCarillonWithoutASeparateStopFlag()
    {
        var session = SessionFixture.NewSession();
        session.State.World.SetFlag("free");
        var bindings = new SceneBindingsDef { CarillonReleasedFlag = "free" };
        Assert.Equal(new ArchiveState(false, true, true), ArchiveState.Read(session, bindings));
        Assert.Equal(new ArchiveState(), ArchiveState.Read(null, bindings));
        Assert.Equal(new ArchiveState(), ArchiveState.Read(session, new()));
    }

    [Fact]
    public void Parse_InvalidArchiveBindings_WarnsAndKeepsNeutralState()
    {
        var parsed = PresentationLoader.Parse("""{"bindings":{"loomSlackFlag":42,"carillonStoppedFlag":[],"carillonReleasedFlag":false}}""");
        Assert.Equal(3, parsed.Warnings.Count);
        Assert.Null(parsed.Presentation.Bindings.LoomSlackFlag);
        Assert.Null(parsed.Presentation.Bindings.CarillonStoppedFlag);
        Assert.Null(parsed.Presentation.Bindings.CarillonReleasedFlag);
        Assert.Equal(new ArchiveState(), ArchiveState.Read(SessionFixture.NewSession(), parsed.Presentation.Bindings));
    }
}
