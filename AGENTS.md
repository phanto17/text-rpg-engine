# AGENTS.md: text-rpg-engine

This is the contributor guide for this repo: architecture rules, coding standards, the JSON content schemas, and the Git workflow. **Read it before changing code or data.** When a rule or schema changes, update this file in the same commit.

The project is a data-driven, narrative text RPG engine in C# (.NET 10), inspired by D&D 5e (SRD), Zork, and Wizardry. The engine knows nothing about any specific story: every room, item, monster, line of dialogue, class, and rule constant is loaded from `Data/`.

---

## Commands

```bash
dotnet build                                   # must report 0 warnings, 0 errors
dotnet test                                    # must be all green
dotnet run --project src/RpgEngine.CLI         # play
dotnet run --project src/RpgEngine.CLI -- --seed 42 --no-anim   # deterministic run, no dice animation
dotnet run --project src/RpgEngine.Pixel       # play in the pixel-art window (same --seed, --data, --saves; plus --crt)
dotnet test --collect:"XPlat Code Coverage"    # Cobertura report under tests/RpgEngine.Tests/TestResults/
dotnet run --project src/RpgEngine.CLI -- --export-schemas   # regenerate .vscode/schemas/ and .vscode/settings.json
```

CLI flags (Milestone 1):

| Flag | Effect |
|---|---|
| `--seed <n>` | Fixed RNG seed, so the run is reproducible |
| `--no-anim` | Skip dice animations |
| `--data <dir>` | Load content from another folder (default: `Data/` next to the executable) |
| `--saves <dir>` | Save folder (default: `./Saves`) |
| `--help` | Print usage |
| `--export-schemas [dir]` | Contributor tool, not listed in `--help`: write the content JSON Schemas under `dir` (default: the current folder) and exit without loading content. See [Editor schemas](#editor-schemas-vscodeschemas). |

Exit codes: `0` normal, `1` content failed validation (every error is listed), `2` bad arguments.

`RpgEngine.Pixel` takes `--seed`, `--data`, `--saves`, `--help`, and `--crt` (start with the scanline overlay on) with the same meanings and defaults, so both clients find the same `Data/` and `./Saves`. Two contributor-only flags, left out of `--help`: `--screenshot <file.png>` saves the canvas (at 2x) and exits, and `--play "line;line;..."` types those lines first, one every dozen frames with the typewriter skipped, e.g. `--play "new;Tester;1;1;1;1;1;2;7;1;down"` to land in the Ossuary Hall. In the window: click a button, press `1`-`9`, or type a command; `Tab` cycles the HUD tabs, `Enter` skips the typewriter, `Esc` goes back, `F2` toggles the CRT, `F3` toggles whole-pixel scaling, `F11` toggles borderless fullscreen.

The SDK is pinned by `global.json` to 10.0.401 with `rollForward: latestFeature`. Package versions live only in `Directory.Packages.props` (Central Package Management: Spectre.Console for the CLI, Raylib-cs for the pixel client). Shared build settings live in `Directory.Build.props`.

---

## Solution layout

```
RpgEngine.slnx
Data/                      ALL game content (JSON). Copied to CLI + test output (PreserveNewest).
Docs/LORE.md               World, philosophy, and narrative bible. Read it before touching Data/.
.vscode/schemas/           Generated JSON Schemas for every Data/ file (see Editor schemas). Never edit by hand.
.vscode/settings.json      Maps the schemas onto Data/ for VS Code (generated "json.schemas" entries).
src/RpgEngine.Core/        Engine: models, dice, checks, world state, content loading, combat, saves.
src/RpgEngine.CLI/         Spectre.Console UI: screens, rendering, hybrid input loop.
src/RpgEngine.Pixel/       Raylib-cs pixel-art UI: a headless game model and sequencer, plus the renderers.
tests/RpgEngine.Tests/     xUnit tests for Core, content validation, CLI playthroughs, and the pixel client's logic.
.github/workflows/ci.yml   CI: restore, Release build, and tests on ubuntu-latest and windows-latest.
ArtSource/                 Editable Pixelorama PXO masters and art manifest.
Tools/ArtPipeline/         Deterministic raster recipes, PNG validation/export, and optional preview broker.
Tools/PixeloramaExtension/ Version-pinned Pixelorama preview extension; not a game dependency.
```

Core folders (namespace = `RpgEngine.Core.<Folder>`): `Dice`, `Characters`, `Checks`, `World`, `Content` (`Defs/` for content records), `Commands`, `Exploration`, `Dialogue`, `Combat`, `Shop`, `Persistence`, `Engine`.

CLI layout:
- `CliHost`: argument parsing and content-error reporting.
- `GameApp`: the title-to-play loop, dispatching on `GameMode`.
- `Rendering/`: `Theme` (every color), `EventRenderer`, `Bars`, `RollText`, `StatusBar`, `DiceAnimator`.
- `Screens/`: Title, CharacterCreation (name, calling, burden, scores, skills, expertise, review), Exploration, Dialogue, Combat, Shop (buy, sell, leave), Info (sheet, inventory, journal, help), End.
- `Input/`: `Prompter`, the line-based numbered menus. There are no arrow-key widgets, so every prompt can be scripted in tests.

Pixel layout (namespace = `RpgEngine.Pixel.<Folder>`). Everything outside `Rendering/` is free of raylib types and is what the tests drive:
- `PixelHost`, `PixelOptions`: argument parsing, content loading, error reporting.
- `Game/`: `PixelGame`, the whole client without a window (title, `CreationFlow`, and the play loop by `GameMode`). It owns the `GameSession`, builds the contextual `ActionChoice` list the action bar shows (numbered buttons, sub-menus with Back), accepts clicks (`Pick`) and typed lines (`Submit`), runs enemy turns once the typewriter is quiet (`Update`), and feeds every `GameEvent` to the sequencer. `HudTab` names the HUD tabs.
- `Sequencing/`: `EventNarrator` turns each `GameEvent` into `LogLine`s (toned `LogSpan`s, with a `LogLineKind` for headings, rules, and quotes) and `SequenceCue`s (d20 banner, hit flash, lunge, floating number, fell, speaker, room, combat, level-up); `EventSequencer` plays those `Beat`s in order with a typewriter reveal, a hold before a dice verdict, `Skip`, and `TakeCues` for the renderer.
- `Presentation/`: `PresentationLoader` reads `Data/World/presentation.json` forgivingly (missing file, unknown names, wrong shapes become warnings, never errors); `PresentationDefaults` supplies content-independent generic fallbacks; `PresentationCatalog` layers the file over those fallbacks and resolves `RoomLook`s, `FigureLook`s, and world-state bindings. All shipped looks and story-specific bindings live in JSON. `PixelColor`, `SceneState`, and `CaptionLifetime` are renderer-free.
- `Layout/`: `PixelLayout` (the 480 by 270 canvas and its four panels), `ViewportFit` (whole-pixel or aspect-fit scaling and mouse mapping), `TextWrapper`, `ButtonFlow`, `PixelRect`.
- `Rendering/` (raylib): `PixelApp` (window, render texture, input routing), `SceneRenderer` and `SceneProps` (one procedural scene per `SceneStyle`), `WeatherField`, `Emblems` (procedural heraldry), `CombatStage`, `PortraitBox`, `MoodCaption`, `DiceBanner`, `HudRenderer`, `LogRenderer`, `ActionBarRenderer`, `CrtOverlay`, `TextureCache` (pictures under `Data/`), `Palette`, `Canvas`.

---

## Golden rules (non-negotiable)

1. **No content in C#.** Story text, rooms, items, monsters, dialogue, classes, spells, status effects, and rule constants live in `Data/`. Only UI chrome (labels like "Inventory", "HP", "Attack") may be written in code.
2. **Core is UI-agnostic.** `RpgEngine.Core` never references Spectre.Console or Raylib-cs and never writes to the console or a window. It returns `GameEvent`s (narration, check results, dice rolls, damage, item and quest changes, mode changes), and each client renders them. `ArchitectureTests` enforces this. In the pixel client, keep state, sequencing, layout, and presentation logic in classes that do not use raylib types, so they stay testable without a window.
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
- Test folders mirror Core folders: `Dice/`, `Checks/`, `World/`, `Content/`, `Dialogue/`, `Combat/`, `Persistence/`, `Commands/`, `Playthrough/`, `Balance/`; `Cli/` covers the terminal client and `Pixel/` the pixel client's headless classes.
- Tests are fully deterministic. Use `FixedRandomSource` for exact outcomes and a fixed-seed `Pcg32RandomSource` for statistical tests, so no test can flake.
  - When scripting dice, count every draw: initiative (player first, then monsters in listed order), checks, damage, weighted attack picks (only for monsters with 2+ attacks), loot rolls, and **one ambient pick each time a room with `ambient` lines is described**.
  - `LuckyRandomSource` (every roll at maximum) keeps story-path tests robust to number tuning.
- Test helpers live in `tests/RpgEngine.Tests/TestSupport/`:
  - `TestContent`: an in-memory JSON content builder.
  - Fixtures: `ExplorationFixture`, `CombatFixture`, `SessionFixture`.
  - Harnesses: `TestWorld`, `ExplorationHarness`, `CombatHarness`, `CliHarness`.
- **Balance checks** (`Balance/`): `BossFightSimulator` plays one boss encounter 2,000 times per class, using seeds 1 to 2000, the real `Data/`, the standard array, the class's starting kit, and the simple documented player in `BalancePolicy`. Higher levels are reached through the real XP thresholds, so HP, mana, and spells grow exactly as in play.
  - `BarrowHeartBalanceTests`: the `barrow_heart` fight at level 1. Every class plays once without the Flask of Holy Water and once with it. Each class's no-flask win rate must stay inside its band.
  - `MemoryWarrensBalanceTests`: the Act II bosses (`archive_vaelen_boss`, `reliquary_silas_boss`) at levels 2 and 3, without the flask. Level 3, where nearly every player meets them, has the fair band (40% to 92%; the ceiling allows for the Liminal Auditor's radiant Writ of Foreclosure against the radiant-weak Vaelen); level 2 only rules out an unwinnable fight (at least 5%).
  - `CradleBalanceTests`: the Act III `cradle_architect_boss` fight (the First Architect and a calcified warden) at levels 3 and 4, once with only the starting kit and once also carrying the three ally blessings (`wrens_ashen_censer`, `vaelens_silver_chime`, `silas_iron_standard`). Level 4 has the fair band (40% to 85%) without the blessings, level 3 only rules out an unwinnable fight, and the blessings must raise every win rate.
  - A new boss fight gets the same treatment: add it to a balance test with bands that bracket its measured win rates.
  - After an intended tuning change, run `dotnet test --filter Balance --logger "console;verbosity=detailed"`. Read the printed table (win rate, average rounds, average HP left on wins) and move the bands in the same commit as the data change.
- CLI tests drive the real UI through `Spectre.Console.Testing.TestConsole` with scripted input lines. The CLI exposes internals to the test assembly only.
- Pixel tests drive `PixelGame` directly (`Pick`, `Submit`, `Update`) with the in-memory fixtures and a fast `EventSequencer`; nothing under `Rendering/` runs in tests, and no test may open a window. Rendering changes are checked by eye with `--screenshot` and `--play` (see Commands). A new `SceneStyle`, `WeatherKind`, or `Emblem` needs a renderer branch, a shipped entry in `Data/World/presentation.json` where used, and a loader test that its JSON name is understood (`Parse_EverySchemaSpelling_IsUnderstood` covers every enum member). `LoreRegistryTests` checks that the canonical flag, counter, and quest registry matches Data in both directions.
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
    "xpThresholdsByLevel": [0, 100, 300, 750, 2700],
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
  - `effects`: `effects[]` run on the caster; `target` must be `self`. An `applyStatus` effect here makes a self-buff, e.g. `[ { "type": "heal", "dice": "2d8+2" }, { "type": "applyStatus", "status": "vigilant" } ]`.
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
- **`presentation.json`** (optional; read only by the pixel client, never by the engine): how rooms, monsters, and NPCs look. `rooms` is keyed by room id, `monsters` by monster id, and `npcs` by NPC id or by the id of the dialogue the NPC speaks (the shipped file uses dialogue ids, so `architect_post` styles `reconciled_architect`). All shipped looks and epithets are here. Missing entries use content-independent generic fallbacks. Unknown properties, unknown names, and malformed entries are reported as warnings and skipped, never errors.
  ```json
  { "bindings": { "resonanceCounter": "lantern_resonance", "sigilCounter": "sigil_seq", "lanternItem": "barrowlight", "sunSealItem": "sun_triad_seal", "moonSealItem": "moon_triad_seal", "bellMendedFlags": ["fitted_bell_splinter"], "brazierLitFlags": ["ending_sun_stasis", "ending_moon_vigil", "ending_triad_passage"] },
    "rooms": { "lichgate": { "palette": { "backgroundTop": "#1b2230", "backgroundBottom": "#3a4a52", "architecture": "#2a2f33", "accent": "#7fa6b8" },
                             "sceneStyle": "moor_gate", "weather": "rain", "backdropImage": "Assets/Rooms/lichgate.png",
                             "moodCaption": "Rain needles the iron gate and faceless stones above Greywater moor." } },
    "monsters": { "gravebound_knight": { "primaryColor": "#6a6f78", "accentColor": "#c04040", "emblem": "knight_helm", "title": "Warden of the Barrow Heart", "spriteImage": "Assets/Monsters/gravebound_knight_idle.png", "animation": { "frames": 4, "frameMilliseconds": 200 } } },
    "npcs": { "wren": { "speakerName": "Sister Wren", "primaryColor": "#404858", "accentColor": "#ffd080", "emblem": "lantern_nun", "title": "Sister of the Lantern Bearers", "portraitImage": "Assets/Portraits/wren.png" } } }
  ```
  - `moodCaption` fades in over the bottom of the scene on room entry, remains visible through the room-description beat, then holds for two seconds and fades out. Skipping the beat starts the same hold.
  - `bindings` maps visual state to data-defined counter, item, and flag IDs. `sigilDoorOpenFlag` optionally names the flag opening the rendered sigil door, independently of `sigilCounter`. `beaconLitFlag` optionally names the flag lighting the sunken-cathedral beacon; missing bindings keep it dark. `releasedSpiritsFlag` names the flag emptying the chapel jars; `passageVisitedRoom` names the destination whose visited state raises the bridge floodgate after crossing. Missing references keep spirits tethered and the gate lowered. Missing references resolve to zero/false. Keep story-specific IDs here, never in pixel renderer code. Stateful scene details render over both PNG backdrops and procedural fallbacks.
  - `speakerName` is the name under the portrait, and how a speaker with no dialogue of their own (a shop keeper's greeting) is matched to an NPC. Keep it equal to the dialogue's `speaker`.
  - `sceneStyle`: `moor_gate ossuary_vault crypt_rotunda spiral_stair sunken_cathedral echo_chapel drowned_bridge twin_seal_gate archive_stacks tallow_loom vow_nursery scoured_barracks lye_cistern iron_bastion uncarved_stair sanctuary_landing sundered_bell_nave cradle_of_ash`.
  - `weather`: `none rain ash_motes spores memory_salt embers`.
  - `emblem`: `knight_helm thrall_skull choir_mask warden_halberd carillon_cradle iron_brazier architect_astrolabe lantern_nun scribe peddler keeper quartermaster monk wright lancer mournling chorister halberdier prelate commander hooded`.
  - Colors are `#rgb`, `#rrggbb`, or `#rrggbbaa`. Picture paths are relative to `Data/` (for example `Assets/Rooms/lichgate.png`); a missing picture falls back to the procedural scene or emblem. The sections may also be arrays of entries carrying an `id`.
  - A monster `animation` is optional and applies to `spriteImage` as a horizontal equal-width sheet. `frames` is 1–16, `frameMilliseconds` is 50–2000; invalid timing is ignored with a warning. A malformed sheet displays statically. Add timing and the image to the same content change.
  - `RealDataTests` requires the shipped file to load with no warnings, to deserialize under the strict content options (so it matches the schema), and to cover every room, monster, and dialogue exactly once. A new room, monster, or dialogue therefore needs an entry here too.
  - The records live in `Content/Defs/PresentationDef.cs` (a closed family in one file, like the conditions) so the schema export covers the file; their enum members carry `JsonStringEnumMemberName` for the snake_case spellings above.
- **`shops.json`** (optional): merchants opened by the `startShop` effect.
  ```json
  { "id": "mote_stall", "name": "Mote's Stall", "keeper": "Dunstan Mote", "greeting": "Coin first, {player.name}.",
    "stock": [ { "item": "healing_potion", "price": 40, "stock": 3 }, { "item": "holy_water", "price": 25 } ],
    "buybackRate": 0.5 }
  ```
  - `stock` is the starting count (1 to 999); leave it out for an item that never runs out. Bought-down stock is saved with the world.
  - The player sells for `floor(value * buybackRate)` (default `0.5`). Quest items are never bought, and equipped gear sells only while a spare is carried.
  - The keeper speaks `greeting` when the shop opens. The game is in `Shop` mode (no saving) until the player leaves.

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
  - **`self`:** `use.effects` (required, non-empty) run on the user, in or out of combat. An `applyStatus` effect takes hold only in combat; used outside a fight, the item's other effects still run.
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
- **Features:** a feature with failing `conditions` is hidden. Interaction verbs must be one lowercase word. `inspect` shows the description and then runs any `inspect` interaction. Only `take`, `use`, and `inspect` fall back to a matching interaction (for `take` and `use`, when no item matches). Any other word the command parser claims (`restore`, `save`, `look`, `go`, `drop`, `talk`, `get`, `examine`, the direction words, and so on) is a validation error as an interaction verb, because typing it runs the built-in command instead.

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
| `gold` | `min?`, `max?` (the gold the player carries) |
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
| `spendGold` | `amount` (at least 1); takes that much gold, never more than the player carries. Gate the interaction with a `gold` condition so it only appears when the player can pay. |
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
| `startShop` | `shop` (opens the buy and sell screen) |
| `applyStatus` | `status` (a `status_effects.json` id); puts the status on the player. Statuses exist only in combat, so outside a fight it does nothing. Use it in self-targeted spell `effects` and self-targeted consumables. |
| `endGame` | none; shows the first matching epilogue |

### Save files (`Saves/<slot>.json`, gitignored)
Each save holds:
- `saveVersion` (currently `2`), `contentVersion`, `savedAtUtc`, `currentRoom`
- `player`: name, class, background, level, XP, abilities, proficient and expertise skills, HP and max HP, mana and max mana, gold, inventory, equipped
- `world`: flags, counters, reputation, questStages, visitedRooms, roomItems, onceUsed, shopStock (optional; remaining limited stock by shop and item)

A save from another format version is refused with a message naming its version (version 2 added `background`). Saving is allowed only during exploration (not in dialogue, combat, or a shop). Per-encounter uses are never saved, because they are always full outside combat.

### Editor schemas (`.vscode/schemas/`)
VS Code autocompletes and validates every file in `Data/` against a JSON Schema generated from the content records:
- `ContentSchemas` (Core) runs .NET's `JsonSchemaExporter` with the loader's own `ContentJson.Options`, so each schema matches exactly what `ContentLoader` accepts: camelCase names, enum values, `required` fields, nullability, polymorphic `type` discriminators (`anyOf` branches), and `additionalProperties: false`. Cross-references (unknown IDs, unset flags) and dice syntax are still checked only by `ContentValidator`.
- `ContentSchemas.All` lists one schema per file kind (`room.schema.json` for `Data/Rooms/*.json`, `game.schema.json` for `Data/game.json`, `presentation.schema.json` for `Data/World/presentation.json`, and so on). A new content folder or file needs an entry there.
- `--export-schemas` writes the schemas and replaces the generated `json.schemas` entries in `.vscode/settings.json`, keeping every other setting (comments in that file are not kept).

**Whenever you change a record in `Content/Defs/`** (add, rename, or remove a property, change a type or enum, add a condition or effect), regenerate and commit the output in the same commit:
```bash
dotnet run --project src/RpgEngine.CLI -- --export-schemas
```
`ContentSchemasTests.CommittedSchemas_MatchTheContentRecords` fails until the committed schemas match the records, and another test fails if any `Data/` file has no schema.

### Adding content
**Before adding or editing any JSON under `Data/`, read [`Docs/LORE.md`](Docs/LORE.md) to keep prose tone, philosophical themes, faction alignments, and flag/counter names consistent. Whenever a commit adds a new flag, counter, faction, class, or quest, update the registry in `Docs/LORE.md` in the same commit.**

1. Add or edit JSON under `Data/`.
2. Run `dotnet test`. The content validation test lists every broken reference or typo.
3. Run `dotnet run --project src/RpgEngine.CLI -- --seed 1` and play through it.
4. Commit with `data: ...`.

---

## Git workflow

- **Base branch:** `main`. Use a review branch for changes. **Remote:** `origin`, the private GitHub repo `text-rpg-engine`. Never rewrite pushed history.
- **Gate:** create a commit **only after `dotnet build` (0 warnings, 0 errors) and `dotnet test` (all passing) succeed.** Never commit a red build. CI runs the same gate (Release configuration, Ubuntu and Windows) on every push and pull request to `main`.
- **Conventional Commits:** `<type>(<optional scope>): <imperative summary>`. Use lowercase, keep it to 72 characters or fewer, no trailing period. Add a body when the *why* isn't obvious.

| Type | Use for |
|---|---|
| `feat` | New engine or UI capability |
| `fix` | Bug fix |
| `data` | Content changes under `Data/` (rooms, dialogue, items, bestiary, rules) |
| `test` | Adding or changing tests only |
| `docs` | README, AGENTS.md, comments-only changes |
| `refactor` | Code change with no behavior change |
| `chore` | Tooling, scaffolding, housekeeping |
| `build` | Build system, SDK, or package changes |

  Scopes (optional): `core`, `cli`, `tests`, `data`. Examples: `feat(core): add d20 dice expression parser`, `data: add sigil door puzzle`.
- Keep commits small and focused. Prefer keeping engine changes and content changes in separate commits.
- Never commit `Saves/`, `Logs/`, `*.log`, coverage output, `bin/`, `obj/`, or `.claude/`.
- **No AI attribution, ever.** Commit messages, tags, and PR titles and descriptions must **never** contain `Co-Authored-By:` trailers, "Generated with …" lines, AI or Claude mentions, or emoji badges. They must read as standard human-written Conventional Commits.
  - In commit messages, call this file "the contributor guide" (e.g. `docs: update contributor guide`) so a history scan for AI mentions stays clean.
  - Before pushing, check the message with `git log -1 --format=%B`.

### Art authoring and live preview

Pixel audio is optional presentation metadata: `audio.effects` maps the fixed roles `select`, `step`, `impact`, `damage`, `lantern` to relative `Assets/Audio/*.wav` paths; `audio.ambience` maps room IDs to loops. Missing/corrupt clips are silent. Clips use bounded canonical PCM mono 22050 Hz / 16-bit WAVs. `AudioPresentationDef` participates in the generated presentation schema. All playback and device lifecycle code belongs in Pixel `Rendering/`; cue selection, rate limiting, path checks and preferences stay renderer-free in `Presentation/`. Never add audio calls to Core. F4 mutes, F5 opens settings; local preferences live at `<saves>/.pixel/audio.json`, outside game slots. Update audio recipes, `ArtSource/audio.json`, reviewed WAVs and [the audio guide](Docs/AUDIO_PIPELINE.md) together. Run `python Tools/AudioPipeline/validate_audio.py Data/Assets/Audio --reproduce` alongside the .NET and art gates; CI must not open an audio device.

For motion QA, `RpgEngine.Pixel --record <fresh-directory> --play "line;line;..."` writes 480×270 PNG frames at 12 fps using a fixed 60 Hz presentation clock, waits for narration between inputs, and exits when the script settles (two-minute cap). It is a contributor flag, omitted from help; it cannot be combined with `--screenshot`. Use explicit typed combat targets, e.g. `attack Ser Halric`, or menu picks `1;1` to open Attack then choose its target. Compile frames with `python Tools/ArtPipeline/make_preview.py <directory> <fresh.gif> --last-seconds 20`. Screenshot and recording captures include CRT when enabled. Keep captures in ignored `ArtBuild/`. Never open a window from unit tests.

Act I art covers all four rooms, six NPC appearances (including reused Wren art), four enemy idle sheets and four ambience loops. `Tools/ArtPipeline/build_act_one.py` and `build_act_one_spokes.py` reproduce the new PNGs. Keep lamps, seepage, beacon, chapel jars, bridge water, floodgate and lantern-visible echoes above both painted and procedural backgrounds; check the beacon dark/restored, jars tethered/released and gate lowered/crossed. See [Act I visual QA](Docs/ACT_ONE_VISUAL_SLICE.md).

`Presentation/AttackMotion` defines a 600 ms stepped strike with damage cues at 240 ms. Keep motion sampling free of raylib and test its phases. Attack narration and dice finish before the lunge begins; damage follows its anticipation hold. Silkscreen draws once for headings; doubling its tiny glyphs closes their counters. Fit long input tails and labels using `Layout/TextFit` with the same metrics used to draw them.

`Docs/STYLE_GUIDE.md` defines the 16-color source palette, licensed font, dimensions, naming, motion targets and visual QA. `Docs/ART_PIPELINE.md` covers editable PXO masters, deterministic recipe exports, Pixelorama CLI commands and rollback. The optional `Docs/PIXELORAMA_BRIDGE.md` describes the local preview protocol and its safety checks. Keep editor scripts and Python/WebSocket tooling out of `src/RpgEngine.Core` and game runtime dependencies. New preview job fields require updating `Tools/ArtPipeline/preview-job.schema.json`, the extension, producer, tests and bridge guide together. Run `python -m unittest discover -s Tools/ArtPipeline/tests -v`, `python Tools/ArtPipeline/validate_assets.py Data/Assets` and `python Tools/ArtPipeline/verify_reproducible.py` alongside the .NET gates; CI runs them on Windows and Ubuntu.

---

## Roadmap status
- [x] **Phase 1:** solution scaffold, tooling, docs, Git and GitHub.
- [x] **Milestone 1 (vertical slice):**
  - character creation
  - 3-room starter dungeon "Barrowlight": an NPC dialogue with skill checks and permanent consequences, an inspectable puzzle, and a turn-based combat encounter
  - save and load
  - full dice and skill-check test coverage
- [x] **CI:** GitHub Actions build and test on ubuntu-latest and windows-latest.
- [x] **Milestone 2 (the Under-Barrow):**
  - [x] 2A: four Callings, Burdens (backgrounds) with keepsakes, the `background` condition, save format v2, prologue reactivity
  - [x] 2B: the `rest` effect, XP thresholds with level-ups and `spellsByLevel`, shops (`shops.json`, `startShop`, Shop mode)
  - [x] 2C: Act I content: the Weeping Stair, Nocturne, the Echo Chapel, and the Drowned Tollbridge. The descent no longer ends the game; crossing the tollbridge leads on into Act II
- [x] **Milestone 3 (the Memory Warrens, Act II):** `warren_crossroads` and the Twin Seal gate, the Archive of Unkept Vows (Prelate Vaelen) and the Iron Reliquary (Commander Silas), level-3 abilities for every Calling, and the Act II boss balance check.
- [x] **Milestone 4 (the Cradle of the First Architect, Act III; content version 1.0.0):** `unseal` now opens the way down to `threshold_vestibule`, `penitent_landing` (a sanctuary, Oakhaven's `cradle_quartermaster` shop, and the ally blessings), `bell_nave`, and `cradle_of_ash` (the First Architect and the Great Brazier). The `applyStatus` effect, level-4 capstones, and buff statuses. The game ends only at the Great Brazier (`relight`, `tune`, or `complete`), with seven epilogues keyed to that choice and to the First Architect, Wren, Vaelen, Silas, the Nocturne beacon, and `lantern_resonance`, plus a fallback. `CradleBalanceTests` guards the final fight.
- [x] **Tooling:** JSON Schema export for editor autocomplete (`--export-schemas`, `.vscode/schemas/`).
- [x] **Milestone 5 (the pixel-art client, `RpgEngine.Pixel`):** a Raylib-cs front end over the same `GameSession` and saves: a 480 by 270 canvas scaled by whole pixels with a CRT toggle, procedural scenes per `SceneStyle` with weather and the Barrowlight glow, dialogue portraits, combat sprites with flashes, lunges, and floating numbers, a typewriter log with a d20 banner, a HUD with Inventory, Quests, Stats, and Exits tabs, clickable or typed actions, and the optional `Data/World/presentation.json` with its schema.
- [ ] **Later:** parties, races, saving mid-combat.
