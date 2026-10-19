# CLAUDE.md: text-rpg-engine

This is the contributor guide for this repo: architecture rules, coding standards, the JSON content schemas, and the Git workflow. **Read it before changing code or data.** When a rule or schema changes, update this file in the same commit.

The project is a data-driven, narrative text RPG engine in C# (.NET 10), inspired by D&D 5e (SRD), Zork, and Wizardry. The engine knows nothing about any specific story: every room, item, monster, line of dialogue, class, and rule constant is loaded from `Data/`.

---

## Commands

```bash
dotnet build                                   # must report 0 warnings, 0 errors
dotnet test                                    # must be all green
dotnet run --project src/RpgEngine.CLI         # play
dotnet run --project src/RpgEngine.CLI -- --seed 42 --no-anim   # deterministic run, no dice animation
dotnet test --collect:"XPlat Code Coverage"    # Cobertura report under tests/RpgEngine.Tests/TestResults/
```

CLI flags (Milestone 1):

| Flag | Effect |
|---|---|
| `--seed <n>` | Fixed RNG seed, so the run is reproducible |
| `--no-anim` | Skip dice animations |
| `--data <dir>` | Load content from another folder (default: `Data/` next to the executable) |
| `--saves <dir>` | Save folder (default: `./Saves`) |
| `--help` | Print usage |

Exit codes: `0` normal, `1` content failed validation (every error is listed), `2` bad arguments.

The SDK is pinned by `global.json` to 10.0.401 with `rollForward: latestFeature`. Package versions live only in `Directory.Packages.props` (Central Package Management). Shared build settings live in `Directory.Build.props`.

---

## Solution layout

```
RpgEngine.slnx
Data/                      ALL game content (JSON). Copied to CLI + test output (PreserveNewest).
Docs/LORE.md               World, philosophy, and narrative bible. Read it before touching Data/.
src/RpgEngine.Core/        Engine: models, dice, checks, world state, content loading, combat, saves.
src/RpgEngine.CLI/         Spectre.Console UI: screens, rendering, hybrid input loop.
tests/RpgEngine.Tests/     xUnit tests for Core, content validation, and CLI playthroughs.
.github/workflows/ci.yml   CI: restore, Release build, and tests on ubuntu-latest and windows-latest.
```

Core folders (namespace = `RpgEngine.Core.<Folder>`): `Dice`, `Characters`, `Checks`, `World`, `Content` (`Defs/` for content records), `Commands`, `Exploration`, `Dialogue`, `Combat`, `Persistence`, `Engine`.

CLI layout:
- `CliHost`: argument parsing and content-error reporting.
- `GameApp`: the title-to-play loop, dispatching on `GameMode`.
- `Rendering/`: `Theme` (every color), `EventRenderer`, `Bars`, `RollText`, `StatusBar`, `DiceAnimator`.
- `Screens/`: Title, CharacterCreation (name, calling, burden, scores, skills, expertise, review), Exploration, Dialogue, Combat, Info (sheet, inventory, journal, help), End.
- `Input/`: `Prompter`, the line-based numbered menus. There are no arrow-key widgets, so every prompt can be scripted in tests.

---

## Golden rules (non-negotiable)

1. **No content in C#.** Story text, rooms, items, monsters, dialogue, classes, spells, status effects, and rule constants live in `Data/`. Only UI chrome (labels like "Inventory", "HP", "Attack") may be written in code.
2. **Core is UI-agnostic.** `RpgEngine.Core` never references Spectre.Console and never writes to the console. It returns `GameEvent`s (narration, check results, dice rolls, damage, item and quest changes, mode changes), and the CLI renders them. `ArchitectureTests` enforces this.
3. **All randomness goes through `IRandomSource`.** Never use `new Random()` or `Random.Shared` in Core. Use `Pcg32RandomSource(seed)` for gameplay and `FixedRandomSource` (scripted faces) in tests. Inject `TimeProvider` instead of reading `DateTime.Now`.
4. **Content text is plain text.** The CLI escapes it and styles it by role. Never put Spectre markup in JSON. Check badges such as `[Persuasion DC 14]` are generated from check data and never typed into text, so each DC is defined in one place.
5. **Fail fast on bad content.** `ContentValidator` collects *every* error (broken references, duplicate IDs, unknown properties, invalid dice) and reports them together at startup. The same validation runs as a unit test over the real `Data/`, so bad data fails `dotnet test`.
6. **Saves store state, never content.** Content is always reloaded from `Data/`.
7. **CLI screens take an injected `IAnsiConsole`** (never the static `AnsiConsole`), so they can be driven by `Spectre.Console.Testing.TestConsole`.

---

## Coding standards

- C# `latest` on `net10.0`, nullable reference types on, **warnings are errors**. Zero warnings is the bar.
- File-scoped namespaces. The namespace mirrors the folder. One public type per file, and the file is named after the type. The one exception is small closed polymorphic families (`ConditionDefs.cs`, `EffectDefs.cs`, `SpellResolutionDefs.cs`), which live together in one file.
- Classes are `sealed` by default. Immutable content definitions are `sealed record`s in `Content/Defs/`.
- Public APIs expose `IReadOnlyList<T>` and `IReadOnlyDictionary<K,V>`, not mutable collections.
- No static mutable state. Services take their dependencies through constructors.
- Naming: `PascalCase` for types and members, `_camelCase` for private fields, `camelCase` for locals and parameters.
- Errors: content problems raise `ContentValidationException` (with the full error list). Programmer errors use the standard `Argument*Exception`s.
- Comments explain *why*, not *what*. Keep methods small and intention-revealing.

---

## Testing standards

- xUnit with plain `Assert`. **No FluentAssertions**, because v8 changed to a commercial license.
- Test names follow `Method_Scenario_Expected`, e.g. `Roll_WithAdvantage_KeepsHigherDie`.
- Test folders mirror Core folders: `Dice/`, `Checks/`, `World/`, `Content/`, `Dialogue/`, `Combat/`, `Persistence/`, `Commands/`, `Playthrough/`, `Balance/`.
- Tests are fully deterministic. Use `FixedRandomSource` for exact outcomes and a fixed-seed `Pcg32RandomSource` for statistical tests, so no test can flake.
  - When scripting dice, count every draw: initiative (player first, then monsters in listed order), checks, damage, weighted attack picks (only for monsters with 2+ attacks), loot rolls, and **one ambient pick each time a room with `ambient` lines is described**.
  - `LuckyRandomSource` (every roll at maximum) keeps story-path tests robust to number tuning.
- Test helpers live in `tests/RpgEngine.Tests/TestSupport/`:
  - `TestContent`: an in-memory JSON content builder.
  - Fixtures: `ExplorationFixture`, `CombatFixture`, `SessionFixture`.
  - Harnesses: `TestWorld`, `ExplorationHarness`, `CombatHarness`, `CliHarness`.
- **Balance check** (`Balance/`): `BarrowHeartBalanceTests` plays the `barrow_heart` boss fight 2,000 times per class, using seeds 1 to 2000, the real `Data/`, the standard array, and the simple documented player in `BalancePolicy`.
  - Every class plays once without the Flask of Holy Water and once with it. Each class's no-flask win rate must stay inside its band.
  - After an intended tuning change, run `dotnet test --filter Balance --logger "console;verbosity=detailed"`. Read the printed table (win rate, average rounds, average HP left on wins) and move the bands in the same commit as the data change.
- CLI tests drive the real UI through `Spectre.Console.Testing.TestConsole` with scripted input lines. The CLI exposes internals to the test assembly only.
- Coverage target: 100% line coverage for `RpgEngine.Core.Dice` and `RpgEngine.Core.Checks`.
- Every new condition type, effect type, or schema field needs a test, and so does every validator rule.

---

## Content (JSON) reference

> These schemas are the contract Milestone 1 implements. Keep this section in sync with `Content/Defs/`.

### Conventions
- Every file in a content folder holds a **JSON array** of definitions. Files may be split or merged freely; the loader reads every `*.json` in the folder. `game.json` is the only single-object file.
- `id`s are `snake_case` and unique per content type. References use these IDs.
- Properties are camelCase. `//` comments and trailing commas are allowed.
- **Unknown properties are errors** (`JsonUnmappedMemberHandling.Disallow`), which catches typos.
- Flags and counters are free-form names. A flag or counter that is **checked but never set by any effect** is a validation error, which catches typos in flag names.
- File layout: `Rules/` and `World/` use the fixed file names shown below. `Items/`, `Bestiary/`, `Rooms/`, and `Dialogues/` load every `*.json` file directly inside them.
- Conditions and effects are polymorphic objects with a `"type"` discriminator, which may appear anywhere in the object.
- **Dice** fields take an expression: `"d20"`, `"2d6+3"`, `"4d6kh3"` (keep highest 3), `"2d20kl1"`, `"1d8+1d6+2"`, or a constant like `"19"`.
- **Abilities:** `str dex con int wis cha`.
- **Directions:** `north south east west up down in out`.
- **Damage types (SRD):** `acid bludgeoning cold fire force lightning necrotic piercing poison psychic radiant slashing thunder`.
- **Text:** plain strings, with paragraphs separated by `\n\n`. Supported tokens are `{player.name}`, `{player.class}`, and `{player.background}`.

### `Data/game.json`: manifest and rule constants
```json
{
  "title": "Barrowlight",
  "contentVersion": "0.1.0",
  "startRoom": "lichgate",
  "intro": "Rain needles the hillside...",
  "startingGold": 10,
  "rules": {
    "standardArray": [16, 14, 13, 12, 10, 8],
    "proficiencyBonusByLevel": [2, 2, 2, 2, 3],
    "xpThresholdsByLevel": [0, 100, 300, 900, 2700],
    "naturalCritsOnChecks": true,
    "monsterHp": "average",
    "flee": { "skill": "acrobatics", "dc": 10 },
    "defendStatus": "defending"
  },
  "epilogues": [
    { "conditions": [ { "type": "flag", "flag": "knight_laid_to_rest" } ], "text": "..." },
    { "text": "Fallback epilogue (last entry should be unconditional)." }
  ],
  "gameOverText": "Shown when the player dies."
}
```
- `monsterHp` is `average` (the floor of the hit-dice average) or `roll`.
- `xpThresholdsByLevel` (default `[0]`, meaning no leveling) is the total XP for each level: the first entry is level 1 and must be `0`, and the list must be strictly increasing. Its length is the maximum level. Every XP award (`grantXp` effects and combat victories) levels the character up as many times as the new total allows. Each level adds `max(1, hitDie / 2 + 1 + CON modifier)` to maximum and current HP, 2 mana for classes with `mana`, and any `spellsByLevel` grants for that level. Proficiency follows `proficiencyBonusByLevel`.
- `flee` is the check for escaping combat.
- `defendStatus` is the status that the Defend action applies.
- When `endGame` fires, the first epilogue whose conditions pass is shown.

### `Data/Rules/`
**`classes.json`**
```json
{ "id": "liminal_auditor", "name": "Liminal Auditor", "description": "...", "hitDie": 8,
  "abilityPriority": ["dex", "con", "cha", "wis", "int", "str"],
  "savingThrows": ["dex", "int"],
  "skillChoices": { "count": 4, "from": ["acrobatics", "deception", "insight", "intimidation", "investigation", "perception", "persuasion", "stealth"] },
  "expertiseCount": 2,
  "mana": null,
  "startingItems": [ { "item": "shortsword", "equip": true }, { "item": "leather_armor", "equip": true }, { "item": "healing_potion", "count": 2 } ],
  "spells": ["sneak_attack", "grave_salt_feint"] }
```
- Classes are the four Callings of `Docs/LORE.md`; the file order is the creation menu order.
- `spellsByLevel` (default `[]`): `[ { "level": 2, "spells": ["ash_brand"] } ]`. Each level must be reachable (2 to the maximum level), and a spell may be granted only once (never one the class already starts with).
- `abilityPriority` drives auto-assignment of the standard array.
- `mana` is `{ "base": 4, "ability": "int" }` or `null`. Max mana = base + ability modifier.

**`backgrounds.json`** (required, at least one): the protagonist's burden, chosen after the class.
```json
{ "id": "moor_bailiff", "name": "Moor Bailiff", "description": "...",
  "bonusSkill": "persuasion", "bonusGold": 10, "keepsakeItem": "elders_writ" }
```
- `bonusSkill` is granted on top of the class picks and is removed from the class skill picker. Every class must still have at least `skillChoices.count` skills left after any background's bonus skill is removed.
- `bonusGold` (default `0`) is added to `startingGold`.
- `keepsakeItem` starts in the inventory. Make keepsakes `type: quest` so they can't be dropped.

**`skills.json`**: `{ "id": "persuasion", "name": "Persuasion", "ability": "cha" }`. All 18 SRD skills are listed; checks may only reference skills defined here.

**`spells.json`**: spells and martial techniques share one schema.
```json
{ "id": "magic_missile", "name": "Chords of Unrest", "kind": "spell", "description": "...",
  "cost": { "mana": 2 }, "target": "enemy",
  "resolution": { "type": "autoHit", "damage": "3d4+3", "damageType": "force" } }
{ "id": "second_wind", "name": "Ash-Blood Second Wind", "kind": "technique", "description": "...",
  "cost": { "usesPerEncounter": 1 }, "target": "self",
  "resolution": { "type": "effects", "effects": [ { "type": "heal", "dice": "1d10+1" } ] } }
```
- `kind` is `spell` or `technique`.
- `target` is `enemy`, `self`, or `ally`.
- `cost` is `{ "mana": n }`, `{ "usesPerEncounter": n }`, or `{}` (free). **Per-encounter uses recharge automatically when any combat ends.** A `rest` effect (for example on a sanctuary brazier) restores HP and mana to their maximums.
- `resolution.type`:
  - `autoHit`: `damage`, `damageType`
  - `spellAttack`: d20 + prof + casting mod vs AC, then `damage`, `damageType`, and an optional `status` on a hit
  - `weaponAttack`: normal weapon attack plus `bonusDamage`
  - `save`: `ability`, `dc` (default 8 + prof + casting mod), `damage`, `damageType`, `halfOnSave`, and an optional `status` on a failed save
  - `effects`: `effects[]` run on the caster; `target` must be `self`
- Every other resolution type needs `target: "enemy"`. The casting ability is the class's `mana.ability`.

**`status_effects.json`**
```json
{ "id": "grave_chill", "name": "Grave Chill", "description": "...", "durationRounds": 1,
  "modifiers": { "attackRolls": "disadvantage", "acBonus": 0, "damagePerTurn": null, "skipTurn": false } }
```
- `attackRolls` is `normal`, `advantage`, or `disadvantage`.
- A status lasts for `durationRounds` of the affected creature's own turns. A status applied during the creature's own turn doesn't count that turn.
- `damagePerTurn` and `skipTurn` trigger at the start of each affected turn.

### `Data/World/`
- **`factions.json`**: `{ "id": "ashen_order", "name": "The Ashen Order", "description": "..." }`
- **`quests.json`**: `{ "id": "barrowlight", "name": "...", "stages": [ { "stage": 10, "journal": "..." } ] }`. Quest stages only move forward.

### `Data/Items/`
```json
{ "id": "longsword", "name": "Longsword", "aliases": ["sword"], "type": "weapon", "description": "...", "value": 15,
  "weapon": { "damage": "1d8", "damageType": "slashing", "finesse": false, "ranged": false } }
{ "id": "leather_armor", "name": "Leather Armor", "type": "armor", "description": "...", "value": 10,
  "armor": { "baseAc": 11, "maxDexBonus": null } }
{ "id": "healing_potion", "name": "Potion of Healing", "aliases": ["potion"], "type": "consumable", "description": "...",
  "value": 50,
  "use": { "inCombat": true, "consume": true, "effects": [ { "type": "heal", "dice": "2d4+2" } ] } }
{ "id": "holy_water", "name": "Flask of Holy Water", "aliases": ["holy water", "flask"], "type": "consumable", "description": "...",
  "value": 25,
  "use": { "inCombat": true, "consume": true, "target": "enemy",
           "resolution": { "type": "autoHit", "damage": "2d4", "damageType": "radiant" } } }
```
- `type` is `weapon`, `armor`, `consumable`, `key`, `quest`, or `misc`.
- AC = `baseAc` + DEX modifier, capped at `maxDexBonus` (`null` means no cap; `0` means heavy armor).
- `use.target` is `self` (the default) or `enemy`.
  - **`self`:** `use.effects` (required, non-empty) run on the user, in or out of combat.
  - **`enemy`:** the item can only be used in combat (`inCombat: true`) and needs a `use.resolution`. The player picks a target, and the resolution works exactly like a spell's: `autoHit`, `spellAttack`, or `save`. Attack bonuses and default save DCs use the player's casting ability. Resistances, vulnerabilities, and immunities apply, so radiant holy water deals double damage to a radiant-vulnerable undead. `use.effects` must be empty.

### `Data/Bestiary/`
```json
{ "id": "gravebound_knight", "name": "Ser Halric", "description": "...",
  "hp": "3d8+2", "ac": 13,
  "abilities": { "str": 15, "dex": 10, "con": 13, "int": 10, "wis": 11, "cha": 12 },
  "attacks": [
    { "name": "Rusted Longsword", "toHit": 3, "damage": "1d6+2", "damageType": "slashing", "weight": 2 },
    { "name": "Grave Touch", "toHit": 3, "damage": "1d4", "damageType": "necrotic", "weight": 1,
      "onHit": { "save": { "ability": "con", "dc": 11 }, "status": "grave_chill" } } ],
  "resistances": ["necrotic"], "vulnerabilities": ["radiant"], "immunities": ["poison"],
  "xp": 100,
  "ai": { "behavior": "aggressive", "fleeBelowHpPercent": 0 },
  "loot": [ { "item": "healing_potion", "chance": 0.5 } ] }
```
- Attacks are picked by `weight`.
- On a failed save, `onHit.status` is applied.

### `Data/Rooms/`
```json
{ "id": "ossuary_hall", "name": "The Ossuary Hall",
  "description": "First-visit text.", "revisit": "Shorter text for later visits.",
  "variants": [ { "conditions": [ { "type": "flag", "flag": "sigil_door_open" } ], "text": "The bronze door stands open." } ],
  "ambient": ["Water ticks somewhere in the dark."],
  "exits": [
    { "direction": "up", "target": "lichgate", "description": "Worn steps climb toward grey light." },
    { "direction": "east", "target": "inner_crypt",
      "conditions": [ { "type": "flag", "flag": "sigil_door_open" } ],
      "blockedText": "The bronze door is sealed." } ],
  "features": [
    { "id": "sigil_door", "name": "bronze door", "aliases": ["door", "sigils", "inscription"],
      "description": "Three raised sigils: a sun, a moon, a skull.", "conditions": [],
      "interactions": [
        { "verb": "read", "label": "Read the inscription", "text": "\"First the light that wakes the field...\"" },
        { "verb": "study", "label": "Study the inscription", "once": true,
          "check": { "skill": "religion", "dc": 13 },
          "success": { "text": "...", "effects": [ { "type": "setFlag", "flag": "knows_sigil_order" } ] },
          "failure": { "text": "The words blur." } },
        { "verb": "press", "target": "sun", "label": "Press the sun sigil",
          "conditions": [ { "type": "counter", "counter": "sigil_seq", "equals": 0 } ],
          "text": "The sun sinks into the bronze with a click.",
          "effects": [ { "type": "addCounter", "counter": "sigil_seq", "amount": 1 } ] },
        { "verb": "press", "target": "sun", "text": "A needle lances out!",
          "effects": [
            { "type": "savingThrow", "ability": "dex", "dc": 12,
              "onFail": [ { "type": "damage", "dice": "1d4", "damageType": "piercing" } ] },
            { "type": "setCounter", "counter": "sigil_seq", "value": 0 } ] } ] } ],
  "items": ["healing_potion"],
  "npcs": [
    { "id": "wren", "name": "Sister Wren", "aliases": ["wren", "nun"], "description": "...",
      "dialogue": "wren", "conditions": [ { "type": "notFlag", "flag": "wren_fled" } ] } ],
  "encounter": null,
  "onFirstEnter": [ { "type": "setQuestStage", "quest": "barrowlight", "stage": 20 } ],
  "onEnter": [] }
```
- **Description order:** `description` on the first visit and for `look`, `revisit` on later visits, then the text of every passing `variants` entry.
- **Entering a room:** the room is marked visited, then `onFirstEnter` runs (first visit only), then `onEnter` (every visit), then the room's encounter starts if its conditions pass.
- **Exits:** when an exit's conditions fail, it shows as blocked if it has `blockedText` and is hidden otherwise (a secret passage).
- **Features:** a feature with failing `conditions` is hidden. Interaction verbs must be one lowercase word. `inspect` shows the description and then runs any `inspect` interaction. Built-in verbs like `take` fall back to a matching interaction when no item matches.

**Interaction resolution:**
- Interactions sharing the same `(verb, target)` form a **group**.
- The group's `label` appears in the numbered menu if any member's conditions pass.
- When used, the **first member whose conditions pass runs**.
- `once: true` interactions are tracked in `WorldState` automatically.
- An interaction with a `check` runs `success` or `failure`, each of which is `{ text, effects }`.

**Encounter** (inline in a room, `null` when there is none):
```json
{ "id": "barrow_heart",
  "conditions": [ { "type": "notFlag", "flag": "barrow_heart_resolved" } ],
  "intro": "The sarcophagus lid grinds aside...",
  "enemies": [ { "monster": "gravebound_knight", "count": 1 }, { "monster": "barrow_thrall", "count": 1 } ],
  "canFlee": true, "fleeTo": "ossuary_hall",
  "parley": {
    "options": [
      { "label": "Tell him his watch is over", "check": { "skill": "persuasion", "dc": 13 } },
      { "label": "Speak the Rite of Rest", "conditions": [ { "type": "flag", "flag": "wren_taught_rite" } ],
        "check": { "skill": "religion", "dc": 10, "advantage": true } } ],
    "onSuccess": [ { "type": "setFlag", "flag": "knight_laid_to_rest" } ],
    "onFailure": [ { "type": "message", "text": "...", "style": "warning" } ] },
  "onVictory": [ { "type": "setFlag", "flag": "knight_destroyed" } ],
  "onResolved": [ { "type": "setFlag", "flag": "barrow_heart_resolved" } ] }
```
- Each parley option can be tried once per fight. A successful parley ends combat.
- `onResolved` runs after either a victory or a successful parley.

### `Data/Dialogues/`
```json
{ "id": "wren", "speaker": "Sister Wren",
  "entries": [
    { "conditions": [ { "type": "flag", "flag": "wren_trusts_player" } ], "node": "friend" },
    { "node": "first_meeting" } ],
  "nodes": [
    { "id": "first_meeting", "text": "\"Another grave-robber?\"",
      "effects": [ { "type": "setFlag", "flag": "met_wren" } ],
      "choices": [
        { "text": "Who are you?", "next": "who", "once": true },
        { "text": "I'm here to end what's below, not loot it.",
          "check": { "skill": "persuasion", "dc": 12, "advantageIf": [ { "type": "flag", "flag": "read_wren" } ] },
          "successNode": "trusts", "failureNode": "refuses", "retry": false },
        { "text": "Leave.", "end": true } ] } ] }
```
- `entries`: the first entry whose conditions pass picks the starting node. The last entry must be unconditional.
- Node `effects` run when the node is entered. A node either has `choices`, continues automatically with `next`, or (with neither) ends the conversation.
- Choice fields:
  - `conditions`: hides the choice when they fail.
  - `once`: the choice can be picked once.
  - `effects`: run when the choice is picked.
  - Exactly one of `next`, `end`, or `check` (with `successNode` and `failureNode`) decides what happens next.
  - `retry: false`: the check is locked forever after one attempt. That lock is itself a permanent consequence.

### Checks
`{ "skill": "persuasion" | "ability": "str", "dc": 14, "advantage": false, "advantageIf": [], "disadvantageIf": [] }`

- Total = d20 + ability modifier + proficiency bonus (doubled with expertise).
- **Meeting the DC succeeds.**
- When `rules.naturalCritsOnChecks` is on, a natural 20 always succeeds and a natural 1 always fails.
- Advantage and disadvantage roll 2d20 and keep the higher or lower; having both cancels out.

### Conditions (every entry in a list must pass)
| `type` | Fields |
|---|---|
| `flag` / `notFlag` | `flag` |
| `hasItem` / `lacksItem` | `item`, `count?` |
| `counter` | `counter`, `min?`, `max?`, `equals?` |
| `reputation` | `faction`, `min?`, `max?` |
| `questStage` | `quest`, `min?`, `max?`, `equals?` |
| `class` | `class` |
| `background` | `background` |
| `abilityScore` | `ability`, `min` |
| `visited` | `room` |
| `all` / `any` / `not` | `conditions` |

### Effects
| `type` | Fields |
|---|---|
| `setFlag` / `clearFlag` | `flag` |
| `setCounter` / `addCounter` | `counter`, `value` / `amount` |
| `changeReputation` | `faction`, `amount` |
| `setQuestStage` | `quest`, `stage` (never moves backwards) |
| `giveItem` / `removeItem` | `item`, `count?` |
| `giveGold` | `amount` |
| `grantXp` | `amount` |
| `heal` / `damage` | `dice`, `damageType?` |
| `restoreMana` | `amount` |
| `rest` | none; restores HP and mana to their maximums (a sanctuary rest) |
| `savingThrow` | `ability`, `dc`, `onSuccess[]`, `onFail[]` |
| `skillCheck` | `check` (a check object), `onSuccess[]`, `onFail[]` |
| `if` | `conditions[]`, `then[]`, `else[]` |
| `message` | `text`, `style` (`narration` `sensory` `dialogue` `system` `success` `failure` `warning`) |
| `moveTo` | `room` |
| `startDialogue` | `dialogue` |
| `startCombat` | `encounter` |
| `endGame` | none; shows the first matching epilogue |

### Save files (`Saves/<slot>.json`, gitignored)
Each save holds:
- `saveVersion` (currently `2`), `contentVersion`, `savedAtUtc`, `currentRoom`
- `player`: name, class, background, level, XP, abilities, proficient and expertise skills, HP and max HP, mana and max mana, gold, inventory, equipped
- `world`: flags, counters, reputation, questStages, visitedRooms, roomItems, onceUsed

A save from another format version is refused with a message naming its version (version 2 added `background`). Saving is allowed only during exploration. Per-encounter uses are never saved, because they are always full outside combat.

### Adding content
**Before adding or editing any JSON under `Data/`, read [`Docs/LORE.md`](Docs/LORE.md) to keep prose tone, philosophical themes, faction alignments, and flag/counter names consistent. Whenever a commit adds a new flag, counter, faction, class, or quest, update the registry in `Docs/LORE.md` in the same commit.**

1. Add or edit JSON under `Data/`.
2. Run `dotnet test`. The content validation test lists every broken reference or typo.
3. Run `dotnet run --project src/RpgEngine.CLI -- --seed 1` and play through it.
4. Commit with `data: ...`.

---

## Git workflow

- **Branch:** `main`. **Remote:** `origin`, the private GitHub repo `text-rpg-engine`. Never rewrite pushed history.
- **Gate:** create a commit **only after `dotnet build` (0 warnings, 0 errors) and `dotnet test` (all passing) succeed.** Never commit a red build. CI runs the same gate (Release configuration, Ubuntu and Windows) on every push and pull request to `main`.
- **Conventional Commits:** `<type>(<optional scope>): <imperative summary>`. Use lowercase, keep it to 72 characters or fewer, no trailing period. Add a body when the *why* isn't obvious.

| Type | Use for |
|---|---|
| `feat` | New engine or UI capability |
| `fix` | Bug fix |
| `data` | Content changes under `Data/` (rooms, dialogue, items, bestiary, rules) |
| `test` | Adding or changing tests only |
| `docs` | README, CLAUDE.md, comments-only changes |
| `refactor` | Code change with no behavior change |
| `chore` | Tooling, scaffolding, housekeeping |
| `build` | Build system, SDK, or package changes |

  Scopes (optional): `core`, `cli`, `tests`, `data`. Examples: `feat(core): add d20 dice expression parser`, `data: add sigil door puzzle`.
- Keep commits small and focused. Prefer keeping engine changes and content changes in separate commits.
- Never commit `Saves/`, `Logs/`, `*.log`, coverage output, `bin/`, `obj/`, or `.claude/`.
- **No AI attribution, ever.** Commit messages, tags, and PR titles and descriptions must **never** contain `Co-Authored-By:` trailers, "Generated with …" lines, AI or Claude mentions, or emoji badges. They must read as standard human-written Conventional Commits.
  - Automatic attribution is disabled in `.claude/settings.local.json` (`"includeCoAuthoredBy": false`, empty `attribution`).
  - In commit messages, call this file "the contributor guide" (e.g. `docs: update contributor guide`) so a history scan for AI mentions stays clean.
  - Before pushing, check the message with `git log -1 --format=%B`.

---

## Roadmap status
- [x] **Phase 1:** solution scaffold, tooling, docs, Git and GitHub.
- [x] **Milestone 1 (vertical slice):**
  - character creation
  - 3-room starter dungeon "Barrowlight": an NPC dialogue with skill checks and permanent consequences, an inspectable puzzle, and a turn-based combat encounter
  - save and load
  - full dice and skill-check test coverage
- [x] **CI:** GitHub Actions build and test on ubuntu-latest and windows-latest.
- [ ] **Milestone 2 (the Under-Barrow):**
  - [x] 2A: four Callings, Burdens (backgrounds) with keepsakes, the `background` condition, save format v2, prologue reactivity
  - [ ] 2B: sanctuary rests, leveling, shops
  - [ ] 2C: Act I content (the Weeping Stair and Nocturne)
- [ ] **Later:** parties, races, saving mid-combat, JSON Schema export for editor autocomplete.
