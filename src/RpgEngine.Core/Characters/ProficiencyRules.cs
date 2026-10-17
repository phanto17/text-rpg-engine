namespace RpgEngine.Core.Characters;

public static class ProficiencyRules
{
    /// <summary>Looks up the proficiency bonus for a level; levels past the end of the table use its last entry.</summary>
    public static int BonusForLevel(int level, IReadOnlyList<int> bonusByLevel)
    {
        ArgumentNullException.ThrowIfNull(bonusByLevel);
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        if (bonusByLevel.Count == 0)
        {
            throw new ArgumentException("The proficiency table is empty.", nameof(bonusByLevel));
        }

        return bonusByLevel[Math.Min(level, bonusByLevel.Count) - 1];
    }

    public static int Bonus(ProficiencyLevel level, int proficiencyBonus) => level switch
    {
        ProficiencyLevel.Proficient => proficiencyBonus,
        ProficiencyLevel.Expertise => proficiencyBonus * 2,
        _ => 0,
    };
}
