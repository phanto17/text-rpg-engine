using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Presentation;

namespace RpgEngine.Tests.Pixel;

public sealed class PresentationLoaderTests
{
    [Fact]
    public void Load_WithoutTheFile_ReturnsAnEmptyPresentation()
    {
        var directory = Path.Combine(Path.GetTempPath(), "rpg-presentation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var result = PresentationLoader.Load(directory);

            Assert.False(result.FileFound);
            Assert.Empty(result.Warnings);
            Assert.Empty(result.Presentation.Rooms);
            Assert.Empty(result.Presentation.Monsters);
            Assert.Empty(result.Presentation.Npcs);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Load_ReadsTheFileUnderWorld()
    {
        var directory = Path.Combine(Path.GetTempPath(), "rpg-presentation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "World"));
        File.WriteAllText(Path.Combine(directory, "World", "presentation.json"), """{ "rooms": { "lichgate": { "weather": "embers" } } }""");
        try
        {
            var result = PresentationLoader.Load(directory);

            Assert.True(result.FileFound);
            Assert.Equal(WeatherKind.Embers, result.Presentation.Rooms["lichgate"].Weather);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Parse_ReadsEveryField_WithSnakeCaseNames()
    {
        const string json = """
            {
              // Comments and trailing commas are fine, as in every content file.
              "rooms": {
                "lichgate": {
                  "palette": { "backgroundTop": "#101820", "backgroundBottom": "#304050", "architecture": "#222", "accent": "#7fa6b8ff" },
                  "sceneStyle": "moor_gate",
                  "weather": "rain",
                  "backdropImage": "Assets/lichgate.png",
                  "moodCaption": "Rain on the moor.",
                },
              },
              "monsters": {
                "gravebound_knight": { "primaryColor": "#6a6f78", "accentColor": "#c04040", "emblem": "knight_helm", "title": "Warden of the Barrow Heart", "spriteImage": "Assets/knight.png" }
              },
              "npcs": {
                "wren": { "speakerName": "Sister Wren", "emblem": "lantern_nun", "title": "Sister of the Lantern Bearers", "portraitImage": "Assets/wren.png" }
              }
            }
            """;

        var result = PresentationLoader.Parse(json);

        Assert.Empty(result.Warnings);
        var room = result.Presentation.Rooms["lichgate"];
        Assert.Equal((SceneStyle.MoorGate, WeatherKind.Rain, "Assets/lichgate.png", "Rain on the moor."), (room.SceneStyle, room.Weather, room.BackdropImage, room.MoodCaption));
        Assert.Equal(("#101820", "#304050", "#222", "#7fa6b8ff"), (room.Palette!.BackgroundTop, room.Palette.BackgroundBottom, room.Palette.Architecture, room.Palette.Accent));
        var knight = result.Presentation.Monsters["gravebound_knight"];
        Assert.Equal((Emblem.KnightHelm, "#6a6f78", "#c04040", "Warden of the Barrow Heart", "Assets/knight.png"), (knight.Emblem, knight.PrimaryColor, knight.AccentColor, knight.Title, knight.SpriteImage));
        var wren = result.Presentation.Npcs["wren"];
        Assert.Equal(("Sister Wren", Emblem.LanternNun, "Sister of the Lantern Bearers", "Assets/wren.png"), (wren.SpeakerName, wren.Emblem, wren.Title, wren.PortraitImage));
        Assert.Null(wren.PrimaryColor);
    }

    [Fact]
    public void Parse_AcceptsArraysOfEntriesWithIds_AndMemberNames()
    {
        const string json = """
            {
              "rooms": [ { "id": "ossuary_hall", "sceneStyle": "OssuaryVault", "weather": "ashMotes" } ],
              "npcs": [ { "id": "orin", "emblem": "Monk" }, { "emblem": "monk" } ]
            }
            """;

        var result = PresentationLoader.Parse(json);

        Assert.Equal(SceneStyle.OssuaryVault, result.Presentation.Rooms["ossuary_hall"].SceneStyle);
        Assert.Equal(WeatherKind.AshMotes, result.Presentation.Rooms["ossuary_hall"].Weather);
        Assert.Equal(Emblem.Monk, result.Presentation.Npcs["orin"].Emblem);
        Assert.Single(result.Presentation.Npcs);
        Assert.Contains(result.Warnings, w => w.Contains("has no id", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_DropsWhatItCannotUse_AndKeepsTheRest()
    {
        const string json = """
            {
              "rooms": {
                "lichgate": { "sceneStyle": "haunted_moor", "weather": "rain", "palette": { "accent": "sky blue", "architecture": "#333333" }, "mood": "grim" },
                "ossuary_hall": "not an object",
                "inner_crypt": { "palette": 12 }
              },
              "monsters": 7,
              "npcs": { "wren": { "emblem": "ghost", "primaryColor": "#404858" } }
            }
            """;

        var result = PresentationLoader.Parse(json);

        var lichgate = result.Presentation.Rooms["lichgate"];
        Assert.Null(lichgate.SceneStyle);
        Assert.Equal(WeatherKind.Rain, lichgate.Weather);
        Assert.Null(lichgate.Palette!.Accent);
        Assert.Equal("#333333", lichgate.Palette.Architecture);
        Assert.False(result.Presentation.Rooms.ContainsKey("ossuary_hall"));
        Assert.Null(result.Presentation.Rooms["inner_crypt"].Palette);
        Assert.Empty(result.Presentation.Monsters);
        Assert.Null(result.Presentation.Npcs["wren"].Emblem);
        Assert.Equal("#404858", result.Presentation.Npcs["wren"].PrimaryColor);

        Assert.Contains(result.Warnings, w => w.Contains("rooms.lichgate.sceneStyle", StringComparison.Ordinal) && w.Contains("haunted_moor", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, w => w.Contains("rooms.lichgate.palette.accent", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, w => w.Contains("rooms.ossuary_hall", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, w => w.Contains("rooms.inner_crypt.palette", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, w => w.Contains("'monsters' must be an object", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, w => w.Contains("npcs.wren.emblem", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, w => w.Contains("'rooms.lichgate.mood' is not a presentation property", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_ReportsUnknownProperties_AtEveryLevel()
    {
        const string json = """
            {
              "items": {},
              "rooms": { "lichgate": { "palette": { "accent": "#fff", "glow": "#000" }, "sceneStlye": "moor_gate" } },
              "monsters": { "rat": { "emblem": "thrall_skull", "portrait": "x.png" } },
              "npcs": [ { "id": "wren", "speaker": "Wren" } ]
            }
            """;

        var result = PresentationLoader.Parse(json);

        string[] expected = ["items", "monsters.rat.portrait", "npcs.wren.speaker", "rooms.lichgate.palette.glow", "rooms.lichgate.sceneStlye"];
        Assert.Equal(expected, result.Warnings.Select(PropertyIn).Order(StringComparer.Ordinal));
        Assert.All(result.Warnings, w => Assert.EndsWith("is not a presentation property and was ignored.", w, StringComparison.Ordinal));

        // The rest of each entry still loads.
        Assert.Equal("#fff", result.Presentation.Rooms["lichgate"].Palette!.Accent);
        Assert.Null(result.Presentation.Rooms["lichgate"].SceneStyle);
        Assert.Equal(Emblem.ThrallSkull, result.Presentation.Monsters["rat"].Emblem);
        Assert.True(result.Presentation.Npcs.ContainsKey("wren"));
    }

    // The quoted path in a warning: "World/presentation.json: 'rooms.x.y' is not ..." -> "rooms.x.y".
    private static string PropertyIn(string warning)
    {
        var start = warning.IndexOf('\'', StringComparison.Ordinal) + 1;
        return warning[start..warning.IndexOf('\'', start)];
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("[]")]
    [InlineData("\"text\"")]
    public void Parse_UnreadableFile_YieldsAnEmptyPresentationAndOneWarning(string json)
    {
        var result = PresentationLoader.Parse(json);

        Assert.Empty(result.Presentation.Rooms);
        Assert.Single(result.Warnings);
        Assert.StartsWith(PresentationLoader.RelativePath, result.Warnings[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_EverySchemaSpelling_IsUnderstood()
    {
        // The loader must accept exactly the names the generated schema offers for autocomplete.
        foreach (var style in Enum.GetValues<SceneStyle>())
        {
            var result = PresentationLoader.Parse($$"""{ "rooms": { "r": { "sceneStyle": "{{JsonName(style)}}" } } }""");
            Assert.Equal(style, result.Presentation.Rooms["r"].SceneStyle);
        }

        foreach (var weather in Enum.GetValues<WeatherKind>())
        {
            var result = PresentationLoader.Parse($$"""{ "rooms": { "r": { "weather": "{{JsonName(weather)}}" } } }""");
            Assert.Equal(weather, result.Presentation.Rooms["r"].Weather);
        }

        foreach (var emblem in Enum.GetValues<Emblem>())
        {
            var result = PresentationLoader.Parse($$"""{ "monsters": { "m": { "emblem": "{{JsonName(emblem)}}" } } }""");
            Assert.Equal(emblem, result.Presentation.Monsters["m"].Emblem);
        }
    }

    private static string JsonName<T>(T value)
        where T : struct, Enum
    {
        var field = typeof(T).GetField(value.ToString())!;
        var attribute = (System.Text.Json.Serialization.JsonStringEnumMemberNameAttribute?)Attribute.GetCustomAttribute(field, typeof(System.Text.Json.Serialization.JsonStringEnumMemberNameAttribute));
        return attribute?.Name ?? value.ToString();
    }
}
