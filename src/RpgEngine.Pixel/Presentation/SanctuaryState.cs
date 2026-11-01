using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Engine;

namespace RpgEngine.Pixel.Presentation;

/// <summary>Data-bound relief and sanctuary tokens, independent of renderer and story IDs.</summary>
public sealed record SanctuaryState(bool FriezeRead = false, bool HasChime = false,
    bool HasDispatch = false, bool OfferingMade = false)
{
    public static SanctuaryState Read(GameSession? session, SceneBindingsDef bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        bool Flag(string? id) => id is not null && (session?.State.World.Flags.Contains(id) ?? false);
        return new(Flag(bindings.FriezeReadFlag), Flag(bindings.SanctuaryChimeFlag),
            Flag(bindings.SanctuaryDispatchFlag), Flag(bindings.SanctuaryOfferingFlag));
    }
}
