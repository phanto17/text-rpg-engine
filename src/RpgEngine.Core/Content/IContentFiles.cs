namespace RpgEngine.Core.Content;

/// <summary>Read-only access to content files by forward-slash relative path (e.g. <c>Rooms/lichgate.json</c>).</summary>
public interface IContentFiles
{
    bool Exists(string relativePath);

    string ReadAllText(string relativePath);

    /// <summary>Relative paths of the <c>*.json</c> files directly inside <paramref name="folder"/>, in ordinal order.</summary>
    IReadOnlyList<string> ListJsonFiles(string folder);
}
