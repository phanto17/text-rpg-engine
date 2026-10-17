namespace RpgEngine.Core.Dice;

/// <summary>
/// Replays a scripted sequence of values, one per <see cref="Next"/> call. Used by tests to force
/// exact die faces. Fails loudly when a value is out of range or the script runs out, so a test
/// can never silently drift from the outcome it describes.
/// </summary>
public sealed class FixedRandomSource : IRandomSource
{
    private readonly Queue<int> _values;

    public FixedRandomSource(params int[] values)
        : this((IEnumerable<int>)values)
    {
    }

    public FixedRandomSource(IEnumerable<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = new Queue<int>(values);
    }

    public int Remaining => _values.Count;

    public int Next(int minInclusive, int maxExclusive)
    {
        if (!_values.TryDequeue(out var value))
        {
            throw new InvalidOperationException("FixedRandomSource ran out of scripted values.");
        }

        if (value < minInclusive || value >= maxExclusive)
        {
            throw new InvalidOperationException(
                $"Scripted value {value} is outside the requested range [{minInclusive}, {maxExclusive}).");
        }

        return value;
    }
}
