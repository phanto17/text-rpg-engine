# Twin-Seal hub visual slice

The first Act II painting covers the Gate of the Twin Seals. Its 304x140 rotunda frames a circular bronze casting between the Archive's silver wires and the Reliquary's pale causeway. A sheltered sanctuary niche marks the road back to the bridge. The editable source is `Tools/ArtPipeline/build_act_two.py`; the PNG and manifest use the established 16-color palette. There are no NPCs or encounters in this room, so this slice needs no portraits or enemy sprites.

## Live gate and state

`ThresholdDetails` draws the floor-gate, seal sockets and stepped sanctuary flame over painted and procedural architecture. It replaces the earlier upright door, including its reversed socket arrangement: Moon is left and Sun is right, matching the room's text. The painting reserves the central casting for these overlays.

Carried seals brighten the outline of the corresponding empty socket; carrying both does not open the gate. `bindings.thresholdOpenFlag` refers to the existing `unsealed_cradle_gate` flag. Only this flag parts the lids and reveals the descending steps. The opened lids retain seated symbols even if a custom save lacks the carried items. A missing binding keeps the gate closed. No gameplay flag, rule constant or save format changes.

`threshold_hush.wav` is an eight-second loop of low harmonics and circular filtered noise. Its original integer recipe and seed live in the audio pipeline and manifest. It uses the established PCM mono 22050 Hz / 16-bit format, volume controls and half-second room crossfade. It supplies restrained cavern ambience; it does not simulate specific dialogue or footsteps. Automated checks verify its format and reproduction; subjective listening remains part of mix review.

## Reproduce and validate

From the repository root:

```powershell
python Tools/ArtPipeline/build_act_two.py ArtBuild/twin-seal-drafts
python Tools/ArtPipeline/validate_assets.py Data/Assets
python Tools/ArtPipeline/verify_reproducible.py
python Tools/AudioPipeline/validate_audio.py Data/Assets/Audio --reproduce
python -m unittest discover -s Tools/ArtPipeline/tests -v
dotnet build -c Release
dotnet test
```

Use an isolated save directory beneath ignored `ArtBuild/`. Capture empty sockets, Moon only, Sun only, both carried but closed, and the opened gate. Prepared saves can isolate the visual states; normal deterministic Act II/III scenario tests separately verify progression. Save a hub slot carrying both seals, then exercise the real interaction:

```powershell
dotnet run -c Release --project src/RpgEngine.Pixel -- --saves ArtBuild/twin-seal-review --seed 42
# After saving a slot called hub in the rotunda, with both seals:
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/twin-seal-review --seed 42 --play "load hub;unseal gate;up;help" --screenshot ArtBuild/twin-seal-open.png
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/twin-seal-review --seed 42 --record ArtBuild/twin-seal-frames --play "load hub;unseal gate;up"
python Tools/ArtPipeline/make_preview.py ArtBuild/twin-seal-frames ArtBuild/twin-seal.gif --last-seconds 20
```

The final `help` in a still capture skips pending room narration. Recordings wait for narration and need fresh output folders. Check the gate at native size and 2x scale, with CRT on and off. Verify that the left Moon and right Sun socket respond independently, both seals leave the gate closed until used, returning upstairs shows the opened shaft, and the sanctuary fire stays visible. Repeat closed/open views using an ignored copy of Data without `Assets/Rooms/warren_crossroads.png`. Keep the shipped Data unchanged during fallback checks. Captures contain no audio.

## Rollback and next slice

Revert painting, recipe and manifest together; remove the backdrop binding to use procedural architecture. Revert the WAV, audio recipe, manifest and ambience binding together. Remove the opening binding to keep the rendered gate closed, or revert its record, loader, state, schema and tests together. No save migration is needed.

Next is the Archive of Unkept Vows: the Whispering Stacks, Tallow Foundry and Vow Sanctum, with Caelan/Vaelen portraits, enemy sheets, live loom and carillon details, and room ambience.
