"""Original Act I spoke art; jars, floodgate, water and echoes remain live overlays."""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw
from build_pilots import P


def chapel():
    image = Image.new("RGBA", (304, 140), P[0])
    d = ImageDraw.Draw(image)
    d.ellipse((20, -34, 284, 133), fill=P[1])
    for x, height in ((9, 74), (43, 93), (78, 65), (224, 68), (258, 96), (292, 72)):
        d.polygon([(x - 10, 110), (x - 7, 25), (x, 110 - height), (x + 8, 34), (x + 11, 110)], fill=P[2])
        d.line((x - 5, 45, x - 4, 106), fill=P[3], width=2)
        d.line((x + 4, 61, x + 5, 101), fill=P[4])
    d.polygon([(0, 111), (303, 111), (303, 139), (0, 139)], fill=P[2])
    for y in range(116, 140, 9):
        d.line((0, y, 303, y), fill=P[3])
    # A mass of unequal bone pipes frames a black organ throat.
    for index in range(11):
        x, top = 98 + index * 10, 17 + abs(5 - index) * 7
        d.rectangle((x, top, x + 6, 89), fill=P[4])
        d.line((x + 1, top + 2, x + 1, 83), fill=P[5])
        d.rectangle((x + 2, top + 5, x + 4, top + 10), fill=P[0])
        d.line((x, 78, x + 6, 78), fill=P[3])
    d.rectangle((104, 86, 204, 105), fill=P[8])
    d.rectangle((110, 87, 197, 91), fill=P[5])
    for x in range(114, 197, 6):
        d.rectangle((x, 87, x + 1, 89), fill=P[0])
    for x in (108, 197):
        d.rectangle((x, 94, x + 3, 104), fill=P[12])
    for y, inset in ((105, 0), (110, -7), (115, -14)):
        d.rectangle((99 + inset, y, 209 - inset, y + 4), fill=P[3])
        d.line((99 + inset, y, 209 - inset, y), fill=P[4])
    for i in range(70):
        x, y = 90 + (i * 29) % 127, 81 + (i * 17) % 44
        d.point((x, y), fill=P[5] if i % 5 == 0 else P[4])
    for x in (23, 270):
        d.rectangle((x, 99, x + 10, 114), fill=P[8])
        d.rectangle((x + 2, 98, x + 8, 100), fill=P[12])
    return image


def bridge():
    image = Image.new("RGBA", (304, 140), P[0])
    d = ImageDraw.Draw(image)
    d.polygon([(0, 28), (43, 15), (91, 26), (143, 9), (201, 22), (265, 8), (303, 19), (303, 114), (0, 114)], fill=P[1])
    for left in (5, 106, 207):
        d.rectangle((left, 20, left + 91, 96), fill=P[2])
        d.rounded_rectangle((left + 12, 28, left + 79, 123), radius=30, fill=P[0])
        d.arc((left + 10, 26, left + 81, 104), 180, 360, fill=P[4], width=3)
        for x in range(left + 3, left + 91, 15):
            d.line((x, 21, x + 1, 29), fill=P[3])
        d.line((left + 2, 53, left + 2, 99), fill=P[4])
    d.rectangle((0, 96, 303, 112), fill=P[3])
    d.line((0, 96, 303, 96), fill=P[5])
    for x in range(6, 304, 21):
        d.line((x, 98, x - 5, 110), fill=P[2])
        d.rectangle((x, 90, x + 2, 93), fill=P[9])
    for left in (28, 130, 233):
        d.rectangle((left, 108, left + 21, 139), fill=P[2])
        d.line((left, 109, left, 138), fill=P[4])
    # Guardhouse and cage are empty shapes; no duplicate character or stolen lens is painted.
    d.rectangle((212, 64, 248, 96), fill=P[2])
    d.polygon([(206, 65), (230, 51), (252, 65)], fill=P[3])
    d.rectangle((222, 74, 236, 95), fill=P[0])
    d.line((211, 65, 248, 65), fill=P[4])
    d.rectangle((71, 69, 76, 97), fill=P[8])
    d.rectangle((64, 55, 84, 73), outline=P[4])
    for x in range(68, 84, 5):
        d.line((x, 56, x, 73), fill=P[3])
    return image


def figure(kind):
    image = Image.new("RGBA", (48, 48))
    d = ImageDraw.Draw(image)
    if kind == "choir_cantor_miriel":
        d.polygon([(19, 15), (29, 15), (35, 39), (39, 44), (10, 44), (15, 34)], fill=P[3])
        d.polygon([(17, 16), (24, 24), (32, 16), (35, 31), (25, 28), (13, 32)], fill=P[15])
        d.line((15, 19, 23, 26, 32, 20), fill=P[5])
        d.ellipse((19, 5, 29, 17), fill=P[5])
        d.polygon([(17, 15), (18, 5), (24, 2), (31, 6), (32, 18), (29, 13), (27, 7), (21, 6)], fill=P[8])
        d.point((22, 11), fill=P[0]); d.point((27, 11), fill=P[0])
        d.line((16, 24, 7, 30, 11, 33), fill=P[5], width=2)
        d.line((32, 24, 39, 29, 35, 33), fill=P[5], width=2)
        for x in range(17, 33, 5): d.line((x, 34, x - 2, 41), fill=P[4])
    elif kind == "calcified_mournling":
        d.polygon([(18, 17), (27, 17), (34, 29), (32, 43), (13, 43), (12, 31)], fill=P[4])
        d.ellipse((16, 5, 29, 19), fill=P[5])
        d.line((19, 12, 22, 12), fill=P[2]); d.line((25, 13, 27, 13), fill=P[2])
        d.line((22, 17, 25, 17), fill=P[3])
        d.line((16, 23, 20, 29, 28, 24), fill=P[6], width=3)
        for x, y in ((17, 6), (28, 16), (15, 30), (29, 35), (21, 38)):
            d.polygon([(x - 2, y + 3), (x, y - 2), (x + 3, y + 3)], fill=P[6])
        d.line((24, 20, 21, 25, 25, 31, 22, 40), fill=P[3])
    else:
        garin = kind == "tollbridge_warden_garin"
        d.polygon([(16, 16), (29, 16), (33, 27), (29, 35), (15, 35), (12, 26)], fill=P[3] if garin else P[2])
        d.line((17, 20, 28, 20), fill=P[4], width=2)
        d.line((17, 23, 18, 30, 27, 30), fill=P[13], width=2)
        d.rectangle((17, 5, 28, 16), fill=P[4])
        d.polygon([(16, 6), (21, 2), (28, 4), (30, 12), (16, 12)], fill=P[3])
        d.line((18, 11, 27, 11), fill=P[0], width=2)
        d.point((20, 11), fill=P[14])
        d.line((17, 34, 15, 43, 10, 44), fill=P[4], width=3)
        d.line((27, 34, 29, 43, 33, 44), fill=P[4], width=3)
        d.line((30, 21, 37, 25), fill=P[4], width=3)
        d.line((38, 5, 38, 44), fill=P[9], width=2)
        if garin:
            d.polygon([(37, 3), (43, 8), (45, 15), (39, 13), (34, 17), (35, 8)], fill=P[4])
            d.line((41, 9, 44, 14), fill=P[5])
            d.rectangle((10, 18, 15, 30), fill=P[8])
            d.line((12, 19, 12, 27), fill=P[9])
        else:
            d.polygon([(38, 0), (41, 8), (38, 11), (35, 8)], fill=P[5])
            for x, y in ((16, 15), (28, 29), (20, 35)):
                d.line((x, y, x - 1, y + 5), fill=P[13])
    return image


def idle(kind):
    sheet = Image.new("RGBA", (192, 48))
    base = figure(kind)
    for index, shift in enumerate((0, 0, 1, 0)):
        frame = Image.new("RGBA", (48, 48))
        frame.alpha_composite(base, (shift, 0))
        sheet.alpha_composite(frame, (index * 48, 0))
    return sheet


def portrait(kind):
    image = Image.new("RGBA", (56, 56))
    d = ImageDraw.Draw(image)
    if kind == "miriel":
        d.polygon([(8, 55), (13, 37), (23, 32), (34, 32), (44, 39), (49, 55)], fill=P[3])
        d.polygon([(14, 35), (28, 43), (41, 35), (46, 49), (28, 46), (11, 51)], fill=P[15])
        d.line((14, 37, 27, 45, 41, 38), fill=P[5])
        d.ellipse((19, 11, 36, 34), fill=P[5])
        d.polygon([(16, 31), (16, 12), (22, 6), (33, 7), (39, 15), (38, 35), (34, 25), (33, 14), (22, 12), (21, 28)], fill=P[8])
        d.line((22, 23, 26, 24), fill=P[2]); d.line((30, 24, 34, 23), fill=P[2])
        d.line((26, 31, 30, 31), fill=P[4])
        d.rectangle((25, 47, 32, 54), outline=P[14])
        d.line((26, 46, 31, 46), fill=P[12])
    else:
        d.polygon([(7, 55), (10, 36), (22, 31), (36, 31), (47, 39), (49, 55)], fill=P[3])
        d.rectangle((18, 8, 39, 35), fill=P[3])
        d.polygon([(17, 16), (20, 6), (29, 3), (38, 8), (40, 19)], fill=P[4])
        d.line((20, 11, 27, 7, 34, 9), fill=P[5])
        # Raised visor distinguishes the relieved warden from his combat silhouette.
        d.rectangle((21, 17, 35, 30), fill=P[4])
        d.line((22, 19, 26, 19), fill=P[0]); d.line((30, 19, 34, 19), fill=P[0])
        d.point((24, 20), fill=P[14]); d.point((32, 20), fill=P[14])
        d.line((26, 27, 31, 27), fill=P[2])
        d.line((15, 39, 40, 39), fill=P[4], width=2)
        d.line((22, 42, 22, 51, 34, 51), fill=P[13], width=2)
        d.line((43, 30, 43, 55), fill=P[9], width=2)
    return image


MONSTERS = ("choir_cantor_miriel", "calcified_mournling", "tollbridge_warden_garin", "drowned_lancer")
ASSETS = {("Rooms", "echo_chapel"): chapel, ("Rooms", "drowned_tollbridge"): bridge,
          **{("Portraits", name): (lambda name=name: portrait(name)) for name in ("miriel", "garin")},
          **{("Monsters", name + "_idle"): (lambda name=name: idle(name)) for name in MONSTERS}}

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    for (category, name), create in ASSETS.items():
        destination = args.directory / category / (name + ".png")
        destination.parent.mkdir(parents=True, exist_ok=True)
        create().save(destination)
        print(destination)
