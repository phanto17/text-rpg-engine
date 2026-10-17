namespace RpgEngine.Core.Content;

/// <summary>Content held in memory, keyed by relative path. Used by tests and tools.</summary>
public sealed class InMemoryContentFiles : IContentFiles
{
    private readonly Dictionary<string, string> _files;

    public InMemoryContentFiles(IReadOnlyDictionary<string, string> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        _files = new Dictionary<string, string>(files, StringComparer.Ordinal);
    }

    public bool Exists(string relativePath) => _files.ContainsKey(relativePath);

    public string ReadAllText(string relativePath) =>
        _files.TryGetValue(relativePath, out var text) ? text : throw new FileNotFoundException(relativePath);

    public IReadOnlyList<string> ListJsonFiles(string folder) => _files.Keys
        .Where(path => path.StartsWith(folder + "/", StringComparison.Ordinal)
            && path.EndsWith(".json", StringComparison.Ordinal)
            && !path[(folder.Length + 1)..].Contains('/'))
        .Order(StringComparer.Ordinal)
        .ToList();
}
