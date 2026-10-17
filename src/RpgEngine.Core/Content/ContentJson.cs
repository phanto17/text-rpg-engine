using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace RpgEngine.Core.Content;

/// <summary>
/// Shared JSON settings for content and saves: camelCase, comments and trailing commas allowed,
/// enums as camelCase strings, and unknown properties rejected so typos fail loudly.
/// </summary>
public static class ContentJson
{
    public static JsonSerializerOptions Options { get; } = Create(writeIndented: false);

    public static JsonSerializerOptions IndentedOptions { get; } = Create(writeIndented: true);

    private static JsonSerializerOptions Create(bool writeIndented)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            AllowOutOfOrderMetadataProperties = true,
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = writeIndented,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        options.MakeReadOnly();
        return options;
    }
}
