using System.Text.Json;
using RpgEngine.Core.Content;
using RpgEngine.Pixel.Presentation;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Content;

public sealed class LoreRegistryTests
{
    [Theory]
    [InlineData("flag", "Flags")]
    [InlineData("counter", "Counters")]
    [InlineData("quest", "Quests")]
    public void ShippedRegistry_MatchesDataInBothDirections(string property, string heading)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "RpgEngine.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var lore = File.ReadAllText(Path.Combine(root.FullName, "Docs", "LORE.md"));
        var documented = LoreRegistry.Read(lore, heading);
        Assert.Equal(documented.Count, documented.Distinct().Count());
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(RealDataTests.DataDirectory, "*.json", SearchOption.AllDirectories))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions
            { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            used.UnionWith(LoreRegistry.References(document.RootElement, property));
        }
        if (property == "quest") used.UnionWith(ContentLoader.LoadFromDirectory(RealDataTests.DataDirectory).Quests.Keys);
        Assert.Empty(LoreRegistry.Compare(property, used, documented));
    }

    [Fact]
    public void Compare_MissingAndObsoleteSymbols_ReportsBothDirections()
    {
        Assert.Equal(["Undocumented flag: new_flag", "Stale flag: old_flag"],
            LoreRegistry.Compare("flag", ["shared", "new_flag"], ["shared", "old_flag"]));
    }

    [Fact]
    public void Read_ProseAndOtherSections_OnlyReadsRegistryRows()
    {
        var markdown = "### Flags (`flag`)\nProse `not_a_flag`.\n| `real_flag` | `another_reference` |\n### Counters (`counter`)\n| `count` | value |";
        Assert.Equal(["real_flag"], LoreRegistry.Read(markdown, "Flags"));
        Assert.Throws<InvalidOperationException>(() => LoreRegistry.Read(markdown, "Quests"));
    }

    [Fact]
    public void References_NestedEffectsAndConditions_FindsEveryReference()
    {
        using var document = JsonDocument.Parse("""{"effects":[{"type":"if","conditions":[{"flag":"read"}],"then":[{"flag":"written"}]}]}""");
        Assert.Equal(["read", "written"], LoreRegistry.References(document.RootElement, "flag"));
    }

    [Fact]
    public void ShippedBindings_ReferenceExistingItemsAndWrittenState()
    {
        var content = ContentLoader.LoadFromDirectory(RealDataTests.DataDirectory);
        var bindings = PresentationLoader.Load(RealDataTests.DataDirectory).Presentation.Bindings;
        var flags = new HashSet<string>();
        var counters = new HashSet<string>();
        foreach (var file in Directory.EnumerateFiles(RealDataTests.DataDirectory, "*.json", SearchOption.AllDirectories))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            flags.UnionWith(LoreRegistry.References(doc.RootElement, "flag"));
            counters.UnionWith(LoreRegistry.References(doc.RootElement, "counter"));
        }
        Assert.All(new[] { bindings.ResonanceCounter, bindings.SigilCounter }, id => Assert.True(id is not null && counters.Contains(id)));
        Assert.All(new[] { bindings.LanternItem, bindings.SunSealItem, bindings.MoonSealItem }, id => Assert.True(id is not null && content.Items.ContainsKey(id)));
        Assert.All(bindings.BellMendedFlags.Concat(bindings.BrazierLitFlags), id => Assert.Contains(id, flags));
    }
}
