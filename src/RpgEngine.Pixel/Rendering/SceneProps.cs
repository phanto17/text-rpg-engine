using System.Numerics;
using Raylib_cs;
using RpgEngine.Pixel.Layout;

namespace RpgEngine.Pixel.Rendering;

/// <summary>The stonework the procedural scenes are built from: arches, pillars, braziers, doors, bells, stairs, water.</summary>
internal static class SceneProps
{
    public static void Gradient(PixelRect r, Color top, Color bottom) =>
        Raylib.DrawRectangleGradientV(r.X, r.Y, r.Width, r.Height, top, bottom);

    /// <summary>The ground from <paramref name="horizon"/> down, with a lit lip along the horizon.</summary>
    public static void Floor(PixelRect r, int horizon, Color color)
    {
        Raylib.DrawRectangle(r.X, horizon, r.Width, r.Bottom - horizon, color);
        Raylib.DrawRectangle(r.X, horizon, r.Width, 1, Emblems.Shade(color, 1.4));
        var flag = Emblems.Shade(color, 0.85);
        for (var y = horizon + 6; y < r.Bottom; y += 8)
        {
            Raylib.DrawRectangle(r.X, y, r.Width, 1, flag);
        }
    }

    public static void Pillar(int x, int top, int width, int height, Color color)
    {
        Raylib.DrawRectangle(x, top, width, height, color);
        Raylib.DrawRectangle(x, top, 1, height, Emblems.Shade(color, 1.35));
        Raylib.DrawRectangle(x + width - 2, top, 2, height, Emblems.Shade(color, 0.6));
        Raylib.DrawRectangle(x - 2, top, width + 4, 3, Emblems.Shade(color, 1.15));
        Raylib.DrawRectangle(x - 2, top + height - 3, width + 4, 3, Emblems.Shade(color, 1.15));
    }

    /// <summary>A round arch: two pillars and a half-circle head, with the opening showing <paramref name="opening"/>.</summary>
    public static void Arch(int x, int top, int width, int height, Color stone, Color opening)
    {
        var radius = width / 2;
        Raylib.DrawRectangle(x, top + radius, width, height - radius, stone);
        Raylib.DrawCircle(x + radius, top + radius, radius, stone);
        var inner = Math.Max(1, radius - 3);
        Raylib.DrawCircle(x + radius, top + radius, inner, opening);
        Raylib.DrawRectangle(x + 3, top + radius, width - 6, height - radius, opening);
        Raylib.DrawCircleLines(x + radius, top + radius, inner, Emblems.Shade(stone, 0.6));
    }

    /// <summary>A flickering flame; <paramref name="time"/> and <paramref name="seed"/> vary the shape.</summary>
    public static void Flame(int cx, int baseY, int height, double time, int seed, Color outer, Color inner)
    {
        var flicker = Math.Sin((time * 9) + seed) * 0.15 + Math.Sin((time * 23) + (seed * 1.7)) * 0.1;
        var h = (int)(height * (1 + flicker));
        var lean = (int)Math.Round(Math.Sin((time * 5) + seed) * 2);
        Raylib.DrawTriangle(new Vector2(cx - (height / 2), baseY), new Vector2(cx + (height / 2), baseY), new Vector2(cx + lean, baseY - h), outer);
        Raylib.DrawTriangle(new Vector2(cx - (height / 4), baseY), new Vector2(cx + (height / 4), baseY), new Vector2(cx + (lean / 2), baseY - (h / 2)), inner);
        Raylib.BeginBlendMode(BlendMode.Additive);
        Raylib.DrawCircleGradient(new Vector2(cx, baseY - (h / 3)), height * 1.8f, Palette.Alpha(outer, 55), Palette.Alpha(outer, 0));
        Raylib.EndBlendMode();
    }

    /// <summary>A bowl on a stand with a fire in it.</summary>
    public static void Brazier(int cx, int baseY, int size, double time, int seed, Color iron, Color fire)
    {
        Raylib.DrawRectangle(cx - 1, baseY - size, 3, size, iron);
        Raylib.DrawRectangle(cx - (size / 2), baseY - 1, size, 2, iron);
        Raylib.DrawEllipse(cx, baseY - size, size / 2 + 2, size / 4, iron);
        Raylib.DrawEllipse(cx, baseY - size - 1, size / 2, size / 5, Emblems.Shade(iron, 0.5));
        Flame(cx, baseY - size - 1, size, time, seed, fire, new Color(255, 240, 190, 255));
    }

    /// <summary>A bronze door with the sun, moon, and skull sigils; the first <paramref name="lit"/> glow.</summary>
    public static void SigilDoor(int x, int top, int width, int height, Color bronze, Color glow, int lit)
    {
        Raylib.DrawRectangle(x - 3, top - 3, width + 6, height + 3, Emblems.Shade(bronze, 0.5));
        Raylib.DrawRectangle(x, top, width, height, bronze);
        Raylib.DrawRectangle(x + (width / 2) - 1, top, 2, height, Emblems.Shade(bronze, 0.6));
        var dim = Emblems.Shade(bronze, 1.3);
        var cy = top + (height / 3);
        var step = width / 4;
        for (var i = 0; i < 3; i++)
        {
            var cx = x + step * (i + 1);
            var color = i < lit ? glow : dim;
            switch (i)
            {
                case 0:
                    Raylib.DrawCircle(cx, cy, 4, color);
                    for (var a = 0; a < 8; a++)
                    {
                        var angle = a * Math.PI / 4;
                        Raylib.DrawLine(cx + (int)(Math.Cos(angle) * 5), cy + (int)(Math.Sin(angle) * 5), cx + (int)(Math.Cos(angle) * 7), cy + (int)(Math.Sin(angle) * 7), color);
                    }

                    break;
                case 1:
                    Raylib.DrawCircle(cx, cy, 5, color);
                    Raylib.DrawCircle(cx + 2, cy - 1, 4, bronze);
                    break;
                default:
                    Raylib.DrawCircle(cx, cy - 1, 4, color);
                    Raylib.DrawRectangle(cx - 2, cy + 1, 5, 3, color);
                    Raylib.DrawPixel(cx - 2, cy - 2, bronze);
                    Raylib.DrawPixel(cx + 1, cy - 2, bronze);
                    break;
            }

            if (i < lit)
            {
                Raylib.BeginBlendMode(BlendMode.Additive);
                Raylib.DrawCircleGradient(new Vector2(cx, cy), 12, Palette.Alpha(glow, 60), Palette.Alpha(glow, 0));
                Raylib.EndBlendMode();
            }
        }
    }

    /// <summary>A hanging bell, cracked unless <paramref name="mended"/>, with its chains up to <paramref name="chainTop"/>.</summary>
    public static void Bell(int cx, int top, int radius, int chainTop, Color metal, Color crackColor, bool mended, double time)
    {
        var sway = (int)Math.Round(Math.Sin(time * 0.7) * 1.5);
        cx += sway;
        for (var y = chainTop; y < top; y += 3)
        {
            Raylib.DrawPixel(cx - 4, y, Emblems.Shade(metal, 0.7));
            Raylib.DrawPixel(cx + 4, y, Emblems.Shade(metal, 0.7));
        }

        Raylib.DrawCircle(cx, top + radius, radius, metal);
        Raylib.DrawRectangle(cx - radius, top + radius, radius * 2, radius + 4, metal);
        Raylib.DrawRectangle(cx - radius - 3, top + (radius * 2) + 2, (radius * 2) + 6, 4, Emblems.Shade(metal, 1.2));
        Raylib.DrawRectangle(cx - radius + 2, top + 4, 2, radius * 2 - 2, Emblems.Shade(metal, 1.4));
        Raylib.DrawCircle(cx, top + (radius * 2) + 7, 3, Emblems.Shade(metal, 0.5));
        if (!mended)
        {
            Raylib.DrawLine(cx + 3, top + 6, cx + 1, top + radius + 2, crackColor);
            Raylib.DrawLine(cx + 1, top + radius + 2, cx + 5, top + (radius * 2) - 2, crackColor);
            Raylib.DrawLine(cx + 5, top + (radius * 2) - 2, cx + 2, top + (radius * 2) + 5, crackColor);
            Raylib.DrawLine(cx + 1, top + radius + 2, cx - 3, top + radius + 6, crackColor);
        }
    }

    /// <summary>Steps descending to the right (<paramref name="rightward"/>) or left.</summary>
    public static void Stairs(int x, int y, int count, int stepWidth, int stepHeight, Color stone, bool rightward)
    {
        for (var i = 0; i < count; i++)
        {
            var sx = rightward ? x + (i * stepWidth) : x - (i * stepWidth) - stepWidth;
            var sy = y + (i * stepHeight);
            Raylib.DrawRectangle(sx, sy, stepWidth, stepHeight * (count - i) + 2, stone);
            Raylib.DrawRectangle(sx, sy, stepWidth, 1, Emblems.Shade(stone, 1.4));
        }
    }

    /// <summary>Dark water with slow highlights.</summary>
    public static void Water(PixelRect r, Color water, double time)
    {
        Raylib.DrawRectangle(r.X, r.Y, r.Width, r.Height, water);
        var glint = Emblems.Shade(water, 1.6);
        for (var y = r.Y + 3; y < r.Bottom; y += 7)
        {
            var offset = (int)(Math.Sin((time * 0.8) + (y * 0.4)) * 8);
            for (var x = r.X + ((y * 13) % 20) + offset; x < r.Right - 6; x += 24)
            {
                Raylib.DrawRectangle(Math.Max(r.X, x), y, 6, 1, glint);
            }
        }
    }

    /// <summary>Rows of small arched niches, each with a skull.</summary>
    public static void SkullNiches(int x, int top, int columns, int rows, Color stone, Color bone)
    {
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < columns; col++)
            {
                var nx = x + (col * 12);
                var ny = top + (row * 14);
                Raylib.DrawRectangle(nx, ny + 3, 8, 8, Emblems.Shade(stone, 0.4));
                Raylib.DrawCircle(nx + 4, ny + 3, 4, Emblems.Shade(stone, 0.4));
                Raylib.DrawCircle(nx + 4, ny + 5, 2, bone);
                Raylib.DrawRectangle(nx + 3, ny + 7, 3, 2, bone);
                Raylib.DrawPixel(nx + 3, ny + 5, Emblems.Shade(stone, 0.3));
                Raylib.DrawPixel(nx + 5, ny + 5, Emblems.Shade(stone, 0.3));
            }
        }
    }

    /// <summary>A bookcase packed with spines in shades of the accent.</summary>
    public static void Shelves(int x, int top, int width, int height, Color wood, Color accent)
    {
        Raylib.DrawRectangle(x, top, width, height, wood);
        var shelfHeight = 14;
        for (var sy = top + 2; sy + shelfHeight <= top + height; sy += shelfHeight)
        {
            var bx = x + 2;
            var n = 0;
            while (bx < x + width - 3)
            {
                var w = 2 + ((n * 7 + sy) % 3);
                var h = shelfHeight - 4 - ((n * 5 + sy) % 4);
                var tint = Emblems.Shade(accent, 0.5 + (((n * 3 + sy) % 5) * 0.12));
                Raylib.DrawRectangle(bx, sy + shelfHeight - 2 - h, w, h, tint);
                bx += w + 1;
                n++;
            }

            Raylib.DrawRectangle(x, sy + shelfHeight - 2, width, 2, Emblems.Shade(wood, 0.6));
        }
    }

    /// <summary>Battlements along the top of a wall.</summary>
    public static void Crenellations(int x, int y, int width, Color stone)
    {
        for (var cx = x; cx < x + width; cx += 10)
        {
            Raylib.DrawRectangle(cx, y - 5, 5, 6, stone);
        }

        Raylib.DrawRectangle(x, y, width, 2, Emblems.Shade(stone, 1.3));
    }

    /// <summary>A standing figure in silhouette (a statue or a distant watcher).</summary>
    public static void Statue(int cx, int baseY, int height, Color stone)
    {
        var w = height / 3;
        Raylib.DrawRectangle(cx - w, baseY - 3, w * 2, 3, Emblems.Shade(stone, 1.1));
        Raylib.DrawRectangle(cx - (w / 2), baseY - (height * 2 / 3), w, height * 2 / 3 - 3, stone);
        Raylib.DrawCircle(cx, baseY - (height * 2 / 3) - (w / 3), w / 3 + 1, stone);
        Raylib.DrawRectangle(cx - (w / 2) - 2, baseY - (height * 2 / 3) + 2, 2, height / 3, Emblems.Shade(stone, 0.8));
    }

    /// <summary>Darkens the edges of the viewport.</summary>
    public static void Vignette(PixelRect r)
    {
        var dark = new Color(0, 0, 0, 120);
        var clear = new Color(0, 0, 0, 0);
        var edge = 24;
        Raylib.DrawRectangleGradientH(r.X, r.Y, edge, r.Height, dark, clear);
        Raylib.DrawRectangleGradientH(r.Right - edge, r.Y, edge, r.Height, clear, dark);
        Raylib.DrawRectangleGradientV(r.X, r.Y, r.Width, edge, Palette.Alpha(dark, 90), clear);
        Raylib.DrawRectangleGradientV(r.X, r.Bottom - 12, r.Width, 12, clear, Palette.Alpha(dark, 90));
    }
}
