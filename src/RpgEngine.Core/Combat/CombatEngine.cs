using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Dice;
using RpgEngine.Core.Events;
using RpgEngine.Core.World;

namespace RpgEngine.Core.Combat;

/// <summary>Creates combat sessions: builds combatants from the bestiary, recharges abilities, and rolls initiative.</summary>
public sealed class CombatEngine
{
    private readonly GameContent _content;
    private readonly DiceRoller _dice;
    private readonly CheckRunner _checks;
    private readonly EffectApplier _effects;
    private readonly TextFormatter _text;

    public CombatEngine(GameContent content, DiceRoller dice, CheckRunner checks, EffectApplier effects, TextFormatter text)
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

    public CombatSession Begin(string encounterId, GameState state, TurnLog log)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(log);
        var encounter = _content.Encounters[encounterId];

        var character = state.Player;
        character.RechargeEncounterUses(CombatRules.EncounterUses(character, _content));
        var player = new PlayerCombatant(character, CombatRules.ArmorClass(character, _content));

        var session = new CombatSession(_content, _dice, _checks, _effects, state, encounter, player, CreateMonsters(encounter));
        session.Start(encounter.Intro is null ? null : _text.Format(encounter.Intro, character), log);
        return session;
    }

    private List<MonsterCombatant> CreateMonsters(EncounterDef encounter)
    {
        var totals = encounter.Enemies
            .GroupBy(enemy => enemy.Monster, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(enemy => enemy.Count), StringComparer.Ordinal);
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var monsters = new List<MonsterCombatant>();

        foreach (var enemy in encounter.Enemies)
        {
            var definition = _content.Monsters[enemy.Monster];
            for (var i = 0; i < enemy.Count; i++)
            {
                var number = seen[enemy.Monster] = seen.GetValueOrDefault(enemy.Monster) + 1;
                var name = totals[enemy.Monster] > 1 ? $"{definition.Name} {(char)('A' + number - 1)}" : definition.Name;
                monsters.Add(new MonsterCombatant($"{definition.Id}#{number}", name, definition, RollHitPoints(definition)));
            }
        }

        return monsters;
    }

    private int RollHitPoints(MonsterDef monster)
    {
        var hitDice = DiceExpression.Parse(monster.Hp);
        var hp = _content.Rules.MonsterHp == MonsterHpMode.Roll ? _dice.Roll(hitDice).Total : (int)Math.Floor(hitDice.Average);
        return Math.Max(1, hp);
    }
}
