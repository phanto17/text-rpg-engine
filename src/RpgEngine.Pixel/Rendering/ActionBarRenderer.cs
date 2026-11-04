using Raylib_cs;
using RpgEngine.Pixel.Game;
using RpgEngine.Pixel.Layout;

namespace RpgEngine.Pixel.Rendering;

/// <summary>The bottom strip: the prompt, the flow of numbered buttons for the current choices, and the command line.</summary>
internal sealed class ActionBarRenderer
{
    private IReadOnlyList<PixelRect?> _buttons = [];
    private IReadOnlyList<string> _labels = [];
    private IReadOnlyList<ButtonPage> _pages = [];
    private int _page;
    private static readonly PixelRect PreviousPage = new(376, 217, 29, 10);
    private static readonly PixelRect NextPage = new(448, 217, 29, 10);

    public void TurnPage(int direction)
    {
        _page = Math.Clamp(_page + direction, 0, Math.Max(0, _pages.Count - 1));
        _buttons = [];
    }

    public int? Shortcut(int number) => _pages.Count > 0 ? _pages[_page].ActionForShortcut(number) : null;

    public bool ClickNavigation(int x, int y)
    {
        if (_pages.Count < 2) return false;
        if (PreviousPage.Contains(x, y)) TurnPage(-1);
        else if (NextPage.Contains(x, y)) TurnPage(1);
        else return false;
        return true;
    }

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
        var labels = actions.Select((action, i) => $"{i + 1} {action.Label}").ToList();
        if (!_labels.SequenceEqual(labels))
        {
            _labels = labels;
            _pages = ButtonFlow.Pages(labels, PixelLayout.Buttons, Canvas.Width,
                PixelLayout.ButtonHeight, PixelLayout.ButtonGap, PixelLayout.ButtonPadding, maxButtons: 9);
            _page = 0;
        }
        var buttons = new PixelRect?[actions.Count];
        if (_pages.Count > 0)
        {
            var page = _pages[_page];
            for (var i = 0; i < page.Buttons.Count; i++) buttons[page.FirstAction + i] = page.Buttons[i];
        }
        _buttons = buttons;
        Hovered = Hit(mouseX, mouseY);

        for (var i = 0; i < actions.Count; i++)
        {
            if (_buttons[i] is not { } rect)
            {
                continue;
            }

            var action = actions[i];
            var hover = Hovered == i && action.Enabled;
            Canvas.Fill(rect, hover ? Palette.ButtonHover : Palette.ButtonBack);
            Canvas.Frame(rect, action.Enabled ? (hover ? Palette.Accent : Palette.ButtonEdge) : Palette.PanelEdge);
            var color = action.Enabled ? Palette.For(action.Tone) : Palette.Alpha(Palette.Muted, 150);
            var label = $"{i - _pages[_page].FirstAction + 1} {action.Label}";
            Canvas.Text(rect.X + PixelLayout.ButtonPadding, rect.Y, Canvas.Ellipsize(label, rect.Width - (2 * PixelLayout.ButtonPadding)), color);
        }

        var prompt = Hovered is { } h && actions[h].Hint is { } hint ? hint : game.Prompt;
        var promptWidth = _pages.Count > 1 ? PreviousPage.X - PixelLayout.Prompt.X - 4 : PixelLayout.Prompt.Width;
        Canvas.Text(PixelLayout.Prompt.X, PixelLayout.Prompt.Y, Canvas.Ellipsize(prompt, promptWidth), Hovered is not null ? Palette.Muted : Palette.Accent);
        if (_pages.Count > 1)
        {
            DrawNavigation(PreviousPage, "Prev", _page > 0);
            var counter = $"{_page + 1}/{_pages.Count}";
            Canvas.Text(426 - Canvas.Width(counter) / 2, PixelLayout.Prompt.Y, counter, Palette.Muted);
            DrawNavigation(NextPage, "Next", _page + 1 < _pages.Count);
        }

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

    private static void DrawNavigation(PixelRect rect, string label, bool enabled)
    {
        Canvas.Fill(rect, Palette.ButtonBack);
        Canvas.Frame(rect, enabled ? Palette.ButtonEdge : Palette.PanelEdge);
        Canvas.Text(rect.X + 3, rect.Y, label, enabled ? Palette.Accent : Palette.Muted);
    }
}
