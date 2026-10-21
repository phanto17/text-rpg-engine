using RpgEngine.Core.Combat;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Events;
using RpgEngine.Core.Views;
using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Tests.Pixel;

public sealed class EventNarratorTests
{
    private readonly EventNarrator _narrator = new() { PlayerName = "Tester" };

    [Fact]
    public void RoomDescribed_WritesAHeading_TheDetails_AndARoomCue()
    {
        var room = new RoomView("hall", "The Hall", ["Dust.\n\nMore dust."], "Water drips.", [new ExitView(Direction.North, "A door.", false, "The Gate"), new ExitView(Direction.East, "Stairs.", true, null)], ["altar"], ["coin"], ["Wren"]);

        var beat = Assert.Single(_narrator.Narrate(new RoomDescribedEvent(room, true)));

        Assert.Equal([new RoomCue("hall")], beat.Cues);
        var lines = beat.Lines.Select(l => l.Text).ToList();
        Assert.Equal(LogLineKind.Heading, beat.Lines[1].Kind);
        Assert.Equal("The Hall", lines[1]);
        Assert.Contains("Dust.", lines);
        Assert.Contains("More dust.", lines);
        Assert.Contains("Water drips.", lines);
        Assert.Contains("Here: Wren", lines);
        Assert.Contains("You notice: altar", lines);
        Assert.Contains("Lying here: coin", lines);
        Assert.Contains("Exit: north (to The Gate) - A door.", lines);
        Assert.Contains("Exit: east (blocked)", lines);
    }

    [Fact]
    public void AttackRolled_CuesTheBannerAndLunge_AndAMissFloats()
    {
        var hit = new AttackRolledEvent("Tester", "Rat", "Sword", new D20Roll(RollMode.Advantage, 17, [17, 4]), 5, 22, 12, true, false);
        var miss = new AttackRolledEvent("Rat", "Tester", "Bite", new D20Roll(RollMode.Normal, 3, [3]), 4, 7, 15, false, false);

        var hitBeat = Assert.Single(_narrator.Narrate(hit));
        var missBeat = Assert.Single(_narrator.Narrate(miss));

        Assert.Equal("You attack Rat with Sword: d20 17 (adv 17/4) +5 = 22 vs AC 12  HIT", hitBeat.Lines[0].Text);
        Assert.Equal(EventNarrator.DiceHoldSeconds, hitBeat.HoldSeconds);
        Assert.Contains(new LungeCue("Tester", true, "Rat"), hitBeat.Cues);
        Assert.DoesNotContain(hitBeat.Cues, c => c is FloatingNumberCue);
        var banner = Assert.Single(hitBeat.Cues.OfType<DiceBannerCue>());
        Assert.Equal(("Sword", "AC 12", DiceOutcome.Success), (banner.Label, banner.Versus, banner.Outcome));

        Assert.Equal("Rat attacks you with Bite: d20 3 +4 = 7 vs AC 15  MISS", missBeat.Lines[0].Text);
        Assert.Contains(new FloatingNumberCue("Tester", true, "MISS", FloatingKind.Miss), missBeat.Cues);
        Assert.Contains(new LungeCue("Rat", false, "Tester"), missBeat.Cues);
    }

    [Fact]
    public void Damage_ShowsTheModifier_AndFloatsTheNumber()
    {
        var beat = Assert.Single(_narrator.Narrate(new DamageTakenEvent("Tester", true, 6, DamageType.Fire, DamageModifier.Vulnerable, 4, 12)));

        Assert.Equal("You take 6 fire damage (vulnerable!)  [HP 4/12]", beat.Lines[0].Text);
        Assert.Equal([new HitFlashCue("Tester", true), new FloatingNumberCue("Tester", true, "-6", FloatingKind.Damage)], beat.Cues);
        Assert.Contains(beat.Lines[0].Spans, s => s.Text == "6 fire damage" && s.Tone == LogTone.Failure);
    }

    [Fact]
    public void DialogueLine_IsAQuotePerParagraph_WithASpeakerCue()
    {
        var beat = Assert.Single(_narrator.Narrate(new DialogueLineEvent("Wren", "Her sword comes up.\n\n\"Another grave-robber?\"\nShe waits.")));

        Assert.Equal(["Wren: Her sword comes up.", "\"Another grave-robber?\" She waits."], beat.Lines.Select(l => l.Text));
        Assert.All(beat.Lines, l => Assert.Equal(LogLineKind.Quote, l.Kind));
        Assert.Equal(LogTone.Speaker, beat.Lines[0].Spans[0].Tone);
        Assert.Equal([new SpeakerCue("Wren")], beat.Cues);
    }

    [Fact]
    public void QuestUpdated_WritesAHeading_AndQuotesEachParagraph()
    {
        var quest = new QuestDef { Id = "q", Name = "The Barrowlight", Stages = [new QuestStageDef { Stage = 10, Journal = "Find a way in.\n\nAsk Wren." }] };

        var beat = Assert.Single(_narrator.Narrate(new QuestUpdatedEvent(quest, quest.Stages[0])));

        Assert.Equal(["Journal: The Barrowlight", "Find a way in.", "", "Ask Wren.", ""], beat.Lines.Select(l => l.Text));
        Assert.Equal([LogLineKind.Heading, LogLineKind.Quote, LogLineKind.Normal, LogLineKind.Quote, LogLineKind.Normal], beat.Lines.Select(l => l.Kind));
    }

    [Fact]
    public void CombatEnd_AndEpilogue_UseRules()
    {
        var victory = Assert.Single(_narrator.Narrate(new CombatEndedEvent(CombatOutcome.Victory)));
        var defeat = Assert.Single(_narrator.Narrate(new CombatEndedEvent(CombatOutcome.Defeat)));
        var epilogue = Assert.Single(_narrator.Narrate(new EpilogueEvent("Rest now.")));

        Assert.Equal((LogLineKind.Rule, "VICTORY"), (victory.Lines[0].Kind, victory.Lines[0].Text));
        Assert.Equal([new CombatCue(false)], victory.Cues);
        Assert.Empty(defeat.Lines);
        Assert.Equal(["", "Epilogue", "Rest now.", "", "THE END"], epilogue.Lines.Select(l => l.Text));
    }

    [Fact]
    public void Fallen_AndFled_CueTheStage()
    {
        var fell = Assert.Single(_narrator.Narrate(new CombatantDefeatedEvent("Rat", false)));
        var fled = Assert.Single(_narrator.Narrate(new CombatantFledEvent("Tester", true)));

        Assert.Equal("Rat falls!", fell.Lines[0].Text);
        Assert.Equal([new FellCue("Rat", false, false)], fell.Cues);
        Assert.Equal("You break away and flee!", fled.Lines[0].Text);
        Assert.Equal([new FellCue("Tester", true, true)], fled.Cues);
    }

    [Fact]
    public void Paragraphs_SplitOnBlankLines_AndSigned_FormatsNumbers()
    {
        Assert.Equal(["a b", "", "c", ""], EventNarrator.Paragraphs("a\nb\n\nc", LogTone.Narration).Select(l => l.Text));
        Assert.Equal("+3", EventNarrator.Signed(3));
        Assert.Equal("-2", EventNarrator.Signed(-2));
        Assert.Equal("+0", EventNarrator.Signed(0));
        Assert.Throws<ArgumentNullException>(() => _narrator.Narrate(null!));
    }
}
