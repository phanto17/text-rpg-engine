using System.Numerics;
using Raylib_cs;
using RpgEngine.Core.Combat;
using RpgEngine.Core.Views;
using RpgEngine.Pixel.Layout;
using RpgEngine.Pixel.Presentation;
using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Pixel.Rendering;

/// <summary>
/// The enemies on the scene during a fight: their heraldic sprites with HP bars and status chips,
/// the hit flashes, lunges, and floating numbers the sequencer cues, and the player's own flash.
/// </summary>
internal sealed class CombatStage(PixelRect area)
{
    private const double FlashSeconds = 0.18;
    private const double LungeSeconds = 0.22;
    private const double FallSeconds = 0.7;
    private const double FloatSeconds = 0.9;

    private readonly Dictionary<string, Figure> _figures = new(StringComparer.Ordinal);
    private readonly List<Floating> _floating = [];
    private IReadOnlyList<CombatantView> _enemies = [];
    private IReadOnlyList<string> _slots = [];
    private double _playerFlash;
    private double _playerLunge;
    private string? _playerTarget;

    public bool Active => _enemies.Count > 0;

    /// <summary>Reads the current combatants; outside a fight the stage empties.</summary>
    public void Sync(CombatSession? combat, PresentationCatalog catalog)
    {
        if (combat is null)
        {
            if (_enemies.Count > 0)
            {
                _enemies = [];
                _slots = [];
                _figures.Clear();
                _floating.Clear();
            }

            return;
        }

        _enemies = combat.Snapshot().Where(c => !c.IsPlayer).ToList();
        if (_slots.Count != _enemies.Count || !_slots.SequenceEqual(_enemies.Select(e => e.Name)))
        {
            _slots = _enemies.Select(e => e.Name).ToList();
        }

        foreach (var enemy in _enemies)
        {
            if (!_figures.ContainsKey(enemy.Name))
            {
                var monsterId = enemy.Id.Split('#')[0];
                _figures[enemy.Name] = new Figure(catalog.Monster(monsterId));
            }
        }
    }

    public void Apply(SequenceCue cue)
    {
        switch (cue)
        {
            case HitFlashCue flash when flash.IsPlayer:
                _playerFlash = FlashSeconds * 1.5;
                break;
            case HitFlashCue flash when _figures.TryGetValue(flash.Target, out var hit):
                hit.Flash = FlashSeconds;
                break;
            case LungeCue lunge when lunge.IsPlayer:
                _playerLunge = LungeSeconds;
                _playerTarget = lunge.Target;
                break;
            case LungeCue lunge when _figures.TryGetValue(lunge.Attacker, out var attacker):
                attacker.Lunge = LungeSeconds;
                break;
            case FloatingNumberCue number:
                var (x, y) = number.IsPlayer ? (area.CenterX, area.Bottom - 30) : Anchor(number.Target);
                _floating.Add(new Floating(number.Text, x, y - 20, number.Kind));
                break;
            case FellCue fell when !fell.IsPlayer && _figures.TryGetValue(fell.Name, out var fallen):
                fallen.Falling = FallSeconds;
                fallen.Fled = fell.Fled;
                break;
        }
    }

    public void Update(double dt)
    {
        foreach (var figure in _figures.Values)
        {
            figure.Flash = Math.Max(0, figure.Flash - dt);
            figure.Lunge = Math.Max(0, figure.Lunge - dt);
            if (figure.Falling > 0)
            {
                figure.Falling = Math.Max(0, figure.Falling - dt);
                if (figure.Falling == 0)
                {
                    figure.Gone = true;
                }
            }
        }

        _playerFlash = Math.Max(0, _playerFlash - dt);
        _playerLunge = Math.Max(0, _playerLunge - dt);
        for (var i = _floating.Count - 1; i >= 0; i--)
        {
            _floating[i].Age += dt;
            if (_floating[i].Age > FloatSeconds)
            {
                _floating.RemoveAt(i);
            }
        }
    }

    public void Draw(TextureCache textures)
    {
        if (!Active)
        {
            return;
        }

        Canvas.Clip(area);
        for (var i = 0; i < _enemies.Count; i++)
        {
            var enemy = _enemies[i];
            if (!_figures.TryGetValue(enemy.Name, out var figure) || figure.Gone)
            {
                continue;
            }

            var (cx, cy) = Slot(i);
            var size = _enemies.Count > 2 ? 32 : 40;
            var lunge = figure.Lunge > 0 ? (int)Math.Round(Math.Sin(figure.Lunge / LungeSeconds * Math.PI) * 8) : 0;
            var dx = (int)Math.Round((area.CenterX - cx) / (double)Math.Max(1, Math.Abs(area.CenterX - cx)) * lunge * 0.5);
            var alpha = figure.Falling > 0 ? (int)(255 * figure.Falling / FallSeconds) : 255;
            var drop = figure.Falling > 0 && !figure.Fled ? (int)((1 - figure.Falling / FallSeconds) * 14) : 0;
            var slide = figure.Falling > 0 && figure.Fled ? (int)((1 - figure.Falling / FallSeconds) * 60) : 0;
            var x = cx + dx + slide;
            var y = cy + lunge + drop;

            var primary = Palette.Alpha(Palette.Of(figure.Look.Primary), alpha);
            var accent = Palette.Alpha(Palette.Of(figure.Look.Accent), alpha);
            Raylib.DrawEllipse(x, y + (size / 2) + 2, size / 2, 4, Palette.Rgba(0, 0, 0, alpha / 3));
            if (textures.Get(figure.Look.Image) is { } sprite)
            {
                var h = size + 8;
                var w = sprite.Width * h / Math.Max(1, sprite.Height);
                Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height), new Rectangle(x - (w / 2), y - (h / 2), w, h), Vector2.Zero, 0, Palette.Alpha(Color.White, alpha));
            }
            else
            {
                Emblems.Draw(figure.Look.Emblem, x, y, size, primary, accent);
            }

            if (figure.Flash > 0)
            {
                Raylib.DrawRectangle(x - (size / 2) - 2, y - (size / 2) - 2, size + 4, size + 4, Palette.Alpha(Color.White, (int)(190 * figure.Flash / FlashSeconds)));
            }

            if (figure.Falling == 0)
            {
                var label = Canvas.Ellipsize(enemy.Name, _enemies.Count > 2 ? 64 : 96);
                Canvas.Text(x - (Canvas.Width(label) / 2), y + (size / 2) + 6, label, enemy.IsActive ? Palette.Title : Palette.Muted);
                Canvas.Bar(new PixelRect(x - 20, y + (size / 2) + 17, 40, 5), enemy.Hp / (double)Math.Max(1, enemy.MaxHp), Palette.HpFill, Palette.HpBack);
                for (var s = 0; s < enemy.Statuses.Count && s < 4; s++)
                {
                    var chip = new PixelRect(x - 20 + (s * 10), y - (size / 2) - 12, 9, 9);
                    Canvas.Fill(chip, Palette.Alpha(Palette.Warning, 200));
                    Canvas.Text(chip.X + 2, chip.Y - 1, enemy.Statuses[s][..1].ToUpperInvariant(), Palette.Ink);
                }
            }
        }

        if (_playerLunge > 0 && _playerTarget is not null)
        {
            var (tx, ty) = Anchor(_playerTarget);
            var k = 1 - (_playerLunge / LungeSeconds);
            var from = new Vector2(area.CenterX, area.Bottom - 4);
            var to = new Vector2(tx, ty);
            var tip = Vector2.Lerp(from, to, (float)Math.Min(1, k * 1.4));
            var tail = Vector2.Lerp(from, to, (float)Math.Max(0, (k * 1.4) - 0.35));
            Raylib.DrawLineEx(tail, tip, 2, Palette.Alpha(Color.White, (int)(220 * (1 - k))));
            Raylib.DrawLineEx(tail, tip, 1, Palette.Alpha(Palette.Accent, 200));
        }

        foreach (var number in _floating)
        {
            var k = number.Age / FloatSeconds;
            var color = number.Kind switch
            {
                FloatingKind.Heal => Palette.Success,
                FloatingKind.Miss => Palette.Muted,
                FloatingKind.Status => Palette.Warning,
                _ => Palette.Failure,
            };
            var y = number.Y - (int)(k * 16);
            var alpha = (int)(255 * (1 - (k * k)));
            var w = Canvas.Width(number.Text);
            Canvas.BoldText(number.X - (w / 2) + 1, y + 1, number.Text, Palette.Alpha(Palette.Ink, alpha));
            Canvas.BoldText(number.X - (w / 2), y, number.Text, Palette.Alpha(color, alpha));
        }

        if (_playerFlash > 0)
        {
            var alpha = (int)(150 * _playerFlash / (FlashSeconds * 1.5));
            var red = Palette.Alpha(Palette.Danger, alpha);
            Raylib.DrawRectangleGradientH(area.X, area.Y, 30, area.Height, red, Palette.Alpha(red, 0));
            Raylib.DrawRectangleGradientH(area.Right - 30, area.Y, 30, area.Height, Palette.Alpha(red, 0), red);
            Raylib.DrawRectangleGradientV(area.X, area.Bottom - 20, area.Width, 20, Palette.Alpha(red, 0), red);
        }

        Canvas.Unclip();
    }

    private (int X, int Y) Slot(int index)
    {
        var count = Math.Max(1, _enemies.Count);
        var spacing = Math.Min(90, (area.Width - 60) / count);
        var x = area.CenterX - ((count - 1) * spacing / 2) + (index * spacing);
        var y = area.Y + 66 + ((index % 2) * (count > 2 ? 6 : 0));
        return (x, y);
    }

    private (int X, int Y) Anchor(string name)
    {
        var index = _slots.ToList().IndexOf(name);
        return index < 0 ? (area.CenterX, area.CenterY) : Slot(index);
    }

    private sealed class Figure(FigureLook look)
    {
        public FigureLook Look { get; } = look;

        public double Flash { get; set; }

        public double Lunge { get; set; }

        public double Falling { get; set; }

        public bool Fled { get; set; }

        public bool Gone { get; set; }
    }

    private sealed class Floating(string text, int x, int y, FloatingKind kind)
    {
        public string Text { get; } = text;

        public int X { get; } = x;

        public int Y { get; } = y;

        public FloatingKind Kind { get; } = kind;

        public double Age { get; set; }
    }
}
