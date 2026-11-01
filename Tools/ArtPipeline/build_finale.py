"""Final room family; bell, bier, brazier and ending effects remain live."""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw
from build_pilots import P


def nave():
    image = Image.new("RGBA", (304, 140), P[0])
    d = ImageDraw.Draw(image)
    d.ellipse((11, -60, 293, 127), fill=P[1])
    for left in (-15, 43, 216, 275):
        d.arc((left, -27, left+48, 134), 180, 360, fill=P[3], width=4)
        d.rectangle((left+3, 38, left+8, 110), fill=P[2])
        d.line((left+4, 40, left+4, 107), fill=P[4])
    for left in (97, 199):
        d.rectangle((left, 2, left+8, 109), fill=P[2])
        d.line((left+2, 4, left+2, 106), fill=P[4])
    d.arc((90, -28, 214, 152), 180, 360, fill=P[3], width=5)
    d.rectangle((0, 107, 303, 139), fill=P[2])
    for y in (113, 125, 138): d.line((0, y, 303, y), fill=P[3])
    for x in range(-30, 350, 37): d.line((152, 99, x, 139), fill=P[1])
    d.polygon([(0, 115), (36, 102), (64, 111), (93, 104), (91, 124), (0, 136)], fill=P[4])
    d.polygon([(211, 111), (247, 105), (270, 113), (303, 100), (303, 135), (216, 124)], fill=P[4])
    for i in range(60):
        x, y = i*41%304, 108+i*17%30
        if x<92 or x>215: d.point((x,y),fill=P[5] if i%4==0 else P[3])
    d.rectangle((116, 106, 188, 110), fill=P[3])
    d.line((116,106,188,106),fill=P[5])
    return image


def cradle():
    image = Image.new("RGBA", (304, 140), P[0])
    d = ImageDraw.Draw(image)
    d.ellipse((3, -37, 301, 124), fill=P[1])
    for i in range(9):
        x=8+i*36
        d.line((x,0,152+(x-152)//2,104),fill=P[2],width=5)
        d.line((x+2,0,154+(x-152)//2,101),fill=P[3])
    for x in (17,86,213,282):
        d.rectangle((x-4,37,x+4,111),fill=P[2])
        d.line((x-2,39,x-2,108),fill=P[4])
    d.rectangle((0,108,303,139),fill=P[2])
    for y in (114,126,138): d.line((0,y,303,y),fill=P[3])
    for x in range(-30,340,36): d.line((152,97,x,139),fill=P[1])
    # The octagonal dais reserves its center for the live Great Brazier.
    d.polygon([(102,99),(119,90),(185,90),(202,99),(202,112),(184,121),(119,121),(102,112)],fill=P[3])
    d.line((103,111,119,119,184,119,201,111),fill=P[4])
    d.polygon([(110,99),(123,94),(181,94),(194,100),(193,108),(179,114),(124,114),(111,108)],fill=P[2])
    # Alabaster platform only: amber and occupant change with the ending.
    d.polygon([(10,90),(70,85),(84,93),(77,101),(15,105),(7,99)],fill=P[3])
    d.polygon([(12,87),(70,82),(83,88),(77,95),(17,99),(9,94)],fill=P[5])
    d.line((16,98,77,94),fill=P[6])
    for i in range(60):
        x,y=i*47%304,111+i*19%29
        if not 100<x<204: d.point((x,y),fill=P[4] if i%7==0 else P[3])
    return image


def figure(kind):
    image=Image.new("RGBA",(48,48)); d=ImageDraw.Draw(image)
    architect=kind=="first_architect"
    d.polygon([(15,17),(29,16),(34,29),(29,37),(13,37),(11,27)],fill=P[13] if architect else P[9])
    d.ellipse((18,4,29,17),fill=P[4] if architect else P[5])
    d.line((20,11,23,11),fill=P[0]); d.line((26,11,28,11),fill=P[0])
    d.line((16,36,14,44,10,45),fill=P[4],width=3)
    d.line((28,36,30,44,34,45),fill=P[4],width=3)
    d.line((14,21,7,29),fill=P[5],width=3)
    d.line((30,21,38,28),fill=P[5],width=3)
    if architect:
        d.arc((16,2,31,17),180,355,fill=P[12],width=2)
        d.ellipse((15,17,32,33),outline=P[12],width=2)
        d.ellipse((19,19,28,32),outline=P[5])
        d.line((14,24,33,24),fill=P[12])
        d.rectangle((9,18,14,25),fill=P[6])
        d.line((36,10,39,40),fill=P[8],width=2)
        d.polygon([(34,38),(40,36),(45,43),(39,46),(33,43)],fill=P[12])
        d.line((36,40,42,43),fill=P[5])
    else:
        d.line((15,20,29,32),fill=P[12],width=2)
        for x,y in ((17,7),(28,15),(14,30),(26,35)):
            d.polygon([(x-1,y+3),(x,y-2),(x+3,y+3)],fill=P[6])
        d.line((38,27,40,37),fill=P[5])
        d.polygon([(40,35),(45,41),(41,46),(35,41)],fill=P[12])
        d.line((37,41,43,41),fill=P[8])
    return image


def idle(kind):
    sheet=Image.new("RGBA",(192,48)); base=figure(kind)
    for i,shift in enumerate((0,0,1,0)):
        sheet.alpha_composite(base,(i*48+shift,0))
    return sheet


def architect_portrait():
    image=Image.new("RGBA",(56,56)); d=ImageDraw.Draw(image)
    d.polygon([(7,55),(11,38),(22,31),(36,31),(46,40),(49,55)],fill=P[13])
    d.ellipse((19,9,37,34),fill=P[4])
    d.arc((16,5,39,35),185,355,fill=P[5],width=3)
    d.line((21,23,26,24),fill=P[1]); d.line((31,24,35,23),fill=P[1])
    d.line((28,24,27,28,31,28),fill=P[3])
    d.line((25,31,32,31),fill=P[2])
    for x,y in ((19,18),(35,29),(17,43)):
        d.polygon([(x-1,y+3),(x,y-2),(x+3,y+3)],fill=P[6])
    d.ellipse((16,38,41,60),outline=P[12],width=2)
    d.line((19,46,39,46),fill=P[12])
    d.line((29,39,29,55),fill=P[5])
    d.rectangle((11,38,17,45),fill=P[6])
    return image


ASSETS={("Rooms","bell_nave"):nave,("Rooms","cradle_of_ash"):cradle,
        ("Portraits","architect_post"):architect_portrait,
        **{("Monsters",name+"_idle"):(lambda name=name:idle(name))
           for name in ("first_architect","calcified_warden")}}

if __name__=="__main__":
    parser=argparse.ArgumentParser(); parser.add_argument("directory",type=Path); args=parser.parse_args()
    for (category,name),create in ASSETS.items():
        path=args.directory/category/(name+".png"); path.parent.mkdir(parents=True,exist_ok=True)
        create().save(path); print(path)
