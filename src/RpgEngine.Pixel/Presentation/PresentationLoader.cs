using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Pixel.Presentation;

/// <summary>
/// Reads <c>Data/World/presentation.json</c> forgivingly: a missing file, an unknown property, a
/// misspelled scene style, or an entry of the wrong shape never stops the game. Anything it cannot
/// use is dropped with a warning and the client falls back to its built-in defaults for it.
/// </summary>
public static class PresentationLoader
{
    public const string RelativePath = "World/presentation.json";

    private static readonly JsonDocumentOptions ReadOptions = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    private static readonly IReadOnlyDictionary<string, SceneStyle> SceneStyles = EnumNames<SceneStyle>();
    private static readonly IReadOnlyDictionary<string, WeatherKind> Weathers = EnumNames<WeatherKind>();
    private static readonly IReadOnlyDictionary<string, Emblem> Emblems = EnumNames<Emblem>();

    // The property names the schema allows; "id" is accepted for the array form of a section.
    private static readonly string[] RoomProperties = ["id", "palette", "sceneStyle", "weather", "backdropImage", "moodCaption"];
    private static readonly string[] PaletteProperties = ["backgroundTop", "backgroundBottom", "architecture", "accent"];
    private static readonly string[] MonsterProperties = ["id", "primaryColor", "accentColor", "emblem", "title", "spriteImage"];
    private static readonly string[] NpcProperties = ["id", "speakerName", "primaryColor", "accentColor", "emblem", "title", "portraitImage"];

    public static PresentationLoadResult Load(string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        var path = Path.Combine(dataDirectory, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            return new PresentationLoadResult(new PresentationDef(), FileFound: false, []);
        }

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new PresentationLoadResult(new PresentationDef(), FileFound: true, [$"{RelativePath}: could not be read ({ex.Message})."]);
        }

        return Parse(json) with { FileFound = true };
    }

    /// <summary>Parses the file's text. Never throws: unreadable JSON yields an empty presentation and one warning.</summary>
    public static PresentationLoadResult Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var warnings = new List<string>();
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json, documentOptions: ReadOptions);
        }
        catch (JsonException ex)
        {
            return new PresentationLoadResult(new PresentationDef(), FileFound: false, [$"{RelativePath}: {ex.Message}"]);
        }

        if (root is not JsonObject top)
        {
            return new PresentationLoadResult(new PresentationDef(), FileFound: false, [$"{RelativePath}: the file must hold a JSON object."]);
        }

        Known(top, string.Empty, ["rooms", "monsters", "npcs"], warnings);
        var presentation = new PresentationDef
        {
            Rooms = Entries(top, "rooms", warnings, (node, where) =>
            {
                Known(node, where, RoomProperties, warnings);
                return new RoomPresentationDef
                {
                    Palette = Palette(node["palette"], where, warnings),
                    SceneStyle = Enum(node, "sceneStyle", SceneStyles, where, warnings),
                    Weather = Enum(node, "weather", Weathers, where, warnings),
                    BackdropImage = Text(node, "backdropImage"),
                    MoodCaption = Text(node, "moodCaption"),
                };
            }),
            Monsters = Entries(top, "monsters", warnings, (node, where) =>
            {
                Known(node, where, MonsterProperties, warnings);
                return new MonsterPresentationDef
                {
                    PrimaryColor = Color(node, "primaryColor", where, warnings),
                    AccentColor = Color(node, "accentColor", where, warnings),
                    Emblem = Enum(node, "emblem", Emblems, where, warnings),
                    Title = Text(node, "title"),
                    SpriteImage = Text(node, "spriteImage"),
                };
            }),
            Npcs = Entries(top, "npcs", warnings, (node, where) =>
            {
                Known(node, where, NpcProperties, warnings);
                return new NpcPresentationDef
                {
                    SpeakerName = Text(node, "speakerName"),
                    PrimaryColor = Color(node, "primaryColor", where, warnings),
                    AccentColor = Color(node, "accentColor", where, warnings),
                    Emblem = Enum(node, "emblem", Emblems, where, warnings),
                    Title = Text(node, "title"),
                    PortraitImage = Text(node, "portraitImage"),
                };
            }),
        };

        return new PresentationLoadResult(presentation, FileFound: false, warnings);
    }

    // A section is an object keyed by id, or (just as welcome) an array of objects carrying an "id".
    private static Dictionary<string, T> Entries<T>(JsonObject top, string section, List<string> warnings, Func<JsonObject, string, T> read)
    {
        var entries = new Dictionary<string, T>(StringComparer.Ordinal);
        switch (top[section])
        {
            case null:
                break;
            case JsonObject map:
                foreach (var (id, node) in map)
                {
                    Add(id, node);
                }

                break;
            case JsonArray list:
                foreach (var node in list)
                {
                    var id = node is JsonObject item ? Text(item, "id") : null;
                    if (id is null)
                    {
                        warnings.Add($"{RelativePath}: an entry in '{section}' has no id and was skipped.");
                        continue;
                    }

                    Add(id, node);
                }

                break;
            default:
                warnings.Add($"{RelativePath}: '{section}' must be an object keyed by id (or an array of entries with an id) and was skipped.");
                break;
        }

        return entries;

        void Add(string id, JsonNode? node)
        {
            if (node is not JsonObject item)
            {
                warnings.Add($"{RelativePath}: '{section}.{id}' is not an object and was skipped.");
                return;
            }

            entries[id] = read(item, $"{section}.{id}");
        }
    }

    private static PalettePresentationDef? Palette(JsonNode? node, string where, List<string> warnings)
    {
        if (node is null)
        {
            return null;
        }

        if (node is not JsonObject palette)
        {
            warnings.Add($"{RelativePath}: '{where}.palette' must be an object and was ignored.");
            return null;
        }

        Known(palette, where + ".palette", PaletteProperties, warnings);
        return new PalettePresentationDef
        {
            BackgroundTop = Color(palette, "backgroundTop", where + ".palette", warnings),
            BackgroundBottom = Color(palette, "backgroundBottom", where + ".palette", warnings),
            Architecture = Color(palette, "architecture", where + ".palette", warnings),
            Accent = Color(palette, "accent", where + ".palette", warnings),
        };
    }

    // Unknown properties are ignored, as the client cannot use them, but reported: the schema rejects
    // them, and a warning is how a misspelled field gets noticed.
    private static void Known(JsonObject node, string where, IReadOnlyCollection<string> allowed, List<string> warnings)
    {
        foreach (var (property, _) in node)
        {
            if (!allowed.Contains(property))
            {
                var path = where.Length == 0 ? property : $"{where}.{property}";
                warnings.Add($"{RelativePath}: '{path}' is not a presentation property and was ignored.");
            }
        }
    }

    private static string? Color(JsonObject node, string property, string where, List<string> warnings)
    {
        var text = Text(node, property);
        if (text is null)
        {
            return null;
        }

        if (PixelColor.TryParse(text) is null)
        {
            warnings.Add($"{RelativePath}: '{where}.{property}' is not a hex color ('{text}') and was ignored.");
            return null;
        }

        return text;
    }

    private static T? Enum<T>(JsonObject node, string property, IReadOnlyDictionary<string, T> names, string where, List<string> warnings)
        where T : struct
    {
        var text = Text(node, property);
        if (text is null)
        {
            return null;
        }

        if (names.TryGetValue(text.Trim(), out var value))
        {
            return value;
        }

        warnings.Add($"{RelativePath}: '{where}.{property}' has an unknown value '{text}' and was ignored.");
        return null;
    }

    private static string? Text(JsonObject node, string property) =>
        node[property] is JsonValue value && value.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text) ? text : null;

    // The JSON spellings come from the enums' JsonStringEnumMemberName attributes, so the schema and
    // this loader agree. The C# member names are accepted too, in any casing.
    private static Dictionary<string, T> EnumNames<T>()
        where T : struct, System.Enum
    {
        var names = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in System.Enum.GetValues<T>())
        {
            var member = value.ToString();
            names[member] = value;
            var attribute = typeof(T).GetField(member)!
                .GetCustomAttributes(typeof(JsonStringEnumMemberNameAttribute), false)
                .OfType<JsonStringEnumMemberNameAttribute>()
                .FirstOrDefault();
            if (attribute is not null)
            {
                names[attribute.Name] = value;
            }
        }

        return names;
    }
}
