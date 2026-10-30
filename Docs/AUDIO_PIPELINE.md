# Pixel audio and atmosphere

The Pixel client plays optional audio from `Data/World/presentation.json`. Click **F5 Sound** in the scene or press F5 to open the modal settings panel. Click either volume bar, or use Up/Down and Left/Right to adjust it. M toggles mute in the panel; F4 toggles mute anywhere. F5, Esc or Close returns to play. Settings apply immediately and save beneath the nominated save directory at `.pixel/audio.json`, outside the engine's game save slots. The panel pauses narration and enemy turns while ambience continues. Default effects volume is 45%; ambience is 20%.

## Data bindings

```json
"audio": {
  "effects": {
    "select": "Assets/Audio/menu_select.wav",
    "step": "Assets/Audio/footstep.wav",
    "impact": "Assets/Audio/weapon_impact.wav",
    "damage": "Assets/Audio/damage.wav",
    "lantern": "Assets/Audio/lantern_ignite.wav"
  },
  "ambience": {
    "lichgate": "Assets/Audio/moor_rain.wav",
    "ossuary_hall": "Assets/Audio/ossuary_drips.wav",
    "inner_crypt": "Assets/Audio/barrow_resonance.wav",
    "weeping_stair": "Assets/Audio/stair_seepage.wav",
    "nocturne_square": "Assets/Audio/nocturne_hush.wav",
    "echo_chapel": "Assets/Audio/chapel_glass.wav",
    "drowned_tollbridge": "Assets/Audio/canal_current.wav",
    "warren_crossroads": "Assets/Audio/threshold_hush.wav"
  }
}
```

These maps are optional. Effects use the fixed client roles above; ambience keys are room IDs. Missing entries are silent. A room-description cue plays a footstep only when its room changes, so `look` does not replay it. Hit-flash cues choose impact for enemies and damage for the player, retaining the prologue's impact timing. An item-acquisition cue chooses the ignition sound only when its ID matches `bindings.lanternItem`. Menu picks and submitted commands use the selection clip. There is no per-character typing sound.

`AudioDirector` resolves cues and limits them to one effect per 40 ms globally and one per role per 150 ms. Muted effects are discarded, with no deferred burst on unmute. `AudioPlayer` owns the native Raylib device, loaded effects and at most two ambience streams. Room ambience crossfades over half a second. Muting changes loaded clip and stream volumes immediately; streams retain their position. Core has no playback dependency and no game rule or RNG draw changed.

## Sources and format

All thirteen WAVs are original synthesis from `Tools/AudioPipeline/build_audio.py`; there are no outside recordings, samples or music. The editable masters are the checked-in integer synthesis recipes plus `ArtSource/audio.json`, which records duration, seed, peak ceiling and loop intent. Fixed integer oscillators, envelopes and noise make PCM output reproducible across Windows and Linux. The pack uses canonical RIFF PCM, mono, signed 16-bit little-endian, 22050 Hz. Rain, drips, low resonance, stair seepage, Nocturne's low harmonic hush, distant chapel glass tones, canal current and the Twin-Seal sanctuary hush are eight-second loops; the five effects are 70–900 ms long. Their peak ceilings leave headroom before the user's volume settings.

Only relative `Assets/Audio/*.wav` paths are accepted. Before native decoding, `PcmWave` rejects missing or malformed files, unsupported headers, files over 8 MiB, traversal and reparse points. Invalid optional bindings warn at content load; missing or corrupt files remain silent at playback. An unavailable audio device leaves the game playable and is shown in Sound settings. Failed preference writes leave controls usable and show a save warning. Unsupported or corrupt preference files fall back to defaults.

## Reproduce and validate

From the repository root, with Python 3.11+ (standard library only for audio):

```powershell
python Tools/AudioPipeline/build_audio.py ArtBuild/audio-drafts
python Tools/AudioPipeline/validate_audio.py ArtBuild/audio-drafts --reproduce
python Tools/AudioPipeline/validate_audio.py Data/Assets/Audio --reproduce
python Tools/AudioPipeline/make_preview.py Data/Assets/Audio ArtBuild/sfx-preview.wav
dotnet build -c Release
dotnet test
```

Review staged WAVs before copying them into `Data/Assets/Audio/`. Validation checks sample format, duration, silence, clipping, DC offset, effect endpoints, loop seam steps and exact recipe parity. CI runs these checks on both platforms without opening an audio device. C# tests cover cue roles, repeated-room suppression, cooldowns, mute, zero volume, PCM path validation, real shipped bindings, preference recovery and save failure.

For a manual check, run `dotnet run --project src/RpgEngine.Pixel -- --saves ArtBuild/audio-test`. Adjust both bars, mute, close and reopen the game, then verify restored values. Play through Lichgate, Ossuary Hall and the Barrow Heart; check restrained steps, distinct ambience, impacts and lantern acquisition. Use headphones or speakers at a comfortable level to judge the final mix. The local Windows smoke check initialized the device, exercised mouse controls and confirmed saved mute after restart; subjective listening remains part of art review. Screenshot/GIF captures do not embed audio.

## Rollback

Remove the optional `audio` section to silence content playback, or revert the WAVs, recipes and manifest together. Remove `.pixel/audio.json` to reset local preferences. No game save migration is needed. Acts I–III can receive additional ambience through data after their sound direction is reviewed.
