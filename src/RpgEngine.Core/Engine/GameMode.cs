namespace RpgEngine.Core.Engine;

public enum GameMode
{
    Exploration,
    Dialogue,
    Combat,

    /// <summary>The player has died.</summary>
    GameOver,

    /// <summary>The adventure reached an epilogue.</summary>
    Ended,
}
