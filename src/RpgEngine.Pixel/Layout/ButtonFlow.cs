namespace RpgEngine.Pixel.Layout;

/// <summary>Lays buttons out left to right, wrapping into new rows, inside an area. Buttons that do not fit get no rectangle.</summary>
public static class ButtonFlow
{
    /// <summary>Partitions all choices into visible pages, retaining their original action indices.</summary>
    public static IReadOnlyList<ButtonPage> Pages(IReadOnlyList<string> labels, PixelRect area,
        Func<string, int> measure, int rowHeight, int gap, int padding, int maxButtons = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(measure);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rowHeight);
        ArgumentOutOfRangeException.ThrowIfNegative(gap);
        ArgumentOutOfRangeException.ThrowIfNegative(padding);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxButtons);
        if (area.Width <= 0 || area.Height < rowHeight)
            throw new ArgumentException("The area must fit at least one button.", nameof(area));

        var pages = new List<ButtonPage>();
        var first = 0;
        while (first < labels.Count)
        {
            var rectangles = Flow(labels.Skip(first).Take(maxButtons).ToArray(), area, measure, rowHeight, gap, padding)
                .TakeWhile(rect => rect.HasValue).Select(rect => rect!.Value).ToArray();
            pages.Add(new ButtonPage(first, rectangles));
            first += rectangles.Length;
        }
        return pages;
    }

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
