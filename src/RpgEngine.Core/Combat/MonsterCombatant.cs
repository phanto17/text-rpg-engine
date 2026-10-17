using RpgEngine.Core.Characters;
using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Core.Combat;

/// <summary>A monster instance; it has no skill or save proficiencies, and its attack bonuses come from the bestiary.</summary>
public sealed class MonsterCombatant : Combatant
{
    private int _hp;

    internal MonsterCombatant(string id, string name, MonsterDef definition, int maxHp)
        : base(id, name, Side.Enemies)
    {
        Definition = definition;
        Abilities = definition.Abilities.ToScores();
        MaxHp = maxHp;
        _hp = maxHp;
    }

    public MonsterDef Definition { get; }

    public override AbilityScores Abilities { get; }

    public override int ProficiencyBonus => 0;

    public override int CurrentHp => _hp;

    public override int MaxHp { get; }

    public override int BaseArmorClass => Definition.Ac;

    public override IReadOnlyList<DamageType> Resistances => Definition.Resistances;

    public override IReadOnlyList<DamageType> Vulnerabilities => Definition.Vulnerabilities;

    public override IReadOnlyList<DamageType> Immunities => Definition.Immunities;

    public override ProficiencyLevel GetSkillProficiency(string skillId) => ProficiencyLevel.None;

    public override bool IsProficientInSave(Ability ability) => false;

    public override int ApplyDamage(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var before = _hp;
        _hp = Math.Max(0, _hp - amount);
        return before - _hp;
    }

    public override int Heal(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var before = _hp;
        _hp = Math.Min(MaxHp, _hp + amount);
        return _hp - before;
    }
}
