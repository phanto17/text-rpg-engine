# Whole-game presentation review

Review baseline: merged finale slice (`de02f27`), content version 1.0.1. This review compares all eighteen shipped rooms, twelve monster sheets and fourteen NPC/dialogue looks. The source pack remains 46 PNGs and 23 WAVs. No new runtime assets, story flags, rules, packages or save format changes are introduced.

## Findings and resulting change

Room silhouettes, palette roles and source scale form a consistent family: cold moor/crypt architecture, warmer Nocturne lamps, fine Archive wires, rigid Reliquary ranks, then broad basalt ribs and bronze in the finale. Live details remain distinct from the static paintings. Complete figure sheets retain the same baseline, limited movement and source dimensions. These are native-scale visual observations, not proof of every story-state combination; the earlier slice guides retain their state-specific capture notes.

The concrete usability finding was action overflow. Nocturne, the Last Watch and other busy rooms hid choices behind a generic “type a number” message without displaying those choices or their numbers. The action bar now shows Prev/Next, a page counter and at most nine shortcuts per page. Bracket keys `[` and `]` change pages when the command input is empty. Number keys 1–9 select the visible page; mouse clicks use the same action mapping. Brackets and digits remain ordinary text during name entry or while a command is being typed. A changed set of labels resets to the first page. Page turns invalidate old click rectangles.

`ButtonFlow.Pages` and `ButtonPage` keep layout and shortcut mapping renderer-free. Tests require complete, ordered coverage without duplicated or missing actions, rectangles within bounds, a nine-button cap and rejection of an area too small for a button. The existing `PixelGame.Pick` and contributor `--play` numeric lines still use full-list indices; script indices are independent of displayed page-local shortcuts.

Native Windows verification at the default 1280x720 window / 2x canvas scale covered Nocturne's mouse Next button, keyboard backward/forward paging, and shortcut 1 on page two. That shortcut selected the visible “go up” action and reached the Weeping Stair. The first page then reset correctly. The eighteen-room capture set was refreshed after the paging change. Captures use isolated prepared saves; they do not replace the deterministic progression and balance tests.

## Offline review pack

The assembler uses existing Pillow and the Python standard library. It opens no game window, browser, editor or audio device and adds no runtime dependency:

```powershell
python Tools/ArtPipeline/review_pack.py Data ArtBuild/review-pack --captures ArtBuild/review-shots
```

Use a fresh output directory. For each room, supply a native 480x270 or 2x 960x540 screenshot named after its backdrop file, for example `lichgate.png`. The tool creates:

- `index.html`: offline room gallery, individual looping ambience controls and effect previews.
- `rooms.png`: three-column contact sheet of native scene panels, including runtime overlays when captures are supplied.
- `figures.png`: all monster frames and NPC portraits, including reused portraits under each presentation key.
- `review.json`: image inventory, capture availability, frame counts/timing and audio measurements.
- `media/`: gallery images and audio previews at the default 20% ambience / 45% effects amplitude.

The pack uses the shipped object-form, strict JSON presentation file. Missing captures are labelled **source only**, never native-verified. Images must have the expected dimensions; audio must be PCM mono 22050 Hz / 16-bit and at most thirty seconds. Relative asset paths must resolve inside the nominated Data directory. Data labels are HTML-escaped. Existing review outputs cannot be overwritten, and source assets are never modified. A failed run may leave a partial directory for diagnosis; start another run in a fresh folder.

To generate a source-only pack without native captures:

```powershell
python Tools/ArtPipeline/review_pack.py Data ArtBuild/source-review
```

For local room captures, use a valid saved character in an isolated folder and the contributor flags. A restored save re-describes its current room without rerunning entry effects. Prepared review copies may change `currentRoom` and visited-room state to isolate a composition; record those edits as review fixtures, not gameplay evidence. Keep personal save files untouched.

```powershell
dotnet build -c Release
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/review-saves --seed 42
# Save a slot named nocturne during play, then capture it without adding unrelated help text:
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/review-saves --seed 42 --play "load nocturne;;;;;;;" --screenshot ArtBuild/review-shots/nocturne_square.png
```

Empty scripted lines skip queued text. They leave the scene and action list in place, with time for the room fade. Contributor scripts can still select full-list numeric actions as before. Native capture requires a graphical desktop and belongs outside unit tests and CI.

## Audio evidence and remaining listening

All eighteen rooms have ambience and the five effect roles have clips. Source ambience RMS ranges from -37.51 dBFS (Archive ticks) to -21.74 dBFS (Barrow resonance); the highest ambience sample peak is -12.25 dBFS. The large RMS difference includes intentionally sparse ticks versus sustained tones. It is a listening-review item, not a reason to automatically normalize the pack. Preview gain preserves those differences and leaves the shipped files unchanged.

The report measures sample peak, RMS, duration and the endpoint step across a loop. These are not integrated loudness or true-peak measurements. The gallery repeats individual loops; browser playback is not a substitute for the client's room crossfade, cue cooldowns, device behavior and actual output levels.

Subjective audio approval remains **pending**, explicitly recorded in every fresh pack. On headphones and speakers, check:

- Two or more repeats of each ambience loop for seam clicks, beating and fatigue.
- Quiet Archive ticks against sustained bass rooms, without changing the output volume between them.
- Adjacent-room half-second crossfades in the client, with effects at their default level.
- Selection, steps and combat impacts during narration, plus mute and saved-volume recovery.

Record output device, volume settings, rooms checked and concrete findings before tuning. No listening claim is inferred from file validation or successful device initialization.

## Validation and rollout

```powershell
dotnet build -c Release
dotnet test -c Release --no-build
python -m unittest discover -s Tools/ArtPipeline/tests -v
python Tools/ArtPipeline/validate_assets.py Data/Assets
python Tools/ArtPipeline/verify_reproducible.py
python Tools/AudioPipeline/validate_audio.py Data/Assets/Audio --reproduce
```

The local Release build reports zero warnings/errors and all 1,053 tests pass. All sixteen Python tooling tests pass, and all 46 PNGs / 23 WAVs validate and reproduce. The local review pack includes all eighteen native room views, all twelve sheets and all fourteen NPC looks. Eight new pack tests cover native/source-only labelling, output preservation, default-gain audio without normalization, unsafe paths, HTML escaping, invalid capture sizes, malformed sheets and unsupported PCM. CI runs these with the existing tooling tests on Windows and Ubuntu without opening a window or device.

MVP is the action-accessibility fix and reproducible review pack. Follow with the recorded listening review and any justified mix adjustments. Release capture work then selects a short sequence of room, dialogue, combat and ending clips for a trailer; final high-DPI, fullscreen and native Linux window checks remain separate release tasks. No trailer or all-platform visual sign-off is claimed by this review.

Rollback the action-page files and control/help documentation together to restore the previous action bar. Remove the offline assembler, its tests and guide links independently if no longer needed. Game saves, content, asset manifests and generated schemas need no migration or regeneration.

The subsequent [release capture pack](RELEASE_READINESS.md) assembles six selected native clips, with the final brazier excluded from its 25-second teaser. It retains source/fixture fingerprints and explicitly leaves unconfirmed display, Linux-native and subjective listening gates pending.
