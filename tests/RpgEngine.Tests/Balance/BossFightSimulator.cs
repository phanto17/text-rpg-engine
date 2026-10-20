using RpgEngine.Core.Characters;
using RpgEngine.Core.Checks;
using RpgEngine.Core.Combat;
using RpgEngine.Core.Content;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Engine;
using RpgEngine.Core.Events;
using RpgEngine.Core.World;

namespace RpgEngine.Tests.Balance;

/// <summary>
/// Plays one boss encounter (for example Ser Halric and a barrow thrall) over and over, driving
/// <see cref="CombatEngine"/> directly with <see cref="BalancePolicy"/>. Fight <c>n</c> uses
/// <c>new Pcg32RandomSource(n)</c>, so every measured number is exact and repeatable.
/// </summary>
internal sealed class BossFightSimulator
{
    // A fight between two sides that always attack ends long before this; hitting it means a stalemate bug.
    private const int MaxRounds = 100;

    private readonly GameContent _content;
    private readonly string _encounterId;
    private readonly BalancePolicy _policy;

    public BossFightSimulator(GameContent content, string encounterId, string bossId)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(encounterId);
        _content = content;
        _encounterId = encounterId;
        _policy = new BalancePolicy(content, bossId);
    }

    /// <summary>
    /// Runs fights with seeds 1..<paramref name="fights"/> for a fresh character of the class at
    /// <paramref name="level"/>, carrying the class's starting kit and one of each of
    /// <paramref name="extraItems"/> (for example the Flask of Holy Water).
    /// </summary>
    public BalanceRow Run(string classId, int level, int fights, params IReadOnlyList<string> extraItems)
    {
        ArgumentNullException.ThrowIfNull(extraItems);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fights);
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        var wins = 0;
        var rounds = 0;
        var hpLeft = 0;
        var maxHp = 0;

        for (var seed = 1; seed <= fights; seed++)
        {
            var (won, fightRounds, startMaxHp, hpAtEnd) = Fight(classId, level, extraItems, (ulong)seed);
            rounds += fightRounds;
            maxHp = startMaxHp;
            if (won)
            {
                wins++;
                hpLeft += hpAtEnd;
            }
        }

        return new BalanceRow(classId, level, extraItems, maxHp, fights, wins, rounds, hpLeft);
    }

    // Returns the HP left as the fight ended: the victory XP may level the character up, and the
    // hit points that level adds are not something the fight left behind.
    private (bool Won, int Rounds, int StartMaxHp, int HpLeft) Fight(string classId, int level, IReadOnlyList<string> extraItems, ulong seed)
    {
        var dice = new DiceRoller(new Pcg32RandomSource(seed));
        var checks = new CheckRunner(_content, new CheckResolver(dice, _content.Rules.NaturalCritsOnChecks));
        var text = new TextFormatter(_content);
        var effects = new EffectApplier(_content, dice, checks, text);

        var player = CreatePlayer(classId, level);
        var startMaxHp = player.MaxHp;
        foreach (var item in extraItems)
        {
            player.Inventory.Add(item);
        }

        var state = new GameState(player, WorldState.CreateNew(_content), _content.EncounterRooms[_encounterId]);
        var session = new CombatEngine(_content, dice, checks, effects, text).Begin(_encounterId, state, new TurnLog());

        while (!session.IsOver)
        {
            if (session.Round > MaxRounds)
            {
                throw new InvalidOperationException($"Seed {seed}: the {classId} fight against {_encounterId} did not end within {MaxRounds} rounds.");
            }

            var log = new TurnLog();
            if (session.IsPlayerTurn)
            {
                session.Act(_policy.Choose(session), log);
            }
            else
            {
                session.RunEnemyTurn(log);
            }
        }

        return (session.Outcome == CombatOutcome.Victory, session.Round, startMaxHp, player.CurrentHp - (player.MaxHp - startMaxHp));
    }

    // Standard array by class priority, the first background, and the first listed skill choices it
    // leaves (and the first of those for expertise). Backgrounds only add a skill, gold, and a
    // keepsake, and skills never come into play because the policy neither parleys nor flees.
    // Higher levels come from the XP threshold, so HP, mana, and spells grow exactly as in play.
    private PlayerCharacter CreatePlayer(string classId, int level)
    {
        var cls = _content.Classes[classId];
        var background = _content.BackgroundList[0];
        var scores = CharacterFactory.AssignByPriority(_content.Rules.StandardArray, cls);
        var skills = cls.SkillChoices.From.Where(skill => skill != background.BonusSkill).Take(cls.SkillChoices.Count).ToList();
        var expertise = skills.Take(cls.ExpertiseCount).ToList();
        var player = new CharacterFactory(_content).Create("Tester", classId, background.Id, scores, skills, expertise);
        if (level > 1)
        {
            Progression.GrantXp(player, _content, _content.Rules.XpThresholdsByLevel[level - 1], new TurnLog());
        }

        if (player.Level != level)
        {
            throw new InvalidOperationException($"Level {level} is beyond the XP thresholds.");
        }

        return player;
    }
}
