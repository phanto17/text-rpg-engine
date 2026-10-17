namespace RpgEngine.Core.Commands;

/// <summary>
/// Scores how well typed words name a thing. 3 = exact name or alias, 2 = every typed word
/// appears in a name (or the typed words contain a whole name), 1 = a typed word is a prefix
/// (3+ letters) of a name word, 0 = no match.
/// </summary>
public static class TargetMatcher
{
    public const int NoMatch = 0;

    public static int Score(string? query, IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        var queryWords = TextNormalizer.Words(query);
        if (queryWords.Count == 0)
        {
            return NoMatch;
        }

        var best = NoMatch;
        foreach (var name in names)
        {
            var nameWords = TextNormalizer.Words(name);
            if (nameWords.Count == 0)
            {
                continue;
            }

            if (queryWords.SequenceEqual(nameWords))
            {
                return 3;
            }

            if (queryWords.All(nameWords.Contains) || nameWords.All(queryWords.Contains))
            {
                best = Math.Max(best, 2);
            }
            else if (queryWords.Any(q => q.Length >= 3 && nameWords.Any(n => n.StartsWith(q, StringComparison.Ordinal))))
            {
                best = Math.Max(best, 1);
            }
        }

        return best;
    }

    /// <summary>
    /// Picks the best-scoring candidate. Returns the single best match, or every candidate tied
    /// for best (ambiguous), or nothing.
    /// </summary>
    public static IReadOnlyList<T> Best<T>(string? query, IEnumerable<T> candidates, Func<T, IEnumerable<string>> names)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(names);

        var scored = candidates.Select(candidate => (candidate, score: Score(query, names(candidate)))).Where(x => x.score > NoMatch).ToList();
        if (scored.Count == 0)
        {
            return [];
        }

        var top = scored.Max(x => x.score);
        return scored.Where(x => x.score == top).Select(x => x.candidate).ToList();
    }
}
