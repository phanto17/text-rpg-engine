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
                Caption = def.MoodCaption ?? look.Caption,
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

    /// <summary>
    /// The look for an NPC id. A dialogue id is accepted too, and a presentation entry may be keyed
    /// by either: the NPC's own id wins, then the id of the dialogue it speaks.
    /// </summary>
    public FigureLook Npc(string npcId)
    {
        ArgumentNullException.ThrowIfNull(npcId);
        if (_npcs.TryGetValue(npcId, out var cached))
        {
            return cached;
        }

        var npc = _content.RoomList.SelectMany(r => r.Npcs).FirstOrDefault(n => n.Id == npcId);
        var name = npc?.Name ?? (_content.Dialogues.TryGetValue(npcId, out var dialogue) ? dialogue.Speaker : npcId);
        var look = PresentationDefaults.Npcs.TryGetValue(npcId, out var known) ? known : PresentationDefaults.GuessNpc(npcId, name);
        if (_presentation.Npcs.TryGetValue(npcId, out var def) || (npc is not null && _presentation.Npcs.TryGetValue(npc.Dialogue, out def)))
        {
            look = look with
            {
                Emblem = def.Emblem ?? look.Emblem,
                Primary = PixelColor.TryParse(def.PrimaryColor) ?? look.Primary,
                Accent = PixelColor.TryParse(def.AccentColor) ?? look.Accent,
                Title = def.Title ?? look.Title,
                Image = def.PortraitImage ?? look.Image,
                SpeakerName = def.SpeakerName ?? look.SpeakerName,
            };
        }

        return _npcs[npcId] = look;
    }

    /// <summary>
    /// Who is speaking a line, as an NPC id: the owner of <paramref name="dialogueId"/> when the line
    /// is part of a conversation, otherwise (a shop keeper's greeting) the NPC whose name or
    /// presentation <c>speakerName</c> is <paramref name="speaker"/>, preferring one in
    /// <paramref name="roomId"/>. Falls back to the dialogue id, then the speaker's name itself.
    /// </summary>
    public string SpeakerId(string speaker, string? dialogueId, string roomId)
    {
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(roomId);
        if (dialogueId is not null)
        {
            return SpeakerOf(dialogueId, roomId);
        }

        bool Named(NpcDef npc) =>
            string.Equals(npc.Name, speaker, StringComparison.Ordinal)
            || string.Equals(Npc(npc.Id).SpeakerName, speaker, StringComparison.Ordinal);

        var here = _content.Rooms.TryGetValue(roomId, out var room) ? room.Npcs.FirstOrDefault(Named) : null;
        return (here ?? _content.RoomList.SelectMany(r => r.Npcs).FirstOrDefault(Named))?.Id ?? speaker;
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
