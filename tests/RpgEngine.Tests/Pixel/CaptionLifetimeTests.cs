using RpgEngine.Core.Events;
using RpgEngine.Core.Views;
using RpgEngine.Pixel.Presentation;
using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Tests.Pixel;

public sealed class CaptionLifetimeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RoomReveal_LongDescriptionOrSkip_HoldsThenFades(bool skip)
    {
        var sequencer = new EventSequencer(new EventNarrator(), 10);
        var caption = new CaptionLifetime();
        var room = new RoomView("room", "Room", [new string('x', 200)], null, [], [], [], []);
        sequencer.Enqueue([new RoomDescribedEvent(room, true)]);
        sequencer.Advance(0.1);
        Apply();
        caption.Update(10);
        Assert.Equal(1, caption.Opacity);
        Assert.Equal("Caption", caption.Text);

        if (skip) sequencer.Skip();
        else sequencer.Advance(30);
        Apply();
        caption.Update(2);
        Assert.Equal(1, caption.Opacity);
        caption.Update(0.6);
        Assert.InRange(caption.Opacity, 0.49, 0.51);
        caption.Update(0.7);
        Assert.Null(caption.Text);

        void Apply()
        {
            foreach (var cue in sequencer.TakeCues())
            {
                if (cue is RoomCue started) caption.Show(started.RoomId, "Caption");
                if (cue is RoomRevealedCue completed) caption.Complete(completed.RoomId);
            }
        }
    }

    [Fact]
    public void Show_NewRoom_IgnoresPreviousCompletionAndResetsFade()
    {
        var caption = new CaptionLifetime();
        caption.Show("old", "Old");
        caption.Complete("old");
        caption.Update(2.8);
        caption.Show("new", "New");
        caption.Complete("old");
        Assert.Equal(0, caption.Opacity);
        caption.Update(20);
        Assert.Equal("New", caption.Text);
        Assert.Equal(1, caption.Opacity);
        caption.Show("empty", " ");
        Assert.Null(caption.Text);
    }

    [Fact]
    public void Clear_PendingRoom_DiscardsCompletion()
    {
        var sequencer = new EventSequencer(new EventNarrator());
        sequencer.Enqueue(Beat.Of(LogLine.Of("Text")) with { CompletionCues = [new RoomRevealedCue("old")] });
        sequencer.Clear();
        sequencer.Skip();
        Assert.Empty(sequencer.TakeCues());
    }
}
