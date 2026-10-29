using Raylib_cs;
using RpgEngine.Pixel.Game;
using RpgEngine.Pixel.Layout;

namespace RpgEngine.Pixel.Rendering;

/// <summary>The bottom strip: the prompt, the flow of numbered buttons for the current choices, and the command line.</summary>
internal sealed class ActionBarRenderer
{
    private IReadOnlyList<PixelRect?> _buttons = [];

    /// <summary>The index of the button under the mouse, if any.</summary>
    public int? Hovered { get; private set; }

    public int? Hit(int x, int y)
    {
        for (var i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] is { } rect && rect.Contains(x, y))
            {
                return i;
            }
        }

        return null;
    }

    public void Draw(PixelGame game, int mouseX, int mouseY, double time)
    {
        Canvas.Panel(PixelLayout.ActionBar, Palette.HudBack, Palette.PanelEdge);

        var actions = game.Actions;
        var labels = actions.Select((action, i) => i < 9 ? $"{i + 1} {action.Label}" : action.Label).ToList();
        _buttons = ButtonFlow.Flow(labels, PixelLayout.Buttons, Canvas.Width, PixelLayout.ButtonHeight, PixelLayout.ButtonGap, PixelLayout.ButtonPadding);
        Hovered = Hit(mouseX, mouseY);

        var hidden = 0;
        for (var i = 0; i < actions.Count; i++)
        {
            if (_buttons[i] is not { } rect)
            {
                hidden++;
                continue;
            }

            var action = actions[i];
            var hover = Hovered == i && action.Enabled;
            Canvas.Fill(rect, hover ? Palette.ButtonHover : Palette.ButtonBack);
            Canvas.Frame(rect, action.Enabled ? (hover ? Palette.Accent : Palette.ButtonEdge) : Palette.PanelEdge);
            var color = action.Enabled ? Palette.For(action.Tone) : Palette.Alpha(Palette.Muted, 150);
            Canvas.Text(rect.X + PixelLayout.ButtonPadding, rect.Y, Canvas.Ellipsize(labels[i], rect.Width - (2 * PixelLayout.ButtonPadding)), color);
        }

        var prompt = Hovered is { } h && actions[h].Hint is { } hint ? hint : game.Prompt;
        if (hidden > 0)
        {
            prompt += $"  (+{hidden} more: type a number)";
        }

        Canvas.Text(PixelLayout.Prompt.X, PixelLayout.Prompt.Y, Canvas.Ellipsize(prompt, PixelLayout.Prompt.Width), Hovered is not null ? Palette.Muted : Palette.Accent);

        var input = PixelLayout.Input;
        Canvas.Fill(new PixelRect(input.X, input.Y - 1, input.Width, input.Height + 2), Palette.InputBack);
        Canvas.Clip(input);
        Canvas.Text(input.X + 2, input.Y, ">", Palette.Accent);
        var text = TextFit.Tail(game.Input, input.Width - 18, Canvas.Width);
        if (text.Length == 0)
        {
            var placeholder = game.InputIsName ? "type your name and press Enter" : "type a command (help) or a number";
            Canvas.Text(input.X + 10, input.Y, placeholder, Palette.Alpha(Palette.Muted, 120));
        }
        else
        {
            Canvas.Text(input.X + 10, input.Y, text, Palette.Narration);
        }

        if ((time * 2) % 1 < 0.5)
        {
            Raylib.DrawRectangle(input.X + 10 + Canvas.Width(text) + 1, input.Y + 1, 4, 8, Palette.Accent);
        }
        Canvas.Unclip();
    }
}
