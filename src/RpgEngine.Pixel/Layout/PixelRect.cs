namespace RpgEngine.Pixel.Layout;

/// <summary>An integer rectangle on the virtual canvas.</summary>
public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;

    public int Bottom => Y + Height;

    public int CenterX => X + (Width / 2);

    public int CenterY => Y + (Height / 2);

    public bool Contains(int x, int y) => x >= X && y >= Y && x < Right && y < Bottom;

    public PixelRect Inset(int amount) => new(X + amount, Y + amount, Math.Max(0, Width - (2 * amount)), Math.Max(0, Height - (2 * amount)));

    public PixelRect Inset(int horizontal, int vertical) =>
        new(X + horizontal, Y + vertical, Math.Max(0, Width - (2 * horizontal)), Math.Max(0, Height - (2 * vertical)));
}
