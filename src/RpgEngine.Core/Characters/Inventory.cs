namespace RpgEngine.Core.Characters;

/// <summary>Item counts by item id, kept in the order items were first picked up.</summary>
public sealed class Inventory
{
    private readonly List<string> _order = [];
    private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);

    public IReadOnlyList<InventoryEntry> Entries => _order.Select(id => new InventoryEntry(id, _counts[id])).ToList();

    public bool IsEmpty => _order.Count == 0;

    public int CountOf(string itemId) => _counts.GetValueOrDefault(itemId);

    public bool Contains(string itemId, int count = 1) => CountOf(itemId) >= count;

    public void Add(string itemId, int count = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        if (_counts.TryGetValue(itemId, out var existing))
        {
            _counts[itemId] = existing + count;
            return;
        }

        _order.Add(itemId);
        _counts[itemId] = count;
    }

    /// <summary>Removes up to <paramref name="count"/> items; returns false (and removes nothing) if there are not enough.</summary>
    public bool Remove(string itemId, int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        if (!Contains(itemId, count))
        {
            return false;
        }

        var remaining = _counts[itemId] - count;
        if (remaining == 0)
        {
            _counts.Remove(itemId);
            _order.Remove(itemId);
        }
        else
        {
            _counts[itemId] = remaining;
        }

        return true;
    }
}
