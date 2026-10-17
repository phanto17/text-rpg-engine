using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Core.Views;

/// <summary>A spell or technique in the combat menu with its cost (e.g. "2 MP", "1 use left", "free").</summary>
public sealed record SpellOptionView(SpellDef Spell, bool CanCast, string CostText);
