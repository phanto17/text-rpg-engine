namespace RpgEngine.Core.Persistence;

/// <summary>A save could not be written or read; the message is suitable for showing to the player.</summary>
public sealed class SaveGameException : Exception
{
    public SaveGameException(string message)
        : base(message)
    {
    }

    public SaveGameException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
