"""Generate the first reviewed UI, tile and deliberate four-step idle sheet."""
import argparse
from pathlib import Path

from PIL import Image, ImageDraw

from build_pilots import P, knight


def knight_idle() -> Image.Image:
    base = knight()
    sheet = Image.new("RGBA", (192, 48))
    for index, rise in enumerate((0, 0, 1, 0)):
        frame = Image.new("RGBA", (48, 48))
        frame.alpha_composite(base, (0, rise))
        draw = ImageDraw.Draw(frame)
        # A restrained ember pulse and a single-pixel helm shift, held for 200 ms.
        if index == 2:
            draw.point((21, 10), fill=P[11])
            draw.point((26, 10), fill=P[11])
        if index == 3:
            draw.point((38, 18), fill=P[12])
        sheet.alpha_composite(frame, (index * 48, 0))
    return sheet


def lantern_icon() -> Image.Image:
    image = Image.new("RGBA", (16, 16))
    draw = ImageDraw.Draw(image)
    draw.rectangle((5, 1, 10, 2), fill=P[5])
    draw.rectangle((4, 3, 11, 12), fill=P[3])
    draw.rectangle((6, 4, 9, 10), fill=P[12])
    draw.rectangle((7, 5, 8, 8), fill=P[7])
    draw.line((4, 13, 11, 13), fill=P[5])
    draw.point((3, 5), fill=P[4])
    return image


def crypt_stone() -> Image.Image:
    image = Image.new("RGBA", (16, 16), P[2])
    draw = ImageDraw.Draw(image)
    draw.rectangle((1, 1, 7, 6), fill=P[3])
    draw.rectangle((9, 1, 15, 6), fill=P[3])
    draw.rectangle((1, 8, 12, 14), fill=P[3])
    draw.line((2, 2, 6, 2), fill=P[4])
    draw.line((10, 2, 14, 2), fill=P[4])
    draw.line((2, 9, 10, 9), fill=P[4])
    draw.line((7, 10, 8, 11), fill=P[1])
    return image


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    for category, name, image in (
        ("Monsters", "gravebound_knight_idle", knight_idle()),
        ("UI", "lantern_icon", lantern_icon()),
        ("Tiles", "crypt_stone", crypt_stone()),
    ):
        destination = args.directory / category / (name + ".png")
        destination.parent.mkdir(parents=True, exist_ok=True)
        image.save(destination)
        print(destination)


if __name__ == "__main__":
    main()
