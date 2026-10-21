using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Pixel.Presentation;

/// <summary>
/// Answers "how does this room, monster, or NPC look?" by layering <c>presentation.json</c> over
/// <see cref="PresentationDefaults"/>: each field the file sets wins, and every field it leaves out
/// keeps the default for that id (or a keyword guess for an id the defaults do not know).
/// </summary>
public sealed class PresentationCatalog
{
    private readonly GameContent _content;
    private readonly PresentationDef _presentation;
    private readonly Dictionary<string, RoomLook> _rooms = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FigureLook> _monsters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FigureLook> _npcs = new(StringComparer.Ordinal);

    public PresentationCatalog(GameContent content, PresentationDef? presentation = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        _content = content;
        _presentation = presentation ?? new PresentationDef();
    }

    public RoomLook Room(string roomId)
    {
        ArgumentNullException.ThrowIfNull(roomId);
        if (_rooms.TryGetValue(roomId, out var cached))
        {
            return cached;
        }

        var name = _content.Rooms.TryGetValue(roomId, out var room) ? room.Name : roomId;
        var look = PresentationDefaults.Rooms.TryGetValue(roomId, out var known) ? known : PresentationDefaults.GuessRoom(roomId, name);

        if (_presentation.Rooms.TryGetValue(roomId, out var def))
        {
            look = look with
            {
                Style = def.SceneStyle ?? look.Style,
                Weather = def.Weather ?? look.Weather,
                BackgroundTop = PixelColor.TryParse(def.Palette?.BackgroundTop) ?? look.BackgroundTop,
                BackgroundBottom = PixelColor.TryParse(def.Palette?.BackgroundBottom) ?? look.BackgroundBottom,
                Architecture = PixelColor.TryParse(def.Palette?.Architecture) ?? look.Architecture,
                Accent = PixelColor.TryParse(def.Palette?.Accent) ?? look.Accent,
                BackdropImage = def.BackdropImage ?? look.BackdropImage,
            };
        }

        return _rooms[roomId] = look;
    }

    public FigureLook Monster(string monsterId)
    {
        ArgumentNullException.ThrowIfNull(monsterId);
        if (_monsters.TryGetValue(monsterId, out var cached))
        {
            return cached;
        }

        var name = _content.Monsters.TryGetValue(monsterId, out var monster) ? monster.Name : monsterId;
        var look = PresentationDefaults.Monsters.TryGetValue(monsterId, out var known) ? known : PresentationDefaults.GuessMonster(monsterId, name);
        if (_presentation.Monsters.TryGetValue(monsterId, out var def))
        {
            look = look with
            {
                Emblem = def.Emblem ?? look.Emblem,
                Primary = PixelColor.TryParse(def.PrimaryColor) ?? look.Primary,
                Accent = PixelColor.TryParse(def.AccentColor) ?? look.Accent,
                Title = def.Title ?? look.Title,
                Image = def.SpriteImage ?? look.Image,
            };
        }

        return _monsters[monsterId] = look;
    }

    /// <summary>The look for an NPC id; a dialogue id is accepted too, for speakers met only through a conversation.</summary>
    public FigureLook Npc(string npcId)
    {
        ArgumentNullException.ThrowIfNull(npcId);
        if (_npcs.TryGetValue(npcId, out var cached))
        {
            return cached;
        }

        var name = _content.RoomList.SelectMany(r => r.Npcs).FirstOrDefault(n => n.Id == npcId)?.Name
            ?? (_content.Dialogues.TryGetValue(npcId, out var dialogue) ? dialogue.Speaker : npcId);
        var look = PresentationDefaults.Npcs.TryGetValue(npcId, out var known) ? known : PresentationDefaults.GuessNpc(npcId, name);
        if (_presentation.Npcs.TryGetValue(npcId, out var def))
        {
            look = look with
            {
                Emblem = def.Emblem ?? look.Emblem,
                Primary = PixelColor.TryParse(def.PrimaryColor) ?? look.Primary,
                Accent = PixelColor.TryParse(def.AccentColor) ?? look.Accent,
                Title = def.Title ?? look.Title,
                Image = def.PortraitImage ?? look.Image,
            };
        }

        return _npcs[npcId] = look;
    }

    /// <summary>
    /// The NPC speaking a dialogue: the NPC in <paramref name="roomId"/> whose dialogue it is, else
    /// any NPC with that dialogue, else the dialogue id itself.
    /// </summary>
    public string SpeakerOf(string dialogueId, string roomId)
    {
        ArgumentNullException.ThrowIfNull(dialogueId);
        ArgumentNullException.ThrowIfNull(roomId);
        if (_content.Rooms.TryGetValue(roomId, out var room) && room.Npcs.FirstOrDefault(n => n.Dialogue == dialogueId) is { } here)
        {
            return here.Id;
        }

        return _content.RoomList.SelectMany(r => r.Npcs).FirstOrDefault(n => n.Dialogue == dialogueId)?.Id ?? dialogueId;
    }
}
