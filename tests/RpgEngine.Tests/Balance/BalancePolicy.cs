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
/// <item>Heal: the first ready self-targeted spell or technique that heals, below 50% HP; then a
/// Potion of Healing below 40% HP while any remain.</item>
/// <item>Target: a minion if one plain attack likely kills it (average damage, after
/// vulnerabilities, at least its HP); otherwise the boss.</item>
/// <item>Attack: a carried Flask of Holy Water opens on the boss; then the castable mana spell with
/// the highest average damage against the target (after resistances and vulnerabilities; hit and
/// save odds are ignored); then the first ready per-fight technique aimed at an enemy, unless a plain
/// attack is already sure to kill (its minimum damage covers the target's HP); otherwise the plain
/// attack.</item>
/// </list>
/// The plain attack is the first free damaging spell the character knows, otherwise the equipped
/// weapon. The flask waits for the minion to fall because throwing it first hands the minion an extra
/// round against a fragile caster. The policy never defends, parleys, or flees.
/// </summary>
internal sealed class BalancePolicy
{
    public const string Flask = "holy_water";
    private const string Potion = "healing_potion";

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

        var heal = ready.FirstOrDefault(spell => spell.Resolution is EffectsResolutionDef effects && effects.Effects.Any(e => e is HealEffect));
        if (heal is not null && player.CurrentHp * 2 < player.MaxHp)
        {
            return new CastAction(heal.Id, null);
        }

        if (player.Inventory.Contains(Potion) && player.CurrentHp * 5 < player.MaxHp * 2)
        {
            return new UseItemAction(Potion);
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

        if (target == boss && player.Inventory.Contains(Flask))
        {
            return new UseItemAction(Flask, target.Id);
        }

        var manaSpell = ready
            .Where(spell => spell.Target == SpellTarget.Enemy && spell.Cost.Mana > 0)
            .Select(spell => (Spell: spell, Damage: Damage(spell.Resolution, session)))
            .Where(x => x.Damage is not null)
            .OrderByDescending(x => Adjusted(x.Damage!.Value.Expression.Average, x.Damage.Value.Type, target))
            .Select(x => x.Spell)
            .FirstOrDefault();
        if (manaSpell is not null)
        {
            return new CastAction(manaSpell.Id, target.Id);
        }

        var technique = ready.FirstOrDefault(spell => spell.Target == SpellTarget.Enemy && spell.Cost.UsesPerEncounter is not null);
        if (technique is not null && Adjusted(plainDamage.Min, plainType, target) < target.CurrentHp)
        {
            return new CastAction(technique.Id, target.Id);
        }

        return plainSpell is not null ? new CastAction(plainSpell.Id, target.Id) : new AttackAction(target.Id);
    }

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
