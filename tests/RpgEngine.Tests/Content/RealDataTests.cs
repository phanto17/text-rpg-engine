using RpgEngine.Core.Characters;
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

    private static readonly string[] ActTwoRooms =
        ["warren_crossroads", "whispering_stacks", "tallow_foundry", "vow_sanctum", "scoured_cloister", "lye_cistern", "reliquary_bastion"];

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
    public void ShippedContent_HasThePrologueAndActsOneAndTwo()
    {
        var content = Content.Value;

        Assert.Equal(new[] { "oath_eater", "liminal_auditor", "vesper_cantor", "remnant_vessel" }, content.ClassList.Select(c => c.Id));
        Assert.Equal(new[] { "bereaved_kin", "ash_deserter", "moor_bailiff", "fever_dreamer" }, content.BackgroundList.Select(b => b.Id));
        Assert.All(content.BackgroundList, b => Assert.Equal(ItemType.Quest, content.Items[b.KeepsakeItem].Type));
        Assert.All(content.ClassList.SelectMany(c => c.Spells), id => Assert.True(content.Spells.ContainsKey(id)));
        Assert.Equal(18, content.Skills.Count);
        Assert.All(PrologueRooms.Concat(ActOneRooms).Concat(ActTwoRooms), id => Assert.Contains(id, content.Rooms.Keys));
        Assert.All(new[] { "ashen_order", "hollow_choir", "lantern_bearers" }, id => Assert.Contains(id, content.Factions.Keys));
        Assert.All(new[] { "barrowlight", "nocturne_beacon", "twin_seals" }, id => Assert.Contains(id, content.Quests.Keys));
        Assert.All(
            new[] { "wren", "orin", "maeve", "peddler", "wren_nocturne", "miriel", "garin", "caelan", "venn", "vaelen", "silas" },
            id => Assert.Contains(id, content.Dialogues.Keys));
        Assert.All(new[] { "prelate_vaelen", "tethered_chorister", "commander_silas", "scoured_halberdier" }, id => Assert.Contains(id, content.Monsters.Keys));
        Assert.All(
            new[] { "choir_silks", "lye_salve", "silver_tuning_blade", "scoured_halberd", "moon_triad_seal", "sun_triad_seal" },
            id => Assert.Contains(id, content.Items.Keys));
        Assert.All(new[] { "moon_triad_seal", "sun_triad_seal" }, id => Assert.Equal(ItemType.Quest, content.Items[id].Type));
        Assert.Equal("inner_crypt", content.EncounterRooms["barrow_heart"]);
        Assert.Equal("echo_chapel", content.EncounterRooms["echo_chapel_choir"]);
        Assert.Equal("drowned_tollbridge", content.EncounterRooms["drowned_tollbridge_watch"]);
        Assert.Equal("vow_sanctum", content.EncounterRooms["archive_vaelen_boss"]);
        Assert.Equal("reliquary_bastion", content.EncounterRooms["reliquary_silas_boss"]);
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
    public void OnlyUnsealingTheTwinSealGate_EndsTheGame()
    {
        var content = Content.Value;

        var endings = content.RoomList.Where(room => RoomEffects(content, room).OfType<EndGameEffect>().Any()).Select(room => room.Id);

        Assert.Equal(new[] { "warren_crossroads" }, endings);
        var unseal = content.Rooms["warren_crossroads"].Features.Single(f => f.Id == "twin_seal_gate").Interactions.Single();
        Assert.Equal("unseal", unseal.Verb);
        Assert.Contains(unseal.Conditions, c => c is HasItemCondition { Item: "moon_triad_seal" });
        Assert.Contains(unseal.Conditions, c => c is HasItemCondition { Item: "sun_triad_seal" });
        Assert.Contains(unseal.Effects, e => e is SetQuestStageEffect { Quest: "twin_seals", Stage: 100 });
        Assert.Empty(content.Manifest.Epilogues[^1].Conditions);
    }

    [Fact]
    public void CrossingTheTollbridge_LeadsIntoActTwo_WithoutCompletingTheBeacon()
    {
        var cross = Content.Value.Rooms["drowned_tollbridge"].Features.Single(f => f.Id == "far_arch").Interactions.Single();

        Assert.Equal("cross", cross.Verb);
        Assert.Contains(cross.Conditions, c => c is FlagCondition { Flag: "tollbridge_resolved" });
        Assert.Contains(cross.Effects, e => e is MoveToEffect { Room: "warren_crossroads" });

        // The beacon quest's last stage says the beacon was relit, so only relighting it may set that stage.
        Assert.DoesNotContain(Flatten(cross.Effects), e => e is SetQuestStageEffect { Quest: "nocturne_beacon" });
    }

    [Fact]
    public void Epilogues_BranchOnBothActTwoBosses()
    {
        var flags = Content.Value.Manifest.Epilogues
            .SelectMany(e => e.Conditions)
            .OfType<FlagCondition>()
            .Select(c => c.Flag)
            .ToHashSet();

        Assert.Superset(new HashSet<string> { "vaelen_reconciled", "vaelen_destroyed", "silas_relieved", "silas_destroyed", "wren_joins_descent" }, flags);
        Assert.Contains(Content.Value.Manifest.Epilogues.SelectMany(e => e.Conditions), c => c is CounterCondition { Counter: "lantern_resonance" });
    }

    [Theory]
    [InlineData("echo_chapel", "challenged_miriel", "shone_chapel_echo")]
    [InlineData("drowned_tollbridge", "challenged_garin", "shone_bridge_echo")]
    [InlineData("vow_sanctum", "challenged_vaelen", "shone_sanctum_echo")]
    [InlineData("reliquary_bastion", "challenged_silas", "shone_bastion_echo")]
    public void BossFights_WaitForTheApproach_SoTheEchoCanBeShoneFirst(string roomId, string challengeFlag, string echoFlag)
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
    public void XpThresholds_PutLevelTwoAtTheBarrowHeart()
    {
        var content = Content.Value;

        Assert.Equal(new[] { 0, 100, 300, 900, 2700 }, content.Rules.XpThresholdsByLevel);

        // Both ways through the Barrow Heart (100 XP for a parley, 125 for a victory) reach level 2.
        Assert.True(content.Monsters["gravebound_knight"].Xp >= content.Rules.XpThresholdsByLevel[1]);
    }

    [Theory]
    [InlineData("oath_eater", "ash_brand", "iron_penance", SpellKind.Technique)]
    [InlineData("liminal_auditor", "ledger_strike", "writ_of_foreclosure", SpellKind.Technique)]
    [InlineData("vesper_cantor", "dirge_of_tallow", "shattering_cadence", SpellKind.Spell)]
    [InlineData("remnant_vessel", "ember_vigil", "death_knell_echo", SpellKind.Spell)]
    public void EveryCalling_LearnsOneAbilityAtLevelsTwoAndThree(string classId, string levelTwo, string levelThree, SpellKind levelThreeKind)
    {
        var content = Content.Value;
        var cls = content.Classes[classId];

        Assert.Equal(new[] { 2, 3 }, cls.SpellsByLevel.Select(g => g.Level));
        Assert.Equal(levelTwo, Assert.Single(cls.SpellsByLevel[0].Spells));
        Assert.Equal(levelThree, Assert.Single(cls.SpellsByLevel[1].Spells));

        // Level 3 is an attack: martial Callings get a once-per-fight technique, casters a 3 MP spell.
        var spell = content.Spells[levelThree];
        Assert.Equal((levelThreeKind, SpellTarget.Enemy), (spell.Kind, spell.Target));
        Assert.Equal(levelThreeKind == SpellKind.Technique ? (0, (int?)1) : (3, (int?)null), (spell.Cost.Mana, spell.Cost.UsesPerEncounter));
        Assert.Equal(cls.Mana is null, levelThreeKind == SpellKind.Technique);
        Assert.EndsWith("Learned at level 3.", spell.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void LevelThreeAbilities_HaveTheirSpecifiedResolutions()
    {
        var spells = Content.Value.Spells;

        var penance = Assert.IsType<SaveResolutionDef>(spells["iron_penance"].Resolution);
        Assert.Equal((Ability.Wis, "2d8", DamageType.Psychic, true, "grave_chill"), (penance.Ability, penance.Damage, penance.DamageType, penance.HalfOnSave, penance.Status));
        var writ = Assert.IsType<AutoHitResolutionDef>(spells["writ_of_foreclosure"].Resolution);
        Assert.Equal(("2d6+3", DamageType.Radiant), (writ.Damage, writ.DamageType));
        var cadence = Assert.IsType<SaveResolutionDef>(spells["shattering_cadence"].Resolution);
        Assert.Equal((Ability.Con, "3d8", DamageType.Force, true, (string?)null), (cadence.Ability, cadence.Damage, cadence.DamageType, cadence.HalfOnSave, cadence.Status));
        var knell = Assert.IsType<SpellAttackResolutionDef>(spells["death_knell_echo"].Resolution);
        Assert.Equal(("3d6", DamageType.Lightning, "grave_chill"), (knell.Damage, knell.DamageType, knell.Status));
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
