"""Reproducible prologue paintings; door, flames and lantern remain runtime overlays."""
import argparse
import math
from pathlib import Path
from PIL import Image, ImageDraw
from build_pilots import P


def masonry(draw, left, top, right, bottom):
    for row, y in enumerate(range(top, bottom, 9)):
        for x in range(left - (row % 2) * 12, right, 24):
            x0, x1 = max(left, x + 1), min(right, x + 22)
            if x0 >= x1:
                continue
            draw.rectangle((x0, y + 1, x1, min(bottom, y + 8)), fill=P[2])
            draw.line((x0 + 1, y + 1, x1, y + 1), fill=P[3])


def ossuary():
    image = Image.new("RGBA", (304, 140), P[0])
    draw = ImageDraw.Draw(image)
    masonry(draw, 0, 12, 303, 106)
    draw.polygon([(0, 104), (303, 104), (303, 139), (0, 139)], fill=P[8])
    for y in range(109, 140, 8):
        draw.line((0, y, 303, y), fill=P[2])
    for x in range(-60, 380, 32):
        draw.line((152, 94, x, 139), fill=P[1])
    # Ribbed vault and carved voussoirs surround the live bronze puzzle door.
    for inset in (0, 9, 18):
        draw.arc((58 + inset, -10 + inset, 246 - inset, 180 - inset), 186, 354, fill=P[3], width=3)
    draw.polygon([(110, 105), (110, 43), (119, 26), (133, 17), (152, 13),
                  (171, 17), (185, 26), (194, 43), (194, 105)], fill=P[3])
    draw.polygon([(119, 105), (119, 43), (127, 29), (140, 22), (152, 20),
                  (164, 22), (177, 29), (185, 43), (185, 105)], fill=P[0])
    for x, y in ((111, 41), (120, 27), (136, 18), (154, 15), (173, 22), (185, 35)):
        draw.line((x, y, x + 7, y + 3), fill=P[4])
    for side in (12, 208):
        for row in range(4):
            for col in range(6):
                x, y = side + col * 14, 30 + row * 17
                draw.rectangle((x - 2, y - 3, x + 8, y + 10), fill=P[0])
                draw.ellipse((x, y, x + 6, y + 6), fill=P[4])
                draw.rectangle((x + 1, y + 5, x + 5, y + 8), fill=P[4])
                draw.point((x + 1, y + 3), fill=P[0])
                draw.point((x + 4, y + 3), fill=P[0])
                draw.point((x + 3, y + 8), fill=P[1])
                draw.line((x - 2, y + 11, x + 8, y + 11), fill=P[3])
    for x in (4, 99, 199, 293):
        draw.rectangle((x, 18, x + 6, 106), fill=P[3])
        draw.line((x + 1, 18, x + 1, 103), fill=P[4])
        draw.rectangle((x - 2, 103, x + 9, 107), fill=P[2])
    for index in range(60):
        x, y = (index * 47) % 304, 110 + (index * 13) % 29
        draw.point((x, y), fill=P[4] if index % 5 == 0 else P[3])
    return image


def crypt():
    image = Image.new("RGBA", (304, 140), P[0])
    draw = ImageDraw.Draw(image)
    draw.ellipse((16, 4, 288, 92), fill=P[2])
    draw.ellipse((29, 9, 275, 82), fill=P[1])
    draw.arc((17, 5, 287, 91), 180, 360, fill=P[4], width=2)
    for x in (22, 72, 224, 274):
        draw.rectangle((x - 7, 42, x + 7, 108), fill=P[2])
        draw.line((x - 6, 43, x - 6, 104), fill=P[4])
        draw.rectangle((x - 11, 38, x + 11, 43), fill=P[3])
        draw.rectangle((x - 10, 103, x + 10, 109), fill=P[3])
        for y in range(50, 102, 13):
            draw.line((x - 5, y, x + 7, y), fill=P[1])
    draw.polygon([(0, 107), (303, 107), (303, 139), (0, 139)], fill=P[2])
    draw.ellipse((60, 90, 244, 139), fill=P[3])
    draw.ellipse((79, 96, 225, 131), fill=P[2])
    # The worn circle records Halric's endless patrol without depicting a second knight.
    for index in range(16):
        angle = index * math.tau / 16
        x, y = round(152 + math.cos(angle) * 79), round(114 + math.sin(angle) * 18)
        draw.line((x, y, x + 2, y + 1), fill=P[1], width=2)
    draw.polygon([(112, 87), (184, 87), (191, 102), (183, 109), (119, 109), (111, 102)], fill=P[1])
    draw.rectangle((118, 85, 185, 102), fill=P[3])
    draw.polygon([(113, 81), (181, 81), (190, 87), (121, 89)], fill=P[4])
    draw.line((120, 90, 185, 89), fill=P[5])
    draw.line((123, 96, 180, 96), fill=P[2])
    # A recumbent effigy carved into the lid, with room for the live lantern overlay.
    draw.line((126, 84, 171, 84), fill=P[3], width=2)
    draw.ellipse((126, 81, 133, 85), fill=P[5])
    draw.line((138, 82, 162, 86), fill=P[5])
    for index in range(100):
        x, y = (index * 41) % 304, 111 + (index * 7) % 29
        draw.point((x, y), fill=P[4] if index % 9 == 0 else P[3])
    return image


def thrall():
    image = Image.new("RGBA", (48, 48))
    draw = ImageDraw.Draw(image)
    draw.polygon([(18, 17), (28, 15), (33, 24), (34, 37), (28, 43), (15, 42), (12, 32)], fill=P[8])
    draw.polygon([(18, 19), (26, 18), (29, 27), (25, 33), (15, 31)], fill=P[3])
    draw.polygon([(19, 5), (28, 6), (30, 13), (25, 19), (18, 15), (17, 10)], fill=P[4])
    draw.line((20, 7, 26, 8), fill=P[6])
    draw.rectangle((19, 10, 21, 12), fill=P[0])
    draw.rectangle((25, 11, 27, 13), fill=P[0])
    draw.point((20, 11), fill=P[14])
    draw.line((21, 15, 25, 16), fill=P[0])
    for y in (22, 26, 30):
        draw.line((19, y, 26, y + 1), fill=P[4])
    draw.line((14, 21, 8, 29, 10, 35), fill=P[4], width=3)
    draw.line((29, 21, 37, 27, 35, 33), fill=P[4], width=3)
    draw.line((18, 33, 16, 42, 12, 44), fill=P[4], width=3)
    draw.line((26, 34, 29, 42, 33, 44), fill=P[4], width=3)
    draw.line((18, 20, 15, 36, 23, 39), fill=P[9])
    return image


def thrall_idle():
    sheet = Image.new("RGBA", (192, 48))
    base = thrall()
    for index, offset in enumerate((0, 1, 1, 0)):
        frame = Image.new("RGBA", (48, 48))
        frame.alpha_composite(base, (offset, 0))
        if index == 2:
            ImageDraw.Draw(frame).point((21, 11), fill=P[5])
        sheet.alpha_composite(frame, (index * 48, 0))
    return sheet


ASSETS = {("Rooms", "ossuary_hall"): ossuary, ("Rooms", "inner_crypt"): crypt,
          ("Monsters", "barrow_thrall"): thrall, ("Monsters", "barrow_thrall_idle"): thrall_idle}

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    for (category, name), create in ASSETS.items():
        destination = args.directory / category / (name + ".png")
        destination.parent.mkdir(parents=True, exist_ok=True)
        create().save(destination)
        print(destination)
