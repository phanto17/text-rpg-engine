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
/// Plays the Barrowlight boss fight (Ser Halric and a barrow thrall) over and over, driving
/// <see cref="CombatEngine"/> directly with <see cref="BalancePolicy"/>. Fight <c>n</c> uses
/// <c>new Pcg32RandomSource(n)</c>, so every measured number is exact and repeatable.
/// </summary>
internal sealed class BarrowHeartSimulator
{
    public const string EncounterId = "barrow_heart";

    // A fight between two sides that always attack ends long before this; hitting it means a stalemate bug.
    private const int MaxRounds = 100;

    private readonly GameContent _content;
    private readonly BalancePolicy _policy;

    public BarrowHeartSimulator(GameContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        _content = content;
        _policy = new BalancePolicy(content);
    }

    /// <summary>Runs fights with seeds 1..<paramref name="fights"/> for a fresh level-1 character of the class.</summary>
    public BalanceRow Run(string classId, bool withFlask, int fights)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fights);
        var wins = 0;
        var rounds = 0;
        var hpLeft = 0;
        var maxHp = 0;

        for (var seed = 1; seed <= fights; seed++)
        {
            var (won, fightRounds, player) = Fight(classId, withFlask, (ulong)seed);
            rounds += fightRounds;
            maxHp = player.MaxHp;
            if (won)
            {
                wins++;
                hpLeft += player.CurrentHp;
            }
        }

        return new BalanceRow(classId, withFlask, maxHp, fights, wins, rounds, hpLeft);
    }

    private (bool Won, int Rounds, PlayerCharacter Player) Fight(string classId, bool withFlask, ulong seed)
    {
        var dice = new DiceRoller(new Pcg32RandomSource(seed));
        var checks = new CheckRunner(_content, new CheckResolver(dice, _content.Rules.NaturalCritsOnChecks));
        var text = new TextFormatter(_content);
        var effects = new EffectApplier(_content, dice, checks, text);

        var player = CreatePlayer(classId);
        if (withFlask)
        {
            player.Inventory.Add(BalancePolicy.Flask);
        }

        var state = new GameState(player, WorldState.CreateNew(_content), _content.EncounterRooms[EncounterId]);
        var session = new CombatEngine(_content, dice, checks, effects, text).Begin(EncounterId, state, new TurnLog());

        while (!session.IsOver)
        {
            if (session.Round > MaxRounds)
            {
                throw new InvalidOperationException($"Seed {seed}: the {classId} fight did not end within {MaxRounds} rounds.");
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

        return (session.Outcome == CombatOutcome.Victory, session.Round, player);
    }

    // Standard array by class priority, the first background, and the first listed skill choices it
    // leaves (and the first of those for expertise). Backgrounds only add a skill, gold, and a
    // keepsake, and skills never come into play because the policy neither parleys nor flees.
    private PlayerCharacter CreatePlayer(string classId)
    {
        var cls = _content.Classes[classId];
        var background = _content.BackgroundList[0];
        var scores = CharacterFactory.AssignByPriority(_content.Rules.StandardArray, cls);
        var skills = cls.SkillChoices.From.Where(skill => skill != background.BonusSkill).Take(cls.SkillChoices.Count).ToList();
        var expertise = skills.Take(cls.ExpertiseCount).ToList();
        return new CharacterFactory(_content).Create("Tester", classId, background.Id, scores, skills, expertise);
    }
}
