using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RpgEngine.Core.Dice;

/// <summary>
/// A parsed dice expression such as <c>d20</c>, <c>2d6+3</c>, <c>4d6kh3</c>, <c>1d8+1d6+2</c> or <c>19</c>.
/// Grammar: <c>term (('+'|'-') term)*</c> where a term is <c>[N]dS[khK|klK]</c> or an integer.
/// </summary>
public sealed partial class DiceExpression
{
    public const int MaxDiceCount = 100;
    public const int MinSides = 2;
    public const int MaxSides = 1000;
    public const int MaxConstant = 10_000;
    public const int MaxTerms = 20;

    private double? _average;

    private DiceExpression(IReadOnlyList<DiceTerm> terms)
    {
        Terms = terms;
        Text = Format(terms);
        Min = terms.Sum(term => term.Sign > 0 ? term.UnsignedMin : -term.UnsignedMax);
        Max = terms.Sum(term => term.Sign > 0 ? term.UnsignedMax : -term.UnsignedMin);
    }

    public IReadOnlyList<DiceTerm> Terms { get; }

    /// <summary>Normalized text, e.g. <c>" 2D6 + 3 "</c> becomes <c>2d6+3</c>.</summary>
    public string Text { get; }

    public int Min { get; }

    public int Max { get; }

    /// <summary>Exact expected value (keep-highest/lowest terms use order statistics).</summary>
    public double Average => _average ??= Terms.Sum(term => term.Sign * term.UnsignedAverage());

    public bool HasDice => Terms.Any(term => !term.IsConstant);

    public static DiceExpression Parse(string text)
    {
        if (!TryParse(text, out var expression, out var error))
        {
            throw new FormatException(error);
        }

        return expression;
    }

    public static bool TryParse(
        string? text,
        [NotNullWhen(true)] out DiceExpression? expression,
        [NotNullWhen(false)] out string? error)
    {
        expression = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Dice expression is empty.";
            return false;
        }

        var normalized = OperatorSpacing().Replace(text.Trim().ToLowerInvariant(), "$1");
        if (normalized.Any(char.IsWhiteSpace))
        {
            error = $"Dice expression '{text}' contains unexpected spaces.";
            return false;
        }

        var terms = new List<DiceTerm>();
        var position = 0;

        while (position < normalized.Length)
        {
            var match = TermPattern().Match(normalized, position);
            if (!match.Success)
            {
                error = $"Dice expression '{text}' is invalid near '{normalized[position..]}'.";
                return false;
            }

            var hasSign = match.Groups["sign"].Success;
            if (terms.Count > 0 && !hasSign)
            {
                error = $"Dice expression '{text}' needs '+' or '-' between terms.";
                return false;
            }

            if (!TryBuildTerm(match, text, out var term, out error))
            {
                return false;
            }

            terms.Add(term);
            if (terms.Count > MaxTerms)
            {
                error = $"Dice expression '{text}' has more than {MaxTerms} terms.";
                return false;
            }

            position += match.Length;
        }

        expression = new DiceExpression(terms);
        error = null;
        return true;
    }

    /// <summary>The same expression with every dice term doubled (critical hit); constants are not doubled.</summary>
    public DiceExpression Doubled() => new(Terms.Select(term => term.Doubled()).ToList());

    /// <summary>Concatenates two expressions, e.g. weapon damage plus a bonus die.</summary>
    public DiceExpression Plus(DiceExpression other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new DiceExpression(Terms.Concat(other.Terms).ToList());
    }

    /// <summary>Adds a flat modifier (positive or negative). A zero modifier returns this expression.</summary>
    public DiceExpression Plus(int modifier) =>
        modifier == 0 ? this : new DiceExpression([.. Terms, DiceTerm.Flat(Math.Sign(modifier), Math.Abs(modifier))]);

    public override string ToString() => Text;

    private static bool TryBuildTerm(
        Match match,
        string original,
        [NotNullWhen(true)] out DiceTerm? term,
        [NotNullWhen(false)] out string? error)
    {
        term = null;
        var sign = match.Groups["sign"].Value == "-" ? -1 : 1;

        if (match.Groups["constant"].Success)
        {
            if (!int.TryParse(match.Groups["constant"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value > MaxConstant)
            {
                error = $"Dice expression '{original}' has a constant larger than {MaxConstant}.";
                return false;
            }

            term = DiceTerm.Flat(sign, value);
            error = null;
            return true;
        }

        var countText = match.Groups["count"].Value;
        var count = 1;
        if (countText.Length > 0 &&
            (!int.TryParse(countText, NumberStyles.None, CultureInfo.InvariantCulture, out count) || count < 1 || count > MaxDiceCount))
        {
            error = $"Dice expression '{original}' must roll between 1 and {MaxDiceCount} dice per term.";
            return false;
        }

        if (!int.TryParse(match.Groups["sides"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var sides) ||
            sides < MinSides || sides > MaxSides)
        {
            error = $"Dice expression '{original}' must use dice with {MinSides} to {MaxSides} sides.";
            return false;
        }

        var keep = KeepRule.All;
        var keepCount = 0;
        if (match.Groups["keep"].Success)
        {
            keep = match.Groups["keep"].Value == "h" ? KeepRule.Highest : KeepRule.Lowest;
            if (!int.TryParse(match.Groups["keepCount"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedKeep) ||
                parsedKeep < 1 || parsedKeep > count)
            {
                error = $"Dice expression '{original}' must keep between 1 and {count} dice.";
                return false;
            }

            keepCount = parsedKeep;
        }

        term = DiceTerm.Dice(sign, count, sides, keep, keepCount);
        error = null;
        return true;
    }

    private static string Format(IReadOnlyList<DiceTerm> terms)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < terms.Count; i++)
        {
            var term = terms[i];
            if (term.Sign < 0)
            {
                builder.Append('-');
            }
            else if (i > 0)
            {
                builder.Append('+');
            }

            builder.Append(term);
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"\G(?<sign>[+-])?(?:(?<count>\d*)d(?<sides>\d+)(?:k(?<keep>[hl])(?<keepCount>\d+))?|(?<constant>\d+))", RegexOptions.CultureInvariant)]
    private static partial Regex TermPattern();

    [GeneratedRegex(@"\s*([+-])\s*", RegexOptions.CultureInvariant)]
    private static partial Regex OperatorSpacing();
}
