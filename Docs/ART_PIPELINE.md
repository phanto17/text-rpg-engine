# Pixelorama art pipeline

Pixelorama is an optional authoring tool for `RpgEngine.Pixel`. Core and CLI have no Pixelorama dependency. The tested Windows baseline is Pixelorama 1.2.3; `ArtSource/pilots.json` records its archive SHA-256. Install the official Windows release locally under `Tools/Local/Pixelorama-1.2.3/Pixelorama-Windows-64bit/`, or set `PIXELORAMA_EXE` to another verified executable. `Tools/Local/` and `ArtBuild/` are ignored. The repo tracks editable `.pxo` masters and reviewed PNG exports, not the editor binary.

## Reproduce and publish the pilot pack

From the repository root, with Python 3.11+, PowerShell 7 and Pillow:

```powershell
python -m pip install -r Tools/ArtPipeline/requirements.txt
python Tools/ArtPipeline/build_pilots.py ArtBuild/pilots
pwsh -NoProfile -File Tools/ArtPipeline/Export-Pixelorama.ps1
python Tools/ArtPipeline/validate_assets.py ArtBuild/export-<run-id> --compare ArtBuild/pilots
pwsh -NoProfile -File Tools/ArtPipeline/Test-ArtAssets.ps1
```

`build_pilots.py` creates deterministic original raster drafts. The existing `.pxo` files were created by opening those PNGs in Pixelorama and using Save As. They are now the editable masters; rerunning the recipe does not overwrite them. `Export-Pixelorama.ps1` invokes Pixelorama's [documented CLI](https://pixelorama.org/user_manual/cli/) once per PXO master, each with a fresh staging folder. It validates PNG format, dimensions, palette and alpha, and records hashes. The initial three exports were checked pixel-for-pixel against the recipe drafts. Review the staged art at game scale, then copy the approved PNGs to matching paths in `Data/Assets/` and run `Test-ArtAssets.ps1` again. Change `Data/World/presentation.json` image paths only when a new asset is ready.

`build_production.py` generates the first timed monster sheet, UI icon and tile from checked-in Pillow source. Run `python Tools/ArtPipeline/build_production.py ArtBuild/production`, inspect the staged PNGs, then copy approved assets to `Data/Assets/`. Run `python Tools/ArtPipeline/verify_reproducible.py` to compare every reviewed PNG's decoded pixels against its deterministic source. `ArtSource/pilots.json` records size, source type and sheet timing. CI checks this parity on Windows and Linux. The font license and provenance are recorded in [STYLE_GUIDE.md](STYLE_GUIDE.md).

On platforms other than Windows, set `PIXELORAMA_EXE` to a tested Pixelorama desktop build and use PowerShell 7 for the wrapper. The shipped PNGs still work without Pixelorama installed. The optional [preview bridge](PIXELORAMA_BRIDGE.md) uses a version-pinned extension and local inbox; it is this repository's protocol, not a native Pixelorama remote API. The [automation feasibility report](Research/PIXELORAMA_AUTOMATION.md) records the supported editor surface and remaining limits.

## Recovery and rollout

The [prologue visual slice](PROLOGUE_VISUAL_SLICE.md) extends the pack with Ossuary Hall, Barrow Heart and the barrow thrall. `build_prologue.py` produces their staged PNGs; the door, sigil highlights, braziers and plinth lantern remain runtime overlays. That guide also documents the recording-to-GIF review workflow and typography checks.

Keep the previous runtime PNGs until a new export passes visual and automated review. A bad asset change can be rolled back by reverting its `.pxo` or recipe, PNG and presentation entry together; the procedural fallback remains available if an image path is removed. The pack now includes a room, monster, portrait, animated knight sheet, UI icon, stone tile and licensed font. Additional motion and audio can follow the same data-addressed pattern; Core does not need an art or sound dependency.
