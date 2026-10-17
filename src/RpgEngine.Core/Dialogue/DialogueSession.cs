using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Core.Dialogue;

/// <summary>An ongoing conversation. It is over once <see cref="CurrentNode"/> is null.</summary>
public sealed class DialogueSession
{
    internal DialogueSession(DialogueDef dialogue)
    {
        Dialogue = dialogue;
    }

    public DialogueDef Dialogue { get; }

    /// <summary>The node whose choices are waiting for the player.</summary>
    public DialogueNodeDef? CurrentNode { get; internal set; }

    public bool IsOver => CurrentNode is null;
}
