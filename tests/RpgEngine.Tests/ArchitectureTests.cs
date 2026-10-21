using System.Reflection;

namespace RpgEngine.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void CoreAssembly_ReferencesNoUiAssemblies_StaysUiAgnostic()
    {
        var core = Assembly.Load("RpgEngine.Core");

        var references = core.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, name => name.StartsWith("Spectre.Console", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name == "RpgEngine.CLI");
        Assert.DoesNotContain(references, name => name.StartsWith("Raylib", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name == "RpgEngine.Pixel");
    }
}
