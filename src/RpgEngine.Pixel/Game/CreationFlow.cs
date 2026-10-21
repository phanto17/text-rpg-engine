using RpgEngine.Core.Characters;
using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Engine;

namespace RpgEngine.Pixel.Game;

/// <summary>
/// Character creation as a headless state machine, one <see cref="CreationStep"/> at a time, in the
/// same order as the terminal: name, calling, burden, ability scores, skills, expertise, review.
/// </summary>
public sealed class CreationFlow
{
    private readonly GameContent _content;
    private readonly CharacterFactory _factory;
    private readonly DiceRoller _dice;
    private readonly List<string> _skills = [];
    private readonly List<string> _expertise = [];
    private readonly List<int> _remaining = [];
    private readonly Dictionary<Ability, int> _assigned = [];

    public CreationFlow(GameContent content, DiceRoller dice)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(dice);
        _content = content;
        _factory = new CharacterFactory(content);
        _dice = dice;
    }

    public CreationStep Step { get; private set; } = CreationStep.Name;

    public string Name { get; private set; } = string.Empty;

    public ClassDef? Class { get; private set; }

    public BackgroundDef? Background { get; private set; }

    /// <summary>The six values being assigned (the standard array or the rolled totals).</summary>
    public IReadOnlyList<int> Values { get; private set; } = [];

    public AbilityScores? Scores { get; private set; }

    public IReadOnlyList<string> Skills => _skills;

    public IReadOnlyList<string> Expertise => _expertise;

    public IReadOnlyList<int> StandardArray => _factory.StandardArray;

    /// <summary>Values still to be placed during manual assignment, highest first.</summary>
    public IReadOnlyList<int> RemainingValues => _remaining;

    /// <summary>The ability the next manual pick fills, in STR..CHA order.</summary>
    public Ability NextAbility => AbilityExtensions.All.First(ability => !_assigned.ContainsKey(ability));

    public IReadOnlyList<string> SkillOptions =>
        Class is null || Background is null ? [] : Class.SkillChoices.From.Where(id => id != Background.BonusSkill).ToList();

    public IReadOnlyList<string> ExpertiseOptions =>
        Class is null ? [] : Class.SkillChoices.From.Where(_skills.Contains).ToList();

    public bool SkillsComplete => Class is not null && _skills.Count == Class.SkillChoices.Count;

    public bool ExpertiseComplete => Class is not null && _expertise.Count == Class.ExpertiseCount;

    /// <summary>Accepts a non-empty name of at most <see cref="CharacterFactory.MaxNameLength"/> characters.</summary>
    public bool SetName(string? name)
    {
        Require(CreationStep.Name);
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > CharacterFactory.MaxNameLength)
        {
            return false;
        }

        Name = trimmed;
        Step = CreationStep.Calling;
        return true;
    }

    public void ChooseClass(ClassDef cls)
    {
        Require(CreationStep.Calling);
        ArgumentNullException.ThrowIfNull(cls);
        Class = cls;
        Step = CreationStep.Burden;
    }

    public void ChooseBackground(BackgroundDef background)
    {
        Require(CreationStep.Burden);
        ArgumentNullException.ThrowIfNull(background);
        Background = background;
        Step = CreationStep.ScoreMethod;
    }

    public void UseStandardArray()
    {
        Require(CreationStep.ScoreMethod);
        Suggest(_factory.StandardArray);
    }

    /// <summary>Rolls 4d6 drop lowest six times; the rolls are returned so the log can show them.</summary>
    public IReadOnlyList<RollResult> RollScores()
    {
        Require(CreationStep.ScoreMethod);
        var rolls = _factory.RollAbilityScores(_dice);
        Suggest(rolls.Select(r => r.Total).ToList());
        return rolls;
    }

    public void KeepSuggestedScores()
    {
        Require(CreationStep.ScoreReview);
        Step = CreationStep.Skills;
    }

    public void AssignManually()
    {
        Require(CreationStep.ScoreReview);
        _remaining.Clear();
        _remaining.AddRange(Values.OrderByDescending(v => v));
        _assigned.Clear();
        Step = CreationStep.AssignScores;
    }

    /// <summary>Places the value at <paramref name="remainingIndex"/> into <see cref="NextAbility"/>.</summary>
    public void AssignNext(int remainingIndex)
    {
        Require(CreationStep.AssignScores);
        ArgumentOutOfRangeException.ThrowIfNegative(remainingIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(remainingIndex, _remaining.Count);
        _assigned[NextAbility] = _remaining[remainingIndex];
        _remaining.RemoveAt(remainingIndex);
        if (_remaining.Count == 0)
        {
            Scores = AbilityScores.FromMap(_assigned);
            Step = CreationStep.Skills;
        }
    }

    /// <summary>Adds or removes a class skill pick; a pick beyond the class's count is ignored.</summary>
    public void ToggleSkill(string skillId)
    {
        Require(CreationStep.Skills);
        if (!_skills.Remove(skillId) && SkillOptions.Contains(skillId) && !SkillsComplete)
        {
            _skills.Add(skillId);
        }
    }

    public void ConfirmSkills()
    {
        Require(CreationStep.Skills);
        if (!SkillsComplete)
        {
            throw new InvalidOperationException("Choose every skill first.");
        }

        _expertise.Clear();
        Step = Class!.ExpertiseCount == 0 ? CreationStep.Review : CreationStep.Expertise;
    }

    public void ToggleExpertise(string skillId)
    {
        Require(CreationStep.Expertise);
        if (!_expertise.Remove(skillId) && _skills.Contains(skillId) && !ExpertiseComplete)
        {
            _expertise.Add(skillId);
        }
    }

    public void ConfirmExpertise()
    {
        Require(CreationStep.Expertise);
        if (!ExpertiseComplete)
        {
            throw new InvalidOperationException("Choose every expertise first.");
        }

        Step = CreationStep.Review;
    }

    public PlayerCharacter Build()
    {
        Require(CreationStep.Review);
        return _factory.Create(Name, Class!.Id, Background!.Id, Scores!, _skills, _expertise);
    }

    public void Restart()
    {
        Step = CreationStep.Name;
        Name = string.Empty;
        Class = null;
        Background = null;
        Values = [];
        Scores = null;
        _skills.Clear();
        _expertise.Clear();
        _remaining.Clear();
        _assigned.Clear();
    }

    private void Suggest(IReadOnlyList<int> values)
    {
        Values = values;
        Scores = CharacterFactory.AssignByPriority(values, Class!);
        Step = CreationStep.ScoreReview;
    }

    private void Require(CreationStep step)
    {
        if (Step != step)
        {
            throw new InvalidOperationException($"This needs the {step} step, but creation is at {Step}.");
        }
    }
}
