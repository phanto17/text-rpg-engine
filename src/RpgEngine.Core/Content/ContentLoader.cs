using System.Text.Json;
using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Core.Content;

/// <summary>
/// Loads and validates all game content. Every JSON and validation error is collected and thrown
/// together in a <see cref="ContentValidationException"/>.
/// </summary>
public static class ContentLoader
{
    public const string ManifestFile = "game.json";

    public static GameContent LoadFromDirectory(string dataDirectory) => Load(new DirectoryContentFiles(dataDirectory));

    public static GameContent Load(IContentFiles files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var errors = new List<string>();

        var manifest = ReadObject<GameManifestDef>(files, ManifestFile, errors);
        var classes = ReadArray<ClassDef>(files, "Rules/classes.json", errors, required: true);
        var skills = ReadArray<SkillDef>(files, "Rules/skills.json", errors, required: true);
        var spells = ReadArray<SpellDef>(files, "Rules/spells.json", errors);
        var statusEffects = ReadArray<StatusEffectDef>(files, "Rules/status_effects.json", errors);
        var factions = ReadArray<FactionDef>(files, "World/factions.json", errors);
        var quests = ReadArray<QuestDef>(files, "World/quests.json", errors);
        var items = ReadFolder<ItemDef>(files, "Items", errors);
        var monsters = ReadFolder<MonsterDef>(files, "Bestiary", errors);
        var rooms = ReadFolder<RoomDef>(files, "Rooms", errors);
        var dialogues = ReadFolder<DialogueDef>(files, "Dialogues", errors);

        if (manifest is null || errors.Count > 0)
        {
            throw new ContentValidationException(errors);
        }

        var content = new GameContent(manifest, classes, skills, spells, statusEffects, factions, quests, items, monsters, rooms, dialogues);

        var validationErrors = ContentValidator.Validate(content);
        if (validationErrors.Count > 0)
        {
            throw new ContentValidationException(validationErrors);
        }

        return content;
    }

    private static T? ReadObject<T>(IContentFiles files, string path, List<string> errors)
        where T : class
    {
        if (!files.Exists(path))
        {
            errors.Add($"{path}: required file is missing.");
            return null;
        }

        return Deserialize<T>(files, path, errors);
    }

    private static List<T> ReadArray<T>(IContentFiles files, string path, List<string> errors, bool required = false)
    {
        if (!files.Exists(path))
        {
            if (required)
            {
                errors.Add($"{path}: required file is missing.");
            }

            return [];
        }

        return Deserialize<List<T>>(files, path, errors) ?? [];
    }

    private static List<T> ReadFolder<T>(IContentFiles files, string folder, List<string> errors) =>
        files.ListJsonFiles(folder).SelectMany(path => ReadArray<T>(files, path, errors)).ToList();

    private static T? Deserialize<T>(IContentFiles files, string path, List<string> errors)
        where T : class
    {
        try
        {
            var value = JsonSerializer.Deserialize<T>(files.ReadAllText(path), ContentJson.Options);
            if (value is null)
            {
                errors.Add($"{path}: file contains null.");
            }

            return value;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            errors.Add($"{path}: {ex.Message}");
            return null;
        }
    }
}
