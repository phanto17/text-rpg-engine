using System.Text.Json;
using System.Text.RegularExpressions;

namespace RpgEngine.Tests.TestSupport;

/// <summary>Reads the canonical registry sections, excluding incidental identifiers in prose.</summary>
internal static class LoreRegistry
{
    public static IReadOnlyList<string> Compare(string kind, IEnumerable<string> used, IEnumerable<string> documented)
    {
        var actual = used.ToHashSet(StringComparer.Ordinal);
        var registry = documented.ToHashSet(StringComparer.Ordinal);
        return actual.Except(registry).Order().Select(id => $"Undocumented {kind}: {id}")
            .Concat(registry.Except(actual).Order().Select(id => $"Stale {kind}: {id}")).ToArray();
    }

    public static IReadOnlyList<string> Read(string markdown, string heading)
    {
        var start = markdown.IndexOf("### " + heading + " (", StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException($"Missing lore registry section: {heading}");
        var end = markdown.IndexOf("\n### ", start + 1, StringComparison.Ordinal);
        var section = end < 0 ? markdown[start..] : markdown[start..end];
        return Regex.Matches(section, @"(?m)^(?:\|\s*|-\s*)`([a-z][a-z0-9_]*)`")
            .Select(match => match.Groups[1].Value).ToArray();
    }

    public static IEnumerable<string> References(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in element.EnumerateObject())
            {
                if (entry.Name == property && entry.Value.ValueKind == JsonValueKind.String)
                    yield return entry.Value.GetString()!;
                foreach (var value in References(entry.Value, property)) yield return value;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in element.EnumerateArray())
                foreach (var value in References(entry, property)) yield return value;
        }
    }
}
