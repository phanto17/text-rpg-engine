using RpgEngine.Core.Characters;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.World;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.World;

public sealed class ConditionEvaluatorTests
{
    private readonly TestWorld _game = new(player: TestCharacters.Create(abilities: TestCharacters.Scores(str: 16), classId: "fighter"));

    private bool Passes(ConditionDef condition) => ConditionEvaluator.Passes(condition, _game.State);

    [Fact]
    public void EmptyList_Passes()
    {
        Assert.True(ConditionEvaluator.AllPass([], _game.State));
    }

    [Fact]
    public void Flags()
    {
        Assert.False(Passes(new FlagCondition { Flag = "lit" }));
        Assert.True(Passes(new NotFlagCondition { Flag = "lit" }));

        _game.World.SetFlag("lit");

        Assert.True(Passes(new FlagCondition { Flag = "lit" }));
        Assert.False(Passes(new NotFlagCondition { Flag = "lit" }));
    }

    [Fact]
    public void Items()
    {
        _game.Player.Inventory.Add("potion", 2);

        Assert.True(Passes(new HasItemCondition { Item = "potion" }));
        Assert.True(Passes(new HasItemCondition { Item = "potion", Count = 2 }));
        Assert.False(Passes(new HasItemCondition { Item = "potion", Count = 3 }));
        Assert.False(Passes(new LacksItemCondition { Item = "potion" }));
        Assert.True(Passes(new LacksItemCondition { Item = "key" }));
    }

    [Theory]
    [InlineData(3, 2, null, null, true)]
    [InlineData(3, 4, null, null, false)]
    [InlineData(3, null, 3, null, true)]
    [InlineData(3, null, 2, null, false)]
    [InlineData(3, null, null, 3, true)]
    [InlineData(3, null, null, 2, false)]
    [InlineData(3, 1, 5, 3, true)]
    public void Counters_RespectEveryBound(int value, int? min, int? max, int? equalTo, bool expected)
    {
        _game.World.SetCounter("levers", value);

        Assert.Equal(expected, Passes(new CounterCondition { Counter = "levers", Min = min, Max = max, EqualTo = equalTo }));
    }

    [Fact]
    public void Reputation()
    {
        _game.World.ChangeReputation("order", 10);

        Assert.True(Passes(new ReputationCondition { Faction = "order", Min = 10 }));
        Assert.False(Passes(new ReputationCondition { Faction = "order", Max = 5 }));
        Assert.True(Passes(new ReputationCondition { Faction = "guild", Max = 0 }));
    }

    [Theory]
    [InlineData(10, 10, null, true)]
    [InlineData(9, 10, null, false)]
    [InlineData(5, null, 5, true)]
    [InlineData(6, null, 5, false)]
    public void Gold_RespectsMinAndMax(int gold, int? min, int? max, bool expected)
    {
        _game.Player.AddGold(gold);

        Assert.Equal(expected, Passes(new GoldCondition { Min = min, Max = max }));
    }

    [Fact]
    public void QuestStage()
    {
        _game.World.AdvanceQuest("main", 10);

        Assert.True(Passes(new QuestStageCondition { Quest = "main", EqualTo = 10 }));
        Assert.True(Passes(new QuestStageCondition { Quest = "main", Min = 5, Max = 10 }));
        Assert.False(Passes(new QuestStageCondition { Quest = "main", Min = 100 }));
    }

    [Fact]
    public void ClassAndAbility()
    {
        Assert.True(Passes(new ClassCondition { ClassId = "fighter" }));
        Assert.False(Passes(new ClassCondition { ClassId = "wizard" }));
        Assert.True(Passes(new BackgroundCondition { BackgroundId = "orphan" }));
        Assert.False(Passes(new BackgroundCondition { BackgroundId = "bailiff" }));
        Assert.True(Passes(new AbilityScoreCondition { Ability = Ability.Str, Min = 16 }));
        Assert.False(Passes(new AbilityScoreCondition { Ability = Ability.Str, Min = 17 }));
    }

    [Fact]
    public void Visited()
    {
        Assert.False(Passes(new VisitedCondition { Room = "hall" }));

        _game.World.MarkVisited("hall");

        Assert.True(Passes(new VisitedCondition { Room = "hall" }));
    }

    [Fact]
    public void Composites()
    {
        _game.World.SetFlag("a");
        ConditionDef a = new FlagCondition { Flag = "a" };
        ConditionDef b = new FlagCondition { Flag = "b" };

        Assert.False(Passes(new AllCondition { Conditions = [a, b] }));
        Assert.True(Passes(new AllCondition { Conditions = [a] }));
        Assert.True(Passes(new AnyCondition { Conditions = [a, b] }));
        Assert.False(Passes(new AnyCondition { Conditions = [b] }));
        Assert.True(Passes(new NotCondition { Conditions = [a, b] }));
        Assert.False(Passes(new NotCondition { Conditions = [a] }));
    }

    [Fact]
    public void AllPass_RequiresEveryCondition()
    {
        _game.World.SetFlag("a");

        Assert.True(ConditionEvaluator.AllPass([new FlagCondition { Flag = "a" }, new NotFlagCondition { Flag = "b" }], _game.State));
        Assert.False(ConditionEvaluator.AllPass([new FlagCondition { Flag = "a" }, new FlagCondition { Flag = "b" }], _game.State));
    }

    [Fact]
    public void AllPass_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => ConditionEvaluator.AllPass(null!, _game.State));
        Assert.Throws<ArgumentNullException>(() => ConditionEvaluator.AllPass([], null!));
    }

    [Fact]
    public void UnknownConditionType_Throws()
    {
        Assert.Throws<NotSupportedException>(() => Passes(new UnknownCondition()));
    }

    private sealed record UnknownCondition : ConditionDef;
}
