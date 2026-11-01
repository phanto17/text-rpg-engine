# Bell nave and final cradle visual slice

This slice completes the shipped image coverage: paintings for all eighteen rooms, idle sheets for all twelve monsters, and portraits for all fourteen dialogue appearances (some reuse an established portrait). The full pack contains 46 PNGs, including the original static pilots, tile and UI icon, plus 23 WAVs. `PresentationAssetsTests` requires every shipped room, monster and NPC look to name an existing PNG. Optional custom content retains the missing-image fallback.

## Assets and composition

`Tools/ArtPipeline/build_finale.py` is the editable original source for two 304x140 paintings, two 192x48 horizontal idle sheets and a 56x56 portrait. The manifest records the established 16-color palette, binary alpha and source dimensions. Recipe exports can be imported into Pixelorama; they do not claim PXO masters.

The nave uses towering basalt ribs, cold salt banks and a central hanging green-bronze bell. Its empty rim sockets remain empty even while the player carries seals. The cradle uses an octagonal dais, bedrock ribs and an alabaster bier. Aurelius wears a bronze astrolabe and calcified armor; the warden carries a plumb weight. Their four-pose idle cycles hold each frame for 250 ms and 300 ms respectively, with only one pixel of movement. The portrait retains Aurelius's tired face, salt crust and astrolabe motif.

`FinaleDetails` draws the bell, brazier and bier over both paintings and procedural backgrounds. Fitting the splinter repairs the triangular lip notch, leaving the crown-to-lip fracture visible. Only the complete Triad resolves the fracture. Fire and bell motion sample time at four steps per second, independent of gameplay randomness.

## Ending bindings

The existing flags resolve through renderer-free `FinaleState`; no story IDs are embedded in rendering code. No flags, quests, combat balance, RNG draws or save formats change.

| Binding | Shipped reference | Visible result |
|---|---|---|
| `bellMendedFlags` | `fitted_bell_splinter` | Gold lip patch; main fracture persists |
| `sunEndingFlag` | `ending_sun_stasis` | Gold/ivory fire and a rigid halo; amber remains over the occupied bier |
| `moonEndingFlag` | `ending_moon_vigil` | Silver-violet fire; amber and occupant remain |
| `triadEndingFlag` | `ending_triad_passage` | Warm passage fire; amber and occupant disappear |

Missing ending bindings keep a neutral state. The existing `brazierLitFlags` still enables generic warm fire for custom content. Conflicting ending flags resolve in the stable order Triad, Moon, Sun. The ending tests cover independent flags, defaults, conflicts and malformed references; regenerated schemas describe the optional fields.

## Spoken portrait lifetime

Aurelius's post-confrontation dialogue has no choices, so Core returns to exploration before its text is displayed. `EventSequencer.IsSpeaking` now exposes the active spoken beat; the renderer keeps the portrait through that beat, then hides it after completion or skipping. This also fixes other choice-free dialogue. Tests cover cue draining, natural completion, subsequent narration, skipping and clearing. Record this conversation to inspect the portrait during speech; an idle still capture correctly shows the room again.

## Ambience

`sundered_overtones.wav` uses low dissonant harmonics; `cradle_breath.wav` combines a low octave and filtered air. Both are original eight-second PCM mono 22050 Hz / 16-bit loops, recorded in `ArtSource/audio.json` and reproduced by the integer synthesis recipes. They use the existing room crossfade and volume settings. Automated format, headroom and loop-seam checks pass; subjective headphone/speaker listening remains part of the release mix review. Visual captures contain no audio.

## Reproduce and review

```powershell
python Tools/ArtPipeline/build_finale.py ArtBuild/finale-drafts
python Tools/ArtPipeline/validate_assets.py Data/Assets
python Tools/ArtPipeline/verify_reproducible.py
python -m unittest discover -s Tools/ArtPipeline/tests -v
python Tools/AudioPipeline/validate_audio.py Data/Assets/Audio --reproduce
dotnet run --project src/RpgEngine.CLI -- --export-schemas
dotnet build -c Release
dotnet test
```

Use isolated slots beneath ignored `ArtBuild/`: a nave slot before and after fitting the splinter, a cradle slot before the confrontation, and a resolved/reconciled slot with both seals. Prepared saves isolate these visual states; deterministic Act III scenario tests separately verify progression and endings.

```powershell
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/finale-review/bell --seed 42 --play "load quicksave;help" --screenshot ArtBuild/finale-bell.png
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/finale-review/cradle --seed 42 --record ArtBuild/finale-combat-frames --play "load quicksave;approach brazier"
python Tools/ArtPipeline/make_preview.py ArtBuild/finale-combat-frames ArtBuild/finale-combat.gif --last-seconds 15
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/finale-review/resolved --seed 42 --record ArtBuild/finale-portrait-frames --play "load quicksave;talk aurelius"
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/finale-review/sun --seed 42 --play "load quicksave;relight brazier" --screenshot ArtBuild/finale-sun.png
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/finale-review/moon --seed 42 --crt --play "load quicksave;tune brazier" --screenshot ArtBuild/finale-moon.png
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/finale-review/triad --seed 42 --play "load quicksave;complete brazier" --screenshot ArtBuild/finale-triad.png
```

Do not append a command after an ending: further input returns to the title. Inspect native size and 2x, CRT on/off, title contrast, the persistent bell crack after fitting, both combat silhouettes, the empty Triad bier and the occupied Sun/Moon bier. Repeat key views with an ignored Data copy without the two room PNGs. Keep actual game saves and shipped assets untouched during review.

The local review recorded 343 combat frames (about 29 seconds) and 285 portrait frames (about 24 seconds), both at native 480x270 / 12 fps. The combat recording includes both enemy sheets; the portrait recording confirms Aurelius remains visible while his farewell types out. Sun, Moon and Triad were triggered through their real ending commands from isolated resolved saves; the Moon capture uses CRT. Nave captures compare the unfilled and fitted notch. Separate missing-backdrop captures confirm the nave bell and Triad fire/empty bier remain visible on procedural scenes. The Release build reports zero warnings/errors, all 1,049 tests pass, the eight Python tooling tests pass, and all 46 PNGs / 23 WAVs validate and reproduce.

## Rollback and next review

Revert the five PNGs, recipe, manifest and monster timing together. Revert the two WAVs, synthesis recipes, audio manifest and room ambience entries together. Removing optional image paths restores procedural scenes; live details remain. If removing the three ending bindings, revert the records, loader, generated schema and tests together. No save migration is needed. A rollback to missing shipped assets must also revert the shipped-image completeness test.

The next milestone is a whole-game presentation review: check visual consistency from prologue through all endings, review the audio mix by ear, and collect release-ready screenshots and a short trailer. Additional artwork should address findings from that review.
