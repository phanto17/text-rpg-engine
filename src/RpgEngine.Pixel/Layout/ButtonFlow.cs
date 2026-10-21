namespace RpgEngine.Pixel.Layout;

/// <summary>Lays buttons out left to right, wrapping into new rows, inside an area. Buttons that do not fit get no rectangle.</summary>
public static class ButtonFlow
{
    /// <summary>
    /// One rectangle per label (null when the area is full). Each button is as wide as its text plus
    /// padding on both sides; rows are <paramref name="rowHeight"/> tall and <paramref name="gap"/> apart.
    /// </summary>
    public static IReadOnlyList<PixelRect?> Flow(IReadOnlyList<string> labels, PixelRect area, Func<string, int> measure, int rowHeight, int gap, int padding)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(measure);
        var result = new List<PixelRect?>(labels.Count);
        var x = area.X;
        var y = area.Y;

        foreach (var label in labels)
        {
            var width = measure(label) + (2 * padding);
            if (x > area.X && x + width > area.Right)
            {
                x = area.X;
                y += rowHeight + gap;
            }

            if (y + rowHeight > area.Bottom)
            {
                result.Add(null);
                continue;
            }

            result.Add(new PixelRect(x, y, Math.Min(width, area.Width), rowHeight));
            x += width + gap;
        }

        return result;
    }
}
