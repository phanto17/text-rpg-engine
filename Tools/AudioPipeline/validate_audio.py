"""Validate format, headroom, endpoints and reproducibility without an audio device."""
import argparse
import math
import struct
import wave
from pathlib import Path
from build_audio import RATE, manifest, render


def validate(path, asset, reproduce=False):
    with wave.open(str(path), "rb") as stream:
        if (stream.getnchannels(), stream.getsampwidth(), stream.getframerate(), stream.getcomptype()) != (1, 2, RATE, "NONE"):
            raise ValueError(f"{path}: unsupported PCM format")
        pcm = stream.readframes(stream.getnframes())
    samples = struct.unpack("<" + "h" * (len(pcm) // 2), pcm)
    if len(samples) != asset["milliseconds"] * RATE // 1000:
        raise ValueError(f"{path}: duration differs from manifest")
    peak = max(abs(v) for v in samples)
    rms = math.sqrt(sum(v * v for v in samples) / len(samples))
    if not 100 < peak <= asset["peak"] + 1 or rms < 10 or abs(sum(samples) / len(samples)) > 4:
        raise ValueError(f"{path}: silent, clipped or DC-biased output")
    if not asset["loop"] and (samples[0] != 0 or samples[-1] != 0):
        raise ValueError(f"{path}: effect endpoints must be silent")
    if asset["loop"] and abs(samples[0] - samples[-1]) > asset["peak"] // 2:
        raise ValueError(f"{path}: excessive loop seam step")
    if reproduce and pcm != render(asset):
        raise ValueError(f"{path}: PCM differs from deterministic recipe")
    print(f"OK {path.name}: peak {peak}, RMS {rms:.0f}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("--reproduce", action="store_true")
    args = parser.parse_args()
    for asset in manifest()["assets"]:
        validate(args.directory / (asset["id"] + ".wav"), asset, args.reproduce)
