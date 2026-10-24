# วาดรูปสินค้าร้านลุงส้ม (ภาพสี 256px พื้นโปร่ง) — python tools/make-shop-art.py
import math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = 'Assets/Game/Resources/UI/Shop'
import os; os.makedirs(OUT, exist_ok=True)
S = 512

def canvas(): im = Image.new('RGBA', (S, S), (0, 0, 0, 0)); return im, ImageDraw.Draw(im)
def save(im, name):
    a = np.asarray(im.split()[3], np.float32) / 255
    from scipy.ndimage import gaussian_filter
    sh = gaussian_filter(np.roll(np.roll(a, 10, 0), 6, 1), 8) * 0.5
    base = Image.fromarray(np.dstack([np.zeros_like(a), np.zeros_like(a), np.zeros_like(a), sh * 255]).astype(np.uint8), 'RGBA')
    base.alpha_composite(im); base.resize((256, 256), Image.LANCZOS).save(f'{OUT}/{name}.png')

def shade(d, box, c, hi=(255, 255, 255, 70)):
    d.ellipse(box, fill=c); x0, y0, x1, y1 = box; d.ellipse([x0 + (x1 - x0) * 0.2, y0 + (y1 - y0) * 0.12, x0 + (x1 - x0) * 0.5, y0 + (y1 - y0) * 0.4], fill=hi)

# ---------- คันเบ็ด (3 ขั้น: ไฟเบอร์เหลือง · คาร์บอนดำ · ไม้ช่างฝีมือทองเหลือง) ----------
def rod(name, shaft, grip, guides, deco=None):
    im, d = canvas()
    d.line([(70, 450), (450, 60)], fill=(30, 25, 20, 255), width=20); d.line([(70, 450), (450, 60)], fill=shaft, width=13)
    d.line([(70, 450), (190, 328)], fill=grip, width=30)
    for k in range(4): t = 0.42 + k * 0.14; x, y = 70 + 380 * t, 450 - 390 * t; d.ellipse([x - 10, y - 10, x + 10, y + 10], outline=guides, width=5)
    d.line([(450, 60), (470, 200)], fill=(240, 240, 230, 200), width=2)
    if deco: deco(d)
    save(im, name)
rod('rod1', (230, 190, 70, 255), (60, 60, 60, 255), (200, 200, 210, 255))
rod('rod2', (40, 44, 50, 255), (150, 40, 30, 255), (220, 220, 230, 255), lambda d: d.line([(200, 318), (440, 70)], fill=(120, 130, 140, 255), width=3))
rod('rod3', (150, 95, 50, 255), (90, 55, 30, 255), (220, 180, 90, 255), lambda d: [d.line([(130 + k * 8, 390 - k * 8), (150 + k * 8, 410 - k * 8)], fill=(210, 170, 80, 255), width=5) for k in range(6)])

# ---------- รอก (สปิน · เบท · ทองเหลืองโบราณ) ----------
def reel(name, body, spool, handle):
    im, d = canvas()
    d.rounded_rectangle([200, 120, 312, 170], 20, fill=(70, 70, 75, 255))              # ขาจับคัน
    d.rectangle([240, 160, 272, 230], fill=(60, 60, 64, 255))
    shade(d, [140, 210, 372, 440], body)
    shade(d, [195, 265, 317, 385], spool, (255, 255, 255, 90))
    for k in range(6): d.arc([205 + k * 3, 275 + k * 3, 307 - k * 3, 375 - k * 3], 0, 360, fill=(240, 240, 230, 150), width=2)
    d.line([(372, 325), (440, 280)], fill=(50, 50, 55, 255), width=14); shade(d, [425, 255, 470, 300], handle)
    save(im, name)
reel('reel1', (60, 110, 170, 255), (200, 200, 200, 255), (230, 230, 230, 255))
reel('reel2', (150, 40, 35, 255), (60, 60, 60, 255), (240, 200, 60, 255))
reel('reel3', (190, 140, 60, 255), (150, 100, 40, 255), (230, 200, 120, 255))

# ---------- เหยื่อ ----------
im, d = canvas()   # กุ้งเป็น 3 ตัวในถุงน้ำ
d.rounded_rectangle([110, 120, 400, 430], 60, fill=(170, 210, 230, 110), outline=(220, 240, 250, 200), width=6)
d.polygon([(200, 120), (310, 120), (290, 80), (220, 80)], fill=(200, 60, 50, 255))
for k, (cx, cy) in enumerate([(200, 260), (300, 320), (230, 370)]):
    for i in range(9):
        a = i / 8 * math.pi * 0.9; x = cx + math.cos(a) * 45 - 20; y = cy - math.sin(a) * 30
        r = 16 - i * 1.3; d.ellipse([x - r, y - r, x + r, y + r], fill=(235, 150, 120, 255))
    d.line([(cx - 60, cy - 10), (cx - 120, cy - 50)], fill=(230, 140, 110, 255), width=3)
save(im, 'shrimp')
im, d = canvas()   # รำข้าวปั้นในกระด้ง
d.ellipse([70, 250, 440, 430], fill=(150, 110, 60, 255)); d.ellipse([90, 262, 420, 412], fill=(196, 160, 100, 255))
for i in range(40): x, y = random.uniform(110, 400), random.uniform(275, 400); d.line([(x, y), (x + 20, y + 6)], fill=(150, 110, 60, 160), width=3)
for (cx, cy) in [(190, 300), (270, 290), (330, 330), (230, 350), (300, 375)]: shade(d, [cx - 38, cy - 38, cx + 38, cy + 38], (214, 190, 140, 255))
save(im, 'bran')
im, d = canvas()   # กบน้อย
shade(d, [140, 200, 380, 400], (110, 160, 60, 255))
for sx in (-1, 1):
    d.ellipse([256 + sx * 70 - 35, 170, 256 + sx * 70 + 35, 240], fill=(110, 160, 60, 255)); d.ellipse([256 + sx * 70 - 18, 188, 256 + sx * 70 + 18, 224], fill=(250, 240, 200, 255)); d.ellipse([256 + sx * 70 - 8, 198, 256 + sx * 70 + 8, 214], fill=(20, 20, 20, 255))
    d.polygon([(256 + sx * 110, 360), (256 + sx * 190, 420), (256 + sx * 130, 430)], fill=(90, 140, 50, 255))
d.arc([200, 280, 312, 340], 20, 160, fill=(40, 60, 20, 255), width=6)
save(im, 'frog')

# ---------- กันผี ----------
im, d = canvas()   # ตะเกียงรั้วน้ำมัน มีแสง
glow = Image.new('RGBA', (S, S), (0, 0, 0, 0)); gd = ImageDraw.Draw(glow); gd.ellipse([120, 140, 392, 420], fill=(255, 180, 80, 120)); im.alpha_composite(glow.filter(ImageFilter.GaussianBlur(40)))
d.arc([196, 60, 316, 170], 180, 360, fill=(60, 60, 60, 255), width=10)
d.rectangle([180, 140, 332, 170], fill=(120, 30, 25, 255)); d.rounded_rectangle([196, 170, 316, 360], 40, fill=(255, 230, 160, 200), outline=(80, 80, 80, 255), width=8)
d.ellipse([236, 240, 276, 300], fill=(255, 140, 40, 255)); d.ellipse([246, 255, 266, 290], fill=(255, 240, 180, 255))
d.rectangle([170, 360, 342, 400], fill=(120, 30, 25, 255))
save(im, 'lamp')
im, d = canvas()   # ไฟฉายถ่านก้อนใหญ่ + ลำแสง
beam = Image.new('RGBA', (S, S), (0, 0, 0, 0)); bd = ImageDraw.Draw(beam); bd.polygon([(300, 210), (500, 110), (500, 330), (300, 260)], fill=(255, 250, 200, 110)); im.alpha_composite(beam.filter(ImageFilter.GaussianBlur(10)))
d.rounded_rectangle([90, 210, 300, 270], 20, fill=(40, 90, 60, 255)); d.rectangle([280, 195, 330, 285], fill=(170, 170, 175, 255)); d.ellipse([318, 195, 342, 285], fill=(255, 250, 210, 255))
d.rectangle([150, 222, 190, 258], fill=(220, 180, 40, 255))
save(im, 'torch')
im, d = canvas()   # สายสิญจน์ขดเป็นม้วน + พันข้อมือ
for k in range(9): d.ellipse([150 + k * 4, 170 + k * 4, 362 - k * 4, 382 - k * 4], outline=(245, 242, 230, 255), width=7)
d.line([(330, 330), (420, 440)], fill=(245, 242, 230, 255), width=7); d.line([(340, 320), (440, 420)], fill=(230, 226, 210, 255), width=5)
d.ellipse([230, 250, 282, 302], fill=(200, 160, 60, 255))
save(im, 'amulet')
im, d = canvas()   # ถังสังฆทานเหลือง + ธูป
for k in range(3): d.line([(220 + k * 30, 70), (225 + k * 30, 170)], fill=(170, 60, 40, 255), width=6); d.ellipse([214 + k * 30, 60, 228 + k * 30, 74], fill=(255, 100, 40, 255))
d.polygon([(150, 200), (362, 200), (340, 430), (172, 430)], fill=(240, 180, 40, 255)); d.rectangle([150, 190, 362, 215], fill=(220, 150, 30, 255))
d.ellipse([220, 270, 292, 342], outline=(180, 40, 30, 255), width=8)
save(im, 'merit')

# ---------- ของในบ้าน ----------
def boxitem(name, draw):
    im, d = canvas(); draw(d); save(im, name)
boxitem('net', lambda d: [d.polygon([(110, 150), (402, 150), (430, 420), (82, 420)], fill=(235, 235, 245, 170), outline=(200, 200, 215, 255)),
                          *[d.line([(110 + k * 29, 150), (82 + k * 35, 420)], fill=(200, 200, 220, 160), width=2) for k in range(11)],
                          *[d.line([(100, 170 + k * 25), (412, 170 + k * 25)], fill=(200, 200, 220, 160), width=2) for k in range(11)]])
boxitem('fan', lambda d: [d.ellipse([140, 70, 372, 302], outline=(70, 110, 150, 255), width=10),
                          *[d.pieslice([170, 100, 342, 272], a, a + 50, fill=(90, 150, 200, 230)) for a in (0, 120, 240)],
                          d.ellipse([236, 166, 276, 206], fill=(60, 90, 120, 255)), d.rectangle([244, 300, 268, 420], fill=(90, 100, 110, 255)), d.ellipse([170, 405, 342, 450], fill=(70, 80, 90, 255))])
boxitem('cupboard', lambda d: [d.rectangle([140, 80, 372, 440], fill=(120, 80, 45, 255)), d.rectangle([160, 100, 352, 300], fill=(180, 190, 180, 200)),
                               *[d.line([(160, 100 + k * 12), (352, 100 + k * 12)], fill=(150, 160, 150, 160), width=1) for k in range(17)],
                               d.line([(256, 100), (256, 300)], fill=(120, 80, 45, 255), width=6), d.rectangle([160, 320, 352, 420], fill=(140, 95, 55, 255))])
boxitem('radio', lambda d: [d.rounded_rectangle([90, 170, 422, 400], 30, fill=(150, 60, 40, 255)), d.rounded_rectangle([115, 195, 290, 375], 16, fill=(220, 200, 160, 255)),
                            *[d.line([(125, 210 + k * 16), (280, 210 + k * 16)], fill=(150, 120, 80, 255), width=4) for k in range(10)],
                            d.ellipse([315, 205, 395, 285], fill=(230, 220, 200, 255)), d.ellipse([330, 300, 380, 350], fill=(60, 50, 40, 255)), d.line([(300, 170), (380, 70)], fill=(180, 180, 185, 255), width=5)])
def shelf(d):
    d.rectangle([120, 80, 392, 440], fill=(110, 70, 40, 255)); d.rectangle([135, 95, 377, 425], fill=(200, 220, 225, 120))
    for y in (190, 300): d.rectangle([135, y, 377, y + 10], fill=(110, 70, 40, 255))
    for k, c in enumerate([(200, 80, 60), (60, 120, 160), (220, 180, 70), (90, 140, 80)]): d.rectangle([150 + k * 52, 120, 190 + k * 52, 185], fill=c + (255,))
    d.ellipse([170, 220, 300, 280], fill=(140, 150, 120, 255)); d.polygon([(295, 250), (340, 225), (340, 275)], fill=(140, 150, 120, 255))
boxitem('shelf', shelf)
boxitem('fridge', lambda d: [d.rounded_rectangle([150, 60, 362, 450], 30, fill=(150, 210, 200, 255)), d.line([(160, 200), (352, 200)], fill=(110, 170, 160, 255), width=6),
                             d.rectangle([330, 110, 342, 170], fill=(220, 220, 220, 255)), d.rectangle([330, 230, 342, 330], fill=(220, 220, 220, 255))])
boxitem('paint', lambda d: [d.polygon([(130, 230), (382, 230), (360, 430), (152, 430)], fill=(180, 180, 185, 255)), d.ellipse([130, 205, 382, 255], fill=(240, 200, 120, 255)),
                            d.rectangle([300, 80, 340, 210], fill=(140, 90, 50, 255)), d.rounded_rectangle([270, 190, 370, 240], 10, fill=(240, 200, 120, 255))])
boxitem('tv', lambda d: [d.rounded_rectangle([100, 130, 412, 390], 30, fill=(110, 75, 45, 255)), d.rounded_rectangle([125, 155, 330, 365], 40, fill=(70, 80, 80, 255)),
                         d.rounded_rectangle([140, 170, 315, 350], 36, fill=(150, 165, 160, 255)), d.ellipse([350, 180, 390, 220], fill=(200, 190, 170, 255)), d.ellipse([350, 240, 390, 280], fill=(200, 190, 170, 255)),
                         d.line([(220, 130), (170, 50)], fill=(160, 160, 165, 255), width=5), d.line([(260, 130), (320, 50)], fill=(160, 160, 165, 255), width=5),
                         d.rectangle([140, 390, 170, 440], fill=(90, 60, 35, 255)), d.rectangle([342, 390, 372, 440], fill=(90, 60, 35, 255))])
boxitem('tutor', lambda d: [*[d.rounded_rectangle([130 + k * 10, 300 - k * 60, 382 - k * 10, 350 - k * 60], 8, fill=c + (255,)) for k, c in enumerate([(160, 50, 40), (50, 90, 150), (200, 160, 50), (60, 120, 70)])],
                            d.polygon([(256, 40), (390, 90), (256, 140), (122, 90)], fill=(30, 30, 40, 255)), d.line([(370, 95), (380, 170)], fill=(220, 180, 60, 255), width=5)])
boxitem('veranda', lambda d: [d.rectangle([60, 330, 452, 360], fill=(140, 95, 55, 255)), *[d.rectangle([80 + k * 60, 220, 92 + k * 60, 330], fill=(120, 80, 45, 255)) for k in range(7)],
                              d.rectangle([60, 210, 452, 228], fill=(120, 80, 45, 255)), d.polygon([(40, 210), (256, 90), (472, 210)], fill=(150, 70, 50, 255)),
                              d.rectangle([60, 360, 452, 440], fill=(90, 150, 170, 200)), d.ellipse([360, 120, 430, 190], fill=(255, 190, 80, 220))])
print('ok')
