using RpgEngine.Core.Characters;

namespace RpgEngine.Core.Content.Defs;

/// <summary>A skill check (<c>skill</c>) or raw ability check (<c>ability</c>) against a DC.</summary>
public sealed record CheckDef
{
    public string? Skill { get; init; }

    public Ability? Ability { get; init; }

    public required int Dc { get; init; }

    public bool Advantage { get; init; }

    public bool Disadvantage { get; init; }

    public IReadOnlyList<ConditionDef> AdvantageIf { get; init; } = [];

    public IReadOnlyList<ConditionDef> DisadvantageIf { get; init; } = [];
}
