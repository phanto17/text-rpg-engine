"""Build a short SFX sampler from the reviewed files without an audio device."""
import argparse
import wave
from pathlib import Path
from build_audio import RATE, manifest, write

parser = argparse.ArgumentParser()
parser.add_argument("directory", type=Path)
parser.add_argument("output", type=Path)
args = parser.parse_args()
if args.output.exists():
    parser.error("use a fresh preview path")
chunks = []
for asset in manifest()["assets"]:
    if asset["loop"]:
        continue
    with wave.open(str(args.directory / (asset["id"] + ".wav")), "rb") as clip:
        chunks.append(clip.readframes(clip.getnframes()))
    chunks.append(b"\0\0" * (RATE // 3))
write(args.output, b"".join(chunks))
print(args.output)
