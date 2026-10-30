using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Engine;

namespace RpgEngine.Pixel.Presentation;

/// <summary>Resolves data-defined bindings without coupling the renderer to a particular story.</summary>
public sealed record SceneState(int Resonance, int SigilsLit, bool HasLantern, bool SunSealSet,
    bool MoonSealSet, bool BellMended, bool BrazierLit, bool SigilDoorOpen = false, bool BeaconLit = false,
    bool SpiritsReleased = false, bool PassageOpen = false, bool ThresholdOpen = false)
{
    public static SceneState Read(GameSession? session, SceneBindingsDef bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        int Counter(string? id) => id is null ? 0 : session?.State.World.GetCounter(id) ?? 0;
        bool Item(string? id) => id is not null && (session?.State.Player.Inventory.Contains(id) ?? false);
        bool Flag(IReadOnlyList<string> ids) => ids.Any(id => session?.State.World.Flags.Contains(id) ?? false);
        return new(Counter(bindings.ResonanceCounter), Counter(bindings.SigilCounter), Item(bindings.LanternItem),
            Item(bindings.SunSealItem), Item(bindings.MoonSealItem), Flag(bindings.BellMendedFlags), Flag(bindings.BrazierLitFlags),
            bindings.SigilDoorOpenFlag is { } door && (session?.State.World.Flags.Contains(door) ?? false),
            bindings.BeaconLitFlag is { } beacon && (session?.State.World.Flags.Contains(beacon) ?? false),
            bindings.ReleasedSpiritsFlag is { } spirits && (session?.State.World.Flags.Contains(spirits) ?? false),
            bindings.PassageVisitedRoom is { } room && (session?.State.World.VisitedRooms.Contains(room) ?? false),
            bindings.ThresholdOpenFlag is { } threshold && (session?.State.World.Flags.Contains(threshold) ?? false));
    }
}
