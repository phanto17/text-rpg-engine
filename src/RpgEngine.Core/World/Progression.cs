using RpgEngine.Core.Characters;
using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Events;

namespace RpgEngine.Core.World;

/// <summary>
/// Experience and leveling. Every XP source (effects and combat victories) goes through
/// <see cref="GrantXp"/>, so level-ups happen in one place with one set of rules.
/// </summary>
public static class Progression
{
    /// <summary>Mana gained per level by classes that have mana.</summary>
    public const int CasterManaGain = 2;

    /// <summary>Hit points gained per level: half the hit die, plus one, plus the CON modifier (at least 1).</summary>
    public static int HitPointGain(ClassDef cls, AbilityScores scores)
    {
        ArgumentNullException.ThrowIfNull(cls);
        ArgumentNullException.ThrowIfNull(scores);
        return Math.Max(1, (cls.HitDie / 2) + 1 + scores.Modifier(Ability.Con));
    }

    /// <summary>Total XP needed for the next level, or null at the maximum level.</summary>
    public static int? NextLevelXp(PlayerCharacter player, GameContent content)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(content);
        var thresholds = content.Rules.XpThresholdsByLevel;
        return player.Level < thresholds.Count ? thresholds[player.Level] : null;
    }

    /// <summary>Adds XP, then levels up as many times as the new total allows (one event per level).</summary>
    public static void GrantXp(PlayerCharacter player, GameContent content, int amount, TurnLog log)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(log);

        player.AddXp(amount);
        log.Add(new XpGainedEvent(amount, player.Xp));

        // A class missing from content (only possible with hand-built characters) simply never levels.
        if (!content.Classes.TryGetValue(player.ClassId, out var cls))
        {
            return;
        }

        while (player.IsAlive && NextLevelXp(player, content) is { } needed && player.Xp >= needed)
        {
            var newLevel = player.Level + 1;
            var hpGain = HitPointGain(cls, player.Abilities);
            var manaGain = cls.Mana is null ? 0 : CasterManaGain;
            var newSpells = cls.SpellsByLevel
                .Where(grant => grant.Level == newLevel)
                .SelectMany(grant => grant.Spells)
                .Where(id => !player.SpellIds.Contains(id))
                .ToList();

            player.LevelUp(hpGain, manaGain, newSpells);
            log.Add(new LeveledUpEvent(
                player.Level,
                hpGain,
                player.MaxHp,
                manaGain,
                player.MaxMana,
                newSpells.Select(id => content.Spells[id]).ToList(),
                player.ProficiencyBonus));
        }
    }
}
