using RpgEngine.Core.Combat;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Balance;

public sealed class BalancePolicyTests
{
    private static PlayerAction Choose(IReadOnlyList<string> spells, int damageTaken)
    {
        var combat = new CombatHarness(CombatFixture.Hero(spells: spells), dice: [15, 2]);
        combat.World.Player.TakeDamage(damageTaken);
        combat.Begin("brute");
        return new BalancePolicy(combat.World.Content, "brute").Choose(combat.Session);
    }

    [Fact]
    public void Choose_BelowHalfHp_UsesTheStrongestReadyHeal_EvenWhenListedLater()
    {
        // Rally heals a flat 3 and wards; Second Wind heals 1d10+1 (6.5 on average).
        Assert.Equal(new CastAction("second_wind", null), Choose(["rally", "second_wind"], damageTaken: 12));
    }

    [Fact]
    public void Choose_BelowHalfHp_UsesAHealThatAlsoBuffs()
    {
        Assert.Equal(new CastAction("rally", null), Choose(["rally"], damageTaken: 12));
    }

    [Fact]
    public void Choose_AtHalfHpOrMore_AttacksInstead()
    {
        Assert.Equal(new AttackAction("brute#1"), Choose(["rally", "second_wind"], damageTaken: 10));
    }
}
