using RpgEngine.Core.Characters;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Events;
using RpgEngine.Core.World;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.World;

public sealed class EffectApplierTests
{
    private static (TestWorld Game, TurnLog Log) Run(IReadOnlyList<EffectDef> effects, PlayerCharacter? player = null, params int[] dice)
    {
        var game = new TestWorld(player: player, dice: dice);
        var log = new TurnLog();
        game.Effects.Apply(effects, game.State, log);
        return (game, log);
    }

    [Fact]
    public void FlagsAndCounters_ChangeWorldState()
    {
        var (game, log) = Run(
        [
            new SetFlagEffect { Flag = "a" },
            new SetFlagEffect { Flag = "b" },
            new ClearFlagEffect { Flag = "b" },
            new SetCounterEffect { Counter = "c", Value = 5 },
            new AddCounterEffect { Counter = "c", Amount = -2 },
        ]);

        Assert.True(game.World.HasFlag("a"));
        Assert.False(game.World.HasFlag("b"));
        Assert.Equal(3, game.World.GetCounter("c"));
        Assert.Empty(log.Events);
    }

    [Fact]
    public void ChangeReputation_EmitsEventWithNewValue()
    {
        var (game, log) = Run([new ChangeReputationEffect { Faction = "order", Amount = 10 }]);

        var change = Assert.IsType<ReputationChangedEvent>(Assert.Single(log.Events));
        Assert.Equal(("order", 10, 10), (change.Faction.Id, change.Delta, change.Value));
        Assert.Equal(10, game.World.GetReputation("order"));
    }

    [Fact]
    public void SetQuestStage_EmitsEventOnlyWhenAdvancing()
    {
        var (game, log) = Run(
        [
            new SetQuestStageEffect { Quest = "main", Stage = 100 },
            new SetQuestStageEffect { Quest = "main", Stage = 10 },
        ]);

        var update = Assert.IsType<QuestUpdatedEvent>(Assert.Single(log.Events));
        Assert.Equal("Done.", update.Stage.Journal);
        Assert.Equal(100, game.World.GetQuestStage("main"));
    }

    [Fact]
    public void GiveAndRemoveItems_UpdateInventory()
    {
        var (game, log) = Run(
        [
            new GiveItemEffect { Item = "potion", Count = 3 },
            new RemoveItemEffect { Item = "potion", Count = 2 },
            new RemoveItemEffect { Item = "key" },
        ]);

        Assert.Equal(1, game.Player.Inventory.CountOf("potion"));
        Assert.Equal(3, Assert.IsType<ItemGainedEvent>(log.Events[0]).Count);
        Assert.Equal(2, Assert.IsType<ItemLostEvent>(log.Events[1]).Count);
        Assert.Equal(2, log.Events.Count);
    }

    [Fact]
    public void RemoveItem_MoreThanCarried_RemovesAllAndUnequips()
    {
        var player = TestCharacters.Create();
        player.Inventory.Add("sword");
        player.Inventory.Add("mail");
        player.EquippedWeaponId = "sword";
        player.EquippedArmorId = "mail";

        var (_, log) = Run(
        [
            new RemoveItemEffect { Item = "sword", Count = 5 },
            new RemoveItemEffect { Item = "mail" },
        ], player);

        Assert.Null(player.EquippedWeaponId);
        Assert.Null(player.EquippedArmorId);
        Assert.Equal(1, Assert.IsType<ItemLostEvent>(log.Events[0]).Count);
        Assert.True(player.Inventory.IsEmpty);
    }

    [Fact]
    public void RemoveItem_PartialStack_KeepsEquipment()
    {
        var player = TestCharacters.Create();
        player.Inventory.Add("dagger", 2);
        player.EquippedWeaponId = "dagger";

        Run([new RemoveItemEffect { Item = "dagger" }], player);

        Assert.Equal("dagger", player.EquippedWeaponId);
    }

    [Fact]
    public void GoldAndXp_EmitTotals()
    {
        var (_, log) = Run([new GiveGoldEffect { Amount = 7 }, new GrantXpEffect { Amount = 50 }]);

        Assert.Equal(new GoldGainedEvent(7, 7), log.Events[0]);
        Assert.Equal(new XpGainedEvent(50, 50), log.Events[1]);
    }

    [Fact]
    public void Rest_RestoresHpAndManaToMax()
    {
        var player = TestCharacters.Create(maxHp: 12, maxMana: 6);
        player.TakeDamage(9);
        player.TrySpendMana(4);

        var (_, log) = Run([new RestEffect()], player);

        Assert.Equal(new RestedEvent(9, 4, 12, 12, 6, 6), Assert.Single(log.Events));
        Assert.Equal((12, 6), (player.CurrentHp, player.CurrentMana));
    }

    [Fact]
    public void Rest_AtFullStrength_RestoresNothing()
    {
        var (_, log) = Run([new RestEffect()], TestCharacters.Create(maxHp: 8));

        Assert.Equal(new RestedEvent(0, 0, 8, 8, 0, 0), Assert.Single(log.Events));
    }

    [Fact]
    public void Heal_RollsDiceAndClampsToMax()
    {
        var player = TestCharacters.Create(maxHp: 10);
        player.TakeDamage(5);

        var (_, log) = Run([new HealEffect { Dice = "2d4+2" }], player, 4, 3);

        Assert.Equal(9, Assert.IsType<DiceRolledEvent>(log.Events[0]).Roll.Total);
        var healed = Assert.IsType<HealedEvent>(log.Events[1]);
        Assert.Equal((5, 10, true), (healed.Amount, healed.Hp, healed.IsPlayer));
    }

    [Fact]
    public void Damage_RollsDiceAndReportsLoss()
    {
        var player = TestCharacters.Create(maxHp: 10);

        var (_, log) = Run([new DamageEffect { Dice = "1d4", DamageType = DamageType.Piercing }], player, 3);

        var damage = Assert.IsType<DamageTakenEvent>(log.Events[1]);
        Assert.Equal((3, 7, DamageType.Piercing), (damage.Amount, damage.Hp, damage.DamageType));
        Assert.Equal(7, player.CurrentHp);
    }

    [Fact]
    public void RestoreMana_ReportsAmountRestored()
    {
        var player = TestCharacters.Create(maxMana: 6);
        player.TrySpendMana(4);

        var (_, log) = Run([new RestoreManaEffect { Amount = 10 }], player);

        Assert.Equal(new ManaChangedEvent(4, 6, 6), Assert.Single(log.Events));
    }

    [Theory]
    [InlineData(15, "saved")]
    [InlineData(2, "failed")]
    public void SavingThrow_BranchesOnResult(int face, string expectedFlag)
    {
        var (game, log) = Run(
        [
            new SavingThrowEffect
            {
                Ability = Ability.Dex,
                Dc = 12,
                OnSuccess = [new SetFlagEffect { Flag = "saved" }],
                OnFail = [new SetFlagEffect { Flag = "failed" }],
            },
        ], dice: [face]);

        Assert.IsType<CheckRolledEvent>(Assert.Single(log.Events));
        Assert.True(game.World.HasFlag(expectedFlag));
    }

    [Theory]
    [InlineData(14, "passed")]
    [InlineData(5, "failed")]
    public void SkillCheck_BranchesOnResult(int face, string expectedFlag)
    {
        var (game, _) = Run(
        [
            new SkillCheckEffect
            {
                Check = new CheckDef { Skill = "perception", Dc = 12 },
                OnSuccess = [new SetFlagEffect { Flag = "passed" }],
                OnFail = [new SetFlagEffect { Flag = "failed" }],
            },
        ], dice: [face]);

        Assert.True(game.World.HasFlag(expectedFlag));
    }

    [Fact]
    public void Message_FormatsPlayerTokens()
    {
        var player = TestCharacters.Create(name: "Aria", classId: "wizard");

        var (_, log) = Run([new MessageEffect { Text = "Hail, {player.name} the {player.class}.", Style = TextStyle.Sensory }], player);

        Assert.Equal(new NarrationEvent(TextStyle.Sensory, "Hail, Aria the Wizard."), Assert.Single(log.Events));
    }

    [Fact]
    public void ModeChanges_AreQueuedInOrder()
    {
        var (_, log) = Run(
        [
            new MoveToEffect { Room = "hall" },
            new StartDialogueEffect { Dialogue = "sage" },
            new StartCombatEffect { Encounter = "rats" },
            new EndGameEffect(),
        ]);

        var transitions = new List<GameTransition>();
        while (log.TryTakeTransition(out var transition))
        {
            transitions.Add(transition);
        }

        Assert.Equal(
            new GameTransition[] { new MoveToTransition("hall"), new StartDialogueTransition("sage"), new StartCombatTransition("rats"), new EndGameTransition() },
            transitions);
    }

    [Fact]
    public void If_RunsThenOrElse()
    {
        var (game, _) = Run(
        [
            new SetFlagEffect { Flag = "lit" },
            new IfEffect { Conditions = [new FlagCondition { Flag = "lit" }], Then = [new SetFlagEffect { Flag = "then" }], Else = [new SetFlagEffect { Flag = "else" }] },
            new IfEffect { Conditions = [new FlagCondition { Flag = "dark" }], Then = [new SetFlagEffect { Flag = "then2" }], Else = [new SetFlagEffect { Flag = "else2" }] },
        ]);

        Assert.True(game.World.HasFlag("then"));
        Assert.False(game.World.HasFlag("else"));
        Assert.True(game.World.HasFlag("else2"));
        Assert.False(game.World.HasFlag("then2"));
    }

    [Fact]
    public void UnknownEffectType_Throws()
    {
        Assert.Throws<NotSupportedException>(() => Run([new UnknownEffect()]));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var game = new TestWorld();

        Assert.Throws<ArgumentNullException>(() => game.Effects.Apply(null!, game.State, new TurnLog()));
        Assert.Throws<ArgumentNullException>(() => game.Effects.Apply([], null!, new TurnLog()));
        Assert.Throws<ArgumentNullException>(() => game.Effects.Apply([], game.State, null!));
        Assert.Throws<ArgumentNullException>(() => new EffectApplier(null!, game.Dice, game.Checks, game.Text));
        Assert.Throws<ArgumentNullException>(() => new EffectApplier(game.Content, null!, game.Checks, game.Text));
        Assert.Throws<ArgumentNullException>(() => new EffectApplier(game.Content, game.Dice, null!, game.Text));
        Assert.Throws<ArgumentNullException>(() => new EffectApplier(game.Content, game.Dice, game.Checks, null!));
    }

    private sealed record UnknownEffect : EffectDef;
}
