using Raylib_cs;
using RpgEngine.Pixel.Presentation;

namespace RpgEngine.Pixel.Rendering;

/// <summary>Owns the optional audio device, short clips and at most two crossfading ambience streams.</summary>
internal sealed class AudioPlayer(string dataDirectory) : IDisposable
{
    private readonly Dictionary<string, Sound?> _effects = new(StringComparer.Ordinal);
    private Music? _music;
    private Music? _fadingMusic;
    private string? _path;
    private float _gain;
    private float _fadeGain;
    public bool Available { get; private set; }

    public void Open()
    {
        Raylib.InitAudioDevice();
        Available = Raylib.IsAudioDeviceReady();
    }

    public void Play(string? path, AudioPreferences preferences)
    {
        if (!Available || path is null || preferences.Muted) return;
        if (!_effects.TryGetValue(path, out var sound))
        {
            if (PcmWave.Resolve(dataDirectory, path) is { } fullPath)
            {
                var loaded = Raylib.LoadSound(fullPath);
                if (Raylib.IsSoundValid(loaded)) sound = loaded;
            }
            _effects[path] = sound;
        }
        if (sound is { } clip)
        {
            Raylib.SetSoundVolume(clip, preferences.EffectsVolume);
            Raylib.PlaySound(clip);
        }
    }

    public void Update(string? ambiencePath, double dt, AudioPreferences preferences)
    {
        if (!Available) return;
        if (_path != ambiencePath)
        {
            if (_fadingMusic is { } old) Raylib.UnloadMusicStream(old);
            _fadingMusic = _music;
            _fadeGain = _gain;
            _music = null;
            _gain = 0;
            _path = ambiencePath;
            if (ambiencePath is not null && PcmWave.Resolve(dataDirectory, ambiencePath) is { } fullPath)
            {
                var loaded = Raylib.LoadMusicStream(fullPath);
                if (Raylib.IsMusicValid(loaded))
                {
                    loaded.Looping = true;
                    Raylib.SetMusicVolume(loaded, 0);
                    Raylib.PlayMusicStream(loaded);
                    _music = loaded;
                }
            }
        }
        _gain = Math.Min(1, _gain + (float)(dt * 2));
        _fadeGain = Math.Max(0, _fadeGain - (float)(dt * 2));
        ApplyVolumes(preferences);
        if (_music is { } current) Raylib.UpdateMusicStream(current);
        if (_fadingMusic is { } fading)
        {
            Raylib.UpdateMusicStream(fading);
            if (_fadeGain == 0)
            {
                Raylib.UnloadMusicStream(fading);
                _fadingMusic = null;
            }
        }
    }

    public void ApplyVolumes(AudioPreferences preferences)
    {
        if (!Available) return;
        var effects = preferences.Muted ? 0 : preferences.EffectsVolume;
        var ambience = preferences.Muted ? 0 : preferences.AmbienceVolume;
        foreach (var sound in _effects.Values)
            if (sound is { } clip) Raylib.SetSoundVolume(clip, effects);
        if (_music is { } current) Raylib.SetMusicVolume(current, ambience * _gain);
        if (_fadingMusic is { } fading) Raylib.SetMusicVolume(fading, ambience * _fadeGain);
    }

    public void Dispose()
    {
        if (!Available) return;
        foreach (var sound in _effects.Values)
            if (sound is { } clip) Raylib.UnloadSound(clip);
        if (_music is { } current) Raylib.UnloadMusicStream(current);
        if (_fadingMusic is { } fading) Raylib.UnloadMusicStream(fading);
        Raylib.CloseAudioDevice();
        Available = false;
    }
}
