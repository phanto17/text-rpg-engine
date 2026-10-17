using RpgEngine.Core.World;

namespace RpgEngine.Core.Persistence;

/// <summary>A restored game. <see cref="ContentVersionMismatch"/> warns that the save was made with different content.</summary>
public sealed record LoadedGame(GameState State, SaveGame Save, bool ContentVersionMismatch);
