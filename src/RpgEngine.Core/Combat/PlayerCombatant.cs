using RpgEngine.Core.Characters;

namespace RpgEngine.Core.Combat;

/// <summary>The player in combat; HP changes go straight to the persistent <see cref="PlayerCharacter"/>.</summary>
public sealed class PlayerCombatant : Combatant
{
    private readonly int _armorClass;

    internal PlayerCombatant(PlayerCharacter character, int armorClass)
        : base("player", character.Name, Side.Party)
    {
        Character = character;
        _armorClass = armorClass;
    }

    public PlayerCharacter Character { get; }

    public override AbilityScores Abilities => Character.Abilities;

    public override int ProficiencyBonus => Character.ProficiencyBonus;

    public override int CurrentHp => Character.CurrentHp;

    public override int MaxHp => Character.MaxHp;

    public override int BaseArmorClass => _armorClass;

    public override ProficiencyLevel GetSkillProficiency(string skillId) => Character.GetSkillProficiency(skillId);

    public override bool IsProficientInSave(Ability ability) => Character.IsProficientInSave(ability);

    public override int ApplyDamage(int amount) => Character.TakeDamage(amount);

    public override int Heal(int amount) => Character.Heal(amount);
}
