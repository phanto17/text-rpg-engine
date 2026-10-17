namespace RpgEngine.Core.Characters;

/// <summary>Anything that can make ability checks, skill checks, and saving throws.</summary>
public interface ICheckSubject
{
    AbilityScores Abilities { get; }

    int ProficiencyBonus { get; }

    ProficiencyLevel GetSkillProficiency(string skillId);

    bool IsProficientInSave(Ability ability);
}
