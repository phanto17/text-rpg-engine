namespace RpgEngine.Pixel.Layout;

/// <summary>Fits labels and the tail of an editable command using the active font's metrics.</summary>
public static class TextFit
{
    public static string Ellipsize(string text, int maxWidth, Func<string, int> measure)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(measure);
        if (maxWidth <= 0) return string.Empty;
        if (measure(text) <= maxWidth) return text;
        if (measure("..") > maxWidth) return string.Empty;
        var cut = text.Length;
        while (cut > 0 && measure(text[..cut].TrimEnd() + "..") > maxWidth) cut--;
        return text[..cut].TrimEnd() + "..";
    }

    public static string Tail(string text, int maxWidth, Func<string, int> measure)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(measure);
        if (maxWidth <= 0) return string.Empty;
        var start = 0;
        while (start < text.Length && measure(text[start..]) > maxWidth) start++;
        return text[start..];
    }
}
