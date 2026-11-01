using RpgEngine.Pixel.Presentation;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Pixel;

public sealed class SanctuaryStateTests
{
    [Fact]
    public void Read_CustomBindings_TracksReliefAlliesAndOfferingIndependently()
    {
        var session = SessionFixture.NewSession();
        var parsed = PresentationLoader.Parse("""{"bindings":{"friezeReadFlag":"read","sanctuaryChimeFlag":"chime","sanctuaryDispatchFlag":"dispatch","sanctuaryOfferingFlag":"offering"}}""");
        Assert.Empty(parsed.Warnings);
        var bindings = parsed.Presentation.Bindings;
        session.State.World.SetFlag("unrelated");
        Assert.Equal(new SanctuaryState(), SanctuaryState.Read(session, bindings));
        session.State.World.SetFlag("read");
        Assert.Equal(new SanctuaryState(true), SanctuaryState.Read(session, bindings));
        session.State.World.SetFlag("chime");
        Assert.Equal(new SanctuaryState(true, true), SanctuaryState.Read(session, bindings));
        session.State.World.SetFlag("dispatch");
        Assert.Equal(new SanctuaryState(true, true, true), SanctuaryState.Read(session, bindings));
        session.State.World.SetFlag("offering");
        Assert.Equal(new SanctuaryState(true, true, true, true), SceneState.Read(session, bindings).Sanctuary);
    }

    [Fact]
    public void Read_MissingSessionOrBindings_ReturnsNeutralState()
    {
        Assert.Equal(new SanctuaryState(), SanctuaryState.Read(null, new()));
        Assert.Equal(new SanctuaryState(), SceneState.Read(SessionFixture.NewSession(), new()).Sanctuary);
    }

    [Fact]
    public void Parse_InvalidSanctuaryBindings_WarnsAndDiscardsFields()
    {
        var parsed = PresentationLoader.Parse("""{"bindings":{"friezeReadFlag":42,"sanctuaryChimeFlag":[],"sanctuaryDispatchFlag":false,"sanctuaryOfferingFlag":{}}}""");
        Assert.Equal(4, parsed.Warnings.Count);
        Assert.Null(parsed.Presentation.Bindings.FriezeReadFlag);
        Assert.Null(parsed.Presentation.Bindings.SanctuaryChimeFlag);
        Assert.Null(parsed.Presentation.Bindings.SanctuaryDispatchFlag);
        Assert.Null(parsed.Presentation.Bindings.SanctuaryOfferingFlag);
        Assert.Equal(new SanctuaryState(), SanctuaryState.Read(SessionFixture.NewSession(), parsed.Presentation.Bindings));
    }
}
