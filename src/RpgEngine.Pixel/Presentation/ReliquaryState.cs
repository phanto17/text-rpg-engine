using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Engine;

namespace RpgEngine.Pixel.Presentation;

/// <summary>Data-bound cistern and furnace state, independent of rendering and room IDs.</summary>
public sealed record ReliquaryState(bool CisternQuenched = false, bool FurnaceOpen = false)
{
    public static ReliquaryState Read(GameSession? session, SceneBindingsDef bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        bool Flag(string? id) => id is not null && (session?.State.World.Flags.Contains(id) ?? false);
        return new(Flag(bindings.CisternQuenchedFlag), Flag(bindings.FurnaceOpenFlag));
    }
}
