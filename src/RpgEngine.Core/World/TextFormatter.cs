using RpgEngine.Core.Characters;
using RpgEngine.Core.Content;

namespace RpgEngine.Core.World;

/// <summary>Fills content text tokens: <c>{player.name}</c> and <c>{player.class}</c>.</summary>
public sealed class TextFormatter
{
    private readonly GameContent _content;

    public TextFormatter(GameContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        _content = content;
    }

    public string Format(string text, PlayerCharacter player)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(player);

        var className = _content.Classes.TryGetValue(player.ClassId, out var cls) ? cls.Name : player.ClassId;
        return text
            .Replace("{player.name}", player.Name, StringComparison.Ordinal)
            .Replace("{player.class}", className, StringComparison.Ordinal);
    }
}
