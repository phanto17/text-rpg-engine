using System.Text.Json.Serialization;
using RpgEngine.Core.Characters;

namespace RpgEngine.Core.Content.Defs;

// Conditions are a small closed family of records, kept together in one file.
// Every entry in a condition list must pass.

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FlagCondition), "flag")]
[JsonDerivedType(typeof(NotFlagCondition), "notFlag")]
[JsonDerivedType(typeof(HasItemCondition), "hasItem")]
[JsonDerivedType(typeof(LacksItemCondition), "lacksItem")]
[JsonDerivedType(typeof(CounterCondition), "counter")]
[JsonDerivedType(typeof(ReputationCondition), "reputation")]
[JsonDerivedType(typeof(GoldCondition), "gold")]
[JsonDerivedType(typeof(QuestStageCondition), "questStage")]
[JsonDerivedType(typeof(ClassCondition), "class")]
[JsonDerivedType(typeof(BackgroundCondition), "background")]
[JsonDerivedType(typeof(AbilityScoreCondition), "abilityScore")]
[JsonDerivedType(typeof(VisitedCondition), "visited")]
[JsonDerivedType(typeof(AllCondition), "all")]
[JsonDerivedType(typeof(AnyCondition), "any")]
[JsonDerivedType(typeof(NotCondition), "not")]
public abstract record ConditionDef;

public sealed record FlagCondition : ConditionDef
{
    public required string Flag { get; init; }
}

public sealed record NotFlagCondition : ConditionDef
{
    public required string Flag { get; init; }
}

public sealed record HasItemCondition : ConditionDef
{
    public required string Item { get; init; }

    public int Count { get; init; } = 1;
}

public sealed record LacksItemCondition : ConditionDef
{
    public required string Item { get; init; }
}

/// <summary>Passes when the counter (default 0) satisfies every bound given.</summary>
public sealed record CounterCondition : ConditionDef
{
    public required string Counter { get; init; }

    public int? Min { get; init; }

    public int? Max { get; init; }

    [JsonPropertyName("equals")]
    public int? EqualTo { get; init; }
}

public sealed record ReputationCondition : ConditionDef
{
    public required string Faction { get; init; }

    public int? Min { get; init; }

    public int? Max { get; init; }
}

/// <summary>Passes when the quest stage (default 0) satisfies every bound given.</summary>
public sealed record GoldCondition : ConditionDef
{
    public int? Min { get; init; }

    public int? Max { get; init; }
}

public sealed record QuestStageCondition : ConditionDef
{
    public required string Quest { get; init; }

    public int? Min { get; init; }

    public int? Max { get; init; }

    [JsonPropertyName("equals")]
    public int? EqualTo { get; init; }
}

public sealed record ClassCondition : ConditionDef
{
    [JsonPropertyName("class")]
    public required string ClassId { get; init; }
}

public sealed record BackgroundCondition : ConditionDef
{
    [JsonPropertyName("background")]
    public required string BackgroundId { get; init; }
}

public sealed record AbilityScoreCondition : ConditionDef
{
    public required Ability Ability { get; init; }

    public required int Min { get; init; }
}

public sealed record VisitedCondition : ConditionDef
{
    public required string Room { get; init; }
}

public sealed record AllCondition : ConditionDef
{
    public required IReadOnlyList<ConditionDef> Conditions { get; init; }
}

public sealed record AnyCondition : ConditionDef
{
    public required IReadOnlyList<ConditionDef> Conditions { get; init; }
}

/// <summary>Passes when the inner list (all must pass) does not.</summary>
public sealed record NotCondition : ConditionDef
{
    public required IReadOnlyList<ConditionDef> Conditions { get; init; }
}
