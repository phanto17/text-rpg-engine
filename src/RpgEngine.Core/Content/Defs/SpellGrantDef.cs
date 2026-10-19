namespace RpgEngine.Core.Content.Defs;

/// <summary>Spells or techniques a class learns on reaching <see cref="Level"/>.</summary>
public sealed record SpellGrantDef
{
    public required int Level { get; init; }

    public required IReadOnlyList<string> Spells { get; init; }
}
