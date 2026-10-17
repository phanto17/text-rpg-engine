namespace RpgEngine.Core.Views;

/// <summary>A started quest with the journal text for its current stage.</summary>
public sealed record QuestLogEntry(string Name, int Stage, string Journal, bool Completed);
