using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Core.Content;

/// <summary>
/// Generates JSON Schemas for every content file from the content records and the loader's own
/// <see cref="ContentJson.Options"/>, so editor autocomplete and validation always match what
/// <see cref="ContentLoader"/> accepts: the same property names, enums, required fields, and
/// polymorphic <c>type</c> discriminators, with unknown properties rejected.
/// </summary>
public static class ContentSchemas
{
    public const string SchemaDraft = "https://json-schema.org/draft/2020-12/schema";

    /// <summary>Every content file pattern, mirroring the layout <see cref="ContentLoader"/> reads.</summary>
    public static IReadOnlyList<ContentSchema> All { get; } =
    [
        new("game", typeof(GameManifestDef), IsArray: false, ["/Data/" + ContentLoader.ManifestFile]),
        new("class", typeof(ClassDef), IsArray: true, ["/Data/Rules/classes.json"]),
        new("background", typeof(BackgroundDef), IsArray: true, ["/Data/Rules/backgrounds.json"]),
        new("skill", typeof(SkillDef), IsArray: true, ["/Data/Rules/skills.json"]),
        new("spell", typeof(SpellDef), IsArray: true, ["/Data/Rules/spells.json"]),
        new("status-effect", typeof(StatusEffectDef), IsArray: true, ["/Data/Rules/status_effects.json"]),
        new("faction", typeof(FactionDef), IsArray: true, ["/Data/World/factions.json"]),
        new("quest", typeof(QuestDef), IsArray: true, ["/Data/World/quests.json"]),
        new("shop", typeof(ShopDef), IsArray: true, ["/Data/World/shops.json"]),
        new("presentation", typeof(PresentationDef), IsArray: false, ["/Data/World/presentation.json"]),
        new("item", typeof(ItemDef), IsArray: true, ["/Data/Items/*.json"]),
        new("monster", typeof(MonsterDef), IsArray: true, ["/Data/Bestiary/*.json"]),
        new("room", typeof(RoomDef), IsArray: true, ["/Data/Rooms/*.json"]),
        new("dialogue", typeof(DialogueDef), IsArray: true, ["/Data/Dialogues/*.json"]),
    ];

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>Returns the schema for one content file as indented JSON text ending in a newline.</summary>
    public static string Generate(ContentSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        var type = schema.IsArray ? typeof(List<>).MakeGenericType(schema.Type) : schema.Type;
        var exporterOptions = new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true };

        var body = ContentJson.Options.GetJsonSchemaAsNode(type, exporterOptions).AsObject();

        // Root keywords first, then the generated body. The loader skips comments and accepts
        // trailing commas; allowComments and allowTrailingCommas tell VS Code to do the same.
        var root = new JsonObject
        {
            ["$schema"] = SchemaDraft,
            ["title"] = schema.Title,
            ["allowComments"] = true,
            ["allowTrailingCommas"] = true,
        };
        foreach (var (key, value) in body.ToList())
        {
            body.Remove(key);
            root[key] = value;
        }

        return root.ToJsonString(WriteOptions).ReplaceLineEndings("\n") + "\n";
    }
}
