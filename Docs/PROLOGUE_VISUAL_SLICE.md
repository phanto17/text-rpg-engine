# Prologue visual slice

The Upper Threshold now has a complete initial art set: Lichgate, Ossuary Hall, Barrow Heart, Ser Halric, the barrow thrall, and Sister Wren. All room images are 304×140, enemies use 48×48 source frames, and Wren's portrait is 56×56. The new room paintings and thrall come from `Tools/ArtPipeline/build_prologue.py`; their manifest entries and decoded pixel parity are checked in CI. The earlier PXO masters remain the editable source for Lichgate, the static knight and Wren.

## Interactive art

The Ossuary painting leaves the central portal clear. The renderer draws the bronze door, sigil highlights and stepped brazier flames above it. `bindings.sigilDoorOpenFlag` refers to the existing `sigil_door_open` flag in presentation data; opening the passage changes the painting's live overlay even though the puzzle counter remains at two. The same overlay code runs over procedural fallback architecture. The Barrow Heart painting leaves the plinth lantern to the renderer, which removes it when the player carries the bound lantern item. No new gameplay flag or save migration is needed.

## Typography and motion

Silkscreen headings now draw with one stroke, preserving the small glyphs' internal gaps. Long commands keep their editable tail and caret inside the input panel. Oversized centered rules are fitted to the log width; truncated portrait epithets show an ellipsis. Layout uses the rendering font's metrics. The existing whole-pixel scaling remains the default.

Combat uses a 600 ms sequence: 160 ms anticipation, an 80 ms stepped strike, a 120 ms impact hold, and 240 ms recovery. The dice verdict and narration finish before the motion begins; the damage or miss cue starts at 240 ms. Headless tests verify held integer poses and cue ordering. The knight's four idle poses hold for 200 ms each; the thrall's hold for 250 ms, with only a one-pixel sway. Lantern and brazier flicker are sampled at four steps per second.

## Reproduce visual review

Run from the repository root. Use fresh recording and GIF paths on each run.

```powershell
python Tools/ArtPipeline/build_prologue.py ArtBuild/prologue-drafts
python Tools/ArtPipeline/validate_assets.py Data/Assets
python Tools/ArtPipeline/verify_reproducible.py
dotnet run --project src/RpgEngine.Pixel -- --seed 42 --play "new;Maximilian Ashen-Warden;1;1;1;1;1;2;7;1;down;press sun;press moon" --screenshot ArtBuild/door-two.png
dotnet run --project src/RpgEngine.Pixel -- --seed 42 --play "new;Tester;1;1;1;1;1;2;7;1;down;press sun;press moon;press skull" --screenshot ArtBuild/door-open.png
dotnet run --project src/RpgEngine.Pixel -- --seed 42 --crt --play "new;Tester;1;1;1;1;1;2;7;1;talk wren" --screenshot ArtBuild/wren-crt.png
dotnet run --project src/RpgEngine.Pixel -- --seed 42 --record ArtBuild/combat-frames --play "new;Tester;1;1;1;1;1;2;7;1;down;press sun;press moon;press skull;east;attack Ser Halric;attack Ser Halric"
python Tools/ArtPipeline/make_preview.py ArtBuild/combat-frames ArtBuild/combat.gif --last-seconds 20
```

Inspect native frames and 2× screenshots for text overlap, portrait readability, clear enemy silhouettes, two lit sigils, an open portal, and the lantern's presence. Capture CRT both enabled and disabled; contributor captures now include the final CRT pass. The recording uses native canvas frames at 12 fps and a fixed 60 Hz presentation clock, preserving narration and combat holds. It refuses an existing frame directory and stops after two minutes if its script has not settled. It does not record gameplay input from a live user.

Run `dotnet build -c Release`, `dotnet test`, the Python tooling tests, and art validation before committing. Revert PNG/recipe/manifest changes together to roll back art; remove a backdrop path to exercise its procedural fallback. Reverting the optional open-door binding returns its neutral closed state. Audio is outside this visual milestone.
