using RpgEngine.Pixel.Presentation;

namespace RpgEngine.Pixel.Rendering;

/// <summary>Everything the scene viewport needs for one frame of a room, read from the world state by the app.</summary>
internal sealed record SceneFrame(
    RoomLook Look,
    string RoomId,
    double Time,
    int Resonance,
    int SigilsLit,
    bool HasLantern,
    bool SunSealSet,
    bool MoonSealSet,
    bool BellMended,
    bool BrazierLit,
    bool SigilDoorOpen = false,
    bool BeaconLit = false,
    bool SpiritsReleased = false,
    bool PassageOpen = false);
