using RpgEngine.CLI;
using RpgEngine.CLI.Rendering;
using RpgEngine.Tests.TestSupport;
using Spectre.Console.Testing;

namespace RpgEngine.Tests.Cli;

public sealed class CliHostTests
{
    [Theory]
    [InlineData(new[] { "--bogus" }, "Unknown option '--bogus'.")]
    [InlineData(new[] { "--seed" }, "--seed needs a non-negative whole number.")]
    [InlineData(new[] { "--seed", "-4" }, "--seed needs a non-negative whole number.")]
    [InlineData(new[] { "--data" }, "--data needs a folder path.")]
    [InlineData(new[] { "--saves", "--no-anim" }, "--saves needs a folder path.")]
    public void Parse_InvalidArguments_ReportErrors(string[] args, string expected)
    {
        Assert.Null(CliOptions.Parse(args, out var error));
        Assert.Equal(expected, error);
    }

    [Fact]
    public void Parse_AllOptions()
    {
        var options = CliOptions.Parse(["--seed", "42", "--no-anim", "--data", "d", "--saves", "s", "-h"], out var error)!;

        Assert.Null(error);
        Assert.Equal((42UL, false, "d", "s", true), (options.Seed!.Value, options.Animations, options.DataDirectory, options.SavesDirectory, options.ShowHelp));
    }

    [Fact]
    public void Parse_Defaults()
    {
        var options = CliOptions.Parse([], out _)!;

        Assert.Null(options.Seed);
        Assert.True(options.Animations);
        Assert.EndsWith("Data", options.DataDirectory);
        Assert.EndsWith("Saves", options.SavesDirectory);
        Assert.Throws<ArgumentNullException>(() => CliOptions.Parse(null!, out _));
    }

    [Fact]
    public void Run_Help_PrintsUsage()
    {
        using var console = new TestConsole();

        Assert.Equal(CliHost.ExitOk, CliHost.Run(["--help"], console));
        Assert.Contains("--seed <n>", console.Output);
    }

    [Fact]
    public void Run_BadArguments_ExitsWithUsageError()
    {
        using var console = new TestConsole();

        Assert.Equal(CliHost.ExitUsageError, CliHost.Run(["--nope"], console));
        Assert.Contains("Usage:", console.Output);
    }

    [Fact]
    public void Run_BrokenContent_ListsEveryProblem()
    {
        var directory = Path.Combine(Path.GetTempPath(), "rpg-bad-data-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            using var console = new TestConsole();
            console.Profile.Width = 500;

            var exit = CliHost.Run(["--data", directory], console);

            Assert.Equal(CliHost.ExitContentError, exit);
            Assert.Contains("4 problem(s)", console.Output);
            Assert.Contains("game.json: required file is missing.", console.Output);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Run_NullArguments_Throw()
    {
        using var console = new TestConsole();

        Assert.Throws<ArgumentNullException>(() => CliHost.Run(null!, console));
        Assert.Throws<ArgumentNullException>(() => CliHost.Run([], null!));
        Assert.Throws<ArgumentNullException>(() => new GameApp(null!, new TestContent().Load(), new CliOptions()));
        Assert.Throws<ArgumentNullException>(() => new GameApp(console, null!, new CliOptions()));
        Assert.Throws<ArgumentNullException>(() => new GameApp(console, new TestContent().Load(), null!));
    }

    [Theory]
    [InlineData(10, 10, "[#87d787]############[/][grey30][/] 10/10")]
    [InlineData(3, 10, "[#d7af00]####[/][grey30]--------[/] 3/10")]
    [InlineData(1, 10, "[#d75f5f]#[/][grey30]-----------[/] 1/10")]
    [InlineData(0, 0, "[#d75f5f][/][grey30]------------[/] 0/0")]
    public void HpBar_ColorsByRemainingHealth(int current, int max, string expected)
    {
        Assert.Equal(expected, Bars.Hp(current, max, unicode: false));
    }

    [Fact]
    public void Bars_UseBlocksOnUnicodeConsoles()
    {
        Assert.Equal("[#5f87d7]███[/][grey30]░░░[/] 3/6", Bars.Mana(3, 6, unicode: true, width: 6));
    }

    [Fact]
    public void RollText_FormatsBadgesAndModifiers()
    {
        Assert.Equal("[#d7af5f][[Persuasion DC 14]][/] [#87d787](advantage)[/]", RollText.Badge("Persuasion", 14, Core.Dice.RollMode.Advantage));
        Assert.Equal(" [#d75f5f](disadvantage)[/]", RollText.ModeBadge(Core.Dice.RollMode.Disadvantage));
        Assert.Equal("-2", RollText.Signed(-2));
        Assert.Equal("+0", RollText.Signed(0));
    }
}
