# Archive of Unkept Vows visual slice

This slice adds 304x140 paintings for the Whispering Stacks, Tallow-Spindle Loom and Vow Sanctum; 56x56 portraits for Archivist Caelan and Prelate Vaelen; and four-pose horizontal 48x48 sheets for Vaelen and the tethered chorister. Vaelen holds each pose for 250 ms, the chorister for 300 ms, with one-pixel sway. Existing stepped strikes and damage timing remain unchanged.

The editable sources are the original recipes in `Tools/ArtPipeline/build_archive.py`, with entries in `ArtSource/pilots.json`. Art uses the established 16-color palette and binary alpha. Import PNGs into Pixelorama for inspection; these recipe assets do not claim PXO masters.

## Composition and live state

The stacks use shale shelves, vellum spines and brass cylinders around the scribe's desk. The foundry uses a broad loom frame, copper vats and a dark water channel. The sanctum uses converging silver wires and a stepped dais around the central brass cradle. Its presentation accent uses the source palette's tarnished gold so the frame reads as gilded metal. No named figure is painted into a room backdrop.

`ArchiveDetails` draws moving wires, loom threads, low vat lamps and the central carillon above both painted and procedural backgrounds. Motion samples at four steps per second. Slack threads hang still; intact threads keep a restrained vibration. The occupied cradle holds a small spirit silhouette, with a one-pixel sway while active. Stopped clockwork leaves the figure still. Release opens the lid and empties the cradle.

The optional presentation bindings resolve into the headless `ArchiveState` snapshot:

| Binding | Shipped reference | Visual result |
|---|---|---|
| `loomSlackFlag` | `slackened_choir_loom` | Hanging slack threads |
| `carillonStoppedFlag` | `vaelen_resolved` | Clockwork and captive spirit stop moving |
| `carillonReleasedFlag` | `vaelen_reconciled` | Empty cradle with unsealed lid; also stops clockwork |

These references already exist in the canonical story registry. The stopped and released states remain distinct, preserving the difference between victory and reconciliation. Missing bindings keep the default active state. No gameplay flags, balance, RNG draws or save format change.

## Ambience

Three original eight-second PCM mono 22050 Hz / 16-bit loops accompany the rooms: `archive_ticks.wav`, `tallow_simmer.wav` and `sanctum_air.wav`. Integer synthesis recipes and seeds live in the audio pipeline and manifest. The sanctum uses a low air tone rather than a recurring lullaby, so the ambience does not keep Lira singing after release. Room loops retain the existing half-second crossfade and saved volume controls. Automated checks verify format, loop seams and exact reproduction; subjective listening remains part of mix review.

## Reproduce and inspect

```powershell
python Tools/ArtPipeline/build_archive.py ArtBuild/archive-drafts
python Tools/ArtPipeline/validate_assets.py Data/Assets
python Tools/ArtPipeline/verify_reproducible.py
python Tools/AudioPipeline/validate_audio.py Data/Assets/Audio --reproduce
python -m unittest discover -s Tools/ArtPipeline/tests -v
dotnet build -c Release
dotnet test
```

Use isolated saves beneath ignored `ArtBuild/`. Save before approaching Vaelen, after resolving her by steel, and after reconciliation. Capture the loom both intact and slack. Example contributor commands after saving slots named stacks, loom, sanctum and released:

```powershell
dotnet run -c Release --project src/RpgEngine.Pixel -- --saves ArtBuild/archive-review --seed 42
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/archive-review --play "load stacks;talk caelan" --screenshot ArtBuild/caelan.png
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/archive-review --play "load loom;help" --screenshot ArtBuild/loom.png
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/archive-review --crt --play "load sanctum;approach carillon;help" --screenshot ArtBuild/vaelen-combat.png
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/archive-review --play "load released;talk vaelen" --screenshot ArtBuild/vaelen-portrait.png
```

For a motion preview, use `--record ArtBuild/archive-frames --play "load sanctum;approach carillon;help"` with a fresh frame folder, then compile with `python Tools/ArtPipeline/make_preview.py ArtBuild/archive-frames ArtBuild/archive.gif --last-seconds 15`. The local prepared-save encounter recording produced 304 native frames at 12 fps (about 25 seconds); it checks presentation timing rather than boss balance.

The final `help` skips pending narration in still captures. Check native size and 2x scale, long labels, CRT on/off, two distinct enemy silhouettes, slack threads, the stopped occupied cradle and the open empty cradle. Repeat key states using an ignored Data copy without the three new room PNGs. Keep shipped files intact for fallback review. Prepared saves isolate visual states; deterministic scenario and balance tests separately verify actual story progression and encounters. Native captures contain no audio.

## Rollback and next slice

Revert PNGs, recipes and art manifest together. Revert WAVs, recipes, audio manifest and ambience bindings together. Remove backdrop paths to use procedural architecture. Remove the three state bindings to return to active defaults; revert their record, loader, schema and tests together if removing the fields. No save conversion is needed.

Next is the Iron Reliquary: Scoured Cloister, Lye Cistern and Reliquary Bastion, with Venn/Silas portraits, enemy sheets, live cistern/furnace details and ambience.
