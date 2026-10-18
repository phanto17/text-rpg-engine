namespace RpgEngine.Core.Combat;

// Player combat actions are a closed family of small records, kept together in one file.

public abstract record PlayerAction;

public sealed record AttackAction(string TargetId) : PlayerAction;

/// <summary>Cast a spell or use a technique. <see cref="TargetId"/> is ignored for self-targeted ones.</summary>
public sealed record CastAction(string SpellId, string? TargetId) : PlayerAction;

/// <summary>Use a carried item. <see cref="TargetId"/> is ignored for self-targeted items.</summary>
public sealed record UseItemAction(string ItemId, string? TargetId = null) : PlayerAction;

public sealed record DefendAction : PlayerAction;

public sealed record ParleyAction(int OptionIndex) : PlayerAction;

public sealed record FleeAction : PlayerAction;
