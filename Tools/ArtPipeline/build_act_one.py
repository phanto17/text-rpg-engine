"""Act I threshold paintings and portraits; live lamps and beacon are runtime overlays."""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw
from build_pilots import P


def stair():
    image = Image.new("RGBA", (304, 140), P[0])
    d = ImageDraw.Draw(image)
    # A narrow shaft opens onto a distant cathedral, below the curved landing.
    d.polygon([(86, 0), (221, 0), (235, 98), (65, 98)], fill=P[1])
    for x, height in ((104, 18), (124, 29), (162, 22), (187, 35), (208, 19)):
        d.rectangle((x, 99 - height, x + 7, 106), fill=P[2])
        d.polygon([(x - 2, 99 - height), (x + 3, 92 - height), (x + 9, 99 - height)], fill=P[3])
        d.point((x + 3, 102), fill=P[12])
    for side, edge in ((0, 90), (233, 304)):
        d.rectangle((side, 0, edge, 139), fill=P[2])
        for row, y in enumerate(range(0, 140, 8)):
            for x in range(side - (row % 2) * 14, edge, 29):
                left, right = max(side, x + 1), min(edge - 1, x + 26)
                if left < right:
                    d.rectangle((left, y + 1, right, y + 6), fill=P[1])
                    d.line((left, y + 1, right, y + 1), fill=P[3])
        for i in range(15):
            x, y = side + 5 + (i * 17) % (edge - side - 9), (i * 29) % 122
            d.line((x, y, x - 2, y + 13), fill=P[13])
            d.line((x, y + 3, x, y + 9), fill=P[14])
    # Broad wedge steps wind around the central black shaft.
    steps = [([(89, 7), (170, 12), (166, 20), (86, 14)], 3),
             ([(85, 23), (155, 27), (148, 35), (80, 31)], 3),
             ([(77, 42), (139, 44), (129, 54), (70, 52)], 4),
             ([(68, 63), (124, 60), (119, 72), (57, 76)], 4),
             ([(53, 86), (119, 78), (136, 88), (50, 101)], 4),
             ([(48, 107), (147, 91), (188, 104), (88, 124)], 4),
             ([(83, 130), (199, 109), (254, 120), (204, 139), (82, 139)], 4)]
    for points, highlight in steps:
        d.polygon([(x, y + 5) for x, y in points], fill=P[1])
        d.polygon(points, fill=P[3])
        d.line(points[:2], fill=P[highlight], width=2)
    d.polygon([(222, 0), (237, 0), (240, 117), (224, 113)], fill=P[3])
    d.line((224, 0, 226, 111), fill=P[4], width=2)
    for y in range(11, 114, 13):
        d.line((226, y, 237, y + 2), fill=P[1])
    # The pre-Order relief is a worn triad rather than a readable text panel.
    d.rectangle((15, 45, 57, 68), fill=P[3])
    d.rectangle((17, 47, 55, 66), outline=P[4])
    d.ellipse((21, 52, 27, 58), outline=P[5])
    d.arc((32, 51, 40, 60), 60, 300, fill=P[5], width=2)
    d.ellipse((45, 52, 51, 58), fill=P[4])
    d.point((46, 55), fill=P[0]); d.point((49, 55), fill=P[0])
    for i in range(65):
        x, y = 52 + (i * 31) % 83, 16 + (i * 19) % 119
        if image.getpixel((x, y)) in (P[3], P[4]):
            d.rectangle((x, y, x + 2, y + 1), fill=P[14] if i % 4 == 0 else P[13])
    return image


def nocturne():
    image = Image.new("RGBA", (304, 140), P[0])
    d = ImageDraw.Draw(image)
    d.polygon([(0, 31), (36, 17), (84, 33), (133, 12), (182, 26), (248, 8), (303, 27), (303, 109), (0, 109)], fill=P[1])
    for x in (15, 65, 226, 278):
        d.rectangle((x, 26, x + 10, 109), fill=P[2])
        d.line((x + 2, 28, x + 2, 105), fill=P[4])
        d.line((x + 6, 29, x + 6, 105), fill=P[3])
        d.rectangle((x - 4, 22, x + 15, 28), fill=P[3])
        d.rectangle((x - 4, 105, x + 15, 111), fill=P[3])
    for x in (-9, 42, 208, 259):
        d.arc((x, -16, x + 54, 89), 182, 358, fill=P[3], width=4)
    # Broken central ribs expose the cavern; warm stalls frame a cold empty beacon.
    d.line((80, 3, 111, 30, 121, 45), fill=P[2], width=5)
    d.line((222, 0, 190, 29, 183, 40), fill=P[2], width=5)
    d.polygon([(0, 107), (303, 107), (303, 139), (0, 139)], fill=P[2])
    for y in (111, 122, 136):
        d.line((0, y, 303, y), fill=P[3])
    for row, y in enumerate((111, 122)):
        for x in range(-30 + row * 24, 304, 48):
            d.line((x, y, x - 9, y + 13), fill=P[1])
    for left, right, top in ((29, 96, 73), (207, 274, 78)):
        d.rectangle((left + 3, top + 4, left + 5, 116), fill=P[4])
        d.rectangle((right - 5, top + 4, right - 3, 116), fill=P[4])
        d.polygon([(left - 4, top + 3), (left + 17, top - 11), (right - 16, top - 9), (right + 5, top + 5)], fill=P[8])
        d.line((left - 4, top + 3, left + 17, top - 11, right - 16, top - 9), fill=P[9], width=2)
        for x in range(left + 2, right, 9):
            d.line((x, top + 3, x + 2, top - 4), fill=P[9])
        d.rectangle((left, 101, right, 115), fill=P[8])
        d.line((left, 101, right, 101), fill=P[12])
        for i in range(5):
            x = left + 9 + i * 10
            d.rectangle((x, 95 - i % 2 * 3, x + 4, 100), fill=P[3])
            d.point((x + 1, 96 - i % 2 * 3), fill=P[14])
    # Beacon foundation only: the renderer owns the frame, lens and flame.
    d.ellipse((131, 103, 173, 115), fill=P[3])
    d.ellipse((137, 103, 167, 109), fill=P[4])
    for x, y in ((9, 90), (89, 84), (286, 96), (204, 61)):
        d.line((x, y, x, y + 9), fill=P[13], width=2)
        d.ellipse((x - 5, y - 2, x + 5, y + 2), fill=P[13])
        d.line((x - 4, y - 1, x + 4, y - 1), fill=P[14])
    # Sanctuary linen and the chalked pillar of names.
    d.rectangle((100, 104, 122, 108), fill=P[5])
    d.line((100, 109, 100, 116), fill=P[4]); d.line((122, 109, 122, 116), fill=P[4])
    d.rectangle((189, 55, 198, 114), fill=P[3])
    for y in range(72, 98, 4):
        d.line((191, y, 195 - y % 3, y), fill=P[5])
    return image


def portrait(kind):
    image = Image.new("RGBA", (56, 56))
    d = ImageDraw.Draw(image)
    coat = {"orin": 3, "maeve": 2, "peddler": 8}[kind]
    skin = 5 if kind == "orin" else 4
    d.polygon([(9, 55), (12, 38), (21, 32), (35, 32), (46, 39), (49, 55)], fill=P[coat])
    d.line((14, 40, 11, 54), fill=P[4], width=2)
    d.polygon([(20, 14), (35, 14), (38, 22), (34, 33), (27, 37), (20, 30), (18, 22)], fill=P[skin])
    d.polygon([(20, 17), (25, 14), (33, 16), (34, 26), (29, 32), (24, 30)], fill=P[6] if kind == "orin" else P[5])
    d.line((21, 22, 25, 22), fill=P[1]); d.line((30, 22, 34, 22), fill=P[1])
    d.line((27, 23, 26, 27, 29, 27), fill=P[4])
    d.line((25, 31, 30, 31), fill=P[3])
    if kind == "orin":
        d.polygon([(17, 19), (18, 11), (24, 8), (34, 10), (37, 16), (32, 13), (23, 13)], fill=P[3])
        d.polygon([(19, 35), (27, 39), (37, 34), (35, 51), (21, 51)], fill=P[8])
        d.rectangle((21, 43, 39, 53), fill=P[9])
        d.rectangle((23, 44, 37, 50), fill=P[6])
        d.line((30, 44, 30, 50), fill=P[4])
        d.polygon([(14, 45), (18, 43), (22, 49), (19, 53), (14, 51)], fill=P[5])
        for x, y in ((15, 46), (17, 49), (19, 51)):
            d.rectangle((x, y, x + 2, y + 2), fill=P[15])
    elif kind == "maeve":
        d.polygon([(16, 23), (16, 12), (22, 6), (33, 7), (38, 13), (39, 35), (35, 31), (34, 14), (22, 12), (20, 28)], fill=P[4])
        d.line((18, 17, 21, 10, 30, 9), fill=P[5], width=2)
        d.line((19, 37, 25, 46, 27, 54), fill=P[13], width=3)
        d.line((35, 37, 30, 46, 28, 54), fill=P[13], width=3)
        for x in (34, 38, 42):
            d.ellipse((x, 40, x + 4, 44), outline=P[12])
        d.rectangle((15, 48, 23, 53), fill=P[8])
        d.line((16, 47, 22, 47), fill=P[12])
    else:
        d.polygon([(12, 23), (16, 10), (26, 5), (39, 11), (44, 26), (36, 19), (32, 13), (21, 15), (18, 26)], fill=P[9])
        d.line((15, 19, 19, 12, 26, 8), fill=P[12])
        d.polygon([(17, 34), (26, 39), (38, 33), (35, 55), (22, 55)], fill=P[3])
        d.line((18, 35, 39, 50), fill=P[9], width=3)
        d.rectangle((14, 44, 20, 51), fill=P[13])
        d.rectangle((16, 42, 18, 43), fill=P[12])
        d.line((36, 43, 46, 43), fill=P[12])
        d.line((41, 40, 41, 51), fill=P[12])
        d.arc((35, 45, 39, 49), 0, 180, fill=P[12])
        d.arc((44, 45, 48, 49), 0, 180, fill=P[12])
    return image


ASSETS = {("Rooms", "weeping_stair"): stair, ("Rooms", "nocturne_square"): nocturne,
          **{("Portraits", kind): (lambda kind=kind: portrait(kind)) for kind in ("orin", "maeve", "peddler")}}

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    for (category, name), create in ASSETS.items():
        destination = args.directory / category / (name + ".png")
        destination.parent.mkdir(parents=True, exist_ok=True)
        create().save(destination)
        print(destination)
