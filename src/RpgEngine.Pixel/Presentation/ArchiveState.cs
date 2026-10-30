using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Engine;

namespace RpgEngine.Pixel.Presentation;

/// <summary>Data-bound loom and carillon state, independent of rendering and room IDs.</summary>
public sealed record ArchiveState(bool LoomSlack = false, bool CarillonStopped = false, bool SpiritReleased = false)
{
    public static ArchiveState Read(GameSession? session, SceneBindingsDef bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        bool Flag(string? id) => id is not null && (session?.State.World.Flags.Contains(id) ?? false);
        var released = Flag(bindings.CarillonReleasedFlag);
        return new(Flag(bindings.LoomSlackFlag), Flag(bindings.CarillonStoppedFlag) || released, released);
    }
}
