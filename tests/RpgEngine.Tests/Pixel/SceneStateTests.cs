using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Presentation;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Pixel;

public sealed class SceneStateTests
{
    [Fact]
    public void Read_CustomBindings_UsesExactReferences()
    {
        var session = SessionFixture.NewSession();
        session.State.Player.Inventory.Add("custom_lantern");
        session.State.World.SetCounter("warmth", 8);
        session.State.World.SetCounter("steps", 2);
        session.State.World.SetFlag("repaired");
        session.State.World.SetFlag("ending_unrelated");
        var bindings = new SceneBindingsDef
        {
            ResonanceCounter = "warmth", SigilCounter = "steps", LanternItem = "custom_lantern",
            SunSealItem = "missing", BellMendedFlags = ["repaired"], BrazierLitFlags = ["finished"],
        };
        var state = SceneState.Read(session, bindings);
        Assert.Equal(8, state.Resonance);
        Assert.Equal(2, state.SigilsLit);
        Assert.True(state.HasLantern);
        Assert.False(state.SunSealSet);
        Assert.True(state.BellMended);
        Assert.False(state.BrazierLit);
        session.State.World.SetFlag("finished");
        Assert.True(SceneState.Read(session, bindings).BrazierLit);
    }

    [Fact]
    public void Read_MissingSessionOrBindings_ReturnsNeutralState()
    {
        var neutral = new SceneState(0, 0, false, false, false, false, false);
        Assert.Equal(neutral, SceneState.Read(null, new()));
        Assert.Equal(neutral, SceneState.Read(SessionFixture.NewSession(), new()));
    }

    [Fact]
    public void Parse_BadBindings_WarnsAndKeepsValidFields()
    {
        var loaded = PresentationLoader.Parse("""{"bindings":{"resonanceCounter":"warmth","sigilCounter":5,"bellMendedFlags":[null],"unknown":true}}""");
        Assert.Equal(3, loaded.Warnings.Count);
        Assert.Equal("warmth", loaded.Presentation.Bindings.ResonanceCounter);
        Assert.Null(loaded.Presentation.Bindings.SigilCounter);
        Assert.Empty(loaded.Presentation.Bindings.BellMendedFlags);
        Assert.Single(PresentationLoader.Parse("""{"bindings":42}""").Warnings);
        Assert.Empty(PresentationLoader.Parse("""{"bindings":{"bellMendedFlags":null}}""").Presentation.Bindings.BellMendedFlags);
    }
}
