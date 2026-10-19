using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Tests.Content;

/// <summary>Validates the shipped game content in <c>Data/</c> (copied next to the test assembly).</summary>
public sealed class RealDataTests
{
    public static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "Data");

    private static readonly Lazy<GameContent> Content = new(() => ContentLoader.LoadFromDirectory(DataDirectory));

    private static readonly string[] PrologueRooms = ["lichgate", "ossuary_hall", "inner_crypt"];

    private static readonly string[] ActOneRooms = ["weeping_stair", "nocturne_square", "echo_chapel", "drowned_tollbridge"];

    [Fact]
    public void ShippedContent_LoadsWithoutErrors()
    {
        var content = Content.Value;

        Assert.Equal("Barrowlight", content.Manifest.Title);
        Assert.Equal("lichgate", content.Manifest.StartRoom);
    }

    // Later acts add rooms, factions, and quests, so these check that the shipped story is present
    // rather than that nothing else is.
    [Fact]
    public void ShippedContent_HasThePrologueAndActOne()
    {
        var content = Content.Value;

        Assert.Equal(new[] { "oath_eater", "liminal_auditor", "vesper_cantor", "remnant_vessel" }, content.ClassList.Select(c => c.Id));
        Assert.Equal(new[] { "bereaved_kin", "ash_deserter", "moor_bailiff", "fever_dreamer" }, content.BackgroundList.Select(b => b.Id));
        Assert.All(content.BackgroundList, b => Assert.Equal(ItemType.Quest, content.Items[b.KeepsakeItem].Type));
        Assert.All(content.ClassList.SelectMany(c => c.Spells), id => Assert.True(content.Spells.ContainsKey(id)));
        Assert.Equal(18, content.Skills.Count);
        Assert.All(PrologueRooms.Concat(ActOneRooms), id => Assert.Contains(id, content.Rooms.Keys));
        Assert.All(new[] { "ashen_order", "hollow_choir", "lantern_bearers" }, id => Assert.Contains(id, content.Factions.Keys));
        Assert.All(new[] { "barrowlight", "nocturne_beacon" }, id => Assert.Contains(id, content.Quests.Keys));
        Assert.All(new[] { "wren", "orin", "maeve", "peddler", "wren_nocturne", "miriel", "garin" }, id => Assert.Contains(id, content.Dialogues.Keys));
        Assert.Equal("inner_crypt", content.EncounterRooms["barrow_heart"]);
        Assert.Equal("echo_chapel", content.EncounterRooms["echo_chapel_choir"]);
        Assert.Equal("drowned_tollbridge", content.EncounterRooms["drowned_tollbridge_watch"]);
        Assert.Equal(6, content.Encounters["barrow_heart"].Parley!.Options.Count);
        Assert.All(content.Encounters.Values, e => Assert.NotEmpty(e.Parley!.Options));
    }

    [Fact]
    public void EveryRoom_IsReachableFromTheStartRoom()
    {
        var content = Content.Value;
        var reached = new HashSet<string> { content.Manifest.StartRoom };
        var frontier = new Queue<string>(reached);

        while (frontier.TryDequeue(out var id))
        {
            foreach (var next in Destinations(content, content.Rooms[id]).Where(reached.Add))
            {
                frontier.Enqueue(next);
            }
        }

        Assert.Equal(content.Rooms.Keys.Order(StringComparer.Ordinal), reached.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void OnlyCrossingTheTollbridge_EndsTheGame()
    {
        var content = Content.Value;

        var endings = content.RoomList.Where(room => RoomEffects(content, room).OfType<EndGameEffect>().Any()).Select(room => room.Id);

        Assert.Equal(new[] { "drowned_tollbridge" }, endings);
        var cross = content.Rooms["drowned_tollbridge"].Features.Single(f => f.Id == "far_arch").Interactions.Single();
        Assert.Equal("cross", cross.Verb);
        Assert.Contains(cross.Conditions, c => c is FlagCondition { Flag: "tollbridge_resolved" });
        Assert.Empty(content.Manifest.Epilogues[^1].Conditions);
    }

    [Theory]
    [InlineData("echo_chapel", "challenged_miriel", "shone_chapel_echo")]
    [InlineData("drowned_tollbridge", "challenged_garin", "shone_bridge_echo")]
    public void ActOneFights_WaitForTheApproach_SoTheEchoCanBeShoneFirst(string roomId, string challengeFlag, string echoFlag)
    {
        var content = Content.Value;
        var room = content.Rooms[roomId];
        var encounter = room.Encounter!;

        // Entering the room never starts the fight on its own: the approach sets the flag and starts it.
        Assert.Contains(encounter.Conditions, c => c is FlagCondition f && f.Flag == challengeFlag);
        var approach = room.Features.SelectMany(f => f.Interactions).Single(i => i.Verb == "approach");
        Assert.Contains(approach.Effects, e => e is SetFlagEffect s && s.Flag == challengeFlag);
        Assert.Contains(approach.Effects, e => e is StartCombatEffect s && s.Encounter == encounter.Id);

        // The echo needs only the lantern, and its flag gives advantage on the first parley option.
        var shine = room.Features.SelectMany(f => f.Interactions).Single(i => i.Verb == "shine");
        Assert.Contains(shine.Effects, e => e is SetFlagEffect s && s.Flag == echoFlag);
        var advantage = Assert.IsType<AnyCondition>(Assert.Single(encounter.Parley!.Options[0].Check.AdvantageIf));
        Assert.Contains(advantage.Conditions, c => c is FlagCondition f && f.Flag == echoFlag);
    }

    [Fact]
    public void EveryCalling_LearnsOneAbilityAtLevelTwo()
    {
        var content = Content.Value;

        Assert.Equal(new[] { 0, 100, 300, 900, 2700 }, content.Rules.XpThresholdsByLevel);
        Assert.All(content.ClassList, cls => Assert.Single(Assert.Single(cls.SpellsByLevel, grant => grant.Level == 2).Spells));

        // Both ways through the Barrow Heart (100 XP for a parley, 125 for a victory) reach level 2.
        Assert.True(content.Monsters["gravebound_knight"].Xp >= content.Rules.XpThresholdsByLevel[1]);
    }

    [Fact]
    public void WrenAndHalric_AcknowledgeEveryCallingAndBurden()
    {
        var content = Content.Value;
        var hub = content.Dialogues["wren"].Nodes.Single(n => n.Id == "hub");
        var gates = hub.Choices.SelectMany(c => c.Conditions).ToList();

        Assert.All(content.ClassList, cls => Assert.Single(gates, g => g is ClassCondition c && c.ClassId == cls.Id));
        Assert.All(content.BackgroundList, bg => Assert.Single(gates, g => g is BackgroundCondition b && b.BackgroundId == bg.Id));
        Assert.Equal("I'll leave you to your watch.", hub.Choices[^1].Text);

        // The Rite stays the second option so it keeps its menu number; the new options only follow it.
        var parley = content.Encounters["barrow_heart"].Parley!.Options;
        Assert.Equal("Speak the Rite of Rest", parley[1].Label);
        Assert.All(parley.Skip(2), option => Assert.Single(option.Conditions, c => c is ClassCondition or BackgroundCondition));
    }

    [Fact]
    public void WrenDialogue_HasSkillChecksWithPermanentLocks()
    {
        var hub = Content.Value.Dialogues["wren"].Nodes.Single(n => n.Id == "hub");

        var checks = hub.Choices.Where(c => c.Check is not null).ToList();

        Assert.Equal(new[] { "insight", "persuasion", "intimidation" }, checks.Select(c => c.Check!.Skill));
        Assert.All(checks, check => Assert.False(check.Retry));
        Assert.Equal(new[] { 10, 12, 12 }, checks.Select(c => c.Check!.Dc));
    }

    [Fact]
    public void WrensTrust_GivesHolyWater_ThatExploitsTheKnightsVulnerability()
    {
        var content = Content.Value;
        var trusts = content.Dialogues["wren"].Nodes.Single(n => n.Id == "trusts");

        Assert.Contains(trusts.Effects, e => e is GiveItemEffect { Item: "holy_water" });
        var use = content.Items["holy_water"].Use!;
        Assert.Equal(SpellTarget.Enemy, use.Target);
        var damageType = Assert.IsType<AutoHitResolutionDef>(use.Resolution).DamageType;
        Assert.Contains(damageType, content.Monsters["gravebound_knight"].Vulnerabilities);
    }

    private static IEnumerable<string> Destinations(GameContent content, RoomDef room) =>
        room.Exits.Select(e => e.Target).Concat(RoomEffects(content, room).OfType<MoveToEffect>().Select(e => e.Room));

    // Every effect that can run while the player is in the room: features, entry hooks, the
    // encounter, and the conversations with the room's NPCs.
    private static IEnumerable<EffectDef> RoomEffects(GameContent content, RoomDef room)
    {
        var interactions = room.Features.SelectMany(f => f.Interactions);
        var nodes = room.Npcs.SelectMany(n => content.Dialogues[n.Dialogue].Nodes);
        IEnumerable<EffectDef> effects =
        [
            .. room.OnFirstEnter,
            .. room.OnEnter,
            .. interactions.SelectMany(i => i.Effects.Concat(i.Success?.Effects ?? []).Concat(i.Failure?.Effects ?? [])),
            .. room.Encounter is { } e ? e.OnVictory.Concat(e.OnResolved).Concat(e.Parley?.OnSuccess ?? []).Concat(e.Parley?.OnFailure ?? []) : [],
            .. nodes.SelectMany(n => n.Effects.Concat(n.Choices.SelectMany(c => c.Effects))),
        ];
        return Flatten(effects);
    }

    private static IEnumerable<EffectDef> Flatten(IEnumerable<EffectDef> effects) =>
        effects.SelectMany(effect => effect switch
        {
            IfEffect e => Flatten(e.Then.Concat(e.Else)).Prepend(effect),
            SavingThrowEffect e => Flatten(e.OnSuccess.Concat(e.OnFail)).Prepend(effect),
            SkillCheckEffect e => Flatten(e.OnSuccess.Concat(e.OnFail)).Prepend(effect),
            _ => [effect],
        });

    [Fact]
    public void SigilDoor_IsAThreeStepSequencePuzzle()
    {
        var door = Content.Value.Rooms["ossuary_hall"].Features.Single(f => f.Id == "bronze_door");

        var presses = door.Interactions.Where(i => i.Verb == "press").Select(i => i.Target).Distinct();

        Assert.Equal(new[] { "sun", "moon", "skull" }, presses);
        Assert.Contains(door.Interactions, i => i.Effects.OfType<SetFlagEffect>().Any(e => e.Flag == "sigil_door_open"));
    }
}
