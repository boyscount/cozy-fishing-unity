# วาดภาพรอยจารึกโบราณในถ้ำ + ระลอกหยดน้ำ + ค้างคาว — python tools/make-cave-art.py
# รอยจารึก: ภาพเขียนสีดินแดง (ปลา ดวงตา คน มือทาบ) + อักษรโบราณขูดขีดเป็นแถว · หลุดร่อนเป็นหย่อม · คราบเขม่าคบเพลิง · โปร่งใสนอกลาย (alpha cutout)
import math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy.ndimage import gaussian_filter

OUT = 'Assets/Game/Textures/Cave'
W, H = 1024, 512

def noise(h, w, scale, seed, octaves=4):
    rng = np.random.default_rng(seed); acc = np.zeros((h, w)); amp = 1; tot = 0
    for o in range(octaves):
        s = max(1, int(scale / (2 ** o)))
        g = rng.random((h // s + 2, w // s + 2))
        img = Image.fromarray((g * 255).astype(np.uint8)).resize((w + 2 * s, h + 2 * s), Image.BICUBIC)
        acc += np.asarray(img, np.float32)[s:s + h, s:s + w] / 255 * amp; tot += amp; amp *= 0.5
    return acc / tot

def glyphs(seed):
    rng = random.Random(seed)
    paint = Image.new('L', (W, H), 0); pd = ImageDraw.Draw(paint)      # สีดินแดง
    scratch = Image.new('L', (W, H), 0); sd = ImageDraw.Draw(scratch)  # รอยขูด (สีหินซีด)
    def jitter(pts, a=2.5): return [(x + rng.uniform(-a, a), y + rng.uniform(-a, a)) for x, y in pts]
    def fish(cx, cy, L):
        body = [(cx - L * 0.5 + L * t, cy + math.sin(t * math.pi) * L * 0.22 * s) for s in (1, -1) for t in ([i / 12 for i in range(13)] if s == 1 else [1 - i / 12 for i in range(13)])]
        pd.line(jitter(body) + [body[0]], fill=255, width=rng.randint(5, 8), joint='curve')
        pd.polygon(jitter([(cx + L * 0.5, cy), (cx + L * 0.75, cy - L * 0.2), (cx + L * 0.72, cy + L * 0.2)]), fill=255)
        pd.ellipse([cx - L * 0.36, cy - 5, cx - L * 0.28, cy + 3], fill=255)
        for k in range(3): pd.line(jitter([(cx - L * 0.1 + k * L * 0.15, cy - L * 0.12), (cx - L * 0.1 + k * L * 0.15, cy + L * 0.12)]), fill=255, width=3)   # ก้างปลาแบบภาพเขียนโบราณ
    def eye(cx, cy, r):
        for k in range(3): pd.ellipse([cx - r + k * r * 0.3, cy - r * 0.55 + k * r * 0.17, cx + r - k * r * 0.3, cy + r * 0.55 - k * r * 0.17], outline=255, width=5)
        pd.ellipse([cx - r * 0.15, cy - r * 0.15, cx + r * 0.15, cy + r * 0.15], fill=255)
    def person(cx, cy, s):
        pd.ellipse([cx - s * 0.12, cy - s, cx + s * 0.12, cy - s * 0.76], fill=255)
        pd.line(jitter([(cx, cy - s * 0.76), (cx, cy - s * 0.3)]), fill=255, width=6)
        pd.line(jitter([(cx - s * 0.32, cy - s * 0.75), (cx, cy - s * 0.6), (cx + s * 0.3, cy - s * 0.82)]), fill=255, width=5)
        pd.line(jitter([(cx - s * 0.22, cy), (cx, cy - s * 0.3), (cx + s * 0.22, cy)]), fill=255, width=5)
    def hand(cx, cy, s):   # มือทาบพ่นสี (ขอบฟุ้ง)
        m = Image.new('L', (W, H), 0); md = ImageDraw.Draw(m)
        md.ellipse([cx - s * 0.32, cy - s * 0.1, cx + s * 0.32, cy + s * 0.5], fill=255)
        for k, (dx, ln) in enumerate([(-0.28, 0.55), (-0.1, 0.75), (0.08, 0.8), (0.24, 0.65), (0.42, 0.35)]):
            ang = -0.2 + k * 0.12 if k < 4 else 0.8
            x1, y1 = cx + dx * s, cy
            md.line([(x1, y1), (x1 + math.sin(ang) * ln * s, y1 - math.cos(ang) * ln * s)], fill=255, width=int(s * 0.13))
        m = m.filter(ImageFilter.GaussianBlur(3))
        paint.paste(255, (0, 0), m)
    # จัดภาพเป็นหมู่ ๆ ทั่วแผ่น ไม่เรียงเป็นตาราง
    for _ in range(rng.randint(3, 5)): fish(rng.uniform(90, W - 120), rng.uniform(70, H - 70), rng.uniform(80, 150))
    for _ in range(rng.randint(1, 2)): eye(rng.uniform(120, W - 120), rng.uniform(80, H - 80), rng.uniform(40, 70))
    for _ in range(rng.randint(2, 4)): person(rng.uniform(80, W - 80), rng.uniform(150, H - 30), rng.uniform(70, 110))
    for _ in range(rng.randint(2, 3)): hand(rng.uniform(80, W - 80), rng.uniform(90, H - 90), rng.uniform(60, 90))
    # อักษรโบราณขูดขีดเป็นแถวเอียงเล็กน้อย (ขีด วง ตะขอ) — ไม่ใช่ฟอนต์
    for row in range(rng.randint(2, 3)):
        y0 = rng.uniform(60, H - 60); x = rng.uniform(40, 200); slope = rng.uniform(-0.06, 0.06)
        while x < rng.uniform(W * 0.55, W - 60):
            y = y0 + (x - 40) * slope; h = rng.uniform(18, 30); k = rng.randint(0, 4)
            if k == 0: sd.line(jitter([(x, y - h / 2), (x + 4, y + h / 2)], 1.5), fill=255, width=3)
            elif k == 1: sd.arc([x - 8, y - h / 2, x + 8, y + h / 2], rng.uniform(0, 90), rng.uniform(200, 330), fill=255, width=3)
            elif k == 2: sd.line(jitter([(x - 6, y - h / 2), (x + 6, y - h / 2), (x, y + h / 2)], 1.5), fill=255, width=3)
            elif k == 3: sd.line(jitter([(x, y - h / 2), (x, y + h / 3), (x + 8, y + h / 2)], 1.5), fill=255, width=3)
            else: sd.ellipse([x - 5, y - 5, x + 5, y + 5], outline=255, width=3)
            x += rng.uniform(14, 24)
    # หลุดร่อน/เลือนเป็นหย่อม
    erode = noise(H, W, 40, seed) * 0.7 + noise(H, W, 6, seed + 1, 2) * 0.3
    pa = np.asarray(paint, np.float32) / 255 * (erode > 0.42) * np.clip((erode - 0.35) * 4, 0, 1)
    sa = np.asarray(scratch, np.float32) / 255 * (erode > 0.36)
    # คราบเขม่าคบเพลิงเป็นปื้นดำด้านบน
    soot = np.clip(noise(H, W, 90, seed + 5) - 0.72, 0, 1) * 3.5
    soot *= np.linspace(1.2, 0.2, H)[:, None]
    soot = np.clip(soot, 0, 1) * (noise(H, W, 5, seed + 6, 2) > 0.45)
    col = np.zeros((H, W, 3)); a = np.zeros((H, W))
    ochre = np.array([150, 62, 32]) * (0.75 + 0.35 * noise(H, W, 20, seed + 2))[..., None]
    col += ochre * pa[..., None]; a = np.maximum(a, pa)
    scr = np.array([172, 160, 138]); m = sa > 0.5
    col[m] = scr; a = np.maximum(a, sa)
    sm = soot > 0.35; col[sm & (a < 0.5)] = [44, 38, 34]; a = np.maximum(a, (soot > 0.35).astype(np.float32))
    img = np.dstack([np.clip(col, 0, 255), np.clip(a * 255, 0, 255)]).astype(np.uint8)
    return Image.fromarray(img, 'RGBA')

def ripple():
    S = 128; yy, xx = np.mgrid[0:S, 0:S].astype(np.float32); r = np.hypot(xx - 63.5, yy - 63.5) / 63.5
    a = np.exp(-((r - 0.8) / 0.07) ** 2) + 0.4 * np.exp(-((r - 0.55) / 0.06) ** 2)
    a = np.clip(a, 0, 1) * 255
    return Image.fromarray(np.dstack([np.full((S, S), 255), np.full((S, S), 255), np.full((S, S), 255), a]).astype(np.uint8), 'RGBA')

def bat():
    S = 128; img = Image.new('RGBA', (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    c = (16, 14, 14, 255)
    d.ellipse([56, 52, 72, 80], fill=c); d.polygon([(58, 54), (60, 44), (63, 52)], fill=c); d.polygon([(70, 54), (68, 44), (65, 52)], fill=c)
    for s in (-1, 1):
        pts = [(64 + s * 6, 60), (64 + s * 30, 40), (64 + s * 58, 52), (64 + s * 48, 60), (64 + s * 40, 70), (64 + s * 28, 64), (64 + s * 18, 74)]
        d.polygon(pts, fill=c)
    return img

glyphs(11).save(f'{OUT}/glyphs_a.png')
glyphs(23).save(f'{OUT}/glyphs_b.png')
ripple().save(f'{OUT}/ripple.png')
bat().save(f'{OUT}/bat.png')
print('ok')
