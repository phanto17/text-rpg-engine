# Act III entrance and sanctuary visual slice

This slice adds 304x140 paintings for the Stair of the Uncarved Skull and Vigil of the Last Watch, plus a 56x56 portrait for Barrow-wright Oakhaven. Wren's cradle dialogue reuses her established portrait. These two rooms have no combat encounters, so this slice adds no enemy sprites.

The original editable source is `Tools/ArtPipeline/build_sanctuary.py`, tracked in the art manifest and reproduction checks. It uses the established 16-color palette and binary alpha. Import recipe exports into Pixelorama for inspection; these assets do not claim PXO masters. The approved target remains the existing dark pixel-art family and source dimensions in the style guide.

## Composition and live details

The stair uses vitrified basalt faces, broad descending wedges and bronze Triad panels. Its third sigil remains visibly lead-filled. The landing uses a dark abyss, petrified aqueduct ribs, sitting-stones, Oakhaven's crates and a narrow causeway. No named figure is painted into either room.

`SanctuaryDetails` draws the relief, brazier and tokens over painted and procedural scenes. The brazier uses the established four-step-per-second motion. A faint memory silhouette appears on the stair while the player carries the bound lantern item. Existing story references resolve through the headless `SanctuaryState` snapshot:

| Binding | Shipped reference | Visible result |
|---|---|---|
| `friezeReadFlag` | `read_architect_frieze` | Brighter inscription grooves below the lead plug |
| `sanctuaryChimeFlag` | `vaelen_reconciled` | Silver chime on the stair-post |
| `sanctuaryDispatchFlag` | `silas_relieved` | Dispatch tablet and folded standard beside the brazier |
| `sanctuaryOfferingFlag` | `tithed_at_threshold` | Coin in the offering bowl |

Studying the relief reconstructs its meaning without replacing the lead with a Skull. The ally tokens remain visible after their testimony is witnessed, matching the room variants. Missing bindings keep the unread/bare state. No gameplay flags, balance, RNG draws or save format change.

Room captions are suppressed while a portrait is visible, preventing overlap with speaker titles in dialogue and shops. Their lifetime still advances normally; the room-description completion and two-second hold are unchanged.

## Ambience

`uncarved_depths.wav` and `last_watch_embers.wav` are original eight-second PCM mono 22050 Hz / 16-bit loops from the checked-in integer synthesis recipes and audio manifest. Low harmonics and circular filtered noise distinguish the cold descent from the warmer sanctuary. They use existing volume/mute settings and the half-second room crossfade. Automated checks cover format, headroom, loop seams and reproduction; subjective listening remains part of mix review.

## Reproduce and inspect

```powershell
python Tools/ArtPipeline/build_sanctuary.py ArtBuild/sanctuary-drafts
python Tools/ArtPipeline/validate_assets.py Data/Assets
python Tools/ArtPipeline/verify_reproducible.py
python Tools/AudioPipeline/validate_audio.py Data/Assets/Audio --reproduce
python -m unittest discover -s Tools/ArtPipeline/tests -v
dotnet build -c Release
dotnet test
```

Use isolated review saves beneath ignored `ArtBuild/`. Save a stair slot before studying the relief and a landing slot before offering coin; retain a second landing slot with the allied story flags. Example contributor commands after saving slots named stair and landing:

```powershell
dotnet run -c Release --project src/RpgEngine.Pixel -- --saves ArtBuild/sanctuary-review --seed 42
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/sanctuary-review --play "load stair;study relief;help" --screenshot ArtBuild/studied-relief.png
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/sanctuary-review --play "load landing;talk oakhaven;help" --screenshot ArtBuild/oakhaven.png
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/sanctuary-review --play "load landing;talk oakhaven;1;help" --screenshot ArtBuild/oakhaven-shop.png
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/sanctuary-review --crt --play "load landing;tithe bowl;help" --screenshot ArtBuild/offering.png
```

The final `help` skips pending narration in still captures. Studying the relief requires a successful History check; an offering requires ten gold. Compare unread/studied panels, bare/allied landings and empty/occupied offering bowls. Check Oakhaven in dialogue and shop, and Wren when her descent flag is set. Inspect native size and 2x scale, CRT on/off, title readability and the steady fire. Repeat key views with an ignored Data copy without the two new room PNGs, keeping shipped files intact. Prepared saves isolate visual states; deterministic Act III scenario tests separately verify progression.

For motion, use a fresh output folder with `--record ArtBuild/sanctuary-frames --play "load stair;down;rest brazier;tithe bowl"`, then compile with `python Tools/ArtPipeline/make_preview.py ArtBuild/sanctuary-frames ArtBuild/sanctuary.gif --last-seconds 20`. Captures contain no audio.

The local prepared-save traversal produced 422 native frames at 12 fps (about 35 seconds). The separate native offering check spent the ten gold and showed the coin and resonance change. Scenario tests separately cover the complete story path.

## Rollback and next slice

Revert paintings, portrait, recipe and art manifest together; retain the reused Wren path or revert it separately. Revert WAVs, synthesis, manifest and ambience bindings together. Removing backdrop paths restores procedural scenes with live details. Removing the four state bindings returns unread/bare defaults; revert record, loader, schema and tests together if removing those fields. No save conversion is needed.

Next are the Nave of the Sundered Bell and Cradle of the First Architect: two paintings, the last two enemy sheets and Aurelius's portrait, with live bell, brazier and ending-state review.
