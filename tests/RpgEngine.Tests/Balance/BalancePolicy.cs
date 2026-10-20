using RpgEngine.Core.Combat;
using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;
using RpgEngine.Core.World;

namespace RpgEngine.Tests.Balance;

/// <summary>
/// The plain, unclever player the balance check assumes. It reads spells by what they do, never by
/// id, so any class plays the same way. Each turn takes the first rule that applies:
/// <list type="number">
/// <item>Heal: below 50% HP, the ready self-targeted spell or technique that heals the most on
/// average (the first listed on a tie), along with any status it applies; then, below 40% HP, the
/// carried healing item that heals the most on average (an ally blessing, which also applies a
/// status, before a Potion of Healing on a tie).</item>
/// <item>Target: a minion if one plain attack likely kills it (average damage, after
/// vulnerabilities, at least its HP); otherwise the boss.</item>
/// <item>Attack: the castable mana spell or, against the boss, the carried item thrown at an enemy
/// (the Flask of Holy Water, Vaelen's Silver Chime) with the highest average damage against the
/// target (after resistances and vulnerabilities; hit and save odds are ignored; an item wins a
/// tie); then the first ready per-fight technique aimed at an enemy, unless a plain attack is already
/// sure to kill (its minimum damage covers the target's HP); otherwise the plain attack.</item>
/// </list>
/// The plain attack is the first free damaging spell the character knows, otherwise the equipped
/// weapon. Thrown items wait for the minion to fall because throwing one first hands the minion an
/// extra round against a fragile caster. The policy never defends, parleys, or flees.
/// </summary>
internal sealed class BalancePolicy
{
    public const string Flask = "holy_water";

    private readonly GameContent _content;
    private readonly string _bossId;

    public BalancePolicy(GameContent content, string bossId)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(bossId);
        _content = content;
        _bossId = bossId;
    }

    public PlayerAction Choose(CombatSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var player = session.Player.Character;
        var ready = session.SpellOptions().Where(option => option.CanCast).Select(option => option.Spell).ToList();

        var heal = ready
            .Where(spell => spell.Target == SpellTarget.Self)
            .Select(spell => (Spell: spell, Average: AverageHealing(spell.Resolution)))
            .Where(x => x.Average > 0)
            .OrderByDescending(x => x.Average)
            .Select(x => x.Spell)
            .FirstOrDefault();
        if (heal is not null && player.CurrentHp * 2 < player.MaxHp)
        {
            return new CastAction(heal.Id, null);
        }

        var carried = player.Inventory.Entries.Select(entry => _content.Items[entry.ItemId]).Where(item => item.Use is { InCombat: true }).ToList();
        var healingItem = carried
            .Where(item => item.Use!.Target == SpellTarget.Self)
            .Select(item => (Item: item, Average: AverageHealing(item.Use!.Effects)))
            .Where(x => x.Average > 0)
            .OrderByDescending(x => x.Average)
            .ThenByDescending(x => x.Item.Use!.Effects.OfType<ApplyStatusEffect>().Any())
            .Select(x => x.Item)
            .FirstOrDefault();
        if (healingItem is not null && player.CurrentHp * 5 < player.MaxHp * 2)
        {
            return new UseItemAction(healingItem.Id);
        }

        var enemies = session.ActiveEnemies;
        var boss = enemies.OfType<MonsterCombatant>().FirstOrDefault(m => m.Definition.Id == _bossId);
        var (plainSpell, plainDamage, plainType) = PlainAttack(session, ready);
        var target = enemies
            .Where(e => e != boss && Adjusted(plainDamage.Average, plainType, e) >= e.CurrentHp)
            .OrderBy(e => e.CurrentHp)
            .FirstOrDefault()
            ?? boss
            ?? enemies.OrderBy(e => e.CurrentHp).First();

        var thrown = carried
            .Where(item => target == boss && item.Use!.Target == SpellTarget.Enemy && item.Use.Resolution is not null)
            .Select(item => (Action: (PlayerAction)new UseItemAction(item.Id, target.Id), Damage: Damage(item.Use!.Resolution!, session)));
        var cast = ready
            .Where(spell => spell.Target == SpellTarget.Enemy && spell.Cost.Mana > 0)
            .Select(spell => (Action: (PlayerAction)new CastAction(spell.Id, target.Id), Damage: Damage(spell.Resolution, session)));
        var strongest = thrown.Concat(cast)
            .Where(x => x.Damage is not null)
            .OrderByDescending(x => Adjusted(x.Damage!.Value.Expression.Average, x.Damage.Value.Type, target))
            .Select(x => x.Action)
            .FirstOrDefault();
        if (strongest is not null)
        {
            return strongest;
        }

        var technique = ready.FirstOrDefault(spell => spell.Target == SpellTarget.Enemy && spell.Cost.UsesPerEncounter is not null);
        if (technique is not null && Adjusted(plainDamage.Min, plainType, target) < target.CurrentHp)
        {
            return new CastAction(technique.Id, target.Id);
        }

        return plainSpell is not null ? new CastAction(plainSpell.Id, target.Id) : new AttackAction(target.Id);
    }

    // Self-buffs ride along with their healing; a spell that only applies a status never heals here.
    private static double AverageHealing(SpellResolutionDef resolution) =>
        resolution is EffectsResolutionDef effects ? AverageHealing(effects.Effects) : 0;

    private static double AverageHealing(IEnumerable<EffectDef> effects) =>
        effects.OfType<HealEffect>().Sum(heal => DiceExpression.Parse(heal.Dice).Average);

    // The first free damaging spell the character knows (e.g. a cantrip), otherwise the weapon.
    private (SpellDef? Spell, DiceExpression Damage, DamageType Type) PlainAttack(CombatSession session, IReadOnlyList<SpellDef> ready)
    {
        foreach (var spell in ready.Where(s => s.Target == SpellTarget.Enemy && s.Cost.Mana == 0 && s.Cost.UsesPerEncounter is null))
        {
            if (Damage(spell.Resolution, session) is { } damage)
            {
                return (spell, damage.Expression, damage.Type);
            }
        }

        var weapon = CombatRules.Weapon(session.Player.Character, _content);
        return (null, weapon.Damage, weapon.DamageType);
    }

    private (DiceExpression Expression, DamageType Type)? Damage(SpellResolutionDef resolution, CombatSession session) => resolution switch
    {
        AutoHitResolutionDef autoHit => (DiceExpression.Parse(autoHit.Damage), autoHit.DamageType),
        SpellAttackResolutionDef attack => (DiceExpression.Parse(attack.Damage), attack.DamageType),
        SaveResolutionDef save => (DiceExpression.Parse(save.Damage), save.DamageType),
        WeaponAttackResolutionDef weaponAttack => WeaponDamage(weaponAttack, session),
        _ => null,
    };

    private (DiceExpression Expression, DamageType Type) WeaponDamage(WeaponAttackResolutionDef resolution, CombatSession session)
    {
        var weapon = CombatRules.Weapon(session.Player.Character, _content);
        var damage = resolution.BonusDamage is { } bonus ? weapon.Damage.Plus(DiceExpression.Parse(bonus)) : weapon.Damage;
        return (damage, weapon.DamageType);
    }

    // Mirrors the engine's damage adjustment: immunity, else resistance (halved, rounded down), else vulnerability.
    private static double Adjusted(double amount, DamageType type, Combatant target) =>
        target.Immunities.Contains(type) ? 0
        : target.Resistances.Contains(type) ? Math.Floor(amount / 2)
        : target.Vulnerabilities.Contains(type) ? amount * 2
        : amount;
}
