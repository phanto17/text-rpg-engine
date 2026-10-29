using System.Text;
using System.Numerics;
using Raylib_cs;
using RpgEngine.Pixel.Layout;

namespace RpgEngine.Pixel.Rendering;

/// <summary>Small drawing helpers over raylib for the virtual canvas: pixel text, frames, and bars.</summary>
internal static class Canvas
{
    private static readonly Lazy<Font?> UiFont = new(() =>
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "Assets", "Fonts", "Silkscreen-Regular.ttf");
        if (!File.Exists(path))
        {
            return null;
        }

        var loaded = Raylib.LoadFontEx(path, PixelLayout.FontSize, null!, 0);
        if (!Raylib.IsFontValid(loaded))
        {
            return null;
        }

        Raylib.SetTextureFilter(loaded.Texture, TextureFilter.Point);
        return loaded;
    });

    public static void Text(int x, int y, string text, Color color) =>
        Draw(x, y, Clean(text), color);

    private static void Draw(int x, int y, string clean, Color color)
    {
        if (UiFont.Value is { } font)
        {
            Raylib.DrawTextEx(font, clean, new Vector2(x, y), PixelLayout.FontSize, 0, color);
        }
        else
        {
            Raylib.DrawText(clean, x, y, PixelLayout.FontSize, color);
        }
    }

    /// <summary>Text drawn twice, one pixel apart, for a heavier weight.</summary>
    public static void BoldText(int x, int y, string text, Color color)
    {
        var clean = Clean(text);
        Draw(x + 1, y, clean, color);
        Draw(x, y, clean, color);
    }

    public static int Width(string text) => UiFont.Value is { } font
        ? (int)Math.Ceiling(Raylib.MeasureTextEx(font, Clean(text), PixelLayout.FontSize, 0).X)
        : Raylib.MeasureText(Clean(text), PixelLayout.FontSize);

    public static void Fill(PixelRect rect, Color color) => Raylib.DrawRectangle(rect.X, rect.Y, rect.Width, rect.Height, color);

    public static void Frame(PixelRect rect, Color color) => Raylib.DrawRectangleLines(rect.X, rect.Y, rect.Width, rect.Height, color);

    public static void Panel(PixelRect rect, Color back, Color edge)
    {
        Fill(rect, back);
        Frame(rect, edge);
    }

    /// <summary>A filled meter: <paramref name="ratio"/> of the width in <paramref name="fill"/> over <paramref name="back"/>.</summary>
    public static void Bar(PixelRect rect, double ratio, Color fill, Color back)
    {
        Fill(rect, back);
        var width = (int)Math.Round(Math.Clamp(ratio, 0, 1) * (rect.Width - 2));
        if (width > 0)
        {
            Raylib.DrawRectangle(rect.X + 1, rect.Y + 1, width, rect.Height - 2, fill);
            Raylib.DrawRectangle(rect.X + 1, rect.Y + 1, width, 1, Palette.Alpha(Color.White, 60));
        }

        Frame(rect, Palette.PanelEdge);
    }

    public static void Clip(PixelRect rect) => Raylib.BeginScissorMode(rect.X, rect.Y, rect.Width, rect.Height);

    public static void Unclip() => Raylib.EndScissorMode();

    /// <summary>Shortens text with an ellipsis so it fits <paramref name="maxWidth"/>.</summary>
    public static string Ellipsize(string text, int maxWidth)
    {
        if (Width(text) <= maxWidth)
        {
            return text;
        }

        var cut = text.Length;
        while (cut > 1 && Width(text[..cut] + "..") > maxWidth)
        {
            cut--;
        }

        return text[..cut].TrimEnd() + "..";
    }

    // The default font covers ASCII, so typographic punctuation is folded onto its plain cousins,
    // and line breaks become spaces because the log wraps text itself.
    public static string Clean(string text)
    {
        if (text.All(c => c is < (char)127 and >= ' '))
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            builder.Append(c switch
            {
                '\n' or '\r' or '\t' => " ",
                '–' or '—' or '−' => "-",
                '‘' or '’' => "'",
                '“' or '”' => "\"",
                '…' => "...",
                ' ' => " ",
                _ when c is < (char)127 and >= ' ' => c.ToString(),
                _ when c < ' ' => string.Empty,
                _ => "?",
            });
        }

        return builder.ToString();
    }
}
