namespace RpgEngine.Pixel.Sequencing;

public enum LogLineKind
{
    Normal,

    /// <summary>A room or section title.</summary>
    Heading,

    /// <summary>A horizontal rule with the text set into it, e.g. COMBAT or Epilogue.</summary>
    Rule,

    /// <summary>A spoken line, drawn with a bar down its left edge.</summary>
    Quote,
}
