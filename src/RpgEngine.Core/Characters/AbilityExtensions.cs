namespace RpgEngine.Core.Characters;

public static class AbilityExtensions
{
    public static IReadOnlyList<Ability> All { get; } = [Ability.Str, Ability.Dex, Ability.Con, Ability.Int, Ability.Wis, Ability.Cha];

    public static string DisplayName(this Ability ability) => ability switch
    {
        Ability.Str => "Strength",
        Ability.Dex => "Dexterity",
        Ability.Con => "Constitution",
        Ability.Int => "Intelligence",
        Ability.Wis => "Wisdom",
        Ability.Cha => "Charisma",
        _ => throw new ArgumentOutOfRangeException(nameof(ability), ability, null),
    };

    public static string Abbreviation(this Ability ability) => ability switch
    {
        Ability.Str => "STR",
        Ability.Dex => "DEX",
        Ability.Con => "CON",
        Ability.Int => "INT",
        Ability.Wis => "WIS",
        Ability.Cha => "CHA",
        _ => throw new ArgumentOutOfRangeException(nameof(ability), ability, null),
    };
}
