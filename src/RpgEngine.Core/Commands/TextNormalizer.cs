namespace RpgEngine.Core.Commands;

/// <summary>Lowercases text, strips punctuation and articles, and splits it into words.</summary>
public static class TextNormalizer
{
    private static readonly HashSet<string> Articles = new(StringComparer.Ordinal) { "the", "a", "an" };

    public static IReadOnlyList<string> Words(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var cleaned = new string(text.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : ' ').ToArray());
        return cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => !Articles.Contains(word))
            .ToList();
    }
}
