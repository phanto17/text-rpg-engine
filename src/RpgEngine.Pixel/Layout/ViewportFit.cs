namespace RpgEngine.Pixel.Layout;

/// <summary>
/// Where the virtual canvas lands inside the window: scaled by a whole number (crisp pixels,
/// letterboxed) or by the largest fraction that keeps the 16:9 aspect, always centered.
/// </summary>
public static class ViewportFit
{
    public static PixelRect Fit(int windowWidth, int windowHeight, bool integerScale)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windowWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windowHeight);

        var scale = Math.Min(windowWidth / (double)PixelLayout.Width, windowHeight / (double)PixelLayout.Height);
        if (integerScale)
        {
            scale = Math.Max(1, Math.Floor(scale));
        }

        var width = (int)Math.Round(PixelLayout.Width * scale);
        var height = (int)Math.Round(PixelLayout.Height * scale);
        return new PixelRect((windowWidth - width) / 2, (windowHeight - height) / 2, width, height);
    }

    /// <summary>Maps a window position onto the canvas; positions in the letterbox fall outside 0..479 / 0..269.</summary>
    public static (int X, int Y) ToCanvas(PixelRect viewport, double windowX, double windowY)
    {
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return (-1, -1);
        }

        var x = (int)Math.Floor((windowX - viewport.X) * PixelLayout.Width / viewport.Width);
        var y = (int)Math.Floor((windowY - viewport.Y) * PixelLayout.Height / viewport.Height);
        return (x, y);
    }
}
