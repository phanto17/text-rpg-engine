namespace RpgEngine.Core.Events;

/// <summary>How a target's defenses changed incoming damage.</summary>
public enum DamageModifier
{
    None,
    Resisted,
    Vulnerable,
    Immune,
}
