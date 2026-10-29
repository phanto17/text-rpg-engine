using System.Text.Json.Nodes;
using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Pixel.Presentation;

/// <summary>Reads optional audio maps without letting a bad clip prevent play.</summary>
internal static class AudioPresentationReader
{
    public static bool IsWavePath(string path) => path.StartsWith("Assets/Audio/", StringComparison.Ordinal)
        && path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
        && !path.Contains('\\') && path.Split('/').All(p => p.Length > 0 && p is not "." and not "..");

    public static AudioPresentationDef Read(JsonNode? node, List<string> warnings)
    {
        if (node is null) return new();
        if (node is not JsonObject audio)
        {
            warnings.Add("World/presentation.json: 'audio' must be an object and was ignored.");
            return new();
        }
        foreach (var key in audio.Select(p => p.Key).Where(k => k is not "effects" and not "ambience"))
            warnings.Add($"World/presentation.json: 'audio.{key}' is unknown and was ignored.");

        Dictionary<string, string> Map(string property)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (audio[property] is null) return result;
            if (audio[property] is not JsonObject map)
            {
                warnings.Add($"World/presentation.json: 'audio.{property}' must be an object and was ignored.");
                return result;
            }
            foreach (var (key, value) in map)
            {
                if ((property == "effects" && key is not ("select" or "step" or "impact" or "damage" or "lantern"))
                    || value is not JsonValue item || !item.TryGetValue<string>(out var path) || !IsWavePath(path))
                {
                    warnings.Add($"World/presentation.json: 'audio.{property}.{key}' is not a supported WAV binding and was ignored.");
                    continue;
                }
                result[key] = path;
            }
            return result;
        }
        return new() { Effects = Map("effects"), Ambience = Map("ambience") };
    }
}
