using RpgEngine.Pixel;

namespace RpgEngine.Tests.Pixel;

public sealed class PixelHostTests
{
    [Theory]
    [InlineData(new[] { "--bogus" }, "Unknown option '--bogus'.")]
    [InlineData(new[] { "--seed" }, "--seed needs a non-negative whole number.")]
    [InlineData(new[] { "--seed", "x" }, "--seed needs a non-negative whole number.")]
    [InlineData(new[] { "--data" }, "--data needs a folder path.")]
    [InlineData(new[] { "--saves", "--crt" }, "--saves needs a folder path.")]
    [InlineData(new[] { "--screenshot" }, "--screenshot needs a file path.")]
    [InlineData(new[] { "--play" }, "--play needs a semicolon-separated list of lines.")]
    [InlineData(new[] { "--record" }, "--record needs a fresh directory path.")]
    [InlineData(new[] { "--record", "frames", "--screenshot", "shot.png" }, "Use either --screenshot or --record.")]
    public void Parse_InvalidArguments_ReportErrors(string[] args, string expected)
    {
        Assert.Null(PixelOptions.Parse(args, out var error));
        Assert.Equal(expected, error);
    }

    [Fact]
    public void Parse_AllOptions()
    {
        var options = PixelOptions.Parse(["--seed", "42", "--data", "d", "--saves", "s", "--crt", "--screenshot", "shot.png", "--play", "new; Tester ;1", "-h"], out var error)!;

        Assert.Null(error);
        Assert.Equal((42UL, "d", "s", true, "shot.png", true), (options.Seed!.Value, options.DataDirectory, options.SavesDirectory, options.Crt, options.Screenshot, options.ShowHelp));
        Assert.Equal(["new", "Tester", "1"], options.Script);
    }

    [Fact]
    public void Parse_Defaults_MatchTheTerminalClient()
    {
        var options = PixelOptions.Parse([], out _)!;
        var cli = RpgEngine.CLI.CliOptions.Parse([], out _)!;

        Assert.Equal(cli.DataDirectory, options.DataDirectory);
        Assert.Equal(cli.SavesDirectory, options.SavesDirectory);
        Assert.Null(options.Seed);
        Assert.False(options.Crt);
        Assert.Null(options.Screenshot);
        Assert.Throws<ArgumentNullException>(() => PixelOptions.Parse(null!, out _));
    }

    [Fact]
    public void Parse_Recording_UsesNominatedDirectoryAndScript()
    {
        var options = PixelOptions.Parse(["--record", "frames", "--play", "new;Tester"], out var error)!;
        Assert.Null(error);
        Assert.Equal("frames", options.RecordingDirectory);
        Assert.Equal(["new", "Tester"], options.Script);
    }

    [Fact]
    public void Run_Help_PrintsUsage()
    {
        using var output = new StringWriter();
        using var errors = new StringWriter();

        Assert.Equal(PixelHost.ExitOk, PixelHost.Run(["--help"], output, errors));
        Assert.Contains("--seed <n>", output.ToString(), StringComparison.Ordinal);
        Assert.Empty(errors.ToString());
    }

    [Fact]
    public void Run_BadArguments_ExitsWithUsageError()
    {
        using var output = new StringWriter();
        using var errors = new StringWriter();

        Assert.Equal(PixelHost.ExitUsageError, PixelHost.Run(["--nope"], output, errors));
        Assert.Contains("Unknown option", errors.ToString(), StringComparison.Ordinal);
        Assert.Contains("Usage:", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Run_BadContent_ListsEveryProblem_WithoutOpeningAWindow()
    {
        var directory = Path.Combine(Path.GetTempPath(), "rpg-pixel-bad-data-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        using var output = new StringWriter();
        using var errors = new StringWriter();
        try
        {
            Assert.Equal(PixelHost.ExitContentError, PixelHost.Run(["--data", directory], output, errors));
            Assert.Contains("game.json", errors.ToString(), StringComparison.Ordinal);
            Assert.Contains("problem(s)", errors.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
