using RpgEngine.Core.Content;

namespace RpgEngine.Tests.TestSupport;

/// <summary>A three-room content set with a sequence puzzle, a hidden secret, an NPC, and an encounter.</summary>
internal static class ExplorationFixture
{
    public const string Rooms = """
        [
          { "id": "start", "name": "Gate", "description": "A gate in the rain.", "revisit": "The gate again.",
            "variants": [ { "conditions": [ { "type": "flag", "flag": "door_open" } ], "text": "The door stands open." } ],
            "exits": [
              { "direction": "north", "target": "hall", "description": "A dark hall.", "conditions": [ { "type": "flag", "flag": "door_open" } ], "blockedText": "The door is sealed." },
              { "direction": "down", "target": "cellar", "conditions": [ { "type": "flag", "flag": "found_trapdoor" } ] },
              { "direction": "east", "target": "hall" } ],
            "features": [
              { "id": "door", "name": "bronze door", "aliases": ["door"], "description": "Three sigils.",
                "interactions": [
                  { "verb": "read", "label": "Read the inscription", "text": "Sun, then moon." },
                  { "verb": "press", "target": "sun", "label": "Press the sun sigil", "conditions": [ { "type": "counter", "counter": "seq", "equals": 0 } ],
                    "text": "Click.", "effects": [ { "type": "addCounter", "counter": "seq", "amount": 1 } ] },
                  { "verb": "press", "target": "sun", "text": "Wrong!", "effects": [ { "type": "setCounter", "counter": "seq", "value": 0 } ] },
                  { "verb": "press", "target": "moon", "label": "Press the moon sigil", "conditions": [ { "type": "counter", "counter": "seq", "equals": 1 } ],
                    "text": "The door opens.", "effects": [ { "type": "setFlag", "flag": "door_open" } ] },
                  { "verb": "press", "target": "moon", "text": "Wrong!", "effects": [ { "type": "setCounter", "counter": "seq", "value": 0 } ] },
                  { "verb": "use", "target": "key", "text": "The key turns.", "effects": [ { "type": "setFlag", "flag": "door_open" } ] } ] },
              { "id": "stone", "name": "loose stone", "aliases": ["stone"], "description": "A loose stone.",
                "interactions": [
                  { "verb": "search", "once": true, "text": "You pry at it.", "check": { "skill": "perception", "dc": 12 },
                    "success": { "text": "A trapdoor!", "effects": [ { "type": "setFlag", "flag": "found_trapdoor" } ] },
                    "failure": { "text": "Nothing." } },
                  { "verb": "inspect", "once": true, "text": "Scratches mark it." } ] },
              { "id": "statue", "name": "statue", "description": "A stone saint.", "conditions": [ { "type": "flag", "flag": "found_trapdoor" } ],
                "interactions": [ { "verb": "take", "text": "The halo comes loose.", "effects": [ { "type": "giveItem", "item": "relic" } ] } ] } ],
            "items": ["potion", "dagger"],
            "npcs": [ { "id": "sage", "name": "Old Sage", "aliases": ["sage"], "description": "Ancient eyes.", "dialogue": "sage" } ],
            "onFirstEnter": [ { "type": "setQuestStage", "quest": "main", "stage": 10 } ],
            "onEnter": [ { "type": "addCounter", "counter": "visits", "amount": 1 } ] },
          { "id": "hall", "name": "Hall", "description": "A long hall.", "ambient": ["Drip.", "Wind."],
            "exits": [ { "direction": "west", "target": "start" } ],
            "encounter": { "id": "rats", "conditions": [ { "type": "notFlag", "flag": "rats_dead" } ],
                           "enemies": [ { "monster": "rat" } ], "fleeTo": "start",
                           "onResolved": [ { "type": "setFlag", "flag": "rats_dead" } ] } },
          { "id": "cellar", "name": "Cellar", "description": "Damp.", "exits": [ { "direction": "up", "target": "start" } ] }
        ]
        """;

    public const string Dialogue = """
        [ { "id": "sage", "speaker": "Old Sage",
            "entries": [
              { "conditions": [ { "type": "flag", "flag": "sage_friend" } ], "node": "friend" },
              { "conditions": [ { "type": "flag", "flag": "sage_angry" } ], "node": "cold" },
              { "node": "hello" } ],
            "nodes": [
              { "id": "hello", "text": "Greetings, {player.name}.", "effects": [ { "type": "setFlag", "flag": "met" } ], "next": "hub" },
              { "id": "hub", "text": "What do you seek?",
                "choices": [
                  { "text": "Who are you?", "once": true, "next": "who" },
                  { "text": "Trust me.", "check": { "skill": "persuasion", "dc": 14, "advantageIf": [ { "type": "flag", "flag": "read" } ] },
                    "successNode": "trusts", "failureNode": "refuses", "retry": false },
                  { "text": "Read him.", "check": { "skill": "insight", "dc": 12 }, "successNode": "read_ok", "failureNode": "hub" },
                  { "text": "Fight!", "effects": [ { "type": "startCombat", "encounter": "rats" } ], "end": true },
                  { "text": "Wizard talk", "conditions": [ { "type": "class", "class": "wizard" } ], "end": true },
                  { "text": "Goodbye.", "conditions": [ { "type": "flag", "flag": "met" } ], "end": true } ] },
              { "id": "who", "text": "A keeper of secrets.", "next": "hub" },
              { "id": "read_ok", "text": "He is afraid.", "effects": [ { "type": "setFlag", "flag": "read" } ], "next": "hub" },
              { "id": "trusts", "text": "Very well.", "effects": [ { "type": "setFlag", "flag": "sage_friend" }, { "type": "changeReputation", "faction": "order", "amount": 10 } ] },
              { "id": "refuses", "text": "No.", "effects": [ { "type": "setFlag", "flag": "sage_angry" } ] },
              { "id": "friend", "text": "Welcome back, friend.", "choices": [ { "text": "Bye.", "end": true } ] },
              { "id": "cold", "text": "Leave me." } ] } ]
        """;

    public static GameContent Load() => new TestContent()
        .With("Rooms/rooms.json", Rooms)
        .With("Dialogues/sage.json", Dialogue)
        .Load();
}
