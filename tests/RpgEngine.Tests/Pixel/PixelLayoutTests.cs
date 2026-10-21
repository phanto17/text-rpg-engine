using RpgEngine.Pixel.Layout;
using RpgEngine.Pixel.Presentation;
using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Tests.Pixel;

public sealed class PixelLayoutTests
{
    // A fake pixel font: every character is 6 pixels wide.
    private static int Measure(string text) => text.Length * 6;

    [Fact]
    public void Panels_TileTheCanvasWithoutOverlapping()
    {
        PixelRect[] panels = [PixelLayout.Scene, PixelLayout.Hud, PixelLayout.Log, PixelLayout.ActionBar];

        Assert.Equal(PixelLayout.Width * PixelLayout.Height, panels.Sum(p => p.Width * p.Height));
        for (var y = 0; y < PixelLayout.Height; y += 2)
        {
            for (var x = 0; x < PixelLayout.Width; x += 2)
            {
                Assert.Equal(1, panels.Count(p => p.Contains(x, y)));
            }
        }

        Assert.True(PixelLayout.ActionBar.Contains(PixelLayout.Input.X, PixelLayout.Input.Bottom - 1));
        Assert.True(PixelLayout.ActionBar.Contains(PixelLayout.Buttons.Right - 1, PixelLayout.Buttons.Y));
    }

    [Theory]
    [InlineData(1280, 720, true, 160, 90, 960, 540)]
    [InlineData(1920, 1080, true, 0, 0, 1920, 1080)]
    [InlineData(1280, 720, false, 0, 0, 1280, 720)]
    [InlineData(1000, 1000, false, 0, 219, 1000, 562)]
    [InlineData(500, 300, true, 10, 15, 480, 270)]
    [InlineData(300, 200, true, -90, -35, 480, 270)]
    public void Fit_CentersTheCanvas_ByWholePixelsOrAspect(int width, int height, bool integer, int x, int y, int w, int h)
    {
        Assert.Equal(new PixelRect(x, y, w, h), ViewportFit.Fit(width, height, integer));
    }

    [Fact]
    public void ToCanvas_MapsWindowPixelsBackOntoTheCanvas()
    {
        var viewport = ViewportFit.Fit(1280, 720, integerScale: true);

        Assert.Equal((0, 0), ViewportFit.ToCanvas(viewport, 160, 90));
        Assert.Equal((479, 269), ViewportFit.ToCanvas(viewport, 1119, 629));
        Assert.Equal((240, 135), ViewportFit.ToCanvas(viewport, 640, 360));
        Assert.Equal((-1, -1), ViewportFit.ToCanvas(viewport, 158, 88));
        Assert.Equal((-1, -1), ViewportFit.ToCanvas(new PixelRect(0, 0, 0, 0), 5, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => ViewportFit.Fit(0, 10, true));
    }

    [Fact]
    public void Wrap_BreaksBetweenWords_AndKeepsTones()
    {
        var line = LogLine.Build().Add("You take ", LogTone.Narration).Add("7 damage", LogTone.Failure).Add(" and stagger back", LogTone.Narration).Done();

        var rows = TextWrapper.Wrap(line, 15 * 6, Measure);

        Assert.Equal(["You take 7 ", "damage and ", "stagger back"], rows.Select(r => string.Concat(r.Select(s => s.Text))));
        Assert.Equal(line.Text, string.Concat(rows.SelectMany(r => r).Select(s => s.Text)));
        Assert.Equal(LogTone.Failure, rows[0].Last().Tone);
        Assert.Equal([LogTone.Failure, LogTone.Narration], rows[1].Select(s => s.Tone));
    }

    [Fact]
    public void Wrap_SplitsWordsWiderThanARow_AndYieldsOneRowForABlankLine()
    {
        var rows = TextWrapper.Wrap(LogLine.Of("abcdefghij"), 4 * 6, Measure);

        Assert.Equal(["abcd", "efgh", "ij"], rows.Select(r => string.Concat(r.Select(s => s.Text))));
        Assert.Single(TextWrapper.Wrap(LogLine.Blank, 60, Measure));
        Assert.Empty(TextWrapper.Wrap(LogLine.Blank, 60, Measure)[0]);
        Assert.Throws<ArgumentNullException>(() => TextWrapper.Wrap(null!, 60, Measure));
        Assert.Throws<ArgumentOutOfRangeException>(() => TextWrapper.Wrap(LogLine.Blank, 0, Measure));
    }

    [Fact]
    public void Flow_LaysButtonsOutInRows_AndDropsWhatDoesNotFit()
    {
        var area = new PixelRect(0, 0, 100, 22);

        var rects = ButtonFlow.Flow(["Look", "Save", "Talk to Wren", "Quit", "A label far wider than the whole area"], area, Measure, rowHeight: 10, gap: 2, padding: 3);

        Assert.Equal(new PixelRect(0, 0, 30, 10), rects[0]);
        Assert.Equal(new PixelRect(32, 0, 30, 10), rects[1]);
        Assert.Equal(new PixelRect(0, 12, 78, 10), rects[2]);
        Assert.Null(rects[3]);
        Assert.Null(rects[4]);

        var wide = ButtonFlow.Flow(["A label far wider than the whole area"], area, Measure, rowHeight: 10, gap: 2, padding: 3);
        Assert.Equal(new PixelRect(0, 0, 100, 10), wide[0]);
    }

    [Theory]
    [InlineData("#ff8040", 255, 128, 64, 255)]
    [InlineData("ff8040", 255, 128, 64, 255)]
    [InlineData("#f84", 255, 136, 68, 255)]
    [InlineData("#FF804080", 255, 128, 64, 128)]
    public void PixelColor_ParsesHexForms(string hex, int r, int g, int b, int a)
    {
        Assert.Equal(new PixelColor((byte)r, (byte)g, (byte)b, (byte)a), PixelColor.Parse(hex));
    }

    [Theory]
    [InlineData("")]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("#gg0000")]
    public void PixelColor_RejectsOtherText(string hex)
    {
        Assert.Null(PixelColor.TryParse(hex));
        Assert.Throws<FormatException>(() => PixelColor.Parse(hex));
    }

    [Fact]
    public void PixelColor_BlendsAndScales()
    {
        var black = PixelColor.Black;
        var white = PixelColor.White;

        Assert.Equal(new PixelColor(128, 128, 128), black.Lerp(white, 0.5));
        Assert.Equal(white, black.Lerp(white, 2));
        Assert.Equal(new PixelColor(100, 50, 0), new PixelColor(200, 100, 0).Scale(0.5));
        Assert.Equal(new PixelColor(255, 200, 0), new PixelColor(200, 100, 0).Scale(2));
        Assert.Equal("#ff8040", PixelColor.Parse("#ff8040").ToString());
        Assert.Equal("#ff804080", PixelColor.Parse("#ff8040").WithAlpha(128).ToString());
    }

    [Fact]
    public void LanternLight_WarmsFromFrostToAmber_AsResonanceGrows()
    {
        Assert.Equal(LanternLight.Frost, LanternLight.ColorFor(0));
        Assert.Equal(LanternLight.Candle, LanternLight.ColorFor(LanternLight.FullResonance / 2));
        Assert.Equal(LanternLight.Amber, LanternLight.ColorFor(LanternLight.FullResonance));
        Assert.Equal(LanternLight.Amber, LanternLight.ColorFor(99));

        var reds = Enumerable.Range(0, LanternLight.FullResonance + 1).Select(r => (int)LanternLight.ColorFor(r).R).ToList();
        var blues = Enumerable.Range(0, LanternLight.FullResonance + 1).Select(r => (int)LanternLight.ColorFor(r).B).ToList();
        Assert.Equal(reds.OrderBy(v => v), reds);
        Assert.Equal(blues.OrderByDescending(v => v), blues);
        Assert.True(LanternLight.RadiusFor(LanternLight.FullResonance) > LanternLight.RadiusFor(0));
    }
}
