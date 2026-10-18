using RpgEngine.Core.Combat;
using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;

namespace RpgEngine.Tests.Balance;

/// <summary>
/// The plain, unclever player the balance check assumes. Each turn takes the first rule that applies:
/// <list type="number">
/// <item>Heal: Second Wind below 50% HP while unused, then a Potion of Healing below 40% HP while any remain.</item>
/// <item>Target: the thrall if one plain hit likely kills it (average damage, after vulnerabilities, at
/// least its HP); otherwise the knight.</item>
/// <item>Attack: a carried Flask of Holy Water opens on the knight; then Magic Missile while MP allows;
/// Sneak Attack while charges remain, unless a plain hit is already sure to kill (its minimum damage
/// covers the target's HP); otherwise the plain attack.</item>
/// </list>
/// The plain attack is Fire Bolt when known, otherwise the equipped weapon. The flask waits for the
/// thrall to fall because throwing it first hands the thrall an extra round against a fragile wizard.
/// The policy never defends, parleys, or flees.
/// </summary>
internal sealed class BalancePolicy
{
    public const string Knight = "gravebound_knight";
    public const string Flask = "holy_water";
    private const string Potion = "healing_potion";
    private const string SecondWind = "second_wind";
    private const string SneakAttack = "sneak_attack";
    private const string MagicMissile = "magic_missile";
    private const string FireBolt = "fire_bolt";

    private readonly GameContent _content;

    public BalancePolicy(GameContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        _content = content;
    }

    public PlayerAction Choose(CombatSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var player = session.Player.Character;

        if (CanCast(session, SecondWind) && player.CurrentHp * 2 < player.MaxHp)
        {
            return new CastAction(SecondWind, null);
        }

        if (player.Inventory.Contains(Potion) && player.CurrentHp * 5 < player.MaxHp * 2)
        {
            return new UseItemAction(Potion);
        }

        var enemies = session.ActiveEnemies;
        var knight = enemies.OfType<MonsterCombatant>().FirstOrDefault(m => m.Definition.Id == Knight);
        var (damage, damageType) = PlainAttack(session);
        var target = enemies
            .Where(e => e != knight && Adjusted(damage.Average, damageType, e) >= e.CurrentHp)
            .OrderBy(e => e.CurrentHp)
            .FirstOrDefault()
            ?? knight
            ?? enemies.OrderBy(e => e.CurrentHp).First();

        if (target == knight && player.Inventory.Contains(Flask))
        {
            return new UseItemAction(Flask, target.Id);
        }

        if (CanCast(session, MagicMissile))
        {
            return new CastAction(MagicMissile, target.Id);
        }

        if (CanCast(session, SneakAttack) && Adjusted(damage.Min, damageType, target) < target.CurrentHp)
        {
            return new CastAction(SneakAttack, target.Id);
        }

        return CanCast(session, FireBolt) ? new CastAction(FireBolt, target.Id) : new AttackAction(target.Id);
    }

    private static bool CanCast(CombatSession session, string spellId) =>
        session.SpellOptions().Any(option => option.Spell.Id == spellId && option.CanCast);

    private (DiceExpression Damage, DamageType Type) PlainAttack(CombatSession session)
    {
        if (CanCast(session, FireBolt) && _content.Spells[FireBolt].Resolution is SpellAttackResolutionDef bolt)
        {
            return (DiceExpression.Parse(bolt.Damage), bolt.DamageType);
        }

        var weapon = CombatRules.Weapon(session.Player.Character, _content);
        return (weapon.Damage, weapon.DamageType);
    }

    // Mirrors the engine's damage adjustment: immunity, else resistance (halved, rounded down), else vulnerability.
    private static double Adjusted(double amount, DamageType type, Combatant target) =>
        target.Immunities.Contains(type) ? 0
        : target.Resistances.Contains(type) ? Math.Floor(amount / 2)
        : target.Vulnerabilities.Contains(type) ? amount * 2
        : amount;
}
