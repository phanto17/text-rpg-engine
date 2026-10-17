namespace RpgEngine.Core.Content;

public sealed class DirectoryContentFiles : IContentFiles
{
    private readonly string _root;

    public DirectoryContentFiles(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        _root = Path.GetFullPath(root);
    }

    public bool Exists(string relativePath) => File.Exists(Resolve(relativePath));

    public string ReadAllText(string relativePath) => File.ReadAllText(Resolve(relativePath));

    public IReadOnlyList<string> ListJsonFiles(string folder)
    {
        var directory = Resolve(folder);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(path => $"{folder}/{Path.GetFileName(path)}")
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private string Resolve(string relativePath) =>
        Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
}
