using RpgEngine.Core.Checks;
using RpgEngine.Core.Combat;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Views;

namespace RpgEngine.Core.Events;

// Game events are a closed family of small records, kept together in one file.
// Core never prints: it emits these in order and the UI renders them.

public abstract record GameEvent;

/// <summary>A paragraph of text with a semantic style.</summary>
public sealed record NarrationEvent(TextStyle Style, string Text) : GameEvent;

/// <summary>The player entered (<see cref="Entered"/>) or looked around a room.</summary>
public sealed record RoomDescribedEvent(RoomView Room, bool Entered) : GameEvent;

/// <summary>A line spoken by an NPC.</summary>
public sealed record DialogueLineEvent(string Speaker, string Text) : GameEvent;

/// <summary>A resolved check or saving throw, with its full breakdown.</summary>
public sealed record CheckRolledEvent(CheckResult Result) : GameEvent;

/// <summary>A non-d20 roll worth showing (healing, damage, ability scores).</summary>
public sealed record DiceRolledEvent(string Label, RollResult Roll) : GameEvent;

public sealed record ItemGainedEvent(ItemDef Item, int Count) : GameEvent;

public sealed record ItemLostEvent(ItemDef Item, int Count) : GameEvent;

public sealed record GoldGainedEvent(int Amount, int Total) : GameEvent;

public sealed record XpGainedEvent(int Amount, int Total) : GameEvent;

public sealed record DamageTakenEvent(
    string Target,
    bool IsPlayer,
    int Amount,
    DamageType? DamageType,
    DamageModifier Modifier,
    int Hp,
    int MaxHp) : GameEvent;

public sealed record HealedEvent(string Target, bool IsPlayer, int Amount, int Hp, int MaxHp) : GameEvent;

public sealed record ManaChangedEvent(int Delta, int Mana, int MaxMana) : GameEvent;

public sealed record ReputationChangedEvent(FactionDef Faction, int Delta, int Value) : GameEvent;

public sealed record QuestUpdatedEvent(QuestDef Quest, QuestStageDef Stage) : GameEvent;

public sealed record CombatStartedEvent(string? Intro, IReadOnlyList<CombatantView> Combatants) : GameEvent;

/// <summary>Initiative results, already in turn order.</summary>
public sealed record InitiativeRolledEvent(IReadOnlyList<InitiativeEntry> Order) : GameEvent;

public sealed record TurnStartedEvent(string Name, bool IsPlayer, int Round) : GameEvent;

public sealed record AttackRolledEvent(
    string Attacker,
    string Target,
    string AttackName,
    D20Roll Roll,
    int Bonus,
    int Total,
    int TargetArmorClass,
    bool Hit,
    bool Critical) : GameEvent;

public sealed record SpellCastEvent(string Caster, string SpellName, SpellKind Kind) : GameEvent;

public sealed record StatusAppliedEvent(string Target, string StatusName) : GameEvent;

public sealed record StatusExpiredEvent(string Target, string StatusName) : GameEvent;

/// <summary>A combatant loses its turn to a status (e.g. stunned).</summary>
public sealed record TurnSkippedEvent(string Name, string StatusName) : GameEvent;

public sealed record CombatantDefeatedEvent(string Name, bool IsPlayer) : GameEvent;

public sealed record CombatantFledEvent(string Name, bool IsPlayer) : GameEvent;

public sealed record CombatEndedEvent(CombatOutcome Outcome) : GameEvent;

/// <summary>The adventure is over; <see cref="Text"/> is the epilogue chosen by the world state.</summary>
public sealed record EpilogueEvent(string Text) : GameEvent;

/// <summary>The player has fallen.</summary>
public sealed record PlayerDefeatedEvent(string Text) : GameEvent;
