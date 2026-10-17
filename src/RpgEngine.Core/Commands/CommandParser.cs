using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Core.Commands;

/// <summary>Turns typed text into a <see cref="ParsedCommand"/> using classic adventure-game verbs.</summary>
public static class CommandParser
{
    private static readonly Dictionary<string, Direction> Directions = new(StringComparer.Ordinal)
    {
        ["n"] = Direction.North, ["north"] = Direction.North,
        ["s"] = Direction.South, ["south"] = Direction.South,
        ["e"] = Direction.East, ["east"] = Direction.East,
        ["w"] = Direction.West, ["west"] = Direction.West,
        ["u"] = Direction.Up, ["up"] = Direction.Up,
        ["d"] = Direction.Down, ["down"] = Direction.Down,
        ["in"] = Direction.In, ["inside"] = Direction.In,
        ["out"] = Direction.Out, ["outside"] = Direction.Out,
    };

    private static readonly Dictionary<string, CommandVerb> SimpleVerbs = new(StringComparer.Ordinal)
    {
        ["inventory"] = CommandVerb.Inventory, ["inv"] = CommandVerb.Inventory, ["i"] = CommandVerb.Inventory,
        ["stats"] = CommandVerb.Stats, ["status"] = CommandVerb.Stats, ["character"] = CommandVerb.Stats,
        ["char"] = CommandVerb.Stats, ["sheet"] = CommandVerb.Stats, ["c"] = CommandVerb.Stats,
        ["journal"] = CommandVerb.Journal, ["quests"] = CommandVerb.Journal, ["j"] = CommandVerb.Journal,
        ["help"] = CommandVerb.Help, ["h"] = CommandVerb.Help, ["commands"] = CommandVerb.Help,
        ["quit"] = CommandVerb.Quit, ["exit"] = CommandVerb.Quit, ["q"] = CommandVerb.Quit,
    };

    public static ParsedCommand Parse(string? input)
    {
        var trimmed = input?.Trim() ?? string.Empty;
        if (trimmed == "?")
        {
            return new ParsedCommand(CommandVerb.Help, "?");
        }

        var words = TextNormalizer.Words(trimmed);
        if (words.Count == 0)
        {
            return new ParsedCommand(CommandVerb.Empty, string.Empty);
        }

        var verb = words[0];
        var rest = words.Skip(1).ToList();

        if (rest.Count == 0 && Directions.TryGetValue(verb, out var bareDirection))
        {
            return new ParsedCommand(CommandVerb.Go, verb, Direction: bareDirection);
        }

        if (SimpleVerbs.TryGetValue(verb, out var simple))
        {
            return new ParsedCommand(simple, verb);
        }

        return verb switch
        {
            "look" or "l" => rest.Count == 0
                ? new ParsedCommand(CommandVerb.Look, verb)
                : new ParsedCommand(CommandVerb.Inspect, verb, Join(Without(rest, "at"))),
            "examine" or "x" or "inspect" => new ParsedCommand(CommandVerb.Inspect, verb, Join(rest)),
            "go" or "walk" or "move" or "head" or "run" or "climb" => ParseGo(verb, Without(rest, "to")),
            "take" or "get" or "grab" => new ParsedCommand(CommandVerb.Take, verb, Join(rest)),
            "pick" => new ParsedCommand(CommandVerb.Take, verb, Join(Without(rest, "up"))),
            "drop" or "discard" => new ParsedCommand(CommandVerb.Drop, verb, Join(rest)),
            "use" => ParseUse(verb, rest),
            "equip" or "wield" or "wear" => new ParsedCommand(CommandVerb.Equip, verb, Join(rest)),
            "talk" or "speak" or "greet" => new ParsedCommand(CommandVerb.Talk, verb, Join(Without(Without(rest, "to"), "with"))),
            "save" => new ParsedCommand(CommandVerb.Save, verb, Join(rest)),
            "load" or "restore" => new ParsedCommand(CommandVerb.Load, verb, Join(rest)),
            _ => new ParsedCommand(CommandVerb.Interact, verb, Join(rest)),
        };
    }

    private static ParsedCommand ParseGo(string verb, IReadOnlyList<string> rest)
    {
        if (rest.Count == 1 && Directions.TryGetValue(rest[0], out var direction))
        {
            return new ParsedCommand(CommandVerb.Go, verb, Direction: direction);
        }

        return new ParsedCommand(CommandVerb.Go, verb, Join(rest));
    }

    private static ParsedCommand ParseUse(string verb, IReadOnlyList<string> rest)
    {
        var split = rest.ToList().FindIndex(word => word is "on" or "with");
        if (split < 0)
        {
            return new ParsedCommand(CommandVerb.Use, verb, Join(rest));
        }

        return new ParsedCommand(CommandVerb.Use, verb, Join(rest.Take(split).ToList()), Join(rest.Skip(split + 1).ToList()));
    }

    // Drops a leading preposition: "look at door" -> "door".
    private static List<string> Without(IReadOnlyList<string> words, string leading) =>
        words.Count > 0 && words[0] == leading ? words.Skip(1).ToList() : words.ToList();

    private static string? Join(IReadOnlyList<string> words) => words.Count == 0 ? null : string.Join(' ', words);
}
