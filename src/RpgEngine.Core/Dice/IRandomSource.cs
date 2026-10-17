namespace RpgEngine.Core.Dice;

/// <summary>
/// Source of uniformly distributed integers. Every random decision in the engine goes through
/// this interface so runs can be seeded and tests can script exact outcomes.
/// </summary>
public interface IRandomSource
{
    /// <summary>Returns an integer in the range [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).</summary>
    int Next(int minInclusive, int maxExclusive);
}
