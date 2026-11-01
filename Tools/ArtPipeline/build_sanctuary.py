"""Act III entrance family; relief, brazier and ally tokens remain live details."""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw
from build_pilots import P


def vestibule():
    image = Image.new("RGBA", (304, 140), P[0])
    d = ImageDraw.Draw(image)
    d.polygon([(92, 0), (233, 0), (250, 110), (77, 110)], fill=P[1])
    # Broad vitrified basalt faces and sparse reflections keep the depth nearly black.
    for left, right in ((0, 105), (241, 303)):
        d.rectangle((left, 0, right, 139), fill=P[2])
        for i in range(8):
            x = left + 5 + i * (right-left)//8
            d.line((x, 0, x-3, 139), fill=P[1], width=3)
            d.line((x+2, 22+i*7, x+1, 55+i*7), fill=P[3])
    for left, top, right in ((110, 12, 220), (103, 30, 205), (96, 49, 188),
                                               (88, 69, 174), (80, 90, 166), (74, 113, 181)):
        points = [(left, top), (right, top+7), (right-10, top+16), (left-6, top+9)]
        d.polygon([(x, y+5) for x, y in points], fill=P[1])
        d.polygon(points, fill=P[3])
        d.line(points[:2], fill=P[4], width=2)
        d.line((left+3, top+3, right-12, top+9), fill=P[2])
    d.polygon([(78, 137), (186, 122), (241, 138), (241, 139), (78, 139)], fill=P[2])
    d.line((79, 137, 186, 122, 240, 138), fill=P[4])
    # A bronze surround reserves the first panel for the live lead-filled relief.
    d.rectangle((8, 34, 106, 75), fill=P[8])
    d.rectangle((10, 36, 104, 73), outline=P[12])
    d.rectangle((251, 77, 295, 96), fill=P[8])
    for x in (257, 270):
        d.ellipse((x, 82, x+6, 88), outline=P[12])
    d.rectangle((283, 82, 289, 89), fill=P[4])
    for i in range(22):
        x, y = 100+i*29%130, 20+i*17%115
        if image.getpixel((x, y)) == P[3]:
            d.point((x, y), fill=P[5] if i%5 == 0 else P[4])
    return image


def landing():
    image = Image.new("RGBA", (304, 140), P[0])
    d = ImageDraw.Draw(image)
    d.polygon([(85, 94), (224, 94), (243, 139), (62, 139)], fill=P[1])
    # Petrified aqueduct ribs shelter the terrace above the unlit gulf.
    for left in (-21, 79, 179, 279):
        d.arc((left, -47, left+104, 102), 180, 360, fill=P[3], width=8)
        d.arc((left+4, -43, left+100, 98), 180, 360, fill=P[4], width=2)
    for x in (13, 88, 215, 289):
        d.rectangle((x-4, 18, x+4, 97), fill=P[2])
        d.line((x-2, 19, x-2, 94), fill=P[4])
        d.rectangle((x-7, 91, x+7, 97), fill=P[3])
    d.ellipse((7, 71, 297, 133), fill=P[3])
    d.ellipse((11, 70, 293, 126), fill=P[2])
    for y in (91, 104, 118):
        d.line((19, y, 284, y), fill=P[3])
    for x in range(24, 292, 33):
        d.line((152, 75, x, 122), fill=P[1])
    d.arc((7, 71, 297, 133), 0, 180, fill=P[4], width=2)
    # The causeway disappears into the abyss beyond the southern parapet.
    d.polygon([(134, 123), (167, 123), (180, 139), (122, 139)], fill=P[3])
    d.line((134, 124, 122, 139), fill=P[4])
    for y in (128, 133, 138): d.line((128, y, 174, y), fill=P[2])
    for x, y in ((112, 101), (129, 112), (177, 111), (192, 100)):
        d.rectangle((x-6, y, x+6, y+6), fill=P[3])
        d.line((x-5, y, x+5, y), fill=P[5])
    # Oakhaven's crates remain distinct from the movable ally tokens.
    for x, y in ((223, 89), (246, 94), (231, 106)):
        d.rectangle((x, y, x+18, y+12), fill=P[8])
        d.rectangle((x+1, y+1, x+17, y+11), outline=P[9])
        d.line((x+2, y+2, x+16, y+10), fill=P[4])
    for x in (248, 254, 261):
        d.rectangle((x, 88, x+3, 93), fill=P[13])
        d.point((x+1, 87), fill=P[12])
    d.rectangle((66, 66, 71, 112), fill=P[3])
    d.line((67, 67, 67, 109), fill=P[5])
    return image


def oakhaven():
    image = Image.new("RGBA", (56, 56))
    d = ImageDraw.Draw(image)
    d.polygon([(7, 55), (10, 37), (21, 31), (36, 31), (47, 40), (49, 55)], fill=P[3])
    d.ellipse((19, 9, 37, 34), fill=P[4])
    d.polygon([(18, 21), (18, 12), (23, 6), (34, 7), (39, 13), (39, 26), (35, 19), (31, 12), (23, 12)], fill=P[5])
    d.line((22, 22, 26, 22), fill=P[0]); d.line((31, 22, 35, 22), fill=P[0])
    d.line((27, 23, 26, 28, 30, 28), fill=P[3])
    d.line((25, 31, 32, 31), fill=P[2])
    # Salt-crusted brow, an old armorer's apron, vials and a foundation-bronze tool.
    for x, y in ((19, 17), (21, 14), (36, 27)):
        d.polygon([(x-1, y+3), (x, y-2), (x+3, y+3)], fill=P[6])
    d.polygon([(17, 34), (28, 39), (40, 34), (38, 55), (19, 55)], fill=P[8])
    d.line((18, 36, 23, 53), fill=P[9], width=2)
    for x in (23, 29, 35):
        d.rectangle((x, 44, x+3, 51), fill=P[13])
        d.point((x+1, 43), fill=P[12])
    d.line((13, 42, 17, 54), fill=P[9], width=2)
    d.rectangle((10, 39, 17, 43), fill=P[12])
    return image


ASSETS = {("Rooms", "threshold_vestibule"): vestibule, ("Rooms", "penitent_landing"): landing,
          ("Portraits", "oakhaven"): oakhaven}

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    for (category, name), create in ASSETS.items():
        path = args.directory / category / (name + ".png")
        path.parent.mkdir(parents=True, exist_ok=True)
        create().save(path)
        print(path)
