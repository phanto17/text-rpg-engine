namespace RpgEngine.Pixel.Layout;

/// <summary>
/// The 480 by 270 virtual canvas and its four panels: the scene viewport and the character HUD on
/// top, the narrative log beneath the scene, and the action bar with the command line along the bottom.
/// </summary>
public static class PixelLayout
{
    public const int Width = 480;
    public const int Height = 270;

    /// <summary>Silkscreen's raster size on the virtual canvas; output scales by whole pixels.</summary>
    public const int FontSize = 10;
    public const int LineHeight = 11;

    public static readonly PixelRect Canvas = new(0, 0, Width, Height);

    public static readonly PixelRect Scene = new(0, 0, 304, 140);

    public static readonly PixelRect Hud = new(304, 0, 176, 216);

    public static readonly PixelRect Log = new(0, 140, 304, 76);

    public static readonly PixelRect ActionBar = new(0, 216, 480, 54);

    /// <summary>The question or reminder above the buttons.</summary>
    public static readonly PixelRect Prompt = new(3, 217, 474, 10);

    /// <summary>Three rows of buttons inside the action bar.</summary>
    public static readonly PixelRect Buttons = new(2, 228, 476, 32);

    public static readonly PixelRect Input = new(2, 260, 476, 10);

    public const int ButtonHeight = 10;
    public const int ButtonGap = 1;
    public const int ButtonPadding = 3;
}
