namespace RpgEngine.Core.Characters;

/// <summary>An immutable set of the six ability scores (1 to 30).</summary>
public sealed record AbilityScores
{
    public const int MinScore = 1;
    public const int MaxScore = 30;

    public AbilityScores(int str, int dex, int con, int @int, int wis, int cha)
    {
        Str = Validate(str, nameof(str));
        Dex = Validate(dex, nameof(dex));
        Con = Validate(con, nameof(con));
        Int = Validate(@int, nameof(@int));
        Wis = Validate(wis, nameof(wis));
        Cha = Validate(cha, nameof(cha));
    }

    public int Str { get; }

    public int Dex { get; }

    public int Con { get; }

    public int Int { get; }

    public int Wis { get; }

    public int Cha { get; }

    public int this[Ability ability] => ability switch
    {
        Ability.Str => Str,
        Ability.Dex => Dex,
        Ability.Con => Con,
        Ability.Int => Int,
        Ability.Wis => Wis,
        Ability.Cha => Cha,
        _ => throw new ArgumentOutOfRangeException(nameof(ability), ability, null),
    };

    /// <summary>The 5e modifier: floor((score - 10) / 2).</summary>
    public static int ModifierFor(int score) => (int)Math.Floor((Validate(score, nameof(score)) - 10) / 2.0);

    public int Modifier(Ability ability) => ModifierFor(this[ability]);

    public static AbilityScores FromMap(IReadOnlyDictionary<Ability, int> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);
        int Get(Ability ability) => scores.TryGetValue(ability, out var value)
            ? value
            : throw new ArgumentException($"Missing score for {ability.Abbreviation()}.", nameof(scores));

        return new AbilityScores(Get(Ability.Str), Get(Ability.Dex), Get(Ability.Con), Get(Ability.Int), Get(Ability.Wis), Get(Ability.Cha));
    }

    public AbilityScores With(Ability ability, int score) => new(
        ability == Ability.Str ? score : Str,
        ability == Ability.Dex ? score : Dex,
        ability == Ability.Con ? score : Con,
        ability == Ability.Int ? score : Int,
        ability == Ability.Wis ? score : Wis,
        ability == Ability.Cha ? score : Cha);

    private static int Validate(int score, string paramName) =>
        score is < MinScore or > MaxScore
            ? throw new ArgumentOutOfRangeException(paramName, score, $"Ability scores must be between {MinScore} and {MaxScore}.")
            : score;
}
