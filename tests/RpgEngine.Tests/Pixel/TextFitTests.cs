using RpgEngine.Pixel.Layout;

namespace RpgEngine.Tests.Pixel;

public sealed class TextFitTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(80)]
    public void Ellipsize_LongName_AlwaysFitsAvailableWidth(int width)
    {
        static int Measure(string value) => value.Length * 6;
        var result = TextFit.Ellipsize("A very long character or encounter name", width, Measure);
        Assert.True(Measure(result) <= width);
    }

    [Fact]
    public void Tail_LongCommand_KeepsEditableEndVisible()
    {
        var result = TextFit.Tail("inspect the ancient bronze door", 6, s => s.Length);
        Assert.Equal("e door", result);
        Assert.Equal("short", TextFit.Tail("short", 12, s => s.Length));
    }
}
