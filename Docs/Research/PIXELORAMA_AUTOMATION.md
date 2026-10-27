# Pixelorama automation feasibility for Barrowlight

Research date: 26 October 2026; implementation verified 27 October 2026. The original source review informed the pipeline now documented in [ART_PIPELINE.md](../ART_PIPELINE.md). Pixelorama 1.2.3 was installed locally, three PNG drafts were imported and saved as editable PXO projects, and its documented CLI exported the three runtime PNGs. The exports passed pixel-for-pixel parity with the drafts. Extension and live-bridge claims below remain proposals; latency figures are acceptance targets, not measurements.

## Recommendation and evidence boundary

Use Pixelorama as the editable art workstation, PNG as the runtime exchange format, and its official CLI for repeatable exports. Start with externally generated images and manual import. Add a small, trusted GDScript extension for previews and cel updates only after a compatibility spike. Introduce a local network bridge only if interactive use justifies it.

Pixelorama has both an official desktop CLI and an in-process extension API. I found no documented built-in HTTP, WebSocket, REST, MCP, or general remote drawing service in the reviewed official documentation and stable entry points. A remote bridge would be our software, not an existing Pixelorama protocol. [CLI documentation](https://pixelorama.org/user_manual/cli/), [extension API](https://pixelorama.org/extension_system/extension_api/).

Use stable v1.2.3 as the initial compatibility baseline. Its project configuration declares extension API 9, but the published API reference explicitly describes API 8. Check the installed version and actual API at startup, reject unsupported versions, and maintain a tested compatibility matrix. Do not copy signatures from development HEAD or assume documentation is synchronized. [Stable release](https://github.com/Orama-Interactive/Pixelorama/releases/tag/v1.2.3), [version configuration](https://raw.githubusercontent.com/Orama-Interactive/Pixelorama/v1.2.3/project.godot), [API reference version notice](https://pixelorama.org/extension_system/extension_api/).

## Capabilities and normal workflows

Pixelorama is an open-source, MIT-licensed pixel-art editor built with Godot/GDScript. Desktop Windows is the appropriate target for this project; desktop Linux/macOS and browser use also exist, but browser restrictions make the desktop automation plan nonportable without adaptation. [Repository](https://github.com/Orama-Interactive/Pixelorama), [installation](https://pixelorama.org/user_manual/installation/).

| Area | Useful capabilities |
|---|---|
| Drawing | Pencil, eraser, bucket fill, lines, rectangles, ellipses, curves/polygons, text, shading, color picking and selection tools. Independent tools can be assigned to left/right mouse buttons. |
| Brushes | Adjustable brush type/size, pixel-perfect drawing assistance and configurable opacity/overwrite behavior. |
| Composition | Layers, groups, clipping masks, blend modes and non-destructive effects; advanced 3D layers are available but unnecessary for the initial pipeline. |
| Animation | Frames/cels, onion skins, tags, playback, drawing during playback and audio layers for reference. |
| Tiles | Tilemap layers reference reusable tilesets; rectangular, isometric and hexagonal workflows are advertised. |
| Color | Palettes and indexed mode support deliberately limited colors. |

Sources: [tools](https://pixelorama.org/user_manual/tools/), [drawing](https://pixelorama.org/user_manual/drawing/), [feature overview](https://pixelorama.org/), [tilemaps](https://pixelorama.org/user_manual/tilemaps/).

A typical sprite workflow is: create a native-size project, load the palette, draw separate silhouette/detail/highlight layers, add animation frames, inspect onion skins and playback, save an editable project, then export a PNG sheet or sequence. For rooms, use separate architecture, foreground and lighting layers and check the final image at its actual game size. Tilesets should be established with their intended tile dimensions: the manual says tile size/shape cannot subsequently be changed in place. [Tilemap workflow](https://pixelorama.org/user_manual/tilemaps/).

### Formats and interchange limits

| Purpose | Formats and cautions |
|---|---|
| Editable master | `.pxo` preserves Pixelorama project structure. Keep this as the authoritative editable source. |
| Runtime output | PNG images, sequences and sheets; animated GIF/APNG for reviews. Video export requires FFmpeg. |
| Raster import | PNG/APNG, JPEG, WebP, BMP, SVG, TGA and HDR are documented. Importing SVG does not make Pixelorama a general vector-authoring pipeline. |
| Other editors | ASE/ASEPRITE, ORA, KRA, PSD and PISKEL imports are documented. Supported subsets vary; do not promise lossless round trips. |
| Palette exchange | Pixelorama JSON, GIMP GPL, PAL and palette extraction from images. |
| Audio reference | WAV/MP3 imports can become audio layers. This does not provide a procedural game-audio system. |

Sources: [save/export](https://pixelorama.org/user_manual/save_and_export/), [imports and format limitations](https://pixelorama.org/user_manual/Import/), [palettes](https://pixelorama.org/user_manual/palettes/).

There are documentation discrepancies worth handling explicitly. Native GIF importing was added in 1.1.7, although the import manual still mentions FFmpeg for GIFs. ASE export arrived in 1.2.2 and is not fully reflected in the basic export page. Prefer release-specific tests over assumptions. [1.1.7 release article](https://pixelorama.org/blog/pixelorama-1.1.7-is-out/), [1.2.2 release article](https://pixelorama.org/blog/pixelorama-1.2.2/).

Aseprite imports omit features including slices and color-profile chunks; Krita/PSD support is also partial. Palette support should not be mistaken for a verified ICC/CMYK production workflow. Use a defined RGBA palette and check exported pixel values. Large animation/video imports increase memory demand; GIF export can be slow. [Import restrictions](https://pixelorama.org/user_manual/Import/), [export cautions](https://pixelorama.org/user_manual/save_and_export/).

## Supported automation and alternatives

| Approach | Status | Advantages | Costs/limitations |
|---|---|---|---|
| CLI export | Official | Repeatable, headless, easy to wrap | Process startup; not an interactive drawing API |
| PNG generation then import | Standard file interchange | Smallest dependency surface; deterministic output; easy validation | Initial import/review remains manual |
| GDScript extension | Official extension mechanism | Editor UI, project/cel access, custom tools and import/export integration | Trusted executable code; version compatibility work |
| File inbox consumed by extension | Custom integration over normal file APIs | No listening port; inspectable jobs and recovery | Polling latency; requires transaction/acknowledgement design |
| Local WebSocket bridge | Custom integration over standard transport | Persistent connection and interactive previews | Authentication, concurrency, queue and lifecycle complexity |
| GUI automation | External fallback | Can exercise existing dialogs | Fragile focus, layout and timing; unsuitable for per-pixel production |
| Maintained source fork | Technically possible under license | Full control | Highest maintenance cost; upstream divergence |

### CLI: use it for exports

The desktop CLI documents `--headless`, `--quit`, export, spritesheet, output, scale, frame-range, direction, JSON metadata and layer splitting. Godot options precede `--`; Pixelorama options follow it. [Official command syntax](https://pixelorama.org/user_manual/cli/).

The repository's `Tools/ArtPipeline/Export-Pixelorama.ps1` runs the equivalent of this command for each master, with a fresh staging destination:

```powershell
& 'C:\Tools\Pixelorama\Pixelorama.exe' --headless --quit -- `
  'D:\text-rpg-engine\ArtSource\Pixelorama\Rooms\lichgate.pxo' `
  --export --spritesheet --scale 1 `
  --output 'D:\text-rpg-engine\ArtBuild\run_001\lichgate.png'
```

Create the staging directory beforehand. Pass arguments as an argument array in the eventual wrapper, without constructing shell expressions. Capture stdout/stderr, enforce a timeout and verify the PNG's dimensions, decoded contents and expected location.

Stable CLI source processes existing file arguments before exporting the current project. Therefore use one input per process and a fresh output path. This also avoids the possibility of an already-existing output being interpreted as input. Unknown-option handling does not reliably imply a nonzero exit status: output validation must supplement the process exit code. These are source-derived cautions, not runtime reproductions. [v1.2.3 Main.gd](https://raw.githubusercontent.com/Orama-Interactive/Pixelorama/v1.2.3/src/Main.gd).

### Extensions: use a narrow adapter

Extensions use GDScript, a manifest and a scene, distributed as PCK or ZIP and installed/enabled through Preferences. Develop against the Godot version used by the target release. Declare supported API versions, register UI on activation and remove handlers/UI on shutdown. Treat installation as trusting executable code; do not infer a permission sandbox from the manifest. [Extension authoring guide](https://pixelorama.org/extension_system/extension_basics/).

The stable API exposes project creation, frame/layer creation, cel selection, image replacement, palettes and signals. It also exposes internal objects; those accessors do not make every internal method a stable public contract. Use a small adapter and avoid arbitrary calls into editor internals. [v1.2.3 ExtensionsApi.gd](https://raw.githubusercontent.com/Orama-Interactive/Pixelorama/v1.2.3/src/Autoload/ExtensionsApi.gd).

The cel replacement implementation records undo, but can enlarge the canvas for oversized inputs and changes selection. Restrict initial commits to equal-size images in ordinary, unlinked pixel cels. Verify undo/redo and selection behavior in the compatibility spike; do not promise multi-cel atomic undo. [v1.2.3 OpenSave.gd](https://raw.githubusercontent.com/Orama-Interactive/Pixelorama/v1.2.3/src/Autoload/OpenSave.gd).

## Proposed interface and safety model

The following is our proposed design, not a native Pixelorama API.

```text
Controller / procedural generator
             |
       Validated job + PNG
             |
   Local inbox -> trusted extension -> preview
                                      |
                            explicit commit to cel
                                      |
                            editable PXO -> CLI export
                                      |
                          validation -> runtime PNG
```

Begin with a user-selected local inbox and an extension panel containing Preview, Apply, Cancel and Disconnect. Generate pixels outside the live document. A request identifies a project session, frame/layer target, base revision, dimensions, palette, request ID and content hash. Initial targets are newly created preview documents or dedicated generated layers. An image preview is not a saved document.

Before applying, re-check the active project, target identity, revision and dimensions. Reject stale requests, locked layers, linked cels, unsupported layer types and edits arriving during a user stroke. Avoid silently clamped frame/layer indices. Acknowledge only after the intended change has committed and its resulting revision/hash is known. If changes occur outside our adapter, detect them through appropriate signals plus a target-content check.

For file IPC, write temporary files and atomically rename completed requests into the inbox. Keep acknowledgements and failure records separate. Restrict directories to the current user, canonicalize paths, reject traversal and escapes through reparse points, and bound decoded images as well as compressed file size. Jobs must never specify scripts, arbitrary resource loads, shell commands or unrestricted filesystem destinations. Ordinary OS filesystem access is sufficient; no administrator privileges or online account is required.

For a later persistent connection, use a standard broker library bound exclusively to loopback. The extension can connect as a WebSocket client using Godot's supported `WebSocketPeer`. Use JSON-RPC 2.0 envelopes for commands and responses, documenting our own methods and authentication separately. Neither standard supplies authorization by itself. [Godot WebSocket API](https://docs.godotengine.org/en/stable/classes/class_websocketpeer.html), [JSON-RPC specification](https://www.jsonrpc.org/specification).

The broker should require a cryptographically random session token before accepting commands, keep it out of URLs/logs, reject unexpected browser origins and close unauthenticated connections promptly. Use an owner-only token file or local pairing flow. No LAN binding or remote mode in the MVP. Port conflicts must fail explicitly. A malicious process running as the same OS user remains outside the protection this design can guarantee.

Apply explicit scopes: read preview, modify one nominated document, export to a nominated staging directory. Never expose an evaluate-code method. Disable networking and release resources when the extension is disabled. Artwork stays local by default; sending it to a cloud model requires a separately configured, authorized workflow.

### Errors, concurrency and latency

- Return structured parse/parameter/method errors and domain errors such as UnsupportedVersion, StaleRevision, Busy, InvalidTarget, QuotaExceeded and OutputDenied.
- Cache completed request IDs. After disconnect, query an operation's status before retrying a mutation whose acknowledgement was lost.
- Prevalidate and render into a temporary buffer. Do not partially alter the live cel while parsing a job.
- Serialize editor mutations on the main thread. Keep expensive generation outside it; cancellation discards pending generation without undoing unrelated human edits.
- Bound queues. Replace obsolete preview requests with the newest preview, but never silently drop committed operations.
- Initially cap canvases at 1024×1024, jobs at 4,096 operations and messages at 1 MiB. These are proposed bridge limits, not Pixelorama limits; tune after measurement.
- Target 5–15 preview updates/second and under 100 ms local preview latency for small assets, with roughly 2–4 ms maximum editor work per update where achievable. Measure p50/p95; these are goals, not benchmark results.
- A 304×140 RGBA frame is 170,240 bytes; ten full frames/second is about 1.62 MiB/s before overhead. Start with whole images; add dirty rectangles only after profiling.
- Test background-window behavior: a paused editor must report unavailable/paused status rather than silently accumulate jobs.

Interactive procedural drawing can be fast. Semantic AI generation has a separate, variable latency and should be asynchronous. Pixelorama should not be described as having a built-in prompt-to-image system based on this investigation.

## Concrete creation capabilities

These operations are implementable proposals; native editor features alone do not imply remote endpoints.

| Priority | Capability | Practical implementation |
|---|---|---|
| MVP | Pixels, lines, filled/outlined rectangles and ellipses | Integer raster algorithms in an isolated RGBA buffer |
| MVP | Palette enforcement | Explicit 16-color Barrowlight palette; exact membership checks; deterministic remapping |
| MVP | PNG/sheet generation | Fixed dimensions, alpha rules, nearest-neighbor previews and content hashes |
| Next | Brushes and fills | Validated PNG stamps, bounded flood fill, declared overwrite/blend semantics |
| Next | Procedural textures | Seeded stone, ash, cloth and light patterns with reproducible inputs |
| Next | Editor layers/cels | Narrow version-tested extension adapter; preview then undoable single-cel commit |
| Next | Animation | Generate frame buffers, then explicit frame/tag/duration integration after API testing |
| Later | Tilemap and indexed editing | Separate adapters; protect shared tiles and palette-index semantics |
| Later | Live brush previews | Coalesced stroke batches and temporary overlays, committing once per stroke |

For the initial palette contract, use fully transparent or fully opaque source pixels unless an asset explicitly needs partial alpha. Do not require indexed PNG files merely to enforce a limited palette: RGBA PNG plus palette validation is a simpler first implementation. Font/UI generation remains a separate pipeline with font-license and readability checks.

## Phased delivery and acceptance gates

| Phase | Tasks | Deliverables and exit gate |
|---|---|---|
| 0 — Compatibility spike | Pin release, test PXO import/save and CLI export; defer an extension spike until editor integration is justified | Installed 1.2.3 archive hash, three verified master exports; extension compatibility still untested |
| 1 — File-based MVP | Implement deterministic raster recipes, palette/dimension checks, staging output and CLI wrapper; import samples | One room, one monster and one portrait; exported PNG hashes and editable PXO masters delivered |
| 2 — Safe editor integration | Add inbox, preview panel, target/revision checks, bounded jobs, cancellation and acknowledgements | Versioned extension package, JSON Schema, protocol notes and recovery tests; no silent overwrite of human edits |
| 3 — Live interaction | Add authenticated loopback broker, coalescing, operation status and latency measurements | Interactive preview demo, p50/p95 measurements, bounded queue behavior and disconnect recovery |
| 4 — Production expansion | Animation integration, brushes, tilemap/indexed adapters where justified, export provenance and CI validation | Organized asset pack, authoring guide, licensing inventory and reproducible runtime exports |

Test malformed jobs, corrupt/truncated PNGs, oversized decoded dimensions, duplicate IDs, stale revisions, project/tab switches, active strokes, locked/linked cels, extension disable, broker restart, cancellation and interrupted exports. Confirm unsupported versions fail without changing the document. Verify source pixels after save/reopen and export. Exercise a representative effect-heavy project before claiming headless visual parity.

## Fit with Barrowlight

Keep Pixelorama and the broker as authoring tools, outside Core and outside runtime dependencies. Proposed layout:

```text
ArtSource/Pixelorama/{Rooms,Monsters,Portraits,UI}/   editable PXO
ArtSource/Palettes/                               palette sources
Tools/ArtPipeline/                               recipes, validation, export wrapper
ArtBuild/<run-id>/                                temporary exports and editor metadata
Data/Assets/{Rooms,Monsters,Portraits,UI}/          reviewed runtime PNGs
```

The previously verified Pixel client uses a 480×270 virtual canvas, with a 304×140 scene panel. Begin with 304×140 room images, 48×48 monster source images and 56×56 portraits. These are Barrowlight choices, not editor limits. Inspect the smaller enemy rendering size as well. Existing presentation image fields can consume static assets; animated sheets require a separate data-driven runtime feature.

Keep raw Pixelorama JSON exports out of Data: editor metadata is not automatically compatible with the game's schemas. Preserve procedural puzzle/state overlays when introducing static backgrounds. Asset paths belong in presentation data; no story IDs or art-specific rules should be added to Core. Any future content-definition changes require regenerated schemas, and every implementation commit must retain the clean Release build and full passing test suite.

## Implementation status (2026-10-27)

Phases 0 and 1 produced the three checked-in PXO masters and byte-reviewed PNG exports. Phase 2 produced the API-9 extension package, a bounded file inbox, explicit Apply/Undo, target revision checks, cancellation and response records. In Pixelorama 1.2.3, extension 0.2 was exercised on a disposable 56×56 cel: preview left pixels unchanged, switching project rejected a stale Apply, and Undo restored a committed cel. The packaged 0.3 revision adds palette validation and a 40 ms poll interval; its script passed the pinned Godot parser but UI retesting remains required before claiming that revision verified.

Phase 3 produced a loopback-only authenticated broker, one-controller admission, origin rejection, preview coalescing and status lookup. A ten-update 5 Hz controller-to-editor run with the earlier 150 ms poll measured 123.9 ms median and 206.2 ms p95 on the local Windows host. Re-run that measurement with package 0.3 before treating the 100 ms target as achieved. The broker never offers a remote Apply operation.

Phase 4 now includes a deterministic four-frame knight sheet displayed by `RpgEngine.Pixel` from presentation timing, a 16×16 UI lantern and crypt tile, a bounded palette stamp primitive, the licensed Silkscreen font, a provenance inventory, and cross-platform CI checks for source pixel parity. Indexed and tilemap mutations remain deferred because the current runtime consumes RGBA PNGs and the editor's shared-tile/palette-index semantics need a separate compatibility spike. Audio remains an optional future enhancement; no runtime audio dependency was added.
