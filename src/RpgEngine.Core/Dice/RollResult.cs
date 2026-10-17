namespace RpgEngine.Core.Dice;

/// <summary>A single die that was rolled. <see cref="Kept"/> is false for dice dropped by a keep rule.</summary>
public sealed record DieRoll(int Sides, int Value, bool Kept, int Sign);

/// <summary>
/// The outcome of rolling a <see cref="DiceExpression"/>. Every die face is kept so the UI can
/// animate and explain the roll (e.g. <c>2d6+3: [4, 2] + 3 = 9</c>).
/// </summary>
public sealed record RollResult(DiceExpression Expression, IReadOnlyList<DieRoll> Dice, int Modifier, int Total, bool Critical);
