using RpgEngine.Core.Characters;
using RpgEngine.Core.Content;

namespace RpgEngine.Tests.TestSupport;

/// <summary>Monsters, spells, statuses, and encounters that exercise every combat rule.</summary>
internal static class CombatFixture
{
    public const string Monsters = """
        [
          { "id": "rat", "name": "Giant Rat", "description": "Teeth.", "hp": "2d6", "ac": 12,
            "abilities": { "str": 7, "dex": 15, "con": 11, "int": 2, "wis": 10, "cha": 4 },
            "attacks": [ { "name": "Bite", "toHit": 4, "damage": "1d4+2", "damageType": "piercing" } ],
            "xp": 25, "loot": [ { "item": "key", "chance": 1.0 } ] },
          { "id": "ghoul", "name": "Ghoul", "description": "Hungry.", "hp": "10", "ac": 12,
            "abilities": { "str": 13, "dex": 12, "con": 10, "int": 7, "wis": 10, "cha": 6 },
            "attacks": [ { "name": "Claw", "toHit": 3, "damage": "1d4+1", "damageType": "slashing",
                           "onHit": { "save": { "ability": "con", "dc": 10 }, "status": "chilled" } } ],
            "resistances": ["slashing"], "vulnerabilities": ["fire"], "immunities": ["force"], "xp": 50 },
          { "id": "coward", "name": "Kobold", "description": "Nervous.", "hp": "10", "ac": 10,
            "abilities": { "str": 7, "dex": 8, "con": 9, "int": 8, "wis": 7, "cha": 8 },
            "attacks": [ { "name": "Dagger", "toHit": 2, "damage": "1d4", "damageType": "piercing" } ],
            "ai": { "fleeBelowHpPercent": 50 }, "xp": 10 },
          { "id": "brute", "name": "Brute", "description": "Huge.", "hp": "30", "ac": 10,
            "abilities": { "str": 16, "dex": 8, "con": 14, "int": 6, "wis": 8, "cha": 6 },
            "attacks": [ { "name": "Slam", "toHit": 5, "damage": "1d6", "damageType": "bludgeoning", "weight": 3 },
                         { "name": "Torch", "toHit": 5, "damage": "1", "damageType": "fire", "weight": 1, "onHit": { "status": "burning" } } ],
            "xp": 100 }
        ]
        """;

    public const string StatusEffects = """
        [
          { "id": "defending", "name": "Defending", "description": "Braced.", "durationRounds": 1, "modifiers": { "acBonus": 2 } },
          { "id": "chilled", "name": "Chilled", "description": "Numb.", "durationRounds": 2, "modifiers": { "attackRolls": "disadvantage" } },
          { "id": "burning", "name": "Burning", "description": "On fire.", "durationRounds": 2, "modifiers": { "damagePerTurn": "1d4" } },
          { "id": "stunned", "name": "Stunned", "description": "Dazed.", "durationRounds": 1, "modifiers": { "skipTurn": true } },
          { "id": "inspired", "name": "Inspired", "description": "Bold.", "durationRounds": 1, "modifiers": { "attackRolls": "advantage" } },
          { "id": "warded", "name": "Warded", "description": "Shielded.", "durationRounds": 2, "modifiers": { "acBonus": 2 } }
        ]
        """;

    public const string Spells = """
        [
          { "id": "second_wind", "name": "Second Wind", "kind": "technique", "description": "Breathe.",
            "cost": { "usesPerEncounter": 1 }, "target": "self",
            "resolution": { "type": "effects", "effects": [ { "type": "heal", "dice": "1d10+1" } ] } },
          { "id": "firebolt", "name": "Fire Bolt", "kind": "spell", "description": "Flame.", "target": "enemy",
            "resolution": { "type": "spellAttack", "damage": "1d10", "damageType": "fire" } },
          { "id": "missile", "name": "Magic Missile", "kind": "spell", "description": "Darts.", "cost": { "mana": 2 }, "target": "enemy",
            "resolution": { "type": "autoHit", "damage": "3d4+3", "damageType": "force" } },
          { "id": "sneak", "name": "Sneak Attack", "kind": "technique", "description": "Stab.", "cost": { "usesPerEncounter": 2 }, "target": "enemy",
            "resolution": { "type": "weaponAttack", "bonusDamage": "1d6" } },
          { "id": "frost", "name": "Frost Ray", "kind": "spell", "description": "Cold.", "cost": { "mana": 1 }, "target": "enemy",
            "resolution": { "type": "save", "ability": "dex", "damage": "2d6", "damageType": "cold", "halfOnSave": true, "status": "chilled" } },
          { "id": "daze", "name": "Daze", "kind": "spell", "description": "Stun.", "target": "enemy",
            "resolution": { "type": "spellAttack", "damage": "1", "damageType": "psychic", "status": "stunned" } },
          { "id": "blast", "name": "Blast", "kind": "spell", "description": "Boom.", "target": "enemy",
            "resolution": { "type": "save", "ability": "con", "dc": 12, "damage": "2d6", "damageType": "thunder" } },
          { "id": "rally", "name": "Rally", "kind": "technique", "description": "Steady.", "cost": { "usesPerEncounter": 1 }, "target": "self",
            "resolution": { "type": "effects", "effects": [ { "type": "heal", "dice": "3" }, { "type": "applyStatus", "status": "warded" } ] } }
        ]
        """;

    public const string ExtraItems = """
        [
          { "id": "leather", "name": "Leather Armor", "type": "armor", "description": "Soft.", "armor": { "baseAc": 11 } },
          { "id": "scale", "name": "Scale Mail", "type": "armor", "description": "Scaly.", "armor": { "baseAc": 14, "maxDexBonus": 2 } },
          { "id": "bow", "name": "Shortbow", "type": "weapon", "description": "Yew.", "weapon": { "damage": "1d6", "damageType": "piercing", "ranged": true } },
          { "id": "firebomb", "name": "Firebomb", "type": "consumable", "description": "Hiss.",
            "use": { "inCombat": true, "target": "enemy", "resolution": { "type": "autoHit", "damage": "2d4", "damageType": "fire" } } },
          { "id": "acid", "name": "Acid Flask", "type": "consumable", "description": "Fizz.",
            "use": { "inCombat": true, "target": "enemy", "resolution": { "type": "spellAttack", "damage": "1d6", "damageType": "acid" } } },
          { "id": "banner", "name": "War Banner", "type": "consumable", "description": "Snap.",
            "use": { "inCombat": true, "consume": true, "effects": [ { "type": "applyStatus", "status": "inspired" } ] } }
        ]
        """;

    public const string Rooms = """
        [
          { "id": "start", "name": "Start", "description": "Bare.", "exits": [ { "direction": "north", "target": "hall" } ] },
          { "id": "hall", "name": "Hall", "description": "Hall.", "exits": [ { "direction": "south", "target": "start" } ] },
          { "id": "rats_room", "name": "Rats", "description": "R.",
            "encounter": { "id": "rats", "intro": "Squeak, {player.name}!", "enemies": [ { "monster": "rat", "count": 2 } ], "fleeTo": "start",
                           "onVictory": [ { "type": "setFlag", "flag": "rats_won" } ], "onResolved": [ { "type": "setFlag", "flag": "rats_done" } ] } },
          { "id": "ghoul_room", "name": "Ghoul", "description": "G.",
            "encounter": { "id": "ghoul", "enemies": [ { "monster": "ghoul" } ], "fleeTo": "start" } },
          { "id": "kobold_room", "name": "Kobold", "description": "K.",
            "encounter": { "id": "kobold", "enemies": [ { "monster": "coward" } ], "canFlee": false } },
          { "id": "brute_room", "name": "Brute", "description": "B.",
            "encounter": { "id": "brute", "enemies": [ { "monster": "brute" } ], "fleeTo": "hall" } },
          { "id": "talk_room", "name": "Talk", "description": "T.",
            "encounter": { "id": "talkers", "enemies": [ { "monster": "ghoul" } ], "fleeTo": "start",
              "parley": { "options": [
                  { "label": "Plead", "check": { "skill": "persuasion", "dc": 12 } },
                  { "label": "Bribe", "conditions": [ { "type": "hasItem", "item": "key" } ], "check": { "ability": "cha", "dc": 5, "advantage": true } } ],
                "onSuccess": [ { "type": "setFlag", "flag": "talked_down" } ], "onFailure": [ { "type": "message", "text": "It snarls." } ] },
              "onResolved": [ { "type": "setFlag", "flag": "talk_done" } ] } }
        ]
        """;

    public static TestContent Content() => new TestContent()
        .With("Bestiary/monsters.json", Monsters)
        .With("Rules/status_effects.json", StatusEffects)
        .With("Rules/spells.json", Spells)
        .With("Items/extra.json", ExtraItems)
        .With("Rooms/rooms.json", Rooms);

    public static GameContent Load() => Content().Load();

    /// <summary>STR 16, DEX 12, CON 14, INT 14, CHA 10; longsword (+5, 1d8+3) and chain mail (AC 16); 20 HP, 6 MP.</summary>
    public static PlayerCharacter Hero(int maxHp = 20, int maxMana = 6, AbilityScores? abilities = null, IReadOnlyList<string>? spells = null)
    {
        var hero = TestCharacters.Create(
            abilities: abilities ?? TestCharacters.Scores(str: 16, dex: 12, con: 14, @int: 14),
            skills: ["persuasion"],
            saves: [Ability.Str, Ability.Con],
            spells: spells ?? ["second_wind", "sneak", "firebolt", "missile", "frost", "daze", "blast"],
            maxHp: maxHp,
            maxMana: maxMana,
            classId: "wizard");
        hero.Inventory.Add("sword");
        hero.Inventory.Add("mail");
        hero.EquippedWeaponId = "sword";
        hero.EquippedArmorId = "mail";
        return hero;
    }
}
