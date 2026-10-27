"""Package the version-pinned Pixelorama extension as a loadable ZIP resource pack."""
import argparse
from pathlib import Path
from zipfile import ZIP_STORED, ZipFile, ZipInfo

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Tools/PixeloramaExtension"
PREFIX = "src/Extensions/barrowlight_bridge/"


def build(output: Path) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    if output.exists():
        raise FileExistsError(f"use a fresh package path: {output}")
    with ZipFile(output, "w") as archive:
        for name in ("extension.json", "Main.tscn", "Main.gd"):
            item = ZipInfo(PREFIX + name, date_time=(2026, 1, 1, 0, 0, 0))
            item.compress_type = ZIP_STORED
            item.create_system = 3
            item.external_attr = 0o644 << 16
            # Git checkout line endings vary by OS; package canonical UTF-8 LF text.
            contents = (SOURCE / name).read_text(encoding="utf-8").replace("\r\n", "\n")
            archive.writestr(item, contents.encode("utf-8"))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    build(args.output)
    print(args.output)
