using RpgEngine.Core.Characters;
using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Content;

public sealed class ContentLoaderTests
{
    [Fact]
    public void Load_MinimalContent_BuildsLookups()
    {
        var content = new TestContent().Load();

        Assert.Equal("Test Adventure", content.Manifest.Title);
        Assert.Equal(12, content.Rules.Flee.Dc);
        Assert.Equal(new[] { "fighter", "wizard" }, content.ClassList.Select(c => c.Id));
        Assert.Equal(Ability.Cha, content.Skills["persuasion"].Ability);
        Assert.IsType<AutoHitResolutionDef>(content.Spells["missile"].Resolution);
        Assert.Equal(2, content.StatusEffects["defending"].Modifiers.AcBonus);
        Assert.Equal(AttackRollModifier.Disadvantage, content.StatusEffects["chilled"].Modifiers.AttackRolls);
        Assert.Equal(16, content.Items["mail"].Armor!.BaseAc);
        Assert.Equal(DamageType.Piercing, content.Monsters["rat"].Attacks[0].DamageType);
        Assert.Equal(Direction.North, content.Rooms["start"].Exits[0].Direction);
        Assert.Single(content.Factions);
        Assert.Single(content.Quests);
        Assert.Empty(content.Dialogues);
        Assert.Empty(content.Encounters);
        Assert.Equal(7, content.ItemList.Count);
        Assert.Single(content.MonsterList);
        Assert.Equal(2, content.RoomList.Count);
        Assert.Empty(content.DialogueList);
        Assert.Equal(8, content.SkillList.Count);
        Assert.Equal(3, content.SpellList.Count);
        Assert.Equal(2, content.StatusEffectList.Count);
        Assert.Single(content.FactionList);
        Assert.Single(content.QuestList);
    }

    [Fact]
    public void Load_MultipleFilesInFolder_AreMerged()
    {
        var content = new TestContent()
            .With("Items/extra.json", """[ { "id": "coin", "name": "Coin", "type": "misc", "description": "Gold." } ]""")
            .With("Items/nested/ignored.json", """[ { "id": "ignored", "name": "X", "type": "misc", "description": "X" } ]""")
            .With("Items/notes.txt", "not json")
            .Load();

        Assert.True(content.Items.ContainsKey("coin"));
        Assert.True(content.Items.ContainsKey("sword"));
        Assert.False(content.Items.ContainsKey("ignored"));
    }

    [Fact]
    public void Load_EncountersAreIndexedByRoom()
    {
        var content = new TestContent().With("Rooms/rooms.json", """
            [ { "id": "start", "name": "Start", "description": "Rats!",
                "encounter": { "id": "rats", "enemies": [ { "monster": "rat", "count": 2 } ], "fleeTo": "start" } } ]
            """).Load();

        Assert.Equal(2, content.Encounters["rats"].Enemies[0].Count);
        Assert.Equal("start", content.EncounterRooms["rats"]);
    }

    [Fact]
    public void Load_CommentsTrailingCommasAndOutOfOrderType_AreAccepted()
    {
        var content = new TestContent().With("Rooms/rooms.json", """
            [
              // A comment
              { "id": "start", "name": "Start", "description": "Bare.",
                "onEnter": [ { "flag": "entered", "type": "setFlag" }, ],
              },
            ]
            """).Load();

        Assert.IsType<SetFlagEffect>(Assert.Single(content.Rooms["start"].OnEnter));
    }

    [Fact]
    public void Load_MissingManifest_ReportsError()
    {
        var errors = new TestContent().Without("game.json").Errors();

        Assert.Contains(errors, e => e.StartsWith("game.json:", StringComparison.Ordinal) && e.Contains("missing"));
    }

    [Fact]
    public void Load_MissingRequiredRuleFiles_ReportsEach()
    {
        var errors = new TestContent().Without("Rules/classes.json").Without("Rules/skills.json").Errors();

        Assert.Contains(errors, e => e.StartsWith("Rules/classes.json", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.StartsWith("Rules/skills.json", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_MissingOptionalFiles_IsAllowed()
    {
        var content = new TestContent()
            .With("Rules/classes.json", """
                [ { "id": "fighter", "name": "Fighter", "description": "X", "hitDie": 10,
                    "abilityPriority": ["str", "con", "dex", "wis", "cha", "int"],
                    "skillChoices": { "count": 0, "from": [] } } ]
                """)
            .Without("Rules/spells.json")
            .Without("World/factions.json")
            .Without("World/quests.json")
            .Without("Items/items.json")
            .Without("Bestiary/monsters.json")
            .Load();

        Assert.Empty(content.Spells);
        Assert.Empty(content.Items);
    }

    [Fact]
    public void Load_UnknownProperty_ReportsFileAndProperty()
    {
        var errors = new TestContent().With("Items/items.json", """
            [ { "id": "coin", "name": "Coin", "type": "misc", "description": "Gold.", "vaule": 3 } ]
            """).Errors();

        var error = Assert.Single(errors);
        Assert.StartsWith("Items/items.json:", error);
        Assert.Contains("vaule", error);
    }

    [Fact]
    public void Load_MissingRequiredProperty_ReportsIt()
    {
        var errors = new TestContent().With("Items/items.json", """[ { "id": "coin", "type": "misc", "description": "Gold." } ]""").Errors();

        Assert.Contains("name", Assert.Single(errors));
    }

    [Fact]
    public void Load_UnknownEnumValue_ReportsIt()
    {
        var errors = new TestContent().With("Items/items.json", """[ { "id": "coin", "name": "Coin", "type": "trinket", "description": "Gold." } ]""").Errors();

        Assert.StartsWith("Items/items.json:", Assert.Single(errors));
    }

    [Fact]
    public void Load_UnknownConditionType_ReportsIt()
    {
        var errors = new TestContent().With("Rooms/rooms.json", """
            [ { "id": "start", "name": "Start", "description": "Bare.",
                "variants": [ { "conditions": [ { "type": "moonPhase", "phase": "full" } ], "text": "Moonlight." } ] } ]
            """).Errors();

        Assert.StartsWith("Rooms/rooms.json:", Assert.Single(errors));
    }

    [Fact]
    public void Load_MissingTypeDiscriminator_ReportsIt()
    {
        var errors = new TestContent().With("Rooms/rooms.json", """
            [ { "id": "start", "name": "Start", "description": "Bare.", "onEnter": [ { "flag": "x" } ] } ]
            """).Errors();

        Assert.StartsWith("Rooms/rooms.json:", Assert.Single(errors));
    }

    [Fact]
    public void Load_NullFile_ReportsIt()
    {
        var errors = new TestContent().With("Items/items.json", "null").Errors();

        Assert.Equal("Items/items.json: file contains null.", Assert.Single(errors));
    }

    [Fact]
    public void Load_ErrorsInSeveralFiles_ReportsAllOfThem()
    {
        var errors = new TestContent()
            .With("Items/items.json", "[ { ")
            .With("World/factions.json", """[ { "id": "order" } ]""")
            .Errors();

        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void Load_NullFiles_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ContentLoader.Load(null!));
    }

    [Fact]
    public void ContentValidationException_ListsEveryError()
    {
        var exception = new ContentValidationException(["first problem", "second problem"]);

        Assert.Contains("2 error(s)", exception.Message);
        Assert.Contains(" - first problem", exception.Message);
        Assert.Contains(" - second problem", exception.Message);
        Assert.Equal(2, exception.Errors.Count);
    }

    [Fact]
    public void InMemoryFiles_MissingFile_Throws()
    {
        var files = new InMemoryContentFiles(new Dictionary<string, string>());

        Assert.Throws<FileNotFoundException>(() => files.ReadAllText("game.json"));
        Assert.Throws<ArgumentNullException>(() => new InMemoryContentFiles(null!));
    }

    [Fact]
    public void DirectoryFiles_LoadsContentFromDisk()
    {
        var root = Path.Combine(Path.GetTempPath(), "rpg-content-" + Guid.NewGuid().ToString("N"));
        try
        {
            var source = new TestContent().Files();
            foreach (var path in new[] { "game.json", "Rules/classes.json", "Rules/skills.json", "Rules/spells.json", "Rules/status_effects.json", "World/factions.json", "World/quests.json", "Items/items.json", "Bestiary/monsters.json", "Rooms/rooms.json" })
            {
                var target = Path.Combine(root, path);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllText(target, source.ReadAllText(path));
            }

            var content = ContentLoader.LoadFromDirectory(root);
            var files = new DirectoryContentFiles(root);

            Assert.Equal("Test Adventure", content.Manifest.Title);
            Assert.Equal(new[] { "Rooms/rooms.json" }, files.ListJsonFiles("Rooms"));
            Assert.Empty(files.ListJsonFiles("Dialogues"));
            Assert.True(files.Exists("game.json"));
            Assert.False(files.Exists("nope.json"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DirectoryFiles_BlankRoot_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DirectoryContentFiles(" "));
    }
}
