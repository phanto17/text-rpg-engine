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

    private static PlayerCharacter Create(string classId, string backgroundId, string[] skills, string[] expertise)
    {
        var cls = Content.Classes[classId];
        var scores = CharacterFactory.AssignByPriority(Content.Rules.StandardArray, cls);
        return new CharacterFactory(Content).Create("Tamsin", classId, backgroundId, scores, skills, expertise);
    }

    private static IReadOnlyList<GameEvent> Do(GameSession session, string input) => session.Execute(CommandParser.Parse(input));

    private static IReadOnlyList<GameEvent> Say(GameSession session, string startOfChoice) =>
        session.ChooseDialogue(session.DialogueChoices().Single(c => c.Text.StartsWith(startOfChoice, StringComparison.Ordinal)).Index);

    [Fact]
    public void PersuadeWren_ThenSpeakTheRite_EndsInPeace()
    {
        // ambient 0 | persuasion 15 | ambient 0 (ossuary) + 0 (DescribeRoom) | ambient 0 (crypt) | initiative 15, 1, 1 | rite with advantage 15, 10
        var auditor = Create("liminal_auditor", "ash_deserter", ["persuasion", "insight", "stealth", "perception"], ["persuasion", "stealth"]);
        var session = GameSession.NewGame(Content, auditor, new FixedRandomSource(0, 15, 0, 0, 0, 15, 1, 1, 15, 10));
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
        var levelUp = Assert.Single(parley.OfType<LeveledUpEvent>());
        Assert.Equal(2, levelUp.Level);
        Assert.Equal("ledger_strike", Assert.Single(levelUp.NewSpells).Id);
        Assert.Contains("ledger_strike", session.State.Player.SpellIds);
        Assert.Equal(GameMode.Exploration, session.Mode);

        // Claiming the lantern no longer ends the game: it opens the stair to the Under-Barrow.
        var claim = Do(session, "take lantern");

        Assert.Equal(GameMode.Exploration, session.Mode);
        Assert.Contains(claim.OfType<NarrationEvent>(), e => e.Text.Contains("narrow spiral stair cut into the bedrock", StringComparison.Ordinal));
        Assert.Contains(claim.OfType<NarrationEvent>(), e => e.Text.StartsWith("In your hand, the Barrowlight's flame settles into a warm, steady gold", StringComparison.Ordinal));
        Assert.Equal(1, session.State.World.GetCounter("lantern_resonance"));
        Assert.Equal(50, session.State.World.GetQuestStage("barrowlight"));
        Assert.Contains(session.ExplorationActions(), a => a.Label == "Descend the spiral stair into the Under-Barrow");

        var ending = Do(session, "descend stair");

        Assert.Equal(GameMode.Ended, session.Mode);
        Assert.DoesNotContain(ending.OfType<NarrationEvent>(), e => e.Text.Contains("Wren's footsteps", StringComparison.Ordinal));
        Assert.StartsWith("Above you, Ser Halric rests at last on his own sarcophagus", session.Epilogue);
        Assert.Contains("Tamsin", session.Epilogue);
        Assert.IsType<EpilogueEvent>(ending[^1]);
        Assert.Equal(20, session.State.World.GetReputation("ashen_order"));
        Assert.Equal(100, session.State.World.GetQuestStage("barrowlight"));
    }

    [Fact]
    public void ReturnToWrenWithTheLantern_SheJoinsTheDescentOnce()
    {
        // As above without DescribeRoom: ambient 0 | persuasion 15 | ossuary 0 | crypt 0 | initiative 15, 1, 1 | rite 15, 10
        // then west 0 | up 0 | look 0 | down 0 | east 0 (the encounter is resolved, so no initiative)
        var auditor = Create("liminal_auditor", "ash_deserter", ["persuasion", "insight", "stealth", "perception"], ["persuasion", "stealth"]);
        var session = GameSession.NewGame(Content, auditor, new FixedRandomSource(0, 15, 0, 0, 15, 1, 1, 15, 10, 0, 0, 0, 0, 0));
        session.Begin();
        Do(session, "talk to wren");
        Say(session, "I'm not here to rob the dead");
        Say(session, "I'll bring the light back.");
        Do(session, "down");
        Do(session, "press sun");
        Do(session, "press moon");
        Do(session, "press skull");
        Do(session, "east");
        session.CombatAct(new ParleyAction(session.Combat!.ParleyOptions().Single(o => o.Label == "Speak the Rite of Rest").Index));
        Do(session, "take lantern");

        Do(session, "west");
        Do(session, "up");
        var reunion = Do(session, "talk to wren");

        Assert.Contains(reunion.OfType<DialogueLineEvent>(), e => e.Text.Contains("The barrow was never the tomb. It was the seal.", StringComparison.Ordinal));
        Say(session, "Then I'll meet you below, Sister.");
        Assert.True(session.State.World.HasFlag("wren_joins_descent"));
        Assert.Equal(25, session.State.World.GetReputation("ashen_order"));

        // Talking again gets a short farewell; the scene and its reputation gain don't repeat.
        var farewell = Do(session, "talk to wren");
        Assert.Contains(farewell.OfType<DialogueLineEvent>(), e => e.Text.Contains("I'll be right behind you.", StringComparison.Ordinal));
        Assert.Equal(GameMode.Exploration, session.Mode);
        Assert.Equal(25, session.State.World.GetReputation("ashen_order"));

        var look = Assert.IsType<RoomDescribedEvent>(Do(session, "look").Single()).Room;
        Assert.Contains(look.Paragraphs, p => p.StartsWith("Sister Wren stands beneath the gate with her side bound tight", StringComparison.Ordinal));
        Assert.DoesNotContain(look.Paragraphs, p => p.StartsWith("Sister Wren keeps watch", StringComparison.Ordinal));

        Do(session, "down");
        var crypt = Do(session, "east").OfType<RoomDescribedEvent>().Single().Room;
        Assert.Equal(GameMode.Exploration, session.Mode);
        Assert.Contains("Beside the sarcophagus, a spiral stair yawns open in the floor, leading down into the deep earth.", crypt.Paragraphs);
        Assert.DoesNotContain(crypt.Paragraphs, p => p.Contains("iron lantern", StringComparison.Ordinal) || p.Contains("waits on the sarcophagus", StringComparison.Ordinal));
        Assert.Contains("spiral stair", crypt.Features);
        var ending = Do(session, "descend");

        Assert.Contains(ending.OfType<NarrationEvent>(), e => e.Text.Contains("Sister Wren's footsteps follow you down", StringComparison.Ordinal));
        Assert.Equal(GameMode.Ended, session.Mode);
        Assert.StartsWith("Above you, Ser Halric rests at last on his own sarcophagus", session.Epilogue);
        Assert.Equal(100, session.State.World.GetQuestStage("barrowlight"));
    }

    [Fact]
    public void IntimidateWren_ThenFight_EndsAlone()
    {
        // ambient 0 | intimidation 15 | look ambient 1 | ambient 0, 0 | initiative 20, 1, 1 | crit knight 20, 8, 8 | thrall 1 | crit thrall 20, 8, 8
        var oathEater = Create("oath_eater", "moor_bailiff", ["athletics", "intimidation"], []);
        var session = GameSession.NewGame(Content, oathEater, new FixedRandomSource(0, 15, 1, 0, 0, 20, 1, 1, 20, 8, 8, 1, 20, 8, 8));
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
        Assert.Contains(finale, e => e is LeveledUpEvent { Level: 2 });
        Assert.Equal((2, 20), (session.State.Player.Level, session.State.Player.MaxHp));
        Assert.True(session.State.World.HasFlag("knight_destroyed"));

        var claim = Do(session, "take the barrowlight");

        Assert.Contains(claim.OfType<NarrationEvent>(), e => e.Text.StartsWith("In your hand, the Barrowlight's flame settles into a thin, ash-pale glow", StringComparison.Ordinal));
        Assert.Equal(-1, session.State.World.GetCounter("lantern_resonance"));

        Do(session, "descend the stair");

        Assert.StartsWith("Above you, the lichgate stands empty", session.Epilogue);
        Assert.Equal(-15, session.State.World.GetReputation("ashen_order"));
    }

    [Fact]
    public void AshDeserter_SignetWinsKinshipWithWren_AndOpensASignetParley()
    {
        // ambient 0 | persuasion with advantage 15, 3 | ossuary 0 | crypt 0 | initiative 15, 1, 1 | signet religion with advantage 15, 3
        var auditor = Create("liminal_auditor", "ash_deserter", ["persuasion", "insight", "stealth", "perception"], ["persuasion", "stealth"]);
        var session = GameSession.NewGame(Content, auditor, new FixedRandomSource(0, 15, 3, 0, 0, 15, 1, 1, 15, 3));
        session.Begin();

        Do(session, "talk to wren");
        Assert.DoesNotContain(session.DialogueChoices(), c => c.Text.StartsWith("Someone I loved", StringComparison.Ordinal));
        var signet = Say(session, "Show her the scored signet.");
        Assert.Contains(signet.OfType<DialogueLineEvent>(), e => e.Text.Contains("You walked out", StringComparison.Ordinal));
        Assert.True(session.State.World.HasFlag("wren_kinship"));

        var persuade = session.DialogueChoices().Single(c => c.CheckLabel == "Persuasion");
        Assert.Equal(RollMode.Advantage, persuade.Mode);
        Assert.DoesNotContain(session.DialogueChoices(), c => c.Text.StartsWith("Show her the scored signet", StringComparison.Ordinal));
        Say(session, "I'm not here to rob the dead");
        Say(session, "I'll bring the light back.");

        Do(session, "down");
        Do(session, "press sun");
        Do(session, "press moon");
        Do(session, "press skull");
        Do(session, "east");

        var options = session.Combat!.ParleyOptions();
        Assert.Equal(
            new[] { "Tell him his watch is over", "Speak the Rite of Rest", "Show him the scored signet" },
            options.Select(o => o.Label));
        var show = options.Single(o => o.Label == "Show him the scored signet");
        Assert.Equal(("Religion", 11, RollMode.Advantage), (show.CheckLabel, show.Dc, show.Mode));

        var parley = session.CombatAct(new ParleyAction(show.Index));

        Assert.Contains(new CombatEndedEvent(CombatOutcome.Parleyed), parley);
        Assert.True(session.State.World.HasFlag("knight_laid_to_rest"));
    }

    [Fact]
    public void WrongSigil_TriggersNeedleTrapAndResets()
    {
        // ambient 0 | ossuary ambient 0 | DEX save 2 (fails) | 1d4 = 3
        var cantor = Create("vesper_cantor", "fever_dreamer", ["religion", "arcana"], []);
        var session = GameSession.NewGame(Content, cantor, new FixedRandomSource(0, 0, 2, 3));
        session.Begin();
        Do(session, "down");

        var trap = Do(session, "press moon");

        Assert.False(trap.OfType<CheckRolledEvent>().Single().Result.Success);
        Assert.Equal(3, trap.OfType<DamageTakenEvent>().Single().Amount);
        Assert.Equal(0, session.State.World.GetCounter("sigil_seq"));
        Assert.Equal(GameMode.Exploration, session.Mode);
    }
}
