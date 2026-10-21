using RpgEngine.Core.Checks;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Events;
using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Tests.Pixel;

public sealed class EventSequencerTests
{
    private static EventSequencer NewSequencer(double charactersPerSecond = 10) =>
        new(new EventNarrator { PlayerName = "Tester" }, charactersPerSecond);

    [Fact]
    public void Advance_RevealsCharactersAtTheConfiguredRate()
    {
        var sequencer = NewSequencer(charactersPerSecond: 10);
        sequencer.Enqueue(Beat.Of(LogLine.Of("Hello world")));

        Assert.True(sequencer.IsIdle == false);
        sequencer.Advance(0.5);

        Assert.Single(sequencer.Lines);
        Assert.Equal(5, sequencer.RevealedCharacters);
        Assert.False(sequencer.IsIdle);

        sequencer.Advance(0.6);

        Assert.Equal(11, sequencer.RevealedCharacters);
        Assert.True(sequencer.IsIdle);
    }

    [Fact]
    public void Advance_PlaysBeatsAndLinesInOrder_AndFiresCuesWhenABeatStarts()
    {
        var sequencer = NewSequencer(charactersPerSecond: 100);
        sequencer.Enqueue(Beat.Of([LogLine.Of("one"), LogLine.Of("two")], new RoomCue("a")));
        sequencer.Enqueue(Beat.Of([LogLine.Of("three")], new RoomCue("b")));

        Assert.Empty(sequencer.TakeCues());
        sequencer.Advance(0.04);

        Assert.Equal(["one", "two"], sequencer.Lines.Select(l => l.Text));
        Assert.Equal(1, sequencer.RevealedCharacters);
        Assert.Equal([new RoomCue("a")], sequencer.TakeCues());
        Assert.Empty(sequencer.TakeCues());

        sequencer.Advance(1);

        Assert.Equal(["one", "two", "three"], sequencer.Lines.Select(l => l.Text));
        Assert.Equal([new RoomCue("b")], sequencer.TakeCues());
        Assert.True(sequencer.IsIdle);
    }

    [Fact]
    public void Hold_DelaysTheLines_ButTheCueFiresAtOnce()
    {
        var sequencer = NewSequencer(charactersPerSecond: 100);
        sequencer.Enqueue(Beat.Of([LogLine.Of("verdict")], new LevelUpCue(2)).Holding(1));

        // The beat starts (and its cue fires) on the first advance; the hold runs from there.
        sequencer.Advance(0.5);

        Assert.Single(sequencer.TakeCues());
        Assert.Empty(sequencer.Lines);
        Assert.False(sequencer.IsIdle);

        sequencer.Advance(0.5);

        Assert.Empty(sequencer.Lines);

        // The time left after the hold elapses goes toward the text.
        sequencer.Advance(0.52);

        Assert.Equal("verdict", Assert.Single(sequencer.Lines).Text);
        Assert.Equal(2, sequencer.RevealedCharacters);
    }

    [Fact]
    public void Skip_RevealsEverythingQueued()
    {
        var sequencer = NewSequencer(charactersPerSecond: 1);
        sequencer.Enqueue(Beat.Of([LogLine.Of("alpha"), LogLine.Of("beta")], new CombatCue(true)).Holding(5));
        sequencer.Enqueue(Beat.Of(LogLine.Of("gamma")));
        sequencer.Advance(0.1);

        sequencer.Skip();

        Assert.Equal(["alpha", "beta", "gamma"], sequencer.Lines.Select(l => l.Text));
        Assert.Equal(5, sequencer.RevealedCharacters);
        Assert.True(sequencer.IsIdle);
        Assert.Equal([new CombatCue(true)], sequencer.TakeCues());
    }

    [Fact]
    public void Enqueue_GameEvents_GoThroughTheNarrator()
    {
        var sequencer = NewSequencer(charactersPerSecond: 1000);
        var roll = new D20Roll(RollMode.Normal, 13, [13]);
        sequencer.Enqueue(
        [
            new NarrationEvent(TextStyle.Narration, "The door opens.\n\nBeyond, dark."),
            new CheckRolledEvent(new CheckResult(CheckKind.Skill, "Persuasion", Core.Characters.Ability.Cha, roll, 3, 2, 14, 18, true, false, false)),
            new DamageTakenEvent("Rat", false, 4, DamageType.Slashing, DamageModifier.None, 3, 7),
        ]);

        // The narration (28 characters) finishes inside the first advance, so the check's beat starts
        // and its banner cue fires at once, while its verdict waits out the hold.
        sequencer.Advance(0.05);
        var cues = sequencer.TakeCues();

        Assert.Equal(["The door opens.", string.Empty, "Beyond, dark.", string.Empty], sequencer.Lines.Select(l => l.Text));
        var banner = Assert.IsType<DiceBannerCue>(Assert.Single(cues));
        Assert.Equal(("Persuasion", 13, 5, 18, "DC 14", DiceOutcome.Success), (banner.Label, banner.Roll.Natural, banner.Bonus, banner.Total, banner.Versus, banner.Outcome));
        Assert.False(sequencer.IsIdle);

        sequencer.Advance(EventNarrator.DiceHoldSeconds - 0.1);
        Assert.Equal(4, sequencer.Lines.Count);

        sequencer.Advance(0.12);
        Assert.Empty(sequencer.TakeCues());
        Assert.Equal(5, sequencer.Lines.Count);
        Assert.InRange(sequencer.RevealedCharacters, 19, 21);
        Assert.Contains("[Persuasion DC 14]", sequencer.Lines[^1].Text, StringComparison.Ordinal);
        Assert.EndsWith("SUCCESS", sequencer.Lines[^1].Text, StringComparison.Ordinal);

        sequencer.Advance(1);
        cues = sequencer.TakeCues();

        Assert.Contains(new HitFlashCue("Rat", false), cues);
        Assert.Contains(new FloatingNumberCue("Rat", false, "-4", FloatingKind.Damage), cues);
        Assert.Equal("Rat takes 4 slashing damage  [HP 3/7]", sequencer.Lines[^1].Text);
        Assert.True(sequencer.IsIdle);
    }

    [Fact]
    public void Lines_AreCappedAtMaxLines()
    {
        var sequencer = NewSequencer(charactersPerSecond: 100000);
        for (var i = 0; i < EventSequencer.MaxLines + 50; i++)
        {
            sequencer.Enqueue(Beat.Of(LogLine.Of($"line {i}")));
        }

        sequencer.Skip();

        Assert.Equal(EventSequencer.MaxLines, sequencer.Lines.Count);
        Assert.Equal($"line {EventSequencer.MaxLines + 49}", sequencer.Lines[^1].Text);
    }

    [Fact]
    public void Clear_ForgetsEverything_AndBumpsTheVersion()
    {
        var sequencer = NewSequencer();
        sequencer.Enqueue(Beat.Of([LogLine.Of("gone")], new CombatCue(false)));
        sequencer.Advance(0.1);
        var version = sequencer.Version;

        sequencer.Clear();

        Assert.Empty(sequencer.Lines);
        Assert.Empty(sequencer.TakeCues());
        Assert.True(sequencer.IsIdle);
        Assert.True(sequencer.Version > version);
    }

    [Fact]
    public void Say_QueuesASystemLine()
    {
        var sequencer = NewSequencer(charactersPerSecond: 1000);

        sequencer.Say("Game saved.", LogTone.Success);
        sequencer.Advance(1);

        var line = Assert.Single(sequencer.Lines);
        Assert.Equal([new LogSpan("Game saved.", LogTone.Success)], line.Spans);
    }

    [Fact]
    public void Constructor_RejectsBadArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new EventSequencer(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EventSequencer(new EventNarrator(), 0));
    }
}
