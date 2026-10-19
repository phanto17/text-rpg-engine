using System.Text.Json;
using System.Text.Json.Nodes;
using RpgEngine.Core.Content;

namespace RpgEngine.CLI;

/// <summary>
/// Writes the content JSON Schemas to <c>.vscode/schemas/</c> and maps them onto <c>Data/</c> in
/// <c>.vscode/settings.json</c>, keeping every other setting. Run with <c>--export-schemas</c>
/// after changing a content record.
/// </summary>
internal static class SchemaExporter
{
    public const string SchemaFolder = ".vscode/schemas";

    public const string SettingsFile = ".vscode/settings.json";

    private const string SchemasSetting = "json.schemas";

    private static readonly JsonDocumentOptions ReadOptions = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>Writes every schema and the updated settings under <paramref name="workspaceRoot"/>; returns the files written.</summary>
    public static IReadOnlyList<string> Export(string workspaceRoot)
    {
        ArgumentNullException.ThrowIfNull(workspaceRoot);
        var written = new List<string>();
        var schemaDirectory = Path.Combine(workspaceRoot, SchemaFolder);
        Directory.CreateDirectory(schemaDirectory);

        foreach (var schema in ContentSchemas.All)
        {
            var path = Path.Combine(schemaDirectory, schema.FileName);
            File.WriteAllText(path, ContentSchemas.Generate(schema));
            written.Add(path);
        }

        var settingsPath = Path.Combine(workspaceRoot, SettingsFile);
        var existing = File.Exists(settingsPath) ? File.ReadAllText(settingsPath) : null;
        File.WriteAllText(settingsPath, UpdateSettings(existing));
        written.Add(settingsPath);
        return written;
    }

    /// <summary>
    /// Returns the settings with one <c>json.schemas</c> entry per content schema. Entries pointing
    /// anywhere else are kept, as are all other settings. Comments in the old file are not kept.
    /// </summary>
    public static string UpdateSettings(string? existingJson)
    {
        var settings = string.IsNullOrWhiteSpace(existingJson)
            ? new JsonObject()
            : JsonNode.Parse(existingJson, documentOptions: ReadOptions) as JsonObject
              ?? throw new InvalidDataException($"{SettingsFile} must contain a JSON object.");

        var ours = $"./{SchemaFolder}/";
        var entries = new JsonArray();
        if (settings[SchemasSetting] is JsonArray old)
        {
            foreach (var entry in old.Where(e => e?["url"]?.GetValue<string>() is not { } url || !url.StartsWith(ours, StringComparison.Ordinal)))
            {
                entries.Add(entry?.DeepClone());
            }
        }

        foreach (var schema in ContentSchemas.All)
        {
            entries.Add(new JsonObject
            {
                ["fileMatch"] = new JsonArray(schema.FileMatch.Select(p => (JsonNode?)JsonValue.Create(p)).ToArray()),
                ["url"] = ours + schema.FileName,
            });
        }

        settings[SchemasSetting] = entries;
        return settings.ToJsonString(WriteOptions).ReplaceLineEndings("\n") + "\n";
    }
}
