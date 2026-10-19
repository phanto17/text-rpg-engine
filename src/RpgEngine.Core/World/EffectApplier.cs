using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Events;

namespace RpgEngine.Core.World;

/// <summary>
/// Applies data-defined effects to the game state in order. State changes happen immediately;
/// mode changes (move, dialogue, combat, end) are queued on the <see cref="TurnLog"/>.
/// </summary>
public sealed class EffectApplier
{
    private readonly GameContent _content;
    private readonly DiceRoller _dice;
    private readonly CheckRunner _checks;
    private readonly TextFormatter _text;

    public EffectApplier(GameContent content, DiceRoller dice, CheckRunner checks, TextFormatter text)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(dice);
        ArgumentNullException.ThrowIfNull(checks);
        ArgumentNullException.ThrowIfNull(text);
        _content = content;
        _dice = dice;
        _checks = checks;
        _text = text;
    }

    /// <summary>
    /// Runs the effects in order. <paramref name="applyStatus"/> puts a status on the player; combat
    /// supplies it, because statuses live on combatants. Without it, <c>applyStatus</c> does nothing.
    /// </summary>
    public void Apply(IReadOnlyList<EffectDef> effects, GameState state, TurnLog log, Action<StatusEffectDef>? applyStatus = null)
    {
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(log);

        foreach (var effect in effects)
        {
            Apply(effect, state, log, applyStatus);
        }
    }

    private void Apply(EffectDef effect, GameState state, TurnLog log, Action<StatusEffectDef>? applyStatus)
    {
        var world = state.World;
        var player = state.Player;

        switch (effect)
        {
            case SetFlagEffect e:
                world.SetFlag(e.Flag);
                break;
            case ClearFlagEffect e:
                world.ClearFlag(e.Flag);
                break;
            case SetCounterEffect e:
                world.SetCounter(e.Counter, e.Value);
                break;
            case AddCounterEffect e:
                world.AddCounter(e.Counter, e.Amount);
                break;
            case ChangeReputationEffect e:
                log.Add(new ReputationChangedEvent(_content.Factions[e.Faction], e.Amount, world.ChangeReputation(e.Faction, e.Amount)));
                break;
            case SetQuestStageEffect e:
                if (world.AdvanceQuest(e.Quest, e.Stage))
                {
                    var quest = _content.Quests[e.Quest];
                    log.Add(new QuestUpdatedEvent(quest, quest.Stages.First(stage => stage.Stage == e.Stage)));
                }

                break;
            case GiveItemEffect e:
                player.Inventory.Add(e.Item, e.Count);
                log.Add(new ItemGainedEvent(_content.Items[e.Item], e.Count));
                break;
            case RemoveItemEffect e:
                RemoveItem(e, state, log);
                break;
            case GiveGoldEffect e:
                player.AddGold(e.Amount);
                log.Add(new GoldGainedEvent(e.Amount, player.Gold));
                break;
            case GrantXpEffect e:
                Progression.GrantXp(player, _content, e.Amount, log);
                break;
            case HealEffect e:
                var healing = _dice.Roll(e.Dice);
                log.Add(new DiceRolledEvent("Healing", healing));
                log.Add(new HealedEvent(player.Name, true, player.Heal(healing.Total), player.CurrentHp, player.MaxHp));
                break;
            case DamageEffect e:
                var damage = _dice.Roll(e.Dice);
                log.Add(new DiceRolledEvent("Damage", damage));
                log.Add(new DamageTakenEvent(player.Name, true, player.TakeDamage(damage.Total), e.DamageType, DamageModifier.None, player.CurrentHp, player.MaxHp));
                break;
            case RestoreManaEffect e:
                log.Add(new ManaChangedEvent(player.RestoreMana(e.Amount), player.CurrentMana, player.MaxMana));
                break;
            case RestEffect:
                var hpRestored = player.Heal(player.MaxHp - player.CurrentHp);
                var manaRestored = player.RestoreMana(player.MaxMana - player.CurrentMana);
                log.Add(new RestedEvent(hpRestored, manaRestored, player.CurrentHp, player.MaxHp, player.CurrentMana, player.MaxMana));
                break;
            case SavingThrowEffect e:
                var save = _checks.SavingThrow(player, e.Ability, e.Dc);
                log.Add(new CheckRolledEvent(save));
                Apply(save.Success ? e.OnSuccess : e.OnFail, state, log, applyStatus);
                break;
            case SkillCheckEffect e:
                var check = _checks.Run(e.Check, state);
                log.Add(new CheckRolledEvent(check));
                Apply(check.Success ? e.OnSuccess : e.OnFail, state, log, applyStatus);
                break;
            case MessageEffect e:
                log.Add(new NarrationEvent(e.Style, _text.Format(e.Text, player)));
                break;
            case MoveToEffect e:
                log.Request(new MoveToTransition(e.Room));
                break;
            case StartDialogueEffect e:
                log.Request(new StartDialogueTransition(e.Dialogue));
                break;
            case StartCombatEffect e:
                log.Request(new StartCombatTransition(e.Encounter));
                break;
            case StartShopEffect e:
                log.Request(new StartShopTransition(e.Shop));
                break;
            case ApplyStatusEffect e:
                applyStatus?.Invoke(_content.StatusEffects[e.Status]);
                break;
            case EndGameEffect:
                log.Request(new EndGameTransition());
                break;
            case IfEffect e:
                Apply(ConditionEvaluator.AllPass(e.Conditions, state) ? e.Then : e.Else, state, log, applyStatus);
                break;
            default:
                throw new NotSupportedException($"Unsupported effect type '{effect.GetType().Name}'.");
        }
    }

    // Removes as many as the player has (up to Count) and unequips gear that is no longer carried.
    private void RemoveItem(RemoveItemEffect effect, GameState state, TurnLog log)
    {
        var player = state.Player;
        var count = Math.Min(effect.Count, player.Inventory.CountOf(effect.Item));
        if (count == 0)
        {
            return;
        }

        player.Inventory.Remove(effect.Item, count);
        if (!player.Inventory.Contains(effect.Item))
        {
            if (player.EquippedWeaponId == effect.Item)
            {
                player.EquippedWeaponId = null;
            }

            if (player.EquippedArmorId == effect.Item)
            {
                player.EquippedArmorId = null;
            }
        }

        log.Add(new ItemLostEvent(_content.Items[effect.Item], count));
    }
}
