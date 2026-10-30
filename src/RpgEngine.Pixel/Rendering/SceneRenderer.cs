using System.Numerics;
using Raylib_cs;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Layout;
using RpgEngine.Pixel.Presentation;

namespace RpgEngine.Pixel.Rendering;

/// <summary>
/// Draws the room: a picture from <c>Data/Assets</c> when the presentation names one, otherwise a
/// layered procedural scene for the room's <see cref="SceneStyle"/> in its palette, then the weather,
/// the Barrowlight's glow, and a vignette.
/// </summary>
internal sealed class SceneRenderer(PixelRect area)
{
    private readonly WeatherField _weather = new(area.Inset(1));

    public PixelRect Area => area;

    public void Update(WeatherKind weather, double dt)
    {
        _weather.Kind = weather;
        _weather.Update(dt);
    }

    public void Draw(SceneFrame frame, TextureCache textures)
    {
        var look = frame.Look;
        Canvas.Clip(area);
        if (textures.Get(look.BackdropImage) is { } backdrop)
        {
            Raylib.DrawTexturePro(backdrop, new Rectangle(0, 0, backdrop.Width, backdrop.Height), new Rectangle(area.X, area.Y, area.Width, area.Height), Vector2.Zero, 0, Color.White);
        }
        else
        {
            SceneProps.Gradient(area, Palette.Of(look.BackgroundTop), Palette.Of(look.BackgroundBottom));
            DrawArchitecture(frame);
        }

        DrawInteractiveDetails(frame);
        _weather.Draw(frame.Time);
        Lantern(frame);
        SceneProps.Vignette(area);
        Canvas.Unclip();
    }

    private void Lantern(SceneFrame frame)
    {
        var color = Palette.Of(LanternLight.ColorFor(frame.Resonance));
        var radius = LanternLight.RadiusFor(frame.Resonance) * (frame.HasLantern ? 1f : 0.6f);
        var steppedTime = Math.Floor(frame.Time * 4) / 4;
        var flicker = (float)Math.Round(Math.Sin(steppedTime * 7) * 1.5 + Math.Sin(steppedTime * 13));
        var center = new Vector2(area.CenterX, area.Bottom - 6);
        Raylib.BeginBlendMode(BlendMode.Additive);
        Raylib.DrawCircleGradient(center, radius + flicker, Palette.Alpha(color, frame.HasLantern ? 95 : 55), Palette.Alpha(color, 0));
        Raylib.DrawCircleGradient(center, (radius / 3) + flicker, Palette.Alpha(color, 70), Palette.Alpha(color, 0));
        Raylib.EndBlendMode();
    }

    // Stateful details sit above both painted backdrops and procedural fallbacks.
    private void DrawInteractiveDetails(SceneFrame frame)
    {
        var cx = area.CenterX;
        var horizon = area.Y + 104;
        var accent = Palette.Of(frame.Look.Accent);
        var stone = Palette.Of(frame.Look.Architecture);
        if (frame.Look.Style == SceneStyle.OssuaryVault)
        {
            if (frame.SigilDoorOpen)
            {
                Raylib.DrawRectangle(cx - 26, area.Y + 40, 52, 64, Palette.Ink);
                Raylib.DrawRectangle(cx - 26, area.Y + 40, 5, 64, Emblems.Shade(accent, 0.5));
                Raylib.DrawRectangle(cx + 21, area.Y + 40, 5, 64, Emblems.Shade(accent, 0.5));
                Raylib.DrawRectangle(cx - 25, horizon - 2, 50, 2, Emblems.Shade(accent, 0.8));
            }
            else
            {
                SceneProps.SigilDoor(cx - 26, area.Y + 40, 52, 64, Emblems.Shade(accent, 0.6), accent, frame.SigilsLit);
            }
            var time = Math.Floor(frame.Time * 4) / 4;
            SceneProps.Brazier(cx - 60, horizon, 12, time, 1, stone, accent);
            SceneProps.Brazier(cx + 60, horizon, 12, time, 2, stone, accent);
        }
        else if (frame.Look.Style == SceneStyle.CryptRotunda && !frame.HasLantern)
        {
            Raylib.DrawRectangleLines(cx - 3, horizon - 42, 7, 6, Palette.Muted);
            Raylib.DrawRectangle(cx - 5, horizon - 36, 11, 14, Palette.PanelEdgeLight);
            Raylib.DrawRectangle(cx - 3, horizon - 34, 7, 9, Palette.Ink);
            Raylib.DrawRectangle(cx - 5, horizon - 24, 11, 2, Palette.Muted);
        }
        else if (frame.Look.Style == SceneStyle.SpiralStair)
        {
            var time = Math.Floor(frame.Time * 4) / 4;
            for (var i = 0; i < 6; i++)
            {
                var dx = area.X + 20 + (i * 48);
                Raylib.DrawLine(dx, area.Y + 8, dx + 1, area.Y + 30 + (int)(Math.Sin(time * 2 + i) * 6), Palette.Alpha(accent, 120));
            }
        }
        else if (frame.Look.Style == SceneStyle.SunkenCathedral)
        {
            DrawMarketLights(frame);
        }
    }

    private void DrawMarketLights(SceneFrame frame)
    {
        var cx = area.CenterX;
        var horizon = area.Y + 104;
        var stone = Palette.Of(frame.Look.Architecture);
        var accent = Palette.Of(frame.Look.Accent);
        var dark = Emblems.Shade(stone, 0.55);
        var time = Math.Floor(frame.Time * 4) / 4;
        for (var i = -3; i <= 3; i++)
        {
            var lx = cx + (i * 40) + 8;
            var ly = area.Y + 30 + (int)(Math.Sin(time + i) * 2);
            Raylib.DrawLine(lx, area.Y, lx, ly, dark);
            Raylib.DrawRectangle(lx - 3, ly, 6, 8, dark);
            Raylib.DrawRectangle(lx - 2, ly + 1, 4, 6, accent);
            Raylib.BeginBlendMode(BlendMode.Additive);
            Raylib.DrawCircleGradient(new Vector2(lx, ly + 4), 16, Palette.Alpha(accent, 60), Palette.Alpha(accent, 0));
            Raylib.EndBlendMode();
        }

        // The housing stays dark until the data-bound repair flag is set.
        Raylib.DrawRectangle(cx - 4, horizon - 20, 8, 20, stone);
        Raylib.DrawRectangle(cx - 10, horizon - 60, 3, 40, stone);
        Raylib.DrawRectangle(cx + 7, horizon - 60, 3, 40, stone);
        Raylib.DrawRectangle(cx - 12, horizon - 64, 24, 4, Emblems.Shade(stone, 1.2));
        Raylib.DrawRectangle(cx - 12, horizon - 23, 24, 4, stone);
        if (frame.BeaconLit)
        {
            Raylib.DrawRectangleLines(cx - 6, horizon - 54, 12, 25, accent);
            SceneProps.Flame(cx, horizon - 34, 16, time, 5, accent, new Color(255, 240, 200, 255));
        }
        SceneProps.Brazier(cx - 22, horizon + 14, 8, time, 6, dark, accent);
    }

    private void DrawArchitecture(SceneFrame frame)
    {
        var look = frame.Look;
        var stone = Palette.Of(look.Architecture);
        var accent = Palette.Of(look.Accent);
        var dark = Emblems.Shade(stone, 0.55);
        var deep = Emblems.Shade(Palette.Of(look.BackgroundTop), 0.6);
        var t = frame.Time;
        var x = area.X;
        var w = area.Width;
        var cx = area.CenterX;
        var horizon = area.Y + 104;

        switch (look.Style)
        {
            case SceneStyle.MoorGate:
                Hills(area, horizon - 10, Emblems.Shade(stone, 0.8));
                SceneProps.Floor(area, horizon, Emblems.Shade(stone, 0.9));
                for (var i = -3; i <= 3; i++)
                {
                    var hx = cx + (i * 38);
                    Raylib.DrawRectangle(hx - 3, horizon - 10 - Math.Abs(i), 7, 12 + Math.Abs(i), dark);
                    Raylib.DrawCircle(hx, horizon - 10 - Math.Abs(i), 3, dark);
                }

                SceneProps.Pillar(cx - 44, horizon - 70, 10, 70, stone);
                SceneProps.Pillar(cx + 34, horizon - 70, 10, 70, stone);
                Raylib.DrawRectangle(cx - 48, horizon - 76, 96, 8, stone);
                Raylib.DrawRectangle(cx - 30, horizon - 68, 60, 68, deep);
                for (var i = 0; i < 5; i++)
                {
                    Raylib.DrawRectangle(cx - 26 + (i * 12), horizon - 64, 2, 64, Emblems.Shade(stone, 0.7));
                }

                SceneProps.Statue(cx - 70, horizon, 36, dark);
                SceneProps.Statue(cx + 70, horizon, 36, dark);
                Raylib.DrawRectangle(cx - 36, horizon - 2, 72, 4, Emblems.Shade(stone, 1.1));
                break;

            case SceneStyle.OssuaryVault:
                SceneProps.Floor(area, horizon, Emblems.Shade(stone, 0.8));
                SceneProps.SkullNiches(x + 10, area.Y + 20, 7, 5, stone, Emblems.Shade(accent, 0.9));
                SceneProps.SkullNiches(area.Right - 96, area.Y + 20, 7, 5, stone, Emblems.Shade(accent, 0.9));
                SceneProps.Arch(cx - 38, area.Y + 14, 76, horizon - area.Y - 14, stone, deep);
                break;

            case SceneStyle.CryptRotunda:
                SceneProps.Floor(area, horizon, Emblems.Shade(stone, 0.85));
                Raylib.DrawEllipse(cx, area.Y + 30, 120, 26, Emblems.Shade(stone, 0.75));
                Raylib.DrawEllipse(cx, area.Y + 30, 110, 20, deep);
                for (var i = -2; i <= 2; i++)
                {
                    SceneProps.Pillar(cx + (i * 56) - 4, area.Y + 36, 8, horizon - area.Y - 36, stone);
                }

                Raylib.DrawRectangle(cx - 34, horizon - 22, 68, 22, stone);
                Raylib.DrawRectangle(cx - 38, horizon - 26, 76, 6, Emblems.Shade(stone, 1.2));
                Raylib.DrawRectangle(cx - 30, horizon - 24, 60, 2, accent);
                break;

            case SceneStyle.SpiralStair:
                Raylib.DrawRectangle(x, area.Y, w, area.Height, Emblems.Shade(stone, 0.7));
                Raylib.DrawEllipse(cx, area.Y + 60, 70, 90, deep);
                for (var i = 0; i < 9; i++)
                {
                    var angle = (i * 0.55) + (t * 0.0);
                    var sx = cx + (int)(Math.Cos(angle) * 50);
                    var sy = area.Y + 20 + (i * 13);
                    Raylib.DrawRectangle(sx - 18, sy, 36, 5, stone);
                    Raylib.DrawRectangle(sx - 18, sy, 36, 1, Emblems.Shade(stone, 1.4));
                }

                break;

            case SceneStyle.SunkenCathedral:
                SceneProps.Floor(area, horizon, Emblems.Shade(stone, 0.8));
                for (var i = -2; i <= 2; i++)
                {
                    var ax = cx + (i * 58);
                    SceneProps.Arch(ax - 16, area.Y + 10, 32, horizon - area.Y - 10, stone, deep);
                }

                Raylib.DrawRectangle(x + 20, horizon - 14, 50, 14, dark);
                Raylib.DrawRectangle(x + 18, horizon - 16, 54, 3, Emblems.Shade(accent, 0.7));
                Raylib.DrawRectangle(area.Right - 70, horizon - 14, 50, 14, dark);
                Raylib.DrawRectangle(area.Right - 72, horizon - 16, 54, 3, Emblems.Shade(accent, 0.7));
                break;

            case SceneStyle.EchoChapel:
                SceneProps.Floor(area, horizon, Emblems.Shade(stone, 0.8));
                for (var i = 0; i < 9; i++)
                {
                    var px = cx - 40 + (i * 10);
                    var ph = 40 + (int)(Math.Abs(4 - i) * -4) + 20;
                    Raylib.DrawRectangle(px, horizon - 20 - ph, 7, ph, Emblems.Shade(stone, 1.2 - (i % 2) * 0.2));
                }

                Raylib.DrawRectangle(cx - 48, horizon - 22, 96, 22, stone);
                Raylib.DrawRectangle(cx - 44, horizon - 18, 88, 2, accent);
                for (var i = 0; i < 5; i++)
                {
                    var bx = x + 24 + (i * 60);
                    var by = area.Y + 20 + (i % 2) * 10 + (int)(Math.Sin(t * 1.5 + i) * 2);
                    Raylib.DrawLine(bx, area.Y, bx, by, dark);
                    Raylib.DrawRectangle(bx - 4, by, 8, 6, Emblems.Shade(accent, 0.6));
                    Raylib.DrawRectangle(bx - 3, by + 1, 6, 4, accent);
                }

                break;

            case SceneStyle.DrownedBridge:
                SceneProps.Water(new PixelRect(x, horizon - 20, w, area.Bottom - horizon + 20), Emblems.Shade(Palette.Of(look.BackgroundBottom), 0.8), t);
                Raylib.DrawRectangle(x, horizon - 14, w, 10, stone);
                Raylib.DrawRectangle(x, horizon - 14, w, 1, Emblems.Shade(stone, 1.4));
                for (var px = x + 8; px < area.Right; px += 24)
                {
                    Raylib.DrawRectangle(px, horizon - 26, 3, 14, dark);
                    Raylib.DrawRectangle(px - 24, horizon - 4, 20, 30, Palette.Alpha(dark, 70));
                }

                for (var i = 0; i < 5; i++)
                {
                    var ax = x + 10 + (i * 70);
                    SceneProps.Arch(ax, horizon - 60, 40, 46, Emblems.Shade(stone, 0.8), deep);
                }

                Raylib.DrawRectangle(cx + 40, horizon - 52, 30, 38, stone);
                Raylib.DrawRectangle(cx + 46, horizon - 44, 8, 10, deep);
                Raylib.DrawRectangle(cx + 60, horizon - 44, 8, 10, Palette.Alpha(accent, 150));
                break;

            case SceneStyle.TwinSealGate:
                SceneProps.Floor(area, horizon, Emblems.Shade(stone, 0.85));
                Raylib.DrawRectangle(x, area.Y, w, horizon - area.Y, Emblems.Shade(stone, 0.6));
                Raylib.DrawRectangle(x, area.Y + 20, w, 2, dark);
                SceneProps.Pillar(cx - 70, area.Y + 12, 12, horizon - area.Y - 12, stone);
                SceneProps.Pillar(cx + 58, area.Y + 12, 12, horizon - area.Y - 12, stone);
                SceneProps.TwinGate(cx, area.Y + 22, 100, horizon - area.Y - 22, Emblems.Shade(stone, 1.1), accent, new Color(170, 200, 255, 255), frame.SunSealSet, frame.MoonSealSet);
                SceneProps.Brazier(cx - 100, horizon, 11, t, 3, dark, accent);
                SceneProps.Brazier(cx + 100, horizon, 11, t, 4, dark, accent);
                break;

            case SceneStyle.ArchiveStacks:
                SceneProps.Floor(area, horizon, Emblems.Shade(stone, 0.8));
                SceneProps.Shelves(x + 6, area.Y + 6, 70, horizon - area.Y - 6, Emblems.Shade(stone, 0.9), accent);
                SceneProps.Shelves(area.Right - 76, area.Y + 6, 70, horizon - area.Y - 6, Emblems.Shade(stone, 0.9), accent);
                SceneProps.Shelves(cx - 70, area.Y + 30, 46, horizon - area.Y - 30, Emblems.Shade(stone, 0.8), Emblems.Shade(accent, 0.8));
                SceneProps.Shelves(cx + 24, area.Y + 30, 46, horizon - area.Y - 30, Emblems.Shade(stone, 0.8), Emblems.Shade(accent, 0.8));
                Raylib.DrawRectangle(cx - 16, horizon - 30, 32, 30, Emblems.Shade(stone, 1.1));
                Raylib.DrawRectangle(cx - 12, horizon - 34, 24, 4, accent);
                for (var i = 0; i < 6; i++)
                {
                    var sx = cx - 60 + (i * 24);
                    var sy = area.Y + 14 + (int)(Math.Sin(t * 0.6 + i * 1.3) * 3);
                    Raylib.DrawRectangle(sx, sy, 5, 7, Palette.Alpha(Emblems.Shade(accent, 1.2), 130));
                }

                break;

            case SceneStyle.TallowLoom:
                SceneProps.Floor(area, horizon, Emblems.Shade(stone, 0.8));
                Raylib.DrawRectangle(cx - 60, area.Y + 16, 6, horizon - area.Y - 16, stone);
                Raylib.DrawRectangle(cx + 54, area.Y + 16, 6, horizon - area.Y - 16, stone);
                Raylib.DrawRectangle(cx - 62, area.Y + 14, 124, 5, Emblems.Shade(stone, 1.2));
                for (var i = 0; i < 20; i++)
                {
                    var lx = cx - 52 + (i * 5);
                    var wave = (int)(Math.Sin(t * 2 + i * 0.5) * 2);
                    Raylib.DrawLine(lx, area.Y + 19, lx + wave, horizon - 10, Palette.Alpha(accent, (byte)(90 + (i % 3) * 40)));
                }

                Raylib.DrawRectangle(cx - 56, horizon - 36, 112, 4, Emblems.Shade(stone, 1.1));
                Raylib.DrawEllipse(cx - 90, horizon - 6, 24, 8, dark);
                Raylib.DrawEllipse(cx - 90, horizon - 8, 20, 5, Emblems.Shade(accent, 0.8));
                Raylib.DrawEllipse(cx + 90, horizon - 6, 24, 8, dark);
                Raylib.DrawEllipse(cx + 90, horizon - 8, 20, 5, Emblems.Shade(accent, 0.8));
                SceneProps.Flame(cx - 90, horizon - 10, 10, t, 6, accent, new Color(255, 240, 200, 255));
                SceneProps.Flame(cx + 90, horizon - 10, 10, t, 7, accent, new Color(255, 240, 200, 255));
                break;

            case SceneStyle.VowNursery:
                SceneProps.Floor(area, horizon, Emblems.Shade(stone, 0.8));
                for (var row = 0; row < 2; row++)
                {
                    for (var i = -3; i <= 3; i++)
                    {
                        var rx = cx + (i * 40) + (row * 20);
                        var ry = horizon - 6 - (row * 26);
                        Raylib.DrawRing(new Vector2(rx, ry), 9, 12, 0, 180, 10, stone);
                        Raylib.DrawRectangle(rx - 12, ry - 14, 24, 3, stone);
                        Raylib.DrawLine(rx, area.Y, rx, ry - 14, dark);
                        Raylib.DrawRectangle(rx - 6, ry - 11, 12, 10, Palette.Alpha(accent, 50));
                    }
                }

                Raylib.DrawRectangle(cx - 20, area.Y + 10, 40, 50, Emblems.Shade(stone, 1.2));
                Raylib.DrawRectangle(cx - 16, area.Y + 14, 32, 42, deep);
                for (var i = 0; i < 6; i++)
                {
                    Raylib.DrawRectangle(cx - 14 + (i * 5), area.Y + 20 + (int)(Math.Abs(Math.Sin(t * 1.3 + i)) * 20), 3, 3, accent);
                }

                break;

            case SceneStyle.ScouredBarracks:
                SceneProps.Floor(area, horizon, Emblems.Shade(stone, 0.8));
                Raylib.DrawRectangle(x, area.Y, w, horizon - area.Y, Emblems.Shade(stone, 0.7));
                for (var i = 0; i < 4; i++)
                {
                    var bx = x + 14 + (i * 72);
                    Raylib.DrawRectangle(bx, horizon - 40, 50, 4, stone);
                    Raylib.DrawRectangle(bx, horizon - 20, 50, 4, stone);
                    Raylib.DrawRectangle(bx, horizon - 46, 3, 46, dark);
                    Raylib.DrawRectangle(bx + 47, horizon - 46, 3, 46, dark);
                    Raylib.DrawRectangle(bx + 4, horizon - 44, 42, 3, Emblems.Shade(accent, 0.5));
                    Raylib.DrawRectangle(bx + 4, horizon - 24, 42, 3, Emblems.Shade(accent, 0.5));
                }

                for (var i = 0; i < 7; i++)
                {
                    Raylib.DrawRectangle(x + 20 + (i * 42), area.Y + 10, 12, 16, deep);
                    Raylib.DrawRectangle(x + 22 + (i * 42), area.Y + 12, 8, 12, Palette.Alpha(accent, 40));
                }

                break;

            case SceneStyle.LyeCistern:
                Raylib.DrawRectangle(x, area.Y, w, area.Height, Emblems.Shade(stone, 0.7));
                for (var i = 0; i < 4; i++)
                {
                    SceneProps.Arch(x + 10 + (i * 76), area.Y + 8, 56, horizon - area.Y - 8, stone, deep);
                }

                SceneProps.Water(new PixelRect(x, horizon - 16, w, area.Bottom - horizon + 16), Emblems.Shade(accent, 0.35), t);
                for (var i = 0; i < 5; i++)
                {
                    var px = x + 30 + (i * 62);
                    Raylib.DrawRectangle(px, area.Y, 6, horizon - 16 - area.Y, dark);
                    Raylib.DrawRectangle(px - 2, horizon - 30, 10, 4, Emblems.Shade(stone, 1.2));
                    var drip = (int)((t * 30 + i * 17) % 40);
                    Raylib.DrawPixel(px + 3, horizon - 26 + drip, Palette.Alpha(accent, 180));
                }

                Raylib.DrawRectangle(cx - 30, horizon - 20, 60, 6, stone);
                break;

            case SceneStyle.IronBastion:
                SceneProps.Floor(area, horizon, Emblems.Shade(stone, 0.85));
                Raylib.DrawRectangle(x, area.Y + 30, w, horizon - area.Y - 30, Emblems.Shade(stone, 0.8));
                SceneProps.Crenellations(x, area.Y + 30, w, stone);
                SceneProps.Pillar(x + 30, area.Y + 20, 14, horizon - area.Y - 20, stone);
                SceneProps.Pillar(area.Right - 44, area.Y + 20, 14, horizon - area.Y - 20, stone);
                Raylib.DrawRectangle(cx - 30, horizon - 46, 60, 46, dark);
                Raylib.DrawRectangle(cx - 26, horizon - 42, 52, 36, Emblems.Shade(accent, 0.3));
                Raylib.DrawRectangle(cx - 22, horizon - 30, 44, 20, Emblems.Shade(accent, 0.5));
                SceneProps.Flame(cx, horizon - 12, 18, t, 8, accent, new Color(255, 230, 170, 255));
                for (var i = 0; i < 6; i++)
                {
                    Raylib.DrawRectangle(x + 60 + (i * 34), area.Y + 44, 10, 14, Palette.Alpha(accent, 60));
                }

                break;

            case SceneStyle.UncarvedStair:
                Raylib.DrawRectangle(x, area.Y, w, area.Height, Emblems.Shade(stone, 0.6));
                SceneProps.Stairs(cx - 60, area.Y + 24, 10, 12, 9, stone, rightward: true);
                Raylib.DrawRectangle(x, area.Y, 60, area.Height, Emblems.Shade(stone, 0.45));
                Raylib.DrawRectangle(area.Right - 60, area.Y, 60, area.Height, Emblems.Shade(stone, 0.45));
                Raylib.DrawCircle(x + 44, area.Y + 50, 22, Emblems.Shade(stone, 0.9));
                Raylib.DrawRectangle(x + 30, area.Y + 60, 28, 20, Emblems.Shade(stone, 0.9));
                Raylib.DrawCircle(x + 36, area.Y + 46, 5, deep);
                Raylib.DrawCircle(x + 52, area.Y + 46, 5, deep);
                for (var i = 0; i < 4; i++)
                {
                    Raylib.DrawRectangle(x + 34 + (i * 6), area.Y + 70, 3, 8, deep);
                }

                Raylib.DrawRectangle(area.Right - 50, area.Y + 20, 30, 4, dark);
                Raylib.DrawRectangle(area.Right - 46, area.Y + 60, 22, 3, dark);
                break;

            case SceneStyle.SanctuaryLanding:
                SceneProps.Floor(area, horizon, Emblems.Shade(stone, 0.85));
                Raylib.DrawRectangle(x, area.Y, w, horizon - area.Y, Emblems.Shade(stone, 0.6));
                SceneProps.Arch(x + 16, area.Y + 20, 44, horizon - area.Y - 20, stone, deep);
                SceneProps.Arch(area.Right - 60, area.Y + 20, 44, horizon - area.Y - 20, stone, deep);
                SceneProps.Brazier(cx, horizon, 22, t, 9, dark, accent);
                for (var i = 0; i < 5; i++)
                {
                    var bx = cx - 70 + (i * 35);
                    Raylib.DrawRectangle(bx, horizon - 8, 16, 8, Emblems.Shade(stone, 1.1));
                    Raylib.DrawRectangle(bx + 2, horizon - 10, 12, 2, Emblems.Shade(accent, 0.6));
                }

                Raylib.DrawRectangle(x + 80, horizon - 30, 24, 30, dark);
                Raylib.DrawRectangle(x + 82, horizon - 28, 20, 8, Emblems.Shade(accent, 0.6));
                Raylib.DrawRectangle(area.Right - 104, horizon - 30, 24, 30, dark);
                Raylib.DrawRectangle(area.Right - 102, horizon - 28, 20, 8, Emblems.Shade(accent, 0.6));
                break;

            case SceneStyle.SunderedBellNave:
                SceneProps.Floor(area, horizon, Emblems.Shade(stone, 0.8));
                for (var i = -2; i <= 2; i++)
                {
                    if (i == 0)
                    {
                        continue;
                    }

                    SceneProps.Arch(cx + (i * 60) - 18, area.Y + 8, 36, horizon - area.Y - 8, stone, deep);
                }

                SceneProps.Arch(cx - 40, area.Y - 20, 80, horizon - area.Y + 20, stone, deep);
                SceneProps.Bell(cx, area.Y + 26, 22, area.Y, Emblems.Shade(accent, 0.75), Palette.Alpha(Color.Black, 200), frame.BellMended, t);
                Raylib.DrawRectangle(cx - 30, horizon - 6, 60, 6, Emblems.Shade(stone, 1.1));
                Raylib.DrawRectangle(cx + 34, horizon - 4, 10, 4, Emblems.Shade(accent, 0.6));
                break;

            case SceneStyle.CradleOfAsh:
                SceneProps.Floor(area, horizon, Emblems.Shade(stone, 0.8));
                Raylib.DrawEllipse(cx, area.Y + 20, 150, 40, Emblems.Shade(stone, 0.7));
                Raylib.DrawEllipse(cx, area.Y + 20, 140, 32, Emblems.Shade(Palette.Of(look.BackgroundTop), 0.7));
                for (var i = -3; i <= 3; i++)
                {
                    SceneProps.Pillar(cx + (i * 44) - 4, area.Y + 30, 8, horizon - area.Y - 30, Emblems.Shade(stone, 0.9));
                }

                Raylib.DrawEllipse(cx, horizon - 8, 46, 12, dark);
                Raylib.DrawEllipse(cx, horizon - 12, 40, 8, Emblems.Shade(dark, 0.7));
                Raylib.DrawRectangle(cx - 46, horizon - 8, 92, 8, dark);
                Raylib.DrawRectangle(cx - 20, horizon - 2, 40, 2, Emblems.Shade(accent, 0.6));
                if (frame.BrazierLit)
                {
                    SceneProps.Flame(cx, horizon - 12, 40, t, 10, accent, new Color(255, 245, 210, 255));
                    SceneProps.Flame(cx - 16, horizon - 12, 24, t, 11, accent, new Color(255, 240, 200, 255));
                    SceneProps.Flame(cx + 16, horizon - 12, 24, t, 12, accent, new Color(255, 240, 200, 255));
                }
                else
                {
                    for (var i = 0; i < 7; i++)
                    {
                        var ex = cx - 30 + (i * 10);
                        var pulse = (int)(120 + (100 * Math.Abs(Math.Sin(t * 1.5 + i))));
                        Raylib.DrawRectangle(ex, horizon - 14, 3, 2, Palette.Rgba(255, 120, 40, pulse));
                    }
                }

                Raylib.DrawCircle(cx - 100, horizon - 24, 8, accent);
                Raylib.DrawCircleLines(cx - 100, horizon - 24, 11, accent);
                Raylib.DrawCircle(cx + 100, horizon - 24, 8, new Color(170, 200, 255, 255));
                Raylib.DrawCircle(cx + 104, horizon - 26, 7, Palette.Of(look.BackgroundBottom));
                break;
        }
    }

    private static void Hills(PixelRect r, int baseline, Color color)
    {
        for (var i = 0; i < 5; i++)
        {
            var hx = r.X + (i * 80) - 20;
            Raylib.DrawEllipse(hx, baseline + 6, 70, 18 + (i % 2) * 6, Emblems.Shade(color, 0.9 + (i % 2) * 0.1));
        }
    }
}
