using RpgEngine.Pixel.Presentation;

namespace RpgEngine.Tests.Pixel;

public sealed class AudioPreferencesTests
{
    [Fact]
    public void Save_Reload_RetainsIndependentVolumesAndMuteOutsideSaveSlots()
    {
        var directory = Path.Combine(Path.GetTempPath(), "pixel-audio-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new AudioPreferencesStore(directory);
            Assert.Equal(new AudioPreferences(), store.Load());
            var expected = new AudioPreferences { Muted = true, EffectsVolume = 0.7f, AmbienceVolume = 0.1f };
            Assert.True(store.Save(expected));
            Assert.Equal(expected, new AudioPreferencesStore(directory).Load());
            Assert.Empty(Directory.GetFiles(directory, "*.json"));
            File.WriteAllText(store.Path, "broken json");
            Assert.Equal(new AudioPreferences(), store.Load());
            File.WriteAllText(store.Path, "{\"Version\":2,\"Muted\":true}");
            Assert.Equal(new AudioPreferences(), store.Load());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void Normalize_InvalidVolumes_ClampsAndRejectsNonFiniteValues()
    {
        var normalized = new AudioPreferences { EffectsVolume = 8, AmbienceVolume = -1 }.Normalize();
        Assert.Equal(1, normalized.EffectsVolume);
        Assert.Equal(0, normalized.AmbienceVolume);
        Assert.Equal(0.45f, new AudioPreferences { EffectsVolume = float.NaN }.Normalize().EffectsVolume);
        Assert.Equal(0.20f, new AudioPreferences { AmbienceVolume = float.PositiveInfinity }.Normalize().AmbienceVolume);
    }

    [Fact]
    public void Save_UnwritableLocation_ReportsFailureWithoutThrowing()
    {
        var path = Path.Combine(Path.GetTempPath(), "pixel-audio-file-" + Guid.NewGuid().ToString("N"));
        try
        {
            File.WriteAllText(path, "occupied by a file");
            Assert.False(new AudioPreferencesStore(path).Save(new()));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Resolve_MissingCorruptOrEscapingClip_ReturnsSilence()
    {
        var directory = Path.Combine(Path.GetTempPath(), "pixel-wave-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(directory, "Assets", "Audio"));
            File.WriteAllBytes(Path.Combine(directory, "Assets", "Audio", "bad.wav"), new byte[100]);
            Assert.Null(PcmWave.Resolve(directory, "Assets/Audio/bad.wav"));
            Assert.Null(PcmWave.Resolve(directory, "Assets/Audio/missing.wav"));
            Assert.Null(PcmWave.Resolve(directory, "Assets/Audio/../../../outside.wav"));
            Assert.Null(PcmWave.Resolve(directory, "C:/outside.wav"));
            Assert.Null(PcmWave.Resolve(directory, "Assets/Audio/unsupported.mp3"));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
