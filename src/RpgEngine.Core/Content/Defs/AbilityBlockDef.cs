using RpgEngine.Core.Characters;

namespace RpgEngine.Core.Content.Defs;

/// <summary>JSON shape <c>{ "str": 15, "dex": 10, ... }</c>.</summary>
public sealed record AbilityBlockDef
{
    public required int Str { get; init; }

    public required int Dex { get; init; }

    public required int Con { get; init; }

    public required int Int { get; init; }

    public required int Wis { get; init; }

    public required int Cha { get; init; }

    public AbilityScores ToScores() => new(Str, Dex, Con, Int, Wis, Cha);
}
