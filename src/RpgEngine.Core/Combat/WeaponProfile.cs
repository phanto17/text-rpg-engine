using RpgEngine.Core.Characters;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;

namespace RpgEngine.Core.Combat;

/// <summary>How the player attacks with their current weapon: to-hit bonus and damage including the ability modifier.</summary>
public sealed record WeaponProfile(string Name, Ability Ability, int ToHitBonus, DiceExpression Damage, DamageType DamageType);
