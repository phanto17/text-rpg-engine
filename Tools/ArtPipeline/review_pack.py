"""Assemble an offline visual/listening review from shipped data and native captures.

No game process, browser, audio device or editor is opened. Outputs are review artifacts,
not new runtime assets. Measured levels never imply subjective mix approval.
"""
import argparse
import html
import json
import math
import struct
import wave
from pathlib import Path, PurePosixPath

from PIL import Image, ImageDraw

RATE = 22050
DEFAULT_AMBIENCE = 0.20
DEFAULT_EFFECTS = 0.45


def asset_path(data, relative):
    if not isinstance(relative, str) or not relative or "\\" in relative or ":" in relative:
        raise ValueError(f"Invalid relative asset path: {relative!r}")
    parts = PurePosixPath(relative)
    if parts.is_absolute() or ".." in parts.parts:
        raise ValueError(f"Invalid relative asset path: {relative!r}")
    path = (data / relative).resolve()
    if not path.is_relative_to(data.resolve()) or not path.is_file():
        raise ValueError(f"Missing or escaping asset: {relative}")
    return path


def png(path, size=None):
    with Image.open(path) as image:
        if image.format != "PNG" or (size is not None and image.size != size):
            raise ValueError(f"Unexpected PNG size/format: {path}")
        return image.convert("RGBA")


def dbfs(value):
    return round(20 * math.log10(value / 32768), 2) if value else None


def audio_preview(source, destination, gain):
    with wave.open(str(source), "rb") as stream:
        if (stream.getnchannels(), stream.getsampwidth(), stream.getframerate(), stream.getcomptype()) != (1, 2, RATE, "NONE"):
            raise ValueError(f"Unsupported review audio: {source}")
        count = stream.getnframes()
        if not 0 < count <= RATE * 30:
            raise ValueError(f"Review clip must be 0..30 seconds: {source}")
        pcm = stream.readframes(count)
    samples = struct.unpack("<" + "h" * count, pcm)
    peak = max(abs(value) for value in samples)
    rms = math.sqrt(sum(value * value for value in samples) / count)
    scaled = [round(value * gain) for value in samples]
    with wave.open(str(destination), "wb") as output:
        output.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        output.writeframes(struct.pack("<" + "h" * count, *scaled))
    return {"seconds": round(count / RATE, 3), "peakDbfs": dbfs(peak), "rmsDbfs": dbfs(rms),
            "previewGain": gain, "previewPeakDbfs": dbfs(max(abs(value) for value in scaled)),
            "loopStep": abs(samples[0] - samples[-1])}


def contact_sheet(entries, destination, columns, cell):
    if not entries:
        return
    width, height = cell
    sheet = Image.new("RGB", (columns * width, math.ceil(len(entries) / columns) * height), "#151923")
    draw = ImageDraw.Draw(sheet)
    for i, (label, image) in enumerate(entries):
        x, y = i % columns * width, i // columns * height
        sheet.paste(image, (x, y), image if image.mode == "RGBA" else None)
        draw.text((x + 3, y + height - 16), label, fill="#E5DDC8")
    sheet.save(destination)


def build(data, output, captures=None):
    data, output = Path(data).resolve(), Path(output).absolute()
    presentation = json.loads((data / "World/presentation.json").read_text(encoding="utf-8"))
    # A failed or partial run is left for inspection; it can never overwrite an earlier review.
    output.mkdir(parents=True, exist_ok=False)
    media = output / "media"
    media.mkdir()
    report = {"subjectiveAudioReview": "pending", "rooms": [], "monsters": [], "npcs": [], "effects": []}
    room_images, figure_images, cards = [], [], []
    escape = lambda value: html.escape(str(value), quote=True)

    def save_image(image):
        name = f"media/image_{len(list(media.glob('image_*.png'))):03}.png"
        image.save(output / name)
        return name

    def sound(relative, gain):
        source = asset_path(data, relative)
        name = f"media/audio_{len(list(media.glob('audio_*.wav'))):03}.wav"
        metrics = audio_preview(source, output / name, gain)
        return name, metrics

    for room_id, look in presentation["rooms"].items():
        source = png(asset_path(data, look["backdropImage"]), (304, 140))
        entry = {"id": room_id, "image": look["backdropImage"], "nativeCapture": False}
        # Derive capture names from the final safe asset filename, never from unchecked IDs.
        capture_name = Path(look["backdropImage"]).name
        capture = Path(captures) / capture_name if captures else None
        if capture is not None and capture.is_file():
            image = png(capture)
            if image.size not in ((480, 270), (960, 540)):
                raise ValueError(f"Expected native or 2x canvas capture: {capture}")
            image = image.resize((480, 270), Image.Resampling.NEAREST)
            entry["nativeCapture"] = True
            scene = image.crop((0, 0, 304, 140))
        else:
            image, scene = source, source
        room_images.append((room_id, scene))
        image_name = save_image(image)
        audio_path = presentation.get("audio", {}).get("ambience", {}).get(room_id)
        audio_html = "<p>No ambience binding.</p>"
        if audio_path:
            audio_name, metrics = sound(audio_path, DEFAULT_AMBIENCE)
            entry["audio"] = {"path": audio_path, **metrics}
            audio_html = f'<audio controls loop preload="none" src="{audio_name}"></audio>'
            audio_html += f'<p>Source RMS {metrics["rmsDbfs"]} dBFS; preview gain 20%.</p>'
        status = "Native capture" if entry["nativeCapture"] else "Source only — native review pending"
        cards.append(f'<article><h2>{escape(room_id)}</h2><p>{status}</p>'
                     f'<a href="{image_name}"><img src="{image_name}" alt="{escape(room_id)}"></a>{audio_html}</article>')
        report["rooms"].append(entry)

    for section, field, size in (("monsters", "spriteImage", 48), ("npcs", "portraitImage", 56)):
        for identifier, look in presentation[section].items():
            image = png(asset_path(data, look[field]))
            frames = look.get("animation", {}).get("frames", 1)
            if not isinstance(frames, int) or not 1 <= frames <= 16 or image.size != (size * frames, size):
                raise ValueError(f"Unexpected figure layout: {identifier}")
            strip = Image.new("RGBA", (max(192, image.width), 64))
            strip.alpha_composite(image)
            figure_images.append((identifier, strip))
            report[section].append({"id": identifier, "image": look[field], "frames": frames,
                                    "frameMilliseconds": look.get("animation", {}).get("frameMilliseconds")})
    contact_sheet(room_images, output / "rooms.png", 3, (304, 160))
    contact_sheet(figure_images, output / "figures.png", 3, (max(image.width for _, image in figure_images), 84))

    for role, relative in presentation.get("audio", {}).get("effects", {}).items():
        name, metrics = sound(relative, DEFAULT_EFFECTS)
        report["effects"].append({"role": role, "path": relative, **metrics})
        cards.append(f'<article><h2>Effect: {escape(role)}</h2><audio controls preload="none" src="{name}"></audio>'
                     f'<p>Source RMS {metrics["rmsDbfs"]} dBFS; preview gain 45%.</p></article>')
    (output / "review.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    document = '''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width">
<title>Barrowlight presentation review</title><style>
body{background:#151923;color:#e5ddc8;font:16px system-ui;margin:24px}a{color:#d7b46a}
main{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:18px}
article{background:#252b38;padding:12px}h2{font-size:17px}img{width:100%;max-width:960px;image-rendering:pixelated}
audio{width:100%}p{line-height:1.5}header{max-width:1000px;margin-bottom:24px}</style><header>
<h1>Barrowlight presentation review</h1><p>Prepared-save room snapshots isolate appearance; they do not prove story progression.
Source-only entries still need native inspection. These previews use the default 20% ambience and 45% effects gain,
with no loudness normalization. Play one at a time. Room clips loop so the seam can be heard.</p>
<p>Subjective listening: pending. Peak/RMS measurements are sample values, not LUFS or true-peak certification.
Check seams, fatigue, transitions and intelligibility on headphones and speakers in the game.</p>
<p><a href="rooms.png">Room contact sheet</a> · <a href="figures.png">Figure contact sheet</a> ·
<a href="review.json">Measured inventory</a></p></header><main>'''
    (output / "index.html").write_text(document + "\n".join(cards) + "</main></html>", encoding="utf-8")
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("data", type=Path)
    parser.add_argument("output", type=Path, help="fresh output directory")
    parser.add_argument("--captures", type=Path, help="optional native PNGs named after room backdrop files")
    args = parser.parse_args()
    result = build(args.data, args.output, args.captures)
    print(f"{args.output / 'index.html'}: {sum(room['nativeCapture'] for room in result['rooms'])}/{len(result['rooms'])} rooms captured; listening pending")
