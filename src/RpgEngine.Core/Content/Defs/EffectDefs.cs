using System.Text.Json.Serialization;
using RpgEngine.Core.Characters;

namespace RpgEngine.Core.Content.Defs;

// Effects are a small closed family of records, kept together in one file.
// Effect lists run in order.

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SetFlagEffect), "setFlag")]
[JsonDerivedType(typeof(ClearFlagEffect), "clearFlag")]
[JsonDerivedType(typeof(SetCounterEffect), "setCounter")]
[JsonDerivedType(typeof(AddCounterEffect), "addCounter")]
[JsonDerivedType(typeof(ChangeReputationEffect), "changeReputation")]
[JsonDerivedType(typeof(SetQuestStageEffect), "setQuestStage")]
[JsonDerivedType(typeof(GiveItemEffect), "giveItem")]
[JsonDerivedType(typeof(RemoveItemEffect), "removeItem")]
[JsonDerivedType(typeof(GiveGoldEffect), "giveGold")]
[JsonDerivedType(typeof(GrantXpEffect), "grantXp")]
[JsonDerivedType(typeof(HealEffect), "heal")]
[JsonDerivedType(typeof(DamageEffect), "damage")]
[JsonDerivedType(typeof(RestoreManaEffect), "restoreMana")]
[JsonDerivedType(typeof(RestEffect), "rest")]
[JsonDerivedType(typeof(SavingThrowEffect), "savingThrow")]
[JsonDerivedType(typeof(SkillCheckEffect), "skillCheck")]
[JsonDerivedType(typeof(MessageEffect), "message")]
[JsonDerivedType(typeof(MoveToEffect), "moveTo")]
[JsonDerivedType(typeof(StartDialogueEffect), "startDialogue")]
[JsonDerivedType(typeof(StartCombatEffect), "startCombat")]
[JsonDerivedType(typeof(StartShopEffect), "startShop")]
[JsonDerivedType(typeof(ApplyStatusEffect), "applyStatus")]
[JsonDerivedType(typeof(EndGameEffect), "endGame")]
[JsonDerivedType(typeof(IfEffect), "if")]
public abstract record EffectDef;

public sealed record SetFlagEffect : EffectDef
{
    public required string Flag { get; init; }
}

public sealed record ClearFlagEffect : EffectDef
{
    public required string Flag { get; init; }
}

public sealed record SetCounterEffect : EffectDef
{
    public required string Counter { get; init; }

    public required int Value { get; init; }
}

public sealed record AddCounterEffect : EffectDef
{
    public required string Counter { get; init; }

    public required int Amount { get; init; }
}

public sealed record ChangeReputationEffect : EffectDef
{
    public required string Faction { get; init; }

    public required int Amount { get; init; }
}

/// <summary>Advances a quest; stages never move backwards.</summary>
public sealed record SetQuestStageEffect : EffectDef
{
    public required string Quest { get; init; }

    public required int Stage { get; init; }
}

public sealed record GiveItemEffect : EffectDef
{
    public required string Item { get; init; }

    public int Count { get; init; } = 1;
}

public sealed record RemoveItemEffect : EffectDef
{
    public required string Item { get; init; }

    public int Count { get; init; } = 1;
}

public sealed record GiveGoldEffect : EffectDef
{
    public required int Amount { get; init; }
}

public sealed record GrantXpEffect : EffectDef
{
    public required int Amount { get; init; }
}

public sealed record HealEffect : EffectDef
{
    public required string Dice { get; init; }
}

public sealed record DamageEffect : EffectDef
{
    public required string Dice { get; init; }

    public DamageType? DamageType { get; init; }
}

public sealed record RestoreManaEffect : EffectDef
{
    public required int Amount { get; init; }
}

public sealed record SavingThrowEffect : EffectDef
{
    public required Ability Ability { get; init; }

    public required int Dc { get; init; }

    public IReadOnlyList<EffectDef> OnSuccess { get; init; } = [];

    public IReadOnlyList<EffectDef> OnFail { get; init; } = [];
}

public sealed record SkillCheckEffect : EffectDef
{
    public required CheckDef Check { get; init; }

    public IReadOnlyList<EffectDef> OnSuccess { get; init; } = [];

    public IReadOnlyList<EffectDef> OnFail { get; init; } = [];
}

public sealed record MessageEffect : EffectDef
{
    public required string Text { get; init; }

    public TextStyle Style { get; init; } = TextStyle.Narration;
}

public sealed record MoveToEffect : EffectDef
{
    public required string Room { get; init; }
}

public sealed record StartDialogueEffect : EffectDef
{
    public required string Dialogue { get; init; }
}

public sealed record StartCombatEffect : EffectDef
{
    public required string Encounter { get; init; }
}

/// <summary>Opens a shop's buy and sell screen.</summary>
public sealed record StartShopEffect : EffectDef
{
    public required string Shop { get; init; }
}

/// <summary>
/// Puts a status effect on the player, such as a self-buff from a technique or a combat consumable.
/// Statuses exist only in combat, so outside a fight this does nothing.
/// </summary>
public sealed record ApplyStatusEffect : EffectDef
{
    public required string Status { get; init; }
}

/// <summary>A full rest at a sanctuary: restores hit points and mana to their maximums.</summary>
public sealed record RestEffect : EffectDef;

/// <summary>Ends the adventure and shows the first epilogue whose conditions pass.</summary>
public sealed record EndGameEffect : EffectDef;

/// <summary>Runs <see cref="Then"/> when the conditions pass, otherwise <see cref="Else"/>.</summary>
public sealed record IfEffect : EffectDef
{
    public required IReadOnlyList<ConditionDef> Conditions { get; init; }

    public IReadOnlyList<EffectDef> Then { get; init; } = [];

    public IReadOnlyList<EffectDef> Else { get; init; } = [];
}
