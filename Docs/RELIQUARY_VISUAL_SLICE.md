# Iron Reliquary visual slice

This slice completes the initial Act II art set with 304x140 paintings for the Scoured Cloister, Lye Cistern and Reliquary Bastion; 56x56 portraits for Quartermaster Venn and Commander Silas; and four-pose horizontal 48x48 enemy sheets for Silas and the scoured halberdier. Silas holds poses for 250 ms, the halberdier for 300 ms, with one-pixel sway. Existing stepped attack motion and damage timing apply unchanged.

The original editable source is `Tools/ArtPipeline/build_reliquary.py`, with entries in `ArtSource/pilots.json`. Assets use the established 16-color palette and binary alpha. They can be imported into Pixelorama as PNGs; these recipe exports do not claim PXO masters.

## Composition and live details

The cloister uses bleached rectilinear sleeping bays, anonymous iron helms and a confessional grate. The cistern uses vaulted arches, lead pipes and a white-crusted basin with drainage troughs. The bastion frames the furnace with austere stonework, a dispatch table and nameless iron fittings. Named figures stay in sprites and portraits rather than being painted into rooms.

`ReliquaryDetails` draws live basin liquid, condensation, steam, the sluice wheel and stream, furnace fire, grate and Sun Seal above painted and procedural architecture. Environmental motion samples at four steps per second. The cistern no longer uses the generic ember weather; its renderer owns steam and condensation, so quenching removes the boiling effect. Its neutral room caption remains accurate in both states. The existing story bindings resolve into the headless `ReliquaryState` snapshot:

| Binding | Shipped reference | Result |
|---|---|---|
| `cisternQuenchedFlag` | `quenched_lye_cistern` | Steam gives way to brighter cold water pouring from the sluice; the wheel changes pose |
| `furnaceOpenFlag` | `silas_resolved` | The grate lifts clear of the furnace opening after victory or parley |

The existing `sunSealItem` binding hides the visible Sun Seal when it is carried. The furnace keeps burning after Silas is resolved; the story does not extinguish it. Missing bindings leave the cistern boiling and grate closed. No gameplay flags, balance, RNG draws or save format change.

## Ambience

Three original eight-second PCM mono 22050 Hz / 16-bit loops accompany the rooms: `cloister_air.wav`, `cistern_flow.wav` and `furnace_draft.wav`. Editable integer synthesis and seeds live in the audio pipeline and manifest. The cistern loop represents room water flow rather than a timed boiling effect, so it remains suitable after quenching. The existing half-second crossfade, separate volume controls and mute apply. Automated checks verify format, loop seams and reproduction; subjective listening remains part of mix review.

## Reproduce and inspect

```powershell
python Tools/ArtPipeline/build_reliquary.py ArtBuild/reliquary-drafts
python Tools/ArtPipeline/validate_assets.py Data/Assets
python Tools/ArtPipeline/verify_reproducible.py
python Tools/AudioPipeline/validate_audio.py Data/Assets/Audio --reproduce
python -m unittest discover -s Tools/ArtPipeline/tests -v
dotnet build -c Release
dotnet test
```

Keep review saves and captures under ignored `ArtBuild/`. Save slots before quenching, before approaching Silas, after resolving him, and after taking the Sun Seal. Example contributor commands after saving slots named cloister, cistern, bastion and relieved:

```powershell
dotnet run -c Release --project src/RpgEngine.Pixel -- --saves ArtBuild/reliquary-review --seed 42
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/reliquary-review --play "load cloister;talk venn;help" --screenshot ArtBuild/venn.png
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/reliquary-review --play "load cistern;quench cistern;help" --screenshot ArtBuild/quenched.png
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/reliquary-review --crt --play "load bastion;approach furnace;help" --screenshot ArtBuild/silas-combat.png
dotnet run -c Release --no-build --project src/RpgEngine.Pixel -- --saves ArtBuild/reliquary-review --play "load relieved;talk silas;help" --screenshot ArtBuild/silas-portrait.png
```

The final `help` skips pending narration in still captures. Quenching requires a successful Athletics check; confirm success in the log or use separate prepared states for visual comparison. Check boiling and cold-water states, independent furnace access, and the seal before/after taking it. Review portraits and enemy silhouettes at native size and 2x scale, CRT on/off and long labels. Repeat key states with an ignored Data copy without the three new room PNGs; keep shipped files intact. Prepared saves isolate visual states; deterministic scenario and balance tests separately verify progression and encounters. Captures contain no audio.

For motion, use a fresh frame folder with `--record ArtBuild/reliquary-frames --play "load bastion;approach furnace;help"`, then compile with `python Tools/ArtPipeline/make_preview.py ArtBuild/reliquary-frames ArtBuild/reliquary.gif --last-seconds 15`.

The local prepared-save encounter recording produced 278 native frames at 12 fps (about 23 seconds). The seeded native quench check succeeded and showed the cold sluice flow; this presentation review complements the deterministic scenario and balance tests.

## Rollback and next slice

Revert PNGs, recipe and art manifest together. Revert WAVs, synthesis recipes, audio manifest and ambience bindings together. Removing backdrop paths restores procedural architecture. Removing the two state bindings leaves boiling/closed defaults; revert record, loader, schema and tests together if removing the fields. No save conversion is required.

Next is Act III's entrance and sanctuary: the Stair of the Uncarved Skull and Vigil of the Last Watch, with Oakhaven's portrait and reused Wren art, followed by the bell nave and final cradle.
