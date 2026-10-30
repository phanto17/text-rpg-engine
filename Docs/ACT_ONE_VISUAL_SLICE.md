# Act I visual slice: the threshold and Nocturne

This first Act I slice adds 304×140 paintings for the Weeping Stair and Nocturne's Lantern Market, and 56×56 portraits for Brother Orin, Keeper Maeve and Sallow the Peddler. Nocturne's Sister Wren reuses her established portrait. The Echo Chapel, Drowned Tollbridge and their combatants remain the next slice.

## Sources and visual direction

`Tools/ArtPipeline/build_act_one.py` is the editable source for the five new PNGs. It uses the established 16-color palette, binary alpha and integer coordinates. Import the PNGs into Pixelorama for inspection; these assets do not claim PXO masters. Add any later hand-authored master and update its source contract and reproduction check together.

The stair frames a black shaft with wet shale, broad wedge steps, a worn Triad relief and silver moss, opening toward a distant cathedral. Nocturne frames an empty beacon housing with broken cathedral ribs, oilcloth stalls, fungal lights, sanctuary linen and chalked names. Reserve bright accents for lamps and narrow stone edges. No reference-game artwork is included.

Hanging market lamps and stair seepage are live overlays sampled at four steps per second. Both appear above PNGs and procedural fallback scenes. The central beacon housing stays dark until `bindings.beaconLitFlag` resolves true; shipped data binds it to the existing `beacon_restored` flag. The repaired lens and flame then appear inside the housing. This corrects the old unconditional flame. It adds no gameplay state or save migration. The player's resonance glow and weather remain active above both backgrounds.

`stair_seepage.wav` uses irregular drip tones over circular filtered noise; `nocturne_hush.wav` uses subdued integer-frequency harmonics and noise. Both loops are eight-second PCM mono 22050 Hz / 16-bit WAVs from the original integer synthesis recipes and `ArtSource/audio.json`. Room transitions use the existing half-second crossfade and saved volume controls. Captured PNGs do not contain audio; subjective mix review still requires listening.

## Reproduce and check

Run from the repository root:

```powershell
python Tools/ArtPipeline/build_act_one.py ArtBuild/act-one-drafts
python Tools/ArtPipeline/validate_assets.py Data/Assets
python Tools/ArtPipeline/verify_reproducible.py
python Tools/AudioPipeline/build_audio.py ArtBuild/audio-drafts
python Tools/AudioPipeline/validate_audio.py Data/Assets/Audio --reproduce
python -m unittest discover -s Tools/ArtPipeline/tests -v
dotnet build -c Release
dotnet test
```

For native visual review, use a separate save directory beneath ignored `ArtBuild/`. Reach the Weeping Stair by resolving the Barrow Heart, taking the lantern and using `descend`; use `down` to enter Nocturne. Save a slot in each room, then capture via explicit `load <slot>` (plain `load` opens the slot menu):

```powershell
dotnet run -c Release --project src/RpgEngine.Pixel -- --saves ArtBuild/act-one-review --seed 42
# After saving slots named stair and market in their respective rooms:
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/act-one-review --play "load stair;talk orin" --screenshot ArtBuild/orin.png
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/act-one-review --play "load market;talk maeve" --screenshot ArtBuild/maeve.png
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/act-one-review --crt --play "load market;talk peddler" --screenshot ArtBuild/peddler-crt.png
```

Check both before and after beacon restoration. Confirm that the lantern market remains inhabited and lit while the central housing is dark; restoration should light only the central housing. Verify portraits at native size and 2× scale, captions through the full room description, readable menus, CRT on/off and the half-second ambience transition. For fallback review, use an ignored copy of Data without the two new room PNGs. Never alter the shipped files just for screenshots. Prepared local save fixtures can isolate visual states; the deterministic Act I scenario tests separately verify actual progression and restoration.

## Rollback and next slice

Revert recipe, PNG and manifest changes together. Revert ambience bindings, WAVs, recipes and audio manifest together. Removing backdrop paths restores procedural rooms; removing `beaconLitFlag` leaves its housing unlit. Revert the schema with its record if removing the binding. No player save conversion is required.

Next: Echo Chapel and Drowned Tollbridge paintings, Miriel/Garin portraits and enemy idle sheets, their environmental overlays and room loops. Follow with a complete Act I motion recording and listening review.
