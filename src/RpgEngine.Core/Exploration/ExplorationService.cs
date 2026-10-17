using System.Globalization;
using RpgEngine.Core.Commands;
using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Events;
using RpgEngine.Core.Views;
using RpgEngine.Core.World;

namespace RpgEngine.Core.Exploration;

/// <summary>
/// Zork-style exploration: describing rooms, building the numbered action menu, and resolving
/// movement, inspection, items, talking, and data-defined feature interactions.
/// </summary>
public sealed class ExplorationService
{
    private static readonly HashSet<string> RoomWords = new(StringComparer.Ordinal) { "room", "around", "here" };

    private readonly GameContent _content;
    private readonly DiceRoller _dice;
    private readonly CheckRunner _checks;
    private readonly EffectApplier _effects;
    private readonly TextFormatter _text;

    public ExplorationService(GameContent content, DiceRoller dice, CheckRunner checks, EffectApplier effects, TextFormatter text)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(dice);
        ArgumentNullException.ThrowIfNull(checks);
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(text);
        _content = content;
        _dice = dice;
        _checks = checks;
        _effects = effects;
        _text = text;
    }

    /// <summary>Moves the player into a room: describes it, runs entry effects, and starts its encounter if armed.</summary>
    public void EnterRoom(string roomId, GameState state, TurnLog log)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(log);
        var room = _content.Rooms[roomId];
        state.CurrentRoomId = room.Id;
        var firstVisit = state.World.MarkVisited(room.Id);

        log.Add(new RoomDescribedEvent(Describe(room, state, full: firstVisit), Entered: true));
        if (firstVisit)
        {
            _effects.Apply(room.OnFirstEnter, state, log);
        }

        _effects.Apply(room.OnEnter, state, log);

        if (room.Encounter is { } encounter && ConditionEvaluator.AllPass(encounter.Conditions, state))
        {
            log.Request(new StartCombatTransition(encounter.Id));
        }
    }

    public RoomView Describe(GameState state, bool full)
    {
        ArgumentNullException.ThrowIfNull(state);
        return Describe(CurrentRoom(state), state, full);
    }

    public IReadOnlyList<MenuAction> GetActions(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var room = CurrentRoom(state);
        var actions = new List<MenuAction>();

        foreach (var npc in VisibleNpcs(room, state))
        {
            actions.Add(new MenuAction($"Talk to {npc.Name}", MenuActionKind.Talk, new ParsedCommand(CommandVerb.Talk, "talk", npc.Name, ObjectId: npc.Id)));
        }

        foreach (var feature in VisibleFeatures(room, state))
        {
            // "inspect" interactions run as part of Examine, so they get no menu entry of their own.
            foreach (var group in AvailableGroups(room, feature, state).Where(g => g.Verb != "inspect"))
            {
                actions.Add(new MenuAction(group.Label, MenuActionKind.Interact, new ParsedCommand(CommandVerb.Interact, group.Verb, group.Target, ObjectId: feature.Id)));
            }
        }

        foreach (var itemId in state.World.ItemsIn(room.Id).Distinct(StringComparer.Ordinal))
        {
            var item = _content.Items[itemId];
            actions.Add(new MenuAction($"Take {item.Name}", MenuActionKind.Take, new ParsedCommand(CommandVerb.Take, "take", item.Name, ObjectId: item.Id)));
        }

        foreach (var feature in VisibleFeatures(room, state))
        {
            actions.Add(new MenuAction($"Examine {feature.Name}", MenuActionKind.Examine, new ParsedCommand(CommandVerb.Inspect, "examine", feature.Name, ObjectId: feature.Id)));
        }

        foreach (var exit in VisibleExits(room, state))
        {
            var label = $"Go {exit.Exit.Direction.ToString().ToLowerInvariant()}";
            if (exit.IsOpen && state.World.HasVisited(exit.Exit.Target))
            {
                label += $" to {_content.Rooms[exit.Exit.Target].Name}";
            }

            actions.Add(new MenuAction(label, MenuActionKind.Move, new ParsedCommand(CommandVerb.Go, "go", Direction: exit.Exit.Direction)));
        }

        return actions;
    }

    public void Execute(ParsedCommand command, GameState state, TurnLog log)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(log);
        var room = CurrentRoom(state);

        switch (command.Verb)
        {
            case CommandVerb.Empty:
                break;
            case CommandVerb.Look:
                log.Add(new RoomDescribedEvent(Describe(room, state, full: true), Entered: false));
                break;
            case CommandVerb.Go:
                Go(command, room, state, log);
                break;
            case CommandVerb.Inspect:
                Inspect(command, room, state, log);
                break;
            case CommandVerb.Take:
                Take(command, room, state, log);
                break;
            case CommandVerb.Drop:
                Drop(command, room, state, log);
                break;
            case CommandVerb.Use:
                Use(command, room, state, log);
                break;
            case CommandVerb.Equip:
                Equip(command, state, log);
                break;
            case CommandVerb.Talk:
                Talk(command, room, state, log);
                break;
            case CommandVerb.Interact:
                if (!TryInteract(command.Word, command.Target, command.ObjectId, room, state, log))
                {
                    Say(log, command.Target is null
                        ? $"I don't understand '{command.Word}'. Type 'help' for commands."
                        : $"You can't {command.Word} that here.");
                }

                break;
            default:
                throw new InvalidOperationException($"'{command.Verb}' is handled by the game session, not exploration.");
        }
    }

    private RoomDef CurrentRoom(GameState state) => _content.Rooms[state.CurrentRoomId];

    private RoomView Describe(RoomDef room, GameState state, bool full)
    {
        var paragraphs = new List<string> { Format(full || room.Revisit is null ? room.Description : room.Revisit, state) };
        paragraphs.AddRange(room.Variants.Where(v => ConditionEvaluator.AllPass(v.Conditions, state)).Select(v => Format(v.Text, state)));

        var ambient = room.Ambient.Count == 0 ? null : Format(room.Ambient[_dice.Pick(0, room.Ambient.Count)], state);

        var exits = VisibleExits(room, state)
            .Select(exit => new ExitView(
                exit.Exit.Direction,
                exit.Exit.Description is null ? null : Format(exit.Exit.Description, state),
                !exit.IsOpen,
                state.World.HasVisited(exit.Exit.Target) ? _content.Rooms[exit.Exit.Target].Name : null))
            .ToList();

        return new RoomView(
            room.Id,
            room.Name,
            paragraphs,
            ambient,
            exits,
            VisibleFeatures(room, state).Select(feature => feature.Name).ToList(),
            state.World.ItemsIn(room.Id).Select(id => _content.Items[id].Name).ToList(),
            VisibleNpcs(room, state).Select(npc => npc.Name).ToList());
    }

    private void Go(ParsedCommand command, RoomDef room, GameState state, TurnLog log)
    {
        if (command.Direction is not { } direction)
        {
            Say(log, command.Target is null ? "Go where?" : $"You can't go '{command.Target}'.");
            return;
        }

        var exit = room.Exits.FirstOrDefault(e => e.Direction == direction);
        if (exit is null)
        {
            Say(log, "You can't go that way.");
            return;
        }

        if (!ConditionEvaluator.AllPass(exit.Conditions, state))
        {
            if (exit.BlockedText is { } blocked)
            {
                log.Add(new NarrationEvent(TextStyle.Narration, Format(blocked, state)));
            }
            else
            {
                Say(log, "You can't go that way.");
            }

            return;
        }

        log.Request(new MoveToTransition(exit.Target));
    }

    private void Inspect(ParsedCommand command, RoomDef room, GameState state, TurnLog log)
    {
        if (command.ObjectId is { } objectId)
        {
            var feature = VisibleFeatures(room, state).FirstOrDefault(f => f.Id == objectId);
            if (feature is null)
            {
                Say(log, "You don't see that here.");
                return;
            }

            InspectFeature(feature, room, state, log);
            return;
        }

        if (command.Target is not { } target)
        {
            Say(log, "Examine what?");
            return;
        }

        if (RoomWords.Contains(target))
        {
            log.Add(new RoomDescribedEvent(Describe(room, state, full: true), Entered: false));
            return;
        }

        var things = new List<(IReadOnlyList<string> Names, Action Describe)>();
        things.AddRange(VisibleFeatures(room, state).Select(feature => (Names(feature), (Action)(() => InspectFeature(feature, room, state, log)))));
        things.AddRange(VisibleNpcs(room, state).Select(npc => (Names(npc), (Action)(() => log.Add(new NarrationEvent(TextStyle.Narration, Format(npc.Description, state)))))));
        var itemIds = state.World.ItemsIn(room.Id).Concat(state.Player.Inventory.Entries.Select(e => e.ItemId)).Distinct(StringComparer.Ordinal);
        things.AddRange(itemIds.Select(id => _content.Items[id]).Select(item => (Names(item), (Action)(() => InspectItem(item, log)))));

        var matches = TargetMatcher.Best(target, things, thing => thing.Names);
        switch (matches.Count)
        {
            case 0:
                Say(log, $"You see no {target} here.");
                break;
            case 1:
                matches[0].Describe();
                break;
            default:
                Say(log, $"Which do you mean: {string.Join(" or ", matches.Select(m => m.Names[0]))}?");
                break;
        }
    }

    private void InspectFeature(FeatureDef feature, RoomDef room, GameState state, TurnLog log)
    {
        log.Add(new NarrationEvent(TextStyle.Narration, Format(feature.Description, state)));
        var inspect = AvailableGroups(room, feature, state).FirstOrDefault(g => g.Verb == "inspect");
        if (inspect is not null)
        {
            RunInteraction(room, inspect, state, log);
        }
    }

    private void InspectItem(ItemDef item, TurnLog log)
    {
        log.Add(new NarrationEvent(TextStyle.Narration, item.Description));
        if (item.Weapon is { } weapon)
        {
            var traits = (weapon.Finesse ? ", finesse" : string.Empty) + (weapon.Ranged ? ", ranged" : string.Empty);
            Say(log, $"Weapon: {weapon.Damage} {weapon.DamageType.ToString().ToLowerInvariant()} damage{traits}.");
        }

        if (item.Armor is { } armor)
        {
            var dex = armor.MaxDexBonus switch
            {
                null => " + DEX",
                0 => string.Empty,
                var cap => $" + DEX (max {cap})",
            };
            Say(log, $"Armor: AC {armor.BaseAc}{dex}.");
        }
    }

    private void Take(ParsedCommand command, RoomDef room, GameState state, TurnLog log)
    {
        var roomItems = state.World.ItemsIn(room.Id).Distinct(StringComparer.Ordinal).Select(id => _content.Items[id]).ToList();

        if (command.ObjectId is { } objectId && roomItems.FirstOrDefault(item => item.Id == objectId) is { } exact)
        {
            TakeItem(exact, room, state, log);
            return;
        }

        if (command.Target is not { } target)
        {
            Say(log, "Take what?");
            return;
        }

        var matches = TargetMatcher.Best(target, roomItems, Names);
        if (matches.Count == 1)
        {
            TakeItem(matches[0], room, state, log);
            return;
        }

        if (matches.Count > 1)
        {
            Say(log, $"Which do you mean: {string.Join(" or ", matches.Select(m => m.Name))}?");
            return;
        }

        if (TryInteract("take", target, null, room, state, log))
        {
            return;
        }

        if (TargetMatcher.Best(target, VisibleFeatures(room, state), Names) is [var feature, ..])
        {
            Say(log, $"You can't take the {feature.Name}.");
            return;
        }

        Say(log, $"You see no {target} here.");
    }

    private static void TakeItem(ItemDef item, RoomDef room, GameState state, TurnLog log)
    {
        state.World.RemoveItemFromRoom(room.Id, item.Id);
        state.Player.Inventory.Add(item.Id);
        log.Add(new ItemGainedEvent(item, 1));
    }

    private void Drop(ParsedCommand command, RoomDef room, GameState state, TurnLog log)
    {
        if (!TryFindCarried(command.Target, "Drop what?", state, log, out var item))
        {
            return;
        }

        if (item.Type == ItemType.Quest)
        {
            Say(log, $"You'd better hold on to the {item.Name}.");
            return;
        }

        var player = state.Player;
        player.Inventory.Remove(item.Id);
        if (!player.Inventory.Contains(item.Id))
        {
            if (player.EquippedWeaponId == item.Id)
            {
                player.EquippedWeaponId = null;
            }

            if (player.EquippedArmorId == item.Id)
            {
                player.EquippedArmorId = null;
            }
        }

        state.World.AddItemToRoom(room.Id, item.Id);
        Say(log, $"You drop the {item.Name}.");
    }

    private void Use(ParsedCommand command, RoomDef room, GameState state, TurnLog log)
    {
        if (command.Target is not { } target)
        {
            Say(log, "Use what?");
            return;
        }

        var carried = CarriedItems(state);
        var matches = TargetMatcher.Best(target, carried, Names);
        if (matches.Count == 0)
        {
            // "use lever" can mean a feature interaction rather than an item.
            if (!TryInteract("use", target, null, room, state, log))
            {
                Say(log, $"You don't have any {target}.");
            }

            return;
        }

        if (matches.Count > 1)
        {
            Say(log, $"Which do you mean: {string.Join(" or ", matches.Select(m => m.Name))}?");
            return;
        }

        var item = matches[0];
        if (command.Secondary is { } on)
        {
            UseItemOn(item, on, room, state, log);
            return;
        }

        if (item.Use is not { } use)
        {
            Say(log, $"You can't use the {item.Name} on its own.");
            return;
        }

        Say(log, $"You use the {item.Name}.");
        _effects.Apply(use.Effects, state, log);
        if (use.Consume)
        {
            state.Player.Inventory.Remove(item.Id);
        }
    }

    // "use key on door": finds a "use" interaction on the feature whose target names the item.
    private void UseItemOn(ItemDef item, string on, RoomDef room, GameState state, TurnLog log)
    {
        var features = TargetMatcher.Best(on, VisibleFeatures(room, state), Names);
        if (features.Count != 1)
        {
            Say(log, features.Count == 0 ? $"You see no {on} here." : $"Which do you mean: {string.Join(" or ", features.Select(f => f.Name))}?");
            return;
        }

        var group = AvailableGroups(room, features[0], state)
            .FirstOrDefault(g => g.Verb == "use" && g.Target is not null && TargetMatcher.Score(g.Target, Names(item)) > TargetMatcher.NoMatch);

        if (group is null)
        {
            Say(log, "Nothing happens.");
            return;
        }

        RunInteraction(room, group, state, log);
    }

    private void Equip(ParsedCommand command, GameState state, TurnLog log)
    {
        if (!TryFindCarried(command.Target, "Equip what?", state, log, out var item))
        {
            return;
        }

        switch (item.Type)
        {
            case ItemType.Weapon:
                state.Player.EquippedWeaponId = item.Id;
                Say(log, $"You ready the {item.Name}.");
                break;
            case ItemType.Armor:
                state.Player.EquippedArmorId = item.Id;
                Say(log, $"You don the {item.Name}.");
                break;
            default:
                Say(log, $"You can't equip the {item.Name}.");
                break;
        }
    }

    private void Talk(ParsedCommand command, RoomDef room, GameState state, TurnLog log)
    {
        var npcs = VisibleNpcs(room, state).ToList();
        IReadOnlyList<NpcDef> matches = command.ObjectId is { } objectId
            ? npcs.Where(npc => npc.Id == objectId).ToList()
            : command.Target is { } target ? TargetMatcher.Best(target, npcs, Names) : npcs;

        switch (matches.Count)
        {
            case 0:
                Say(log, npcs.Count == 0 ? "There is no one here to talk to." : $"You see no {command.Target} here.");
                break;
            case 1:
                log.Request(new StartDialogueTransition(matches[0].Dialogue));
                break;
            default:
                Say(log, $"Talk to whom: {string.Join(" or ", matches.Select(npc => npc.Name))}?");
                break;
        }
    }

    private bool TryInteract(string verb, string? target, string? objectId, RoomDef room, GameState state, TurnLog log)
    {
        var groups = VisibleFeatures(room, state)
            .SelectMany(feature => AvailableGroups(room, feature, state))
            .Where(group => group.Verb == verb)
            .ToList();

        if (objectId is not null)
        {
            var exact = groups.FirstOrDefault(g => g.Feature.Id == objectId && g.Target == target);
            if (exact is null)
            {
                return false;
            }

            RunInteraction(room, exact, state, log);
            return true;
        }

        if (groups.Count == 0)
        {
            return false;
        }

        if (target is null)
        {
            if (groups.Count == 1)
            {
                RunInteraction(room, groups[0], state, log);
            }
            else
            {
                Say(log, $"{Capitalize(verb)} what? ({string.Join(", ", groups.Select(g => g.Target ?? g.Feature.Name))})");
            }

            return true;
        }

        var matches = TargetMatcher.Best(target, groups, g => g.Target is null ? Names(g.Feature) : [g.Target]);
        if (matches.Count == 1)
        {
            RunInteraction(room, matches[0], state, log);
            return true;
        }

        if (matches.Count > 1)
        {
            Say(log, $"Which do you mean: {string.Join(" or ", matches.Select(g => g.Target ?? g.Feature.Name))}?");
            return true;
        }

        // "press door" when the door's interactions need a sub-target (sun, moon, skull).
        var subTargets = groups
            .Where(g => g.Target is not null && TargetMatcher.Score(target, Names(g.Feature)) > TargetMatcher.NoMatch)
            .Select(g => g.Target!)
            .ToList();

        if (subTargets.Count > 0)
        {
            Say(log, $"{Capitalize(verb)} which: {string.Join(", ", subTargets)}?");
            return true;
        }

        return false;
    }

    private void RunInteraction(RoomDef room, InteractionGroup group, GameState state, TurnLog log)
    {
        var member = FirstAvailable(room, group, state)!;
        var interaction = member.Definition;
        if (interaction.Once)
        {
            state.World.MarkUsed(OnceKey(room, group.Feature, member.Index));
        }

        if (interaction.Text is { } text)
        {
            log.Add(new NarrationEvent(TextStyle.Narration, Format(text, state)));
        }

        _effects.Apply(interaction.Effects, state, log);

        if (interaction.Check is { } check)
        {
            var result = _checks.Run(check, state);
            log.Add(new CheckRolledEvent(result));
            var outcome = result.Success ? interaction.Success : interaction.Failure;
            if (outcome?.Text is { } outcomeText)
            {
                log.Add(new NarrationEvent(result.Success ? TextStyle.Success : TextStyle.Failure, Format(outcomeText, state)));
            }

            _effects.Apply(outcome?.Effects ?? [], state, log);
        }
    }

    private IEnumerable<InteractionGroup> AvailableGroups(RoomDef room, FeatureDef feature, GameState state) =>
        feature.Interactions
            .Select((definition, index) => new InteractionMember(definition, index))
            .GroupBy(member => (member.Definition.Verb, member.Definition.Target))
            .Select(group => new InteractionGroup(feature, group.Key.Verb, group.Key.Target, group.ToList()))
            .Where(group => FirstAvailable(room, group, state) is not null)
            .Select(group => group with { Label = LabelFor(room, group, state) });

    private InteractionMember? FirstAvailable(RoomDef room, InteractionGroup group, GameState state) =>
        group.Members.FirstOrDefault(member =>
            ConditionEvaluator.AllPass(member.Definition.Conditions, state)
            && !(member.Definition.Once && state.World.IsUsed(OnceKey(room, group.Feature, member.Index))));

    private string LabelFor(RoomDef room, InteractionGroup group, GameState state) =>
        FirstAvailable(room, group, state)?.Definition.Label
        ?? group.Members.Select(member => member.Definition.Label).FirstOrDefault(label => label is not null)
        ?? Capitalize($"{group.Verb} {group.Target ?? group.Feature.Name}");

    private static string OnceKey(RoomDef room, FeatureDef feature, int index) => $"interaction:{room.Id}:{feature.Id}:{index}";

    private static IEnumerable<FeatureDef> VisibleFeatures(RoomDef room, GameState state) =>
        room.Features.Where(feature => ConditionEvaluator.AllPass(feature.Conditions, state));

    private static IEnumerable<NpcDef> VisibleNpcs(RoomDef room, GameState state) =>
        room.Npcs.Where(npc => ConditionEvaluator.AllPass(npc.Conditions, state));

    private static IEnumerable<(ExitDef Exit, bool IsOpen)> VisibleExits(RoomDef room, GameState state) =>
        room.Exits
            .Select(exit => (Exit: exit, IsOpen: ConditionEvaluator.AllPass(exit.Conditions, state)))
            .Where(exit => exit.IsOpen || exit.Exit.BlockedText is not null);

    private List<ItemDef> CarriedItems(GameState state) =>
        state.Player.Inventory.Entries.Select(entry => _content.Items[entry.ItemId]).ToList();

    private bool TryFindCarried(string? target, string prompt, GameState state, TurnLog log, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ItemDef? item)
    {
        item = null;
        if (target is null)
        {
            Say(log, prompt);
            return false;
        }

        var matches = TargetMatcher.Best(target, CarriedItems(state), Names);
        if (matches.Count != 1)
        {
            Say(log, matches.Count == 0 ? $"You don't have any {target}." : $"Which do you mean: {string.Join(" or ", matches.Select(m => m.Name))}?");
            return false;
        }

        item = matches[0];
        return true;
    }

    private string Format(string text, GameState state) => _text.Format(text, state.Player);

    private static IReadOnlyList<string> Names(FeatureDef feature) => [feature.Name, .. feature.Aliases, feature.Id];

    private static IReadOnlyList<string> Names(NpcDef npc) => [npc.Name, .. npc.Aliases, npc.Id];

    private static IReadOnlyList<string> Names(ItemDef item) => [item.Name, .. item.Aliases, item.Id];

    private static void Say(TurnLog log, string text) => log.Add(new NarrationEvent(TextStyle.System, text));

    private static string Capitalize(string text) =>
        text.Length == 0 ? text : char.ToUpper(text[0], CultureInfo.InvariantCulture) + text[1..];

    private sealed record InteractionMember(InteractionDef Definition, int Index);

    private sealed record InteractionGroup(FeatureDef Feature, string Verb, string? Target, IReadOnlyList<InteractionMember> Members)
    {
        public string Label { get; init; } = string.Empty;
    }
}
