using Raylib_cs;

namespace RpgEngine.Pixel.Rendering;

/// <summary>Loads pictures named in <c>presentation.json</c> from under <c>Data/</c>, once each; a missing or broken file stays null.</summary>
internal sealed class TextureCache(string dataDirectory) : IDisposable
{
    private readonly Dictionary<string, Texture2D?> _textures = new(StringComparer.Ordinal);

    public Texture2D? Get(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        if (_textures.TryGetValue(relativePath, out var cached))
        {
            return cached;
        }

        Texture2D? texture = null;
        var path = Path.Combine(dataDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path))
        {
            var loaded = Raylib.LoadTexture(path);
            if (Raylib.IsTextureValid(loaded))
            {
                Raylib.SetTextureFilter(loaded, TextureFilter.Point);
                texture = loaded;
            }
        }

        _textures[relativePath] = texture;
        return texture;
    }

    public void Dispose()
    {
        foreach (var texture in _textures.Values)
        {
            if (texture is { } loaded)
            {
                Raylib.UnloadTexture(loaded);
            }
        }

        _textures.Clear();
    }
}
