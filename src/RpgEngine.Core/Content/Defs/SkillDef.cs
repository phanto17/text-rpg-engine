using RpgEngine.Core.Characters;

namespace RpgEngine.Core.Content.Defs;

public sealed record SkillDef : IContentDef
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required Ability Ability { get; init; }
}
