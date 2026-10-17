namespace RpgEngine.Core.Persistence;

public sealed record SaveSlotInfo(string Slot, string PlayerName, int Level, string RoomId, DateTimeOffset SavedAtUtc);
