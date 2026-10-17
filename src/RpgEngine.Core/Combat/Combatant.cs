using RpgEngine.Core.Characters;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;

namespace RpgEngine.Core.Combat;

/// <summary>A participant in a fight. Combat-only state (statuses, initiative, fleeing) lives here.</summary>
public abstract class Combatant : ICheckSubject
{
    private readonly List<ActiveStatus> _statuses = [];

    protected Combatant(string id, string name, Side side)
    {
        Id = id;
        Name = name;
        Side = side;
    }

    public string Id { get; }

    public string Name { get; }

    public Side Side { get; }

    public abstract AbilityScores Abilities { get; }

    public abstract int ProficiencyBonus { get; }

    public abstract int CurrentHp { get; }

    public abstract int MaxHp { get; }

    /// <summary>Armor class before status bonuses.</summary>
    public abstract int BaseArmorClass { get; }

    public virtual IReadOnlyList<DamageType> Resistances => [];

    public virtual IReadOnlyList<DamageType> Vulnerabilities => [];

    public virtual IReadOnlyList<DamageType> Immunities => [];

    public int ArmorClass => BaseArmorClass + _statuses.Sum(status => status.Definition.Modifiers.AcBonus);

    public IReadOnlyList<ActiveStatus> Statuses => _statuses;

    public int Initiative { get; internal set; }

    public bool HasFled { get; internal set; }

    public bool IsAlive => CurrentHp > 0;

    /// <summary>Still fighting: alive and has not fled.</summary>
    public bool IsActive => IsAlive && !HasFled;

    /// <summary>Advantage and disadvantage on attack rolls from statuses (they cancel out).</summary>
    public RollMode AttackRollMode => RollModes.Combine(
        _statuses.Any(status => status.Definition.Modifiers.AttackRolls == AttackRollModifier.Advantage),
        _statuses.Any(status => status.Definition.Modifiers.AttackRolls == AttackRollModifier.Disadvantage));

    public abstract ProficiencyLevel GetSkillProficiency(string skillId);

    public abstract bool IsProficientInSave(Ability ability);

    /// <summary>Returns the HP actually lost.</summary>
    public abstract int ApplyDamage(int amount);

    /// <summary>Returns the HP actually gained.</summary>
    public abstract int Heal(int amount);

    internal void AddOrRefreshStatus(StatusEffectDef definition, bool duringOwnTurn)
    {
        _statuses.RemoveAll(status => status.Definition.Id == definition.Id);
        _statuses.Add(new ActiveStatus(definition, duringOwnTurn));
    }

    internal void RemoveStatus(ActiveStatus status) => _statuses.Remove(status);
}
