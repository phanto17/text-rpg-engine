using RpgEngine.Core.Characters;
using RpgEngine.Core.Content;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Engine;

namespace RpgEngine.Tests.TestSupport;

/// <summary>A tiny adventure for session tests: an NPC who can start a fight, a lethal trap, an altar that ends the game.</summary>
internal static class SessionFixture
{
    public const string Rooms = """
        [
          { "id": "start", "name": "Gate", "description": "A gate.",
            "exits": [ { "direction": "north", "target": "arena" } ],
            "features": [
              { "id": "needle", "name": "needle", "description": "Sharp.", "interactions": [ { "verb": "touch", "effects": [ { "type": "damage", "dice": "100" } ] } ] },
              { "id": "altar", "name": "altar", "description": "Old.", "interactions": [ { "verb": "pray", "text": "Light!", "effects": [ { "type": "setQuestStage", "quest": "main", "stage": 100 }, { "type": "endGame" } ] } ] } ],
            "npcs": [
              { "id": "sage", "name": "Sage", "description": "Old.", "dialogue": "sage" },
              { "id": "mute", "name": "Mute Monk", "aliases": ["monk"], "description": "Silent.", "dialogue": "mute" } ] },
          { "id": "arena", "name": "Arena", "description": "Sand.",
            "exits": [ { "direction": "south", "target": "start" } ],
            "encounter": { "id": "rats", "conditions": [ { "type": "notFlag", "flag": "rats_dead" } ], "enemies": [ { "monster": "rat" } ], "fleeTo": "start",
                           "onResolved": [ { "type": "setFlag", "flag": "rats_dead" } ] } }
        ]
        """;

    public const string Dialogues = """
        [
          { "id": "sage", "speaker": "Sage", "entries": [ { "node": "hello" } ],
            "nodes": [ { "id": "hello", "text": "Hello, {player.name}.",
              "choices": [
                { "text": "Fight me.", "effects": [ { "type": "startCombat", "encounter": "rats" } ], "end": true },
                { "text": "Bless me.", "effects": [ { "type": "changeReputation", "faction": "order", "amount": 5 }, { "type": "setQuestStage", "quest": "main", "stage": 10 } ], "end": true },
                { "text": "Bye.", "end": true } ] } ] },
          { "id": "mute", "speaker": "Monk", "entries": [ { "node": "silence" } ], "nodes": [ { "id": "silence", "text": "..." } ] }
        ]
        """;

    public static readonly string Manifest = TestContent.Manifest
        .Replace(
            "\"epilogues\": [ { \"text\": \"The end, {player.name}.\" } ]",
            "\"epilogues\": [ { \"conditions\": [ { \"type\": \"reputation\", \"faction\": \"order\", \"min\": 5 } ], \"text\": \"Blessed ending, {player.name}.\" }, { \"text\": \"The end, {player.name}.\" } ], \"gameOverText\": \"Rest now, {player.name}.\"",
            StringComparison.Ordinal);

    public static GameContent Load() => new TestContent()
        .With("game.json", Manifest)
        .With("Rooms/rooms.json", Rooms)
        .With("Dialogues/all.json", Dialogues)
        .Load();

    public static PlayerCharacter Hero(GameContent content, int? hp = null)
    {
        var hero = new CharacterFactory(content).Create("Tester", "fighter", TestCharacters.Scores(str: 16, dex: 12, con: 14), ["athletics", "persuasion"], []);
        if (hp is { } target)
        {
            hero.TakeDamage(hero.MaxHp - target);
        }

        return hero;
    }

    public static GameSession NewSession(int? hp = null, params int[] dice)
    {
        var content = Load();
        return GameSession.NewGame(content, Hero(content, hp), new FixedRandomSource(dice));
    }
}
