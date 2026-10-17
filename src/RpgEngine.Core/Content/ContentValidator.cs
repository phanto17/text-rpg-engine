using System.Text.RegularExpressions;
using RpgEngine.Core.Characters;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;

namespace RpgEngine.Core.Content;

/// <summary>
/// Cross-checks loaded content: ids, references, dice expressions, dialogue graphs, and flags or
/// counters that are read but never written (almost always a typo). Returns every error found.
/// </summary>
public static partial class ContentValidator
{
    private static readonly HashSet<int> HitDice = [6, 8, 10, 12];
    private static readonly HashSet<string> ReservedVerbs = new(StringComparer.Ordinal) { "go", "inventory", "stats", "journal", "save", "load", "help", "quit" };

    public static IReadOnlyList<string> Validate(GameContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new Run(content).Execute();
    }

    [GeneratedRegex("^[a-z0-9]+(_[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SnakeCase();

    [GeneratedRegex("^[a-z]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SingleWord();

    private sealed class Run(GameContent content)
    {
        private readonly List<string> _errors = [];
        private readonly HashSet<string> _flagsSet = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _flagsRead = new(StringComparer.Ordinal);
        private readonly HashSet<string> _countersWritten = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _countersRead = new(StringComparer.Ordinal);

        public IReadOnlyList<string> Execute()
        {
            ValidateIds(content.ClassList, "class");
            ValidateIds(content.SkillList, "skill");
            ValidateIds(content.SpellList, "spell");
            ValidateIds(content.StatusEffectList, "status effect");
            ValidateIds(content.FactionList, "faction");
            ValidateIds(content.QuestList, "quest");
            ValidateIds(content.ItemList, "item");
            ValidateIds(content.MonsterList, "monster");
            ValidateIds(content.RoomList, "room");
            ValidateIds(content.DialogueList, "dialogue");

            ValidateManifest();
            content.ClassList.ToList().ForEach(ValidateClass);
            content.SkillList.ToList().ForEach(skill => RequireText($"skill '{skill.Id}'", skill.Name, "name"));
            content.SpellList.ToList().ForEach(ValidateSpell);
            content.StatusEffectList.ToList().ForEach(ValidateStatus);
            content.FactionList.ToList().ForEach(faction => RequireText($"faction '{faction.Id}'", faction.Name, "name"));
            content.QuestList.ToList().ForEach(ValidateQuest);
            content.ItemList.ToList().ForEach(ValidateItem);
            content.MonsterList.ToList().ForEach(ValidateMonster);
            content.RoomList.ToList().ForEach(ValidateRoom);
            content.DialogueList.ToList().ForEach(ValidateDialogue);
            ValidateEncounterIds();

            if (content.ClassList.Count == 0)
            {
                _errors.Add("Rules/classes.json: at least one class is required.");
            }

            foreach (var (flag, context) in _flagsRead.Where(pair => !_flagsSet.Contains(pair.Key)))
            {
                Error(context, $"flag '{flag}' is checked but never set by any effect.");
            }

            foreach (var (counter, context) in _countersRead.Where(pair => !_countersWritten.Contains(pair.Key)))
            {
                Error(context, $"counter '{counter}' is checked but never changed by any effect.");
            }

            return _errors;
        }

        private void Error(string context, string message) => _errors.Add($"{context}: {message}");

        private void ValidateIds<T>(IReadOnlyList<T> definitions, string kind)
            where T : IContentDef
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (!SnakeCase().IsMatch(definition.Id))
                {
                    Error($"{kind} '{definition.Id}'", "id must be snake_case (a-z, 0-9, underscores).");
                }

                if (!seen.Add(definition.Id))
                {
                    Error($"{kind} '{definition.Id}'", "duplicate id.");
                }
            }
        }

        private void ValidateManifest()
        {
            const string context = "game.json";
            var manifest = content.Manifest;
            RequireText(context, manifest.Title, "title");
            RequireText(context, manifest.ContentVersion, "contentVersion");
            RequireText(context, manifest.Intro, "intro");
            RequireRoom(context, manifest.StartRoom, "startRoom");
            RequireRange(context, manifest.StartingGold, 0, int.MaxValue, "startingGold");

            var rules = manifest.Rules;
            if (rules.StandardArray.Count != 6)
            {
                Error(context, "rules.standardArray must have exactly 6 values.");
            }

            foreach (var score in rules.StandardArray)
            {
                RequireRange(context, score, AbilityScores.MinScore, AbilityScores.MaxScore, "rules.standardArray value");
            }

            if (rules.ProficiencyBonusByLevel.Count == 0)
            {
                Error(context, "rules.proficiencyBonusByLevel must not be empty.");
            }

            foreach (var bonus in rules.ProficiencyBonusByLevel)
            {
                RequireRange(context, bonus, 0, 10, "rules.proficiencyBonusByLevel value");
            }

            ValidateCheck($"{context} rules.flee", rules.Flee);
            RequireStatus(context, rules.DefendStatus, "rules.defendStatus");

            if (manifest.Epilogues.Count == 0)
            {
                Error(context, "at least one epilogue is required.");
            }
            else if (manifest.Epilogues[^1].Conditions.Count > 0)
            {
                Error(context, "the last epilogue must have no conditions so one always applies.");
            }

            for (var i = 0; i < manifest.Epilogues.Count; i++)
            {
                var epilogueContext = $"{context} epilogue {i + 1}";
                RequireText(epilogueContext, manifest.Epilogues[i].Text, "text");
                ValidateConditions(epilogueContext, manifest.Epilogues[i].Conditions);
            }
        }

        private void ValidateClass(ClassDef cls)
        {
            var context = $"class '{cls.Id}'";
            RequireText(context, cls.Name, "name");
            RequireText(context, cls.Description, "description");

            if (!HitDice.Contains(cls.HitDie))
            {
                Error(context, $"hitDie must be one of {string.Join(", ", HitDice)}.");
            }

            if (cls.AbilityPriority.Distinct().Count() != 6 || cls.AbilityPriority.Count != 6)
            {
                Error(context, "abilityPriority must list each of the six abilities exactly once.");
            }

            if (cls.SavingThrows.Distinct().Count() != cls.SavingThrows.Count)
            {
                Error(context, "savingThrows contains duplicates.");
            }

            var choices = cls.SkillChoices;
            RequireRange(context, choices.Count, 0, choices.From.Count, "skillChoices.count");
            if (choices.From.Distinct(StringComparer.Ordinal).Count() != choices.From.Count)
            {
                Error(context, "skillChoices.from contains duplicates.");
            }

            foreach (var skill in choices.From)
            {
                RequireSkill(context, skill, "skillChoices.from");
            }

            RequireRange(context, cls.ExpertiseCount, 0, choices.Count, "expertiseCount");

            if (cls.Mana is { } mana)
            {
                RequireRange(context, mana.Base, 0, 100, "mana.base");
            }

            foreach (var starting in cls.StartingItems)
            {
                if (!RequireItem(context, starting.Item, "startingItems", out var item))
                {
                    continue;
                }

                RequireRange(context, starting.Count, 1, 99, $"startingItems '{starting.Item}' count");
                if (starting.Equip && item.Type is not (ItemType.Weapon or ItemType.Armor))
                {
                    Error(context, $"startingItems '{starting.Item}' cannot be equipped (only weapons and armor).");
                }
            }

            foreach (var spell in cls.Spells)
            {
                if (!content.Spells.ContainsKey(spell))
                {
                    Error(context, $"spells references unknown spell '{spell}'.");
                }
            }
        }

        private void ValidateSpell(SpellDef spell)
        {
            var context = $"spell '{spell.Id}'";
            RequireText(context, spell.Name, "name");
            RequireText(context, spell.Description, "description");
            RequireRange(context, spell.Cost.Mana, 0, 100, "cost.mana");

            if (spell.Cost.UsesPerEncounter is { } uses)
            {
                RequireRange(context, uses, 1, 99, "cost.usesPerEncounter");
                if (spell.Cost.Mana > 0)
                {
                    Error(context, "cost may use mana or usesPerEncounter, not both.");
                }
            }

            var expectedTarget = spell.Resolution is EffectsResolutionDef ? SpellTarget.Self : SpellTarget.Enemy;
            if (spell.Target != expectedTarget)
            {
                Error(context, $"target must be '{expectedTarget.ToString().ToLowerInvariant()}' for this resolution type.");
            }

            switch (spell.Resolution)
            {
                case AutoHitResolutionDef autoHit:
                    RequireDice(context, autoHit.Damage, "resolution.damage");
                    break;
                case SpellAttackResolutionDef attack:
                    RequireDice(context, attack.Damage, "resolution.damage");
                    RequireOptionalStatus(context, attack.Status);
                    break;
                case WeaponAttackResolutionDef weapon when weapon.BonusDamage is not null:
                    RequireDice(context, weapon.BonusDamage, "resolution.bonusDamage");
                    break;
                case SaveResolutionDef save:
                    RequireDice(context, save.Damage, "resolution.damage");
                    RequireOptionalStatus(context, save.Status);
                    if (save.Dc is { } dc)
                    {
                        RequireRange(context, dc, 1, 30, "resolution.dc");
                    }

                    break;
                case EffectsResolutionDef effects:
                    if (effects.Effects.Count == 0)
                    {
                        Error(context, "resolution.effects must not be empty.");
                    }

                    ValidateEffects(context, effects.Effects);
                    break;
            }
        }

        private void ValidateStatus(StatusEffectDef status)
        {
            var context = $"status effect '{status.Id}'";
            RequireText(context, status.Name, "name");
            RequireText(context, status.Description, "description");
            RequireRange(context, status.DurationRounds, 1, 99, "durationRounds");
            if (status.Modifiers.DamagePerTurn is { } dice)
            {
                RequireDice(context, dice, "modifiers.damagePerTurn");
            }
        }

        private void ValidateQuest(QuestDef quest)
        {
            var context = $"quest '{quest.Id}'";
            RequireText(context, quest.Name, "name");
            if (quest.Stages.Count == 0)
            {
                Error(context, "stages must not be empty.");
            }

            if (quest.Stages.Select(stage => stage.Stage).Distinct().Count() != quest.Stages.Count)
            {
                Error(context, "stage numbers must be unique.");
            }

            foreach (var stage in quest.Stages)
            {
                RequireRange(context, stage.Stage, 1, int.MaxValue, "stage");
                RequireText(context, stage.Journal, $"stage {stage.Stage} journal");
            }
        }

        private void ValidateItem(ItemDef item)
        {
            var context = $"item '{item.Id}'";
            RequireText(context, item.Name, "name");
            RequireText(context, item.Description, "description");
            RequireRange(context, item.Value, 0, int.MaxValue, "value");
            RequireAliases(context, item.Aliases);

            if ((item.Type == ItemType.Weapon) != (item.Weapon is not null))
            {
                Error(context, "items of type 'weapon' (and only those) must have a 'weapon' block.");
            }

            if ((item.Type == ItemType.Armor) != (item.Armor is not null))
            {
                Error(context, "items of type 'armor' (and only those) must have an 'armor' block.");
            }

            if (item.Type == ItemType.Consumable && item.Use is null)
            {
                Error(context, "consumable items must have a 'use' block.");
            }

            if (item.Weapon is { } weapon)
            {
                RequireDice(context, weapon.Damage, "weapon.damage");
            }

            if (item.Armor is { } armor)
            {
                RequireRange(context, armor.BaseAc, 1, 30, "armor.baseAc");
                if (armor.MaxDexBonus is { } maxDex)
                {
                    RequireRange(context, maxDex, 0, 10, "armor.maxDexBonus");
                }
            }

            if (item.Use is { } use)
            {
                if (use.Effects.Count == 0)
                {
                    Error(context, "use.effects must not be empty.");
                }

                ValidateEffects(context, use.Effects);
            }
        }

        private void ValidateMonster(MonsterDef monster)
        {
            var context = $"monster '{monster.Id}'";
            RequireText(context, monster.Name, "name");
            RequireText(context, monster.Description, "description");
            if (RequireDice(context, monster.Hp, "hp") is { Min: < 1 })
            {
                Error(context, "hp must always be at least 1.");
            }

            RequireRange(context, monster.Ac, 1, 30, "ac");
            RequireRange(context, monster.Xp, 0, int.MaxValue, "xp");
            RequireRange(context, monster.Ai.FleeBelowHpPercent, 0, 99, "ai.fleeBelowHpPercent");

            try
            {
                monster.Abilities.ToScores();
            }
            catch (ArgumentOutOfRangeException)
            {
                Error(context, $"abilities must be between {AbilityScores.MinScore} and {AbilityScores.MaxScore}.");
            }

            if (monster.Attacks.Count == 0)
            {
                Error(context, "attacks must not be empty.");
            }

            foreach (var attack in monster.Attacks)
            {
                var attackContext = $"{context} attack '{attack.Name}'";
                RequireText(attackContext, attack.Name, "name");
                RequireDice(attackContext, attack.Damage, "damage");
                RequireRange(attackContext, attack.Weight, 1, 100, "weight");
                if (attack.OnHit is { } onHit)
                {
                    RequireStatus(attackContext, onHit.Status, "onHit.status");
                    if (onHit.Save is { } save)
                    {
                        RequireRange(attackContext, save.Dc, 1, 30, "onHit.save.dc");
                    }
                }
            }

            foreach (var loot in monster.Loot)
            {
                RequireItem(context, loot.Item, "loot", out _);
                if (loot.Chance is <= 0 or > 1)
                {
                    Error(context, $"loot '{loot.Item}' chance must be greater than 0 and at most 1.");
                }
            }
        }

        private void ValidateRoom(RoomDef room)
        {
            var context = $"room '{room.Id}'";
            RequireText(context, room.Name, "name");
            RequireText(context, room.Description, "description");

            foreach (var variant in room.Variants)
            {
                RequireText(context, variant.Text, "variants text");
                ValidateConditions($"{context} variant", variant.Conditions);
            }

            var directions = new HashSet<Direction>();
            foreach (var exit in room.Exits)
            {
                var exitContext = $"{context} exit '{exit.Direction.ToString().ToLowerInvariant()}'";
                if (!directions.Add(exit.Direction))
                {
                    Error(exitContext, "duplicate direction.");
                }

                RequireRoom(exitContext, exit.Target, "target");
                ValidateConditions(exitContext, exit.Conditions);
            }

            var featureIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var feature in room.Features)
            {
                ValidateFeature(context, feature, featureIds);
            }

            foreach (var itemId in room.Items)
            {
                RequireItem(context, itemId, "items", out _);
            }

            var npcIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var npc in room.Npcs)
            {
                var npcContext = $"{context} npc '{npc.Id}'";
                if (!npcIds.Add(npc.Id))
                {
                    Error(npcContext, "duplicate npc id in room.");
                }

                RequireText(npcContext, npc.Name, "name");
                RequireText(npcContext, npc.Description, "description");
                RequireAliases(npcContext, npc.Aliases);
                if (!content.Dialogues.ContainsKey(npc.Dialogue))
                {
                    Error(npcContext, $"references unknown dialogue '{npc.Dialogue}'.");
                }

                ValidateConditions(npcContext, npc.Conditions);
            }

            if (room.Encounter is { } encounter)
            {
                ValidateEncounter(context, encounter);
            }

            ValidateEffects($"{context} onFirstEnter", room.OnFirstEnter);
            ValidateEffects($"{context} onEnter", room.OnEnter);
        }

        private void ValidateFeature(string roomContext, FeatureDef feature, HashSet<string> featureIds)
        {
            var context = $"{roomContext} feature '{feature.Id}'";
            if (!SnakeCase().IsMatch(feature.Id))
            {
                Error(context, "id must be snake_case.");
            }

            if (!featureIds.Add(feature.Id))
            {
                Error(context, "duplicate feature id in room.");
            }

            RequireText(context, feature.Name, "name");
            RequireText(context, feature.Description, "description");
            RequireAliases(context, feature.Aliases);
            ValidateConditions(context, feature.Conditions);

            foreach (var interaction in feature.Interactions)
            {
                var interactionContext = $"{context} interaction '{interaction.Verb}{(interaction.Target is null ? string.Empty : " " + interaction.Target)}'";
                if (!SingleWord().IsMatch(interaction.Verb))
                {
                    Error(interactionContext, "verb must be a single lowercase word.");
                }
                else if (ReservedVerbs.Contains(interaction.Verb))
                {
                    Error(interactionContext, $"verb '{interaction.Verb}' is a built-in command and can never be reached.");
                }

                if (interaction.Target is { } target && string.IsNullOrWhiteSpace(target))
                {
                    Error(interactionContext, "target must not be blank.");
                }

                ValidateConditions(interactionContext, interaction.Conditions);
                ValidateEffects(interactionContext, interaction.Effects);

                if (interaction.Check is { } check)
                {
                    ValidateCheck(interactionContext, check);
                }
                else if (interaction.Success is not null || interaction.Failure is not null)
                {
                    Error(interactionContext, "success/failure outcomes require a check.");
                }

                ValidateEffects($"{interactionContext} success", interaction.Success?.Effects ?? []);
                ValidateEffects($"{interactionContext} failure", interaction.Failure?.Effects ?? []);
            }
        }

        private void ValidateEncounter(string roomContext, EncounterDef encounter)
        {
            var context = $"{roomContext} encounter '{encounter.Id}'";
            if (!SnakeCase().IsMatch(encounter.Id))
            {
                Error(context, "id must be snake_case.");
            }

            ValidateConditions(context, encounter.Conditions);
            if (encounter.Enemies.Count == 0)
            {
                Error(context, "enemies must not be empty.");
            }

            foreach (var enemy in encounter.Enemies)
            {
                if (!content.Monsters.ContainsKey(enemy.Monster))
                {
                    Error(context, $"references unknown monster '{enemy.Monster}'.");
                }

                RequireRange(context, enemy.Count, 1, 10, $"enemy '{enemy.Monster}' count");
            }

            if (encounter.FleeTo is { } fleeTo)
            {
                RequireRoom(context, fleeTo, "fleeTo");
            }
            else if (encounter.CanFlee)
            {
                Error(context, "fleeTo is required when canFlee is true.");
            }

            if (encounter.Parley is { } parley)
            {
                if (parley.Options.Count == 0)
                {
                    Error(context, "parley.options must not be empty.");
                }

                foreach (var option in parley.Options)
                {
                    var optionContext = $"{context} parley '{option.Label}'";
                    RequireText(optionContext, option.Label, "label");
                    ValidateConditions(optionContext, option.Conditions);
                    ValidateCheck(optionContext, option.Check);
                }

                ValidateEffects($"{context} parley.onSuccess", parley.OnSuccess);
                ValidateEffects($"{context} parley.onFailure", parley.OnFailure);
            }

            ValidateEffects($"{context} onVictory", encounter.OnVictory);
            ValidateEffects($"{context} onResolved", encounter.OnResolved);
        }

        private void ValidateEncounterIds()
        {
            var duplicates = content.RoomList
                .Where(room => room.Encounter is not null)
                .GroupBy(room => room.Encounter!.Id, StringComparer.Ordinal)
                .Where(group => group.Count() > 1);

            foreach (var group in duplicates)
            {
                Error($"encounter '{group.Key}'", $"duplicate encounter id in rooms {string.Join(", ", group.Select(room => $"'{room.Id}'"))}.");
            }
        }

        private void ValidateDialogue(DialogueDef dialogue)
        {
            var context = $"dialogue '{dialogue.Id}'";
            RequireText(context, dialogue.Speaker, "speaker");

            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in dialogue.Nodes)
            {
                if (!SnakeCase().IsMatch(node.Id))
                {
                    Error($"{context} node '{node.Id}'", "id must be snake_case.");
                }

                if (!nodeIds.Add(node.Id))
                {
                    Error($"{context} node '{node.Id}'", "duplicate node id.");
                }
            }

            if (dialogue.Nodes.Count == 0)
            {
                Error(context, "nodes must not be empty.");
            }

            if (dialogue.Entries.Count == 0)
            {
                Error(context, "entries must not be empty.");
            }
            else if (dialogue.Entries[^1].Conditions.Count > 0)
            {
                Error(context, "the last entry must have no conditions so the conversation can always start.");
            }

            foreach (var entry in dialogue.Entries)
            {
                RequireNode(context, nodeIds, entry.Node, "entry node");
                ValidateConditions($"{context} entry '{entry.Node}'", entry.Conditions);
            }

            foreach (var node in dialogue.Nodes)
            {
                ValidateNode(context, node, nodeIds);
            }
        }

        private void ValidateNode(string dialogueContext, DialogueNodeDef node, HashSet<string> nodeIds)
        {
            var context = $"{dialogueContext} node '{node.Id}'";
            RequireText(context, node.Text, "text");
            ValidateEffects(context, node.Effects);

            if (node.Next is { } next)
            {
                RequireNode(context, nodeIds, next, "next");
                if (node.Choices.Count > 0)
                {
                    Error(context, "a node cannot have both choices and next.");
                }
            }

            for (var i = 0; i < node.Choices.Count; i++)
            {
                var choice = node.Choices[i];
                var choiceContext = $"{context} choice {i + 1}";
                RequireText(choiceContext, choice.Text, "text");
                ValidateConditions(choiceContext, choice.Conditions);
                ValidateEffects(choiceContext, choice.Effects);

                var routes = (choice.Next is null ? 0 : 1) + (choice.End ? 1 : 0) + (choice.Check is null ? 0 : 1);
                if (routes != 1)
                {
                    Error(choiceContext, "must have exactly one of next, end, or check.");
                }

                if (choice.Next is { } choiceNext)
                {
                    RequireNode(choiceContext, nodeIds, choiceNext, "next");
                }

                if (choice.Check is { } check)
                {
                    ValidateCheck(choiceContext, check);
                    RequireNode(choiceContext, nodeIds, choice.SuccessNode, "successNode");
                    RequireNode(choiceContext, nodeIds, choice.FailureNode, "failureNode");
                }
                else if (choice.SuccessNode is not null || choice.FailureNode is not null)
                {
                    Error(choiceContext, "successNode/failureNode require a check.");
                }
            }
        }

        private void ValidateCheck(string context, CheckDef check)
        {
            if ((check.Skill is null) == (check.Ability is null))
            {
                Error(context, "a check needs exactly one of skill or ability.");
            }

            if (check.Skill is { } skill)
            {
                RequireSkill(context, skill, "check.skill");
            }

            RequireRange(context, check.Dc, 1, 30, "check.dc");
            ValidateConditions(context, check.AdvantageIf);
            ValidateConditions(context, check.DisadvantageIf);
        }

        private void ValidateConditions(string context, IReadOnlyList<ConditionDef> conditions)
        {
            foreach (var condition in conditions)
            {
                ValidateCondition(context, condition);
            }
        }

        private void ValidateCondition(string context, ConditionDef condition)
        {
            switch (condition)
            {
                case FlagCondition flag:
                    ReadFlag(context, flag.Flag);
                    break;
                case NotFlagCondition notFlag:
                    ReadFlag(context, notFlag.Flag);
                    break;
                case HasItemCondition hasItem:
                    RequireItem(context, hasItem.Item, "hasItem", out _);
                    RequireRange(context, hasItem.Count, 1, 999, "hasItem count");
                    break;
                case LacksItemCondition lacksItem:
                    RequireItem(context, lacksItem.Item, "lacksItem", out _);
                    break;
                case CounterCondition counter:
                    if (RequireText(context, counter.Counter, "counter"))
                    {
                        _countersRead.TryAdd(counter.Counter, context);
                    }

                    RequireBound(context, counter.Min, counter.Max, counter.EqualTo, "counter");
                    break;
                case ReputationCondition reputation:
                    RequireFaction(context, reputation.Faction);
                    RequireBound(context, reputation.Min, reputation.Max, null, "reputation");
                    break;
                case QuestStageCondition quest:
                    RequireQuest(context, quest.Quest, out _);
                    RequireBound(context, quest.Min, quest.Max, quest.EqualTo, "questStage");
                    break;
                case ClassCondition cls:
                    if (!content.Classes.ContainsKey(cls.ClassId))
                    {
                        Error(context, $"condition references unknown class '{cls.ClassId}'.");
                    }

                    break;
                case AbilityScoreCondition ability:
                    RequireRange(context, ability.Min, AbilityScores.MinScore, AbilityScores.MaxScore, "abilityScore min");
                    break;
                case VisitedCondition visited:
                    RequireRoom(context, visited.Room, "visited");
                    break;
                case AllCondition all:
                    ValidateComposite(context, all.Conditions, "all");
                    break;
                case AnyCondition any:
                    ValidateComposite(context, any.Conditions, "any");
                    break;
                case NotCondition not:
                    ValidateComposite(context, not.Conditions, "not");
                    break;
            }
        }

        private void ValidateComposite(string context, IReadOnlyList<ConditionDef> conditions, string kind)
        {
            if (conditions.Count == 0)
            {
                Error(context, $"'{kind}' condition needs at least one inner condition.");
            }

            ValidateConditions(context, conditions);
        }

        private void ValidateEffects(string context, IReadOnlyList<EffectDef> effects)
        {
            foreach (var effect in effects)
            {
                ValidateEffect(context, effect);
            }
        }

        private void ValidateEffect(string context, EffectDef effect)
        {
            switch (effect)
            {
                case SetFlagEffect setFlag:
                    if (RequireText(context, setFlag.Flag, "setFlag flag"))
                    {
                        _flagsSet.Add(setFlag.Flag);
                    }

                    break;
                case ClearFlagEffect clearFlag:
                    RequireText(context, clearFlag.Flag, "clearFlag flag");
                    break;
                case SetCounterEffect setCounter:
                    WriteCounter(context, setCounter.Counter);
                    break;
                case AddCounterEffect addCounter:
                    WriteCounter(context, addCounter.Counter);
                    break;
                case ChangeReputationEffect reputation:
                    RequireFaction(context, reputation.Faction);
                    break;
                case SetQuestStageEffect questStage:
                    if (RequireQuest(context, questStage.Quest, out var quest) && quest.Stages.All(stage => stage.Stage != questStage.Stage))
                    {
                        Error(context, $"quest '{quest.Id}' has no stage {questStage.Stage}.");
                    }

                    break;
                case GiveItemEffect giveItem:
                    RequireItem(context, giveItem.Item, "giveItem", out _);
                    RequireRange(context, giveItem.Count, 1, 999, "giveItem count");
                    break;
                case RemoveItemEffect removeItem:
                    RequireItem(context, removeItem.Item, "removeItem", out _);
                    RequireRange(context, removeItem.Count, 1, 999, "removeItem count");
                    break;
                case GiveGoldEffect gold:
                    RequireRange(context, gold.Amount, 0, int.MaxValue, "giveGold amount");
                    break;
                case GrantXpEffect xp:
                    RequireRange(context, xp.Amount, 0, int.MaxValue, "grantXp amount");
                    break;
                case RestoreManaEffect mana:
                    RequireRange(context, mana.Amount, 0, int.MaxValue, "restoreMana amount");
                    break;
                case HealEffect heal:
                    RequireNonNegativeDice(context, heal.Dice, "heal dice");
                    break;
                case DamageEffect damage:
                    RequireNonNegativeDice(context, damage.Dice, "damage dice");
                    break;
                case SavingThrowEffect save:
                    RequireRange(context, save.Dc, 1, 30, "savingThrow dc");
                    ValidateEffects(context, save.OnSuccess);
                    ValidateEffects(context, save.OnFail);
                    break;
                case SkillCheckEffect check:
                    ValidateCheck(context, check.Check);
                    ValidateEffects(context, check.OnSuccess);
                    ValidateEffects(context, check.OnFail);
                    break;
                case MessageEffect message:
                    RequireText(context, message.Text, "message text");
                    break;
                case MoveToEffect moveTo:
                    RequireRoom(context, moveTo.Room, "moveTo");
                    break;
                case StartDialogueEffect dialogue:
                    if (!content.Dialogues.ContainsKey(dialogue.Dialogue))
                    {
                        Error(context, $"startDialogue references unknown dialogue '{dialogue.Dialogue}'.");
                    }

                    break;
                case StartCombatEffect combat:
                    if (!content.Encounters.ContainsKey(combat.Encounter))
                    {
                        Error(context, $"startCombat references unknown encounter '{combat.Encounter}'.");
                    }

                    break;
                case IfEffect ifEffect:
                    if (ifEffect.Conditions.Count == 0)
                    {
                        Error(context, "'if' effect needs at least one condition.");
                    }

                    ValidateConditions(context, ifEffect.Conditions);
                    ValidateEffects(context, ifEffect.Then);
                    ValidateEffects(context, ifEffect.Else);
                    break;
            }
        }

        private void ReadFlag(string context, string flag)
        {
            if (RequireText(context, flag, "flag"))
            {
                _flagsRead.TryAdd(flag, context);
            }
        }

        private void WriteCounter(string context, string counter)
        {
            if (RequireText(context, counter, "counter"))
            {
                _countersWritten.Add(counter);
            }
        }

        private bool RequireText(string context, string? value, string field)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            Error(context, $"{field} must not be blank.");
            return false;
        }

        private void RequireAliases(string context, IReadOnlyList<string> aliases)
        {
            if (aliases.Any(string.IsNullOrWhiteSpace))
            {
                Error(context, "aliases must not contain blank entries.");
            }
        }

        private void RequireRange(string context, int value, int min, int max, string field)
        {
            if (value < min || value > max)
            {
                Error(context, max == int.MaxValue
                    ? $"{field} must be at least {min} (was {value})."
                    : $"{field} must be between {min} and {max} (was {value}).");
            }
        }

        private void RequireBound(string context, int? min, int? max, int? equalTo, string kind)
        {
            if (min is null && max is null && equalTo is null)
            {
                Error(context, $"'{kind}' condition needs at least one of min, max, or equals.");
            }
        }

        private DiceExpression? RequireDice(string context, string dice, string field)
        {
            if (DiceExpression.TryParse(dice, out var expression, out var error))
            {
                return expression;
            }

            Error(context, $"{field}: {error}");
            return null;
        }

        private void RequireNonNegativeDice(string context, string dice, string field)
        {
            if (RequireDice(context, dice, field) is { Min: < 0 })
            {
                Error(context, $"{field} must never roll below 0.");
            }
        }

        private void RequireRoom(string context, string roomId, string field)
        {
            if (!content.Rooms.ContainsKey(roomId))
            {
                Error(context, $"{field} references unknown room '{roomId}'.");
            }
        }

        private bool RequireItem(string context, string itemId, string field, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ItemDef? item)
        {
            if (content.Items.TryGetValue(itemId, out item))
            {
                return true;
            }

            Error(context, $"{field} references unknown item '{itemId}'.");
            return false;
        }

        private void RequireSkill(string context, string skillId, string field)
        {
            if (!content.Skills.ContainsKey(skillId))
            {
                Error(context, $"{field} references unknown skill '{skillId}'.");
            }
        }

        private void RequireStatus(string context, string statusId, string field)
        {
            if (!content.StatusEffects.ContainsKey(statusId))
            {
                Error(context, $"{field} references unknown status effect '{statusId}'.");
            }
        }

        private void RequireOptionalStatus(string context, string? statusId)
        {
            if (statusId is not null)
            {
                RequireStatus(context, statusId, "resolution.status");
            }
        }

        private void RequireFaction(string context, string factionId)
        {
            if (!content.Factions.ContainsKey(factionId))
            {
                Error(context, $"references unknown faction '{factionId}'.");
            }
        }

        private bool RequireQuest(string context, string questId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out QuestDef? quest)
        {
            if (content.Quests.TryGetValue(questId, out quest))
            {
                return true;
            }

            Error(context, $"references unknown quest '{questId}'.");
            return false;
        }

        private void RequireNode(string context, HashSet<string> nodeIds, string? nodeId, string field)
        {
            if (nodeId is null || !nodeIds.Contains(nodeId))
            {
                Error(context, $"{field} references unknown node '{nodeId ?? "(none)"}'.");
            }
        }
    }
}
