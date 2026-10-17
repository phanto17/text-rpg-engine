namespace RpgEngine.Core.Dice;

/// <summary>Which dice of a term count toward the total (e.g. <c>4d6kh3</c> keeps the highest three).</summary>
public enum KeepRule
{
    All,
    Highest,
    Lowest,
}
