using Raylib_cs;
using RpgEngine.Pixel.Layout;
using RpgEngine.Pixel.Presentation;

namespace RpgEngine.Pixel.Rendering;

internal sealed class AudioPanel(AudioDirector director, AudioPreferencesStore store, AudioPlayer player)
{
    private static readonly PixelRect Toggle = new(244, 3, 56, 12);
    private static readonly PixelRect Box = new(112, 66, 256, 138);
    private int _selected;
    private bool _saveFailed;
    public bool Visible { get; private set; }

    public bool HandleInput(int mx, int my)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F4))
        {
            Change(director.Preferences with { Muted = !director.Preferences.Muted });
            return true;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.F5) || (Raylib.IsMouseButtonPressed(MouseButton.Left) && Toggle.Contains(mx, my)))
        {
            Visible = !Visible;
            return true;
        }
        if (!Visible) return false;
        // Consume typed characters as well, so closing the panel cannot insert them into a command.
        while (Raylib.GetCharPressed() != 0) { }
        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) Visible = false;
        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.Down)) _selected = 1 - _selected;
        var delta = Raylib.IsKeyPressed(KeyboardKey.Left) ? -0.1f : Raylib.IsKeyPressed(KeyboardKey.Right) ? 0.1f : 0;
        if (delta != 0) SetVolume(_selected, Volume(_selected) + delta);
        if (Raylib.IsKeyPressed(KeyboardKey.M)) Change(director.Preferences with { Muted = !director.Preferences.Muted });
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            for (var row = 0; row < 2; row++)
            {
                var slider = Slider(row);
                if (slider.Contains(mx, my))
                {
                    _selected = row;
                    SetVolume(row, (mx - slider.X) / (float)(slider.Width - 1));
                }
            }
            if (new PixelRect(Box.X + 12, Box.Y + 92, 104, 14).Contains(mx, my))
                Change(director.Preferences with { Muted = !director.Preferences.Muted });
            if (new PixelRect(Box.Right - 68, Box.Y + 92, 56, 14).Contains(mx, my)) Visible = false;
        }
        return true;
    }

    public void Draw()
    {
        Canvas.Panel(Toggle, Palette.PanelBack, Palette.PanelEdge);
        Canvas.Text(Toggle.X + 3, Toggle.Y + 1, director.Preferences.Muted ? "F5 Muted" : "F5 Sound", Palette.Accent);
        if (!Visible) return;
        Canvas.Fill(PixelLayout.Canvas, Palette.Alpha(Color.Black, 160));
        Canvas.Panel(Box, Palette.PanelBack, Palette.Accent);
        Canvas.BoldText(Box.X + 12, Box.Y + 9, "Sound", Palette.Title);
        Canvas.Text(Box.X + 12, Box.Y + 24, player.Available ? "Arrows adjust; M mutes" : "Audio device unavailable", Palette.Muted);
        for (var row = 0; row < 2; row++)
        {
            var slider = Slider(row);
            Canvas.Text(Box.X + 12, slider.Y + 2, row == 0 ? "Effects" : "Ambience", Palette.Narration);
            Canvas.Bar(slider, Volume(row), Palette.Accent, Palette.HudBack);
            if (row == _selected) Canvas.Frame(slider, Palette.Title);
            Canvas.Text(slider.Right + 5, slider.Y + 2, $"{Math.Round(Volume(row) * 100)}%", Palette.Muted);
        }
        Canvas.Text(Box.X + 12, Box.Y + 94, director.Preferences.Muted ? "[M] Unmute" : "[M] Mute", Palette.Accent);
        Canvas.Text(Box.Right - 68, Box.Y + 94, "Close F5", Palette.Accent);
        Canvas.Text(Box.X + 12, Box.Y + 118, _saveFailed ? "Could not save preferences" : "Settings saved automatically", _saveFailed ? Palette.Warning : Palette.Muted);
    }

    private static PixelRect Slider(int row) => new(Box.X + 80, Box.Y + 45 + row * 22, 112, 12);
    private float Volume(int row) => row == 0 ? director.Preferences.EffectsVolume : director.Preferences.AmbienceVolume;
    private void SetVolume(int row, float volume) => Change(row == 0
        ? director.Preferences with { EffectsVolume = volume }
        : director.Preferences with { AmbienceVolume = volume });
    private void Change(AudioPreferences value)
    {
        director.Preferences = value.Normalize();
        player.ApplyVolumes(director.Preferences);
        _saveFailed = !store.Save(director.Preferences);
    }
}
