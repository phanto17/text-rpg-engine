using System.Text.Json;

namespace RpgEngine.Pixel.Presentation;

/// <summary>Local client preferences live beneath the save directory, outside game save slots.</summary>
public sealed class AudioPreferencesStore(string savesDirectory)
{
    public string Path { get; } = System.IO.Path.Combine(savesDirectory, ".pixel", "audio.json");

    public AudioPreferences Load()
    {
        try
        {
            if (!File.Exists(Path)) return new();
            var value = JsonSerializer.Deserialize<AudioPreferences>(File.ReadAllText(Path));
            return value is { Version: 1 } ? value.Normalize() : new();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }

    public bool Save(AudioPreferences preferences)
    {
        var temporary = Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(preferences.Normalize()));
            File.Move(temporary, Path, overwrite: true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
    }
}
