namespace RpgEngine.Core.Events;

/// <summary>Collects the events and pending transitions produced while handling one player action.</summary>
public sealed class TurnLog
{
    private readonly List<GameEvent> _events = [];
    private readonly Queue<GameTransition> _transitions = new();

    public IReadOnlyList<GameEvent> Events => _events;

    public bool HasPendingTransition => _transitions.Count > 0;

    public void Add(GameEvent gameEvent)
    {
        ArgumentNullException.ThrowIfNull(gameEvent);
        _events.Add(gameEvent);
    }

    public void Request(GameTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        _transitions.Enqueue(transition);
    }

    public bool TryTakeTransition([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out GameTransition? transition) =>
        _transitions.TryDequeue(out transition);

    public void ClearTransitions() => _transitions.Clear();
}
