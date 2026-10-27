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
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    manifest = json.loads((root / "ArtSource/pilots.json").read_text())
    palette = {tuple(bytes.fromhex(c)) for c in manifest["palette"]}
    for asset in manifest["assets"]:
        relative = Path(asset["category"]) / (asset["id"] + ".png")
        pixels = validate(args.directory / relative, asset["width"], asset["height"], palette)
        if args.compare and pixels != validate(args.compare / relative, asset["width"], asset["height"], palette):
            raise ValueError(f"{relative}: pixels differ after export")
        print(f"OK {relative}")


if __name__ == "__main__":
    main()
