using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Presentation;
using RpgEngine.Pixel.Sequencing;
using RpgEngine.Core.Events;

namespace RpgEngine.Tests.Pixel;

public sealed class AudioDirectorTests
{
    private static AudioDirector NewDirector() => new(new AudioPresentationDef
    {
        Effects = new Dictionary<string, string>
        {
            ["select"] = "select.wav", ["step"] = "step.wav", ["impact"] = "impact.wav",
            ["damage"] = "damage.wav", ["lantern"] = "lantern.wav",
        },
        Ambience = new Dictionary<string, string> { ["hall"] = "hall.wav" },
    }, "custom_lantern");

    [Fact]
    public void Resolve_RoomEntry_DoesNotReplayStepsForLook()
    {
        var director = NewDirector();
        Assert.Null(director.Resolve(new RoomCue("gate")));
        Assert.Equal("step.wav", director.Resolve(new RoomCue("hall")));
        director.Advance(1);
        Assert.Null(director.Resolve(new RoomCue("hall")));
        Assert.Equal("hall.wav", director.Ambience("hall"));
        Assert.Null(director.Ambience("unknown"));
    }

    [Fact]
    public void Resolve_ImpactAndItemCues_UseConfiguredRolesAndItemBinding()
    {
        var director = NewDirector();
        Assert.Equal("impact.wav", director.Resolve(new HitFlashCue("Enemy", false)));
        director.Advance(1);
        Assert.Equal("damage.wav", director.Resolve(new HitFlashCue("Player", true)));
        director.Advance(1);
        Assert.Null(director.Resolve(new ItemCue("another_item")));
        Assert.Equal("lantern.wav", director.Resolve(new ItemCue("custom_lantern")));
        Assert.Null(director.Resolve(new LungeCue("Player", true, "Enemy")));
    }

    [Fact]
    public void Effect_BurstMuteAndZeroVolume_StayQuietWithoutQueuingPlayback()
    {
        var director = NewDirector();
        Assert.Equal("select.wav", director.Effect("select"));
        Assert.Null(director.Effect("damage"));
        director.Advance(0.05);
        Assert.Null(director.Effect("select"));
        Assert.Equal("damage.wav", director.Effect("damage"));
        director.Advance(1);
        director.Preferences = new() { Muted = true };
        Assert.Null(director.Effect("select"));
        director.Preferences = new() { EffectsVolume = 0 };
        Assert.Null(director.Effect("select"));
        director.Preferences = new();
        Assert.Equal("select.wav", director.Effect("select"));
    }

    [Fact]
    public void Parse_AudioMaps_ReadsPathsAndSkipsInvalidEntries()
    {
        var loaded = PresentationLoader.Parse("""{"audio":{"effects":{"impact":"Assets/Audio/hit.wav","select":"../outside.wav","typo":"Assets/Audio/x.wav"},"ambience":{"hall":"Assets/Audio/hall.wav"}}}""");
        Assert.Equal(2, loaded.Warnings.Count);
        Assert.Equal("Assets/Audio/hit.wav", loaded.Presentation.Audio.Effects["impact"]);
        Assert.Equal("Assets/Audio/hall.wav", loaded.Presentation.Audio.Ambience["hall"]);
        Assert.Single(loaded.Presentation.Audio.Effects);
        Assert.Single(PresentationLoader.Parse("""{"audio":4}""").Warnings);
        Assert.Single(PresentationLoader.Parse("""{"audio":{"effects":4}}""").Warnings);
        Assert.Single(PresentationLoader.Parse("""{"audio":{"typo":{}}}""").Warnings);
        Assert.Empty(PresentationLoader.Parse("{}").Warnings);
    }

    [Fact]
    public void ShippedAudio_ReferencesRealRoomsAndValidPcmClips()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Data");
        var presentation = PresentationLoader.Load(directory).Presentation;
        Assert.NotEmpty(presentation.Audio.Effects);
        Assert.NotEmpty(presentation.Audio.Ambience);
        Assert.All(presentation.Audio.Ambience.Keys, key => Assert.True(presentation.Rooms.ContainsKey(key)));
        Assert.All(presentation.Audio.Effects.Values.Concat(presentation.Audio.Ambience.Values),
            path => Assert.NotNull(PcmWave.Resolve(directory, path)));
    }

    [Fact]
    public void Narrate_AcquiredItem_QueuesItsStableIdForTheAudioBinding()
    {
        var item = new ItemDef { Id = "custom_lantern", Name = "A light", Type = ItemType.Quest, Description = "Iron." };
        var beat = Assert.Single(new EventNarrator().Narrate(new ItemGainedEvent(item, 1)));
        Assert.Contains(new ItemCue("custom_lantern"), beat.Cues);
    }
}
