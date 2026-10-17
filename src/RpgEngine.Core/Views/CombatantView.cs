namespace RpgEngine.Core.Views;

public sealed record CombatantView(
    string Id,
    string Name,
    bool IsPlayer,
    int Hp,
    int MaxHp,
    int ArmorClass,
    IReadOnlyList<string> Statuses,
    bool IsActive);
