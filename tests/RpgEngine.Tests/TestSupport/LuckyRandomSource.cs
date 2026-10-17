using RpgEngine.Core.Dice;

namespace RpgEngine.Tests.TestSupport;

/// <summary>Always returns the highest value: every d20 is a natural 20 and every damage die rolls its maximum.</summary>
internal sealed class LuckyRandomSource : IRandomSource
{
    public int Next(int minInclusive, int maxExclusive) => maxExclusive - 1;
}
