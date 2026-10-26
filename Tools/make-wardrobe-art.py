# วาดหุ่นจำลองตู้แต่งตัว (เป็นชั้น ย้อมสีในเกมได้) + ไอคอนทุ่น สายเบ็ด กรอบการ์ด — python tools/make-wardrobe-art.py
#   doll_base (คน ผิว กางเกง หน้า) · doll_shirt / doll_hat / doll_hatband (สีขาว เกมย้อมตามของที่เลือก) · ชั้นวางซ้อนตรงกันพอดี
#   ico_bobber / ico_bobber_tip · ico_line (คันเบ็ดกับสาย สีขาว ย้อมได้) · ico_frame
import math
from PIL import Image, ImageDraw, ImageFilter

OUT = 'Assets/Game/Resources/UI/Wardrobe'
import os; os.makedirs(OUT, exist_ok=True)
W, H, S = 360, 520, 2

def canvas(): return Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0))
def save(img, name):
    img = img.resize((img.width // S, img.height // S), Image.LANCZOS); img.save(f'{OUT}/{name}.png'); print(name, img.size)
def P(*xy): return [(x * S, y * S) for x, y in xy]

def outline(img, col=(40, 26, 14, 255), w=3):
    a = img.split()[3]; e = a.filter(ImageFilter.MaxFilter(w * 2 + 1))
    o = Image.new('RGBA', img.size, col); o.putalpha(e); o.alpha_composite(img); return o

def shade(img, k=0.82):   # ไล่เงาด้านขวาให้ดูมีมิติ
    px = img.load(); w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a: f = 1 - (1 - k) * (x / w); px[x, y] = (int(r * f), int(g * f), int(b * f), a)
    return img

# ---------- หุ่นฐาน: หัว แขน ขา กางเกง รองเท้า ----------
base = canvas(); d = ImageDraw.Draw(base)
skin, pants, shoe, hair = (214, 166, 120, 255), (70, 74, 96, 255), (60, 44, 30, 255), (40, 30, 22, 255)
d.polygon(P((135, 300), (225, 300), (232, 455), (192, 455), (180, 345), (168, 455), (128, 455)), fill=pants)       # กางเกงขาสั้นถึงเข่า + ขา
d.rectangle(P((134, 400), (166, 470)), fill=skin); d.rectangle(P((194, 400), (226, 470)), fill=skin)
d.polygon(P((128, 470), (170, 470), (172, 490), (118, 490)), fill=shoe); d.polygon(P((190, 470), (232, 470), (242, 490), (188, 490)), fill=shoe)
d.ellipse(P((100, 175), (130, 290)), fill=skin); d.ellipse(P((230, 175), (260, 290)), fill=skin)                    # แขน
d.ellipse(P((95, 280), (128, 312)), fill=skin); d.ellipse(P((232, 280), (265, 312)), fill=skin)                     # มือ
d.rectangle(P((166, 140), (194, 172)), fill=skin)                                                                   # คอ
d.ellipse(P((136, 62), (224, 156)), fill=skin)                                                                      # หัว
d.ellipse(P((140, 66), (220, 112)), fill=hair)                                                                       # ผม
for ex in (162, 198): d.ellipse(P((ex - 5, 108), (ex + 5, 120)), fill=(40, 28, 20, 255))                          # ตา
d.arc(P((166, 120), (194, 140)), 20, 160, fill=(120, 60, 40, 255), width=3 * S)                                   # ยิ้ม
save(outline(shade(base)), 'doll_base')

# ---------- เสื้อ (ขาว ย้อมสีได้) ----------
sh = canvas(); d = ImageDraw.Draw(sh)
d.polygon(P((132, 168), (228, 168), (262, 198), (246, 232), (228, 220), (230, 312), (130, 312), (132, 220), (114, 232), (98, 198)), fill=(255, 255, 255, 255))
d.polygon(P((166, 168), (180, 188), (194, 168)), fill=(0, 0, 0, 0))                                                   # คอเสื้อ (เว้นให้เห็นผิว)
d.line(P((180, 192), (180, 310)), fill=(215, 215, 215, 255), width=2 * S)                                          # แนวกระดุม
for y in (210, 240, 270): d.ellipse(P((176, y), (184, y + 8)), fill=(200, 200, 200, 255))
save(outline(shade(sh, 0.78)), 'doll_shirt')

# ---------- หมวกสาน ปีกกว้าง (ขาว) + แถบหมวก ----------
hat = canvas(); d = ImageDraw.Draw(hat)
d.ellipse(P((96, 78), (264, 116)), fill=(255, 255, 255, 255))                                                       # ปีกหมวก
d.polygon(P((140, 96), (148, 46), (180, 36), (212, 46), (220, 96)), fill=(255, 255, 255, 255))                     # ทรงหมวก
for i in range(7): d.line(P((104 + i * 24, 98), (118 + i * 22, 82)), fill=(225, 225, 225, 255), width=2 * S)      # ลายสาน
save(outline(shade(hat, 0.8)), 'doll_hat')
band = canvas(); d = ImageDraw.Draw(band)
d.polygon(P((142, 86), (218, 86), (220, 98), (140, 98)), fill=(255, 255, 255, 255))
save(band, 'doll_hatband')

# ---------- ไอคอนทุ่น (ตัว + ยอด แยกชั้น) ----------
SZ = 200
bob = Image.new('RGBA', (SZ * S, SZ * S), (0, 0, 0, 0)); d = ImageDraw.Draw(bob)
d.line([(100 * S, 10 * S), (100 * S, 40 * S)], fill=(230, 230, 230, 255), width=2 * S)
d.ellipse([(70 * S, 60 * S), (130 * S, 150 * S)], fill=(255, 255, 255, 255)); d.rectangle([(94 * S, 150 * S), (106 * S, 190 * S)], fill=(255, 255, 255, 255))
bob = outline(shade(bob, 0.75))
bob.resize((SZ, SZ), Image.LANCZOS).save(f'{OUT}/ico_bobber.png'); print('ico_bobber')
tip = Image.new('RGBA', (SZ * S, SZ * S), (0, 0, 0, 0)); d = ImageDraw.Draw(tip)
d.pieslice([(70 * S, 60 * S), (130 * S, 150 * S)], 180, 360, fill=(255, 255, 255, 255)); d.rectangle([(94 * S, 36 * S), (106 * S, 64 * S)], fill=(255, 255, 255, 255))
tip.resize((SZ, SZ), Image.LANCZOS).save(f'{OUT}/ico_bobber_tip.png'); print('ico_bobber_tip')

# ---------- ไอคอนสายเบ็ด: คันเบ็ดน้ำตาล (ฐาน) + เส้นสายโค้ง (ขาว ย้อมได้) ----------
rod = Image.new('RGBA', (SZ * S, SZ * S), (0, 0, 0, 0)); d = ImageDraw.Draw(rod)
d.line([(20 * S, 185 * S), (170 * S, 25 * S)], fill=(120, 80, 40, 255), width=7 * S); d.ellipse([(38 * S, 150 * S), (62 * S, 174 * S)], fill=(90, 90, 96, 255))
rod.resize((SZ, SZ), Image.LANCZOS).save(f'{OUT}/ico_rod.png'); print('ico_rod')
ln = Image.new('RGBA', (SZ * S, SZ * S), (0, 0, 0, 0)); d = ImageDraw.Draw(ln)
pts = [(170 + 10 * math.sin(t * 3) * t - 0, 25 + 150 * t) for t in [i / 40 for i in range(41)]]
d.line([(x * S, y * S) for x, y in pts], fill=(255, 255, 255, 255), width=4 * S, joint='curve')
d.ellipse([(160 * S, 170 * S), (184 * S, 194 * S)], fill=(255, 255, 255, 255))
ln.resize((SZ, SZ), Image.LANCZOS).save(f'{OUT}/ico_line.png'); print('ico_line')

# ---------- ไอคอนกรอบการ์ด (กรอบขาวย้อมได้ + ปลากลาง) ----------
fr = Image.new('RGBA', (SZ * S, SZ * S), (0, 0, 0, 0)); d = ImageDraw.Draw(fr)
d.rounded_rectangle([(20 * S, 34 * S), (180 * S, 166 * S)], 14 * S, outline=(255, 255, 255, 255), width=14 * S)
fr.resize((SZ, SZ), Image.LANCZOS).save(f'{OUT}/ico_frame.png'); print('ico_frame')
fish = Image.new('RGBA', (SZ * S, SZ * S), (0, 0, 0, 0)); d = ImageDraw.Draw(fish)
d.rectangle([(34 * S, 48 * S), (166 * S, 152 * S)], fill=(236, 222, 190, 255))
d.ellipse([(60 * S, 80 * S), (130 * S, 120 * S)], fill=(90, 120, 130, 255)); d.polygon([(128 * S, 100 * S), (152 * S, 82 * S), (152 * S, 118 * S)], fill=(90, 120, 130, 255))
fish.resize((SZ, SZ), Image.LANCZOS).save(f'{OUT}/ico_frame_in.png'); print('ico_frame_in')
