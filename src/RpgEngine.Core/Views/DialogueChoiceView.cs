using RpgEngine.Core.Dice;

namespace RpgEngine.Core.Views;

/// <summary>
/// A dialogue choice the player can pick. For choices with a check, <see cref="CheckLabel"/> and
/// <see cref="Dc"/> drive the <c>[Persuasion DC 14]</c> badge and <see cref="Mode"/> the advantage badge.
/// </summary>
public sealed record DialogueChoiceView(int Index, string Text, string? CheckLabel, int? Dc, RollMode Mode);
