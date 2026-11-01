using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Engine;

namespace RpgEngine.Pixel.Presentation;

/// <summary>Ending presentation resolved from data, without story IDs in the renderer.</summary>
public sealed record FinaleState(FinaleEnding Ending = FinaleEnding.None)
{
    public static FinaleState Read(GameSession? session, SceneBindingsDef bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        bool Flag(string? id) => id is not null && (session?.State.World.Flags.Contains(id) ?? false);
        // Prefer passage if a custom or edited save carries conflicting ending flags.
        return new(Flag(bindings.TriadEndingFlag) ? FinaleEnding.Triad
            : Flag(bindings.MoonEndingFlag) ? FinaleEnding.Moon
            : Flag(bindings.SunEndingFlag) ? FinaleEnding.Sun : FinaleEnding.None);
    }
}
