using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Events;
using RpgEngine.Core.Views;
using RpgEngine.Core.World;

namespace RpgEngine.Core.Dialogue;

/// <summary>
/// Runs branching dialogue trees: picks the entry node from world state, applies node and
/// choice effects, resolves skill-check choices, and permanently locks one-shot choices.
/// </summary>
public sealed class DialogueRunner
{
    private readonly GameContent _content;
    private readonly CheckRunner _checks;
    private readonly EffectApplier _effects;
    private readonly TextFormatter _text;

    public DialogueRunner(GameContent content, CheckRunner checks, EffectApplier effects, TextFormatter text)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(checks);
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(text);
        _content = content;
        _checks = checks;
        _effects = effects;
        _text = text;
    }

    public DialogueSession Start(string dialogueId, GameState state, TurnLog log)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(log);
        var dialogue = _content.Dialogues[dialogueId];
        var session = new DialogueSession(dialogue);
        var entry = dialogue.Entries.First(e => ConditionEvaluator.AllPass(e.Conditions, state));
        EnterNode(session, entry.Node, state, log);
        return session;
    }

    public IReadOnlyList<DialogueChoiceView> GetChoices(DialogueSession session, GameState state)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(state);
        if (session.CurrentNode is not { } node)
        {
            return [];
        }

        return VisibleChoices(session.Dialogue, node, state)
            .Select(choice => new DialogueChoiceView(
                choice.Index,
                Format(choice.Definition.Text, state),
                choice.Definition.Check is { } check ? _checks.Label(check) : null,
                choice.Definition.Check?.Dc,
                choice.Definition.Check is { } modeCheck ? _checks.ModeFor(modeCheck, state) : Dice.RollMode.Normal))
            .ToList();
    }

    /// <summary>Picks a choice by its <see cref="DialogueChoiceView.Index"/>.</summary>
    public void Choose(DialogueSession session, int choiceIndex, GameState state, TurnLog log)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(log);

        var node = session.CurrentNode ?? throw new InvalidOperationException("The conversation is over.");
        var visible = VisibleChoices(session.Dialogue, node, state).FirstOrDefault(choice => choice.Index == choiceIndex)
            ?? throw new ArgumentOutOfRangeException(nameof(choiceIndex), choiceIndex, "That choice is not available.");
        var choice = visible.Definition;
        var key = ChoiceKey(session.Dialogue, node, visible.Index);

        if (choice.Once)
        {
            state.World.MarkUsed(key);
        }

        _effects.Apply(choice.Effects, state, log);
        if (log.HasPendingTransition)
        {
            session.CurrentNode = null;
            return;
        }

        if (choice.Check is { } check)
        {
            if (!choice.Retry)
            {
                state.World.MarkUsed(CheckKey(key));
            }

            var result = _checks.Run(check, state);
            log.Add(new CheckRolledEvent(result));
            EnterNode(session, result.Success ? choice.SuccessNode! : choice.FailureNode!, state, log);
        }
        else if (choice.Next is { } next)
        {
            EnterNode(session, next, state, log);
        }
        else
        {
            session.CurrentNode = null;
        }
    }

    // Shows nodes, following automatic "next" links, until one offers choices or the conversation ends.
    private void EnterNode(DialogueSession session, string nodeId, GameState state, TurnLog log)
    {
        var dialogue = session.Dialogue;
        string? currentId = nodeId;

        while (currentId is not null)
        {
            var node = dialogue.Nodes.First(n => n.Id == currentId);
            log.Add(new DialogueLineEvent(dialogue.Speaker, Format(node.Text, state)));
            _effects.Apply(node.Effects, state, log);

            if (log.HasPendingTransition)
            {
                break;
            }

            if (VisibleChoices(dialogue, node, state).Count > 0)
            {
                session.CurrentNode = node;
                return;
            }

            currentId = node.Next;
        }

        session.CurrentNode = null;
    }

    private List<VisibleChoice> VisibleChoices(DialogueDef dialogue, DialogueNodeDef node, GameState state) =>
        node.Choices
            .Select((choice, index) => new VisibleChoice(choice, index))
            .Where(choice =>
            {
                var key = ChoiceKey(dialogue, node, choice.Index);
                return ConditionEvaluator.AllPass(choice.Definition.Conditions, state)
                    && !(choice.Definition.Once && state.World.IsUsed(key))
                    && !(choice.Definition.Check is not null && !choice.Definition.Retry && state.World.IsUsed(CheckKey(key)));
            })
            .ToList();

    private static string ChoiceKey(DialogueDef dialogue, DialogueNodeDef node, int index) => $"dialogue:{dialogue.Id}:{node.Id}:{index}";

    private static string CheckKey(string choiceKey) => choiceKey + ":check";

    private string Format(string text, GameState state) => _text.Format(text, state.Player);

    private sealed record VisibleChoice(DialogueChoiceDef Definition, int Index);
}
