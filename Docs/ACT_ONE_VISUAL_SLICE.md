# Act I visual slice

Act I now has 304x140 paintings for the Weeping Stair, Nocturne, Echo Chapel and Drowned Tollbridge. Five 56x56 portraits cover Orin, Maeve, Sallow, Miriel and Garin; Nocturne's Wren reuses her established portrait. The four spoke combatants have horizontal four-pose 48x48 idle sheets. Miriel and Garin hold poses for 250 ms, the mournling and lancer for 300 ms. Sway stays within one source pixel; existing stepped attack motion and impact timing apply unchanged.

## Sources and visual direction

`Tools/ArtPipeline/build_act_one.py` is the editable source for the first five PNGs; `build_act_one_spokes.py` supplies two more rooms, two portraits and four enemy sheets. It uses the established 16-color palette, binary alpha and integer coordinates. Import the PNGs into Pixelorama for inspection; these assets do not claim PXO masters. Add any later hand-authored master and update its source contract and reproduction check together.

The stair frames a black shaft with wet shale, broad wedge steps, a worn Triad relief and silver moss, opening toward a distant cathedral. Nocturne frames an empty beacon housing with broken cathedral ribs, oilcloth stalls, fungal lights, sanctuary linen and chalked names. Reserve bright accents for lamps and narrow stone edges. No reference-game artwork is included.

Hanging market lamps and stair seepage are live overlays sampled at four steps per second. Both appear above PNGs and procedural fallback scenes. The central beacon housing stays dark until `bindings.beaconLitFlag` resolves true; shipped data binds it to the existing `beacon_restored` flag. The repaired lens and flame then appear inside the housing. This corrects the old unconditional flame. It adds no gameplay state or save migration. The player's resonance glow and weather remain active above both backgrounds.

`stair_seepage.wav` uses irregular drip tones over circular filtered noise; `nocturne_hush.wav` uses subdued integer-frequency harmonics and noise. Both loops are eight-second PCM mono 22050 Hz / 16-bit WAVs from the original integer synthesis recipes and `ArtSource/audio.json`. Room transitions use the existing half-second crossfade and saved volume controls. Captured PNGs do not contain audio; subjective mix review still requires listening.

The chapel painting uses salt-crusted bone pipes and broad stalactites around the pulpit; the bridge uses three basalt arches, a flooded guardhouse and quarantine iron. Hanging spirit-jars, stepped water ripples and chains render above both backdrop paths. Lantern-visible memory silhouettes appear near the organ loft and floodgate winch. `bindings.releasedSpiritsFlag` binds the existing `released_choir_jars` flag to empty, unsealed jars. `bindings.passageVisitedRoom` binds `warren_crossroads` to the raised floodgate: resolving Garin only permits crossing; the gate remains lowered until the winch is used and the player reaches that room. No new flags or quests are introduced.

`chapel_glass.wav` supplies distant glass-like tones and low harmonics; `canal_current.wav` supplies circular filtered noise and a low undertone. Both are eight-second loops, independent of individual organ keys or jar actions. Silence after reconciliation is narrative; the ambience is the surrounding cavern, not a persistent performance by Miriel.

## Reproduce and check

Run from the repository root:

```powershell
python Tools/ArtPipeline/build_act_one.py ArtBuild/act-one-drafts
python Tools/ArtPipeline/build_act_one_spokes.py ArtBuild/act-one-spoke-drafts
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

Check both before and after beacon restoration. Confirm that the lantern market remains inhabited and lit while the central housing is dark; restoration should light only the central housing. Verify portraits at native size and 2× scale, captions through the full room description, readable menus, CRT on/off and the half-second ambience transition. For fallback review, use an ignored copy of Data without the room PNGs. Never alter the shipped files just for screenshots. Prepared local save fixtures can isolate visual states; the deterministic Act I scenario tests separately verify actual progression and restoration.

For the spoke review, save separate slots before approaching each encounter. Capture `load chapel;approach organ;help` and `load bridge;approach line;help` for enemy silhouettes. The final help input skips the encounter narration in still captures. Long enemy names fit inside the actual slot spacing rather than overlapping adjacent labels. After reconciliation, capture `talk miriel` and `talk garin`; use `release jars` to check empty jars, then cross into the warrens and return west to verify the raised gate. Check the lowered gate after resolving Garin but before crossing. Repeat the state views with an ignored fallback Data copy. Use the existing `--record` command with fresh frame folders for idle and attack timing; recordings contain no audio. A tour can use `--play "load stair;down;east;west;west"` to visit all four scenes without triggering their optional encounters. The local prepared-save tour produced 614 native frames (about 51 seconds) at 12 fps. It verifies presentation traversal; the deterministic scenario tests separately cover combat, mercy, restoration and crossing.

## Rollback and next slice

Revert recipe, PNG and manifest changes together. Revert ambience bindings, WAVs, recipes and audio manifest together. Removing backdrop paths restores procedural rooms; removing `beaconLitFlag` leaves its housing unlit. Revert the schema with its record if removing the binding. No player save conversion is required.

Next: the Act II Twin-Seal hub, then the Archive and Iron Reliquary art sets. Continue listening review of the ambient mix independently of screenshot and deterministic validation; no subjective listening result is claimed by the automated checks.
