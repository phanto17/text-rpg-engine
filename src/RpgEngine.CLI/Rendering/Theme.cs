using RpgEngine.Core.Content.Defs;
using Spectre.Console;

namespace RpgEngine.CLI.Rendering;

/// <summary>Every color the UI uses, in one place. Values are Spectre.Console markup styles.</summary>
internal static class Theme
{
    public const string Title = "bold #d7af5f";
    public const string Accent = "#d7af5f";
    public const string Narration = "#e4dccb";
    public const string Sensory = "italic #8a8a8a";
    public const string Speaker = "bold #d7af5f";
    public const string Dialogue = "#f0e6d2";
    public const string System = "#87afd7";
    public const string Success = "#87d787";
    public const string Failure = "#d75f5f";
    public const string Warning = "#d7af00";
    public const string Critical = "bold #ff87ff";
    public const string Muted = "grey50";
    public const string Danger = "bold #d75f5f";
    public const string Mana = "#5f87d7";
    public const string Gold = "#ffd75f";

    public static string For(TextStyle style) => style switch
    {
        TextStyle.Sensory => Sensory,
        TextStyle.Dialogue => Dialogue,
        TextStyle.System => System,
        TextStyle.Success => Success,
        TextStyle.Failure => Failure,
        TextStyle.Warning => Warning,
        _ => Narration,
    };

    /// <summary>Escapes <paramref name="text"/> and wraps it in a style.</summary>
    public static string Paint(string style, string text) => $"[{style}]{Markup.Escape(text)}[/]";
}
