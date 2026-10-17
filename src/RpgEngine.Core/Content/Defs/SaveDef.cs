using RpgEngine.Core.Characters;

namespace RpgEngine.Core.Content.Defs;

public sealed record SaveDef
{
    public required Ability Ability { get; init; }

    public required int Dc { get; init; }
}
