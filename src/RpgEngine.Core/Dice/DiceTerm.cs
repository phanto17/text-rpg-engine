namespace RpgEngine.Core.Dice;

/// <summary>One signed term of a dice expression: either a group of dice (<c>4d6kh3</c>) or a constant (<c>3</c>).</summary>
public sealed record DiceTerm
{
    private DiceTerm(int sign, int count, int sides, KeepRule keep, int keepCount, int constant)
    {
        Sign = sign;
        Count = count;
        Sides = sides;
        Keep = keep;
        KeepCount = keepCount;
        Constant = constant;
    }

    /// <summary>+1 or -1.</summary>
    public int Sign { get; }

    /// <summary>Number of dice rolled; 0 for a constant term.</summary>
    public int Count { get; }

    /// <summary>Faces per die; 0 for a constant term.</summary>
    public int Sides { get; }

    public KeepRule Keep { get; }

    /// <summary>Number of dice that count toward the total (equals <see cref="Count"/> when keeping all).</summary>
    public int KeepCount { get; }

    /// <summary>Unsigned value of a constant term; 0 for dice.</summary>
    public int Constant { get; }

    public bool IsConstant => Sides == 0;

    public static DiceTerm Dice(int sign, int count, int sides, KeepRule keep = KeepRule.All, int keepCount = 0) =>
        new(sign, count, sides, keep, keep == KeepRule.All ? count : keepCount, 0);

    public static DiceTerm Flat(int sign, int value) => new(sign, 0, 0, KeepRule.All, 0, value);

    /// <summary>Same term with twice as many dice (critical hit). Constants are unchanged.</summary>
    public DiceTerm Doubled() => IsConstant ? this : new(Sign, Count * 2, Sides, Keep, KeepCount * 2, 0);

    internal int UnsignedMin => IsConstant ? Constant : KeepCount;

    internal int UnsignedMax => IsConstant ? Constant : KeepCount * Sides;

    internal double UnsignedAverage()
    {
        if (IsConstant)
        {
            return Constant;
        }

        if (Keep == KeepRule.All || KeepCount == Count)
        {
            return KeepCount * (Sides + 1) / 2.0;
        }

        return ExpectedKeptSum(Count, Sides, KeepCount, Keep == KeepRule.Highest);
    }

    public override string ToString()
    {
        if (IsConstant)
        {
            return Constant.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var keep = Keep switch
        {
            KeepRule.Highest => $"kh{KeepCount}",
            KeepRule.Lowest => $"kl{KeepCount}",
            _ => string.Empty,
        };

        return $"{Count}d{Sides}{keep}";
    }

    // E[i-th largest of n dice] = sum over x of P(at least i dice show >= x).
    // Keeping the lowest k dice is the same as keeping ranks (n-k+1)..n of the descending order.
    private static double ExpectedKeptSum(int count, int sides, int keepCount, bool highest)
    {
        var firstRank = highest ? 1 : count - keepCount + 1;
        var lastRank = highest ? keepCount : count;
        var total = 0.0;

        for (var rank = firstRank; rank <= lastRank; rank++)
        {
            for (var face = 1; face <= sides; face++)
            {
                var pAtLeastFace = (sides - face + 1) / (double)sides;
                total += ProbabilityAtLeast(count, rank, pAtLeastFace);
            }
        }

        return total;
    }

    private static double ProbabilityAtLeast(int trials, int successes, double p)
    {
        var sum = 0.0;
        for (var j = successes; j <= trials; j++)
        {
            sum += BinomialCoefficient(trials, j) * Math.Pow(p, j) * Math.Pow(1 - p, trials - j);
        }

        return sum;
    }

    private static double BinomialCoefficient(int n, int k)
    {
        var result = 1.0;
        for (var i = 1; i <= k; i++)
        {
            result = result * (n - k + i) / i;
        }

        return result;
    }
}
