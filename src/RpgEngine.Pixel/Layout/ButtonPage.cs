namespace RpgEngine.Pixel.Layout;

/// <summary>Visible button rectangles with their starting index in the full action list.</summary>
public sealed record ButtonPage(int FirstAction, IReadOnlyList<PixelRect> Buttons)
{
    public int? ActionForShortcut(int number) => number >= 1 && number <= Buttons.Count
        ? FirstAction + number - 1 : null;
}
