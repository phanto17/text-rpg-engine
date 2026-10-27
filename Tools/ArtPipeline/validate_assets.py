"""Validate decoded pixels, dimensions and palette membership for the pilot pack."""
import argparse
import json
from pathlib import Path
from PIL import Image


def validate(path, width, height, palette):
    with Image.open(path) as image:
        if image.format != "PNG" or image.size != (width, height):
            raise ValueError(f"{path}: expected PNG {width}x{height}, got {image.format} {image.size}")
        pixels = image.convert("RGBA")
        if not pixels.getbbox():
            raise ValueError(f"{path}: empty image")
        data = pixels.tobytes()
        for index in range(0, len(data), 4):
            r, g, b, a = data[index:index + 4]
            if a not in (0, 255):
                raise ValueError(f"{path}: partial alpha {a}")
            if a and (r, g, b) not in palette:
                raise ValueError(f"{path}: off-palette color {(r,g,b)}")
        return data


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("--compare", type=Path)
    parser.add_argument("--source", choices=("pixelorama", "recipe"))
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    manifest = json.loads((root / "ArtSource/pilots.json").read_text())
    palette = {tuple(bytes.fromhex(c)) for c in manifest["palette"]}
    for asset in manifest["assets"]:
        if args.source and asset["source"] != args.source:
            continue
        relative = Path(asset["category"]) / (asset["id"] + ".png")
        pixels = validate(args.directory / relative, asset["width"], asset["height"], palette)
        if "frames" in asset:
            frames = asset["frames"]
            if not 1 < frames <= 16 or asset["width"] % frames or not 50 <= asset["frameMilliseconds"] <= 2000:
                raise ValueError(f"{relative}: invalid sheet timing or layout")
            stride = asset["width"] // frames * 4
            frame_data = [b"".join(pixels[y * asset["width"] * 4 + frame * stride:y * asset["width"] * 4 + (frame + 1) * stride] for y in range(asset["height"])) for frame in range(frames)]
            if len(set(frame_data)) < 2:
                raise ValueError(f"{relative}: animation has no distinct frames")
        if args.compare and pixels != validate(args.compare / relative, asset["width"], asset["height"], palette):
            raise ValueError(f"{relative}: pixels differ after export")
        print(f"OK {relative}")


if __name__ == "__main__":
    main()
