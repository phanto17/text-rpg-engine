using RpgEngine.Core.Characters;
using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Pixel.Presentation;

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

    private static readonly string[] ActThreeRooms = ["threshold_vestibule", "penitent_landing", "bell_nave", "cradle_of_ash"];

    private static readonly string[] EndingFlags = ["ending_triad_passage", "ending_sun_stasis", "ending_moon_vigil"];

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
    public void ShippedContent_HasThePrologueAndAllThreeActs()
    {
        var content = Content.Value;

        Assert.Equal(new[] { "oath_eater", "liminal_auditor", "vesper_cantor", "remnant_vessel" }, content.ClassList.Select(c => c.Id));
        Assert.Equal(new[] { "bereaved_kin", "ash_deserter", "moor_bailiff", "fever_dreamer" }, content.BackgroundList.Select(b => b.Id));
        Assert.All(content.BackgroundList, b => Assert.Equal(ItemType.Quest, content.Items[b.KeepsakeItem].Type));
        Assert.All(content.ClassList.SelectMany(c => c.Spells), id => Assert.True(content.Spells.ContainsKey(id)));
        Assert.Equal(18, content.Skills.Count);
        Assert.All(PrologueRooms.Concat(ActOneRooms).Concat(ActTwoRooms).Concat(ActThreeRooms), id => Assert.Contains(id, content.Rooms.Keys));
        Assert.All(new[] { "ashen_order", "hollow_choir", "lantern_bearers" }, id => Assert.Contains(id, content.Factions.Keys));
        Assert.All(new[] { "barrowlight", "nocturne_beacon", "twin_seals", "cradle_of_ash_quest" }, id => Assert.Contains(id, content.Quests.Keys));
        Assert.All(
            new[]
            {
                "wren", "orin", "maeve", "peddler", "wren_nocturne", "miriel", "garin", "caelan", "venn", "vaelen", "silas",
                "wren_cradle", "oakhaven", "architect_post",
            },
            id => Assert.Contains(id, content.Dialogues.Keys));
        Assert.All(
            new[] { "prelate_vaelen", "tethered_chorister", "commander_silas", "scoured_halberdier", "first_architect", "calcified_warden" },
            id => Assert.Contains(id, content.Monsters.Keys));
        Assert.All(
            new[]
            {
                "choir_silks", "lye_salve", "silver_tuning_blade", "scoured_halberd", "moon_triad_seal", "sun_triad_seal",
                "architect_scale_mail", "triad_ward_mantle", "wrens_ashen_censer", "vaelens_silver_chime", "silas_iron_standard",
            },
            id => Assert.Contains(id, content.Items.Keys));
        Assert.Contains("cradle_quartermaster", content.Shops.Keys);
        Assert.All(new[] { "moon_triad_seal", "sun_triad_seal" }, id => Assert.Equal(ItemType.Quest, content.Items[id].Type));
        Assert.Equal("inner_crypt", content.EncounterRooms["barrow_heart"]);
        Assert.Equal("echo_chapel", content.EncounterRooms["echo_chapel_choir"]);
        Assert.Equal("drowned_tollbridge", content.EncounterRooms["drowned_tollbridge_watch"]);
        Assert.Equal("vow_sanctum", content.EncounterRooms["archive_vaelen_boss"]);
        Assert.Equal("reliquary_bastion", content.EncounterRooms["reliquary_silas_boss"]);
        Assert.Equal("cradle_of_ash", content.EncounterRooms["cradle_architect_boss"]);
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
    public void OnlyTheGreatBrazier_EndsTheGame()
    {
        var content = Content.Value;

        var endings = content.RoomList.Where(room => RoomEffects(content, room).OfType<EndGameEffect>().Any()).Select(room => room.Id);

        Assert.Equal(new[] { "cradle_of_ash" }, endings);

        // Each of the three choices waits for the First Architect, sets its own ending flag, and closes the last quest.
        var brazier = content.Rooms["cradle_of_ash"].Features.Single(f => f.Id == "great_brazier").Interactions;
        var choices = brazier.Where(i => i.Effects.OfType<EndGameEffect>().Any()).ToList();
        Assert.Equal(new[] { "relight", "tune", "complete" }, choices.Select(i => i.Verb));
        Assert.Equal(new[] { "ending_sun_stasis", "ending_moon_vigil", "ending_triad_passage" }, choices.Select(i => i.Effects.OfType<SetFlagEffect>().Single().Flag));
        Assert.All(choices, i => Assert.Contains(i.Conditions, c => c is FlagCondition { Flag: "architect_resolved" }));
        Assert.All(choices, i => Assert.Contains(i.Effects, e => e is SetQuestStageEffect { Quest: "cradle_of_ash_quest", Stage: 100 }));
        Assert.All(choices, i => Assert.IsType<EndGameEffect>(i.Effects[^1]));
        Assert.Empty(content.Manifest.Epilogues[^1].Conditions);
    }

    [Fact]
    public void UnsealingTheTwinSealGate_LeadsDownIntoActThree()
    {
        var crossroads = Content.Value.Rooms["warren_crossroads"];
        var unseal = crossroads.Features.Single(f => f.Id == "twin_seal_gate").Interactions.Single();

        Assert.Equal("unseal", unseal.Verb);
        Assert.Contains(unseal.Conditions, c => c is HasItemCondition { Item: "moon_triad_seal" });
        Assert.Contains(unseal.Conditions, c => c is HasItemCondition { Item: "sun_triad_seal" });
        Assert.Contains(unseal.Effects, e => e is SetQuestStageEffect { Quest: "twin_seals", Stage: 100 });
        Assert.Contains(unseal.Effects, e => e is MoveToEffect { Room: "threshold_vestibule" });
        var down = crossroads.Exits.Single(e => e.Direction == Direction.Down);
        Assert.Equal("threshold_vestibule", down.Target);
        Assert.Contains(down.Conditions, c => c is FlagCondition { Flag: "unsealed_cradle_gate" });
    }

    [Theory]
    [InlineData("vaelen_parting_chime", "vaelen_reconciled", "listen", "vaelens_silver_chime", "carries_vaelen_testimony")]
    [InlineData("silas_parting_tablet", "silas_relieved", "read", "silas_iron_standard", "carries_silas_testimony")]
    public void PenitentLanding_HoldsTheRelicOfEachSparedBoss(string featureId, string sparedFlag, string verb, string relic, string testimonyFlag)
    {
        var feature = Content.Value.Rooms["penitent_landing"].Features.Single(f => f.Id == featureId);
        var take = feature.Interactions.Single();

        Assert.Contains(feature.Conditions, c => c is FlagCondition f && f.Flag == sparedFlag);
        Assert.Equal((verb, true), (take.Verb, take.Once));
        Assert.Contains(take.Effects, e => e is GiveItemEffect g && g.Item == relic);
        Assert.Contains(take.Effects, e => e is SetFlagEffect s && s.Flag == testimonyFlag);
    }

    [Fact]
    public void ThresholdTithe_CostsTenGold_AndHidesWithoutIt()
    {
        var tithe = Content.Value.Rooms["penitent_landing"].Features.Single(f => f.Id == "threshold_alms_bowl").Interactions.Single();

        Assert.Equal("tithe", tithe.Verb);
        Assert.Contains(tithe.Conditions, c => c is GoldCondition { Min: 10 });
        Assert.Contains(tithe.Effects, e => e is SpendGoldEffect { Amount: 10 });
    }

    [Fact]
    public void GreatVesperBell_AnswersToTheSplinterAndItsNotch()
    {
        var bell = Content.Value.Rooms["bell_nave"].Features.Single(f => f.Id == "sundered_vesper_bell");

        Assert.Superset(new HashSet<string> { "bell", "splinter", "notch" }, bell.Aliases.ToHashSet());
        Assert.Null(bell.Interactions.Single(i => i.Verb == "fit").Target);
    }

    [Theory]
    [InlineData("vaelen", "vaelen_gift_given")]
    [InlineData("silas", "silas_gift_given")]
    public void SparedActTwoBosses_PayTowardTheEndgameArmor(string dialogueId, string giftFlag)
    {
        var content = Content.Value;
        var gift = content.Dialogues[dialogueId].Nodes.Single(n => n.Effects.Any(e => e is SetFlagEffect s && s.Flag == giftFlag));

        Assert.Contains(gift.Effects, e => e is GiveGoldEffect { Amount: 25 });

        // Both purses together buy one piece of Oakhaven's armor, and the shop charges the item's value.
        var shop = content.Shops["cradle_quartermaster"];
        Assert.All(new[] { ("architect_scale_mail", 55), ("triad_ward_mantle", 50) }, x =>
            Assert.Equal((x.Item2, x.Item2), (shop.Stock.Single(s => s.Item == x.Item1).Price, content.Items[x.Item1].Value)));
    }

    [Fact]
    public void WrensBlessing_GivesHerCenser_OnlyWhenSheFollowedYouDown()
    {
        var content = Content.Value;
        var wren = content.Rooms["penitent_landing"].Npcs.Single(n => n.Id == "wren_cradle");
        var blessing = content.Dialogues[wren.Dialogue].Nodes.Single(n => n.Id == "blessing_node");

        Assert.Contains(wren.Conditions, c => c is FlagCondition { Flag: "wren_joins_descent" });
        Assert.Contains(blessing.Effects, e => e is GiveItemEffect { Item: "wrens_ashen_censer" });
        Assert.Contains(blessing.Effects, e => e is SetFlagEffect { Flag: "wren_cradle_blessing" });
    }

    [Fact]
    public void OakhavensShop_SellsTheEndgameArmorOnce()
    {
        var content = Content.Value;
        var shop = content.Shops["cradle_quartermaster"];

        Assert.Contains(content.Dialogues["oakhaven"].Nodes.SelectMany(n => n.Choices).SelectMany(c => c.Effects), e => e is StartShopEffect { Shop: "cradle_quartermaster" });
        Assert.All(new[] { "architect_scale_mail", "triad_ward_mantle" }, id => Assert.Equal(1, shop.Stock.Single(s => s.Item == id).Stock));
        Assert.Equal((17, 0), (content.Items["architect_scale_mail"].Armor!.BaseAc, content.Items["architect_scale_mail"].Armor!.MaxDexBonus));
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
    public void Epilogues_BranchOnTheEndingChoice_AndTheJourney()
    {
        var epilogues = Content.Value.Manifest.Epilogues;

        // Every epilogue but the fallback starts from exactly one choice at the Great Brazier, and each
        // choice has a gentler variation and an unconditional one of its own.
        var gated = epilogues.SkipLast(1).Select(e => e.Conditions.OfType<FlagCondition>().Single(c => EndingFlags.Contains(c.Flag)).Flag).ToList();
        Assert.Equal(EndingFlags, gated.Distinct());
        Assert.All(EndingFlags, flag => Assert.InRange(gated.Count(g => g == flag), 2, 3));
        Assert.All(EndingFlags, flag => Assert.Single(epilogues, e => e.Conditions.Count == 1 && e.Conditions[0] is FlagCondition f && f.Flag == flag));

        var flags = Flatten(epilogues.SelectMany(e => e.Conditions)).OfType<FlagCondition>().Select(c => c.Flag).ToHashSet();
        Assert.Superset(new HashSet<string> { "architect_reconciled", "wren_joins_descent", "vaelen_reconciled", "silas_relieved", "beacon_restored" }, flags);
        Assert.Contains(Flatten(epilogues.SelectMany(e => e.Conditions)), c => c is CounterCondition { Counter: "lantern_resonance" });
        Assert.All(epilogues, e => Assert.Contains("{player.name}", e.Text, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("echo_chapel", "challenged_miriel", "shone_chapel_echo")]
    [InlineData("drowned_tollbridge", "challenged_garin", "shone_bridge_echo")]
    [InlineData("vow_sanctum", "challenged_vaelen", "shone_sanctum_echo")]
    [InlineData("reliquary_bastion", "challenged_silas", "shone_bastion_echo")]
    [InlineData("cradle_of_ash", "challenged_architect", "shone_cradle_echo")]
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

        Assert.Equal(new[] { 0, 100, 300, 750, 2700 }, content.Rules.XpThresholdsByLevel);

        // Both ways through the Barrow Heart (100 XP for a parley, 125 for a victory) reach level 2.
        Assert.True(content.Monsters["gravebound_knight"].Xp >= content.Rules.XpThresholdsByLevel[1]);
    }

    [Theory]
    [InlineData("oath_eater", "ash_brand", "iron_penance", SpellKind.Technique, "unbroken_vigil")]
    [InlineData("liminal_auditor", "ledger_strike", "writ_of_foreclosure", SpellKind.Technique, "final_audit")]
    [InlineData("vesper_cantor", "dirge_of_tallow", "shattering_cadence", SpellKind.Spell, "requiem_of_the_triad")]
    [InlineData("remnant_vessel", "ember_vigil", "death_knell_echo", SpellKind.Spell, "reclaim_the_ember")]
    public void EveryCalling_LearnsOneAbilityAtLevelsTwoThreeAndFour(string classId, string levelTwo, string levelThree, SpellKind levelThreeKind, string capstone)
    {
        var content = Content.Value;
        var cls = content.Classes[classId];

        Assert.Equal(new[] { 2, 3, 4 }, cls.SpellsByLevel.Select(g => g.Level));
        Assert.Equal(levelTwo, Assert.Single(cls.SpellsByLevel[0].Spells));
        Assert.Equal(levelThree, Assert.Single(cls.SpellsByLevel[1].Spells));
        Assert.Equal(capstone, Assert.Single(cls.SpellsByLevel[2].Spells));
        Assert.EndsWith("Learned at level 4.", content.Spells[capstone].Description, StringComparison.Ordinal);

        // Martial Callings' capstones are once-per-fight techniques, casters' cost mana.
        Assert.Equal(cls.Mana is null, content.Spells[capstone].Kind == SpellKind.Technique);

        // Level 3 is an attack: martial Callings get a once-per-fight technique, casters a 3 MP spell.
        var spell = content.Spells[levelThree];
        Assert.Equal((levelThreeKind, SpellTarget.Enemy), (spell.Kind, spell.Target));
        Assert.Equal(levelThreeKind == SpellKind.Technique ? (0, (int?)1) : (3, (int?)null), (spell.Cost.Mana, spell.Cost.UsesPerEncounter));
        Assert.Equal(cls.Mana is null, levelThreeKind == SpellKind.Technique);
        Assert.EndsWith("Learned at level 3.", spell.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void LevelFourCapstones_HaveTheirSpecifiedResolutions()
    {
        var spells = Content.Value.Spells;

        var vigil = spells["unbroken_vigil"];
        Assert.Equal((SpellTarget.Self, (int?)1), (vigil.Target, vigil.Cost.UsesPerEncounter));
        var vigilEffects = Assert.IsType<EffectsResolutionDef>(vigil.Resolution).Effects;
        Assert.Equal("2d8+2", Assert.IsType<HealEffect>(vigilEffects[0]).Dice);
        Assert.Equal("vigilant", Assert.IsType<ApplyStatusEffect>(vigilEffects[1]).Status);

        var audit = spells["final_audit"];
        Assert.Equal((SpellTarget.Enemy, (int?)1), (audit.Target, audit.Cost.UsesPerEncounter));
        Assert.Equal("3d6", Assert.IsType<WeaponAttackResolutionDef>(audit.Resolution).BonusDamage);

        var requiem = spells["requiem_of_the_triad"];
        Assert.Equal(4, requiem.Cost.Mana);
        var requiemSave = Assert.IsType<SaveResolutionDef>(requiem.Resolution);
        Assert.Equal((Ability.Wis, "4d8", DamageType.Radiant, true, "grave_chill"), (requiemSave.Ability, requiemSave.Damage, requiemSave.DamageType, requiemSave.HalfOnSave, requiemSave.Status));

        var ember = spells["reclaim_the_ember"];
        Assert.Equal(3, ember.Cost.Mana);
        var emberHit = Assert.IsType<AutoHitResolutionDef>(ember.Resolution);
        Assert.Equal(("3d6+3", DamageType.Force), (emberHit.Damage, emberHit.DamageType));
    }

    [Theory]
    [InlineData("vigilant", "Vigilant", 2, 2, AttackRollModifier.Advantage)]
    [InlineData("blessed_ward", "Wren's Ward", 3, 2, AttackRollModifier.Normal)]
    [InlineData("unburdened_focus", "Unburdened Focus", 2, 1, AttackRollModifier.Advantage)]
    public void BuffStatuses_RaiseArmorClass(string id, string name, int rounds, int acBonus, AttackRollModifier attackRolls)
    {
        var status = Content.Value.StatusEffects[id];

        Assert.Equal((name, rounds, acBonus, attackRolls), (status.Name, status.DurationRounds, status.Modifiers.AcBonus, status.Modifiers.AttackRolls));
        Assert.Null(status.Modifiers.DamagePerTurn);
        Assert.False(status.Modifiers.SkipTurn);
    }

    [Theory]
    [InlineData("wrens_ashen_censer", "2d6+3", "blessed_ward")]
    [InlineData("silas_iron_standard", "2d4+2", "unburdened_focus")]
    public void AllyBlessings_HealAndBuffTheUser(string itemId, string heal, string status)
    {
        var item = Content.Value.Items[itemId];
        var use = item.Use!;

        Assert.Equal((ItemType.Consumable, 50, true, true, SpellTarget.Self), (item.Type, item.Value, use.InCombat, use.Consume, use.Target));
        Assert.Equal(heal, Assert.IsType<HealEffect>(use.Effects[0]).Dice);
        Assert.Equal(status, Assert.IsType<ApplyStatusEffect>(use.Effects[1]).Status);
    }

    [Fact]
    public void VaelensChime_StrikesAnEnemyForForceDamage()
    {
        var item = Content.Value.Items["vaelens_silver_chime"];
        var use = item.Use!;

        Assert.Equal((ItemType.Consumable, 50, true, true, SpellTarget.Enemy), (item.Type, item.Value, use.InCombat, use.Consume, use.Target));
        var hit = Assert.IsType<AutoHitResolutionDef>(use.Resolution);
        Assert.Equal(("3d6", DamageType.Force), (hit.Damage, hit.DamageType));
        Assert.Empty(use.Effects);
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

    private static IEnumerable<ConditionDef> Flatten(IEnumerable<ConditionDef> conditions) =>
        conditions.SelectMany(condition => condition switch
        {
            AllCondition c => Flatten(c.Conditions).Prepend(condition),
            AnyCondition c => Flatten(c.Conditions).Prepend(condition),
            NotCondition c => Flatten(c.Conditions).Prepend(condition),
            _ => [condition],
        });

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

    [Fact]
    public void ShippedPresentation_LoadsWithoutWarnings_AndMatchesTheSchemaRecords()
    {
        var result = PresentationLoader.Load(DataDirectory);

        Assert.True(result.FileFound);
        Assert.Empty(result.Warnings);

        // The strict content options reject unknown properties and enum names, as the schema does.
        var json = File.ReadAllText(Path.Combine(DataDirectory, "World", "presentation.json"));
        var strict = System.Text.Json.JsonSerializer.Deserialize<PresentationDef>(json, ContentJson.Options)!;
        Assert.Equal(result.Presentation.Rooms.Keys.Order(), strict.Rooms.Keys.Order());
        Assert.Equal(result.Presentation.Monsters.Keys.Order(), strict.Monsters.Keys.Order());
        Assert.Equal(result.Presentation.Npcs.Keys.Order(), strict.Npcs.Keys.Order());
    }

    [Fact]
    public void ShippedPresentation_CoversEveryRoomMonsterAndDialogue()
    {
        var content = Content.Value;
        var presentation = PresentationLoader.Load(DataDirectory).Presentation;
        var npcs = content.RoomList.SelectMany(room => room.Npcs).ToList();

        Assert.Equal(content.Rooms.Keys.Order(), presentation.Rooms.Keys.Order());
        Assert.Equal(content.Monsters.Keys.Order(), presentation.Monsters.Keys.Order());

        // NPC entries are keyed by NPC id or by the dialogue the NPC speaks; every dialogue is covered once.
        var dialogueOf = presentation.Npcs.Keys.ToDictionary(
            key => key,
            key => content.Dialogues.ContainsKey(key) ? key : npcs.FirstOrDefault(n => n.Id == key)?.Dialogue);
        Assert.All(dialogueOf, pair => Assert.True(pair.Value is not null, $"presentation npc '{pair.Key}' is neither a dialogue nor an NPC"));
        Assert.Equal(content.Dialogues.Keys.Order(), dialogueOf.Values.Order());

        Assert.All(presentation.Rooms, pair =>
        {
            var room = pair.Value;
            Assert.NotNull(room.SceneStyle);
            Assert.NotNull(room.Weather);
            Assert.NotNull(room.MoodCaption);
            Assert.All(new[] { room.Palette?.BackgroundTop, room.Palette?.BackgroundBottom, room.Palette?.Architecture, room.Palette?.Accent }, Assert.NotNull);
        });
        Assert.Equal(presentation.Rooms.Count, presentation.Rooms.Values.Select(r => r.SceneStyle).Distinct().Count());
        Assert.All(presentation.Monsters.Values, monster => Assert.NotNull(monster.Emblem));

        // The name under a portrait matches the speaker named in the log.
        Assert.All(dialogueOf, pair => Assert.Equal(content.Dialogues[pair.Value!].Speaker, presentation.Npcs[pair.Key].SpeakerName));
        Assert.All(presentation.Npcs.Values, npc => Assert.NotNull(npc.Emblem));
    }
}
