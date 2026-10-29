"""Compile a bounded Pixel --record frame sequence into a review GIF."""
import argparse
from pathlib import Path
from PIL import Image


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--last-seconds", type=int, default=20)
    args = parser.parse_args()
    if not 1 <= args.last_seconds <= 120:
        parser.error("last-seconds must be 1..120")
    if args.output.exists():
        parser.error("use a fresh output path")
    paths = sorted(args.directory.glob("frame_*.png"))
    if not paths or len(paths) > 1440:
        parser.error("expected 1..1440 recorded frames")
    paths = paths[-args.last_seconds * 12:]
    frames = []
    for path in paths:
        with Image.open(path) as image:
            if image.format != "PNG" or image.size != (480, 270):
                parser.error(f"unexpected recorded image: {path}")
            frames.append(image.convert("RGB").quantize(colors=128))
    durations = [round((i + 1) * 100 / 12) * 10 - round(i * 100 / 12) * 10 for i in range(len(frames))]
    frames[0].save(args.output, save_all=True, append_images=frames[1:],
                   duration=durations, loop=0, disposal=2, optimize=False)
    print(f"{args.output}: {len(frames)} frames at 12 fps")


if __name__ == "__main__":
    main()
