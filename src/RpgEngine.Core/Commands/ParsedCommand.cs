using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Core.Commands;

/// <summary>
/// A player command. <see cref="Word"/> is the verb as typed; <see cref="Target"/> and
/// <see cref="Secondary"/> are the objects (<c>use key on door</c>). Menu actions also set
/// <see cref="ObjectId"/> (a feature, NPC, or item id in the current room) so they resolve
/// exactly, without text matching.
/// </summary>
public sealed record ParsedCommand(
    CommandVerb Verb,
    string Word,
    string? Target = null,
    string? Secondary = null,
    Direction? Direction = null,
    string? ObjectId = null);
