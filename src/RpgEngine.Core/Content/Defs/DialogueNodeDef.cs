namespace RpgEngine.Core.Content.Defs;

/// <summary>
/// One line of dialogue. Effects run on entry. A node either offers <see cref="Choices"/>,
/// continues automatically to <see cref="Next"/>, or (with neither) ends the conversation.
/// </summary>
public sealed record DialogueNodeDef
{
    public required string Id { get; init; }

    public required string Text { get; init; }

    public IReadOnlyList<EffectDef> Effects { get; init; } = [];

    public IReadOnlyList<DialogueChoiceDef> Choices { get; init; } = [];

    public string? Next { get; init; }
}
