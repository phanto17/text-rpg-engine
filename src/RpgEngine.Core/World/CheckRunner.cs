using RpgEngine.Core.Characters;
using RpgEngine.Core.Checks;
using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;

namespace RpgEngine.Core.World;

/// <summary>Runs content-defined checks for the player, applying data-driven advantage and disadvantage.</summary>
public sealed class CheckRunner
{
    private readonly GameContent _content;
    private readonly CheckResolver _resolver;

    public CheckRunner(GameContent content, CheckResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(resolver);
        _content = content;
        _resolver = resolver;
    }

    /// <summary>The roll mode a check would use right now (shown as a badge before the player commits).</summary>
    public RollMode ModeFor(CheckDef check, GameState state)
    {
        ArgumentNullException.ThrowIfNull(check);
        var advantage = check.Advantage || (check.AdvantageIf.Count > 0 && ConditionEvaluator.AllPass(check.AdvantageIf, state));
        var disadvantage = check.Disadvantage || (check.DisadvantageIf.Count > 0 && ConditionEvaluator.AllPass(check.DisadvantageIf, state));
        return RollModes.Combine(advantage, disadvantage);
    }

    /// <summary>Display name for a check, e.g. "Persuasion" or "Strength".</summary>
    public string Label(CheckDef check)
    {
        ArgumentNullException.ThrowIfNull(check);
        return check.Skill is { } skill ? _content.Skills[skill].Name : check.Ability!.Value.DisplayName();
    }

    public CheckResult Run(CheckDef check, GameState state)
    {
        ArgumentNullException.ThrowIfNull(check);
        ArgumentNullException.ThrowIfNull(state);
        var mode = ModeFor(check, state);

        if (check.Skill is { } skillId)
        {
            var skill = _content.Skills[skillId];
            return _resolver.SkillCheck(state.Player, skill.Id, skill.Name, skill.Ability, check.Dc, mode);
        }

        return _resolver.AbilityCheck(state.Player, check.Ability!.Value, check.Dc, mode);
    }

    public CheckResult SavingThrow(ICheckSubject subject, Ability ability, int dc, RollMode mode = RollMode.Normal) =>
        _resolver.SavingThrow(subject, ability, dc, mode);
}
