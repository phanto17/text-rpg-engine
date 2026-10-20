using RpgEngine.Core.Combat;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Balance;

public sealed class BalancePolicyTests
{
    // Two healing items next to the fixture's Potion of Healing (2d4+2): a blessing that heals as
    // much and also wards, and a weaker one that wards.
    private const string Blessings = """
        [
          { "id": "censer", "name": "Censer", "type": "consumable", "description": "Smoke.",
            "use": { "inCombat": true, "effects": [ { "type": "heal", "dice": "2d4+2" }, { "type": "applyStatus", "status": "warded" } ] } },
          { "id": "charm", "name": "Charm", "type": "consumable", "description": "Faint.",
            "use": { "inCombat": true, "effects": [ { "type": "heal", "dice": "1d4" }, { "type": "applyStatus", "status": "warded" } ] } }
        ]
        """;

    private static PlayerAction Choose(IReadOnlyList<string> spells, int damageTaken, params string[] items)
    {
        var content = CombatFixture.Content().With("Items/blessings.json", Blessings).Load();
        var combat = new CombatHarness(CombatFixture.Hero(spells: spells), content, dice: [15, 2]);
        combat.World.Player.TakeDamage(damageTaken);
        foreach (var item in items)
        {
            combat.World.Player.Inventory.Add(item);
        }

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

    [Theory]
    [InlineData("potion", "censer", "censer")]
    [InlineData("charm", "potion", "potion")]
    public void Choose_BelowFortyPercentHp_DrinksTheStrongestHealingItem_ABlessingWinningATie(string first, string second, string expected)
    {
        Assert.Equal(new UseItemAction(expected), Choose([], damageTaken: 13, first, second));
    }

    [Theory]
    [InlineData("missile", "cast")]
    [InlineData("rally", "throw")]
    public void Choose_AgainstTheBoss_ThrowsAnItemOnlyWhenNoSpellHitsHarder(string spell, string expected)
    {
        // Magic Missile (3d4+3, 10.5 on average) outdamages the firebomb (2d4, 5).
        PlayerAction action = expected == "cast" ? new CastAction("missile", "brute#1") : new UseItemAction("firebomb", "brute#1");

        Assert.Equal(action, Choose([spell], damageTaken: 0, "firebomb"));
    }
}
