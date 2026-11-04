using RpgEngine.Pixel.Layout;

namespace RpgEngine.Tests.Pixel;

public sealed class ButtonPagesTests
{
    [Fact]
    public void Pages_OverflowingChoices_EachActionIsVisibleExactlyOnce()
    {
        var labels = Enumerable.Range(1, 19).Select(i => $"Choice {i} with a long label").ToArray();
        var area = new PixelRect(2, 228, 120, 32);
        var pages = ButtonFlow.Pages(labels, area, text => text.Length * 6, 10, 1, 3, maxButtons: 9);
        Assert.True(pages.Count > 1);
        var indices = pages.SelectMany(page => Enumerable.Range(page.FirstAction, page.Buttons.Count));
        Assert.Equal(Enumerable.Range(0, labels.Length), indices);
        Assert.All(pages.SelectMany(page => page.Buttons), rect =>
        {
            Assert.True(rect.X >= area.X && rect.Y >= area.Y);
            Assert.True(rect.Right <= area.Right && rect.Bottom <= area.Bottom);
        });
    }

    [Fact]
    public void Pages_ManyShortChoices_RespectsShortcutLimit()
    {
        var labels = Enumerable.Repeat("A", 20).ToArray();
        var pages = ButtonFlow.Pages(labels, PixelLayout.Buttons, text => text.Length * 6, 10, 1, 3, 9);
        Assert.Equal([9, 9, 2], pages.Select(page => page.Buttons.Count));
        Assert.Equal(9, pages[1].ActionForShortcut(1));
        Assert.Equal(17, pages[1].ActionForShortcut(9));
        Assert.Null(pages[2].ActionForShortcut(3));
        Assert.Null(pages[0].ActionForShortcut(0));
    }

    [Fact]
    public void Pages_EmptyChoices_ReturnsNoPages()
        => Assert.Empty(ButtonFlow.Pages([], PixelLayout.Buttons, text => text.Length, 10, 1, 3));

    [Fact]
    public void Pages_AreaCannotFitAButton_RejectsInsteadOfLooping()
        => Assert.Throws<ArgumentException>(() => ButtonFlow.Pages(["A"], new PixelRect(0, 0, 100, 9),
            text => text.Length, 10, 1, 3));
}
