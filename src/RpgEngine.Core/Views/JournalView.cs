namespace RpgEngine.Core.Views;

public sealed record JournalView(IReadOnlyList<QuestLogEntry> Quests, IReadOnlyList<ReputationEntry> Reputation);
