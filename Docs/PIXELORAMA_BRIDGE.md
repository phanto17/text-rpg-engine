# Barrowlight preview bridge

This is a local authoring integration for Pixelorama 1.2.3 (ExtensionsAPI 9). It is **not** a Pixelorama built-in remote API. The extension uses documented panel and pixel-cel APIs; the broker and file protocol belong to this repository. No component loads executable content from a job, and the game runtime does not connect to the editor.

## Install and connect

1. Install the optional Python tools: `python -m pip install -r Tools/ArtPipeline/requirements.txt`.
2. In Pixelorama, open **Edit → Preferences → Extensions → Add Extension** and choose `Tools/PixeloramaExtension/dist/0.3/barrowlight_bridge.zip`. Enable it after reviewing its source in `Tools/PixeloramaExtension/`.
3. Make a local inbox folder with `requests/`, `images/`, and `responses/` subfolders. Use a disposable PXO project for the first test. In the **Barrowlight Preview** panel, enter its absolute folder path and click **Connect local inbox**.
4. The panel writes `status.json` with a process session ID, selected project/cel identity, dimensions, current cel SHA-256 and `canApply`. It refuses linked cels, locked layers, indexed projects, unsupported cel types and canvases over 1024×1024.
5. Run `python Tools/ArtPipeline/publish_preview.py <recipe.json> <inbox>` or start the broker below. Previewing never changes the document. Click **Apply to selected cel** to commit an undoable replacement, or **Cancel preview** to discard it. Save the PXO separately. Changing project, cel, or contents between preview and apply rejects the request.

Use a project-local inbox such as `ArtBuild/live-inbox`, which is ignored by Git. The extension can only access the folder selected in its panel. Keep that folder owner-controlled; the extension is trusted code and has the same filesystem permissions as Pixelorama.

## File protocol v1

`Tools/ArtPipeline/preview-job.schema.json` describes the request envelope. The producer writes a PNG to `images/<uuid>.png` and atomically renames a complete JSON file to `requests/<uuid>.json`. The extension checks the UUID, current target snapshot, PNG hash, dimensions, alpha, and 16-color palette before it displays anything. It deletes consumed request JSON files and writes `responses/<uuid>.json` with `previewed`, `applied`, `cancelled`, `superseded`, or `rejected`. A response includes a short detail string; an `applied` response includes the resulting cel hash. Query a response before retrying after a lost acknowledgement. Jobs never contain commands, scripts, arbitrary file paths or export destinations.

The producer caps dimensions at 1024×1024, layers at 32, operations at 4,096, pending jobs at four, and recipe/job messages at 1 MiB. The extension caps PNG files at 4 MiB and validates decoded dimensions. Only opaque palette colors and fully transparent source pixels are accepted. The supported recipe primitives are pixel, line, filled/outlined rectangle, filled/outlined ellipse and a bounded 16×16 palette stamp, composited in named layers. No tilemap or indexed-cel editing is attempted: both require separate safety work for shared tiles and palette-index semantics, and the current asset pack uses ordinary RGBA PNGs. Editor-side preview status is updated while connected; disconnect clears the active preview.

## Authenticated live broker

Start `python Tools/ArtPipeline/live_broker.py <inbox> --port 8765`. It binds **127.0.0.1** only and creates `<inbox>/broker.token` with a random 256-bit token. Keep the token private; it is ignored by Git, never put in a URL or printed. A controller connects via WebSocket with no browser Origin and sends `{"type":"auth","token":"..."}` within three seconds. The broker accepts one controller at a time, rejects browser origins, caps messages at 1 MiB, and disables compression. All further requests use `{"protocol":1,"id":"...","method":"status|preview"}`. `preview` carries a bounded recipe; `status` may carry a request UUID to return its latest editor response. The broker coalesces preview bursts for 25 ms before publishing the newest file job. It has no apply method: the editor user owns the final document mutation.

The broker uses the standard WebSocket transport and a small JSON envelope, not an undocumented Pixelorama socket. Authentication limits accidental access by other local processes; another process already running under the same OS user may still read the token file. Port conflicts fail at startup. Invalid jobs return structured errors, and disconnects leave already-published previews available in the inbox for inspection. Remove stale inbox files only after inspecting their final responses.

For a latency check with a connected disposable project, run `python Tools/ArtPipeline/live_demo.py <inbox> --count 20 --frequency 5`. It reports median and p95 time from controller submission to Pixelorama's preview acknowledgement. Preview jobs remain reversible until Apply. The first 150 ms poll implementation measured 123.9 ms median and 206.2 ms p95 for ten 56×56 previews at 5 Hz on Windows. Installed extension 0.3, polling at 40 ms, measured 63.4 ms median and 120.0 ms p95 for twenty 64×64 previews at 5 Hz. The p95 remains above the proposed 100 ms target; this is a local observation, not a general performance guarantee.

## Failure and recovery checks

- Try a malformed JSON job, wrong PNG hash, off-palette pixel, oversized PNG, and an unsupported API version; the document must remain unchanged.
- Preview on one project, switch tabs or edit the cel, then Apply; the extension must reject a stale target.
- Preview on a disposable ordinary pixel cel, Apply, Undo and Redo. Confirm the image and output hash after saving/reopening and CLI export.
- Disable or uninstall the extension; its panel must disappear and no listener should remain. Stop the broker; the editor's file inbox can still operate without it.
- On reconnect, inspect `status.json` and `responses/` before resubmitting a request. A new Pixelorama process has a new session ID, so old jobs are rejected.

Only the 1.2.3/API-9 combination has been exercised. Extension 0.3 was installed in Pixelorama and checked with a valid preview, an off-palette rejection, an explicit Apply to a disposable cel, and Undo restoring its blank pixels. Version checks fail closed, and another Pixelorama release needs a source/API review plus these tests before support is claimed.
