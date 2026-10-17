using RpgEngine.Core.Characters;
using RpgEngine.Core.Combat;
using RpgEngine.Core.Content;
using RpgEngine.Core.Events;

namespace RpgEngine.Tests.TestSupport;

/// <summary>Runs a combat over <see cref="CombatFixture"/> with scripted dice.</summary>
internal sealed class CombatHarness
{
    public CombatHarness(PlayerCharacter? hero = null, GameContent? content = null, params int[] dice)
    {
        World = new TestWorld(content ?? CombatFixture.Load(), hero ?? CombatFixture.Hero(), dice);
        Engine = new CombatEngine(World.Content, World.Dice, World.Checks, World.Effects, World.Text);
    }

    public TestWorld World { get; }

    public CombatEngine Engine { get; }

    public CombatSession Session { get; private set; } = null!;

    public TurnLog Begin(string encounterId)
    {
        var log = new TurnLog();
        Session = Engine.Begin(encounterId, World.State, log);
        return log;
    }

    public TurnLog Act(PlayerAction action)
    {
        var log = new TurnLog();
        Session.Act(action, log);
        return log;
    }

    public TurnLog EnemyTurn()
    {
        var log = new TurnLog();
        Session.RunEnemyTurn(log);
        return log;
    }

    public string EnemyId(int index = 0) => Session.ActiveEnemies[index].Id;
}
