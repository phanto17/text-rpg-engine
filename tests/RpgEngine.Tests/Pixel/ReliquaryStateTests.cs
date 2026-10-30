using RpgEngine.Pixel.Presentation;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Pixel;

public sealed class ReliquaryStateTests
{
    [Fact]
    public void Read_CustomBindings_TracksCisternAndFurnaceIndependently()
    {
        var session = SessionFixture.NewSession();
        var parsed = PresentationLoader.Parse("""{"bindings":{"cisternQuenchedFlag":"cooled","furnaceOpenFlag":"unlatched"}}""");
        Assert.Empty(parsed.Warnings);
        var bindings = parsed.Presentation.Bindings;
        Assert.Equal(new ReliquaryState(), ReliquaryState.Read(session, bindings));
        session.State.World.SetFlag("unrelated");
        Assert.Equal(new ReliquaryState(), ReliquaryState.Read(session, bindings));
        session.State.World.SetFlag("cooled");
        Assert.Equal(new ReliquaryState(true), ReliquaryState.Read(session, bindings));
        session.State.World.SetFlag("unlatched");
        Assert.Equal(new ReliquaryState(true, true), SceneState.Read(session, bindings).Reliquary);
    }

    [Fact]
    public void Read_MissingSessionOrBindings_ReturnsNeutralState()
    {
        Assert.Equal(new ReliquaryState(), ReliquaryState.Read(null, new()));
        Assert.Equal(new ReliquaryState(), SceneState.Read(SessionFixture.NewSession(), new()).Reliquary);
    }

    [Fact]
    public void Parse_InvalidReliquaryBindings_WarnsAndDiscardsFields()
    {
        var parsed = PresentationLoader.Parse("""{"bindings":{"cisternQuenchedFlag":42,"furnaceOpenFlag":[]}}""");
        Assert.Equal(2, parsed.Warnings.Count);
        Assert.Null(parsed.Presentation.Bindings.CisternQuenchedFlag);
        Assert.Null(parsed.Presentation.Bindings.FurnaceOpenFlag);
        Assert.Equal(new ReliquaryState(), ReliquaryState.Read(SessionFixture.NewSession(), parsed.Presentation.Bindings));
    }
}
