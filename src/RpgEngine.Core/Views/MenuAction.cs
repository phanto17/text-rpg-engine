using RpgEngine.Core.Commands;

namespace RpgEngine.Core.Views;

/// <summary>A numbered option in the exploration menu; choosing it runs <see cref="Command"/>.</summary>
public sealed record MenuAction(string Label, MenuActionKind Kind, ParsedCommand Command);
