# Barrowlight — World, Philosophy & Narrative Bible

> **Instructions for Contributors & Coding Agents:** 
> Read this document before creating or modifying any content in `Data/Rooms/`, `Data/Dialogues/`, `Data/World/`, `Data/Items/`, `Data/Bestiary/`, or `Data/Rules/`. All prose, quests, classes, factions, and `WorldState` identifiers must strictly follow the canon and rules below.

---

## 1. Narrative Voice & Formatting Rules

- **Tone:** Grounded gothic-philosophical dark fantasy. Rain-sodden peat, green-black bronze, tallow smoke, rusted mail, and quiet existential weight. Avoid generic high-fantasy clichés, modern slang, or marvel-style quips.
- **The "Undertale / Dziady" Rule for Encounters:** No named enemy or boss is a mindless XP pinata. Every major adversary in the barrows is a person or spirit trapped in an existential loop of grief, duty, or fear. Combat encounters should always offer a meaningful `parley` path alongside martial/arcane victory, with advantages unlocked by paying attention to the world (`insight`, `religion`, `history`, or inspecting room features).
- **Room Description Structure:**
  - `description` (first visit / `look`): 2–3 atmospheric paragraphs grounding the player in sight, sound, smell, and temperature.
  - `revisit` (subsequent visits): 1 concise sentence summarizing the space without repeating full exposition.
  - `ambient`: 3–4 short, standalone sensory lines (one is drawn via `IRandomSource` whenever the room is described).
- **Strict Engine Formatting Constraints:**
  - Write **plain text only** inside JSON strings. Never include Spectre.Console markup tags (e.g. `[red]`) or manual skill check badges (e.g. `[Persuasion DC 12]`)—`EventRenderer` and `RollText` generate those dynamically from data.
  - Use `{player.name}` and `{player.class}` tokens naturally in dialogue and epilogues.
  - Every `flag` or `counter` checked in a `conditions` block **must** be set by at least one effect in `Data/`, or `ContentValidator` will fail the build.

---

## 2. The Setting: Greywater Moor & The Under-Barrow

### The Surface: Greywater Moor & Hollin's Rest
On the surface lies the **Greywater moor**, a bleak borderland of rain-swept hills and peat bogs. At the foot of the hills huddles the village of **Hollin's Rest**, whose superstitious inhabitants make the sign of the ash and refuse to speak the barrow's name. For three hundred years, the **Ashen Order** tended the barrows above the moor, keeping the **Barrowlight** lanterns burning so the dead would stay asleep. At the turn of the season, the Barrowlight went dark—and the graves stopped staying shut.

### The Subterranean Twist: The Under-Barrow
What the villagers of Hollin's Rest believe is a self-contained hilltop tomb (*The Lichgate*, *The Ossuary Hall*, and *The Barrow Heart*) is actually the **Upper Threshold**—an airlock sealing off a vast, vertical subterranean civilization known as **The Under-Barrow**. 

Beneath Ser Halric's sarcophagus, a hidden spiral stair descends into a cavernous world of sunken cathedrals, fungal lantern-towns (**Nocturne**), drowned tollbridges, and memory-flooded warrens. Here, awakened spirits who still remember their names live alongside mortal runaways, deserters, and heretics in a fragile, melancholic society.

---

## 3. Philosophical Core & The Triad of Passage

Every funeral lock, faction debate, and questline in *Barrowlight* revolves around the **Triad of Passage**—represented by the three sigils on the Ossuary Hall's bronze door (**Sun, Moon, and Skull**) and the Ashen litany:
> *"First the light that wakes the field, / Then the lamp the night must wield, / Last the face all faces yield."*

### The Four Philosophical Pillars
1. **Mortality as the Source of Value (The Skull — *Heidegger's Being-Towards-Death*):**
   - *The Original Sin:* Centuries ago, the First Architects feared death and loss so deeply that they built the Barrowlight network. The lanterns did not grant the dead peace; they suspended souls in artificial, dreamless stasis so nothing would ever truly be lost. By cheating finality (the Skull), they poisoned mortal life (the Sun).
2. **The Weight of Memory & The Ethics of Grief (The Moon — *Phenomenology of Loss*):**
   - *The Rupture:* As the Barrowlights fail across the deep, three hundred years of repressed grief, unkept promises, and buried trauma flood the Under-Barrow. Characters must choose between **Pathological Clinging** (refusing to let the dead go) and **Callous Erasure** (burning the past to ash).
3. **Existence vs. Calcified Essence (The Sun — *Sartre & Camus*):**
   - *The Curse of the Loop:* For the living, *existence precedes essence*—we define ourselves through choice and change. In the Under-Barrow, when an awakened spirit surrenders to despair or obsession, they **calcify into pure essence**—losing their memories until only a single frozen duty or grudge repeats forever (as happened to Ser Halric walking his endless circle in the ash).
4. **Defiant Empathy in a Dimming World (The Triad Restored — *Frankl's Tragic Optimism*):**
   - *The Player's Agency:* There is no grand prophecy. Hope is forged through micro-acts of witness, empathy, and release—listening to a wounded deserter in the rain, learning a spirit's forgotten name, and speaking the **Rite of Rest** (`"Your watch is ended. You are relieved. Rest."`).

---

## 4. The Three Ideological Factions (`Data/World/factions.json`)

| Faction ID | Name | Triad Stance | Philosophy & Goals | Reputation Triggers |
| :--- | :--- | :--- | :--- | :--- |
| `ashen_order` | **The Ashen Order** | **Corrupted Sun**<br>*(Order via Erasure)* | Keepers of the Greywater barrows for 300 years. Traditionalists believe memory and grief are contagious rot; the dead must be forced back into dreamless stasis or purged with iron and holy water. Fracturing between hardliners and compassionate reformers like Sister Wren. | **+Rep:** Earning Wren's trust (+10), joining her in the descent (+5), laying Ser Halric to rest (+10).<br>**-Rep:** Failing/antagonizing Wren (-5), intimidating her into fleeing (-15), desecrating Order seals. |
| `hollow_choir` | **The Hollow Choir** | **Corrupted Moon**<br>*(Refusal of Loss)* | A subterranean conclave of grieving mortals and ancient spirits in the Under-Barrow who view final death as an atrocity. They sabotage Barrowlights to keep the dead awake forever, binding fading spirits to music boxes, ledgers, and reliquaries—even as those spirits calcify into agony. | **+Rep:** Preserving memory anchors, shielding awakened spirits from Ashen purges.<br>**-Rep:** Severing spirit-tethers via the Rite of Rest, relighting stasis beacons. |
| `lantern_bearers` | **The Wayfarers of the Threshold** | **Balanced Triad**<br>*(Witness & Release)* | A loose sanctuary movement in Nocturne (which Sister Wren joins if `wren_joins_descent` is set). They believe grief must be witnessed and honored, protecting conscious spirits in the Under-Barrow while guiding calcifying souls toward peaceful finality. | **+Rep:** Resolving encounters via empathetic `parley`, healing the afflicted in Nocturne, restoring balanced resonance to the Barrowlight.<br>**-Rep:** Slaughtering sentient spirits without parley, siding with extremist purges. |

---

## 5. Character Callings & Protagonist Backgrounds (`Data/Rules/classes.json`)

To ground the protagonist in Greywater's world, character classes represent **Callings**: unconventional vocations with built-in backstories and `ClassCondition` hooks. All four ship in `classes.json` (file order is the creation menu order); spells and techniques keep stable ids even when renamed.

1. **The Oath-Eater (`oath_eater`: martial, `d10` hit die, priority STR/CON/CHA):**
   - *Lore:* A former iron-penitent who survived an Ashen tribunal by swallowing the ash of a broken vow. `longsword` and `chain_mail`, with per-fight techniques *Ash-Blood Second Wind* (`second_wind`, self-heal 1d10+1) and *Vow-Breaker's Strike* (`vowbreaker_strike`, weapon attack +1d6).
   - *Narrative Hook:* Understands the crushing weight of sworn duty; unlocks unique dialogue with bound guardians and Ashen soldiers.
2. **The Liminal Auditor (`liminal_auditor`: specialist, `d8` hit die, priority DEX/CON/CHA, `expertiseCount: 2`):**
   - *Lore:* A funerary bailiff and tomb-runner from Hollin's Rest hired to audit the unsettled debts and heirlooms of the restless dead. `shortsword`, `dagger`, `leather_armor`, with per-fight techniques *Clause of Severance* (`sneak_attack`, weapon attack +1d6, twice) and *Grave-Salt Feint* (`grave_salt_feint`, CON DC 12 save or 1d4 piercing and `grave_chill`).
   - *Narrative Hook:* Approaches the supernatural as a web of human promises, contracts, and secrets; excels at skill checks and underground commerce.
3. **The Vesper Cantor (`vesper_cantor`: liturgical caster, `d6` hit die, priority INT/CON/DEX, mana 4 + INT):**
   - *Lore:* An exiled scholar of funerary acoustics and memory-tallow who casts spells by plucking the resonant frequencies of the dead. `quarterstaff`; spells *Tallow-Spark* (`fire_bolt`, free 1d10 fire), *Chords of Unrest* (`magic_missile`, 2 MP, 3d4+3 force, never misses), and *Toll the Vesper* (`vesper_toll`, 2 MP, WIS save, 2d8 radiant, half on a save).
   - *Narrative Hook:* Deciphers ancient Under-Barrow litanies and senses the emotional echoes left in stone and ash.
4. **The Remnant Vessel (`remnant_vessel`: half-dead caster, `d8` hit die, priority CHA/CON/DEX, mana 3 + CHA):**
   - *Lore:* A traveler who died on the night the Barrowlight went out and woke with a cold ember in their ribs. Walks the razor's edge between mortal agency and ghostly calcification. `shortsword` and `leather_armor`; *Hollow Rasp* (`hollow_rasp`, free 1d8 cold), *Borrowed Ember* (`borrowed_ember`, 2 MP, DEX save, 2d6 radiant, half on a save), and *Shroud of Ash* (`shroud_of_ash`, per-fight self-heal 1d8+1).
   - *Narrative Hook:* The dead recognize one of their own; the living are never quite sure.

**Level 2 abilities** (`spellsByLevel`; both ways through the Barrow Heart reach level 2 at 100 XP): the Oath-Eater learns *Ash Brand* (`ash_brand`, weapon attack +1d8 once per fight), the Liminal Auditor *Ledger Strike* (`ledger_strike`, weapon attack +2d6 once per fight), the Vesper Cantor *Dirge of Tallow* (`dirge_of_tallow`, 3 MP, CON save, 3d6 fire, half on a save), and the Remnant Vessel *Ember Vigil* (`ember_vigil`, 1 MP self-heal 1d6+2). XP thresholds are `[0, 100, 300, 900, 2700]`.

Every Calling starts with two `healing_potion`s. The balance check (`tests/RpgEngine.Tests/Balance/`) keeps each Calling's solo win rate against the Barrow Heart inside a band (about 55-80% without the holy water).

### Burdens (`Data/Rules/backgrounds.json`)

After choosing a Calling, the protagonist chooses a **Burden**: the grief, debt, or vow that brought them up the hill. Each grants one bonus skill, a keepsake (`type: quest`, so it can't be dropped), and sometimes coin. Dialogue and parley options can react to it with `{ "type": "background", "background": "<id>" }`.

**Prologue reactivity:** Sister Wren's hub offers one line per Calling and one per Burden (`class`/`background` conditions). The Bereaved Kin and the Ash Deserter earn `wren_kinship`; the Vesper Cantor learns the sigil order from her litany; the others are acknowledged in prose only. Ser Halric's parley adds an option for the Oath-Eater (the vow he keeps), the Remnant Vessel (one dead thing to another), the Ash Deserter (the scored signet), and the Bereaved Kin (who you lost).

| Burden ID | Name | Bonus Skill | Keepsake | Hook |
| :--- | :--- | :--- | :--- | :--- |
| `bereaved_kin` | Bereaved Kin | `insight` | `tarnished_locket` | Someone they loved was buried at Hollin's Rest and no longer stays in the grave. Understands grief from the inside; the Moon pillar made personal. |
| `ash_deserter` | Ash Deserter | `religion` | `scored_signet` | Walked out of the Ashen Order the night it burned a grave with the mourners beside it. Knows the litanies; the Order's hardliners know their face. |
| `moor_bailiff` | Moor Bailiff (+10 gold) | `persuasion` | `elders_writ` | Hired by the elders of Hollin's Rest to settle the barrow's "arrears". Treats the dead as parties to a contract, which is sometimes exactly what they need. |
| `fever_dreamer` | Fever Dreamer | `perception` | `bell_splinter` | Has heard a bell beneath the moor every night since the Barrowlight died. The Under-Barrow is already calling them by name. |

---

## 6. The Barrowlight Lantern Mechanics

Once claimed in `inner_crypt` (`item: barrowlight`), the lantern serves as both a narrative compass and a mechanical tool:
- **Lantern Resonance (`counter: lantern_resonance`):**
  - Resolving encounters through empathy, release, and `parley` increases `lantern_resonance` (`+1` when Ser Halric is laid to rest), causing the lantern to burn with a warm, steady golden light.
  - Resolving encounters through brute destruction decreases `lantern_resonance` (`-1` when Ser Halric is destroyed), chilling the flame into a thin, ash-pale frost.
- **Unveiling Memory Echoes (`hasItem: barrowlight`):**
  - In Act I–III rooms, holding the `barrowlight` reveals hidden `features` (Memory Echoes). Using the `shine` or `inspect` verb on these echoes reveals past traumas and sets knowledge flags that grant `advantageIf` on boss `parley` checks.

---

## 7. Plot Outline & World Geography

### Prologue: *The Upper Threshold* (Milestone 1 — Shipped)
- **`lichgate` (The Lichgate):** Rain-swept hilltop gate above Hollin's Rest. Features the `toppled_headstone` (`perception` DC 10 -> `healing_potion`), `statues` with chiselled-off faces, and **Sister Wren** (`dialogue: wren`).
- **`ossuary_hall` (The Ossuary Hall):** Vault of ash-marked skulls (`investigation` DC 11 -> 12 gold) and the `bronze_door` puzzle (Sun -> Moon -> Skull sequence; `religion` DC 13 `study`, `dex` DC 12 needle trap dealing `1d4` piercing).
- **`inner_crypt` (The Barrow Heart):** Circular ash chamber housing **Ser Halric** (`gravebound_knight`) and a `barrow_thrall`. Resolvable via combat (`holy_water` deals double radiant damage) or `parley` (`persuasion` DC 13, or `religion` DC 10 with advantage if `wren_taught_rite`). Claiming `barrowlight` shifts the plinth, revealing the `sunken_stair` (`verb: descend`), which leads down to the `weeping_stair` and Act I.

### Act I: *Nocturne & The Weeping Stair* (Milestone 2 — Shipped)
- **`weeping_stair` (The Weeping Stair):** The spiral descent beneath the Barrow Heart where surface rain gives way to bioluminescent ash-moss and echoing memory fragments.
- **`nocturne_square` (Nocturne — The Lantern Market):** A subterranean hub settlement built inside a sunken cathedral. Populated by half-remembering revenant merchants, runaway heretics, and Sister Wren's makeshift sanctuary (if `wren_joins_descent`). Offers **Rest / Campfire recovery** and **Merchant bartering**.
- **`drowned_tollbridge` & `echo_chapel`:** Spoke locations where the player investigates who sabotaged Nocturne's central beacon—uncovering the rivalry between the **Hollow Choir** and an **Ashen Order** expeditionary patrol. Neither fight starts on entry: the player chooses when to `approach` Cantor Miriel or Toll-Warden Garin, so each room's memory echo can be shone first for parley advantage. The beacon is relit with `relight` in the square (`restore` is the parser's synonym for loading a save).
- **Act I transition:** once the tollbridge is resolved, `cross` at the `far_arch` completes `nocturne_beacon` (`100`) and leads into `warren_crossroads`.

### Act II: *The Memory Warrens* (Milestone 3 — Shipped)
- **`warren_crossroads` (The Gate of the Twin Seals):** The central subterranean junction beyond the Drowned Tollbridge (`east`), splitting `north` toward the Hollow Choir's **Archive of Unkept Vows** and `south` toward the Ashen Order's **Iron Reliquary**. Features a Wayfarer **Sanctuary Brazier** (`rest`) and the **Twin-Seal Threshold Gate** (`unseal` with both `moon_triad_seal` and `sun_triad_seal` to complete `twin_seals` and trigger the Act II climax).
- **Northern Spoke — The Archive of Unkept Vows (`whispering_stacks` -> `tallow_foundry` -> `vow_sanctum`):** The Hollow Choir's acoustic scriptorium where fading memories are spun into silver-inlaid vellum and brass cylinders. In `vow_sanctum`, the player chooses when to `approach` **Prelate Vaelen** (`archive_vaelen_boss`), who is keeping her own drowned daughter Lira's spirit tethered inside a grand carillon. Finding Memory Echoes with the `barrowlight` (`shone_stacks_echo`, `shone_foundry_echo`, `shone_sanctum_echo`), reading Vaelen's ledger (`read_vaelen_ledger`), slackening the Great Acoustic Loom (`slackened_choir_loom`), or speaking with **Archivist Caelan** (`knows_vaelen_lullaby`) unlocks and strengthens Parley options to release Lira (`vaelen_reconciled`), enabling retrieval of the `moon_triad_seal`.
- **Southern Spoke — The Iron Reliquary (`scoured_cloister` -> `lye_cistern` -> `reliquary_bastion`):** An Ashen Order garrison where soldiers submerge confession tablets in boiling lye and inhale whitewash steam to scrub away their own names, fear, and grief. In `reliquary_bastion`, the player chooses when to `approach` **Commander Silas** (`reliquary_silas_boss`), a hollowed zealot guarding the `sun_triad_seal` who has scoured his own mind twelve times and forgotten why he fights. Uncovering Memory Echoes (`shone_cloister_echo`, `shone_cistern_echo`, `shone_bastion_echo`), salvaging his lead strip (`found_silas_lead_strip`), quenching the cistern (`quenched_lye_cistern`), or speaking with **Armorer Venn** (`knows_silas_son`) unlocks and strengthens Parley options to stand him down (`silas_relieved`).

### Act III: *The Cradle of the First Architect* (Future Milestone)
- Confronting the immortal First Architect at the Great Brazier and choosing the fate of the Under-Barrow based on your accumulated `WorldState` flags, faction reputations, and `lantern_resonance`.

---

## 8. Canonical `WorldState` Registry

*Update this table in the same commit whenever a JSON file introduces a new flag, counter, faction, class, background, or quest.*

### Flags (`flag` / `notFlag`)
| Flag ID | Set In | Meaning & Downstream Effects |
| :--- | :--- | :--- |
| `met_wren` | `Dialogues/wren.json` | Player has spoken to Sister Wren at least once. |
| `read_wren` | `Dialogues/wren.json` | Passed `[Insight DC 10]` on Wren; grants advantage on `[Persuasion DC 12]`. |
| `wren_kinship` | `Dialogues/wren.json` | A Bereaved Kin or Ash Deserter shared their burden with Wren; like `read_wren`, grants advantage on `[Persuasion DC 12]`. |
| `wren_trusts_player` | `Dialogues/wren.json` | Passed `[Persuasion DC 12]` (+10 `ashen_order` rep, grants `holy_water`, sets `wren_taught_rite` and `knows_sigil_order`). |
| `wren_distrusts` | `Dialogues/wren.json` | Failed Persuasion or Intimidation (-5 `ashen_order` rep); locks Wren into `cold` greeting. |
| `wren_fled` | `Dialogues/wren.json` | Passed `[Intimidation DC 12]` (-15 `ashen_order` rep); Wren vanishes from `lichgate`. |
| `wren_taught_rite` | `Dialogues/wren.json` | Unlocks `"Speak the Rite of Rest"` (`[Religion DC 10]` with advantage) in `barrow_heart` parley. |
| `wren_joins_descent` | `Dialogues/wren.json` | Spoke to trusted Wren while holding `barrowlight`; she vows to follow you into the Under-Barrow (+5 `ashen_order` rep). |
| `knows_sigil_order` | `wren.json` / `ossuary_hall.json` | Learned the Sun -> Moon -> Skull order from Wren (her trust, or a Vesper Cantor hearing her litany as a tuning) or `[Religion DC 13]` on `bronze_door`. |
| `sigil_door_open` | `Rooms/ossuary_hall.json` | Completed the Sun -> Moon -> Skull puzzle; unlocks the `east` exit to `inner_crypt`. |
| `barrow_heart_resolved` | `Rooms/inner_crypt.json` | Defeated or parleyed with Ser Halric; reveals `barrowlight_lantern` feature. |
| `knight_laid_to_rest` | `Rooms/inner_crypt.json` | Parleyed with Ser Halric (+10 `ashen_order` rep, +100 XP, +1 `lantern_resonance`). |
| `knight_destroyed` | `Rooms/inner_crypt.json` | Defeated Ser Halric in combat (-1 `lantern_resonance`). |
| `read_stair_relief` | `Rooms/weeping_stair.json` | Passed `[History DC 11]` on the pre-Order relief; grants advantage on steadying Brother Orin. |
| `saw_stair_echo` | `Rooms/weeping_stair.json` | Shone the `barrowlight` on the Weeping Stair echo; grants advantage on Cantor Miriel's parley. |
| `met_orin` | `Dialogues/orin.json` | Spoke to Brother Orin on the Weeping Stair. |
| `steadied_orin` | `Dialogues/orin.json` | Passed `[Insight DC 11]` to help Brother Orin recall his own name (grants `toll_token`, +5 `lantern_bearers` rep, +1 `lantern_resonance`). |
| `orin_slipped` | `Dialogues/orin.json` | Failed `[Insight DC 11]` with Brother Orin; his fingers calcify further. |
| `rested_in_nocturne` | `Rooms/nocturne_square.json` | Rested by the Wayfarers' sanctuary brazier; grants advantage when asking Keeper Maeve to confide. |
| `inspected_beacon` | `Rooms/nocturne_square.json` | Passed `[Investigation DC 11]` on the sabotaged Nocturne Beacon. |
| `beacon_restored` | `Rooms/nocturne_square.json` | Restored the Nocturne Beacon with `choir_tuning_fork` and `beacon_lens` (+15 `lantern_bearers` rep, +150 XP, +1 `lantern_resonance`). |
| `met_maeve` | `Dialogues/maeve.json` | Spoke to Keeper Maeve in Nocturne Square. |
| `knows_miriel_grief` | `Dialogues/maeve.json` | Learned of Cantor Miriel's brother Jonas from Keeper Maeve; grants advantage on Miriel's parley. |
| `knows_garin_guilt` | `Dialogues/maeve.json` | Learned of Toll-Warden Garin's drowned patrol from Keeper Maeve; grants advantage on Garin's parley. |
| `maeve_rewarded` | `Dialogues/maeve.json` | Claimed Keeper Maeve's reward after restoring the Nocturne Beacon (+50 gold, `tallow_draught`, +10 `lantern_bearers` rep). |
| `met_peddler` | `Dialogues/peddler.json` | Spoke to Sallow the Peddler in Nocturne Square. |
| `audited_peddler` | `Dialogues/peddler.json` | Passed `[Investigation DC 11]` on Sallow's ledger (grants `grave_salt_bomb` and 10 gold). |
| `traded_with_peddler` | `Dialogues/peddler.json` | Bartered a `healing_potion` with Sallow for `wayfarer_brigandine` and `grave_salt_bomb`. |
| `met_wren_nocturne` | `Dialogues/wren_nocturne.json` | Reunited with Sister Wren in Nocturne Square (+5 `lantern_bearers` rep). |
| `knows_garin_oath` | `Dialogues/wren_nocturne.json` | Learned Toll-Warden Garin's history from Sister Wren; grants advantage on Garin's parley. |
| `echo_chapel_resolved` | `Rooms/echo_chapel.json` | Defeated or parleyed with Cantor Miriel; allows taking `choir_tuning_fork`. |
| `miriel_reconciled` | `Rooms/echo_chapel.json` | Parleyed with Cantor Miriel (+10 `lantern_bearers` rep, -5 `hollow_choir` rep, +135 XP, +1 `lantern_resonance`). |
| `miriel_defeated` | `Rooms/echo_chapel.json` | Defeated Cantor Miriel in combat (-15 `hollow_choir` rep, -1 `lantern_resonance`). |
| `challenged_miriel` | `Rooms/echo_chapel.json` | Climbed the pulpit steps to face Cantor Miriel; starts the `echo_chapel_choir` fight, which then resumes whenever the chapel is entered until it is resolved. |
| `shone_chapel_echo` | `Rooms/echo_chapel.json` | Shone the `barrowlight` on Jonas's echo in the Echo Chapel; grants advantage on Miriel's parley. |
| `released_choir_jars` | `Rooms/echo_chapel.json` | Unsealed the tethered spirit-jars in the Echo Chapel (+1 `lantern_resonance`, +5 `lantern_bearers` rep, -10 `hollow_choir` rep). |
| `preserved_choir_jars` | `Rooms/echo_chapel.json` | Wound the Hollow Choir's brass music boxes (+10 `hollow_choir` rep, -5 `ashen_order` rep, grants `vesper_bell_blade`). |
| `miriel_gift_given` | `Dialogues/miriel.json` | Spoke to reconciled Cantor Miriel and received `tallow_draught`. |
| `tollbridge_resolved` | `Rooms/drowned_tollbridge.json` | Defeated or parleyed with Toll-Warden Garin; allows taking `beacon_lens`. |
| `garin_relieved` | `Rooms/drowned_tollbridge.json` | Parleyed with Toll-Warden Garin (+10 `ashen_order` rep, +10 `lantern_bearers` rep, +130 XP, +1 `lantern_resonance`). |
| `garin_destroyed` | `Rooms/drowned_tollbridge.json` | Defeated Toll-Warden Garin in combat (-10 `ashen_order` rep, -1 `lantern_resonance`). |
| `challenged_garin` | `Rooms/drowned_tollbridge.json` | Stepped up to the toll-line to face Toll-Warden Garin; starts the `drowned_tollbridge_watch` fight, which then resumes whenever the bridge is entered until it is resolved. |
| `shone_bridge_echo` | `Rooms/drowned_tollbridge.json` | Shone the `barrowlight` on the floodgate echo at the Drowned Tollbridge; grants advantage on Garin's parley. |
| `garin_gift_given` | `Dialogues/garin.json` | Spoke to relieved Toll-Warden Garin and received `grave_salt_bomb`. |
| `rested_at_crossroads` | `Rooms/warren_crossroads.json` | Rested by the Wayfarers' sanctuary brazier at the Gate of the Twin Seals. |
| `read_vaelen_ledger` | `Rooms/whispering_stacks.json` | Passed `[Investigation DC 12]` on the Master Index Desk (`Entry 0: Lira, age seven`); grants advantage on Prelate Vaelen's parley. |
| `shone_stacks_echo` | `Rooms/whispering_stacks.json` | Shone the `barrowlight` on Vaelen's memory echo in the Whispering Stacks; grants advantage on Prelate Vaelen's parley. |
| `met_caelan` | `Dialogues/caelan.json` | Spoke to Archivist Caelan in the Whispering Stacks. |
| `knows_vaelen_lullaby` | `Dialogues/caelan.json` | Learned of Lira's frayed cylinder and lullaby from Archivist Caelan; unlocks and grants advantage on Prelate Vaelen's parley. |
| `slackened_choir_loom` | `Rooms/tallow_foundry.json` | Slackened the Great Acoustic Loom (+1 `lantern_resonance`, +10 `lantern_bearers` rep, -10 `hollow_choir` rep); grants advantage on Prelate Vaelen's parley. |
| `tuned_choir_loom` | `Rooms/tallow_foundry.json` | Tuned the Great Acoustic Loom (+10 `hollow_choir` rep, -5 `lantern_bearers` rep, -1 `lantern_resonance`, grants `silver_tuning_blade`). |
| `shone_foundry_echo` | `Rooms/tallow_foundry.json` | Shone the `barrowlight` on the Wax-Tallow Vat echo in the Tallow Foundry; grants advantage on Prelate Vaelen's parley. |
| `heard_seal_harmonic` | `Rooms/vow_sanctum.json` | Vesper Cantor listened to the Waxing Moon Reliquary's harmonic; grants advantage on Prelate Vaelen's parley. |
| `shone_sanctum_echo` | `Rooms/vow_sanctum.json` | Shone the `barrowlight` on Lira's echo in the Sanctum of the Unbroken Note; unlocks `"Repeat Lira's whispered plea"` in Prelate Vaelen's parley. |
| `challenged_vaelen` | `Rooms/vow_sanctum.json` | Stepped onto the chancel dais (`approach` on `lira_carillon`) to confront Prelate Vaelen; starts `archive_vaelen_boss`. |
| `vaelen_resolved` | `Rooms/vow_sanctum.json` | Defeated or parleyed with Prelate Vaelen; allows taking `moon_triad_seal`. |
| `vaelen_reconciled` | `Rooms/vow_sanctum.json` | Parleyed with Prelate Vaelen to release Lira's spirit (+15 `lantern_bearers` rep, +5 `hollow_choir` rep, +175 XP, +1 `lantern_resonance`). |
| `vaelen_destroyed` | `Rooms/vow_sanctum.json` | Defeated Prelate Vaelen in combat (-15 `hollow_choir` rep, -1 `lantern_resonance`). |
| `vaelen_gift_given` | `Dialogues/vaelen.json` | Spoke to reconciled Prelate Vaelen and received `choir_silks`. |
| `found_silas_lead_strip` | `Rooms/scoured_cloister.json` | Passed `[Investigation DC 12]` on the Chiseled Roll-Call Wall to salvage Silas's twelfth lead strip (`Corren — West Gate`); grants advantage on Commander Silas's parley. |
| `shone_cloister_echo` | `Rooms/scoured_cloister.json` | Shone the `barrowlight` on the Scoured Cloister echo; grants advantage on Commander Silas's parley. |
| `met_venn` | `Dialogues/venn.json` | Spoke to Armorer Venn in the Cloister of the Scoured Name. |
| `knows_silas_son` | `Dialogues/venn.json` | Learned of Commander Silas's son Corren and his twelve scourings from Armorer Venn; unlocks and grants advantage on Commander Silas's parley. |
| `quenched_lye_cistern` | `Rooms/lye_cistern.json` | Quenched the Cistern of White Lye (+1 `lantern_resonance`, +10 `lantern_bearers` rep, -10 `ashen_order` rep); grants advantage on Commander Silas's parley. |
| `shone_cistern_echo` | `Rooms/lye_cistern.json` | Shone the `barrowlight` on the Immersion Grate echo in the Cistern of White Lye; grants advantage on Commander Silas's parley. |
| `heard_sun_harmonic` | `Rooms/reliquary_bastion.json` | Vesper Cantor listened to the Unclouded Sun Reliquary's harmonic; grants advantage on Commander Silas's parley. |
| `shone_bastion_echo` | `Rooms/reliquary_bastion.json` | Shone the `barrowlight` on Silas's echo at the Furnace of Remission; unlocks `"Recite the oath Silas spoke before his twelfth scouring"` in Commander Silas's parley. |
| `challenged_silas` | `Rooms/reliquary_bastion.json` | Stepped up to the bastion floor (`approach` on `furnace_of_remission`) to face Commander Silas; starts `reliquary_silas_boss`. |
| `silas_resolved` | `Rooms/reliquary_bastion.json` | Defeated or parleyed with Commander Silas; allows taking `sun_triad_seal`. |
| `silas_relieved` | `Rooms/reliquary_bastion.json` | Parleyed with Commander Silas to stand him down (+15 `ashen_order` rep, +10 `lantern_bearers` rep, +175 XP, +1 `lantern_resonance`). |
| `silas_destroyed` | `Rooms/reliquary_bastion.json` | Defeated Commander Silas in combat (-15 `ashen_order` rep, -1 `lantern_resonance`). |
| `silas_gift_given` | `Dialogues/silas.json` | Spoke to relieved Commander Silas and received `scoured_halberd`. |

### Counters (`counter`)
| Counter ID | Used In | Meaning |
| :--- | :--- | :--- |
| `sigil_seq` | `Rooms/ossuary_hall.json` | Tracks bronze door progress (`0` = start, `1` = sun pressed, `2` = moon pressed; resets to `0` on trap trigger). |
| `lantern_resonance` | `Rooms/inner_crypt.json` / Act I & II rooms / `game.json` | Tracks the moral/philosophical resonance of the Barrowlight (`+` for mercy/release, `-` for destruction). |

### Quests (`questStage`)
- `barrowlight`: `10` (Entered Lichgate) -> `20` (Entered Ossuary Hall) -> `30` (Opened bronze door) -> `40` (Resolved Barrow Heart encounter) -> `50` (Claimed the Barrowlight & unsealed the spiral stair) -> `100` (Descended into the Under-Barrow).
- `nocturne_beacon`: `10` (Descended the Weeping Stair) -> `20` (Reached Nocturne Square and learned of the sabotaged beacon) -> `30` (Recovered the first missing beacon component) -> `40` (Recovered both `choir_tuning_fork` and `beacon_lens`) -> `100` (Restored the Nocturne Beacon and crossed the Drowned Tollbridge).
- `twin_seals`: `10` (Reached the Gate of the Twin Seals at `warren_crossroads`) -> `20` (Recovered the first Triad Seal) -> `30` (Recovered both `moon_triad_seal` and `sun_triad_seal`) -> `100` (Unsealed the Twin-Seal Threshold Gate into the Cradle of the First Architect).

### Backgrounds (`background`)
- `bereaved_kin`, `ash_deserter`, `moor_bailiff`, `fever_dreamer` (see section 5). Keepsakes: `tarnished_locket`, `scored_signet`, `elders_writ`, `bell_splinter` (`Items/quest_items.json`).


