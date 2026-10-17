namespace RpgEngine.Core.Content.Defs;

public sealed record ItemDef : IContentDef
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<string> Aliases { get; init; } = [];

    public required ItemType Type { get; init; }

    public required string Description { get; init; }

    public int Value { get; init; }

    public WeaponDef? Weapon { get; init; }

    public ArmorDef? Armor { get; init; }

    public ItemUseDef? Use { get; init; }
}
