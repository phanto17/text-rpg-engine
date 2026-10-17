using RpgEngine.Core.Characters;

namespace RpgEngine.Tests.TestSupport;

internal static class TestCharacters
{
    public static readonly IReadOnlyList<int> ProficiencyTable = [2, 2, 2, 2, 3];

    public static AbilityScores Scores(int str = 10, int dex = 10, int con = 10, int @int = 10, int wis = 10, int cha = 10) =>
        new(str, dex, con, @int, wis, cha);

    public static PlayerCharacter Create(
        AbilityScores? abilities = null,
        IEnumerable<string>? skills = null,
        IEnumerable<string>? expertise = null,
        IEnumerable<Ability>? saves = null,
        IEnumerable<string>? spells = null,
        int maxHp = 10,
        int maxMana = 0,
        string classId = "fighter",
        string name = "Tester") =>
        new(
            name,
            classId,
            abilities ?? Scores(),
            maxHp,
            maxMana,
            skills ?? [],
            expertise ?? [],
            saves ?? [],
            spells ?? [],
            ProficiencyTable);
}
