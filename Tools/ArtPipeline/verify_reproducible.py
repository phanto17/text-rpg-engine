"""Compare reviewed PNG pixels with deterministic checked-in recipes."""
import json
import hashlib
from pathlib import Path
from tempfile import TemporaryDirectory

from build_pilots import room, knight, portrait
from build_production import knight_idle, lantern_icon, crypt_stone
from build_extension import build as build_extension

ROOT = Path(__file__).resolve().parents[2]
FONT_SHA256 = "c845473330b94c2079ce9af01c51ac8ba2d99c24f4d14c039843bbb8e642ebd8"
GENERATORS = {
    "lichgate": room,
    "gravebound_knight": knight,
    "wren": portrait,
    "gravebound_knight_idle": knight_idle,
    "lantern_icon": lantern_icon,
    "crypt_stone": crypt_stone,
}


def verify() -> None:
    from validate_assets import validate

    manifest = json.loads((ROOT / "ArtSource/pilots.json").read_text(encoding="utf-8"))
    palette = {tuple(bytes.fromhex(c)) for c in manifest["palette"]}
    font = ROOT / "Data/Assets/Fonts/Silkscreen-Regular.ttf"
    if hashlib.sha256(font.read_bytes()).hexdigest() != FONT_SHA256:
        raise ValueError("Silkscreen font differs from the reviewed upstream file")
    if not (ROOT / "Data/Assets/Fonts/OFL.txt").is_file():
        raise ValueError("Silkscreen license is missing")
    with TemporaryDirectory() as directory:
        staging = Path(directory)
        package = staging / "barrowlight_bridge.zip"
        build_extension(package)
        reviewed_package = ROOT / "Tools/PixeloramaExtension/dist/0.3/barrowlight_bridge.zip"
        if package.read_bytes() != reviewed_package.read_bytes():
            raise ValueError("Pixelorama extension package differs from its checked-in sources")
        for asset in manifest["assets"]:
            relative = Path(asset["category"]) / (asset["id"] + ".png")
            generated = staging / relative
            generated.parent.mkdir(parents=True, exist_ok=True)
            GENERATORS[asset["id"]]().save(generated)
            reviewed = ROOT / "Data/Assets" / relative
            if validate(generated, asset["width"], asset["height"], palette) != validate(reviewed, asset["width"], asset["height"], palette):
                raise ValueError(f"{relative}: reviewed pixels differ from checked-in recipe")
            print(f"MATCH {relative}")


if __name__ == "__main__":
    verify()
