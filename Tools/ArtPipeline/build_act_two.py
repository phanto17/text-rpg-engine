"""Act II rotunda painting; the floor-gate and sanctuary fire stay live."""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw
from build_pilots import P


def crossroads():
    image = Image.new("RGBA", (304, 140), P[0])
    d = ImageDraw.Draw(image)
    d.ellipse((4, -54, 300, 133), fill=P[1])
    for inset in (0, 11, 22):
        d.arc((12 + inset, -39 + inset, 292 - inset, 151 - inset), 180, 360, fill=P[3], width=3)
    # Two contrasting galleries frame the bronze floor casting.
    for left in (30, 205):
        d.rounded_rectangle((left - 7, 15, left + 62, 104), radius=28, fill=P[3])
        d.rounded_rectangle((left, 22, left + 55, 106), radius=25, fill=P[0])
        for x in (left - 8, left + 59):
            d.rectangle((x, 41, x + 5, 106), fill=P[3])
            d.line((x + 1, 43, x + 1, 103), fill=P[4])
    # Silver resonance wires toward the Archive, pale scouring on the Reliquary road.
    for i in range(6):
        x=38+i*7
        d.line((x, 25, x - 4, 80), fill=P[4])
        d.point((x - 3, 64 + i % 3 * 5), fill=P[15])
    d.polygon([(211, 75), (252, 75), (268, 108), (198, 108)], fill=P[3])
    for y in range(76, 106, 7):
        d.line((211 - (y - 76)//3, y, 252 + (y - 76)//3, y), fill=P[4])
    for x in range(213, 252, 8):
        d.line((x, 44, x, 69), fill=P[2])
    for x in (107, 185):
        d.rectangle((x, 28, x + 10, 102), fill=P[2])
        d.rectangle((x - 3, 24, x + 13, 29), fill=P[3])
        d.line((x + 2, 31, x + 2, 99), fill=P[4])
        for y in range(43, 99, 13): d.line((x + 3, y, x + 9, y), fill=P[1])
    d.polygon([(0, 103), (303, 103), (303, 139), (0, 139)], fill=P[2])
    for y in (108, 122, 138):
        d.line((0, y, 303, y), fill=P[3])
    for x in range(-40, 350, 37):
        d.line((152, 84, x, 139), fill=P[1])
    # Empty rim leaves the entire stateful gate to the renderer.
    d.ellipse((76, 73, 228, 119), fill=P[3])
    d.ellipse((80, 76, 224, 116), fill=P[13])
    d.ellipse((84, 78, 220, 114), fill=P[1])
    for x,y in ((83,96),(95,84),(119,78),(152,77),(185,78),(209,84),(221,96),(205,108),(152,114),(101,108)):
        d.line((x,y,x+2,y+1),fill=P[12])
    # A sheltered niche marks the sanctuary near the returning road.
    d.rectangle((265, 61, 294, 105),fill=P[1])
    d.arc((265, 48, 294, 80),180,360,fill=P[3],width=3)
    d.line((267, 62, 267, 103),fill=P[4])
    d.rectangle((270, 96, 290, 102),fill=P[8])
    d.line((271, 95, 289, 95),fill=P[5])
    for i in range(50):
        x,y=(i*43)%304,107+(i*17)%32
        if not 80<x<224: d.point((x,y),fill=P[4] if i%6==0 else P[3])
    return image


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    destination = args.directory / "Rooms/warren_crossroads.png"
    destination.parent.mkdir(parents=True, exist_ok=True)
    crossroads().save(destination)
    print(destination)
