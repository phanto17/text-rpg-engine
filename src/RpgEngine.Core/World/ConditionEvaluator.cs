using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Core.World;

public static class ConditionEvaluator
{
    /// <summary>True when every condition passes (an empty list always passes).</summary>
    public static bool AllPass(IReadOnlyList<ConditionDef> conditions, GameState state)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(state);
        return conditions.All(condition => Passes(condition, state));
    }

    public static bool Passes(ConditionDef condition, GameState state) => condition switch
    {
        FlagCondition c => state.World.HasFlag(c.Flag),
        NotFlagCondition c => !state.World.HasFlag(c.Flag),
        HasItemCondition c => state.Player.Inventory.Contains(c.Item, c.Count),
        LacksItemCondition c => !state.Player.Inventory.Contains(c.Item),
        CounterCondition c => InBounds(state.World.GetCounter(c.Counter), c.Min, c.Max, c.EqualTo),
        ReputationCondition c => InBounds(state.World.GetReputation(c.Faction), c.Min, c.Max, null),
        QuestStageCondition c => InBounds(state.World.GetQuestStage(c.Quest), c.Min, c.Max, c.EqualTo),
        ClassCondition c => string.Equals(state.Player.ClassId, c.ClassId, StringComparison.Ordinal),
        AbilityScoreCondition c => state.Player.Abilities[c.Ability] >= c.Min,
        VisitedCondition c => state.World.HasVisited(c.Room),
        AllCondition c => AllPass(c.Conditions, state),
        AnyCondition c => c.Conditions.Any(inner => Passes(inner, state)),
        NotCondition c => !AllPass(c.Conditions, state),
        _ => throw new NotSupportedException($"Unsupported condition type '{condition.GetType().Name}'."),
    };

    private static bool InBounds(int value, int? min, int? max, int? equalTo) =>
        (min is null || value >= min) && (max is null || value <= max) && (equalTo is null || value == equalTo);
}
