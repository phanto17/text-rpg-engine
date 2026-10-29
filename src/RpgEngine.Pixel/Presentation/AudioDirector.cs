using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Pixel.Presentation;

/// <summary>Chooses clips and coalesces bursts using presentation time, never gameplay randomness.</summary>
public sealed class AudioDirector(AudioPresentationDef definition, string? lanternItem)
{
    private readonly Dictionary<string, double> _lastPlayed = new(StringComparer.Ordinal);
    private string? _room;
    private double _time;
    private double _lastAny = double.NegativeInfinity;

    public AudioPreferences Preferences { get; set; } = new();

    public void Advance(double deltaSeconds) => _time += Math.Max(0, deltaSeconds);

    public string? Ambience(string roomId) => definition.Ambience.GetValueOrDefault(roomId);

    public string? Resolve(SequenceCue cue) => cue switch
    {
        RoomCue room => Enter(room.RoomId),
        HitFlashCue hit => Effect(hit.IsPlayer ? "damage" : "impact"),
        ItemCue item when item.ItemId == lanternItem => Effect("lantern"),
        _ => null,
    };

    public string? Effect(string role)
    {
        if (Preferences.Muted || Preferences.EffectsVolume <= 0 || !definition.Effects.TryGetValue(role, out var path)) return null;
        if (_time - _lastAny < 0.04 || (_lastPlayed.TryGetValue(role, out var last) && _time - last < 0.15)) return null;
        _lastAny = _time;
        _lastPlayed[role] = _time;
        return path;
    }

    private string? Enter(string room)
    {
        var previous = _room;
        _room = room;
        return previous is not null && previous != room ? Effect("step") : null;
    }
}
