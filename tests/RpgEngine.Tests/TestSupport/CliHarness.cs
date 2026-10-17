using System.Text.RegularExpressions;
using RpgEngine.CLI;
using RpgEngine.Core.Dice;
using RpgEngine.Tests.Content;
using Spectre.Console.Testing;

namespace RpgEngine.Tests.TestSupport;

/// <summary>Runs the real CLI against a <see cref="TestConsole"/> with scripted keyboard input.</summary>
internal sealed partial class CliHarness : IDisposable
{
    public CliHarness()
    {
        Console = new TestConsole().Interactive();
        Console.Profile.Width = 1000;
        SavesDirectory = Path.Combine(Path.GetTempPath(), "rpg-cli-saves-" + Guid.NewGuid().ToString("N"));
    }

    public TestConsole Console { get; }

    public string SavesDirectory { get; }

    /// <summary>Console output with all whitespace collapsed, so assertions ignore layout.</summary>
    public string Output => Whitespace().Replace(Console.Output, " ");

    public int Run(IEnumerable<string> inputs, Func<ulong, IRandomSource>? random = null, params string[] extraArgs)
    {
        foreach (var input in inputs)
        {
            Console.Input.PushTextWithEnter(input);
        }

        string[] args = ["--no-anim", "--seed", "7", "--data", RealDataTests.DataDirectory, "--saves", SavesDirectory, .. extraArgs];
        return CliHost.Run(args, Console, random ?? (_ => new LuckyRandomSource()));
    }

    public void Dispose()
    {
        Console.Dispose();
        if (Directory.Exists(SavesDirectory))
        {
            Directory.Delete(SavesDirectory, recursive: true);
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
