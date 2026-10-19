using RpgEngine.Core.Content;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Content;

public sealed class ContentValidatorTests
{
    private const string BaseRules = """
        "standardArray": [15, 14, 13, 12, 10, 8], "proficiencyBonusByLevel": [2],
        "flee": { "skill": "acrobatics", "dc": 12 }, "defendStatus": "defending"
        """;

    /// <summary>A content set that uses nearly every feature; it must produce no errors.</summary>
    private const string KitchenSinkRooms = """
        [
          { "id": "start", "name": "Start", "description": "A bare room.", "revisit": "Bare again.",
            "variants": [ { "conditions": [ { "type": "flag", "flag": "lit" } ], "text": "Torchlight." } ],
            "ambient": ["Drip."],
            "exits": [
              { "direction": "north", "target": "hall", "conditions": [ { "type": "flag", "flag": "door_open" } ], "blockedText": "Locked." },
              { "direction": "down", "target": "hall", "conditions": [ { "type": "counter", "counter": "levers", "min": 2 } ] } ],
            "features": [
              { "id": "door", "name": "iron door", "aliases": ["door"], "description": "Iron.",
                "interactions": [
                  { "verb": "open", "conditions": [ { "type": "hasItem", "item": "key" } ], "text": "Click.",
                    "effects": [ { "type": "setFlag", "flag": "door_open" }, { "type": "removeItem", "item": "key" } ] },
                  { "verb": "search", "once": true, "check": { "skill": "perception", "dc": 12, "advantageIf": [ { "type": "any", "conditions": [ { "type": "class", "class": "fighter" }, { "type": "background", "background": "orphan" } ] } ] },
                    "success": { "text": "A key!", "effects": [ { "type": "giveItem", "item": "key" } ] },
                    "failure": { "text": "Nothing." } },
                  { "verb": "pull", "target": "lever", "effects": [ { "type": "addCounter", "counter": "levers", "amount": 1 } ] } ] },
              { "id": "torch", "name": "torch", "description": "Unlit.", "conditions": [ { "type": "notFlag", "flag": "lit" } ],
                "interactions": [ { "verb": "light", "effects": [ { "type": "setFlag", "flag": "lit" },
                  { "type": "if", "conditions": [ { "type": "any", "conditions": [ { "type": "visited", "room": "hall" }, { "type": "abilityScore", "ability": "int", "min": 12 } ] } ],
                    "then": [ { "type": "message", "text": "You remember.", "style": "sensory" } ],
                    "else": [ { "type": "savingThrow", "ability": "dex", "dc": 10, "onFail": [ { "type": "damage", "dice": "1d4", "damageType": "fire" } ] } ] } ] } ] } ],
            "items": ["potion"],
            "npcs": [ { "id": "sage", "name": "Sage", "aliases": ["old man"], "description": "Old.", "dialogue": "sage",
                        "conditions": [ { "type": "not", "conditions": [ { "type": "flag", "flag": "sage_gone" } ] } ] } ],
            "onFirstEnter": [ { "type": "setQuestStage", "quest": "main", "stage": 10 } ],
            "onEnter": [ { "type": "setCounter", "counter": "visits", "value": 1 }, { "type": "rest" } ] },
          { "id": "hall", "name": "Hall", "description": "A hall.",
            "exits": [ { "direction": "south", "target": "start" } ],
            "encounter": { "id": "rats", "conditions": [ { "type": "notFlag", "flag": "rats_dead" } ], "intro": "Squeak!",
              "enemies": [ { "monster": "rat", "count": 2 } ], "fleeTo": "start",
              "parley": { "options": [ { "label": "Offer cheese", "conditions": [ { "type": "all", "conditions": [ { "type": "lacksItem", "item": "relic" } ] } ],
                                         "check": { "ability": "cha", "dc": 10 } } ],
                          "onSuccess": [ { "type": "changeReputation", "faction": "order", "amount": 1 } ],
                          "onFailure": [ { "type": "message", "text": "They hiss." } ] },
              "onVictory": [ { "type": "grantXp", "amount": 10 }, { "type": "giveGold", "amount": 3 } ],
              "onResolved": [ { "type": "setFlag", "flag": "rats_dead" }, { "type": "setQuestStage", "quest": "main", "stage": 100 } ] } }
        ]
        """;

    private const string KitchenSinkDialogue = """
        [
          { "id": "sage", "speaker": "The Sage",
            "entries": [ { "conditions": [ { "type": "reputation", "faction": "order", "min": 1 } ], "node": "friend" }, { "node": "hello" } ],
            "nodes": [
              { "id": "hello", "text": "Greetings, {player.name}.", "effects": [ { "type": "setFlag", "flag": "met_sage" } ], "next": "hub" },
              { "id": "hub", "text": "Well?",
                "choices": [
                  { "text": "Who are you?", "once": true, "next": "hub" },
                  { "text": "Leave.", "check": { "skill": "persuasion", "dc": 14, "disadvantageIf": [ { "type": "questStage", "quest": "main", "equals": 100 } ] },
                    "successNode": "friend", "failureNode": "gone", "retry": false },
                  { "text": "Bye.", "conditions": [ { "type": "flag", "flag": "met_sage" } ], "effects": [ { "type": "restoreMana", "amount": 1 } ], "end": true } ] },
              { "id": "friend", "text": "Friend!", "effects": [ { "type": "heal", "dice": "1d4" }, { "type": "startCombat", "encounter": "rats" }, { "type": "startShop", "shop": "stall" } ] },
              { "id": "gone", "text": "Begone.", "effects": [ { "type": "setFlag", "flag": "sage_gone" }, { "type": "clearFlag", "flag": "lit" },
                { "type": "skillCheck", "check": { "skill": "insight", "dc": 10 }, "onSuccess": [ { "type": "moveTo", "room": "hall" } ] },
                { "type": "startDialogue", "dialogue": "sage" }, { "type": "endGame" } ] }
            ] }
        ]
        """;

    private static IReadOnlyList<string> Errors(params (string Path, string Json)[] files)
    {
        var content = new TestContent();
        foreach (var (path, json) in files)
        {
            content.With(path, json);
        }

        return content.Errors();
    }

    private static IReadOnlyList<string> RoomErrors(string roomsJson) => Errors(("Rooms/rooms.json", roomsJson));

    private static void AssertError(IReadOnlyList<string> errors, string fragment) =>
        Assert.True(errors.Any(e => e.Contains(fragment, StringComparison.Ordinal)), $"Expected an error containing '{fragment}'. Got:\n{string.Join("\n", errors)}");

    [Fact]
    public void Validate_MinimalContent_HasNoErrors()
    {
        Assert.Empty(new TestContent().Errors());
    }

    [Fact]
    public void Validate_KitchenSinkContent_HasNoErrors()
    {
        var errors = Errors(("Rooms/rooms.json", KitchenSinkRooms), ("Dialogues/sage.json", KitchenSinkDialogue));

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_NullContent_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ContentValidator.Validate(null!));
    }

    [Theory]
    [InlineData("""[ { "id": "Start", "name": "S", "description": "D" } ]""", "snake_case")]
    [InlineData("""[ { "id": "start", "name": "S", "description": "D" }, { "id": "start", "name": "S", "description": "D" } ]""", "duplicate id")]
    [InlineData("""[ { "id": "start", "name": " ", "description": "D" } ]""", "name must not be blank")]
    public void Validate_RoomIds(string rooms, string fragment)
    {
        AssertError(RoomErrors(rooms), fragment);
    }

    [Theory]
    [InlineData("\"startRoom\": \"start\"", "\"startRoom\": \"nowhere\"", "unknown room 'nowhere'")]
    [InlineData("[15, 14, 13, 12, 10, 8]", "[15, 14]", "exactly 6 values")]
    [InlineData("[15, 14, 13, 12, 10, 8]", "[15, 14, 13, 12, 10, 40]", "standardArray value")]
    [InlineData("[2, 2, 2, 2, 3]", "[]", "must not be empty")]
    [InlineData("[2, 2, 2, 2, 3]", "[2, 20]", "proficiencyBonusByLevel value")]
    [InlineData("\"defendStatus\": \"defending\"", "\"defendStatus\": \"braced\"", "unknown status effect 'braced'")]
    [InlineData("\"flee\": { \"skill\": \"acrobatics\", \"dc\": 12 }", "\"flee\": { \"skill\": \"acrobatics\", \"ability\": \"dex\", \"dc\": 12 }", "exactly one of skill or ability")]
    [InlineData("\"startingGold\": 5", "\"startingGold\": -5", "startingGold must be at least 0")]
    [InlineData("\"title\": \"Test Adventure\"", "\"title\": \"\"", "title must not be blank")]
    [InlineData("[ { \"text\": \"The end, {player.name}.\" } ]", "[ { \"conditions\": [ { \"type\": \"flag\", \"flag\": \"x\" } ], \"text\": \"End\" } ]", "last epilogue")]
    [InlineData("[ { \"text\": \"The end, {player.name}.\" } ]", "[]", "at least one epilogue")]
    public void Validate_Manifest(string original, string replacement, string fragment)
    {
        var errors = Errors(("game.json", TestContent.Manifest.Replace(original, replacement, StringComparison.Ordinal)));

        AssertError(errors, fragment);
    }

    [Theory]
    [InlineData("\"hitDie\": 10", "\"hitDie\": 7", "hitDie must be one of")]
    [InlineData("[\"str\", \"con\", \"dex\", \"wis\", \"cha\", \"int\"]", "[\"str\", \"str\", \"dex\", \"wis\", \"cha\", \"int\"]", "abilityPriority")]
    [InlineData("\"savingThrows\": [\"str\", \"con\"]", "\"savingThrows\": [\"str\", \"str\"]", "savingThrows contains duplicates")]
    [InlineData("\"count\": 2, \"from\": [\"athletics\"", "\"count\": 9, \"from\": [\"athletics\"", "skillChoices.count")]
    [InlineData("[\"athletics\", \"perception\", \"persuasion\", \"intimidation\"]", "[\"athletics\", \"athletics\", \"juggling\"]", "unknown skill 'juggling'")]
    [InlineData("[\"athletics\", \"perception\", \"persuasion\", \"intimidation\"]", "[\"athletics\", \"athletics\", \"perception\"]", "skillChoices.from contains duplicates")]
    [InlineData("\"expertiseCount\": 1", "\"expertiseCount\": 5", "expertiseCount")]
    [InlineData("{ \"item\": \"potion\", \"count\": 2 }", "{ \"item\": \"potion\", \"equip\": true }", "cannot be equipped")]
    [InlineData("{ \"item\": \"potion\", \"count\": 2 }", "{ \"item\": \"potion\", \"count\": 0 }", "count must be between 1 and 99")]
    [InlineData("{ \"item\": \"potion\", \"count\": 2 }", "{ \"item\": \"elixir\" }", "unknown item 'elixir'")]
    [InlineData("\"spells\": [\"second_wind\"]", "\"spells\": [\"fireball\"]", "unknown spell 'fireball'")]
    [InlineData("\"mana\": { \"base\": 4", "\"mana\": { \"base\": -4", "mana.base")]
    [InlineData("\"description\": \"A trained warrior.\"", "\"description\": \"\"", "description must not be blank")]
    public void Validate_Classes(string original, string replacement, string fragment)
    {
        var errors = Errors(("Rules/classes.json", TestContent.Classes.Replace(original, replacement, StringComparison.Ordinal)));

        AssertError(errors, fragment);
    }

    [Fact]
    public void Validate_NoClasses_IsAnError()
    {
        AssertError(Errors(("Rules/classes.json", "[]")), "at least one class");
    }

    [Theory]
    [InlineData("\"cost\": { \"mana\": 2 }", "\"cost\": { \"mana\": 2, \"usesPerEncounter\": 1 }", "not both")]
    [InlineData("\"cost\": { \"usesPerEncounter\": 1 }", "\"cost\": { \"usesPerEncounter\": 0 }", "cost.usesPerEncounter")]
    [InlineData("\"cost\": { \"mana\": 2 }", "\"cost\": { \"mana\": -2 }", "cost.mana")]
    [InlineData("\"cost\": { \"mana\": 2 }, \"target\": \"enemy\"", "\"cost\": { \"mana\": 2 }, \"target\": \"self\"", "target must be 'enemy'")]
    [InlineData("\"target\": \"self\"", "\"target\": \"enemy\"", "target must be 'self'")]
    [InlineData("\"damage\": \"3d4+3\"", "\"damage\": \"3x4\"", "resolution.damage")]
    [InlineData("\"damage\": \"1d10\", \"damageType\": \"fire\"", "\"damage\": \"1d10\", \"damageType\": \"fire\", \"status\": \"burning\"", "unknown status effect 'burning'")]
    [InlineData("\"effects\": [ { \"type\": \"heal\", \"dice\": \"1d10+1\" } ]", "\"effects\": []", "resolution.effects must not be empty")]
    [InlineData("\"name\": \"Fire Bolt\"", "\"name\": \"\"", "name must not be blank")]
    public void Validate_Spells(string original, string replacement, string fragment)
    {
        var errors = Errors(("Rules/spells.json", TestContent.Spells.Replace(original, replacement, StringComparison.Ordinal)));

        AssertError(errors, fragment);
    }

    [Theory]
    [InlineData("""{ "type": "weaponAttack", "bonusDamage": "1q6" }""", "resolution.bonusDamage")]
    [InlineData("""{ "type": "save", "ability": "dex", "dc": 40, "damage": "2d6", "damageType": "fire" }""", "resolution.dc")]
    [InlineData("""{ "type": "save", "ability": "dex", "damage": "2d6", "damageType": "fire", "status": "nope" }""", "unknown status effect 'nope'")]
    public void Validate_SpellResolutions(string resolution, string fragment)
    {
        var spells = $$"""
            [ { "id": "trick", "name": "Trick", "kind": "technique", "description": "X", "target": "enemy", "resolution": {{resolution}} },
              { "id": "second_wind", "name": "SW", "kind": "technique", "description": "X", "target": "self",
                "resolution": { "type": "effects", "effects": [ { "type": "heal", "dice": "1" } ] } },
              { "id": "firebolt", "name": "FB", "kind": "spell", "description": "X", "target": "enemy", "resolution": { "type": "autoHit", "damage": "1", "damageType": "fire" } },
              { "id": "missile", "name": "MM", "kind": "spell", "description": "X", "target": "enemy", "resolution": { "type": "autoHit", "damage": "1", "damageType": "force" } } ]
            """;

        AssertError(Errors(("Rules/spells.json", spells)), fragment);
    }

    [Theory]
    [InlineData("\"durationRounds\": 1", "\"durationRounds\": 0", "durationRounds")]
    [InlineData("\"modifiers\": { \"acBonus\": 2 }", "\"modifiers\": { \"damagePerTurn\": \"d\" }", "modifiers.damagePerTurn")]
    [InlineData("\"name\": \"Defending\"", "\"name\": \"\"", "name must not be blank")]
    public void Validate_StatusEffects(string original, string replacement, string fragment)
    {
        var errors = Errors(("Rules/status_effects.json", TestContent.StatusEffects.Replace(original, replacement, StringComparison.Ordinal)));

        AssertError(errors, fragment);
    }

    [Theory]
    [InlineData("""[ { "id": "main", "name": "Q", "stages": [] } ]""", "stages must not be empty")]
    [InlineData("""[ { "id": "main", "name": "Q", "stages": [ { "stage": 10, "journal": "A" }, { "stage": 10, "journal": "B" } ] } ]""", "unique")]
    [InlineData("""[ { "id": "main", "name": "Q", "stages": [ { "stage": 0, "journal": "A" } ] } ]""", "stage must be at least 1")]
    [InlineData("""[ { "id": "main", "name": "Q", "stages": [ { "stage": 10, "journal": "" } ] } ]""", "journal must not be blank")]
    public void Validate_Quests(string quests, string fragment)
    {
        AssertError(Errors(("World/quests.json", quests)), fragment);
    }

    [Fact]
    public void Validate_FactionWithoutName_IsAnError()
    {
        AssertError(Errors(("World/factions.json", """[ { "id": "order", "name": "" } ]""")), "faction 'order': name must not be blank");
    }

    [Fact]
    public void Validate_SkillWithoutName_IsAnError()
    {
        var skills = TestContent.Skills.Replace("\"name\": \"Stealth\"", "\"name\": \"\"", StringComparison.Ordinal);

        AssertError(Errors(("Rules/skills.json", skills)), "skill 'stealth': name must not be blank");
    }

    [Theory]
    [InlineData("\"bonusSkill\": \"stealth\"", "\"bonusSkill\": \"juggling\"", "background 'orphan': bonusSkill references unknown skill 'juggling'")]
    [InlineData("\"keepsakeItem\": \"relic\"", "\"keepsakeItem\": \"gem\"", "background 'orphan': keepsakeItem references unknown item 'gem'")]
    [InlineData("\"bonusGold\": 10", "\"bonusGold\": -1", "background 'bailiff': bonusGold must be at least 0")]
    [InlineData("\"name\": \"Orphan\"", "\"name\": \" \"", "background 'orphan': name must not be blank")]
    [InlineData("\"description\": \"Raised by no one.\"", "\"description\": \"\"", "background 'orphan': description must not be blank")]
    [InlineData("\"id\": \"bailiff\"", "\"id\": \"orphan\"", "background 'orphan': duplicate id")]
    [InlineData("\"id\": \"bailiff\"", "\"id\": \"Bailiff\"", "background 'Bailiff': id must be snake_case")]
    public void Validate_Backgrounds(string original, string replacement, string fragment)
    {
        var errors = Errors(("Rules/backgrounds.json", TestContent.Backgrounds.Replace(original, replacement, StringComparison.Ordinal)));

        AssertError(errors, fragment);
    }

    [Theory]
    [InlineData("[1, 100]", "rules.xpThresholdsByLevel must start with 0")]
    [InlineData("[]", "rules.xpThresholdsByLevel must start with 0")]
    [InlineData("[0, 100, 100]", "rules.xpThresholdsByLevel must be strictly increasing")]
    public void Validate_XpThresholds(string thresholds, string fragment)
    {
        var manifest = TestContent.Manifest.Replace("\"naturalCritsOnChecks\": true", $"\"xpThresholdsByLevel\": {thresholds}, \"naturalCritsOnChecks\": true", StringComparison.Ordinal);

        AssertError(Errors(("game.json", manifest)), fragment);
    }

    [Theory]
    [InlineData("""[ { "level": 1, "spells": ["second_wind"] } ]""", "spellsByLevel level 1 can never be reached (levels 2 to 3 exist)")]
    [InlineData("""[ { "level": 4, "spells": ["second_wind"] } ]""", "spellsByLevel level 4 can never be reached")]
    [InlineData("""[ { "level": 2, "spells": [] } ]""", "spellsByLevel level 2 must list at least one spell")]
    [InlineData("""[ { "level": 2, "spells": ["fireball"] } ]""", "spellsByLevel level 2 references unknown spell 'fireball'")]
    [InlineData("""[ { "level": 2, "spells": ["missile"] } ]""", "spellsByLevel level 2: 'missile' is already known")]
    [InlineData("""[ { "level": 2, "spells": ["second_wind"] }, { "level": 3, "spells": ["second_wind"] } ]""", "spellsByLevel level 3: 'second_wind' is already known")]
    public void Validate_SpellsByLevel(string grants, string fragment)
    {
        var manifest = TestContent.Manifest.Replace("\"naturalCritsOnChecks\": true", "\"xpThresholdsByLevel\": [0, 100, 300], \"naturalCritsOnChecks\": true", StringComparison.Ordinal);
        var classes = TestContent.Classes.Replace("\"spells\": [\"firebolt\", \"missile\"]", $"\"spells\": [\"firebolt\", \"missile\"], \"spellsByLevel\": {grants}", StringComparison.Ordinal);

        AssertError(Errors(("game.json", manifest), ("Rules/classes.json", classes)), fragment);
    }

    [Theory]
    [InlineData("\"name\": \"The Stall\"", "\"name\": \"\"", "shop 'stall': name must not be blank")]
    [InlineData("\"keeper\": \"Mote\"", "\"keeper\": \" \"", "shop 'stall': keeper must not be blank")]
    [InlineData("\"greeting\": \"Coin first, {player.name}.\"", "\"greeting\": \"\"", "shop 'stall': greeting must not be blank")]
    [InlineData("\"stock\": [", "\"buybackRate\": 1.5, \"stock\": [", "shop 'stall': buybackRate must be between 0 and 1")]
    [InlineData("\"stock\": [", "\"buybackRate\": -0.1, \"stock\": [", "shop 'stall': buybackRate must be between 0 and 1")]
    [InlineData("\"item\": \"sword\"", "\"item\": \"laser\"", "shop 'stall': stock references unknown item 'laser'")]
    [InlineData("\"price\": 15", "\"price\": -1", "shop 'stall': stock 'sword' price must be at least 0")]
    [InlineData("\"stock\": 2", "\"stock\": 0", "shop 'stall': stock 'potion' stock must be between 1 and 999")]
    [InlineData("\"item\": \"sword\"", "\"item\": \"potion\"", "shop 'stall': stock lists 'potion' more than once")]
    [InlineData("\"id\": \"stall\"", "\"id\": \"Stall\"", "shop 'Stall': id must be snake_case")]
    public void Validate_Shops(string original, string replacement, string fragment)
    {
        var errors = Errors(("World/shops.json", TestContent.Shops.Replace(original, replacement, StringComparison.Ordinal)));

        AssertError(errors, fragment);
    }

    [Fact]
    public void Validate_NoBackgrounds_IsAnError()
    {
        AssertError(Errors(("Rules/backgrounds.json", "[]")), "Rules/backgrounds.json: at least one background is required.");
    }

    [Fact]
    public void Validate_BackgroundSkillThatStarvesAClassPicker_IsAnError()
    {
        // The wizard picks 2 of [religion, insight, perception]; a burden granting insight leaves only 2, which is fine,
        // but shrinking the list to 2 skills leaves 1 after the burden's skill is removed.
        var classes = TestContent.Classes.Replace("[\"religion\", \"insight\", \"perception\"]", "[\"religion\", \"insight\"]", StringComparison.Ordinal);
        var backgrounds = TestContent.Backgrounds.Replace("\"bonusSkill\": \"stealth\"", "\"bonusSkill\": \"insight\"", StringComparison.Ordinal);

        AssertError(Errors(("Rules/classes.json", classes), ("Rules/backgrounds.json", backgrounds)), "class 'wizard': background 'orphan' leaves fewer than skillChoices.count skills to pick from.");
    }

    [Theory]
    [InlineData("\"type\": \"weapon\", \"description\": \"Steel.\"", "\"type\": \"misc\", \"description\": \"Steel.\"", "'weapon' block")]
    [InlineData("\"type\": \"armor\"", "\"type\": \"misc\"", "'armor' block")]
    [InlineData("\"type\": \"key\", \"description\": \"Cold iron.\"", "\"type\": \"consumable\", \"description\": \"Cold iron.\"", "must have a 'use' block")]
    [InlineData("\"damage\": \"1d8\"", "\"damage\": \"1d0\"", "weapon.damage")]
    [InlineData("\"baseAc\": 16", "\"baseAc\": 0", "armor.baseAc")]
    [InlineData("\"maxDexBonus\": 0", "\"maxDexBonus\": -1", "armor.maxDexBonus")]
    [InlineData("\"effects\": [ { \"type\": \"heal\", \"dice\": \"2d4+2\" } ]", "\"effects\": []", "use.effects must not be empty")]
    [InlineData("\"aliases\": [\"blade\"]", "\"aliases\": [\" \"]", "aliases must not contain blank")]
    [InlineData("\"value\": 15", "\"value\": -15", "value must be at least 0")]
    [InlineData("\"name\": \"Dagger\"", "\"name\": \"\"", "name must not be blank")]
    public void Validate_Items(string original, string replacement, string fragment)
    {
        var errors = Errors(("Items/items.json", TestContent.Items.Replace(original, replacement, StringComparison.Ordinal)));

        AssertError(errors, fragment);
    }

    private static IReadOnlyList<string> ThrownItemErrors(string use) =>
        Errors(("Items/thrown.json", $$"""[ { "id": "bomb", "name": "Bomb", "type": "consumable", "description": "Boom.", "use": {{use}} } ]"""));

    [Fact]
    public void Validate_EnemyTargetedItem_HasNoErrors()
    {
        var errors = ThrownItemErrors("""{ "inCombat": true, "target": "enemy", "resolution": { "type": "autoHit", "damage": "2d4", "damageType": "radiant" } }""");

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("""{ "inCombat": true, "effects": [ { "type": "heal", "dice": "1" } ], "resolution": { "type": "autoHit", "damage": "1", "damageType": "fire" } }""", "use.resolution is only allowed when use.target is 'enemy'")]
    [InlineData("""{ "inCombat": false, "target": "enemy", "resolution": { "type": "autoHit", "damage": "1", "damageType": "fire" } }""", "must set use.inCombat to true")]
    [InlineData("""{ "inCombat": true, "target": "enemy", "effects": [ { "type": "heal", "dice": "1" } ], "resolution": { "type": "autoHit", "damage": "1", "damageType": "fire" } }""", "use.effects are not allowed when use.target is 'enemy'")]
    [InlineData("""{ "inCombat": true, "target": "enemy" }""", "use.resolution is required")]
    [InlineData("""{ "inCombat": true, "target": "enemy", "resolution": { "type": "weaponAttack" } }""", "use.resolution type must be")]
    [InlineData("""{ "inCombat": true, "target": "enemy", "resolution": { "type": "effects", "effects": [ { "type": "heal", "dice": "1" } ] } }""", "use.resolution type must be")]
    [InlineData("""{ "inCombat": true, "target": "enemy", "resolution": { "type": "autoHit", "damage": "1d0", "damageType": "fire" } }""", "use.resolution.damage")]
    [InlineData("""{ "inCombat": true, "target": "enemy", "resolution": { "type": "save", "ability": "dex", "dc": 0, "damage": "1", "damageType": "fire" } }""", "use.resolution.dc")]
    [InlineData("""{ "inCombat": true, "target": "enemy", "resolution": { "type": "spellAttack", "damage": "1", "damageType": "fire", "status": "dazed" } }""", "unknown status effect 'dazed'")]
    public void Validate_ItemUseTargets(string use, string fragment)
    {
        AssertError(ThrownItemErrors(use), fragment);
    }

    [Theory]
    [InlineData("\"hp\": \"2d6\"", "\"hp\": \"0\"", "hp must always be at least 1")]
    [InlineData("\"hp\": \"2d6\"", "\"hp\": \"lots\"", "hp:")]
    [InlineData("\"ac\": 12", "\"ac\": 0", "ac must be between")]
    [InlineData("\"str\": 7", "\"str\": 0", "abilities must be between")]
    [InlineData("\"xp\": 25", "\"xp\": -1", "xp must be at least 0")]
    [InlineData("\"chance\": 1.0", "\"chance\": 0", "chance must be greater than 0")]
    [InlineData("\"item\": \"key\", \"chance\"", "\"item\": \"gem\", \"chance\"", "unknown item 'gem'")]
    [InlineData("\"damage\": \"1d4+2\"", "\"damage\": \"1d4+2\", \"weight\": 0", "weight")]
    [InlineData("\"damage\": \"1d4+2\"", "\"damage\": \"1d4+2\", \"onHit\": { \"status\": \"rabid\" }", "unknown status effect 'rabid'")]
    [InlineData("\"damage\": \"1d4+2\"", "\"damage\": \"1d4+2\", \"onHit\": { \"status\": \"chilled\", \"save\": { \"ability\": \"con\", \"dc\": 0 } }", "onHit.save.dc")]
    [InlineData("\"attacks\": [ { \"name\": \"Bite\", \"toHit\": 4, \"damage\": \"1d4+2\", \"damageType\": \"piercing\" } ]", "\"attacks\": []", "attacks must not be empty")]
    [InlineData("\"description\": \"Big teeth.\"", "\"description\": \"Big teeth.\", \"ai\": { \"fleeBelowHpPercent\": 100 }", "fleeBelowHpPercent")]
    public void Validate_Monsters(string original, string replacement, string fragment)
    {
        var errors = Errors(("Bestiary/monsters.json", TestContent.Monsters.Replace(original, replacement, StringComparison.Ordinal)));

        AssertError(errors, fragment);
    }

    [Theory]
    [InlineData("""[ { "id": "start", "name": "S", "description": "D", "exits": [ { "direction": "north", "target": "void" } ] } ]""", "unknown room 'void'")]
    [InlineData("""[ { "id": "start", "name": "S", "description": "D", "exits": [ { "direction": "north", "target": "start" }, { "direction": "north", "target": "start" } ] } ]""", "duplicate direction")]
    [InlineData("""[ { "id": "start", "name": "S", "description": "D", "items": ["gem"] } ]""", "unknown item 'gem'")]
    [InlineData("""[ { "id": "start", "name": "S", "description": "D", "variants": [ { "text": "" } ] } ]""", "variants text must not be blank")]
    public void Validate_RoomBasics(string rooms, string fragment)
    {
        AssertError(RoomErrors(rooms), fragment);
    }

    [Theory]
    [InlineData("""{ "id": "box", "name": "box", "description": "Box." }, { "id": "box", "name": "box", "description": "Box." }""", "duplicate feature id")]
    [InlineData("""{ "id": "Box", "name": "box", "description": "Box." }""", "id must be snake_case")]
    [InlineData("""{ "id": "box", "name": "box", "description": "Box.", "interactions": [ { "verb": "pry open" } ] }""", "single lowercase word")]
    [InlineData("""{ "id": "box", "name": "box", "description": "Box.", "interactions": [ { "verb": "inventory" } ] }""", "built-in command")]
    [InlineData("""{ "id": "box", "name": "box", "description": "Box.", "interactions": [ { "verb": "open", "target": " " } ] }""", "target must not be blank")]
    [InlineData("""{ "id": "box", "name": "box", "description": "Box.", "interactions": [ { "verb": "open", "success": { "text": "Yes" } } ] }""", "outcomes require a check")]
    [InlineData("""{ "id": "box", "name": "box", "description": "Box.", "interactions": [ { "verb": "open", "check": { "skill": "lockpicking", "dc": 10 } } ] }""", "unknown skill 'lockpicking'")]
    [InlineData("""{ "id": "box", "name": "box", "description": "Box.", "interactions": [ { "verb": "open", "check": { "skill": "athletics", "dc": 0 } } ] }""", "check.dc")]
    [InlineData("""{ "id": "box", "name": "box", "description": "Box.", "interactions": [ { "verb": "open", "check": { "skill": "athletics", "dc": 10 }, "success": { "effects": [ { "type": "giveItem", "item": "gem" } ] } } ] }""", "unknown item 'gem'")]
    [InlineData("""{ "id": "box", "name": "box", "aliases": [""], "description": "Box." }""", "aliases must not contain blank")]
    public void Validate_Features(string feature, string fragment)
    {
        AssertError(RoomErrors($$"""[ { "id": "start", "name": "S", "description": "D", "features": [ {{feature}} ] } ]"""), fragment);
    }

    [Theory]
    [InlineData("""[ { "id": "sage", "name": "Sage", "description": "Old.", "dialogue": "nobody" } ]""", "unknown dialogue 'nobody'")]
    [InlineData("""[ { "id": "sage", "name": "Sage", "description": "Old.", "dialogue": "nobody" }, { "id": "sage", "name": "Sage", "description": "Old.", "dialogue": "nobody" } ]""", "duplicate npc id")]
    [InlineData("""[ { "id": "sage", "name": "", "description": "Old.", "dialogue": "nobody" } ]""", "name must not be blank")]
    public void Validate_Npcs(string npcs, string fragment)
    {
        AssertError(RoomErrors($$"""[ { "id": "start", "name": "S", "description": "D", "npcs": {{npcs}} } ]"""), fragment);
    }

    [Theory]
    [InlineData("""{ "id": "fight", "enemies": [ { "monster": "dragon" } ], "fleeTo": "start" }""", "unknown monster 'dragon'")]
    [InlineData("""{ "id": "fight", "enemies": [], "fleeTo": "start" }""", "enemies must not be empty")]
    [InlineData("""{ "id": "fight", "enemies": [ { "monster": "rat", "count": 0 } ], "fleeTo": "start" }""", "count must be between 1 and 10")]
    [InlineData("""{ "id": "fight", "enemies": [ { "monster": "rat" } ] }""", "fleeTo is required")]
    [InlineData("""{ "id": "fight", "enemies": [ { "monster": "rat" } ], "fleeTo": "void" }""", "unknown room 'void'")]
    [InlineData("""{ "id": "Fight", "enemies": [ { "monster": "rat" } ], "canFlee": false }""", "id must be snake_case")]
    [InlineData("""{ "id": "fight", "enemies": [ { "monster": "rat" } ], "canFlee": false, "parley": { "options": [] } }""", "parley.options must not be empty")]
    [InlineData("""{ "id": "fight", "enemies": [ { "monster": "rat" } ], "canFlee": false, "parley": { "options": [ { "label": "", "check": { "skill": "persuasion", "dc": 10 } } ] } }""", "label must not be blank")]
    [InlineData("""{ "id": "fight", "enemies": [ { "monster": "rat" } ], "canFlee": false, "onVictory": [ { "type": "grantXp", "amount": -1 } ] }""", "grantXp amount")]
    public void Validate_Encounters(string encounter, string fragment)
    {
        AssertError(RoomErrors($$"""[ { "id": "start", "name": "S", "description": "D", "encounter": {{encounter}} } ]"""), fragment);
    }

    [Fact]
    public void Validate_DuplicateEncounterIdsAcrossRooms_IsAnError()
    {
        var errors = RoomErrors("""
            [ { "id": "start", "name": "S", "description": "D", "encounter": { "id": "fight", "enemies": [ { "monster": "rat" } ], "canFlee": false } },
              { "id": "hall", "name": "H", "description": "D", "encounter": { "id": "fight", "enemies": [ { "monster": "rat" } ], "canFlee": false } } ]
            """);

        AssertError(errors, "duplicate encounter id in rooms 'start', 'hall'");
    }

    [Theory]
    [InlineData("""{ "id": "d", "speaker": "S", "entries": [ { "node": "missing" } ], "nodes": [ { "id": "a", "text": "Hi" } ] }""", "unknown node 'missing'")]
    [InlineData("""{ "id": "d", "speaker": "S", "entries": [ { "node": "a", "conditions": [ { "type": "visited", "room": "start" } ] } ], "nodes": [ { "id": "a", "text": "Hi" } ] }""", "last entry")]
    [InlineData("""{ "id": "d", "speaker": "S", "entries": [], "nodes": [ { "id": "a", "text": "Hi" } ] }""", "entries must not be empty")]
    [InlineData("""{ "id": "d", "speaker": "S", "entries": [ { "node": "a" } ], "nodes": [] }""", "nodes must not be empty")]
    [InlineData("""{ "id": "d", "speaker": "", "entries": [ { "node": "a" } ], "nodes": [ { "id": "a", "text": "Hi" } ] }""", "speaker must not be blank")]
    [InlineData("""{ "id": "d", "speaker": "S", "entries": [ { "node": "a" } ], "nodes": [ { "id": "a", "text": "Hi" }, { "id": "a", "text": "Again" } ] }""", "duplicate node id")]
    [InlineData("""{ "id": "d", "speaker": "S", "entries": [ { "node": "A" } ], "nodes": [ { "id": "A", "text": "Hi" } ] }""", "id must be snake_case")]
    [InlineData("""{ "id": "d", "speaker": "S", "entries": [ { "node": "a" } ], "nodes": [ { "id": "a", "text": "" } ] }""", "text must not be blank")]
    [InlineData("""{ "id": "d", "speaker": "S", "entries": [ { "node": "a" } ], "nodes": [ { "id": "a", "text": "Hi", "next": "b" } ] }""", "next references unknown node 'b'")]
    [InlineData("""{ "id": "d", "speaker": "S", "entries": [ { "node": "a" } ], "nodes": [ { "id": "a", "text": "Hi", "next": "a", "choices": [ { "text": "X", "end": true } ] } ] }""", "both choices and next")]
    [InlineData("""{ "id": "d", "speaker": "S", "entries": [ { "node": "a" } ], "nodes": [ { "id": "a", "text": "Hi", "choices": [ { "text": "X", "end": true, "next": "a" } ] } ] }""", "exactly one of next, end, or check")]
    [InlineData("""{ "id": "d", "speaker": "S", "entries": [ { "node": "a" } ], "nodes": [ { "id": "a", "text": "Hi", "choices": [ { "text": "X" } ] } ] }""", "exactly one of next, end, or check")]
    [InlineData("""{ "id": "d", "speaker": "S", "entries": [ { "node": "a" } ], "nodes": [ { "id": "a", "text": "Hi", "choices": [ { "text": "X", "check": { "skill": "persuasion", "dc": 10 }, "successNode": "a" } ] } ] }""", "failureNode references unknown node '(none)'")]
    [InlineData("""{ "id": "d", "speaker": "S", "entries": [ { "node": "a" } ], "nodes": [ { "id": "a", "text": "Hi", "choices": [ { "text": "X", "end": true, "successNode": "a" } ] } ] }""", "require a check")]
    [InlineData("""{ "id": "d", "speaker": "S", "entries": [ { "node": "a" } ], "nodes": [ { "id": "a", "text": "Hi", "choices": [ { "text": "", "end": true } ] } ] }""", "text must not be blank")]
    [InlineData("""{ "id": "d", "speaker": "S", "entries": [ { "node": "a" } ], "nodes": [ { "id": "a", "text": "Hi", "choices": [ { "text": "X", "next": "zzz" } ] } ] }""", "next references unknown node 'zzz'")]
    public void Validate_Dialogues(string dialogue, string fragment)
    {
        AssertError(Errors(("Dialogues/d.json", $"[ {dialogue} ]")), fragment);
    }

    [Theory]
    [InlineData("""{ "type": "flag", "flag": "never_set" }""", "flag 'never_set' is checked but never set")]
    [InlineData("""{ "type": "notFlag", "flag": "" }""", "flag must not be blank")]
    [InlineData("""{ "type": "counter", "counter": "never_changed", "min": 1 }""", "counter 'never_changed' is checked but never changed")]
    [InlineData("""{ "type": "counter", "counter": "", "min": 1 }""", "counter must not be blank")]
    [InlineData("""{ "type": "counter", "counter": "c" }""", "needs at least one of min, max, or equals")]
    [InlineData("""{ "type": "hasItem", "item": "gem" }""", "unknown item 'gem'")]
    [InlineData("""{ "type": "hasItem", "item": "key", "count": 0 }""", "hasItem count")]
    [InlineData("""{ "type": "lacksItem", "item": "gem" }""", "unknown item 'gem'")]
    [InlineData("""{ "type": "reputation", "faction": "guild", "min": 1 }""", "unknown faction 'guild'")]
    [InlineData("""{ "type": "reputation", "faction": "order" }""", "needs at least one of min, max, or equals")]
    [InlineData("""{ "type": "questStage", "quest": "side", "min": 1 }""", "unknown quest 'side'")]
    [InlineData("""{ "type": "questStage", "quest": "main" }""", "needs at least one of min, max, or equals")]
    [InlineData("""{ "type": "class", "class": "bard" }""", "unknown class 'bard'")]
    [InlineData("""{ "type": "background", "background": "noble" }""", "unknown background 'noble'")]
    [InlineData("""{ "type": "abilityScore", "ability": "str", "min": 0 }""", "abilityScore min")]
    [InlineData("""{ "type": "visited", "room": "void" }""", "unknown room 'void'")]
    [InlineData("""{ "type": "all", "conditions": [] }""", "'all' condition needs at least one")]
    [InlineData("""{ "type": "any", "conditions": [] }""", "'any' condition needs at least one")]
    [InlineData("""{ "type": "not", "conditions": [] }""", "'not' condition needs at least one")]
    public void Validate_Conditions(string condition, string fragment)
    {
        AssertError(RoomErrors($$"""[ { "id": "start", "name": "S", "description": "D", "variants": [ { "conditions": [ {{condition}} ], "text": "T" } ] } ]"""), fragment);
    }

    [Theory]
    [InlineData("""{ "type": "setFlag", "flag": "" }""", "setFlag flag must not be blank")]
    [InlineData("""{ "type": "clearFlag", "flag": "" }""", "clearFlag flag must not be blank")]
    [InlineData("""{ "type": "setCounter", "counter": "", "value": 1 }""", "counter must not be blank")]
    [InlineData("""{ "type": "changeReputation", "faction": "guild", "amount": 1 }""", "unknown faction 'guild'")]
    [InlineData("""{ "type": "setQuestStage", "quest": "main", "stage": 55 }""", "quest 'main' has no stage 55")]
    [InlineData("""{ "type": "setQuestStage", "quest": "side", "stage": 10 }""", "unknown quest 'side'")]
    [InlineData("""{ "type": "giveItem", "item": "gem" }""", "unknown item 'gem'")]
    [InlineData("""{ "type": "startShop", "shop": "bazaar" }""", "startShop references unknown shop 'bazaar'")]
    [InlineData("""{ "type": "giveItem", "item": "key", "count": 0 }""", "giveItem count")]
    [InlineData("""{ "type": "removeItem", "item": "gem" }""", "unknown item 'gem'")]
    [InlineData("""{ "type": "removeItem", "item": "key", "count": 0 }""", "removeItem count")]
    [InlineData("""{ "type": "giveGold", "amount": -1 }""", "giveGold amount")]
    [InlineData("""{ "type": "restoreMana", "amount": -1 }""", "restoreMana amount")]
    [InlineData("""{ "type": "heal", "dice": "1d4-10" }""", "heal dice must never roll below 0")]
    [InlineData("""{ "type": "damage", "dice": "nope" }""", "damage dice")]
    [InlineData("""{ "type": "savingThrow", "ability": "dex", "dc": 0 }""", "savingThrow dc")]
    [InlineData("""{ "type": "skillCheck", "check": { "skill": "juggling", "dc": 10 } }""", "unknown skill 'juggling'")]
    [InlineData("""{ "type": "message", "text": "" }""", "message text must not be blank")]
    [InlineData("""{ "type": "moveTo", "room": "void" }""", "moveTo references unknown room 'void'")]
    [InlineData("""{ "type": "startDialogue", "dialogue": "nobody" }""", "unknown dialogue 'nobody'")]
    [InlineData("""{ "type": "startCombat", "encounter": "ambush" }""", "unknown encounter 'ambush'")]
    [InlineData("""{ "type": "if", "conditions": [] }""", "'if' effect needs at least one condition")]
    public void Validate_Effects(string effect, string fragment)
    {
        AssertError(RoomErrors($$"""[ { "id": "start", "name": "S", "description": "D", "onEnter": [ {{effect}} ] } ]"""), fragment);
    }
}
