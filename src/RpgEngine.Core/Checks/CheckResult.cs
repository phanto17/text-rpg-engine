using RpgEngine.Core.Characters;
using RpgEngine.Core.Dice;

namespace RpgEngine.Core.Checks;

/// <summary>
/// A resolved check with its full breakdown, so the UI can show
/// <c>d20 13 + 3 CHA + 2 prof = 18 vs DC 14</c>.
/// </summary>
public sealed record CheckResult(
    CheckKind Kind,
    string Label,
    Ability Ability,
    D20Roll Roll,
    int AbilityModifier,
    int ProficiencyBonus,
    int Dc,
    int Total,
    bool Success,
    bool IsCriticalSuccess,
    bool IsCriticalFailure);
