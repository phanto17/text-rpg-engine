"""Assemble reviewed native recordings into a silent teaser and separate spoiler clips.

The manifest selects zero-based, stop-exclusive recorded PNG indices at 12 fps.
Scripts are provenance only: this tool never launches a process or executes a script.
"""
import argparse
import hashlib
import html
import json
import re
from pathlib import Path, PurePosixPath

from PIL import Image
from make_preview import frame_durations


def source_path(root, relative):
    if not isinstance(relative, str) or "\\" in relative or ":" in relative:
        raise ValueError("Recording paths must be relative forward-slash paths")
    path = PurePosixPath(relative)
    if not relative or path.is_absolute() or ".." in path.parts:
        raise ValueError("Recording path escapes the manifest directory")
    full = (root / relative).resolve()
    if not full.is_relative_to(root.resolve()):
        raise ValueError(f"Escaping capture source: {relative}")
    return full


def fixture_hash(root, shot):
    if shot.get("fixture") != "prepared-save":
        return None
    path = source_path(root, shot.get("save"))
    if not path.is_file():
        raise ValueError("Prepared-save shots require their input save file")
    return hashlib.sha256(path.read_bytes()).hexdigest()


def select(root, shot):
    identifier = shot.get("id")
    if not isinstance(identifier, str) or not re.fullmatch(r"[a-z][a-z0-9_]{0,47}", identifier):
        raise ValueError("Shot id must be a short snake_case identifier")
    if type(shot.get("spoiler")) is not bool:
        raise ValueError(f"{identifier}: spoiler classification is required")
    if shot.get("fixture") not in ("title", "prepared-save", "playthrough"):
        raise ValueError(f"{identifier}: declare title, prepared-save or playthrough provenance")
    if not isinstance(shot.get("script"), str) or not isinstance(shot.get("label"), str):
        raise ValueError(f"{identifier}: label and script provenance are required")
    directory = source_path(root, shot.get("recording"))
    if not directory.is_dir():
        raise ValueError(f"Missing recording: {shot.get('recording')}")
    paths = sorted(directory.glob("frame_*.png"))
    if not 1 <= len(paths) <= 1440 or any(not re.fullmatch(r"frame_\d{6}\.png", path.name) for path in paths):
        raise ValueError(f"{identifier}: expected 1..1440 contributor recording frames")
    if [int(path.stem[6:]) for path in paths] != list(range(5, len(paths) * 5 + 1, 5)):
        raise ValueError(f"{identifier}: missing or non-contiguous contributor frames")
    start, stop = shot.get("start"), shot.get("stop")
    if type(start) is not int or type(stop) is not int or not 0 <= start < stop <= len(paths) or stop - start > 120:
        raise ValueError(f"{identifier}: select 1..120 existing recorded frames")
    selected = paths[start:stop]
    if any(not path.resolve().is_relative_to(root.resolve()) for path in selected):
        raise ValueError(f"{identifier}: a selected frame escapes the manifest directory")
    return selected


def write_gif(frames, destination):
    frames[0].save(destination, save_all=True, append_images=frames[1:],
                   duration=frame_durations(len(frames)), loop=0, disposal=2, optimize=False)


def build(manifest_path, output):
    manifest_path, output = Path(manifest_path).resolve(), Path(output).absolute()
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if type(manifest.get("version")) is not int or manifest["version"] != 1:
        raise ValueError("Expected capture manifest version 1")
    for field in ("baselineCommit", "contentVersion"):
        if not isinstance(manifest.get(field), str) or not manifest[field]:
            raise ValueError(f"Missing capture provenance: {field}")
    if type(manifest.get("seed")) is not int or manifest["seed"] < 0:
        raise ValueError("A non-negative seed is required")
    shots = manifest.get("shots")
    if not isinstance(shots, list) or not 1 <= len(shots) <= 12 or any(not isinstance(shot, dict) for shot in shots):
        raise ValueError("Expected 1..12 capture shots")
    selections = [select(manifest_path.parent, shot) for shot in shots]
    save_hashes = [fixture_hash(manifest_path.parent, shot) for shot in shots]
    if len({shot["id"] for shot in shots}) != len(shots):
        raise ValueError("Duplicate shot ids")
    teaser_count = sum(len(paths) for shot, paths in zip(shots, selections) if not shot["spoiler"])
    if not 1 <= teaser_count <= 360 or sum(map(len, selections)) > 720:
        raise ValueError("Select 1..360 teaser frames and at most 720 total frames")

    # No output can replace an earlier review. A failed run leaves no completed receipt.
    output.mkdir(parents=True, exist_ok=False)
    teaser, receipts, cards = [], [], []
    for shot, paths, save_hash in zip(shots, selections, save_hashes):
        frames, hashes = [], []
        poster_name = shot["id"] + ".png"
        for i, path in enumerate(paths):
            with Image.open(path) as image:
                if image.format != "PNG" or image.size != (480, 270):
                    raise ValueError(f"Expected native 480x270 PNG: {path}")
                if i == 0:
                    image.save(output / poster_name)
                frames.append(image.convert("RGB").quantize(colors=128))
            hashes.append({"file": path.name, "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
        clip_name = shot["id"] + ".gif"
        write_gif(frames, output / clip_name)
        if not shot["spoiler"]:
            teaser.extend(frames)
        receipts.append({**shot, "saveSha256": save_hash, "selectedFrames": len(paths), "sourceFrames": hashes})
        label = html.escape(shot["label"], quote=True)
        status = "Spoiler — excluded from teaser" if shot["spoiler"] else "Included in teaser"
        cards.append(f'<article><h2>{label}</h2><p>{status}; {len(paths) / 12:g} seconds; '
                     f'{html.escape(shot["fixture"])}</p><a href="{clip_name}">'
                     f'<img src="{poster_name}" alt="{label}"></a><p><a href="{clip_name}">Play clip</a></p></article>')
    write_gif(teaser, output / "teaser.gif")
    receipt = {"version": 1, "baselineCommit": manifest["baselineCommit"], "contentVersion": manifest["contentVersion"],
               "seed": manifest["seed"], "fps": 12, "silent": True, "teaserFrames": teaser_count,
               "teaserSeconds": teaser_count / 12, "shots": receipts}
    (output / "capture-receipt.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    document = '''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width">
<title>Barrowlight capture review</title><style>body{background:#151923;color:#e5ddc8;font:16px system-ui;margin:24px}
a{color:#d7b46a}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:20px}
article{background:#252b38;padding:12px}img{max-width:100%;image-rendering:pixelated}p{line-height:1.5}</style>
<h1>Barrowlight capture review</h1><p>Silent native gameplay captures, assembled from selected 12 fps frames.
GIFs use a 128-color preview palette. PNG posters retain the captured pixels. Prepared saves isolate appearance;
this montage is not an uninterrupted playthrough. Spoiler flags are supplied by the reviewer.</p>
<p><a href="teaser.gif">Play teaser (no declared spoilers)</a> · <a href="capture-receipt.json">Capture provenance</a></p><main>'''
    (output / "index.html").write_text(document + "\n".join(cards) + "</main></html>", encoding="utf-8")
    return receipt


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("output", type=Path, help="fresh output directory")
    args = parser.parse_args()
    receipt = build(args.manifest, args.output)
    print(f"{args.output}: {receipt['teaserSeconds']:g}-second silent teaser; {len(receipt['shots'])} reviewed clips")
