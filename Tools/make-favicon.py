# ไอคอนเว็บ (favicon): ปลาสีเงินกระโดดเหนือตะกร้าสาน บนพื้นแสงอาทิตย์ยามเย็น — python tools/make-favicon.py <TemplateData dir>
#   วาดใหญ่ 4 เท่าแล้วย่อ (ขอบเนียน) · ออก favicon.ico (16/32/48) · icon-180.png (iPhone) · icon-192/512.png (Android)
import sys, math, os
from PIL import Image, ImageDraw, ImageFilter

S = 1024
img = Image.new('RGBA', (S, S), (0, 0, 0, 0))

# พื้น: สี่เหลี่ยมมุมมน ไล่สีจากแสงทองตรงกลางไปน้ำตาลเข้มที่ขอบ
bg = Image.new('RGBA', (S, S))
px = bg.load()
for y in range(S):
    for x in range(S):
        d = math.hypot(x - S * 0.5, y - S * 0.42) / (S * 0.72)
        t = min(1, d)
        c0, c1 = (255, 196, 92), (74, 38, 18)
        px[x, y] = tuple(int(c0[i] + (c1[i] - c0[i]) * (t ** 0.9)) for i in range(3)) + (255,)
mask = Image.new('L', (S, S), 0)
ImageDraw.Draw(mask).rounded_rectangle([0, 0, S - 1, S - 1], radius=int(S * 0.22), fill=255)
img.paste(bg, (0, 0), mask)
d = ImageDraw.Draw(img)

# ผิวน้ำ: แถบน้ำเงินอมเขียวด้านล่าง + ประกายแดด
wy = int(S * 0.70)
water = Image.new('RGBA', (S, S), (0, 0, 0, 0)); wd = ImageDraw.Draw(water)
wd.rectangle([0, wy, S, S], fill=(28, 74, 84, 255))
for k in range(5):
    yy = wy + 40 + k * 46; wd.line([(S * 0.2 + k * 30, yy), (S * 0.8 - k * 30, yy)], fill=(255, 205, 120, 120), width=14)
img.paste(water, (0, 0), Image.composite(water, Image.new('RGBA', (S, S)), mask).split()[3])

# ตะกร้าสาน (ข้อง): ทรงคางหมูสีฟาง ลายสานเป็นตาราง
bx0, bx1, by0, by1 = S * 0.27, S * 0.73, S * 0.56, S * 0.86
basket = [(bx0, by0), (bx1, by0), (bx1 - S * 0.06, by1), (bx0 + S * 0.06, by1)]
d.polygon(basket, fill=(214, 160, 82, 255), outline=(92, 52, 22, 255))
for i in range(1, 6):
    y = by0 + (by1 - by0) * i / 6; inset = S * 0.06 * i / 6
    d.line([(bx0 + inset, y), (bx1 - inset, y)], fill=(150, 98, 40, 255), width=12)
for i in range(1, 9):
    u = i / 9; d.line([(bx0 + (bx1 - bx0) * u, by0), (bx0 + S * 0.06 + (bx1 - bx0 - S * 0.12) * u, by1)], fill=(170, 116, 52, 255), width=10)
d.rounded_rectangle([bx0 - S * 0.025, by0 - S * 0.035, bx1 + S * 0.025, by0 + S * 0.02], radius=24, fill=(178, 120, 56, 255), outline=(92, 52, 22, 255), width=8)

# ปลากระโดด: ลำตัวโค้งสีเงินอมฟ้า หางแฉก ตาโต
fish = Image.new('RGBA', (S, S), (0, 0, 0, 0)); fd = ImageDraw.Draw(fish)
cx, cy = S * 0.5, S * 0.34
body = []
for i in range(41):
    a = math.pi * i / 40
    body.append((cx + math.cos(a) * S * 0.24, cy - math.sin(a) * S * 0.11))
for i in range(41):
    a = math.pi * (1 - i / 40)
    body.append((cx + math.cos(a) * S * 0.24, cy + math.sin(a) * S * 0.08))
fd.polygon(body, fill=(206, 226, 232, 255), outline=(40, 70, 86, 255))
fd.polygon([(cx - S * 0.22, cy), (cx - S * 0.36, cy - S * 0.12), (cx - S * 0.32, cy), (cx - S * 0.36, cy + S * 0.11)], fill=(150, 190, 204, 255), outline=(40, 70, 86, 255))
fd.polygon([(cx - S * 0.02, cy - S * 0.1), (cx + S * 0.07, cy - S * 0.19), (cx + S * 0.1, cy - S * 0.09)], fill=(150, 190, 204, 255))
fd.chord([cx - S * 0.2, cy - S * 0.02, cx + S * 0.22, cy + S * 0.08], 0, 180, fill=(150, 186, 200, 255))   # ท้องเงา
fd.ellipse([cx + S * 0.13, cy - S * 0.05, cx + S * 0.18, cy], fill=(30, 30, 34, 255))
fd.ellipse([cx + S * 0.145, cy - S * 0.04, cx + S * 0.16, cy - S * 0.025], fill=(255, 255, 255, 255))
fish = fish.rotate(18, center=(cx, cy), resample=Image.BICUBIC)
img.alpha_composite(fish)
# หยดน้ำกระเซ็น
for (x, y, r) in [(0.22, 0.52, 0.022), (0.79, 0.5, 0.018), (0.7, 0.58, 0.014), (0.3, 0.6, 0.012)]:
    d.ellipse([S * (x - r), S * (y - r), S * (x + r), S * (y + r)], fill=(214, 240, 248, 255))

out = sys.argv[1] if len(sys.argv) > 1 else '.'
os.makedirs(out, exist_ok=True)
def sized(n):
    im = img.resize((n, n), Image.LANCZOS)
    return im.filter(ImageFilter.UnsharpMask(radius=0.6, percent=60, threshold=1)) if n <= 48 else im
sized(512).save(os.path.join(out, 'icon-512.png'))
sized(192).save(os.path.join(out, 'icon-192.png'))
sized(180).convert('RGB').save(os.path.join(out, 'icon-180.png'))   # iPhone ไม่ใช้พื้นโปร่ง
sized(48).save(os.path.join(out, 'favicon.ico'), sizes=[(16, 16), (32, 32), (48, 48)])
sized(64).save(os.path.join(out, 'favicon-preview.png'))
print('ok')
