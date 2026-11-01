"""Original Iron Reliquary art; basin, sluice, furnace and seal stay live."""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw
from build_pilots import P


def floor(draw):
    draw.rectangle((0, 106, 303, 139), fill=P[2])
    for y in (112, 124, 138):
        draw.line((0, y, 303, y), fill=P[3])
    for x in range(-40, 350, 36):
        draw.line((152, 94, x, 139), fill=P[1])


def cloister():
    image = Image.new("RGBA", (304, 140), P[1])
    d = ImageDraw.Draw(image)
    d.rectangle((0, 16, 303, 106), fill=P[3])
    floor(d)
    for left in (7, 80, 153, 226):
        d.rectangle((left, 28, left + 65, 102), fill=P[2])
        d.rectangle((left + 3, 31, left + 62, 36), fill=P[4])
        for y in (67, 91):
            d.rectangle((left + 5, y, left + 60, y + 5), fill=P[3])
            d.line((left + 5, y, left + 60, y), fill=P[5])
        for x in (left + 12, left + 32, left + 49):
            d.ellipse((x, 42, x + 9, 51), fill=P[4])
            d.rectangle((x, 47, x + 9, 55), fill=P[4])
            d.line((x + 2, 49, x + 7, 49), fill=P[0])
        for x in (left, left + 64):
            d.rectangle((x, 22, x + 4, 106), fill=P[4])
            d.line((x + 1, 24, x + 1, 103), fill=P[5])
    # An anonymous armory rack and confessional grate leave the quartermaster to the portrait UI.
    for x in (16, 24, 32):
        d.line((x, 94, x, 124), fill=P[8], width=2)
        d.polygon([(x, 91), (x + 4, 97), (x, 100)], fill=P[4])
    d.polygon([(117, 117), (178, 117), (190, 130), (108, 130)], fill=P[0])
    for x in range(115, 184, 7):
        d.line((x, 119, x + 3, 129), fill=P[4])
    for i in range(70):
        x, y = i * 41 % 304, 18 + i * 17 % 85
        if image.getpixel((x, y)) == P[3]:
            d.line((x, y, min(303, x + 3), y), fill=P[4])
    return image


def cistern():
    image = Image.new("RGBA", (304, 140), P[0])
    d = ImageDraw.Draw(image)
    for left in (3, 79, 155, 231):
        d.rounded_rectangle((left, 9, left + 68, 112), radius=30, fill=P[3])
        d.rounded_rectangle((left + 6, 16, left + 62, 113), radius=27, fill=P[1])
        d.arc((left + 1, 10, left + 67, 86), 180, 360, fill=P[4], width=2)
    floor(d)
    for x in (34, 92, 210, 271):
        d.rectangle((x, 0, x + 5, 78), fill=P[2])
        d.line((x + 1, 1, x + 1, 75), fill=P[4])
        d.rectangle((x - 2, 72, x + 8, 78), fill=P[4])
    d.ellipse((69, 77, 235, 122), fill=P[4])
    d.ellipse((73, 80, 231, 119), fill=P[5])
    d.ellipse((78, 83, 226, 115), fill=P[1])
    # Liquid surface and valve motion are renderer-owned.
    d.rectangle((145, 25, 157, 83), fill=P[2])
    d.line((147, 26, 147, 79), fill=P[4])
    d.rectangle((140, 69, 164, 75), fill=P[4])
    d.polygon([(73, 112), (88, 118), (48, 139), (23, 139)], fill=P[1])
    d.line((73, 113, 30, 139), fill=P[5])
    d.polygon([(218, 111), (229, 117), (283, 139), (258, 139)], fill=P[1])
    d.line((229, 118, 280, 139), fill=P[4])
    for x, y in ((83, 91), (218, 96), (59, 126), (251, 129)):
        d.rectangle((x, y, x + 3, y + 1), fill=P[6])
    return image


def bastion():
    image = Image.new("RGBA", (304, 140), P[1])
    d = ImageDraw.Draw(image)
    d.rectangle((0, 31, 303, 107), fill=P[2])
    for y in range(38, 107, 13):
        d.line((0, y, 303, y), fill=P[3])
    for x in range(0, 304, 30):
        d.rectangle((x, 22, x + 18, 34), fill=P[3])
        d.line((x, 22, x + 18, 22), fill=P[4])
    floor(d)
    for x in (30, 264):
        d.rectangle((x, 20, x + 10, 110), fill=P[3])
        d.line((x + 2, 21, x + 2, 107), fill=P[5])
        d.rectangle((x - 5, 16, x + 15, 22), fill=P[4])
    d.polygon([(105, 102), (105, 55), (118, 37), (152, 29), (186, 37), (199, 55), (199, 102)], fill=P[3])
    d.polygon([(115, 102), (115, 57), (125, 45), (152, 38), (180, 45), (189, 57), (189, 102)], fill=P[0])
    for x in (74, 216):
        d.rectangle((x, 47, x + 11, 76), fill=P[1])
        d.line((x + 2, 49, x + 2, 73), fill=P[4])
    d.rectangle((63, 104, 102, 118), fill=P[8])
    d.polygon([(58, 100), (98, 100), (106, 105), (64, 109)], fill=P[4])
    d.rectangle((69, 98, 88, 103), fill=P[5])
    for x in (214, 222, 230):
        d.rectangle((x, 98, x + 5, 113), fill=P[3])
        d.line((x + 1, 99, x + 1, 111), fill=P[4])
    return image


def figure(kind):
    image = Image.new("RGBA", (48, 48))
    d = ImageDraw.Draw(image)
    commander = kind == "commander_silas"
    armor = P[5] if commander else P[4]
    d.polygon([(15, 17), (29, 17), (33, 27), (29, 35), (14, 35), (11, 26)], fill=armor)
    d.line((16, 22, 28, 22), fill=P[3], width=2)
    d.line((18, 25, 19, 31, 27, 31), fill=P[4] if commander else P[2])
    d.rectangle((17, 5, 28, 17), fill=armor)
    d.polygon([(16, 6), (20, 2), (27, 3), (30, 8), (29, 11), (16, 11)], fill=P[4] if commander else P[3])
    d.line((19, 11, 26, 11), fill=P[0])
    d.line((16, 34, 14, 44, 10, 45), fill=P[4], width=3)
    d.line((27, 34, 30, 44, 34, 45), fill=P[4], width=3)
    d.line((30, 21, 38, 25), fill=armor, width=3)
    d.line((39, 6, 39, 45), fill=P[8], width=2)
    d.polygon([(39, 1), (44, 10), (43, 17), (39, 13), (34, 16), (36, 7)], fill=P[5])
    d.line((41, 8, 43, 14), fill=P[6])
    if commander:
        d.rectangle((9, 18, 15, 27), fill=P[6])
        d.line((14, 18, 12, 36), fill=P[8], width=3)
        d.line((13, 37, 23, 43), fill=P[9], width=2)
    else:
        d.line((14, 19, 10, 31), fill=P[3], width=3)
    return image


def idle(kind):
    sheet = Image.new("RGBA", (192, 48))
    base = figure(kind)
    for index, shift in enumerate((0, 0, 1, 0)):
        sheet.alpha_composite(base, (index * 48 + shift, 0))
    return sheet


def portrait(kind):
    image = Image.new("RGBA", (56, 56))
    d = ImageDraw.Draw(image)
    d.polygon([(8, 55), (11, 37), (23, 32), (35, 32), (46, 39), (49, 55)], fill=P[4])
    d.ellipse((19, 10, 36, 34), fill=P[5])
    d.line((22, 22, 26, 22), fill=P[2]); d.line((30, 22, 34, 22), fill=P[2])
    d.line((27, 23, 26, 28, 29, 28), fill=P[4]); d.line((25, 31, 31, 31), fill=P[3])
    if kind == "venn":
        d.arc((16, 7, 39, 36), 183, 355, fill=P[4], width=3)
        d.line((21, 15, 31, 15), fill=P[6])
        d.polygon([(17, 35), (27, 40), (39, 35), (36, 55), (20, 55)], fill=P[8])
        d.ellipse((24, 43, 44, 60), fill=P[3])
        d.arc((26, 45, 42, 60), 185, 350, fill=P[5], width=2)
        d.line((15, 46, 28, 49), fill=P[5], width=3)
    else:
        d.polygon([(16, 19), (17, 9), (23, 5), (34, 7), (39, 14), (39, 20), (34, 14), (21, 14)], fill=P[6])
        d.line((18, 15, 37, 15), fill=P[3])
        d.line((15, 39, 41, 39), fill=P[6], width=2)
        d.line((16, 43, 17, 52), fill=P[3], width=2)
        d.line((39, 43, 38, 52), fill=P[3], width=2)
        d.line((25, 42, 25, 48), fill=P[5])
    return image


ASSETS = {("Rooms", "scoured_cloister"): cloister, ("Rooms", "lye_cistern"): cistern,
          ("Rooms", "reliquary_bastion"): bastion,
          **{("Portraits", name): (lambda name=name: portrait(name)) for name in ("venn", "silas")},
          **{("Monsters", name + "_idle"): (lambda name=name: idle(name))
             for name in ("commander_silas", "scoured_halberdier")}}

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    for (category, name), create in ASSETS.items():
        path = args.directory / category / (name + ".png")
        path.parent.mkdir(parents=True, exist_ok=True)
        create().save(path)
        print(path)
