namespace RpgEngine.Pixel.Sequencing;

/// <summary>One line of the narrative log, as a list of toned spans. The typewriter reveals it a character at a time.</summary>
public sealed record LogLine(IReadOnlyList<LogSpan> Spans, LogLineKind Kind = LogLineKind.Normal)
{
    public static readonly LogLine Blank = new([]);

    public int Length => Spans.Sum(span => span.Text.Length);

    public string Text => string.Concat(Spans.Select(span => span.Text));

    public static LogLine Of(string text, LogTone tone = LogTone.Narration, LogLineKind kind = LogLineKind.Normal) =>
        new([new LogSpan(text, tone)], kind);

    /// <summary>Starts a mixed-tone line: <c>LogLine.Build().Add("You ", LogTone.Accent).Add("take 3 damage", LogTone.Failure).Done()</c>.</summary>
    public static LineBuilder Build(LogLineKind kind = LogLineKind.Normal) => new(kind);

    public sealed class LineBuilder(LogLineKind kind)
    {
        private readonly List<LogSpan> _spans = [];

        public LineBuilder Add(string text, LogTone tone = LogTone.Narration)
        {
            if (text.Length > 0)
            {
                _spans.Add(new LogSpan(text, tone));
            }

            return this;
        }

        public LogLine Done() => new(_spans, kind);
    }
}
