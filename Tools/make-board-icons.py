# ไอคอนกระดานปลาใหญ่: ถ้วยรางวัล + เหรียญทอง/เงิน/ทองแดง (วาดเอง ไม่ต้องหาภาพ) → Assets/Game/Resources/UI/Icons
#   python tools/make-board-icons.py
import os
from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Assets', 'Game', 'Resources', 'UI', 'Icons')
S = 256  # วาดใหญ่แล้วย่อ ขอบเนียน

def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3]) + (255,)

def radial(size, inner, outer):
    im = Image.new('RGBA', (size, size)); px = im.load(); r = size / 2
    for y in range(size):
        for x in range(size):
            d = (((x - r * 0.8) ** 2 + (y - r * 0.75) ** 2) ** 0.5) / (r * 1.15)
            d = min(1, d); px[x, y] = tuple(int(inner[i] * (1 - d) + outer[i] * d) for i in range(3)) + (255,)
    return im

def medal(name, base, num):
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    # ริบบิ้นสองสี
    d.polygon([(70, 0), (118, 0), (138, 110), (96, 110)], fill=(178, 40, 40, 255))
    d.polygon([(138, 0), (186, 0), (160, 110), (118, 110)], fill=(40, 70, 150, 255))
    # ตัวเหรียญ: ไล่สี + ขอบนูน
    disc = radial(170, shade(base, 1.35), shade(base, 0.7))
    mask = Image.new('L', (170, 170), 0); ImageDraw.Draw(mask).ellipse((0, 0, 169, 169), fill=255)
    shadow = Image.new('RGBA', (S, S), (0, 0, 0, 0)); ImageDraw.Draw(shadow).ellipse((47, 85, 217, 255), fill=(0, 0, 0, 110))
    im = Image.alpha_composite(im, shadow.filter(ImageFilter.GaussianBlur(6)))
    im.paste(disc, (43, 78), mask); d = ImageDraw.Draw(im)
    d.ellipse((43, 78, 212, 247), outline=shade(base, 0.55), width=8)
    d.ellipse((62, 97, 193, 228), outline=shade(base, 1.25), width=4)
    # เลขอันดับ (วาดเป็นเส้นหนา ไม่พึ่งฟอนต์)
    cx, cy = 128, 163; w = 13; col = shade(base, 0.45)
    if num == 1:
        d.line([(cx, cy - 42), (cx, cy + 42)], fill=col, width=w); d.line([(cx - 16, cy - 28), (cx, cy - 42)], fill=col, width=w)
        d.line([(cx - 20, cy + 42), (cx + 20, cy + 42)], fill=col, width=w)
    elif num == 2:
        d.arc((cx - 28, cy - 44, cx + 28, cy + 4), 200, 20, fill=col, width=w)
        d.line([(cx + 25, cy - 14), (cx - 26, cy + 40)], fill=col, width=w); d.line([(cx - 28, cy + 42), (cx + 30, cy + 42)], fill=col, width=w)
    else:
        d.arc((cx - 26, cy - 46, cx + 26, cy - 2), 200, 90, fill=col, width=w)
        d.arc((cx - 28, cy - 4, cx + 28, cy + 46), 270, 160, fill=col, width=w)
    im.resize((128, 128), Image.LANCZOS).save(os.path.join(OUT, name + '.png'))

def trophy():
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    gold, dark = (240, 190, 70, 255), (150, 100, 25, 255)
    for side in (-1, 1):   # หูถ้วย
        x0 = 128 + side * 92
        d.arc((x0 - 34, 46, x0 + 34, 130), 90 if side > 0 else -90, 270 if side > 0 else 90, fill=dark, width=14)
    cup = radial(170, (255, 226, 130), (190, 130, 30))
    mask = Image.new('L', (170, 170), 0); ImageDraw.Draw(mask).pieslice((0, -150, 169, 160), 0, 180, fill=255)
    im.paste(cup, (43, 30), mask); d = ImageDraw.Draw(im)
    d.line([(43, 32), (212, 32)], fill=dark, width=10)
    d.rectangle((116, 182, 140, 210), fill=gold); d.rectangle((80, 208, 176, 230), fill=dark); d.rectangle((92, 200, 164, 214), fill=gold)
    d.polygon([(128, 70), (138, 98), (166, 98), (143, 114), (152, 142), (128, 125), (104, 142), (113, 114), (90, 98), (118, 98)], fill=(255, 245, 200, 255))   # ดาวกลางถ้วย
    im.resize((128, 128), Image.LANCZOS).save(os.path.join(OUT, 'trophy.png'))

os.makedirs(OUT, exist_ok=True)
medal('medal_1', (232, 182, 60), 1)
medal('medal_2', (196, 200, 210), 2)
medal('medal_3', (196, 120, 70), 3)
trophy()
print('ok', OUT)
