namespace RpgEngine.Core.Events;

// Transitions are a closed family of small records, kept together in one file.
// Effects request them; the game session carries them out after the effect list finishes.

public abstract record GameTransition;

public sealed record MoveToTransition(string RoomId) : GameTransition;

public sealed record StartDialogueTransition(string DialogueId) : GameTransition;

public sealed record StartCombatTransition(string EncounterId) : GameTransition;

public sealed record EndGameTransition : GameTransition;
