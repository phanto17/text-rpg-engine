namespace RpgEngine.CLI.Screens;

internal enum ScreenAction
{
    Continue,
    Load,
    Title,
    Quit,
}

/// <summary>What a screen wants the app to do next. <see cref="Slot"/> is the save to load (null = ask).</summary>
internal sealed record ScreenResult(ScreenAction Action, string? Slot = null)
{
    public static readonly ScreenResult Continue = new(ScreenAction.Continue);
    public static readonly ScreenResult Title = new(ScreenAction.Title);
    public static readonly ScreenResult Quit = new(ScreenAction.Quit);

    public static ScreenResult Load(string? slot) => new(ScreenAction.Load, slot);
}
