namespace RpgEngine.Core.Content.Defs;

/// <summary>
/// A protagonist's background (a "burden"): who they were before the barrow. It grants one bonus
/// skill on top of the class picks, optional extra gold, and a keepsake item.
/// </summary>
public sealed record BackgroundDef : IContentDef
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required string BonusSkill { get; init; }

    public int BonusGold { get; init; }

    public required string KeepsakeItem { get; init; }
}
