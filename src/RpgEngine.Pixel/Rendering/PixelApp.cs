using System.Numerics;
using Raylib_cs;
using RpgEngine.Core.Engine;
using RpgEngine.Pixel.Game;
using RpgEngine.Pixel.Layout;
using RpgEngine.Pixel.Presentation;
using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Pixel.Rendering;

/// <summary>
/// The window: a 480 by 270 render texture scaled up with point filtering into a resizable window,
/// the four panels drawn onto it every frame, and keyboard and mouse input routed to the headless
/// <see cref="PixelGame"/>.
/// </summary>
internal sealed class PixelApp : IDisposable
{
    private const int DefaultWidth = 1280;
    private const int DefaultHeight = 720;

    private readonly PixelGame _game;
    private readonly PresentationCatalog _catalog;
    private readonly TextureCache _textures;
    private readonly AudioDirector _audioDirector;
    private readonly AudioPlayer _audio;
    private readonly AudioPanel _audioPanel;
    private readonly SceneRenderer _scene = new(PixelLayout.Scene);
    private readonly CombatStage _stage = new(PixelLayout.Scene);
    private readonly PortraitBox _portrait = new();
    private readonly DiceBanner _dice = new();
    private readonly MoodCaption _caption = new();
    private readonly HudRenderer _hud = new(PixelLayout.Hud);
    private readonly LogRenderer _log = new(PixelLayout.Log);
    private readonly ActionBarRenderer _actions = new();
    private readonly Queue<string> _script;
    private readonly string? _screenshot;
    private readonly string? _recordingDirectory;
    private bool _crt;
    private bool _integerScale = true;
    private double _roomFade;
    private double _time;
    private int _frame;
    private bool _done;
    private RenderTexture2D _canvas;
    private bool _open;

    public PixelApp(PixelGame game, PresentationCatalog catalog, string dataDirectory, AudioPreferencesStore preferencesStore, bool crt, IReadOnlyList<string>? script = null, string? screenshot = null, string? recordingDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(catalog);
        _game = game;
        _catalog = catalog;
        _textures = new TextureCache(dataDirectory);
        _audioDirector = new AudioDirector(catalog.Audio, catalog.Bindings.LanternItem) { Preferences = preferencesStore.Load() };
        _audio = new AudioPlayer(dataDirectory);
        _audioPanel = new AudioPanel(_audioDirector, preferencesStore, _audio);
        _crt = crt;
        _script = new Queue<string>(script ?? []);
        _screenshot = screenshot;
        _recordingDirectory = recordingDirectory;
    }

    public void Run()
    {
        if (_recordingDirectory is not null)
        {
            if (Directory.Exists(_recordingDirectory)) throw new IOException("Recording requires a fresh directory.");
            Directory.CreateDirectory(_recordingDirectory);
        }
        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);
        Raylib.InitWindow(DefaultWidth, DefaultHeight, _game.Content.Manifest.Title);
        Raylib.SetWindowMinSize(PixelLayout.Width, PixelLayout.Height);
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.SetTargetFPS(60);
        _open = true;
        _audio.Open();
        _canvas = Raylib.LoadRenderTexture(PixelLayout.Width, PixelLayout.Height);
        Raylib.SetTextureFilter(_canvas.Texture, TextureFilter.Point);

        while (!Raylib.WindowShouldClose() && !_game.WantsQuit && !_done)
        {
            var dt = _recordingDirectory is null ? Math.Min(0.1, Raylib.GetFrameTime()) : 1.0 / 60;
            _time += dt;
            _audioDirector.Advance(dt);
            _frame++;
            PlayScript();
            var viewport = ViewportFit.Fit(Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), _integerScale);
            var mouse = Raylib.GetMousePosition();
            var (mx, my) = ViewportFit.ToCanvas(viewport, mouse.X, mouse.Y);

            HandleInput(mx, my);
            if (!_audioPanel.Visible) _game.Update(dt);
            foreach (var cue in _game.Sequencer.TakeCues())
            {
                Apply(cue);
            }

            SyncVisuals(_audioPanel.Visible ? 0 : dt);
            var audioRoom = _game.Session?.State.CurrentRoomId ?? _game.Content.Manifest.StartRoom;
            _audio.Update(_audioDirector.Ambience(audioRoom), dt, _audioDirector.Preferences);

            Raylib.BeginTextureMode(_canvas);
            Raylib.ClearBackground(Palette.PanelBack);
            DrawCanvas(dt, mx, my);
            Raylib.EndTextureMode();
            if (_recordingDirectory is not null && _frame % 5 == 0)
            {
                SaveCanvas(Path.Combine(_recordingDirectory, $"frame_{_frame:D6}.png"), scale: 1);
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Palette.Letterbox);
            Raylib.DrawTexturePro(
                _canvas.Texture,
                new Rectangle(0, 0, PixelLayout.Width, -PixelLayout.Height),
                new Rectangle(viewport.X, viewport.Y, viewport.Width, viewport.Height),
                Vector2.Zero,
                0,
                Color.White);
            if (_crt)
            {
                CrtOverlay.Draw(viewport);
            }

            Raylib.EndDrawing();
        }
    }

    // Screenshots skip narration; recordings wait for it and preserve every presentation hold.
    private void PlayScript()
    {
        if (_audioPanel.Visible) return;
        if (_screenshot is null && _recordingDirectory is null)
        {
            return;
        }

        if (_recordingDirectory is not null && _frame >= 7200)
        {
            throw new TimeoutException("Recording exceeded two minutes; check the scripted inputs.");
        }

        if (_frame % 12 == 0 && (_recordingDirectory is null || _game.Sequencer.IsIdle) && _script.TryDequeue(out var line))
        {
            if (_recordingDirectory is null) _game.Sequencer.Skip();
            _game.Submit(line);
            _audio.Play(_audioDirector.Effect("select"), _audioDirector.Preferences);
            return;
        }

        if (_script.Count == 0 && _frame % 12 == 0 && _game.Sequencer.IsIdle && _frame > 24)
        {
            if (_screenshot is not null) SaveCanvas(_screenshot, scale: 2);
            _done = true;
        }
    }

    private void SaveCanvas(string path, int scale)
    {
        var output = Raylib.LoadRenderTexture(PixelLayout.Width * scale, PixelLayout.Height * scale);
        Raylib.SetTextureFilter(output.Texture, TextureFilter.Point);
        Raylib.BeginTextureMode(output);
        Raylib.ClearBackground(Palette.Letterbox);
        var viewport = new PixelRect(0, 0, PixelLayout.Width * scale, PixelLayout.Height * scale);
        Raylib.DrawTexturePro(_canvas.Texture, new Rectangle(0, 0, PixelLayout.Width, -PixelLayout.Height),
            new Rectangle(0, 0, viewport.Width, viewport.Height), Vector2.Zero, 0, Color.White);
        if (_crt) CrtOverlay.Draw(viewport);
        Raylib.EndTextureMode();
        var image = Raylib.LoadImageFromTexture(output.Texture);
        Raylib.ImageFlipVertical(ref image);
        var saved = Raylib.ExportImage(image, path);
        Raylib.UnloadImage(image);
        Raylib.UnloadRenderTexture(output);
        if (!saved) throw new IOException($"Could not save canvas to {path}.");
    }

    public void Dispose()
    {
        if (!_open)
        {
            return;
        }

        _textures.Dispose();
        _audio.Dispose();
        Raylib.UnloadRenderTexture(_canvas);
        Raylib.CloseWindow();
        _open = false;
    }

    private void HandleInput(int mx, int my)
    {
        if (_audioPanel.HandleInput(mx, my)) return;
        for (var key = Raylib.GetCharPressed(); key > 0; key = Raylib.GetCharPressed())
        {
            var c = (char)key;
            if (_game.Input.Length == 0 && !_game.InputIsName && c is >= '1' and <= '9')
            {
                _game.Pick(c - '1');
                _audio.Play(_audioDirector.Effect("select"), _audioDirector.Preferences);
            }
            else
            {
                _game.Type(c);
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter))
        {
            _game.SubmitInput();
            _audio.Play(_audioDirector.Effect("select"), _audioDirector.Preferences);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) || Raylib.IsKeyPressedRepeat(KeyboardKey.Backspace))
        {
            _game.Backspace();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            if (_game.Input.Length > 0)
            {
                while (_game.Input.Length > 0)
                {
                    _game.Backspace();
                }
            }
            else
            {
                _game.Back();
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Tab))
        {
            _game.Tab = (HudTab)(((int)_game.Tab + 1) % 4);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.F2))
        {
            _crt = !_crt;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.F3))
        {
            _integerScale = !_integerScale;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.F11))
        {
            Raylib.ToggleBorderlessWindowed();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.PageUp))
        {
            _log.Scroll(3);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.PageDown))
        {
            _log.Scroll(-3);
        }

        var wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0 && PixelLayout.Log.Contains(mx, my))
        {
            _log.Scroll(wheel > 0 ? 2 : -2);
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            if (_actions.Hit(mx, my) is { } index)
            {
                _game.Pick(index);
                _audio.Play(_audioDirector.Effect("select"), _audioDirector.Preferences);
            }
            else if (PixelLayout.Hud.Contains(mx, my))
            {
                if (_hud.Click(_game, mx, my) is { } command)
                {
                    _game.Submit(command);
                }
            }
            else if (PixelLayout.Log.Contains(mx, my) || PixelLayout.Scene.Contains(mx, my))
            {
                if (!_game.Sequencer.IsIdle)
                {
                    _game.Sequencer.Skip();
                }
            }
        }
    }

    private void Apply(SequenceCue cue)
    {
        _audio.Play(_audioDirector.Resolve(cue), _audioDirector.Preferences);
        switch (cue)
        {
            case DiceBannerCue dice:
                _dice.Play(dice);
                break;
            case RoomCue room:
                _roomFade = 0.5;
                _caption.Show(room.RoomId, _catalog.Room(room.RoomId).Caption);
                break;
            case RoomRevealedCue revealed:
                _caption.Complete(revealed.RoomId);
                break;
            case SpeakerCue speaker when _game.Session is { } session:
                var npcId = _catalog.SpeakerId(speaker.Speaker, session.Dialogue?.Dialogue.Id, session.State.CurrentRoomId);
                var look = _catalog.Npc(npcId);
                _portrait.Show(look.SpeakerName ?? speaker.Speaker, look);
                break;
            default:
                _stage.Apply(cue);
                break;
        }
    }

    private void SyncVisuals(double dt)
    {
        var session = _game.Session;
        _stage.Sync(session?.Combat, _catalog);
        if (session is null || session.Mode is not (GameMode.Dialogue or GameMode.Shop))
        {
            _portrait.Hide();
        }

        var roomId = session?.State.CurrentRoomId ?? _game.Content.Manifest.StartRoom;
        _scene.Update(_catalog.Room(roomId).Weather, dt);
        _stage.Update(dt);
        _portrait.Update(dt);
        _dice.Update(dt);
        _caption.Update(dt);
        _roomFade = Math.Max(0, _roomFade - dt);
    }

    private void DrawCanvas(double dt, int mx, int my)
    {
        var session = _game.Session;
        var roomId = session?.State.CurrentRoomId ?? _game.Content.Manifest.StartRoom;
        var state = SceneState.Read(session, _catalog.Bindings);
        var frame = new SceneFrame(
            _catalog.Room(roomId),
            roomId,
            _time,
            state.Resonance,
            state.SigilsLit,
            state.HasLantern,
            state.SunSealSet,
            state.MoonSealSet,
            state.BellMended,
            state.BrazierLit,
            state.SigilDoorOpen);

        _scene.Draw(frame, _textures);
        _stage.Draw(_textures);
        _portrait.Draw(PixelLayout.Scene, _textures);
        if (_roomFade > 0)
        {
            Canvas.Fill(PixelLayout.Scene, Palette.Alpha(Color.Black, (int)(255 * Math.Min(1, _roomFade / 0.5))));
        }

        var roomName = session is not null ? session.Content.Rooms[roomId].Name : _game.Content.Manifest.Title;
        var label = Canvas.Ellipsize(roomName, PixelLayout.Scene.Width - 76);
        Canvas.Fill(new PixelRect(PixelLayout.Scene.X + 3, PixelLayout.Scene.Y + 3, Canvas.Width(label) + 6, 11), Palette.Alpha(Palette.PanelBack, 170));
        Canvas.Text(PixelLayout.Scene.X + 6, PixelLayout.Scene.Y + 3, label, Palette.Title);
        if (!_dice.Active && !_stage.Active)
        {
            _caption.Draw(PixelLayout.Scene, Palette.Of(_catalog.Room(roomId).Accent));
        }

        _dice.Draw(PixelLayout.Scene);
        Canvas.Frame(PixelLayout.Scene, Palette.PanelEdge);

        _hud.Draw(_game, dt, _time, state.Resonance);
        _log.Draw(_game.Sequencer, _time);
        _actions.Draw(_game, mx, my, _time);
        _audioPanel.Draw();
    }
}
