using RpgEngine.Core.Events;

namespace RpgEngine.Pixel.Sequencing;

/// <summary>
/// Turns batches of <see cref="GameEvent"/>s into a steady typewriter reveal. Beats are played in
/// order: a beat's cues fire the moment it starts, its hold elapses, then its lines are revealed at
/// <see cref="CharactersPerSecond"/>. The renderer reads <see cref="Lines"/> and
/// <see cref="RevealedCharacters"/> every frame and drains <see cref="TakeCues"/> for effects.
/// </summary>
public sealed class EventSequencer
{
    public const int MaxLines = 600;

    private readonly EventNarrator _narrator;
    private readonly Queue<Beat> _pending = new();
    private readonly List<LogLine> _lines = [];
    private readonly List<SequenceCue> _cues = [];
    private Beat? _current;
    private int _lineIndex;
    private double _budget;
    private double _hold;

    public EventSequencer(EventNarrator narrator, double charactersPerSecond = 80)
    {
        ArgumentNullException.ThrowIfNull(narrator);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(charactersPerSecond);
        _narrator = narrator;
        CharactersPerSecond = charactersPerSecond;
    }

    public EventNarrator Narrator => _narrator;

    public double CharactersPerSecond { get; set; }

    /// <summary>Every line started so far; only the last may be partly revealed.</summary>
    public IReadOnlyList<LogLine> Lines => _lines;

    /// <summary>How many characters of the last line are visible.</summary>
    public int RevealedCharacters { get; private set; }

    /// <summary>Nothing is queued, holding, or still being typed.</summary>
    public bool IsIdle => _current is null && _pending.Count == 0 && _hold <= 0;

    /// <summary>Increments whenever the visible text changes, so a view can scroll to the newest line.</summary>
    public int Version { get; private set; }

    public void Enqueue(IEnumerable<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        foreach (var gameEvent in events)
        {
            foreach (var beat in _narrator.Narrate(gameEvent))
            {
                _pending.Enqueue(beat);
            }
        }
    }

    public void Enqueue(Beat beat)
    {
        ArgumentNullException.ThrowIfNull(beat);
        _pending.Enqueue(beat);
    }

    /// <summary>Queues one plain line, for the client's own messages ("Game saved.").</summary>
    public void Say(string text, LogTone tone = LogTone.System) => Enqueue(Beat.Of(LogLine.Of(text, tone)));

    /// <summary>Moves the reveal forward by <paramref name="deltaSeconds"/>.</summary>
    public void Advance(double deltaSeconds)
    {
        if (deltaSeconds <= 0)
        {
            return;
        }

        if (_hold > 0)
        {
            _hold -= deltaSeconds;
            if (_hold > 0)
            {
                return;
            }

            // The time left after the hold goes toward the text.
            deltaSeconds = -_hold;
            _hold = 0;
        }

        _budget += deltaSeconds * CharactersPerSecond;
        while (true)
        {
            if (_current is null)
            {
                if (!_pending.TryDequeue(out var beat))
                {
                    _budget = 0;
                    return;
                }

                Start(beat);
                if (_hold > 0)
                {
                    _budget = 0;
                    return;
                }

                if (_current is null)
                {
                    continue;
                }
            }

            if (_lineIndex < 0)
            {
                _lineIndex = 0;
                Append(_current.Lines[0]);
            }

            var line = _lines[^1];
            var remaining = line.Length - RevealedCharacters;
            if (remaining > _budget)
            {
                var step = (int)_budget;
                RevealedCharacters += step;
                _budget -= step;
                if (step > 0)
                {
                    Version++;
                }

                return;
            }

            RevealedCharacters = line.Length;
            _budget -= remaining;
            Version++;

            _lineIndex++;
            if (_lineIndex < _current.Lines.Count)
            {
                Append(_current.Lines[_lineIndex]);
            }
            else
            {
                _current = null;
            }
        }
    }

    /// <summary>Reveals everything that is queued at once.</summary>
    public void Skip()
    {
        _hold = 0;
        _budget = 0;
        while (_current is not null || _pending.Count > 0)
        {
            if (_current is null)
            {
                Start(_pending.Dequeue());
                _hold = 0;
                continue;
            }

            for (var i = Math.Max(0, _lineIndex); i < _current.Lines.Count; i++)
            {
                if (i != _lineIndex)
                {
                    Append(_current.Lines[i]);
                }

                RevealedCharacters = _lines[^1].Length;
            }

            _current = null;
        }

        Version++;
    }

    /// <summary>Returns the cues fired since the last call and forgets them.</summary>
    public IReadOnlyList<SequenceCue> TakeCues()
    {
        if (_cues.Count == 0)
        {
            return [];
        }

        var taken = _cues.ToList();
        _cues.Clear();
        return taken;
    }

    public void Clear()
    {
        _pending.Clear();
        _lines.Clear();
        _cues.Clear();
        _current = null;
        _lineIndex = 0;
        _budget = 0;
        _hold = 0;
        RevealedCharacters = 0;
        Version++;
    }

    // A beat's cues fire now; its first line waits until any hold has elapsed.
    private void Start(Beat beat)
    {
        _cues.AddRange(beat.Cues);
        _hold = beat.HoldSeconds;
        _lineIndex = -1;
        _current = beat.Lines.Count == 0 ? null : beat;
    }

    private void Append(LogLine line)
    {
        _lines.Add(line);
        RevealedCharacters = 0;
        if (_lines.Count > MaxLines)
        {
            _lines.RemoveRange(0, _lines.Count - MaxLines);
        }

        Version++;
    }
}
