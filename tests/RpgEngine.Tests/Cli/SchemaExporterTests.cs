using System.Text.Json.Nodes;
using RpgEngine.CLI;
using RpgEngine.Core.Content;
using Spectre.Console.Testing;

namespace RpgEngine.Tests.Cli;

public sealed class SchemaExporterTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "rpg-schemas-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Parse_ExportSchemas_TakesAnOptionalFolder()
    {
        Assert.Equal("ws", CliOptions.Parse(["--export-schemas", "ws"], out _)!.ExportSchemasRoot);
        Assert.Equal(Environment.CurrentDirectory, CliOptions.Parse(["--export-schemas", "--no-anim"], out _)!.ExportSchemasRoot);
        Assert.Null(CliOptions.Parse([], out _)!.ExportSchemasRoot);
        Assert.DoesNotContain("--export-schemas", CliOptions.Usage, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ExportSchemas_WritesEverySchemaAndTheSettings_WithoutLoadingContent()
    {
        using var console = new TestConsole();

        // --data points nowhere: exporting never loads or validates content.
        var exit = CliHost.Run(["--data", Path.Combine(_root, "missing"), "--export-schemas", _root], console);

        Assert.Equal(CliHost.ExitOk, exit);
        Assert.All(ContentSchemas.All, s => Assert.True(File.Exists(Path.Combine(_root, ".vscode", "schemas", s.FileName))));
        var settings = JsonNode.Parse(File.ReadAllText(Path.Combine(_root, ".vscode", "settings.json")))!;
        var room = settings["json.schemas"]!.AsArray().Single(e => (string?)e!["url"] == "./.vscode/schemas/room.schema.json")!;
        Assert.Equal("/Data/Rooms/*.json", (string?)room["fileMatch"]![0]);
        Assert.Contains($"Wrote {ContentSchemas.All.Count + 1} files", console.Output.Replace("\n", " ", StringComparison.Ordinal));
        Assert.Contains("room.schema.json", console.Output);
    }

    [Fact]
    public void UpdateSettings_KeepsOtherSettingsAndSchemas_AndReplacesOurs()
    {
        const string existing = """
            {
              // a comment the loader skips
              "editor.tabSize": 2,
              "json.schemas": [
                { "fileMatch": ["/other.json"], "url": "https://example.com/other.json" },
                { "fileMatch": ["/Data/Old/*.json"], "url": "./.vscode/schemas/old.schema.json" },
              ],
            }
            """;

        var updated = JsonNode.Parse(SchemaExporter.UpdateSettings(existing))!;

        Assert.Equal(2, (int)updated["editor.tabSize"]!);
        var urls = updated["json.schemas"]!.AsArray().Select(e => (string?)e!["url"]).ToList();
        Assert.Equal("https://example.com/other.json", urls[0]);
        Assert.DoesNotContain("./.vscode/schemas/old.schema.json", urls);
        Assert.Equal(ContentSchemas.All.Count + 1, urls.Count);
        Assert.Equal(SchemaExporter.UpdateSettings(null), SchemaExporter.UpdateSettings("  "));
    }

    [Fact]
    public void Run_ExportSchemas_SettingsThatAreNotAnObject_ExitWithUsageError()
    {
        Directory.CreateDirectory(Path.Combine(_root, ".vscode"));
        File.WriteAllText(Path.Combine(_root, ".vscode", "settings.json"), "[]");
        using var console = new TestConsole();

        Assert.Equal(CliHost.ExitUsageError, CliHost.Run(["--export-schemas", _root], console));
        Assert.Contains("Could not export the schemas", console.Output);
    }
}
