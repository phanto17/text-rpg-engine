namespace RpgEngine.CLI.Rendering;

/// <summary>Retro HP and MP bars, e.g. <c>██████░░░░ 6/10</c>, with an ASCII fallback for legacy consoles.</summary>
internal static class Bars
{
    public static string Hp(int current, int max, bool unicode, int width = 12)
    {
        var ratio = max <= 0 ? 0 : (double)current / max;
        var color = ratio > 0.5 ? Theme.Success : ratio > 0.25 ? Theme.Warning : Theme.Failure;
        return Bar(current, max, width, color, unicode);
    }

    public static string Mana(int current, int max, bool unicode, int width = 12) => Bar(current, max, width, Theme.Mana, unicode);

    private static string Bar(int current, int max, int width, string color, bool unicode)
    {
        var clamped = Math.Clamp(current, 0, Math.Max(max, 0));
        var filled = max <= 0 ? 0 : (int)Math.Round(width * (double)clamped / max, MidpointRounding.AwayFromZero);
        var full = unicode ? '█' : '#';
        var empty = unicode ? '░' : '-';
        return $"[{color}]{new string(full, filled)}[/][grey30]{new string(empty, width - filled)}[/] {current}/{max}";
    }
}
