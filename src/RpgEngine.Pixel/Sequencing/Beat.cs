namespace RpgEngine.Pixel.Sequencing;

/// <summary>
/// One step of the narrative: lines for the typewriter, cues for the renderer, and an optional pause
/// (<see cref="HoldSeconds"/>) played before the lines, so a dice banner finishes before its verdict is typed.
/// </summary>
public sealed record Beat(IReadOnlyList<LogLine> Lines, IReadOnlyList<SequenceCue> Cues, double HoldSeconds = 0)
{
    public static Beat Of(params LogLine[] lines) => new(lines, []);

    public static Beat Of(IReadOnlyList<LogLine> lines, params SequenceCue[] cues) => new(lines, cues);

    public static Beat Cue(params SequenceCue[] cues) => new([], cues);

    public Beat Holding(double seconds) => this with { HoldSeconds = seconds };
}
