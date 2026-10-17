using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Core.Combat;

/// <summary>A status effect on a combatant, counting down the affected creature's own turns.</summary>
public sealed class ActiveStatus
{
    internal ActiveStatus(StatusEffectDef definition, bool appliedDuringOwnTurn)
    {
        Definition = definition;
        RemainingTurns = definition.DurationRounds;
        AppliedDuringOwnTurn = appliedDuringOwnTurn;
    }

    public StatusEffectDef Definition { get; }

    public int RemainingTurns { get; internal set; }

    /// <summary>True until the end of the turn it was applied in, so that turn doesn't count.</summary>
    internal bool AppliedDuringOwnTurn { get; set; }
}
