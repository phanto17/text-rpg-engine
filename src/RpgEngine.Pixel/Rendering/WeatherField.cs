using Raylib_cs;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Layout;

namespace RpgEngine.Pixel.Rendering;

/// <summary>The particles drifting through the scene: rain, ash motes, spores, memory-salt glints, or embers.</summary>
internal sealed class WeatherField(PixelRect area)
{
    private readonly Random _random = new(7);
    private readonly List<Particle> _particles = [];
    private WeatherKind _kind = WeatherKind.None;

    public WeatherKind Kind
    {
        get => _kind;
        set
        {
            if (_kind == value)
            {
                return;
            }

            _kind = value;
            _particles.Clear();
            for (var i = 0; i < Count(value); i++)
            {
                _particles.Add(Spawn(value, anywhere: true));
            }
        }
    }

    public void Update(double dt)
    {
        for (var i = 0; i < _particles.Count; i++)
        {
            var p = _particles[i];
            p.Age += dt;
            p.X += p.Vx * dt + Math.Sin((p.Age * p.Wobble) + p.Phase) * p.Sway * dt;
            p.Y += p.Vy * dt;
            if (p.Y > area.Bottom + 2 || p.Y < area.Y - 4 || p.X < area.X - 4 || p.X > area.Right + 4 || p.Age > p.Life)
            {
                p = Spawn(_kind, anywhere: false);
            }

            _particles[i] = p;
        }
    }

    public void Draw(double time)
    {
        foreach (var p in _particles)
        {
            var x = (int)p.X;
            var y = (int)p.Y;
            switch (_kind)
            {
                case WeatherKind.Rain:
                    Raylib.DrawLine(x, y, x + (int)(p.Vx * 0.04), y + (int)(p.Vy * 0.04), new Color(150, 190, 220, 120));
                    break;
                case WeatherKind.AshMotes:
                    Raylib.DrawPixel(x, y, Palette.Rgba(170, 160, 150, (int)(90 + (40 * Math.Sin(time * 2 + p.Phase)))));
                    break;
                case WeatherKind.Spores:
                    var glow = (int)(120 + (60 * Math.Sin(time * 3 + p.Phase)));
                    Raylib.DrawPixel(x, y, Palette.Rgba(140, 230, 160, glow));
                    if (p.Size > 1)
                    {
                        Raylib.DrawPixel(x + 1, y, Palette.Rgba(90, 180, 110, glow / 2));
                    }

                    break;
                case WeatherKind.MemorySalt:
                    var twinkle = Math.Sin(time * 5 + p.Phase);
                    if (twinkle > 0)
                    {
                        Raylib.DrawPixel(x, y, Palette.Rgba(230, 235, 255, (int)(60 + (160 * twinkle))));
                        if (twinkle > 0.8)
                        {
                            Raylib.DrawPixel(x + 1, y, new Color(230, 235, 255, 80));
                            Raylib.DrawPixel(x - 1, y, new Color(230, 235, 255, 80));
                        }
                    }

                    break;
                case WeatherKind.Embers:
                    var heat = 1 - (p.Age / p.Life);
                    var ember = Palette.Rgba(255, (int)(120 + (100 * heat)), (int)(40 * heat), (int)(60 + (160 * heat)));
                    Raylib.DrawPixel(x, y, ember);
                    if (p.Size > 1)
                    {
                        Raylib.DrawPixel(x, y - 1, Palette.Alpha(ember, ember.A / 2));
                    }

                    break;
            }
        }
    }

    private static int Count(WeatherKind kind) => kind switch
    {
        WeatherKind.Rain => 80,
        WeatherKind.AshMotes => 45,
        WeatherKind.Spores => 32,
        WeatherKind.MemorySalt => 40,
        WeatherKind.Embers => 36,
        _ => 0,
    };

    private Particle Spawn(WeatherKind kind, bool anywhere)
    {
        var x = area.X + _random.NextDouble() * area.Width;
        var y = anywhere ? area.Y + _random.NextDouble() * area.Height : area.Y - 2;
        var phase = _random.NextDouble() * Math.PI * 2;
        return kind switch
        {
            WeatherKind.Rain => new Particle(x, y, -40, 220 + _random.Next(80), 1, phase, 0, 0, 3),
            WeatherKind.AshMotes => new Particle(x, y, 2, 6 + _random.Next(8), 1, phase, 0.8, 6, 40),
            WeatherKind.Spores => new Particle(x, y, 0, 3 + _random.Next(5), _random.Next(2) + 1, phase, 1.6, 10, 60),
            WeatherKind.MemorySalt => new Particle(x, y, 0, 2 + _random.Next(4), 1, phase, 0.5, 3, 90),
            WeatherKind.Embers => new Particle(x, anywhere ? y : area.Bottom + 1, 0, -(14 + _random.Next(18)), _random.Next(2) + 1, phase, 3, 8, 2 + (_random.NextDouble() * 3)),
            _ => new Particle(x, y, 0, 0, 1, phase, 0, 0, 1),
        };
    }

    private struct Particle(double x, double y, double vx, double vy, int size, double phase, double wobble, double sway, double life)
    {
        public double X = x;
        public double Y = y;
        public double Vx = vx;
        public double Vy = vy;
        public int Size = size;
        public double Phase = phase;
        public double Wobble = wobble;
        public double Sway = sway;
        public double Life = life;
        public double Age;
    }
}
