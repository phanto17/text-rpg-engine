namespace RpgEngine.Core.Dice;

/// <summary>Rolls dice expressions and d20s using an injected <see cref="IRandomSource"/>.</summary>
public sealed class DiceRoller
{
    private readonly IRandomSource _random;

    public DiceRoller(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
    }

    public int RollDie(int sides)
    {
        if (sides < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sides), "A die needs at least one side.");
        }

        return _random.Next(1, sides + 1);
    }

    /// <summary>Uniform integer in [minInclusive, maxExclusive) for non-dice choices (targets, weights, loot).</summary>
    public int Pick(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

    public RollResult Roll(string expression, bool critical = false) => Roll(DiceExpression.Parse(expression), critical);

    /// <summary>Rolls the expression. On a <paramref name="critical"/> hit every dice term is doubled, constants are not.</summary>
    public RollResult Roll(DiceExpression expression, bool critical = false)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var dice = new List<DieRoll>();
        var modifier = 0;
        var total = 0;

        foreach (var term in critical ? expression.Doubled().Terms : expression.Terms)
        {
            if (term.IsConstant)
            {
                modifier += term.Sign * term.Constant;
                continue;
            }

            var faces = new int[term.Count];
            for (var i = 0; i < faces.Length; i++)
            {
                faces[i] = RollDie(term.Sides);
            }

            var kept = SelectKept(faces, term.Keep, term.KeepCount);
            for (var i = 0; i < faces.Length; i++)
            {
                dice.Add(new DieRoll(term.Sides, faces[i], kept[i], term.Sign));
                if (kept[i])
                {
                    total += term.Sign * faces[i];
                }
            }
        }

        return new RollResult(expression, dice, modifier, total + modifier, critical);
    }

    public D20Roll RollD20(RollMode mode = RollMode.Normal)
    {
        var first = RollDie(20);
        if (mode == RollMode.Normal)
        {
            return new D20Roll(mode, first, [first]);
        }

        var second = RollDie(20);
        var natural = mode == RollMode.Advantage ? Math.Max(first, second) : Math.Min(first, second);
        return new D20Roll(mode, natural, [first, second]);
    }

    private static bool[] SelectKept(int[] faces, KeepRule keep, int keepCount)
    {
        var kept = new bool[faces.Length];
        var order = Enumerable.Range(0, faces.Length);
        order = keep switch
        {
            KeepRule.Highest => order.OrderByDescending(i => faces[i]).ThenBy(i => i),
            KeepRule.Lowest => order.OrderBy(i => faces[i]).ThenBy(i => i),
            _ => order,
        };

        foreach (var index in order.Take(keepCount))
        {
            kept[index] = true;
        }

        return kept;
    }
}
