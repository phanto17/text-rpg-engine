using RpgEngine.Core.Characters;

namespace RpgEngine.Core.World;

/// <summary>The complete mutable state of a playthrough: the player, the world, and where the player stands.</summary>
public sealed class GameState
{
    public GameState(PlayerCharacter player, WorldState world, string currentRoomId)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentRoomId);
        Player = player;
        World = world;
        CurrentRoomId = currentRoomId;
    }

    public PlayerCharacter Player { get; }

    public WorldState World { get; }

    public string CurrentRoomId { get; set; }
}
