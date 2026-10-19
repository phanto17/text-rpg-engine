using System.Text.Json;
using System.Text.RegularExpressions;
using RpgEngine.Core.Characters;
using RpgEngine.Core.Content;
using RpgEngine.Core.World;

namespace RpgEngine.Core.Persistence;

/// <summary>Writes and reads <c>&lt;slot&gt;.json</c> save files in a directory (by default <c>./Saves</c>).</summary>
public sealed partial class SaveService
{
    private readonly TimeProvider _time;

    public SaveService(string directory, TimeProvider time)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(time);
        Directory = Path.GetFullPath(directory);
        _time = time;
    }

    public string Directory { get; }

    public static bool IsValidSlotName(string? slot) => slot is not null && SlotPattern().IsMatch(slot);

    public bool Exists(string slot) => IsValidSlotName(slot) && File.Exists(PathFor(slot));

    public string Save(GameState state, GameContent content, string slot)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(content);
        RequireValidSlot(slot);

        var save = Capture(state, content, _time.GetUtcNow());
        var path = PathFor(slot);
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            var temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(save, ContentJson.IndentedOptions));
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new SaveGameException($"Could not write save '{slot}': {ex.Message}", ex);
        }

        return path;
    }

    public LoadedGame Load(string slot, GameContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        RequireValidSlot(slot);
        var save = Read(slot);

        if (save.SaveVersion != SaveGame.CurrentVersion)
        {
            throw new SaveGameException($"Save '{slot}' uses format version {save.SaveVersion}; this game reads version {SaveGame.CurrentVersion}.");
        }

        return new LoadedGame(Restore(save, content), save, save.ContentVersion != content.Manifest.ContentVersion);
    }

    public IReadOnlyList<SaveSlotInfo> ListSlots()
    {
        if (!System.IO.Directory.Exists(Directory))
        {
            return [];
        }

        var slots = new List<SaveSlotInfo>();
        foreach (var path in System.IO.Directory.EnumerateFiles(Directory, "*.json"))
        {
            var slot = Path.GetFileNameWithoutExtension(path);
            if (!IsValidSlotName(slot))
            {
                continue;
            }

            try
            {
                var save = Read(slot);
                slots.Add(new SaveSlotInfo(slot, save.Player.Name, save.Player.Level, save.CurrentRoom, save.SavedAtUtc));
            }
            catch (SaveGameException)
            {
                // Unreadable saves are skipped in the listing; loading them reports the problem.
            }
        }

        return slots.OrderByDescending(info => info.SavedAtUtc).ToList();
    }

    public static SaveGame Capture(GameState state, GameContent content, DateTimeOffset savedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(content);
        var player = state.Player;
        var world = state.World;

        return new SaveGame
        {
            SaveVersion = SaveGame.CurrentVersion,
            ContentVersion = content.Manifest.ContentVersion,
            SavedAtUtc = savedAtUtc,
            CurrentRoom = state.CurrentRoomId,
            Player = new PlayerData
            {
                Name = player.Name,
                ClassId = player.ClassId,
                Background = player.BackgroundId,
                Level = player.Level,
                Xp = player.Xp,
                Abilities = player.Abilities,
                ProficientSkills = Sorted(player.ProficientSkills),
                ExpertiseSkills = Sorted(player.ExpertiseSkills),
                SavingThrows = player.SavingThrows.Order().ToList(),
                Spells = player.SpellIds.ToList(),
                MaxHp = player.MaxHp,
                CurrentHp = player.CurrentHp,
                MaxMana = player.MaxMana,
                CurrentMana = player.CurrentMana,
                Gold = player.Gold,
                Inventory = player.Inventory.Entries,
                EquippedWeapon = player.EquippedWeaponId,
                EquippedArmor = player.EquippedArmorId,
            },
            World = new WorldData
            {
                Flags = Sorted(world.Flags),
                Counters = SortedMap(world.Counters),
                Reputation = SortedMap(world.Reputation),
                QuestStages = SortedMap(world.QuestStages),
                VisitedRooms = Sorted(world.VisitedRooms),
                RoomItems = world.RoomItems.Where(pair => pair.Value.Count > 0).OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
                Used = Sorted(world.Used),
            },
        };
    }

    public static GameState Restore(SaveGame save, GameContent content)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(content);
        var data = save.Player;

        Require(content.Classes.ContainsKey(data.ClassId), $"unknown class '{data.ClassId}'");
        Require(data.Background is not null && content.Backgrounds.ContainsKey(data.Background), $"unknown background '{data.Background ?? "(none)"}'");
        Require(content.Rooms.ContainsKey(save.CurrentRoom), $"unknown room '{save.CurrentRoom}'");
        foreach (var itemId in data.Inventory.Select(e => e.ItemId).Concat(save.World.RoomItems.Values.SelectMany(items => items)))
        {
            Require(content.Items.ContainsKey(itemId), $"unknown item '{itemId}'");
        }

        foreach (var spellId in data.Spells)
        {
            Require(content.Spells.ContainsKey(spellId), $"unknown spell '{spellId}'");
        }

        Require(data.EquippedWeapon is null || data.Inventory.Any(e => e.ItemId == data.EquippedWeapon), "the equipped weapon is not in the inventory");
        Require(data.EquippedArmor is null || data.Inventory.Any(e => e.ItemId == data.EquippedArmor), "the equipped armor is not in the inventory");

        PlayerCharacter player;
        try
        {
            player = new PlayerCharacter(
                data.Name,
                data.ClassId,
                data.Background!,
                data.Abilities,
                data.MaxHp,
                data.MaxMana,
                data.ProficientSkills,
                data.ExpertiseSkills,
                data.SavingThrows,
                data.Spells,
                content.Rules.ProficiencyBonusByLevel);
            player.RestoreState(data.Level, data.Xp, data.CurrentHp, data.CurrentMana, data.Gold);
            foreach (var entry in data.Inventory)
            {
                player.Inventory.Add(entry.ItemId, entry.Count);
            }
        }
        catch (ArgumentException ex)
        {
            throw new SaveGameException($"The save file is damaged: {ex.Message}", ex);
        }

        player.EquippedWeaponId = data.EquippedWeapon;
        player.EquippedArmorId = data.EquippedArmor;

        var world = new WorldState();
        var w = save.World;
        foreach (var flag in w.Flags)
        {
            world.SetFlag(flag);
        }

        foreach (var (counter, value) in w.Counters)
        {
            world.SetCounter(counter, value);
        }

        foreach (var (faction, value) in w.Reputation)
        {
            world.ChangeReputation(faction, value);
        }

        foreach (var (quest, stage) in w.QuestStages)
        {
            world.AdvanceQuest(quest, stage);
        }

        foreach (var room in w.VisitedRooms)
        {
            world.MarkVisited(room);
        }

        foreach (var (room, items) in w.RoomItems)
        {
            foreach (var item in items)
            {
                world.AddItemToRoom(room, item);
            }
        }

        foreach (var key in w.Used)
        {
            world.MarkUsed(key);
        }

        return new GameState(player, world, save.CurrentRoom);
    }

    private SaveGame Read(string slot)
    {
        var path = PathFor(slot);
        if (!File.Exists(path))
        {
            throw new SaveGameException($"There is no save named '{slot}'.");
        }

        try
        {
            return JsonSerializer.Deserialize<SaveGame>(File.ReadAllText(path), ContentJson.Options)
                ?? throw new SaveGameException($"Save '{slot}' is empty.");
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or IOException or ArgumentException)
        {
            throw new SaveGameException($"Save '{slot}' could not be read: {ex.Message}", ex);
        }
    }

    private string PathFor(string slot) => Path.Combine(Directory, slot + ".json");

    private static void RequireValidSlot(string slot)
    {
        if (!IsValidSlotName(slot))
        {
            throw new SaveGameException($"'{slot}' is not a valid save name (use 1-32 letters, digits, '-' or '_').");
        }
    }

    private static void Require(bool condition, string problem)
    {
        if (!condition)
        {
            throw new SaveGameException($"The save file does not match the game content: {problem}.");
        }
    }

    private static List<string> Sorted(IEnumerable<string> values) => values.Order(StringComparer.Ordinal).ToList();

    private static Dictionary<string, int> SortedMap(IReadOnlyDictionary<string, int> values) =>
        values.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    [GeneratedRegex("^[A-Za-z0-9_-]{1,32}$", RegexOptions.CultureInvariant)]
    private static partial Regex SlotPattern();
}
