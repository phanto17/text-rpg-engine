"""Original integer-only synthesis: reproducible mono 22050 Hz, 16-bit PCM WAVs."""
import argparse
import json
import struct
import wave
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RATE = 22050
SINE = (0, 6393, 12539, 18204, 23170, 27245, 30273, 32137,
        32767, 32137, 30273, 27245, 23170, 18204, 12539, 6393,
        0, -6393, -12539, -18204, -23170, -27245, -30273, -32137,
        -32767, -32137, -30273, -27245, -23170, -18204, -12539, -6393)


def noise(count, seed):
    result = []
    state = seed
    for _ in range(count):
        state ^= (state << 13) & 0xFFFFFFFF
        state ^= state >> 17
        state ^= (state << 5) & 0xFFFFFFFF
        result.append((state & 65535) - 32768)
    return result


def tone(index, frequency):
    return SINE[(index * frequency * 32 // RATE) % 32]


def envelope(index, length, attack=110):
    return min(1024, index * 1024 // attack) * (length - index) ** 2 // length ** 2


def render(asset):
    count = asset["milliseconds"] * RATE // 1000
    random = noise(count, asset["seed"])
    kind = asset["id"]
    samples = []
    filtered = 0
    for i in range(count):
        filtered = (filtered * 7 + random[i]) // 8
        if kind == "menu_select":
            sample = tone(i, 660) * envelope(i, count) // 1024
        elif kind == "footstep":
            local = i % (RATE * 16 // 100)
            length = RATE * 12 // 100
            sample = ((filtered * 2 + tone(local, 90)) // 3) * envelope(local, length) // 1024 if local < length else 0
        elif kind == "weapon_impact":
            sample = ((random[i] + tone(i, 135) * 2 + tone(i, 410)) // 4) * envelope(i, count, 40) // 1024
        elif kind == "damage":
            sample = ((filtered + tone(i, 70) * 2) // 3) * envelope(i, count, 180) // 1024
        elif kind == "lantern_ignite":
            sample = ((tone(i, 220) + tone(i, 330) + tone(i, 440) + filtered) // 4) * envelope(i, count, 1100) // 1024
        elif kind == "moor_rain":
            # Circular smoothing keeps the loop seam equivalent to other adjacent samples.
            sample = sum(random[(i - k) % count] for k in range(6)) // 6
        elif kind == "ossuary_drips":
            sample = sum(random[(i - k) % count] for k in range(24)) // 96
            for onset in (RATE, RATE * 27 // 10, RATE * 61 // 10):
                for delay, strength in ((0, 4), (RATE // 5, 1), (RATE * 2 // 5, 1)):
                    local = i - onset - delay
                    length = RATE // 5
                    if 0 <= local < length:
                        sample += tone(local, 740) * envelope(local, length, 20) * strength // 8192
        elif kind == "barrow_resonance":
            swell = 700 + SINE[(i * 32 // count) % 32] * 160 // 32768
            sample = (tone(i, 55) + tone(i, 83)) * swell // 2048
        else:
            raise ValueError(f"unknown recipe {kind}")
        samples.append(sample)
    # Remove synthesis DC and normalize conservatively, leaving ample runtime headroom.
    mean = sum(samples) // len(samples)
    samples = [value - mean for value in samples]
    peak = max(abs(value) for value in samples) or 1
    ceiling = asset["peak"]
    samples = [max(-32767, min(32767, value * ceiling // peak)) for value in samples]
    if not asset["loop"]:
        samples[0] = samples[-1] = 0
    return struct.pack("<" + "h" * len(samples), *samples)


def manifest():
    return json.loads((ROOT / "ArtSource/audio.json").read_text(encoding="utf-8"))


def write(path, pcm):
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as output:
        output.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        output.writeframes(pcm)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    for asset in manifest()["assets"]:
        path = args.directory / (asset["id"] + ".wav")
        write(path, render(asset))
        print(path)
