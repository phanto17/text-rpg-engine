# Barrowlight pixel-art style guide

## Direction and constraints

Dark, deliberate pixel art supports the story without obscuring text or the action menu. The runtime is Raylib-cs on a 480×270 virtual canvas. The scene panel is 304×140; nearest-neighbor scaling and whole-pixel window scaling are available. The CLI and Core do not depend on visual assets. Room backgrounds may be 304×140 PNGs, while the pilot enemy and portrait masters are 48×48 and 56×56. These sizes are project conventions, not Pixelorama limits.

Reference intent: [Deltarune](https://deltarune.com/) for readable character silhouettes and restrained UI; [SKALD: Against the Black Priory](https://www.skaldrpg.com/) for low-color darkness and strong scene composition; [Blasphemous](https://thegamekitchen.com/blasphemous/) for gothic mass, religious forms and controlled red/gold accents. Study their principles; create original shapes, icons and characters. The in-repository mood board is the iron moor gate, the knight silhouette and idle sheet, Wren's lantern portrait, a stone tile, and a small lantern icon under `Data/Assets/`.

## Palette

The 16 RGB colors below are the source-art palette. Transparent pixels are allowed for figures. Exported source art uses opaque or fully transparent pixels; lighting effects are applied by the runtime.

| Role | Hex |
|---|---|
| Deep void | `#0B0D12` |
| Night | `#151923` |
| Slate shadow | `#252B38` |
| Iron | `#3C4554` |
| Mid steel | `#697584` |
| Cold highlight | `#AAB8C5` |
| Bone | `#E5DDC8` |
| Lantern core | `#FFF1C9` |
| Earth shadow | `#2B211F` |
| Blood shadow | `#63372E` |
| Rust red | `#A84B45` |
| Ember | `#DB8552` |
| Tarnished gold | `#D7B46A` |
| Moss | `#597465` |
| Cold teal | `#7FA89B` |
| Spirit violet | `#A29BC4` |

Keep most screen area in the first five dark colors. Reserve bone and cold steel for readable edges, amber for the lantern, and red for danger. Avoid globally lifting darks. Lighting can use stepped halos, sparse highlight pixels and restrained additive overlay at render time; any future bloom pass must be optional, bounded to emissive accents, and checked at native 480×270 so outlines stay crisp. CRT remains optional.

## Shape, motion and typography

- Build silhouettes first. Give architecture broad dark masses and one dominant focal point. Keep gameplay state overlays (seals, bell, brazier, weather) legible above static backdrops.
- Draw animations with deliberate holds. Target 60 Hz rendering but 5–8 distinct art frames per second for idle/environment cycles, 8–10 for walking, and 10–12 for brief attacks. The first knight sheet has four poses held for 200 ms each. A heavy attack should hold anticipation for 100–180 ms, move over 80–120 ms, hold impact for 100–150 ms, then recover over 180–300 ms.
- Limit idle motion to 1–2 pixels, use 4–6 key poses for a walk, and prefer a short strike plus longer recovery. Parallax should shift slowly in whole source pixels; shadows may flicker at 2–4 Hz without full-screen brightness pulses.
- The client loads Silkscreen Regular at 10 virtual pixels and falls back to raylib's default if the font file is unavailable. It folds unsupported punctuation to ASCII through `Canvas.Clean`. Use integer scaling, never fractional font scaling on the virtual canvas. Check long names, dialogue, 100% and 200% window scale, and high-DPI displays for overflow; `Canvas.Width` and drawing use the same font metrics.

## Asset contract

| Category | Editable master | Runtime export | Pilot dimension |
|---|---|---|---|
| Rooms | `ArtSource/Pixelorama/Rooms/<room_id>.pxo` | `Data/Assets/Rooms/<room_id>.png` | 304×140 |
| Monsters | `ArtSource/Pixelorama/Monsters/<monster_id>.pxo` | `Data/Assets/Monsters/<monster_id>.png` | 48×48 |
| Portraits | `ArtSource/Pixelorama/Portraits/<npc_id>.pxo` | `Data/Assets/Portraits/<npc_id>.png` | 56×56 |
| UI | `Tools/ArtPipeline/build_production.py` recipe | `Data/Assets/UI/lantern_icon.png` | 16×16 |
| Tiles | `Tools/ArtPipeline/build_production.py` recipe | `Data/Assets/Tiles/crypt_stone.png` | 16×16 |
| Animation | `Tools/ArtPipeline/build_production.py` recipe | `Data/Assets/Monsters/gravebound_knight_idle.png` | 4 horizontal 48×48 frames |
| Font | Official Google Fonts TTF plus license | `Data/Assets/Fonts/Silkscreen-Regular.ttf` | 10 virtual px |

Use `snake_case` identifiers matching `Data/` IDs. The three original PXO masters are editable in Pixelorama; production recipe outputs remain editable through the checked-in Pillow source and can be imported into Pixelorama as PNG. A sheet's `animation.frames` and `animation.frameMilliseconds` live beside its `spriteImage` in `Data/World/presentation.json`; the renderer crops horizontal equal-width frames. A malformed sheet displays as a static image. New assets need a manifest entry and deterministic source or PXO master in the same change.

## Provenance and licenses

All twenty-four PNG assets are original procedural drawings from `build_pilots.py`, `build_production.py`, `build_prologue.py`, `build_act_one.py`, `build_act_one_spokes.py` or `build_act_two.py`. They have no third-party asset dependency. Silkscreen Regular is by Jason Kottke, distributed in [Google Fonts](https://github.com/google/fonts/tree/main/ofl/silkscreen) under the SIL Open Font License 1.1; the license is copied to `Data/Assets/Fonts/OFL.txt`. The checked-in TTF SHA-256 is `C845473330B94C2079CE9AF01C51AC8BA2D99C24F4D14C039843BBB8E642EBD8`. The reference games above are visual study only; no assets are copied from them.

## Review checklist

Inspect each asset at native size and 2× integer scale, check palette/dimensions/alpha using `Test-ArtAssets.ps1`, run `verify_reproducible.py`, confirm Pixelorama export parity for PXO masters, and capture a `--play` plus `--screenshot` scene. Verify characters and labels against both darkest and brightest backgrounds. Keep animation independent of Core game logic. Indexed/tilemap editor mutation remains unsupported by the bridge; edit tile PNGs as ordinary pixel art and import them explicitly when needed.
