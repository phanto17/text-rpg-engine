using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Presentation;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Pixel;

public sealed class FinaleStateTests
{
    [Theory]
    [InlineData("sun", FinaleEnding.Sun)]
    [InlineData("moon", FinaleEnding.Moon)]
    [InlineData("triad", FinaleEnding.Triad)]
    public void Read_CustomEndingBinding_ResolvesIndependently(string flag, FinaleEnding expected)
    {
        var parsed = PresentationLoader.Parse("""{"bindings":{"sunEndingFlag":"sun","moonEndingFlag":"moon","triadEndingFlag":"triad"}}""");
        Assert.Empty(parsed.Warnings);
        var session = SessionFixture.NewSession();
        session.State.World.SetFlag(flag);
        Assert.Equal(expected, SceneState.Read(session, parsed.Presentation.Bindings).Finale.Ending);
    }

    [Fact]
    public void Read_ConflictingFlags_PrefersTriadThenMoon()
    {
        var session = SessionFixture.NewSession();
        var bindings = new SceneBindingsDef { SunEndingFlag = "s", MoonEndingFlag = "m", TriadEndingFlag = "t" };
        session.State.World.SetFlag("s");
        session.State.World.SetFlag("m");
        Assert.Equal(FinaleEnding.Moon, FinaleState.Read(session, bindings).Ending);
        session.State.World.SetFlag("t");
        Assert.Equal(FinaleEnding.Triad, FinaleState.Read(session, bindings).Ending);
    }

    [Fact]
    public void Read_MissingSessionOrBindings_ReturnsNeutralState()
    {
        Assert.Equal(new FinaleState(), FinaleState.Read(null, new() { SunEndingFlag = "s" }));
        Assert.Equal(new FinaleState(), SceneState.Read(SessionFixture.NewSession(), new()).Finale);
    }

    [Fact]
    public void Parse_InvalidEndingBindings_WarnsAndDiscardsFields()
    {
        var parsed = PresentationLoader.Parse("""{"bindings":{"sunEndingFlag":42,"moonEndingFlag":[],"triadEndingFlag":false}}""");
        Assert.Equal(3, parsed.Warnings.Count);
        Assert.Null(parsed.Presentation.Bindings.SunEndingFlag);
        Assert.Null(parsed.Presentation.Bindings.MoonEndingFlag);
        Assert.Null(parsed.Presentation.Bindings.TriadEndingFlag);
    }
}
