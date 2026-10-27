using RpgEngine.Core.Dice;

namespace RpgEngine.Pixel.Sequencing;

// Cues are the closed family of visual effects a beat asks the renderer for, kept together in one
// file like the game events they come from. The sequencer hands them out as each beat starts.

public abstract record SequenceCue;

public enum DiceOutcome
{
    Success,
    Failure,
    CriticalSuccess,
    CriticalFailure,
}

/// <summary>Play the d20 banner: <see cref="Label"/> is what was rolled for, <see cref="Versus"/> reads "DC 14" or "AC 13".</summary>
public sealed record DiceBannerCue(string Label, D20Roll Roll, int Bonus, int Total, string Versus, DiceOutcome Outcome) : SequenceCue;

/// <summary>Flash the combatant white (or the screen edges red for the player).</summary>
public sealed record HitFlashCue(string Target, bool IsPlayer) : SequenceCue;

/// <summary>Jolt the attacker toward its target.</summary>
public sealed record LungeCue(string Attacker, bool IsPlayer, string Target) : SequenceCue;

public enum FloatingKind
{
    Damage,
    Heal,
    Miss,
    Status,
}

/// <summary>A number or word that rises from a combatant and fades.</summary>
public sealed record FloatingNumberCue(string Target, bool IsPlayer, string Text, FloatingKind Kind) : SequenceCue;

/// <summary>The combatant goes down (or breaks away, when <see cref="Fled"/>).</summary>
public sealed record FellCue(string Name, bool IsPlayer, bool Fled) : SequenceCue;

/// <summary>Someone is speaking: slide in their portrait.</summary>
public sealed record SpeakerCue(string Speaker) : SequenceCue;

/// <summary>The scene changed to another room.</summary>
public sealed record RoomCue(string RoomId) : SequenceCue;

public sealed record CombatCue(bool Started) : SequenceCue;

public sealed record LevelUpCue(int Level) : SequenceCue;

/// <summary>The room description and its details have finished revealing.</summary>
public sealed record RoomRevealedCue(string RoomId) : SequenceCue;
