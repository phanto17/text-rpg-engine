using RpgEngine.Core.Content;

namespace RpgEngine.Tests.TestSupport;

/// <summary>
/// Builds small in-memory content sets from JSON snippets. Starts from a minimal valid adventure;
/// tests replace individual files with <see cref="With"/>.
/// </summary>
internal sealed class TestContent
{
    public const string Manifest = """
        {
          "title": "Test Adventure",
          "contentVersion": "0.0.1",
          "startRoom": "start",
          "intro": "You wake in the dark.",
          "startingGold": 5,
          "rules": {
            "standardArray": [15, 14, 13, 12, 10, 8],
            "proficiencyBonusByLevel": [2, 2, 2, 2, 3],
            "naturalCritsOnChecks": true,
            "monsterHp": "average",
            "flee": { "skill": "acrobatics", "dc": 12 },
            "defendStatus": "defending"
          },
          "epilogues": [ { "text": "The end, {player.name}." } ]
        }
        """;

    public const string Classes = """
        [
          {
            "id": "fighter", "name": "Fighter", "description": "A trained warrior.", "hitDie": 10,
            "abilityPriority": ["str", "con", "dex", "wis", "cha", "int"],
            "savingThrows": ["str", "con"],
            "skillChoices": { "count": 2, "from": ["athletics", "perception", "persuasion", "intimidation"] },
            "startingItems": [ { "item": "sword", "equip": true }, { "item": "mail", "equip": true }, { "item": "potion", "count": 2 } ],
            "spells": ["second_wind"]
          },
          {
            "id": "wizard", "name": "Wizard", "description": "A scholar of magic.", "hitDie": 6,
            "abilityPriority": ["int", "con", "dex", "wis", "cha", "str"],
            "savingThrows": ["int", "wis"],
            "skillChoices": { "count": 2, "from": ["religion", "insight", "perception"] },
            "expertiseCount": 1,
            "mana": { "base": 4, "ability": "int" },
            "startingItems": [ { "item": "staff", "equip": true } ],
            "spells": ["firebolt", "missile"]
          }
        ]
        """;

    /// <summary>Burdens: <c>orphan</c> grants stealth (in neither class's list), <c>bailiff</c> grants persuasion and 10 gold.</summary>
    public const string Backgrounds = """
        [
          { "id": "orphan", "name": "Orphan", "description": "Raised by no one.", "bonusSkill": "stealth", "keepsakeItem": "relic" },
          { "id": "bailiff", "name": "Bailiff", "description": "Collects debts.", "bonusSkill": "persuasion", "bonusGold": 10, "keepsakeItem": "key" }
        ]
        """;

    public const string Skills = """
        [
          { "id": "athletics", "name": "Athletics", "ability": "str" },
          { "id": "acrobatics", "name": "Acrobatics", "ability": "dex" },
          { "id": "stealth", "name": "Stealth", "ability": "dex" },
          { "id": "religion", "name": "Religion", "ability": "int" },
          { "id": "insight", "name": "Insight", "ability": "wis" },
          { "id": "perception", "name": "Perception", "ability": "wis" },
          { "id": "persuasion", "name": "Persuasion", "ability": "cha" },
          { "id": "intimidation", "name": "Intimidation", "ability": "cha" }
        ]
        """;

    public const string Spells = """
        [
          { "id": "second_wind", "name": "Second Wind", "kind": "technique", "description": "Catch your breath.",
            "cost": { "usesPerEncounter": 1 }, "target": "self",
            "resolution": { "type": "effects", "effects": [ { "type": "heal", "dice": "1d10+1" } ] } },
          { "id": "firebolt", "name": "Fire Bolt", "kind": "spell", "description": "A mote of flame.",
            "target": "enemy", "resolution": { "type": "spellAttack", "damage": "1d10", "damageType": "fire" } },
          { "id": "missile", "name": "Magic Missile", "kind": "spell", "description": "Unerring darts.",
            "cost": { "mana": 2 }, "target": "enemy", "resolution": { "type": "autoHit", "damage": "3d4+3", "damageType": "force" } }
        ]
        """;

    public const string StatusEffects = """
        [
          { "id": "defending", "name": "Defending", "description": "Braced.", "durationRounds": 1, "modifiers": { "acBonus": 2 } },
          { "id": "chilled", "name": "Chilled", "description": "Numb.", "durationRounds": 2, "modifiers": { "attackRolls": "disadvantage" } }
        ]
        """;

    public const string Factions = """
        [ { "id": "order", "name": "The Order", "description": "Keepers of the dead." } ]
        """;

    public const string Quests = """
        [ { "id": "main", "name": "The Main Quest", "stages": [ { "stage": 10, "journal": "Begin." }, { "stage": 100, "journal": "Done." } ] } ]
        """;

    public const string Items = """
        [
          { "id": "sword", "name": "Longsword", "aliases": ["blade"], "type": "weapon", "description": "Steel.", "value": 15,
            "weapon": { "damage": "1d8", "damageType": "slashing" } },
          { "id": "dagger", "name": "Dagger", "type": "weapon", "description": "Short and sharp.",
            "weapon": { "damage": "1d4", "damageType": "piercing", "finesse": true } },
          { "id": "staff", "name": "Quarterstaff", "type": "weapon", "description": "Oak.",
            "weapon": { "damage": "1d6", "damageType": "bludgeoning" } },
          { "id": "mail", "name": "Chain Mail", "type": "armor", "description": "Heavy rings.", "armor": { "baseAc": 16, "maxDexBonus": 0 } },
          { "id": "potion", "name": "Potion of Healing", "aliases": ["red potion"], "type": "consumable", "description": "Red and warm.",
            "use": { "inCombat": true, "effects": [ { "type": "heal", "dice": "2d4+2" } ] } },
          { "id": "key", "name": "Iron Key", "type": "key", "description": "Cold iron." },
          { "id": "relic", "name": "Relic", "type": "quest", "description": "Ancient." }
        ]
        """;

    public const string Monsters = """
        [
          { "id": "rat", "name": "Giant Rat", "description": "Big teeth.", "hp": "2d6", "ac": 12,
            "abilities": { "str": 7, "dex": 15, "con": 11, "int": 2, "wis": 10, "cha": 4 },
            "attacks": [ { "name": "Bite", "toHit": 4, "damage": "1d4+2", "damageType": "piercing" } ],
            "xp": 25, "loot": [ { "item": "key", "chance": 1.0 } ] }
        ]
        """;

    public const string Rooms = """
        [
          { "id": "start", "name": "Start", "description": "A bare room.",
            "exits": [ { "direction": "north", "target": "hall", "description": "A corridor." } ] },
          { "id": "hall", "name": "Hall", "description": "A long hall.",
            "exits": [ { "direction": "south", "target": "start" } ] }
        ]
        """;

    private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal)
    {
        ["game.json"] = Manifest,
        ["Rules/classes.json"] = Classes,
        ["Rules/backgrounds.json"] = Backgrounds,
        ["Rules/skills.json"] = Skills,
        ["Rules/spells.json"] = Spells,
        ["Rules/status_effects.json"] = StatusEffects,
        ["World/factions.json"] = Factions,
        ["World/quests.json"] = Quests,
        ["Items/items.json"] = Items,
        ["Bestiary/monsters.json"] = Monsters,
        ["Rooms/rooms.json"] = Rooms,
    };

    public TestContent With(string path, string json)
    {
        _files[path] = json;
        return this;
    }

    public TestContent Without(string path)
    {
        _files.Remove(path);
        return this;
    }

    public InMemoryContentFiles Files() => new(_files);

    public GameContent Load() => ContentLoader.Load(Files());

    /// <summary>Loads the content and returns every error (empty when it is valid).</summary>
    public IReadOnlyList<string> Errors()
    {
        try
        {
            ContentLoader.Load(Files());
            return [];
        }
        catch (ContentValidationException ex)
        {
            return ex.Errors;
        }
    }
}
