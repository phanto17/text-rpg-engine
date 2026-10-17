namespace RpgEngine.Core.Dice;

/// <summary>
/// PCG-XSH-RR 64/32 generator (O'Neill, pcg-random.org). We ship our own PRNG because
/// <see cref="Random"/> does not guarantee the same sequence for a seed across .NET versions,
/// and seeded runs (and saves made from them) must stay reproducible.
/// </summary>
public sealed class Pcg32RandomSource : IRandomSource
{
    private const ulong Multiplier = 6364136223846793005UL;
    private const ulong DefaultSequence = 54UL;

    private readonly ulong _increment;
    private ulong _state;

    public Pcg32RandomSource(ulong seed, ulong sequence = DefaultSequence)
    {
        // Mirrors pcg32_srandom_r so outputs match the reference implementation.
        _increment = (sequence << 1) | 1UL;
        _state = 0UL;
        NextUInt32();
        _state = unchecked(_state + seed);
        NextUInt32();
    }

    public uint NextUInt32()
    {
        var oldState = _state;
        _state = unchecked((oldState * Multiplier) + _increment);
        var xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
        var rotation = (int)(oldState >> 59);
        return (xorShifted >> rotation) | (xorShifted << (-rotation & 31));
    }

    public int Next(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive must be greater than minInclusive.");
        }

        var range = (uint)((long)maxExclusive - minInclusive);

        // Rejection sampling removes modulo bias: discard the low values that would over-represent some results.
        var threshold = unchecked(0u - range) % range;
        while (true)
        {
            var value = NextUInt32();
            if (value >= threshold)
            {
                return (int)(minInclusive + (long)(value % range));
            }
        }
    }
}
