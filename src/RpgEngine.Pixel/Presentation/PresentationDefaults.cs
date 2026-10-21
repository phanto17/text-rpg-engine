using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Pixel.Presentation;

/// <summary>
/// The looks the client falls back to when <c>presentation.json</c> says nothing: one per shipped
/// room, monster, and NPC, and keyword guesses for anything new so an unknown id still gets a scene.
/// </summary>
public static class PresentationDefaults
{
    public static readonly RoomLook GenericRoom = Room(SceneStyle.OssuaryVault, WeatherKind.AshMotes, "#14110f", "#2b241e", "#4a3b2c", "#c08a3e");

    public static readonly FigureLook GenericMonster = Figure(Emblem.ThrallSkull, "#8a8070", "#d0c8a0", string.Empty);

    public static readonly FigureLook GenericNpc = Figure(Emblem.Hooded, "#4a4a58", "#d8c8a0", string.Empty);

    public static readonly IReadOnlyDictionary<string, RoomLook> Rooms = new Dictionary<string, RoomLook>(StringComparer.Ordinal)
    {
        ["lichgate"] = Room(SceneStyle.MoorGate, WeatherKind.Rain, "#1b2230", "#3a4a52", "#2a2f33", "#7fa6b8"),
        ["ossuary_hall"] = Room(SceneStyle.OssuaryVault, WeatherKind.AshMotes, "#14110f", "#2b241e", "#4a3b2c", "#c08a3e"),
        ["inner_crypt"] = Room(SceneStyle.CryptRotunda, WeatherKind.AshMotes, "#0f0d14", "#26202c", "#3b3140", "#a8b8d8"),
        ["weeping_stair"] = Room(SceneStyle.SpiralStair, WeatherKind.Rain, "#101418", "#22303a", "#30444c", "#6fa0a8"),
        ["nocturne_square"] = Room(SceneStyle.SunkenCathedral, WeatherKind.Embers, "#1a1410", "#3a2a1c", "#4e3a28", "#ffb347"),
        ["echo_chapel"] = Room(SceneStyle.EchoChapel, WeatherKind.MemorySalt, "#121018", "#2a2440", "#3c3458", "#b8a0ff"),
        ["drowned_tollbridge"] = Room(SceneStyle.DrownedBridge, WeatherKind.Rain, "#0b1418", "#16303a", "#2a4a52", "#5fb0b0"),
        ["warren_crossroads"] = Room(SceneStyle.TwinSealGate, WeatherKind.AshMotes, "#14120e", "#2e2818", "#4a3f22", "#e0b040"),
        ["whispering_stacks"] = Room(SceneStyle.ArchiveStacks, WeatherKind.MemorySalt, "#100e12", "#2a2230", "#4a3a44", "#c8c0a0"),
        ["tallow_foundry"] = Room(SceneStyle.TallowLoom, WeatherKind.Embers, "#1a1208", "#3a2810", "#5a4018", "#ffcc66"),
        ["vow_sanctum"] = Room(SceneStyle.VowNursery, WeatherKind.MemorySalt, "#0e1016", "#202838", "#344058", "#c0d0ff"),
        ["scoured_cloister"] = Room(SceneStyle.ScouredBarracks, WeatherKind.AshMotes, "#121212", "#2a2a2a", "#404040", "#c0c0c0"),
        ["lye_cistern"] = Room(SceneStyle.LyeCistern, WeatherKind.Spores, "#0c1410", "#183028", "#244838", "#7fd0a0"),
        ["reliquary_bastion"] = Room(SceneStyle.IronBastion, WeatherKind.Embers, "#140e0c", "#2c1c18", "#4a2c24", "#d06040"),
        ["threshold_vestibule"] = Room(SceneStyle.UncarvedStair, WeatherKind.AshMotes, "#0e0c0a", "#1e1a16", "#302a24", "#a09070"),
        ["penitent_landing"] = Room(SceneStyle.SanctuaryLanding, WeatherKind.Embers, "#16120e", "#30261c", "#4a3a2a", "#ffb060"),
        ["bell_nave"] = Room(SceneStyle.SunderedBellNave, WeatherKind.MemorySalt, "#0c0e14", "#1c2030", "#2c3448", "#b0b8d0"),
        ["cradle_of_ash"] = Room(SceneStyle.CradleOfAsh, WeatherKind.Embers, "#120a08", "#2c1410", "#4c2418", "#ff8040"),
    };

    public static readonly IReadOnlyDictionary<string, FigureLook> Monsters = new Dictionary<string, FigureLook>(StringComparer.Ordinal)
    {
        ["gravebound_knight"] = Figure(Emblem.KnightHelm, "#6a6f78", "#c04040", "Warden of the Barrow Heart"),
        ["barrow_thrall"] = Figure(Emblem.ThrallSkull, "#8a8070", "#d0c8a0", "Bound to the Knight"),
        ["choir_cantor_miriel"] = Figure(Emblem.ChoirMask, "#5a4a80", "#e0d0ff", "Cantor of the Echo Chapel"),
        ["calcified_mournling"] = Figure(Emblem.Mournling, "#a0a090", "#ffffff", "Grief Turned to Stone"),
        ["tollbridge_warden_garin"] = Figure(Emblem.WardenHalberd, "#305060", "#80c0c0", "Toll-Warden of the Drowned Bridge"),
        ["drowned_lancer"] = Figure(Emblem.Lancer, "#204048", "#60a0a0", "The Toll's Last Guard"),
        ["prelate_vaelen"] = Figure(Emblem.Prelate, "#403060", "#ffd080", "Prelate of Unkept Vows"),
        ["tethered_chorister"] = Figure(Emblem.Chorister, "#504070", "#c0a0ff", "Voice on a Memory-Thread"),
        ["commander_silas"] = Figure(Emblem.Commander, "#602820", "#ffb060", "Commander of the Unburdened"),
        ["scoured_halberdier"] = Figure(Emblem.Halberdier, "#404040", "#d0d0d0", "Scoured of Every Memory"),
        ["first_architect"] = Figure(Emblem.ArchitectAstrolabe, "#5a3020", "#ffc060", "Who Broke the Rite"),
        ["calcified_warden"] = Figure(Emblem.IronBrazier, "#706050", "#ff9040", "Keeper of the Cradle"),
    };

    public static readonly IReadOnlyDictionary<string, FigureLook> Npcs = new Dictionary<string, FigureLook>(StringComparer.Ordinal)
    {
        ["wren"] = Figure(Emblem.LanternNun, "#404858", "#ffd080", "Sister of the Lantern Bearers"),
        ["wren_nocturne"] = Figure(Emblem.LanternNun, "#404858", "#ffd080", "Sister of the Lantern Bearers"),
        ["wren_cradle"] = Figure(Emblem.LanternNun, "#404858", "#ffd080", "Sister of the Lantern Bearers"),
        ["orin"] = Figure(Emblem.Monk, "#3a3a44", "#c8b890", "Brother of the Weeping Stair"),
        ["maeve"] = Figure(Emblem.Keeper, "#4a3a2a", "#ffb347", "Keeper of the Beacon"),
        ["peddler"] = Figure(Emblem.Peddler, "#4a4030", "#d0c060", "Sallow, Who Sells Anything"),
        ["miriel"] = Figure(Emblem.ChoirMask, "#5a4a80", "#e0d0ff", "Cantor of the Echo Chapel"),
        ["garin"] = Figure(Emblem.WardenHalberd, "#305060", "#80c0c0", "Toll-Warden"),
        ["caelan"] = Figure(Emblem.Scribe, "#3a3040", "#c8c0a0", "Archivist of the Whispering Stacks"),
        ["vaelen"] = Figure(Emblem.Prelate, "#403060", "#ffd080", "Prelate of Unkept Vows"),
        ["venn"] = Figure(Emblem.Quartermaster, "#383838", "#c0c0c0", "Quartermaster of the Cloister"),
        ["silas"] = Figure(Emblem.Commander, "#602820", "#ffb060", "Commander of the Unburdened"),
        ["oakhaven"] = Figure(Emblem.Wright, "#4a3a2a", "#ffb060", "Barrow-wright"),
        ["reconciled_architect"] = Figure(Emblem.ArchitectAstrolabe, "#5a3020", "#ffc060", "The First Architect"),
    };

    /// <summary>A look for a room the tables do not know, guessed from words in its id and name.</summary>
    public static RoomLook GuessRoom(string roomId, string roomName)
    {
        var text = (roomId + " " + roomName).ToLowerInvariant();
        return text switch
        {
            _ when Has(text, "gate", "moor", "yard", "lich") => Rooms["lichgate"],
            _ when Has(text, "stair", "steps", "well") => Rooms["weeping_stair"],
            _ when Has(text, "bridge", "ford", "drown", "river") => Rooms["drowned_tollbridge"],
            _ when Has(text, "bell", "nave") => Rooms["bell_nave"],
            _ when Has(text, "brazier", "cradle", "ash", "forge") => Rooms["cradle_of_ash"],
            _ when Has(text, "chapel", "choir", "echo") => Rooms["echo_chapel"],
            _ when Has(text, "archive", "stack", "library", "scriptorium") => Rooms["whispering_stacks"],
            _ when Has(text, "cistern", "lye", "pool", "sewer") => Rooms["lye_cistern"],
            _ when Has(text, "bastion", "reliquary", "keep", "fort") => Rooms["reliquary_bastion"],
            _ when Has(text, "barracks", "cloister", "cell") => Rooms["scoured_cloister"],
            _ when Has(text, "market", "square", "town", "cathedral", "nocturne") => Rooms["nocturne_square"],
            _ when Has(text, "crypt", "heart", "tomb", "sarcophag") => Rooms["inner_crypt"],
            _ when Has(text, "landing", "sanctuary", "vigil", "camp") => Rooms["penitent_landing"],
            _ when Has(text, "loom", "foundry", "tallow") => Rooms["tallow_foundry"],
            _ when Has(text, "nursery", "sanctum", "vow") => Rooms["vow_sanctum"],
            _ when Has(text, "seal", "crossroads", "threshold") => Rooms["warren_crossroads"],
            _ => GenericRoom,
        };
    }

    public static FigureLook GuessMonster(string monsterId, string monsterName)
    {
        var text = (monsterId + " " + monsterName).ToLowerInvariant();
        return text switch
        {
            _ when Has(text, "knight", "ser ") => Monsters["gravebound_knight"],
            _ when Has(text, "cantor", "choir", "singer") => Monsters["choir_cantor_miriel"],
            _ when Has(text, "chorister") => Monsters["tethered_chorister"],
            _ when Has(text, "warden", "guard") => Monsters["tollbridge_warden_garin"],
            _ when Has(text, "lancer", "spear") => Monsters["drowned_lancer"],
            _ when Has(text, "halberd") => Monsters["scoured_halberdier"],
            _ when Has(text, "mournling", "calcified", "stone") => Monsters["calcified_mournling"],
            _ when Has(text, "prelate", "priest") => Monsters["prelate_vaelen"],
            _ when Has(text, "commander", "captain") => Monsters["commander_silas"],
            _ when Has(text, "architect") => Monsters["first_architect"],
            _ => GenericMonster,
        };
    }

    public static FigureLook GuessNpc(string npcId, string npcName)
    {
        var text = (npcId + " " + npcName).ToLowerInvariant();
        return text switch
        {
            _ when Has(text, "sister", "nun", "wren") => Npcs["wren"],
            _ when Has(text, "brother", "monk") => Npcs["orin"],
            _ when Has(text, "keeper") => Npcs["maeve"],
            _ when Has(text, "peddler", "merchant", "trader") => Npcs["peddler"],
            _ when Has(text, "cantor", "choir") => Npcs["miriel"],
            _ when Has(text, "warden", "guard") => Npcs["garin"],
            _ when Has(text, "archivist", "scribe", "librarian") => Npcs["caelan"],
            _ when Has(text, "prelate", "priest") => Npcs["vaelen"],
            _ when Has(text, "quartermaster") => Npcs["venn"],
            _ when Has(text, "commander", "captain") => Npcs["silas"],
            _ when Has(text, "wright", "smith") => Npcs["oakhaven"],
            _ when Has(text, "architect") => Npcs["reconciled_architect"],
            _ => GenericNpc,
        };
    }

    private static bool Has(string text, params string[] words) => words.Any(word => text.Contains(word, StringComparison.Ordinal));

    private static RoomLook Room(SceneStyle style, WeatherKind weather, string top, string bottom, string architecture, string accent) =>
        new(style, weather, PixelColor.Parse(top), PixelColor.Parse(bottom), PixelColor.Parse(architecture), PixelColor.Parse(accent), null);

    private static FigureLook Figure(Emblem emblem, string primary, string accent, string title) =>
        new(emblem, PixelColor.Parse(primary), PixelColor.Parse(accent), title, null);
}
