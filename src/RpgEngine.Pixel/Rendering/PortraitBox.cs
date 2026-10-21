using System.Numerics;
using Raylib_cs;
using RpgEngine.Pixel.Layout;
using RpgEngine.Pixel.Presentation;

namespace RpgEngine.Pixel.Rendering;

/// <summary>The speaker's portrait during a conversation, sliding in from the left edge of the scene.</summary>
internal sealed class PortraitBox
{
    private const double SlideSeconds = 0.25;
    private const int BoxWidth = 72;
    private const int BoxHeight = 98;

    private string _name = string.Empty;
    private FigureLook? _look;
    private double _progress;
    private bool _showing;

    public bool Visible => _progress > 0;

    public void Show(string name, FigureLook look)
    {
        _name = name;
        _look = look;
        _showing = true;
    }

    public void Hide() => _showing = false;

    public void Update(double dt)
    {
        var target = _showing ? 1 : 0;
        var step = dt / SlideSeconds;
        _progress = _progress < target ? Math.Min(target, _progress + step) : Math.Max(target, _progress - step);
    }

    public void Draw(PixelRect scene, TextureCache textures)
    {
        if (!Visible || _look is null)
        {
            return;
        }

        var eased = 1 - Math.Pow(1 - _progress, 3);
        var x = scene.X + (int)Math.Round(-BoxWidth - 4 + ((BoxWidth + 8) * eased));
        var y = scene.Y + ((scene.Height - BoxHeight) / 2);
        var box = new PixelRect(x, y, BoxWidth, BoxHeight);

        Canvas.Clip(scene);
        Canvas.Fill(box, Palette.Alpha(Palette.PanelBack, 230));
        Canvas.Frame(box, Palette.Of(_look.Accent));
        Canvas.Frame(box.Inset(1), Palette.PanelEdge);

        var portrait = new PixelRect(box.X + 8, box.Y + 6, BoxWidth - 16, 56);
        Canvas.Fill(portrait, Emblems.Shade(Palette.Of(_look.Primary), 0.35));
        if (textures.Get(_look.Image) is { } image)
        {
            Raylib.DrawTexturePro(image, new Rectangle(0, 0, image.Width, image.Height), new Rectangle(portrait.X, portrait.Y, portrait.Width, portrait.Height), Vector2.Zero, 0, Color.White);
        }
        else
        {
            Emblems.Draw(_look.Emblem, portrait.CenterX, portrait.CenterY + 2, 44, Palette.Of(_look.Primary), Palette.Of(_look.Accent));
        }

        Canvas.Frame(portrait, Palette.PanelEdgeLight);
        var name = Canvas.Ellipsize(_name, BoxWidth - 8);
        Canvas.Text(box.X + ((BoxWidth - Canvas.Width(name)) / 2), box.Y + 66, name, Palette.Speaker);
        if (_look.Title.Length > 0)
        {
            var rows = TextWrapper.Wrap(Sequencing.LogLine.Of(_look.Title), BoxWidth - 8, Canvas.Width);
            for (var i = 0; i < rows.Count && i < 2; i++)
            {
                var text = string.Concat(rows[i].Select(s => s.Text));
                Canvas.Text(box.X + ((BoxWidth - Canvas.Width(text)) / 2), box.Y + 77 + (i * 10), text, Palette.Muted);
            }
        }

        Canvas.Unclip();
    }
}
