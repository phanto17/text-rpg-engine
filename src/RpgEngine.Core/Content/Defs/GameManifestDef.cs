namespace RpgEngine.Core.Content.Defs;

/// <summary><c>Data/game.json</c>: title, intro, start room, rule constants, and epilogues.</summary>
public sealed record GameManifestDef
{
    public required string Title { get; init; }

    public required string ContentVersion { get; init; }

    public required string StartRoom { get; init; }

    public required string Intro { get; init; }

    public int StartingGold { get; init; }

    public required RulesDef Rules { get; init; }

    public IReadOnlyList<EpilogueDef> Epilogues { get; init; } = [];

    /// <summary>Shown when the player dies.</summary>
    public string GameOverText { get; init; } = "You have fallen. Your story ends here.";
}
