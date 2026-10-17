using RpgEngine.Core.Characters;
using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;

namespace RpgEngine.Core.Combat;

/// <summary>Derived combat numbers for the player, shared by the engine and the character sheet.</summary>
public static class CombatRules
{
    public const int UnarmoredBase = 10;

    /// <summary>Armor's base AC + DEX modifier (capped by the armor), or 10 + DEX when unarmored.</summary>
    public static int ArmorClass(PlayerCharacter player, GameContent content)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(content);
        var dex = player.Abilities.Modifier(Ability.Dex);

        if (player.EquippedArmorId is { } armorId && content.Items.TryGetValue(armorId, out var item) && item.Armor is { } armor)
        {
            return armor.BaseAc + (armor.MaxDexBonus is { } cap ? Math.Min(dex, cap) : dex);
        }

        return UnarmoredBase + dex;
    }

    /// <summary>
    /// The equipped weapon (or an unarmed strike): ranged weapons use DEX, finesse weapons use the
    /// better of STR and DEX, everything else uses STR. To-hit adds the proficiency bonus.
    /// </summary>
    public static WeaponProfile Weapon(PlayerCharacter player, GameContent content)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(content);

        var weaponItem = player.EquippedWeaponId is { } weaponId && content.Items.TryGetValue(weaponId, out var item) && item.Weapon is not null
            ? item
            : null;

        var weapon = weaponItem?.Weapon;
        var ability = weapon switch
        {
            { Ranged: true } => Ability.Dex,
            { Finesse: true } => player.Abilities.Modifier(Ability.Dex) > player.Abilities.Modifier(Ability.Str) ? Ability.Dex : Ability.Str,
            _ => Ability.Str,
        };

        var modifier = player.Abilities.Modifier(ability);
        var damage = DiceExpression.Parse(weapon?.Damage ?? "1").Plus(modifier);
        return new WeaponProfile(weaponItem?.Name ?? "Unarmed strike", ability, player.ProficiencyBonus + modifier, damage, weapon?.DamageType ?? DamageType.Bludgeoning);
    }

    /// <summary>The ability used for spells: the class's mana ability, or INT if the class defines none.</summary>
    public static Ability CastingAbility(PlayerCharacter player, GameContent content)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(content);
        return content.Classes.TryGetValue(player.ClassId, out var cls) && cls.Mana is { } mana ? mana.Ability : Ability.Int;
    }

    /// <summary>Uses per encounter for every technique the player knows.</summary>
    public static IReadOnlyDictionary<string, int> EncounterUses(PlayerCharacter player, GameContent content)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(content);
        return player.SpellIds
            .Where(content.Spells.ContainsKey)
            .Select(id => content.Spells[id])
            .Where(spell => spell.Cost.UsesPerEncounter is not null)
            .ToDictionary(spell => spell.Id, spell => spell.Cost.UsesPerEncounter!.Value, StringComparer.Ordinal);
    }
}
