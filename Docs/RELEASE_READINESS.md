# Release-readiness evidence and capture pack

Baseline: PR #17 merged at `9109e78b3a979d3683f5b65b33a701af3310166e`, content version 1.0.1. This milestone prepares local review material and records remaining release gates. It does not publish a release or assert complete platform/mix approval.

## Current status

| Gate | Evidence | Status |
|---|---|---|
| Release build and deterministic tests | Zero warnings/errors; 1,053 tests pass | Passed locally |
| Python tooling | 25 tests, including nine capture-pack tests | Passed locally |
| Runtime asset checks | 46 PNGs and 23 WAVs validate and reproduce | Passed locally |
| Native capture selection | Six current-build recordings, selected frame ranges inspected | Passed locally |
| Capture provenance | Selected PNG hashes, input-save fingerprints, script/seed and baseline recorded | Passed locally |
| Teaser assembly | 25 seconds / 300 selected frames; ending exported separately | Passed locally |
| Fullscreen and aspect-fit input mapping | F2, F3 and F11 verified in the Windows client. Mouse actions mapped in both scaling modes and Nocturne page two survived the fullscreen round trip. | Passed on the tested display |
| Display scaling and multiple monitors | Observed a 1282x752 window surface and 2560x1440 borderless output. Physical DPI settings and other monitors remain unverified. | Pending |
| Native Linux window/audio behavior | Requires a graphical Linux session; headless CI is a separate check | Pending |
| Subjective headphones/speaker mix | Follow the existing presentation-review checklist | Pending |

The earlier presentation review verified mouse action paging, bracket navigation and a page-two numeric shortcut. The new recordings also show title, dialogue, combat, exploration and an ending at the native 480x270 canvas. They do not replace interactive checks for creation, shop, sound settings, fullscreen transitions or all display configurations.

## Selected shots

The local pack was assembled from retained recordings under ignored `ArtBuild/release-18/`. Every selected clip is five seconds at 12 fps. Indices below refer to sorted, recorded PNGs, starting at zero; stop is exclusive. They are not the 60 Hz numbers embedded in the filenames.

| Shot | Recording directory | Start | Stop | Fixture | Teaser |
|---|---|---:|---:|---|---|
| Lichgate rain/title | `frames/moor` | 14 | 74 | Title screen | Included |
| Sister Wren | `frames/wren_retry` | 192 | 252 | Prepared save at Lichgate | Included |
| Barrow Heart combat | `frames/combat` | 300 | 360 | Prepared save; enter crypt and attack | Included |
| Nocturne lamps | `frames/nocturne` | 223 | 283 | Prepared save at Nocturne | Included |
| Loom state change | `frames/loom_retry` | 140 | 200 | Prepared save; successful `sabotage loom` | Included |
| Final brazier | `frames/ending` | 516 | 576 | Resolved/reconciled prepared save; `complete brazier` | Separate spoiler clip |

The selected combat range shows a real player attack and subsequent enemy response; it excludes the later death in that source recording. The loom range shows taut wires becoming slack after the real interaction. Wren's portrait and dialogue choices remain visible. The source recordings are independent scene checks, not an uninterrupted playthrough. Ending footage is excluded from the main teaser by the reviewed spoiler flag.

Two initial attempts were rejected: a Wren recording used the wrong room and timed out, and a loom script used a verb the content does not define. Only the corrected recordings appear in the manifest. Source selection is reviewed by eye; successful file encoding alone does not approve a shot.

## Assemble and reproduce

```powershell
# Existing contributor capture workflow; each recording folder must be fresh.
dotnet build -c Release
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/release-18/saves/wren --seed 42 --record ArtBuild/release-18/frames/wren_retry --play "load quicksave;talk wren;;;;;;;;;;;;;;;;;;;;;;;;;"
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/release-18/saves/loom --seed 42 --record ArtBuild/release-18/frames/loom_retry --play "load quicksave;sabotage loom;;;;;;;;;;;;;;;;;;;;;;;;;"

# With reviewed source frames and their retained initial save files:
python Tools/ArtPipeline/capture_pack.py ArtBuild/release-18/shots.json ArtBuild/release-18/fresh-pack
```

`Tools/ArtPipeline/capture-shots.example.json` records the reviewed six-shot selection. Copy it beside the `frames/` and `saves/` folders to use those paths. For newly made recordings, inspect the scene and motion before selecting frame ranges; their exact timing depends on narration and initial state. Keep the original initial saves, not a replacement saved after the interaction. Existing output folders are refused.

The assembler uses Pillow already pinned for the art tools. It opens no window or audio device, launches no processes and never executes the manifest's game scripts. It emits:

- `teaser.gif`: selected non-spoiler shots in manifest order, with ten-millisecond GIF ticks distributed to preserve 12 fps timing.
- `<shot>.gif`: individual clips, including explicitly classified spoilers.
- `<shot>.png`: the first selected frame with its captured pixels preserved.
- `capture-receipt.json`: declared baseline/content version, seed, script and fixture classification; hashes of every selected PNG and each required prepared-save input.
- `index.html`: an offline gallery linking the teaser, clips and receipt. Clips play when opened; they do not autoplay in the gallery.

GIF previews use 128-color quantization. PNG posters retain the source image pixels. Captures are silent; this is a review teaser, not an encoded trailer with a soundtrack.

## Manifest contract and limits

Manifest version 1 requires a non-empty `baselineCommit`, `contentVersion`, non-negative integer `seed` and 1–12 `shots`. Each shot declares `id`, `label`, `recording`, `start`, `stop`, `spoiler`, `fixture` and `script`. IDs are short snake_case names. Fixture classifications are `title`, `prepared-save` or `playthrough`; prepared saves also require the relative `save` file path.

Each source directory must contain 1–1440 contributor PNG filenames, contiguous from `frame_000005.png` in steps of five. Missing frames are rejected rather than silently changing timing. Select 1–120 native 480x270 frames per shot, at most 360 non-spoiler frames (30 seconds) and 720 selected frames overall. At least one shot must be non-spoiler. Duplicate IDs, invalid ranges, scaled images and paths escaping the manifest directory are rejected. Labels are escaped in HTML. Missing fixture/spoiler declarations cannot silently produce a teaser. A failed run may leave a partial output folder, without a completed receipt.

Spoiler flags and baseline metadata are reviewer declarations. File hashes establish which source files were used; the assembler does not independently prove the running game's Git revision, initial save validity or narrative spoiler classification. Preserve the source captures and saves alongside the input manifest to reproduce an assembly. Do not upload personal saves with a public capture pack.

When changing this manifest contract, update the assembler, example, tests and this guide together. The ordinary single-recording `make_preview.py` workflow uses the same timing helper.

## Remaining release checks

Brief automated taps of F2, F3 and F11 left the display unchanged on the PR #18 baseline. The renderer now reads these shortcuts from Raylib’s press queue. On the rebuilt Windows client, F3 filled the window, F2 drew visible scanlines, and F11 entered borderless fullscreen. Clicking Continue and Next worked in aspect-fit mode. After toggling back, the same Nocturne session and page two remained visible. Clicking the first exit in whole-pixel mode reached the Weeping Stair and reset the action page. The Sound panel also opened by mouse in the baseline session and displayed the default 45% effects and 20% ambience settings.

Repeat representative UI checks across display scaling settings and monitors. Record the dimensions and OS scaling used. Creation, shop and audio preference persistence were outside this display check.

Repeat representative checks in a native Linux graphical session. CI does not open a window or audio device. Retain the audio listening checklist in `PRESENTATION_REVIEW.md`, including two loop repeats, room crossfades, effects against ambience, and mute/preference recovery on headphones and speakers. Keep the current mix until recorded listening findings justify a change.

## Validation and rollback

```powershell
dotnet build -c Release
dotnet test -c Release --no-build
python -m unittest discover -s Tools/ArtPipeline/tests -v
python Tools/ArtPipeline/validate_assets.py Data/Assets
python Tools/ArtPipeline/verify_reproducible.py
python Tools/AudioPipeline/validate_audio.py Data/Assets/Audio --reproduce
```

Capture tests cover spoiler exclusion, separate extras, source/save hashes, exact poster pixels, timing drift, existing-output preservation, invalid paths/ranges, missing source frames or declarations, scaled-frame rejection and the requirement for a non-spoiler teaser. Local artifact verification additionally checked every selected hash, all six five-second clip durations, the 25-second teaser and all gallery links.

Rollback the assembler, its example, tests, shared timing-helper extraction and guide links together. Generated captures remain disposable under ArtBuild. No game data, schema, asset manifest, dependency or save migration is required.
