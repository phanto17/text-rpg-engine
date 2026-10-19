using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using RpgEngine.CLI;
using RpgEngine.Core.Content;

namespace RpgEngine.Tests.Content;

public sealed class ContentSchemasTests
{
    private const string RegenerateHint = "Run `dotnet run --project src/RpgEngine.CLI -- --export-schemas` from the repository root and commit the result.";

    private static JsonObject Schema(string name) =>
        JsonNode.Parse(ContentSchemas.Generate(ContentSchemas.All.Single(s => s.Name == name)))!.AsObject();

    // VS Code fileMatch globs: '*' matches within one path segment.
    private static bool Matches(string pattern, string path) =>
        Regex.IsMatch(path, "^" + Regex.Escape(pattern).Replace(@"\*", "[^/]*", StringComparison.Ordinal) + "$");

    [Fact]
    public void All_CoversEveryShippedContentFile_ExactlyOnce()
    {
        var files = Directory.EnumerateFiles(RealDataTests.DataDirectory, "*.json", SearchOption.AllDirectories)
            .Select(f => "/Data/" + Path.GetRelativePath(RealDataTests.DataDirectory, f).Replace('\\', '/'))
            .ToList();

        Assert.NotEmpty(files);
        Assert.All(files, file => Assert.Single(ContentSchemas.All, s => s.FileMatch.Any(p => Matches(p, file))));
        Assert.Equal(ContentSchemas.All.Count, ContentSchemas.All.Select(s => s.FileName).Distinct().Count());
    }

    [Fact]
    public void Generate_RoomSchema_IsAnArrayOfStrictRoomObjects()
    {
        var schema = Schema("room");

        Assert.Equal(ContentSchemas.SchemaDraft, (string?)schema["$schema"]);
        Assert.True((bool)schema["allowComments"]!);
        Assert.True((bool)schema["allowTrailingCommas"]!);
        Assert.Equal("array", (string?)schema["type"]);
        var room = schema["items"]!.AsObject();
        Assert.False((bool)room["additionalProperties"]!);
        Assert.Contains("id", room["required"]!.AsArray().Select(n => (string?)n));
        Assert.Contains("description", room["required"]!.AsArray().Select(n => (string?)n));

        // Enums are camelCase strings, as the loader reads them.
        var direction = room["properties"]!["exits"]!["items"]!["properties"]!["direction"]!;
        Assert.Contains("north", direction["enum"]!.AsArray().Select(n => (string?)n));

        // Polymorphic effects list each "type" discriminator as a strict anyOf branch.
        // Repeated definitions are shared through "$ref" pointers into the same document.
        var effect = Resolve(schema, room["properties"]!["onEnter"]!["items"]!);
        var branches = effect["anyOf"]!.AsArray().Select(b => b!.AsObject()).ToList();
        Assert.Contains(branches, b => (string?)b["properties"]!["type"]!["const"] == "setFlag" && !(bool)b["additionalProperties"]!);
        Assert.Contains(branches, b => (string?)b["properties"]!["type"]!["const"] == "startShop");
    }

    [Fact]
    public void Generate_Manifest_IsASingleObject()
    {
        var schema = Schema("game");

        Assert.Equal("object", (string?)schema["type"]);
        Assert.NotNull(schema["properties"]!["epilogues"]);
        Assert.Equal("RpgEngine content: game manifest", (string?)schema["title"]);
        Assert.Throws<ArgumentNullException>(() => ContentSchemas.Generate(null!));
    }

    [Fact]
    public void CommittedSchemas_MatchTheContentRecords()
    {
        var root = RepositoryRoot();

        foreach (var schema in ContentSchemas.All)
        {
            var path = Path.Combine(root, SchemaExporter.SchemaFolder, schema.FileName);
            Assert.True(File.Exists(path), $"{schema.FileName} is missing. {RegenerateHint}");
            Assert.True(ContentSchemas.Generate(schema) == File.ReadAllText(path).ReplaceLineEndings("\n"), $"{schema.FileName} is out of date. {RegenerateHint}");
        }

        var settings = File.ReadAllText(Path.Combine(root, SchemaExporter.SettingsFile)).ReplaceLineEndings("\n");
        Assert.True(SchemaExporter.UpdateSettings(settings) == settings, $"{SchemaExporter.SettingsFile} is out of date. {RegenerateHint}");
    }

    private static JsonObject Resolve(JsonObject document, JsonNode node)
    {
        if ((string?)node["$ref"] is not { } pointer)
        {
            return node.AsObject();
        }

        JsonNode current = document;
        foreach (var part in pointer.TrimStart('#', '/').Split('/'))
        {
            current = current is JsonArray array ? array[int.Parse(part, System.Globalization.CultureInfo.InvariantCulture)]! : current[part]!;
        }

        return Resolve(document, current);
    }

    private static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "RpgEngine.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("Could not find RpgEngine.slnx above the test output folder.");
    }
}
