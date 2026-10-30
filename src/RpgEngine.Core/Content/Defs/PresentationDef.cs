using System.Text.Json.Serialization;

namespace RpgEngine.Core.Content.Defs;

// The presentation records are a small closed family, kept together in one file like the
// conditions and effects. They describe how a graphical client draws rooms, monsters, and NPCs;
// the engine itself never reads them, so every field is optional and a client falls back to its
// own defaults for anything missing.

/// <summary>
/// <c>Data/World/presentation.json</c>: optional looks for a graphical client, keyed by room id,
/// monster id, and NPC id (or the id of the dialogue the NPC speaks).
/// </summary>
public sealed record PresentationDef
{
    public AudioPresentationDef Audio { get; init; } = new();

    public SceneBindingsDef Bindings { get; init; } = new();

    public IReadOnlyDictionary<string, RoomPresentationDef> Rooms { get; init; } = new Dictionary<string, RoomPresentationDef>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, MonsterPresentationDef> Monsters { get; init; } = new Dictionary<string, MonsterPresentationDef>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, NpcPresentationDef> Npcs { get; init; } = new Dictionary<string, NpcPresentationDef>(StringComparer.Ordinal);
}

/// <summary>Optional world-state references used only by the graphical client.</summary>
public sealed record SceneBindingsDef
{
    public string? ResonanceCounter { get; init; }
    public string? SigilCounter { get; init; }
    public string? SigilDoorOpenFlag { get; init; }
    public string? BeaconLitFlag { get; init; }
    public string? ThresholdOpenFlag { get; init; }
    public string? ReleasedSpiritsFlag { get; init; }
    /// <summary>A visited room indicating that the floodgate has been crossed.</summary>
    public string? PassageVisitedRoom { get; init; }
    public string? LanternItem { get; init; }
    public string? SunSealItem { get; init; }
    public string? MoonSealItem { get; init; }
    public IReadOnlyList<string> BellMendedFlags { get; init; } = [];
    public IReadOnlyList<string> BrazierLitFlags { get; init; } = [];
}

/// <summary>Hex colors (<c>#rrggbb</c>) for a room's backdrop gradient, its stonework, and its highlights.</summary>
public sealed record PalettePresentationDef
{
    public string? BackgroundTop { get; init; }

    public string? BackgroundBottom { get; init; }

    public string? Architecture { get; init; }

    public string? Accent { get; init; }
}

public sealed record RoomPresentationDef
{
    public PalettePresentationDef? Palette { get; init; }

    public SceneStyle? SceneStyle { get; init; }

    public WeatherKind? Weather { get; init; }

    /// <summary>A picture to draw instead of the procedural scene, relative to <c>Data/</c>.</summary>
    public string? BackdropImage { get; init; }

    /// <summary>A one-line caption shown over the scene for a few seconds after the player enters.</summary>
    public string? MoodCaption { get; init; }
}

public sealed record MonsterPresentationDef
{
    public string? PrimaryColor { get; init; }

    public string? AccentColor { get; init; }

    public Emblem? Emblem { get; init; }

    /// <summary>A short epithet shown under the name, loaded from presentation data.</summary>
    public string? Title { get; init; }

    /// <summary>A sprite to draw instead of the heraldic emblem, relative to <c>Data/</c>.</summary>
    public string? SpriteImage { get; init; }

    /// <summary>Optional horizontal sprite sheet timing. Omit for a static image.</summary>
    public SpriteAnimationDef? Animation { get; init; }
}

public sealed record SpriteAnimationDef
{
    public int Frames { get; init; }

    public int FrameMilliseconds { get; init; }
}

public sealed record NpcPresentationDef
{
    /// <summary>
    /// The name under the portrait, and the name a speaker without a dialogue of their own (a shop
    /// keeper) is matched by. Defaults to the NPC's name.
    /// </summary>
    public string? SpeakerName { get; init; }

    public string? PrimaryColor { get; init; }

    public string? AccentColor { get; init; }

    public Emblem? Emblem { get; init; }

    public string? Title { get; init; }

    /// <summary>A portrait to draw instead of the heraldic emblem, relative to <c>Data/</c>.</summary>
    public string? PortraitImage { get; init; }
}

/// <summary>The procedural backdrop a client draws for a room.</summary>
public enum SceneStyle
{
    [JsonStringEnumMemberName("moor_gate")] MoorGate,
    [JsonStringEnumMemberName("ossuary_vault")] OssuaryVault,
    [JsonStringEnumMemberName("crypt_rotunda")] CryptRotunda,
    [JsonStringEnumMemberName("spiral_stair")] SpiralStair,
    [JsonStringEnumMemberName("sunken_cathedral")] SunkenCathedral,
    [JsonStringEnumMemberName("echo_chapel")] EchoChapel,
    [JsonStringEnumMemberName("drowned_bridge")] DrownedBridge,
    [JsonStringEnumMemberName("twin_seal_gate")] TwinSealGate,
    [JsonStringEnumMemberName("archive_stacks")] ArchiveStacks,
    [JsonStringEnumMemberName("tallow_loom")] TallowLoom,
    [JsonStringEnumMemberName("vow_nursery")] VowNursery,
    [JsonStringEnumMemberName("scoured_barracks")] ScouredBarracks,
    [JsonStringEnumMemberName("lye_cistern")] LyeCistern,
    [JsonStringEnumMemberName("iron_bastion")] IronBastion,
    [JsonStringEnumMemberName("uncarved_stair")] UncarvedStair,
    [JsonStringEnumMemberName("sanctuary_landing")] SanctuaryLanding,
    [JsonStringEnumMemberName("sundered_bell_nave")] SunderedBellNave,
    [JsonStringEnumMemberName("cradle_of_ash")] CradleOfAsh,
}

/// <summary>The particles drifting through a room.</summary>
public enum WeatherKind
{
    [JsonStringEnumMemberName("none")] None,
    [JsonStringEnumMemberName("rain")] Rain,
    [JsonStringEnumMemberName("ash_motes")] AshMotes,
    [JsonStringEnumMemberName("spores")] Spores,
    [JsonStringEnumMemberName("memory_salt")] MemorySalt,
    [JsonStringEnumMemberName("embers")] Embers,
}

/// <summary>The heraldic device a client draws for a monster or NPC that has no picture of its own.</summary>
public enum Emblem
{
    [JsonStringEnumMemberName("knight_helm")] KnightHelm,
    [JsonStringEnumMemberName("thrall_skull")] ThrallSkull,
    [JsonStringEnumMemberName("choir_mask")] ChoirMask,
    [JsonStringEnumMemberName("warden_halberd")] WardenHalberd,
    [JsonStringEnumMemberName("carillon_cradle")] CarillonCradle,
    [JsonStringEnumMemberName("iron_brazier")] IronBrazier,
    [JsonStringEnumMemberName("architect_astrolabe")] ArchitectAstrolabe,
    [JsonStringEnumMemberName("lantern_nun")] LanternNun,
    [JsonStringEnumMemberName("scribe")] Scribe,
    [JsonStringEnumMemberName("peddler")] Peddler,
    [JsonStringEnumMemberName("keeper")] Keeper,
    [JsonStringEnumMemberName("quartermaster")] Quartermaster,
    [JsonStringEnumMemberName("monk")] Monk,
    [JsonStringEnumMemberName("wright")] Wright,
    [JsonStringEnumMemberName("lancer")] Lancer,
    [JsonStringEnumMemberName("mournling")] Mournling,
    [JsonStringEnumMemberName("chorister")] Chorister,
    [JsonStringEnumMemberName("halberdier")] Halberdier,
    [JsonStringEnumMemberName("prelate")] Prelate,
    [JsonStringEnumMemberName("commander")] Commander,
    [JsonStringEnumMemberName("hooded")] Hooded,
}
