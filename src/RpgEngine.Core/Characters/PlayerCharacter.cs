namespace RpgEngine.Core.Characters;

/// <summary>The player's persistent character: scores, proficiencies, vitals, gear, and resources.</summary>
public sealed class PlayerCharacter : ICheckSubject
{
    private readonly HashSet<string> _proficientSkills;
    private readonly HashSet<string> _expertiseSkills;
    private readonly HashSet<Ability> _savingThrows;
    private readonly IReadOnlyList<int> _proficiencyBonusByLevel;
    private readonly Dictionary<string, int> _encounterUses = new(StringComparer.Ordinal);
    private readonly List<string> _spellIds;

    public PlayerCharacter(
        string name,
        string classId,
        string backgroundId,
        AbilityScores abilities,
        int maxHp,
        int maxMana,
        IEnumerable<string> proficientSkills,
        IEnumerable<string> expertiseSkills,
        IEnumerable<Ability> savingThrows,
        IEnumerable<string> spellIds,
        IReadOnlyList<int> proficiencyBonusByLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(classId);
        ArgumentException.ThrowIfNullOrWhiteSpace(backgroundId);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxHp, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(maxMana);
        ArgumentNullException.ThrowIfNull(proficientSkills);
        ArgumentNullException.ThrowIfNull(expertiseSkills);
        ArgumentNullException.ThrowIfNull(savingThrows);
        ArgumentNullException.ThrowIfNull(spellIds);
        ArgumentNullException.ThrowIfNull(proficiencyBonusByLevel);

        Name = name.Trim();
        ClassId = classId;
        BackgroundId = backgroundId;
        Abilities = abilities;
        MaxHp = maxHp;
        CurrentHp = maxHp;
        MaxMana = maxMana;
        CurrentMana = maxMana;
        _proficientSkills = new HashSet<string>(proficientSkills, StringComparer.Ordinal);
        _expertiseSkills = new HashSet<string>(expertiseSkills, StringComparer.Ordinal);
        _savingThrows = [.. savingThrows];
        _spellIds = spellIds.ToList();
        _proficiencyBonusByLevel = proficiencyBonusByLevel;

        if (!_expertiseSkills.IsSubsetOf(_proficientSkills))
        {
            throw new ArgumentException("Expertise can only be chosen for proficient skills.", nameof(expertiseSkills));
        }
    }

    public string Name { get; }

    public string ClassId { get; }

    /// <summary>The protagonist's burden (background), chosen after the class.</summary>
    public string BackgroundId { get; }

    public int Level { get; private set; } = 1;

    public int Xp { get; private set; }

    public AbilityScores Abilities { get; }

    public int MaxHp { get; private set; }

    public int CurrentHp { get; private set; }

    public int MaxMana { get; private set; }

    public int CurrentMana { get; private set; }

    public int Gold { get; private set; }

    public Inventory Inventory { get; } = new();

    public string? EquippedWeaponId { get; set; }

    public string? EquippedArmorId { get; set; }

    public IReadOnlyList<string> SpellIds => _spellIds;

    public IReadOnlySet<string> ProficientSkills => _proficientSkills;

    public IReadOnlySet<string> ExpertiseSkills => _expertiseSkills;

    public IReadOnlySet<Ability> SavingThrows => _savingThrows;

    public bool IsAlive => CurrentHp > 0;

    public int ProficiencyBonus => ProficiencyRules.BonusForLevel(Level, _proficiencyBonusByLevel);

    public ProficiencyLevel GetSkillProficiency(string skillId) =>
        _expertiseSkills.Contains(skillId) ? ProficiencyLevel.Expertise
        : _proficientSkills.Contains(skillId) ? ProficiencyLevel.Proficient
        : ProficiencyLevel.None;

    public bool IsProficientInSave(Ability ability) => _savingThrows.Contains(ability);

    /// <summary>Applies damage (never below 0 HP) and returns the HP actually lost.</summary>
    public int TakeDamage(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var before = CurrentHp;
        CurrentHp = Math.Max(0, CurrentHp - amount);
        return before - CurrentHp;
    }

    /// <summary>Restores HP (never above max) and returns the HP actually gained.</summary>
    public int Heal(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var before = CurrentHp;
        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
        return CurrentHp - before;
    }

    public bool TrySpendMana(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (CurrentMana < amount)
        {
            return false;
        }

        CurrentMana -= amount;
        return true;
    }

    public int RestoreMana(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var before = CurrentMana;
        CurrentMana = Math.Min(MaxMana, CurrentMana + amount);
        return CurrentMana - before;
    }

    /// <summary>Spends gold if the player has enough; returns whether it was spent.</summary>
    public bool TrySpendGold(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (Gold < amount)
        {
            return false;
        }

        Gold -= amount;
        return true;
    }

    public void AddGold(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Gold += amount;
    }

    public void AddXp(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Xp += amount;
    }

    /// <summary>
    /// Advances one level: raises maximum HP and mana (and the current values by the same amount)
    /// and learns any spells not already known.
    /// </summary>
    public void LevelUp(int hpGain, int manaGain, IEnumerable<string> newSpells)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(hpGain);
        ArgumentOutOfRangeException.ThrowIfNegative(manaGain);
        ArgumentNullException.ThrowIfNull(newSpells);

        Level++;
        MaxHp += hpGain;
        CurrentHp += hpGain;
        MaxMana += manaGain;
        CurrentMana += manaGain;
        _spellIds.AddRange(newSpells.Where(id => !_spellIds.Contains(id)).Distinct().ToList());
    }

    public int GetEncounterUses(string spellId) => _encounterUses.GetValueOrDefault(spellId);

    public bool TryUseEncounterCharge(string spellId)
    {
        if (GetEncounterUses(spellId) <= 0)
        {
            return false;
        }

        _encounterUses[spellId]--;
        return true;
    }

    /// <summary>Refills per-encounter abilities (called whenever combat starts or ends).</summary>
    public void RechargeEncounterUses(IReadOnlyDictionary<string, int> maxUsesBySpell)
    {
        ArgumentNullException.ThrowIfNull(maxUsesBySpell);
        _encounterUses.Clear();
        foreach (var (spellId, uses) in maxUsesBySpell)
        {
            _encounterUses[spellId] = uses;
        }
    }

    /// <summary>Restores mutable state from a save file.</summary>
    public void RestoreState(int level, int xp, int currentHp, int currentMana, int gold)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(xp);
        ArgumentOutOfRangeException.ThrowIfNegative(currentHp);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(currentHp, MaxHp);
        ArgumentOutOfRangeException.ThrowIfNegative(currentMana);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(currentMana, MaxMana);
        ArgumentOutOfRangeException.ThrowIfNegative(gold);

        Level = level;
        Xp = xp;
        CurrentHp = currentHp;
        CurrentMana = currentMana;
        Gold = gold;
    }
}
