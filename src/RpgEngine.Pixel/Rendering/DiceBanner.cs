using System.Numerics;
using Raylib_cs;
using RpgEngine.Core.Dice;
using RpgEngine.Pixel.Layout;
using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Pixel.Rendering;

/// <summary>The d20 banner: a tumbling die that settles on the natural roll beside the check's breakdown and verdict.</summary>
internal sealed class DiceBanner
{
    private const double TumbleSeconds = 0.45;
    private const double TotalSeconds = 1.15;

    private DiceBannerCue? _cue;
    private double _age;
    private int _tumbleFace;
    private int _seed;

    public bool Active => _cue is not null;

    public void Play(DiceBannerCue cue)
    {
        _cue = cue;
        _age = 0;
        _seed = cue.GetHashCode();
    }

    public void Update(double dt)
    {
        if (_cue is null)
        {
            return;
        }

        _age += dt;
        if (_age < TumbleSeconds)
        {
            // A cheap hash of elapsed ticks keeps the tumble deterministic for a given roll.
            var tick = (int)(_age * 20);
            _tumbleFace = 1 + (Math.Abs(HashCode.Combine(_seed, tick)) % 20);
        }

        if (_age > TotalSeconds)
        {
            _cue = null;
        }
    }

    public void Draw(PixelRect scene)
    {
        if (_cue is null)
        {
            return;
        }

        var tumbling = _age < TumbleSeconds;
        var fade = _age > TotalSeconds - 0.2 ? (TotalSeconds - _age) / 0.2 : 1;
        var alpha = (int)(255 * Math.Clamp(fade, 0, 1));
        var banner = new PixelRect(scene.X + 46, scene.Bottom - 30, scene.Width - 92, 26);
        Canvas.Fill(banner, Palette.Alpha(Palette.PanelBack, (int)(alpha * 0.9)));
        Canvas.Frame(banner, Palette.Alpha(Palette.Accent, alpha));

        var center = new Vector2(banner.X + 15, banner.CenterY);
        var face = tumbling ? _tumbleFace : _cue.Roll.Natural;
        var tone = tumbling ? Palette.Narration : Palette.For(EventNarrator.VerdictTone(_cue.Outcome));
        var spin = tumbling ? (float)(_age * 720) : 30f;
        Raylib.DrawPoly(center, 6, 10, spin, Palette.Alpha(Emblems.Shade(tone, 0.45), alpha));
        Raylib.DrawPolyLines(center, 6, 10, spin, Palette.Alpha(tone, alpha));
        Raylib.DrawPolyLines(center, 3, 6, spin + 30, Palette.Alpha(tone, alpha));
        var faceText = face.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Canvas.BoldText((int)center.X - (Canvas.Width(faceText) / 2), (int)center.Y - 5, faceText, Palette.Alpha(Color.White, alpha));

        var textX = banner.X + 30;
        Canvas.Text(textX, banner.Y + 3, Canvas.Ellipsize(_cue.Label, banner.Width - 40), Palette.Alpha(Palette.Accent, alpha));
        if (tumbling)
        {
            Canvas.Text(textX, banner.Y + 14, $"d20 ...  vs {_cue.Versus}", Palette.Alpha(Palette.Muted, alpha));
            return;
        }

        var faces = _cue.Roll.Mode == RollMode.Normal ? string.Empty : $" ({string.Join("/", _cue.Roll.Faces)})";
        var breakdown = $"d20 {_cue.Roll.Natural}{faces} {EventNarrator.Signed(_cue.Bonus)} = {_cue.Total} vs {_cue.Versus}";
        Canvas.Text(textX, banner.Y + 14, breakdown, Palette.Alpha(Palette.Narration, alpha));
        var verdict = _cue.Outcome switch
        {
            DiceOutcome.CriticalSuccess => "CRIT!",
            DiceOutcome.CriticalFailure => "FUMBLE",
            DiceOutcome.Success => "SUCCESS",
            _ => "FAIL",
        };
        var pulse = (int)(alpha * (0.7 + (0.3 * Math.Sin(_age * 14))));
        Canvas.BoldText(banner.Right - Canvas.Width(verdict) - 6, banner.Y + 8, verdict, Palette.Alpha(tone, pulse));
    }
}
