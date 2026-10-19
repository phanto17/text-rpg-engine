using RpgEngine.Core.Characters;
using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;

namespace RpgEngine.Core.Engine;

/// <summary>Builds level-1 characters from class data: ability scores, HP, mana, skills, gold, and starting gear.</summary>
public sealed class CharacterFactory
{
    public const int MaxNameLength = 24;
    private static readonly DiceExpression AbilityRoll = DiceExpression.Parse("4d6kh3");

    private readonly GameContent _content;

    public CharacterFactory(GameContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        _content = content;
    }

    public IReadOnlyList<int> StandardArray => _content.Rules.StandardArray;

    /// <summary>Rolls six ability scores, each 4d6 keeping the highest three.</summary>
    public IReadOnlyList<RollResult> RollAbilityScores(DiceRoller dice)
    {
        ArgumentNullException.ThrowIfNull(dice);
        return Enumerable.Range(0, 6).Select(_ => dice.Roll(AbilityRoll)).ToList();
    }

    /// <summary>Assigns the highest value to the class's first-priority ability, and so on.</summary>
    public static AbilityScores AssignByPriority(IReadOnlyList<int> values, ClassDef cls)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(cls);
        if (values.Count != 6)
        {
            throw new ArgumentException("Exactly six ability scores are required.", nameof(values));
        }

        var sorted = values.OrderByDescending(v => v).ToList();
        var map = cls.AbilityPriority.Select((ability, index) => (ability, score: sorted[index])).ToDictionary(x => x.ability, x => x.score);
        return AbilityScores.FromMap(map);
    }

    public static int MaxHp(ClassDef cls, AbilityScores scores)
    {
        ArgumentNullException.ThrowIfNull(cls);
        ArgumentNullException.ThrowIfNull(scores);
        return Math.Max(1, cls.HitDie + scores.Modifier(Ability.Con));
    }

    public static int MaxMana(ClassDef cls, AbilityScores scores)
    {
        ArgumentNullException.ThrowIfNull(cls);
        ArgumentNullException.ThrowIfNull(scores);
        return cls.Mana is { } mana ? Math.Max(0, mana.Base + scores.Modifier(mana.Ability)) : 0;
    }

    /// <summary>
    /// Builds a level 1 character. <paramref name="skills"/> are the class picks only: the
    /// background's bonus skill is granted on top and may not be picked again.
    /// </summary>
    public PlayerCharacter Create(string name, string classId, string backgroundId, AbilityScores scores, IReadOnlyCollection<string> skills, IReadOnlyCollection<string> expertise)
    {
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(skills);
        ArgumentNullException.ThrowIfNull(expertise);

        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > MaxNameLength)
        {
            throw new ArgumentException($"A name must be 1 to {MaxNameLength} characters.", nameof(name));
        }

        if (!_content.Classes.TryGetValue(classId, out var cls))
        {
            throw new ArgumentException($"Unknown class '{classId}'.", nameof(classId));
        }

        if (!_content.Backgrounds.TryGetValue(backgroundId ?? string.Empty, out var background))
        {
            throw new ArgumentException($"Unknown background '{backgroundId}'.", nameof(backgroundId));
        }

        var choices = cls.SkillChoices;
        if (skills.Count != choices.Count || skills.Distinct(StringComparer.Ordinal).Count() != skills.Count || !skills.All(choices.From.Contains))
        {
            throw new ArgumentException($"Choose exactly {choices.Count} different skills from the {cls.Name} list.", nameof(skills));
        }

        if (skills.Contains(background.BonusSkill))
        {
            throw new ArgumentException($"Your {background.Name} background already grants that skill; choose another.", nameof(skills));
        }

        if (expertise.Count != cls.ExpertiseCount || expertise.Distinct(StringComparer.Ordinal).Count() != expertise.Count || !expertise.All(skills.Contains))
        {
            throw new ArgumentException($"Choose exactly {cls.ExpertiseCount} of your chosen skills for expertise.", nameof(expertise));
        }

        var player = new PlayerCharacter(
            trimmed,
            cls.Id,
            background.Id,
            scores,
            MaxHp(cls, scores),
            MaxMana(cls, scores),
            skills.Append(background.BonusSkill),
            expertise,
            cls.SavingThrows,
            cls.Spells,
            _content.Rules.ProficiencyBonusByLevel);

        player.AddGold(_content.Manifest.StartingGold + background.BonusGold);
        foreach (var starting in cls.StartingItems)
        {
            player.Inventory.Add(starting.Item, starting.Count);
            if (starting.Equip)
            {
                var item = _content.Items[starting.Item];
                if (item.Type == ItemType.Weapon)
                {
                    player.EquippedWeaponId = item.Id;
                }
                else
                {
                    player.EquippedArmorId = item.Id;
                }
            }
        }

        // Last, so combat item menus keep listing the class's consumables first.
        player.Inventory.Add(background.KeepsakeItem);
        return player;
    }
}
