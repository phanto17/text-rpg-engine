"""Archive paintings, portraits and idle sheets in the reviewed source palette."""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw
from build_pilots import P


def floor(d):
    d.rectangle((0, 105, 303, 139), fill=P[2])
    for y in (111, 122, 136): d.line((0, y, 303, y), fill=P[3])
    for x in range(-30, 340, 32): d.line((152, 91, x, 139), fill=P[1])


def stacks():
    image=Image.new("RGBA",(304,140),P[0]); d=ImageDraw.Draw(image)
    d.polygon([(91,0),(213,0),(241,110),(63,110)],fill=P[1])
    floor(d)
    for left,width,top in ((3,65,4),(236,65,4),(75,40,29),(189,40,29)):
        d.rectangle((left,top,left+width,107),fill=P[2])
        for y in range(top+8,107,17):
            d.rectangle((left+2,y,left+width-2,y+2),fill=P[4])
            for i,x in enumerate(range(left+4,left+width-4,7)):
                d.rectangle((x,y-11,x+4,y-1),fill=P[8] if i%3 else P[3])
                d.line((x+1,y-9,x+1,y-3),fill=P[4] if i%3 else P[12])
                if i%4==0: d.point((x+2,y-7),fill=P[15])
        d.line((left,top,left,107),fill=P[3],width=2)
    d.rectangle((126,85,178,107),fill=P[8])
    d.polygon([(122,82),(176,82),(184,87),(126,89)],fill=P[4])
    d.rectangle((137,79,160,83),fill=P[6])
    d.line((149,79,149,83),fill=P[3])
    for x in (128,167):
        d.rectangle((x,75,x+5,81),fill=P[12])
        d.line((x+1,75,x+1,80),fill=P[5])
    for i in range(12):
        x=85+i*12
        d.line((x,0,x+(-5 if i%2 else 5),57+i%3*9),fill=P[3])
    return image


def foundry():
    image=Image.new("RGBA",(304,140),P[0]); d=ImageDraw.Draw(image)
    for left in (5,245):
        d.rectangle((left,7,left+54,106),fill=P[2])
        for y in range(13,105,12): d.line((left+2,y,left+52,y),fill=P[3])
        d.arc((left-5,-20,left+65,100),180,360,fill=P[4],width=3)
    floor(d)
    for x in (89,208):
        d.rectangle((x,17,x+7,109),fill=P[8])
        d.line((x+1,18,x+1,105),fill=P[12])
    d.rectangle((86,14,218,20),fill=P[4])
    d.line((88,14,216,14),fill=P[5])
    for x in range(99,206,12):
        d.ellipse((x,74,x+8,86),fill=P[9])
        d.line((x+3,76,x+3,83),fill=P[12])
    d.rectangle((93,88,212,93),fill=P[3])
    d.line((94,88,210,88),fill=P[5])
    for x in (48,255):
        d.rectangle((x-21,94,x+21,106),fill=P[9])
        d.ellipse((x-24,87,x+24,100),fill=P[12])
        d.ellipse((x-20,88,x+20,96),fill=P[13])
        d.line((x-15,90,x+11,90),fill=P[14])
        for y in (99,103): d.line((x-19,y,x+19,y),fill=P[8])
    d.rectangle((0,125,303,139),fill=P[1])
    for x in range(3,304,21): d.line((x,126,x-7,139),fill=P[3])
    return image


def sanctum():
    image=Image.new("RGBA",(304,140),P[0]); d=ImageDraw.Draw(image)
    d.ellipse((15,-31,289,122),fill=P[1])
    for inset in (0,15,30):
        d.arc((18+inset,-23+inset,286-inset,149-inset),180,360,fill=P[3],width=2)
    for x in (23,65,232,274):
        d.rectangle((x-5,40,x+5,110),fill=P[2])
        d.line((x-3,41,x-3,107),fill=P[4])
        d.rectangle((x-9,37,x+9,42),fill=P[3])
    floor(d)
    for radius,y,color in ((84,110,3),(70,104,4),(58,99,3)):
        d.ellipse((152-radius,y-8,152+radius,y+9),fill=P[color])
        d.arc((152-radius,y-8,152+radius,y+9),0,180,fill=P[5])
    # The central cradle, glass and Lira are drawn from current world state.
    for i in range(13):
        x=35+i*19
        d.line((x,0,152+(i-6)*4,57),fill=P[3] if i%2 else P[4])
    for i in range(45):
        x,y=75+(i*31)%155,112+(i*17)%26
        d.point((x,y),fill=P[4])
    return image


def figure(kind):
    image=Image.new("RGBA",(48,48)); d=ImageDraw.Draw(image)
    if kind=="prelate_vaelen":
        d.polygon([(17,17),(29,17),(32,34),(39,45),(9,45),(15,33)],fill=P[3])
        d.polygon([(15,18),(24,25),(32,18),(36,33),(25,29),(12,34)],fill=P[15])
        d.line((16,20,24,27,32,21),fill=P[5])
        d.ellipse((19,6,29,19),fill=P[5])
        d.polygon([(17,10),(19,4),(23,1),(27,4),(31,10),(29,11),(23,6),(19,11)],fill=P[12])
        d.line((21,13,23,13),fill=P[1]); d.line((26,13,28,13),fill=P[1])
        d.line((16,25,8,30,13,35),fill=P[5],width=2)
        d.line((31,25,38,30,34,36),fill=P[5],width=2)
        d.ellipse((35,32,41,38),outline=P[12]); d.line((38,38,38,43),fill=P[12])
        for x in (16,22,28,33): d.line((x,33,x-2,43),fill=P[4])
    else:
        d.polygon([(17,18),(29,17),(33,29),(28,43),(12,43),(14,29)],fill=P[4])
        d.ellipse((17,5,29,18),fill=P[5])
        d.line((19,11,22,11),fill=P[0]); d.line((25,11,27,11),fill=P[0])
        d.rectangle((21,15,26,18),fill=P[0])
        d.line((15,23,7,29),fill=P[5],width=2)
        d.line((30,23,38,28),fill=P[5],width=2)
        d.rectangle((20,28,28,37),fill=P[12])
        d.line((22,29,22,36),fill=P[8])
        for x,y in ((23,18),(8,29),(38,28)):
            d.line((x,y,24,30),fill=P[5])
        d.line((17,39,14,45),fill=P[3],width=3)
        d.line((28,39,32,45),fill=P[3],width=3)
    return image


def idle(kind):
    sheet=Image.new("RGBA",(192,48)); base=figure(kind)
    for i,shift in enumerate((0,0,1,0)):
        sheet.alpha_composite(base,(i*48+shift,0))
    return sheet


def portrait(kind):
    image=Image.new("RGBA",(56,56)); d=ImageDraw.Draw(image)
    d.polygon([(8,55),(12,37),(23,31),(35,31),(45,39),(49,55)],fill=P[3])
    d.ellipse((19,10,36,34),fill=P[5])
    d.line((26,25,25,28,29,28),fill=P[4])
    d.line((25,31,30,31),fill=P[3])
    if kind=="caelan":
        d.arc((17,5,38,35),185,355,fill=P[4],width=4)
        d.line((20,22,25,22),fill=P[3]); d.line((30,22,35,22),fill=P[3])
        d.polygon([(17,35),(27,40),(39,35),(37,55),(19,55)],fill=P[8])
        for x,y in ((22,43),(30,48),(35,39)): d.rectangle((x,y,x+2,y+2),fill=P[6])
        d.rectangle((22,46,35,53),fill=P[12])
        for x in (24,29,34): d.line((x,47,x,52),fill=P[9])
        d.line((15,45,24,48),fill=P[5],width=2)
    else:
        d.polygon([(16,16),(17,9),(22,7),(28,2),(34,8),(38,11),(39,19),(32,14),(28,9),(23,13)],fill=P[12])
        d.line((22,22,26,23),fill=P[3]); d.line((30,23,34,22),fill=P[3])
        d.polygon([(13,35),(27,43),(41,35),(47,50),(28,47),(10,52)],fill=P[15])
        d.line((15,37,27,45,40,38),fill=P[5])
        d.rectangle((21,48,25,54),fill=P[6]); d.rectangle((32,47,36,53),fill=P[6])
        d.ellipse((26,45,32,51),outline=P[12]); d.line((29,51,29,55),fill=P[12])
    return image


ASSETS={("Rooms","whispering_stacks"):stacks,("Rooms","tallow_foundry"):foundry,("Rooms","vow_sanctum"):sanctum,
        **{("Portraits",name):(lambda name=name:portrait(name)) for name in ("caelan","vaelen")},
        **{("Monsters",name+"_idle"):(lambda name=name:idle(name)) for name in ("prelate_vaelen","tethered_chorister")}}

if __name__=="__main__":
    parser=argparse.ArgumentParser(); parser.add_argument("directory",type=Path); args=parser.parse_args()
    for (category,name),create in ASSETS.items():
        path=args.directory/category/(name+".png"); path.parent.mkdir(parents=True,exist_ok=True)
        create().save(path); print(path)
