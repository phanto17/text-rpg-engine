namespace RpgEngine.Core.Commands;

public enum CommandVerb
{
    Empty,
    Look,
    Go,
    Inspect,
    Take,
    Drop,
    Use,
    Equip,
    Talk,
    Inventory,
    Stats,
    Journal,
    Save,
    Load,
    Help,
    Quit,

    /// <summary>A verb defined by content (e.g. <c>press</c>, <c>read</c>), resolved against room features.</summary>
    Interact,
}
