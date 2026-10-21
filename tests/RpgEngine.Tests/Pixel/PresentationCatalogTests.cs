using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Presentation;
using RpgEngine.Tests.Content;

namespace RpgEngine.Tests.Pixel;

/// <summary>Every shipped room, monster, and NPC must have a look even with no presentation file at all.</summary>
public sealed class PresentationCatalogTests
{
    private static readonly Lazy<GameContent> Content = new(() => ContentLoader.LoadFromDirectory(RealDataTests.DataDirectory));

    [Fact]
    public void EveryShippedRoom_HasADistinctDefaultLook()
    {
        var catalog = new PresentationCatalog(Content.Value);

        var looks = Content.Value.RoomList.Select(room => (room.Id, Look: catalog.Room(room.Id))).ToList();

        Assert.All(looks, x => Assert.NotNull(x.Look));
        Assert.Equal(looks.Count, looks.Select(x => x.Look.Style).Distinct().Count());
        Assert.Equal((SceneStyle.MoorGate, WeatherKind.Rain), (catalog.Room("lichgate").Style, catalog.Room("lichgate").Weather));
        Assert.Equal((SceneStyle.CradleOfAsh, WeatherKind.Embers), (catalog.Room("cradle_of_ash").Style, catalog.Room("cradle_of_ash").Weather));
        Assert.Equal(SceneStyle.SunderedBellNave, catalog.Room("bell_nave").Style);
    }

    [Fact]
    public void EveryShippedMonsterAndNpc_HasADefaultLookWithATitle()
    {
        var catalog = new PresentationCatalog(Content.Value);

        foreach (var monster in Content.Value.MonsterList)
        {
            var look = catalog.Monster(monster.Id);
            Assert.NotEmpty(look.Title);
            Assert.NotEqual(look.Primary, look.Accent);
        }

        foreach (var npc in Content.Value.RoomList.SelectMany(room => room.Npcs))
        {
            Assert.NotEmpty(catalog.Npc(npc.Id).Title);
        }

        Assert.Equal(Emblem.KnightHelm, catalog.Monster("gravebound_knight").Emblem);
        Assert.Equal(Emblem.ArchitectAstrolabe, catalog.Monster("first_architect").Emblem);
        Assert.Equal(Emblem.LanternNun, catalog.Npc("wren_cradle").Emblem);
    }

    [Fact]
    public void UnknownIds_GetAGuessFromTheirWords_OrTheGenericLook()
    {
        var catalog = new PresentationCatalog(Content.Value);

        Assert.Equal(SceneStyle.SunderedBellNave, catalog.Room("ruined_bell_tower").Style);
        Assert.Equal(SceneStyle.DrownedBridge, catalog.Room("some_river_ford").Style);
        Assert.Equal(PresentationDefaults.GenericRoom, catalog.Room("xyzzy"));
        Assert.Equal(Emblem.KnightHelm, catalog.Monster("rust_knight").Emblem);
        Assert.Equal(PresentationDefaults.GenericMonster, catalog.Monster("giant_rat"));
        Assert.Equal(Emblem.Scribe, catalog.Npc("old_librarian").Emblem);
        Assert.Equal(PresentationDefaults.GenericNpc, catalog.Npc("nobody"));
    }

    [Fact]
    public void PresentationEntries_OverrideOnlyTheFieldsTheySet()
    {
        var presentation = PresentationLoader.Parse("""
            {
              "rooms": { "lichgate": { "weather": "ash_motes", "palette": { "accent": "#ff0000" } } },
              "monsters": { "barrow_thrall": { "title": "The Risen", "emblem": "mournling" } },
              "npcs": { "wren": { "portraitImage": "Assets/wren.png" } }
            }
            """).Presentation;
        var catalog = new PresentationCatalog(Content.Value, presentation);
        var defaults = PresentationDefaults.Rooms["lichgate"];

        var lichgate = catalog.Room("lichgate");
        Assert.Equal(SceneStyle.MoorGate, lichgate.Style);
        Assert.Equal(WeatherKind.AshMotes, lichgate.Weather);
        Assert.Equal(PixelColor.Parse("#ff0000"), lichgate.Accent);
        Assert.Equal(defaults.BackgroundTop, lichgate.BackgroundTop);
        Assert.Equal(defaults.Architecture, lichgate.Architecture);

        var thrall = catalog.Monster("barrow_thrall");
        Assert.Equal(("The Risen", Emblem.Mournling, PresentationDefaults.Monsters["barrow_thrall"].Primary), (thrall.Title, thrall.Emblem, thrall.Primary));

        var wren = catalog.Npc("wren");
        Assert.Equal(("Assets/wren.png", Emblem.LanternNun), (wren.Image, wren.Emblem));
        Assert.Same(wren, catalog.Npc("wren"));
    }

    [Fact]
    public void SpeakerOf_FindsTheNpcWhoOwnsTheDialogue()
    {
        var catalog = new PresentationCatalog(Content.Value);

        Assert.Equal("wren", catalog.SpeakerOf("wren", "lichgate"));
        Assert.Equal("wren_nocturne", catalog.SpeakerOf("wren_nocturne", "nocturne_square"));
        Assert.Equal("reconciled_architect", catalog.SpeakerOf("architect_post", "cradle_of_ash"));
        Assert.Equal("oakhaven", catalog.SpeakerOf("oakhaven", "lichgate"));
        Assert.Equal("nobody", catalog.SpeakerOf("nobody", "lichgate"));
    }
}
