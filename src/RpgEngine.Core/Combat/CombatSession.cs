using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Events;
using RpgEngine.Core.Views;
using RpgEngine.Core.World;

namespace RpgEngine.Core.Combat;

/// <summary>
/// A turn-based fight. After every call, <see cref="CurrentActor"/> is the next combatant who must
/// act (start-of-turn effects already applied), or <see cref="Outcome"/> is decided. The UI calls
/// <see cref="Act"/> on the player's turn and <see cref="RunEnemyTurn"/> otherwise.
/// </summary>
public sealed class CombatSession
{
    private readonly GameContent _content;
    private readonly DiceRoller _dice;
    private readonly CheckRunner _checks;
    private readonly EffectApplier _effects;
    private readonly GameState _state;
    private readonly List<Combatant> _order = [];
    private readonly HashSet<int> _triedParley = [];
    private int _turnIndex = -1;
    private bool _finished;

    internal CombatSession(
        GameContent content,
        DiceRoller dice,
        CheckRunner checks,
        EffectApplier effects,
        GameState state,
        EncounterDef encounter,
        PlayerCombatant player,
        IReadOnlyList<MonsterCombatant> monsters)
    {
        _content = content;
        _dice = dice;
        _checks = checks;
        _effects = effects;
        _state = state;
        Encounter = encounter;
        Player = player;
        _order.Add(player);
        _order.AddRange(monsters);
    }

    public EncounterDef Encounter { get; }

    public PlayerCombatant Player { get; }

    /// <summary>All combatants in initiative order.</summary>
    public IReadOnlyList<Combatant> TurnOrder => _order;

    public int Round { get; private set; }

    public CombatOutcome Outcome { get; private set; }

    public bool IsOver => Outcome != CombatOutcome.Ongoing;

    public Combatant CurrentActor => _order[_turnIndex];

    public bool IsPlayerTurn => !IsOver && CurrentActor == Player;

    public IReadOnlyList<Combatant> ActiveEnemies => _order.Where(c => c.Side == Side.Enemies && c.IsActive).ToList();

    public bool CanFlee => Encounter.CanFlee;

    public IReadOnlyList<CombatantView> Snapshot() => _order
        .Select(c => new CombatantView(c.Id, c.Name, c == Player, c.CurrentHp, c.MaxHp, c.ArmorClass, c.Statuses.Select(s => s.Definition.Name).ToList(), c.IsActive))
        .ToList();

    public IReadOnlyList<SpellOptionView> SpellOptions()
    {
        var character = Player.Character;
        return character.SpellIds.Select(id => _content.Spells[id]).Select(spell => spell.Cost switch
        {
            { UsesPerEncounter: { } max } => new SpellOptionView(spell, character.GetEncounterUses(spell.Id) > 0, $"{character.GetEncounterUses(spell.Id)}/{max} uses"),
            { Mana: > 0 } cost => new SpellOptionView(spell, character.CurrentMana >= cost.Mana, $"{cost.Mana} MP"),
            _ => new SpellOptionView(spell, true, "free"),
        }).ToList();
    }

    public IReadOnlyList<ItemDef> UsableItems() => Player.Character.Inventory.Entries
        .Select(entry => _content.Items[entry.ItemId])
        .Where(item => item.Use is { InCombat: true })
        .ToList();

    public IReadOnlyList<ParleyOptionView> ParleyOptions()
    {
        if (Encounter.Parley is not { } parley)
        {
            return [];
        }

        return parley.Options
            .Select((option, index) => (option, index))
            .Where(x => !_triedParley.Contains(x.index) && ConditionEvaluator.AllPass(x.option.Conditions, _state))
            .Select(x => new ParleyOptionView(x.index, x.option.Label, _checks.Label(x.option.Check), x.option.Check.Dc, _checks.ModeFor(x.option.Check, _state)))
            .ToList();
    }

    /// <summary>Performs the player's action, then advances to the next combatant who must act.</summary>
    public void Act(PlayerAction action, TurnLog log)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(log);
        if (!IsPlayerTurn)
        {
            throw new InvalidOperationException("It is not the player's turn.");
        }

        switch (action)
        {
            case AttackAction attack:
                WeaponAttack(RequireEnemy(attack.TargetId), null, null, log);
                break;
            case CastAction cast:
                Cast(cast, log);
                break;
            case UseItemAction use:
                UseItem(use, log);
                break;
            case DefendAction:
                ApplyStatus(Player, _content.StatusEffects[_content.Rules.DefendStatus], log);
                break;
            case ParleyAction parley:
                Parley(parley.OptionIndex, log);
                break;
            case FleeAction:
                Flee(log);
                break;
            default:
                throw new NotSupportedException($"Unsupported action '{action.GetType().Name}'.");
        }

        if (DecideOutcome(log))
        {
            return;
        }

        EndOfTurn(Player, log);
        AdvanceToNextActor(log);
    }

    /// <summary>Lets the current monster act (flee or attack the weakest party member), then advances.</summary>
    public void RunEnemyTurn(TurnLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        if (IsOver || CurrentActor is not MonsterCombatant monster)
        {
            throw new InvalidOperationException("It is not an enemy's turn.");
        }

        var ai = monster.Definition.Ai;
        if (ai.FleeBelowHpPercent > 0 && monster.CurrentHp * 100 < monster.MaxHp * ai.FleeBelowHpPercent)
        {
            monster.HasFled = true;
            log.Add(new CombatantFledEvent(monster.Name, false));
        }
        else
        {
            var target = _order.Where(c => c.Side == Side.Party && c.IsActive).OrderBy(c => c.CurrentHp).First();
            var attack = PickAttack(monster.Definition.Attacks);
            var hit = ResolveAttack(monster, target, attack.Name, attack.ToHit, DiceExpression.Parse(attack.Damage), attack.DamageType, null, log);
            if (hit && attack.OnHit is { } onHit && target.IsAlive)
            {
                ApplyOnHit(target, onHit, log);
            }
        }

        if (DecideOutcome(log))
        {
            return;
        }

        EndOfTurn(monster, log);
        AdvanceToNextActor(log);
    }

    internal void Start(string? intro, TurnLog log)
    {
        log.Add(new CombatStartedEvent(intro, Snapshot()));

        var rolls = _order.Select((combatant, index) =>
        {
            var roll = _dice.RollDie(20);
            var modifier = combatant.Abilities.Modifier(Characters.Ability.Dex);
            combatant.Initiative = roll + modifier;
            return (combatant, index, entry: new InitiativeEntry(combatant.Name, combatant == Player, roll, modifier, combatant.Initiative));
        }).ToList();

        var sorted = rolls
            .OrderByDescending(x => x.combatant.Initiative)
            .ThenByDescending(x => x.combatant.Abilities.Dex)
            .ThenBy(x => x.combatant.Side == Side.Party ? 0 : 1)
            .ThenBy(x => x.index)
            .ToList();

        _order.Clear();
        _order.AddRange(sorted.Select(x => x.combatant));
        log.Add(new InitiativeRolledEvent(sorted.Select(x => x.entry).ToList()));

        Round = 1;
        AdvanceToNextActor(log);
    }

    private void AdvanceToNextActor(TurnLog log)
    {
        while (!DecideOutcome(log))
        {
            _turnIndex++;
            if (_turnIndex >= _order.Count)
            {
                _turnIndex = 0;
                Round++;
            }

            var actor = _order[_turnIndex];
            if (!actor.IsActive)
            {
                continue;
            }

            log.Add(new TurnStartedEvent(actor.Name, actor == Player, Round));
            if (StartOfTurn(actor, log))
            {
                return;
            }

            if (actor.IsActive)
            {
                EndOfTurn(actor, log);
            }
        }
    }

    // Applies damage-over-time and skip-turn statuses; returns whether the actor can act.
    private bool StartOfTurn(Combatant actor, TurnLog log)
    {
        foreach (var status in actor.Statuses.ToList())
        {
            if (status.Definition.Modifiers.DamagePerTurn is { } dice)
            {
                var roll = _dice.Roll(dice);
                log.Add(new DiceRolledEvent(status.Definition.Name, roll));
                DealDamage(actor, roll.Total, null, log);
                if (!actor.IsAlive)
                {
                    return false;
                }
            }
        }

        var skip = actor.Statuses.FirstOrDefault(status => status.Definition.Modifiers.SkipTurn);
        if (skip is not null)
        {
            log.Add(new TurnSkippedEvent(actor.Name, skip.Definition.Name));
            return false;
        }

        return true;
    }

    private static void EndOfTurn(Combatant actor, TurnLog log)
    {
        foreach (var status in actor.Statuses.ToList())
        {
            if (status.AppliedDuringOwnTurn)
            {
                status.AppliedDuringOwnTurn = false;
                continue;
            }

            status.RemainingTurns--;
            if (status.RemainingTurns <= 0)
            {
                actor.RemoveStatus(status);
                log.Add(new StatusExpiredEvent(actor.Name, status.Definition.Name));
            }
        }
    }

    private bool DecideOutcome(TurnLog log)
    {
        if (Outcome == CombatOutcome.Ongoing)
        {
            if (!Player.IsAlive)
            {
                Outcome = CombatOutcome.Defeat;
            }
            else if (ActiveEnemies.Count == 0)
            {
                Outcome = CombatOutcome.Victory;
            }
            else
            {
                return false;
            }
        }

        Finish(log);
        return true;
    }

    private void Finish(TurnLog log)
    {
        if (_finished)
        {
            return;
        }

        _finished = true;
        log.Add(new CombatEndedEvent(Outcome));
        var character = Player.Character;

        switch (Outcome)
        {
            case CombatOutcome.Victory:
                var defeated = _order.OfType<MonsterCombatant>().Where(m => !m.IsAlive).ToList();
                var xp = defeated.Sum(m => m.Definition.Xp);
                if (xp > 0)
                {
                    // Before loot and the end-of-fight recharge, so a technique learned now starts full.
                    Progression.GrantXp(character, _content, xp, log);
                }

                foreach (var loot in defeated.SelectMany(m => m.Definition.Loot))
                {
                    if (_dice.Pick(0, 100) < loot.Chance * 100)
                    {
                        character.Inventory.Add(loot.Item);
                        log.Add(new ItemGainedEvent(_content.Items[loot.Item], 1));
                    }
                }

                _effects.Apply(Encounter.OnVictory, _state, log);
                _effects.Apply(Encounter.OnResolved, _state, log);
                break;
            case CombatOutcome.Parleyed:
                _effects.Apply(Encounter.Parley!.OnSuccess, _state, log);
                _effects.Apply(Encounter.OnResolved, _state, log);
                break;
            case CombatOutcome.Fled:
                log.Request(new MoveToTransition(Encounter.FleeTo!));
                break;
        }

        character.RechargeEncounterUses(CombatRules.EncounterUses(character, _content));
    }

    private void WeaponAttack(Combatant target, DiceExpression? bonusDamage, string? attackName, TurnLog log)
    {
        var weapon = CombatRules.Weapon(Player.Character, _content);
        ResolveAttack(Player, target, attackName ?? weapon.Name, weapon.ToHitBonus, weapon.Damage, weapon.DamageType, bonusDamage, log);
    }

    // d20 + bonus vs AC. A natural 20 always hits and doubles damage dice; a natural 1 always misses.
    private bool ResolveAttack(
        Combatant attacker,
        Combatant target,
        string attackName,
        int bonus,
        DiceExpression damage,
        DamageType damageType,
        DiceExpression? bonusDamage,
        TurnLog log)
    {
        var roll = _dice.RollD20(attacker.AttackRollMode);
        var total = roll.Natural + bonus;
        var critical = roll.IsNatural20;
        var hit = critical || (!roll.IsNatural1 && total >= target.ArmorClass);
        log.Add(new AttackRolledEvent(attacker.Name, target.Name, attackName, roll, bonus, total, target.ArmorClass, hit, critical));

        if (!hit)
        {
            return false;
        }

        var damageRoll = _dice.Roll(damage, critical);
        log.Add(new DiceRolledEvent($"{attackName} damage", damageRoll));
        var amount = damageRoll.Total;

        if (bonusDamage is not null)
        {
            var bonusRoll = _dice.Roll(bonusDamage, critical);
            log.Add(new DiceRolledEvent("Bonus damage", bonusRoll));
            amount += bonusRoll.Total;
        }

        DealDamage(target, Math.Max(0, amount), damageType, log);
        return true;
    }

    private void DealDamage(Combatant target, int amount, DamageType? damageType, TurnLog log)
    {
        var modifier = DamageModifier.None;
        if (damageType is { } type)
        {
            if (target.Immunities.Contains(type))
            {
                amount = 0;
                modifier = DamageModifier.Immune;
            }
            else if (target.Resistances.Contains(type))
            {
                amount /= 2;
                modifier = DamageModifier.Resisted;
            }
            else if (target.Vulnerabilities.Contains(type))
            {
                amount *= 2;
                modifier = DamageModifier.Vulnerable;
            }
        }

        target.ApplyDamage(amount);
        log.Add(new DamageTakenEvent(target.Name, target == Player, amount, damageType, modifier, target.CurrentHp, target.MaxHp));
        if (!target.IsAlive)
        {
            log.Add(new CombatantDefeatedEvent(target.Name, target == Player));
        }
    }

    private void ApplyOnHit(Combatant target, OnHitDef onHit, TurnLog log)
    {
        if (onHit.Save is { } save)
        {
            var result = _checks.SavingThrow(target, save.Ability, save.Dc);
            log.Add(new CheckRolledEvent(result));
            if (result.Success)
            {
                return;
            }
        }

        ApplyStatus(target, _content.StatusEffects[onHit.Status], log);
    }

    private void ApplyStatus(Combatant target, StatusEffectDef status, TurnLog log)
    {
        target.AddOrRefreshStatus(status, duringOwnTurn: target == CurrentActor);
        log.Add(new StatusAppliedEvent(target.Name, status.Name));
    }

    private void Cast(CastAction cast, TurnLog log)
    {
        var option = SpellOptions().FirstOrDefault(o => o.Spell.Id == cast.SpellId)
            ?? throw new ArgumentException($"The player does not know '{cast.SpellId}'.", nameof(cast));
        if (!option.CanCast)
        {
            throw new ArgumentException($"'{option.Spell.Name}' cannot be used right now.", nameof(cast));
        }

        var spell = option.Spell;
        var target = spell.Target == SpellTarget.Enemy ? RequireEnemy(cast.TargetId) : null;
        var character = Player.Character;

        if (spell.Cost.UsesPerEncounter is not null)
        {
            character.TryUseEncounterCharge(spell.Id);
        }
        else if (spell.Cost.Mana > 0)
        {
            character.TrySpendMana(spell.Cost.Mana);
            log.Add(new ManaChangedEvent(-spell.Cost.Mana, character.CurrentMana, character.MaxMana));
        }

        log.Add(new SpellCastEvent(Player.Name, spell.Name, spell.Kind));
        Resolve(spell.Name, spell.Resolution, target, log);
    }

    // Shared by spells, techniques, and enemy-targeted items. Attack bonuses and default save DCs
    // use the player's casting ability, so a thrown flask lands like a spell would.
    private void Resolve(string name, SpellResolutionDef resolution, Combatant? target, TurnLog log)
    {
        var castingModifier = Player.Abilities.Modifier(CombatRules.CastingAbility(Player.Character, _content));

        switch (resolution)
        {
            case AutoHitResolutionDef autoHit:
                var autoRoll = _dice.Roll(autoHit.Damage);
                log.Add(new DiceRolledEvent($"{name} damage", autoRoll));
                DealDamage(target!, autoRoll.Total, autoHit.DamageType, log);
                break;
            case SpellAttackResolutionDef attack:
                var hit = ResolveAttack(Player, target!, name, Player.ProficiencyBonus + castingModifier, DiceExpression.Parse(attack.Damage), attack.DamageType, null, log);
                if (hit && attack.Status is { } attackStatus && target!.IsAlive)
                {
                    ApplyStatus(target, _content.StatusEffects[attackStatus], log);
                }

                break;
            case WeaponAttackResolutionDef weapon:
                WeaponAttack(target!, weapon.BonusDamage is null ? null : DiceExpression.Parse(weapon.BonusDamage), name, log);
                break;
            case SaveResolutionDef save:
                ResolveSave(name, save, target!, Player.ProficiencyBonus, castingModifier, log);
                break;
            case EffectsResolutionDef effects:
                _effects.Apply(effects.Effects, _state, log);
                break;
        }
    }

    private void ResolveSave(string name, SaveResolutionDef save, Combatant target, int proficiency, int castingModifier, TurnLog log)
    {
        var dc = save.Dc ?? 8 + proficiency + castingModifier;
        var result = _checks.SavingThrow(target, save.Ability, dc);
        log.Add(new CheckRolledEvent(result));

        var roll = _dice.Roll(save.Damage);
        log.Add(new DiceRolledEvent($"{name} damage", roll));
        var amount = result.Success ? (save.HalfOnSave ? roll.Total / 2 : 0) : roll.Total;
        DealDamage(target, amount, save.DamageType, log);

        if (!result.Success && save.Status is { } status && target.IsAlive)
        {
            ApplyStatus(target, _content.StatusEffects[status], log);
        }
    }

    private void UseItem(UseItemAction action, TurnLog log)
    {
        var item = UsableItems().FirstOrDefault(i => i.Id == action.ItemId)
            ?? throw new ArgumentException($"'{action.ItemId}' cannot be used in combat.", nameof(action));
        var use = item.Use!;
        var target = use.Target == SpellTarget.Enemy ? RequireEnemy(action.TargetId) : null;

        if (target is null)
        {
            log.Add(new NarrationEvent(TextStyle.System, $"You use the {item.Name}."));
            _effects.Apply(use.Effects, _state, log);
        }
        else
        {
            log.Add(new NarrationEvent(TextStyle.System, $"You use the {item.Name} on {target.Name}."));
            Resolve(item.Name, use.Resolution!, target, log);
        }

        if (use.Consume)
        {
            Player.Character.Inventory.Remove(item.Id);
        }
    }

    private void Parley(int optionIndex, TurnLog log)
    {
        var option = ParleyOptions().FirstOrDefault(o => o.Index == optionIndex)
            ?? throw new ArgumentException("That parley option is not available.", nameof(optionIndex));

        _triedParley.Add(option.Index);
        var parley = Encounter.Parley!;
        var result = _checks.Run(parley.Options[option.Index].Check, _state);
        log.Add(new CheckRolledEvent(result));

        if (result.Success)
        {
            Outcome = CombatOutcome.Parleyed;
        }
        else
        {
            _effects.Apply(parley.OnFailure, _state, log);
        }
    }

    private void Flee(TurnLog log)
    {
        if (!CanFlee)
        {
            throw new InvalidOperationException("There is no escape from this fight.");
        }

        var result = _checks.Run(_content.Rules.Flee, _state);
        log.Add(new CheckRolledEvent(result));
        if (result.Success)
        {
            Player.HasFled = true;
            log.Add(new CombatantFledEvent(Player.Name, true));
            Outcome = CombatOutcome.Fled;
        }
        else
        {
            log.Add(new NarrationEvent(TextStyle.Failure, "You fail to break away!"));
        }
    }

    private MonsterAttackDef PickAttack(IReadOnlyList<MonsterAttackDef> attacks)
    {
        if (attacks.Count == 1)
        {
            return attacks[0];
        }

        var roll = _dice.Pick(0, attacks.Sum(a => a.Weight));
        foreach (var attack in attacks)
        {
            if (roll < attack.Weight)
            {
                return attack;
            }

            roll -= attack.Weight;
        }

        return attacks[^1];
    }

    private Combatant RequireEnemy(string? targetId) =>
        ActiveEnemies.FirstOrDefault(enemy => enemy.Id == targetId)
        ?? throw new ArgumentException($"'{targetId}' is not a valid target.", nameof(targetId));
}
