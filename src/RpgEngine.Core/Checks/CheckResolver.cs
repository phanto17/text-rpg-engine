using RpgEngine.Core.Characters;
using RpgEngine.Core.Dice;

namespace RpgEngine.Core.Checks;

/// <summary>
/// Resolves d20 checks: d20 + ability modifier + proficiency (doubled with expertise) against a DC.
/// Meeting the DC succeeds. With natural crits enabled, a natural 20 always succeeds and a natural 1 always fails.
/// </summary>
public sealed class CheckResolver
{
    private readonly DiceRoller _dice;

    public CheckResolver(DiceRoller dice, bool naturalCritsOnChecks)
    {
        ArgumentNullException.ThrowIfNull(dice);
        _dice = dice;
        NaturalCritsOnChecks = naturalCritsOnChecks;
    }

    public bool NaturalCritsOnChecks { get; }

    public CheckResult SkillCheck(ICheckSubject subject, string skillId, string skillName, Ability ability, int dc, RollMode mode = RollMode.Normal)
    {
        ArgumentNullException.ThrowIfNull(subject);
        var proficiency = ProficiencyRules.Bonus(subject.GetSkillProficiency(skillId), subject.ProficiencyBonus);
        return Resolve(CheckKind.Skill, skillName, subject, ability, proficiency, dc, mode);
    }

    public CheckResult AbilityCheck(ICheckSubject subject, Ability ability, int dc, RollMode mode = RollMode.Normal)
    {
        ArgumentNullException.ThrowIfNull(subject);
        return Resolve(CheckKind.Ability, ability.DisplayName(), subject, ability, 0, dc, mode);
    }

    public CheckResult SavingThrow(ICheckSubject subject, Ability ability, int dc, RollMode mode = RollMode.Normal)
    {
        ArgumentNullException.ThrowIfNull(subject);
        var proficiency = subject.IsProficientInSave(ability) ? subject.ProficiencyBonus : 0;
        return Resolve(CheckKind.SavingThrow, $"{ability.DisplayName()} save", subject, ability, proficiency, dc, mode);
    }

    private CheckResult Resolve(CheckKind kind, string label, ICheckSubject subject, Ability ability, int proficiency, int dc, RollMode mode)
    {
        var roll = _dice.RollD20(mode);
        var modifier = subject.Abilities.Modifier(ability);
        var total = roll.Natural + modifier + proficiency;
        var criticalSuccess = NaturalCritsOnChecks && roll.IsNatural20;
        var criticalFailure = NaturalCritsOnChecks && roll.IsNatural1;
        var success = criticalSuccess || (!criticalFailure && total >= dc);

        return new CheckResult(kind, label, ability, roll, modifier, proficiency, dc, total, success, criticalSuccess, criticalFailure);
    }
}
