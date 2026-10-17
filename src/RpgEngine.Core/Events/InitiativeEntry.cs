namespace RpgEngine.Core.Events;

public sealed record InitiativeEntry(string Name, bool IsPlayer, int Natural, int Modifier, int Total);
