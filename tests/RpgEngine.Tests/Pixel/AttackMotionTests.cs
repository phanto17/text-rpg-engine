using RpgEngine.Core.Combat;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Events;
using RpgEngine.Pixel.Presentation;
using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Tests.Pixel;

public sealed class AttackMotionTests
{
    [Theory]
    [InlineData(0.02, -2)]
    [InlineData(0.18, 2)]
    [InlineData(0.22, 5)]
    [InlineData(0.28, 8)]
    [InlineData(0.40, 5)]
    [InlineData(0.48, 2)]
    [InlineData(0.58, 0)]
    [InlineData(0.70, 0)]
    public void Offset_AtAuthoredPose_HoldsWholePixels(double age, int expected)
        => Assert.Equal(expected, AttackMotion.Offset(AttackMotion.Duration - age));

    [Fact]
    public void Advance_AttackThenDamage_FlashesAtImpactAfterAnticipation()
    {
        var narrator = new EventNarrator { PlayerName = "Hero" };
        var sequencer = new EventSequencer(narrator, 100000);
        sequencer.Enqueue([
            new AttackRolledEvent("Hero", "Enemy", "Sword", new D20Roll(RollMode.Normal, 15, [15]), 3, 18, 12, true, false),
            new DamageTakenEvent("Enemy", false, 3, RpgEngine.Core.Content.Defs.DamageType.Slashing, DamageModifier.None, 7, 10),
        ]);
        sequencer.Advance(0.01);
        Assert.Single(sequencer.TakeCues().OfType<DiceBannerCue>());
        sequencer.Advance(EventNarrator.DiceHoldSeconds + 0.01);
        Assert.Single(sequencer.TakeCues().OfType<LungeCue>());
        sequencer.Advance(AttackMotion.ImpactSeconds - 0.01);
        Assert.Empty(sequencer.TakeCues());
        sequencer.Advance(0.02);
        Assert.Single(sequencer.TakeCues().OfType<HitFlashCue>());
    }
}
