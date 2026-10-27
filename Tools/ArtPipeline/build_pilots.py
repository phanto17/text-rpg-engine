"""Reproduce the original pilot artwork. Python 3.11+ and requirements.txt required.

Writes only to the nominated staging directory; editable PXO masters are never overwritten.
"""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw

COLORS = ["0B0D12", "151923", "252B38", "3C4554", "697584", "AAB8C5",
          "E5DDC8", "FFF1C9", "2B211F", "63372E", "A84B45", "DB8552",
          "D7B46A", "597465", "7FA89B", "A29BC4"]
P = [tuple(bytes.fromhex(color)) + (255,) for color in COLORS]


def room():
    im = Image.new("RGBA", (304, 140), P[1]); d = ImageDraw.Draw(im)
    d.polygon([(0, 65), (37, 42), (84, 57), (119, 32), (175, 54), (227, 35), (303, 67), (303, 140), (0, 140)], fill=P[2])
    d.polygon([(0, 85), (60, 63), (97, 81), (145, 66), (214, 76), (264, 61), (303, 80), (303, 140), (0, 140)], fill=P[0])
    d.rectangle((0, 105, 303, 139), fill=P[8])
    d.polygon([(140, 98), (164, 98), (235, 139), (69, 139)], fill=P[2])
    for y in range(108, 140, 7):
        d.line((70, y, 234, y), fill=P[3])
    # Squared stone gateway, iron bars and quiet moon-lit edges.
    for x in (99, 191):
        d.rectangle((x, 38, x + 13, 108), fill=P[3])
        d.line((x, 38, x, 108), fill=P[4])
        for y in range(42, 108, 9): d.line((x, y, x + 13, y), fill=P[1])
        d.rectangle((x - 3, 33, x + 16, 39), fill=P[4])
        d.rectangle((x - 4, 107, x + 17, 112), fill=P[3])
    d.rectangle((97, 27, 207, 34), fill=P[3]); d.line((97, 27, 207, 27), fill=P[5])
    d.polygon([(112, 41), (131, 28), (173, 28), (190, 41), (190, 105), (112, 105)], fill=P[0])
    for x in range(118, 190, 9):
        d.line((x, 43, x, 104), fill=P[3]); d.polygon([(x-2, 43), (x, 37), (x+2, 43)], fill=P[4])
    d.line((115, 66, 188, 66), fill=P[3]); d.line((115, 91, 188, 91), fill=P[3])
    d.rectangle((149, 63, 155, 73), fill=P[9]); d.point((153, 65), fill=P[12])
    # Weathered faceless sentinels, grave markers and reeds.
    for x in (70, 233):
        d.rectangle((x-7, 103, x+7, 108), fill=P[3])
        d.polygon([(x-4, 65), (x+4, 65), (x+7, 99), (x-7, 99)], fill=P[2])
        d.rectangle((x-3, 57, x+3, 65), fill=P[3]); d.line((x-2, 58, x-2, 64), fill=P[4])
        d.line((x-4, 72, x-4, 95), fill=P[3])
    for x, y in [(19,103),(40,93),(261,101),(285,113),(27,123),(246,123)]:
        d.rectangle((x, y-9, x+5, y), fill=P[2]); d.line((x,y-9,x+5,y-9), fill=P[3])
    for x in range(4, 300, 13):
        if 90 < x < 214: continue
        y = 119 + (x * 7 % 17)
        d.line((x,y,x-2,y-5),fill=P[13]); d.line((x+2,y,x+3,y-3),fill=P[3])
    for x,y in [(124,118),(183,130),(138,126),(172,110)]: d.line((x,y,x+8,y),fill=P[4])
    return im


def knight():
    im=Image.new("RGBA",(48,48)); d=ImageDraw.Draw(im)
    d.polygon([(17,12),(31,12),(37,37),(30,42),(13,41),(11,30)],fill=P[8])
    d.polygon([(13,16),(18,13),(30,13),(35,19),(31,27),(16,26)],fill=P[3])
    d.line((17,15,29,15),fill=P[5]); d.line((23,17,23,25),fill=P[4])
    d.rectangle((18,5,28,13),fill=P[3]); d.rectangle((20,3,26,5),fill=P[4])
    d.line((18,6,28,6),fill=P[5]); d.rectangle((19,9,28,10),fill=P[0])
    d.point((21,9),fill=P[10]); d.point((26,9),fill=P[10])
    d.line((24,5,24,13),fill=P[4]); d.rectangle((18,26,29,29),fill=P[9])
    d.rectangle((22,26,25,28),fill=P[12])
    for x in (17,26):
        d.polygon([(x,30),(x+5,30),(x+4,40),(x+6,43),(x-1,43)],fill=P[3])
        d.line((x,32,x,39),fill=P[4])
    d.polygon([(9,19),(16,18),(18,32),(12,37),(7,30)],fill=P[2])
    d.line((9,20,8,29,12,34,16,30),fill=P[4]); d.line((12,22,12,30),fill=P[9])
    d.rectangle((32,20,35,28),fill=P[4]); d.line((37,18,37,40),fill=P[5])
    d.line((38,20,38,38),fill=P[9]); d.line((34,29,40,29),fill=P[12])
    return im


def portrait():
    im=Image.new("RGBA",(56,56)); d=ImageDraw.Draw(im)
    d.polygon([(10,55),(14,35),(19,28),(17,16),(23,6),(34,6),(40,17),(37,30),(44,38),(48,55)],fill=P[1])
    d.polygon([(18,30),(18,17),(24,8),(33,8),(38,18),(36,32),(31,36),(24,35)],fill=P[3])
    d.polygon([(22,17),(26,12),(32,13),(35,19),(33,29),(29,32),(24,28)],fill=P[4])
    d.polygon([(24,17),(28,14),(32,16),(33,24),(29,29),(25,26)],fill=P[6])
    d.line((24,20,27,20),fill=P[2]); d.line((30,20,32,20),fill=P[2])
    d.line((28,21,27,24),fill=P[4]); d.point((29,26),fill=P[9])
    d.polygon([(16,37),(23,32),(28,39),(34,32),(41,40),(44,55),(12,55)],fill=P[2])
    d.line((23,33,27,42,23,54),fill=P[4]); d.line((34,33,30,43,33,54),fill=P[3])
    d.line((18,39,17,51),fill=P[3])
    # Lantern held close: isolated warm pixels against cold cloth.
    d.rectangle((34,39,43,51),fill=P[9]); d.rectangle((36,41,41,49),fill=P[12])
    d.rectangle((38,43,40,48),fill=P[7]); d.line((35,39,35,36,42,36,42,39),fill=P[4])
    d.line((34,40,43,40),fill=P[4]); d.line((34,51,43,51),fill=P[3])
    d.rectangle((31,43,34,46),fill=P[6])
    return im


if __name__ == "__main__":
    parser=argparse.ArgumentParser(); parser.add_argument("output",type=Path); args=parser.parse_args()
    for category,name,art in [("Rooms","lichgate",room()),("Monsters","gravebound_knight",knight()),("Portraits","wren",portrait())]:
        path=args.output/category/(name+".png"); path.parent.mkdir(parents=True,exist_ok=True)
        art.save(path)
        print(path)
