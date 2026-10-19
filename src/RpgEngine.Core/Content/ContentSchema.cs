namespace RpgEngine.Core.Content;

/// <summary>
/// One content file kind: the record it deserializes to, whether the file holds an array of them,
/// and the workspace file patterns (VS Code <c>fileMatch</c> globs) it applies to.
/// </summary>
public sealed record ContentSchema(string Name, Type Type, bool IsArray, IReadOnlyList<string> FileMatch)
{
    public string FileName => $"{Name}.schema.json";

    public string Title => IsArray ? $"RpgEngine content: {Name} definitions" : $"RpgEngine content: {Name} manifest";
}
