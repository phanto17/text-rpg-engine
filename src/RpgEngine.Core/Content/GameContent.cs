using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Core.Content;

/// <summary>All loaded content, in file order (lists) and by id (dictionaries; the first of any duplicate wins).</summary>
public sealed class GameContent
{
    public GameContent(
        GameManifestDef manifest,
        IReadOnlyList<ClassDef> classes,
        IReadOnlyList<SkillDef> skills,
        IReadOnlyList<SpellDef> spells,
        IReadOnlyList<StatusEffectDef> statusEffects,
        IReadOnlyList<FactionDef> factions,
        IReadOnlyList<QuestDef> quests,
        IReadOnlyList<ItemDef> items,
        IReadOnlyList<MonsterDef> monsters,
        IReadOnlyList<RoomDef> rooms,
        IReadOnlyList<DialogueDef> dialogues)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        Manifest = manifest;
        ClassList = classes;
        SkillList = skills;
        SpellList = spells;
        StatusEffectList = statusEffects;
        FactionList = factions;
        QuestList = quests;
        ItemList = items;
        MonsterList = monsters;
        RoomList = rooms;
        DialogueList = dialogues;

        Classes = ById(classes);
        Skills = ById(skills);
        Spells = ById(spells);
        StatusEffects = ById(statusEffects);
        Factions = ById(factions);
        Quests = ById(quests);
        Items = ById(items);
        Monsters = ById(monsters);
        Rooms = ById(rooms);
        Dialogues = ById(dialogues);

        var encounters = new Dictionary<string, EncounterDef>(StringComparer.Ordinal);
        var encounterRooms = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var room in rooms)
        {
            if (room.Encounter is { } encounter && encounters.TryAdd(encounter.Id, encounter))
            {
                encounterRooms[encounter.Id] = room.Id;
            }
        }

        Encounters = encounters;
        EncounterRooms = encounterRooms;
    }

    public GameManifestDef Manifest { get; }

    public RulesDef Rules => Manifest.Rules;

    public IReadOnlyList<ClassDef> ClassList { get; }

    public IReadOnlyList<SkillDef> SkillList { get; }

    public IReadOnlyList<SpellDef> SpellList { get; }

    public IReadOnlyList<StatusEffectDef> StatusEffectList { get; }

    public IReadOnlyList<FactionDef> FactionList { get; }

    public IReadOnlyList<QuestDef> QuestList { get; }

    public IReadOnlyList<ItemDef> ItemList { get; }

    public IReadOnlyList<MonsterDef> MonsterList { get; }

    public IReadOnlyList<RoomDef> RoomList { get; }

    public IReadOnlyList<DialogueDef> DialogueList { get; }

    public IReadOnlyDictionary<string, ClassDef> Classes { get; }

    public IReadOnlyDictionary<string, SkillDef> Skills { get; }

    public IReadOnlyDictionary<string, SpellDef> Spells { get; }

    public IReadOnlyDictionary<string, StatusEffectDef> StatusEffects { get; }

    public IReadOnlyDictionary<string, FactionDef> Factions { get; }

    public IReadOnlyDictionary<string, QuestDef> Quests { get; }

    public IReadOnlyDictionary<string, ItemDef> Items { get; }

    public IReadOnlyDictionary<string, MonsterDef> Monsters { get; }

    public IReadOnlyDictionary<string, RoomDef> Rooms { get; }

    public IReadOnlyDictionary<string, DialogueDef> Dialogues { get; }

    /// <summary>Encounters by id, gathered from the rooms that define them.</summary>
    public IReadOnlyDictionary<string, EncounterDef> Encounters { get; }

    /// <summary>Room id for each encounter id.</summary>
    public IReadOnlyDictionary<string, string> EncounterRooms { get; }

    private static Dictionary<string, T> ById<T>(IEnumerable<T> definitions)
        where T : IContentDef
    {
        var map = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            map.TryAdd(definition.Id, definition);
        }

        return map;
    }
}
