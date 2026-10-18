using RpgEngine.Core.Characters;
using RpgEngine.Core.Combat;
using RpgEngine.Core.Commands;
using RpgEngine.Core.Content;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Engine;
using RpgEngine.Core.Events;
using RpgEngine.Tests.Content;

namespace RpgEngine.Tests.Playthrough;

/// <summary>
/// Plays complete storylines through the shipped Barrowlight content with scripted dice, proving
/// that choices made with Wren change the world, the final encounter, and the epilogue.
/// Every room has ambient lines, so each room description consumes one die (the ambient pick).
/// </summary>
public sealed class BarrowlightScenarioTests
{
    private static readonly GameContent Content = ContentLoader.LoadFromDirectory(RealDataTests.DataDirectory);

    private static PlayerCharacter Create(string classId, string[] skills, string[] expertise)
    {
        var cls = Content.Classes[classId];
        var scores = CharacterFactory.AssignByPriority(Content.Rules.StandardArray, cls);
        return new CharacterFactory(Content).Create("Tamsin", classId, scores, skills, expertise);
    }

    private static IReadOnlyList<GameEvent> Do(GameSession session, string input) => session.Execute(CommandParser.Parse(input));

    private static IReadOnlyList<GameEvent> Say(GameSession session, string startOfChoice) =>
        session.ChooseDialogue(session.DialogueChoices().Single(c => c.Text.StartsWith(startOfChoice, StringComparison.Ordinal)).Index);

    [Fact]
    public void PersuadeWren_ThenSpeakTheRite_EndsInPeace()
    {
        // ambient 0 | persuasion 15 | ambient 0 (ossuary) + 0 (DescribeRoom) | ambient 0 (crypt) | initiative 15, 1, 1 | rite with advantage 15, 10
        var rogue = Create("rogue", ["persuasion", "insight", "stealth", "perception"], ["persuasion", "stealth"]);
        var session = GameSession.NewGame(Content, rogue, new FixedRandomSource(0, 15, 0, 0, 0, 15, 1, 1, 15, 10));
        session.Begin();

        Do(session, "talk to wren");
        var choices = session.DialogueChoices();
        var persuade = choices.Single(c => c.CheckLabel == "Persuasion");
        Assert.Equal(12, persuade.Dc);

        var reply = Say(session, "I'm not here to rob the dead");
        Assert.True(reply.OfType<CheckRolledEvent>().Single().Result.Success);
        Assert.Contains(reply.OfType<ItemGainedEvent>(), e => e.Item.Id == "holy_water");
        Say(session, "I'll bring the light back.");
        Assert.True(session.State.World.HasFlag("wren_trusts_player"));

        Do(session, "down");
        Assert.Contains("sun, then the moon, then the skull", session.DescribeRoom().Paragraphs[^1]);
        Do(session, "press sun");
        Do(session, "press moon");
        Do(session, "press skull");
        Assert.True(session.State.World.HasFlag("sigil_door_open"));

        Do(session, "east");
        Assert.Equal(GameMode.Combat, session.Mode);
        var rite = session.Combat!.ParleyOptions().Single(o => o.Label == "Speak the Rite of Rest");
        Assert.Equal(RollMode.Advantage, rite.Mode);

        var parley = session.CombatAct(new ParleyAction(rite.Index));

        Assert.Contains(new CombatEndedEvent(CombatOutcome.Parleyed), parley);
        Assert.True(session.State.World.HasFlag("knight_laid_to_rest"));
        Assert.Equal(GameMode.Exploration, session.Mode);

        var ending = Do(session, "take lantern");

        Assert.Equal(GameMode.Ended, session.Mode);
        Assert.StartsWith("You climb out of the barrow with the Barrowlight burning in your hands, and Sister Wren is waiting", session.Epilogue);
        Assert.Contains("Tamsin", session.Epilogue);
        Assert.IsType<EpilogueEvent>(ending[^1]);
        Assert.Equal(20, session.State.World.GetReputation("ashen_order"));
        Assert.Equal(100, session.State.World.GetQuestStage("barrowlight"));
    }

    [Fact]
    public void IntimidateWren_ThenFight_EndsAlone()
    {
        // ambient 0 | intimidation 15 | look ambient 1 | ambient 0, 0 | initiative 20, 1, 1 | crit knight 20, 8, 8 | thrall 1 | crit thrall 20, 8, 8
        var fighter = Create("fighter", ["athletics", "intimidation"], []);
        var session = GameSession.NewGame(Content, fighter, new FixedRandomSource(0, 15, 1, 0, 0, 20, 1, 1, 20, 8, 8, 1, 20, 8, 8));
        session.Begin();

        Do(session, "talk to wren");
        Say(session, "Step aside");
        Assert.Equal(GameMode.Exploration, session.Mode);
        Assert.True(session.State.World.HasFlag("wren_fled"));

        var look = Assert.IsType<RoomDescribedEvent>(Do(session, "look").Single()).Room;
        Assert.Empty(look.Npcs);
        Assert.Contains(look.Paragraphs, p => p.StartsWith("Wren's bedroll lies abandoned", StringComparison.Ordinal));

        Do(session, "down");
        Do(session, "press sun");
        Do(session, "press moon");
        Do(session, "press skull");
        Do(session, "east");

        session.CombatAct(new AttackAction("gravebound_knight#1"));
        session.CombatEnemyTurn();
        var finale = session.CombatAct(new AttackAction("barrow_thrall#1"));

        Assert.Contains(new CombatEndedEvent(CombatOutcome.Victory), finale);
        Assert.Contains(finale, e => e is XpGainedEvent { Amount: 125 });
        Assert.True(session.State.World.HasFlag("knight_destroyed"));

        Do(session, "take the barrowlight");

        Assert.StartsWith("You carry the Barrowlight out over the knight's shattered bones", session.Epilogue);
        Assert.Equal(-15, session.State.World.GetReputation("ashen_order"));
    }

    [Fact]
    public void WrongSigil_TriggersNeedleTrapAndResets()
    {
        // ambient 0 | ossuary ambient 0 | DEX save 2 (fails) | 1d4 = 3
        var wizard = Create("wizard", ["religion", "arcana"], []);
        var session = GameSession.NewGame(Content, wizard, new FixedRandomSource(0, 0, 2, 3));
        session.Begin();
        Do(session, "down");

        var trap = Do(session, "press moon");

        Assert.False(trap.OfType<CheckRolledEvent>().Single().Result.Success);
        Assert.Equal(3, trap.OfType<DamageTakenEvent>().Single().Amount);
        Assert.Equal(0, session.State.World.GetCounter("sigil_seq"));
        Assert.Equal(GameMode.Exploration, session.Mode);
    }
}
