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

The SDK is pinned by `global.json` to 10.0.401 with `rollForward: latestFeature`. Package versions live only in `Directory.Packages.props` (Central Package Management). Shared build settings live in `Directory.Build.props`.

---

## Solution layout

```
RpgEngine.slnx
Data/                      ALL game content (JSON). Copied to CLI + test output (PreserveNewest).
src/RpgEngine.Core/        Engine: models, dice, checks, world state, content loading, combat, saves.
src/RpgEngine.CLI/         Spectre.Console UI: screens, rendering, hybrid input loop.
tests/RpgEngine.Tests/     xUnit tests for Core, content validation, and CLI playthroughs.
```

Core folders (namespace = `RpgEngine.Core.<Folder>`): `Dice`, `Characters`, `Checks`, `World`, `Content` (`Defs/` for content records), `Commands`, `Exploration`, `Dialogue`, `Combat`, `Persistence`, `Engine`.

CLI folders: `Rendering` (theme, bars, tables, dice animation), `Screens` (Title, CharacterCreation, Exploration, Dialogue, Combat, GameOver, Epilogue), `Input` (numbered-menu-or-text prompt).

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
- File-scoped namespaces. The namespace mirrors the folder. One public type per file, and the file is named after the type.
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
- Test folders mirror Core folders: `Dice/`, `Checks/`, `World/`, `Content/`, `Dialogue/`, `Combat/`, `Persistence/`, `Commands/`, `Playthrough/`.
- Tests are fully deterministic. Use `FixedRandomSource` for exact outcomes and a fixed-seed `Pcg32RandomSource` for statistical tests, so no test can flake.
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
- Conditions and effects are polymorphic objects with a `"type"` discriminator, which may appear anywhere in the object.
- **Dice** fields take an expression: `"d20"`, `"2d6+3"`, `"4d6kh3"` (keep highest 3), `"2d20kl1"`, `"1d8+1d6+2"`, or a constant like `"19"`.
- **Abilities:** `str dex con int wis cha`.
- **Directions:** `north south east west up down in out`.
- **Damage types (SRD):** `acid bludgeoning cold fire force lightning necrotic piercing poison psychic radiant slashing thunder`.
- **Text:** plain strings, with paragraphs separated by `\n\n`. Supported tokens are `{player.name}` and `{player.class}`.

### `Data/game.json`: manifest and rule constants
```json
{
  "title": "Barrowlight",
  "contentVersion": "0.1.0",
  "startRoom": "lichgate",
  "intro": "Rain needles the hillside...",
  "startingGold": 10,
  "rules": {
    "standardArray": [15, 14, 13, 12, 10, 8],
    "proficiencyBonusByLevel": [2, 2, 2, 2, 3],
    "naturalCritsOnChecks": true,
    "monsterHp": "average",
    "fleeDc": 12
  },
  "epilogues": [
    { "conditions": [ { "type": "flag", "flag": "knight_laid_to_rest" } ], "text": "..." },
    { "text": "Fallback epilogue (last entry should be unconditional)." }
  ]
}
```
`monsterHp` is `average` or `roll`. The first epilogue whose conditions pass is shown when `endGame` fires.

### `Data/Rules/`
**`classes.json`**
```json
{ "id": "rogue", "name": "Rogue", "description": "...", "hitDie": 8,
  "abilityPriority": ["dex", "con", "cha", "wis", "int", "str"],
  "savingThrows": ["dex", "int"],
  "skillChoices": { "count": 4, "from": ["acrobatics", "deception", "insight", "intimidation", "investigation", "perception", "persuasion", "stealth"] },
  "expertiseCount": 2,
  "mana": null,
  "startingItems": [ { "item": "shortsword", "equip": true }, { "item": "leather_armor", "equip": true }, { "item": "healing_potion", "count": 1 } ],
  "spells": ["sneak_attack"] }
```
- `abilityPriority` drives auto-assignment of the standard array.
- `mana` is `{ "base": 4, "ability": "int" }` or `null`. Max mana = base + ability modifier.

**`skills.json`**: `{ "id": "persuasion", "name": "Persuasion", "ability": "cha" }`. All 18 SRD skills are listed; checks may only reference skills defined here.

**`spells.json`**: spells and martial techniques share one schema.
```json
{ "id": "magic_missile", "name": "Magic Missile", "kind": "spell", "description": "...",
  "cost": { "mana": 2 }, "target": "enemy",
  "resolution": { "type": "autoHit", "damage": "3d4+3", "damageType": "force" } }
{ "id": "second_wind", "name": "Second Wind", "kind": "technique", "description": "...",
  "cost": { "usesPerEncounter": 1 }, "target": "self",
  "resolution": { "type": "effects", "effects": [ { "type": "heal", "dice": "1d10+1" } ] } }
```
- `kind` is `spell` or `technique`.
- `target` is `enemy`, `self`, or `ally`.
- `cost` is `{ "mana": n }`, `{ "usesPerEncounter": n }`, or `{}` (free). **Per-encounter uses recharge automatically when any combat ends.** Rests are not part of Milestone 1.
- `resolution.type`:
  - `autoHit`: `damage`, `damageType`
  - `spellAttack`: d20 + prof + casting mod vs AC, then `damage`, `damageType`
  - `weaponAttack`: normal weapon attack plus `bonusDamage`
  - `save`: `ability`, `dc`, `damage`, `halfOnSave`
  - `effects`: `effects[]`

**`status_effects.json`**
```json
{ "id": "chilled", "name": "Chilled", "description": "...", "durationRounds": 2,
  "modifiers": { "attackRolls": "disadvantage", "acBonus": 0, "damagePerTurn": null, "skipTurn": false } }
```
- `attackRolls` is `normal`, `advantage`, or `disadvantage`.
- Effects tick at the start of the affected creature's turn and count down at the end of it.

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
  "value": 50, "stackable": true,
  "use": { "inCombat": true, "consume": true, "effects": [ { "type": "heal", "dice": "2d4+2" } ] } }
```
- `type` is `weapon`, `armor`, `consumable`, `key`, `quest`, or `misc`.
- AC = `baseAc` + DEX modifier, capped at `maxDexBonus` (`null` means no cap; `0` means heavy armor).

### `Data/Bestiary/`
```json
{ "id": "gravebound_knight", "name": "The Gravebound Knight", "description": "...",
  "hp": "4d8+4", "ac": 15,
  "abilities": { "str": 15, "dex": 10, "con": 13, "int": 10, "wis": 11, "cha": 12 },
  "attacks": [
    { "name": "Rusted Longsword", "toHit": 4, "damage": "1d8+2", "damageType": "slashing", "weight": 2 },
    { "name": "Grave Touch", "toHit": 4, "damage": "1d4", "damageType": "necrotic", "weight": 1,
      "onHit": { "save": { "ability": "con", "dc": 12 }, "status": "chilled" } } ],
  "resistances": ["necrotic"], "vulnerabilities": ["radiant"], "immunities": [],
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
  "onEnter": [ { "type": "setQuestStage", "quest": "barrowlight", "stage": 20 } ] }
```
**Description order:** `description` on the first visit, then `revisit`, then the text of every passing `variants` entry.

**Interaction resolution:**
- Interactions sharing the same `(verb, target)` form a **group**.
- The group's `label` appears in the numbered menu if any member's conditions pass.
- When used, the **first member whose conditions pass runs**.
- `once: true` interactions are tracked in `WorldState` automatically.
- An interaction with a `check` runs `success` or `failure`, each of which is `{ text, effects }`.

**Encounter** (inline in a room, `null` when there is none):
```json
{ "id": "barrow_heart", "trigger": "onEnter",
  "conditions": [ { "type": "notFlag", "flag": "barrow_heart_resolved" } ],
  "intro": "The sarcophagus lid grinds aside...",
  "enemies": [ { "monster": "gravebound_knight", "count": 1 }, { "monster": "barrow_thrall", "count": 1 } ],
  "canFlee": true, "fleeTo": "ossuary_hall",
  "parley": {
    "options": [
      { "label": "Ask what binds him here", "check": { "skill": "persuasion", "dc": 15 } },
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
          "check": { "skill": "persuasion", "dc": 14, "advantageIf": [ { "type": "flag", "flag": "read_wren" } ] },
          "successNode": "trusts", "failureNode": "refuses", "retry": false },
        { "text": "Leave.", "end": true } ] } ] }
```
- `entries`: the first entry whose conditions pass picks the starting node.
- Node `effects` run when the node is entered.
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
| `applyStatus` | `status` |
| `savingThrow` / `skillCheck` | `ability` or `skill`, `dc`, `onSuccess[]`, `onFail[]` |
| `message` | `text`, `style` (`narration` `sensory` `dialogue` `system` `success` `failure` `warning`) |
| `moveTo` | `room` |
| `startDialogue` | `dialogue` |
| `startCombat` | `encounter` |
| `endGame` | none; shows the first matching epilogue |

### Save files (`Saves/<slot>.json`, gitignored)
Each save holds:
- `saveVersion`, `contentVersion`, `savedAtUtc`, `currentRoom`
- `player`: name, class, level, XP, abilities, proficient and expertise skills, HP and max HP, mana and max mana, gold, inventory, equipped
- `world`: flags, counters, reputation, questStages, visitedRooms, roomItems, onceUsed

Saving is allowed only during exploration. Per-encounter uses are never saved, because they are always full outside combat.

### Adding content
1. Add or edit JSON under `Data/`.
2. Run `dotnet test`. The content validation test lists every broken reference or typo.
3. Run `dotnet run --project src/RpgEngine.CLI -- --seed 1` and play through it.
4. Commit with `data: ...`.

---

## Git workflow

- **Branch:** `main`. **Remote:** `origin`, the private GitHub repo `text-rpg-engine`. Never rewrite pushed history.
- **Gate:** create a commit **only after `dotnet build` (0 warnings, 0 errors) and `dotnet test` (all passing) succeed.** Never commit a red build.
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
- [ ] **Milestone 1 (vertical slice):**
  - character creation
  - 3-room starter dungeon "Barrowlight": an NPC dialogue with skill checks and permanent consequences, an inspectable puzzle, and a turn-based combat encounter
  - save and load
  - full dice and skill-check test coverage
- [ ] **Later:** leveling and rests, parties, races and backgrounds, shops, saving mid-combat, JSON Schema export for editor autocomplete, CI.
