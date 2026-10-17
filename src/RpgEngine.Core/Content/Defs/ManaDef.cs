using RpgEngine.Core.Characters;

namespace RpgEngine.Core.Content.Defs;

/// <summary>Max mana = <see cref="Base"/> + modifier of <see cref="Ability"/> (also the casting ability).</summary>
public sealed record ManaDef
{
    public required int Base { get; init; }

    public required Ability Ability { get; init; }
}
