# วาดป้ายไม้ดิบ + ของประกอบปุ่มหน้าแรก (สิ่งของย้อนยุค) — python tools/make-title-props.py
# ป้ายไม้เศษไม้เฉือนขอบขรุขระ เชือกพัน หมุดสนิม · แผนที่พับ · พวงกุญแจ+ตราแดง · ทุ่น+เหยื่อปลอม · ภาพถ่ายปลา+คลิป · กุญแจไขลาน · ตราครั่งประจำยาม
import math, os, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageChops
from scipy.ndimage import gaussian_filter, distance_transform_edt

OUT = 'Assets/Game/Resources/UI/Title'
os.makedirs(OUT, exist_ok=True)

def noise(h, w, scale, seed, octaves=4):
    """value noise หลายชั้น (0..1)"""
    rng = np.random.default_rng(seed); acc = np.zeros((h, w)); amp = 1; tot = 0
    for o in range(octaves):
        s = max(1, int(scale / (2 ** o)))
        g = rng.random((h // s + 2, w // s + 2))
        img = Image.fromarray((g * 255).astype(np.uint8)).resize((w + 2 * s, h + 2 * s), Image.BICUBIC)
        acc += np.asarray(img, np.float32)[s:s + h, s:s + w] / 255 * amp; tot += amp; amp *= 0.5
    return acc / tot

def rgba(arr_rgb, alpha):
    a = np.dstack([np.clip(arr_rgb, 0, 255), np.clip(alpha, 0, 255)]).astype(np.uint8)
    return Image.fromarray(a, 'RGBA')

def shadow(img, off=(6, 10), blur=10, op=0.55):
    """เงาตกใต้วัตถุ (ภาพต้องมีขอบว่างพอ)"""
    a = np.asarray(img.split()[3], np.float32) / 255
    sh = gaussian_filter(np.roll(np.roll(a, off[1], 0), off[0], 1), blur) * op
    base = rgba(np.zeros(a.shape + (3,)) + [12, 8, 4], sh * 255)
    base.alpha_composite(img); return base

# ---------------------------------------------------------------- ป้ายไม้
def plank(w, h, seed, tone=(172, 112, 58), ropes=(True, True), nails=True):
    rng = random.Random(seed); P = 40   # ขอบว่างรอบป้าย (เงา/เชือกล้น)
    W, H = w + 2 * P, h + 2 * P
    # รูปทรง: ขอบบน/ล่างขรุขระ มีรอยบิ่น · หัวท้ายเฉือนเฉียง/แตกเสี้ยน
    def edge(y0, sign):
        pts = []; x = P + rng.randint(4, 14)
        while x < P + w - 10:
            y = y0 + sign * (rng.uniform(-2.5, 2.5)); pts.append((x, y))
            if rng.random() < 0.08:   # รอยบิ่น
                d = rng.uniform(5, 11); pts.append((x + 6, y0 - sign * -d * -1 + sign * d)); x += 12
            x += rng.randint(14, 30)
        return pts
    top = edge(P, 1); bot = edge(P + h, -1)
    left = [(P + rng.randint(0, 10), P + h * t + rng.uniform(-3, 3)) for t in (0.9, 0.62, 0.35, 0.1)]
    right = []
    for t in (0.06, 0.25, 0.42, 0.6, 0.8, 0.95):   # หัวขวาแตกเป็นเสี้ยน
        right.append((P + w - rng.randint(0, 6) - (10 if rng.random() < 0.4 else 0), P + h * t))
    poly = [(P + 6, P + 2)] + top + [(P + w - 4, P + 3)] + right + [(P + w - 8, P + h - 2)] + bot[::-1] + [(P + 3, P + h - 4)] + left
    m = Image.new('L', (W, H), 0); ImageDraw.Draw(m).polygon(poly, fill=255)
    mask = np.asarray(m, np.float32) / 255
    # ลายไม้: เส้นตามแนวนอนบิดด้วย noise
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    warp = noise(H, W, 90, seed) * 26 + noise(H, W, 30, seed + 1) * 6
    g = np.sin((yy + warp) * 0.32) * 0.5 + 0.5
    g2 = np.sin((yy * 0.13 + warp * 0.2)) * 0.5 + 0.5
    fib = noise(H, W, 3, seed + 2, 2); streak = gaussian_filter(np.random.default_rng(seed).random((H, W)), (0.6, 18)) * 4 - 1.5
    lum = 0.86 + 0.08 * g + 0.08 * g2 + 0.06 * (fib - 0.5) + 0.2 * (streak - 0.5) - 0.14 * noise(H, W, 60, seed + 3)
    # ปุ่มตาไม้ 1 จุด
    kx, ky = P + w * rng.uniform(0.3, 0.7), P + h * rng.uniform(0.3, 0.7)
    kd = np.hypot((xx - kx) / 1.8, yy - ky); lum -= 0.22 * np.exp(-(kd / 9) ** 2) * (np.sin(kd * 0.9) * 0.5 + 0.8)
    # ขอบมืด + สันบนสว่าง
    dist = distance_transform_edt(mask > 0.5)
    lum *= 0.62 + 0.38 * np.clip(dist / 14, 0, 1)
    lum += 0.12 * np.clip(1 - np.abs(yy - (P + 5)) / 4, 0, 1) * mask
    lum -= 0.18 * np.clip(1 - np.abs(yy - (P + h - 4)) / 5, 0, 1)
    # รอยแตกตามเสี้ยน
    crack = np.zeros((H, W), np.float32); cd = ImageDraw.Draw(cm := Image.new('L', (W, H), 0))
    for _ in range(3):
        cy = P + rng.uniform(0.15, 0.85) * h; cx = P + rng.uniform(0, 0.6) * w; L = rng.uniform(0.15, 0.4) * w
        pts = [(cx + t * L, cy + math.sin(t * 7 + seed) * 2 + rng.uniform(-1, 1)) for t in np.linspace(0, 1, 12)]
        cd.line(pts, fill=255, width=2)
    crack = gaussian_filter(np.asarray(cm, np.float32) / 255, 0.7)
    lum -= 0.45 * crack
    col = np.dstack([lum * tone[0], lum * tone[1], lum * tone[2]])
    # คราบเปื้อนเขียวตะไคร่/ดำ
    stain = np.clip(noise(H, W, 70, seed + 9) - 0.55, 0, 1) * 2.2
    col = col * (1 - stain[..., None] * 0.45) + stain[..., None] * 0.45 * np.array([70, 74, 40])
    img = rgba(col, mask * 255)
    d = ImageDraw.Draw(img)
    # หมุดเหล็กขึ้นสนิม
    if nails:
        for nx in (P + 0.15 * w, P + 0.85 * w):
            for ny in (P + 0.24 * h, P + 0.76 * h):
                if rng.random() < 0.6: continue
                nx2, ny2 = nx + rng.uniform(-8, 8), ny + rng.uniform(-3, 3); r = max(4, h * 0.038)
                d.ellipse([nx2 - r * 1.8, ny2 - r * 1.5, nx2 + r * 1.8, ny2 + r * 2.2], fill=(120, 62, 28, 45))   # คราบสนิมรอบหมุด
                d.ellipse([nx2 - r - 1, ny2 - r + 1, nx2 + r + 1, ny2 + r + 2], fill=(25, 15, 8, 200))
                d.ellipse([nx2 - r, ny2 - r, nx2 + r, ny2 + r], fill=(96, 56, 32, 255))
                d.ellipse([nx2 - r * 0.6, ny2 - r * 0.7, nx2 + r * 0.2, ny2 - r * 0.05], fill=(170, 110, 70, 255))
    # เชือกพันรอบหัวท้าย
    def rope(cx):
        n = 4; sw = h * 0.08
        for i in range(n):
            x = cx + (i - n / 2) * sw * 0.95
            y0, y1 = P - 9, P + h + 9
            for k in range(14):   # เกลียวเชือก: วงรีเรียงเฉียง
                t = k / 13; px = x + (t - 0.5) * sw * 1.7; py = y0 + t * (y1 - y0)
                e = sw * 0.62
                d.ellipse([px - e, py - e * 1.25, px + e, py + e * 1.25], fill=(60, 40, 22, 255))
            for k in range(14):
                t = k / 13; px = x + (t - 0.5) * sw * 1.7; py = y0 + t * (y1 - y0); e = sw * 0.5
                c0 = 150 + rng.randint(-18, 14)
                d.ellipse([px - e, py - e * 1.15, px + e, py + e * 1.05], fill=(c0, int(c0 * 0.8), int(c0 * 0.52), 255))
                d.line([(px - e * 0.6, py - e * 0.5), (px + e * 0.5, py + e * 0.4)], fill=(110, 82, 50, 255), width=max(1, int(e * 0.25)))
    if ropes[0]: rope(P + w * 0.075)
    if ropes[1]: rope(P + w * 0.93)
    # ลายผิวเชือก/ไม้เนียนขึ้นเล็กน้อย
    img = img.filter(ImageFilter.SMOOTH)
    return shadow(img, (4, 8), 7, 0.6)

# ---------------------------------------------------------------- ของประกอบ
def paper(w, h, seed, base=(222, 202, 160)):
    n = noise(h, w, 40, seed) * 0.6 + noise(h, w, 6, seed + 1, 2) * 0.4
    col = np.dstack([n * 0 + base[0], n * 0 + base[1], n * 0 + base[2]]) * (0.82 + 0.25 * n[..., None])
    yy, xx = np.mgrid[0:h, 0:w]
    edge = np.minimum(np.minimum(xx, w - 1 - xx), np.minimum(yy, h - 1 - yy)).astype(np.float32)
    col *= (0.72 + 0.28 * np.clip(edge / 18, 0, 1))[..., None]
    burn = np.clip(1 - edge / 6, 0, 1) * (noise(h, w, 10, seed + 3) > 0.35)
    col = col * (1 - burn[..., None] * 0.5)
    a = np.clip(edge * 2 - (noise(h, w, 8, seed + 4) * 8), 0, 1) * 255
    return rgba(col, a)

def prop_map():
    S = 300; img = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    p = paper(190, 170, 11)
    d = ImageDraw.Draw(p)
    # รอยพับ + แม่น้ำ + ทางประ + กากบาท
    for x in (63, 127): d.line([(x, 4), (x + 2, 166)], fill=(150, 120, 80, 150), width=2)
    d.line([(4, 86), (186, 84)], fill=(150, 120, 80, 150), width=2)
    river = [(10, 140), (40, 120), (60, 128), (90, 100), (120, 104), (150, 70), (180, 60)]
    d.line(river, fill=(70, 100, 120, 200), width=5, joint='curve')
    for i in range(0, 9):
        x = 30 + i * 14; y = 40 + math.sin(i) * 10
        d.line([(x, y), (x + 7, y + 2)], fill=(120, 40, 25, 210), width=3)
    d.line([(150, 30), (166, 46)], fill=(160, 30, 20, 255), width=5); d.line([(166, 30), (150, 46)], fill=(160, 30, 20, 255), width=5)
    for tx, ty in ((30, 70), (100, 140), (140, 120)):   # ต้นไม้เล็ก
        d.polygon([(tx, ty - 12), (tx - 8, ty + 4), (tx + 8, ty + 4)], fill=(80, 90, 50, 200))
    p = p.rotate(-10, Image.BICUBIC, expand=True)
    img.alpha_composite(shadow(p, (5, 8), 6, 0.5), (40, 50))
    d = ImageDraw.Draw(img)
    # หมุดไม้ (ไม้หนีบผ้าไม้ไผ่) ด้านบน
    d.rounded_rectangle([120, 20, 150, 110], 10, fill=(60, 36, 16, 255))
    d.rounded_rectangle([123, 22, 147, 106], 9, fill=(176, 128, 74, 255))
    d.line([(135, 26), (135, 100)], fill=(120, 80, 40, 255), width=2)
    d.ellipse([127, 54, 143, 70], fill=(90, 90, 92, 255)); d.ellipse([130, 57, 140, 67], fill=(150, 150, 150, 255))
    return img

def prop_key():
    S = 300; img = Image.new('RGBA', (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    # ห่วงเหล็ก
    d.ellipse([90, 20, 170, 100], outline=(40, 34, 30, 255), width=12); d.ellipse([92, 22, 168, 98], outline=(120, 108, 96, 255), width=7)
    d.arc([96, 26, 164, 94], 200, 260, fill=(200, 190, 175, 255), width=3)
    # กุญแจทองเหลืองเก่า
    k = Image.new('RGBA', (S, S), (0, 0, 0, 0)); kd = ImageDraw.Draw(k)
    br, br2, dk = (176, 132, 60, 255), (226, 186, 110, 255), (70, 46, 18, 255)
    kd.ellipse([118, 80, 182, 144], fill=dk); kd.ellipse([122, 84, 178, 140], fill=br); kd.ellipse([138, 100, 162, 124], fill=(0, 0, 0, 0))
    kd.arc([124, 86, 176, 138], 190, 250, fill=br2, width=4)
    kd.rectangle([145, 140, 157, 262], fill=dk); kd.rectangle([147, 140, 155, 260], fill=br); kd.line([(149, 146), (149, 255)], fill=br2, width=2)
    for y0, w0 in ((222, 28), (244, 20)):
        kd.rectangle([155, y0, 155 + w0, y0 + 14], fill=dk); kd.rectangle([156, y0 + 2, 153 + w0, y0 + 12], fill=br)
    k = k.rotate(18, Image.BICUBIC, center=(130, 70))
    img.alpha_composite(shadow(k, (5, 7), 5, 0.5))
    d = ImageDraw.Draw(img)
    # พู่เชือกถักแดง
    for i in range(10):
        x = 92 + i * 4; d.line([(100, 80), (x, 230 + (i % 3) * 8)], fill=(150 + (i % 3) * 25, 30, 25, 255), width=3)
    d.rounded_rectangle([88, 74, 114, 100], 6, fill=(120, 20, 18, 255)); d.line([(90, 87), (112, 87)], fill=(220, 170, 60, 255), width=3)
    return img

def stamp_red(seed=5, text_ring=True):
    S = 200; img = Image.new('RGBA', (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    c = (176, 34, 28, 235)
    d.ellipse([12, 12, 188, 188], outline=c, width=9); d.ellipse([34, 34, 166, 166], outline=c, width=4)
    for k in range(28):   # อักษรวงรอบ (ขีดสั้นแทนตัวหนังสือ)
        a = k / 28 * math.tau; r = 78
        d.line([(100 + math.cos(a) * (r - 6), 100 + math.sin(a) * (r - 6)), (100 + math.cos(a) * (r + 4), 100 + math.sin(a) * (r + 4))], fill=c, width=5)
    # ประจำยามกลางตรา
    for k in range(4):
        a = k * math.pi / 2; cx, cy = 100 + math.cos(a) * 22, 100 + math.sin(a) * 22
        d.ellipse([cx - 18, cy - 18, cx + 18, cy + 18], fill=c)
    d.ellipse([86, 86, 114, 114], fill=(0, 0, 0, 0)); d.ellipse([92, 92, 108, 108], fill=c)
    # หมึกจางเป็นหย่อม
    a = np.asarray(img.split()[3], np.float32) * np.clip(noise(S, S, 12, seed) * 1.8 - 0.25, 0, 1)
    img.putalpha(Image.fromarray(a.astype(np.uint8)))
    return img.rotate(-14, Image.BICUBIC)

def prop_lure():
    S = 320; img = Image.new('RGBA', (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    # สายเบ็ด
    d.line([(160, 0), (160, 70)], fill=(230, 225, 210, 220), width=2)
    # ทุ่นลอยไม้ทาสี (บนแดง ล่างครีม)
    b = Image.new('RGBA', (S, S), (0, 0, 0, 0)); bd = ImageDraw.Draw(b)
    bd.ellipse([126, 66, 194, 200], fill=(40, 20, 10, 255))
    bd.pieslice([129, 69, 191, 197], 180, 360, fill=(200, 64, 30, 255)); bd.pieslice([129, 69, 191, 197], 0, 180, fill=(232, 214, 170, 255))
    bd.rectangle([129, 131, 191, 137], fill=(30, 22, 16, 255))
    bd.ellipse([140, 82, 158, 120], fill=(255, 170, 120, 160)); bd.ellipse([140, 142, 154, 170], fill=(255, 250, 230, 140))
    bd.rectangle([157, 40, 163, 70], fill=(60, 40, 20, 255)); bd.rectangle([157, 198, 163, 222], fill=(60, 40, 20, 255))
    img.alpha_composite(shadow(b, (5, 7), 6, 0.5))
    d = ImageDraw.Draw(img)
    # ตะขอเบ็ด
    d.line([(160, 222), (160, 280)], fill=(230, 225, 210, 220), width=2)
    d.line([(160, 280), (160, 300)], fill=(150, 150, 155, 255), width=5)
    d.arc([132, 280, 168, 316], 0, 180, fill=(150, 150, 155, 255), width=5); d.line([(133, 298), (140, 288)], fill=(150, 150, 155, 255), width=5)
    return img

def prop_lure_fish():
    """เหยื่อปลอมทำมือ (ไม้แกะรูปปลา ทาสี) ห้อยที่มุมซ้าย"""
    S = 300; f = Image.new('RGBA', (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(f)
    body = [(40, 150), (90, 112), (170, 108), (230, 128), (250, 150), (230, 172), (170, 190), (90, 186)]
    d.polygon([(x + 3, y + 3) for x, y in body], fill=(30, 26, 14, 255)); d.polygon(body, fill=(96, 140, 60, 255))
    d.polygon([(90, 150), (170, 150), (240, 152), (230, 170), (170, 188), (90, 184)], fill=(224, 206, 120, 255))
    for x in range(100, 220, 22): d.line([(x, 116), (x - 8, 150)], fill=(50, 80, 30, 255), width=6)
    d.polygon([(250, 150), (290, 120), (282, 150), (290, 180)], fill=(190, 60, 30, 255))   # หาง
    d.ellipse([60, 132, 84, 156], fill=(240, 220, 120, 255)); d.ellipse([67, 139, 79, 151], fill=(20, 16, 10, 255))
    d.polygon([(40, 150), (20, 160), (44, 162)], fill=(200, 190, 170, 255))   # ลิ้นพลาสติก
    for hx in (120, 200):   # ตะขอสามทาง
        d.line([(hx, 188), (hx, 222)], fill=(140, 140, 145, 255), width=4)
        d.arc([hx - 14, 210, hx + 2, 236], 0, 200, fill=(140, 140, 145, 255), width=4); d.arc([hx - 2, 210, hx + 14, 236], -20, 180, fill=(140, 140, 145, 255), width=4)
    d.ellipse([20, 140, 34, 154], outline=(160, 160, 160, 255), width=3)
    f = f.rotate(24, Image.BICUBIC)
    return shadow(f, (6, 8), 6, 0.5)

def prop_photo():
    S = 300; img = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    p = Image.new('RGBA', (200, 160), (236, 228, 208, 255)); d = ImageDraw.Draw(p)
    n = noise(132, 172, 20, 21)
    sep = np.dstack([110 + 70 * n, 88 + 56 * n, 60 + 40 * n])
    ph = rgba(sep, np.full(n.shape, 255)); pd = ImageDraw.Draw(ph)
    # ปลาช่อนในภาพ (เงาเข้ม) + ใบไม้น้ำ
    pd.ellipse([30, 50, 140, 90], fill=(52, 40, 26, 255)); pd.polygon([(136, 70), (166, 48), (160, 70), (166, 94)], fill=(52, 40, 26, 255))
    pd.polygon([(60, 52), (120, 44), (110, 54)], fill=(52, 40, 26, 255)); pd.ellipse([40, 62, 50, 70], fill=(200, 180, 140, 255))
    for x in range(56, 130, 14): pd.arc([x, 56, x + 18, 84], 260, 330, fill=(90, 72, 50, 255), width=2)
    pd.line([(10, 120), (20, 100), (14, 80)], fill=(70, 60, 40, 255), width=3)
    p.paste(ph, (14, 12)); d = ImageDraw.Draw(p)
    d.line([(14, 150), (186, 150)], fill=(150, 130, 100, 120), width=1)
    v = np.asarray(p).astype(np.float32); yy, xx = np.mgrid[0:160, 0:200]
    vig = 1 - 0.25 * (((xx - 100) / 100) ** 2 + ((yy - 80) / 80) ** 2); v[..., :3] *= vig[..., None]
    p = Image.fromarray(np.clip(v, 0, 255).astype(np.uint8), 'RGBA').rotate(8, Image.BICUBIC, expand=True)
    img.alpha_composite(shadow(p, (5, 8), 6, 0.5), (50, 60))
    d = ImageDraw.Draw(img)
    # คลิปหนีบรูป (ลวดเหล็ก)
    clip = [(150, 30), (150, 110), (166, 110), (166, 44), (158, 44), (158, 96)]
    d.line(clip, fill=(40, 40, 44, 255), width=7, joint='curve'); d.line(clip, fill=(170, 172, 178, 255), width=4, joint='curve')
    return img

def prop_scales():
    """เกล็ดปลาแห้ง + ก้อนหิน"""
    S = 220; img = Image.new('RGBA', (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    rng = random.Random(3)
    for i in range(5):
        cx, cy = 60 + rng.uniform(-30, 80), 70 + rng.uniform(-30, 80); r = rng.uniform(18, 26)
        sc = Image.new('RGBA', (80, 80), (0, 0, 0, 0)); sd = ImageDraw.Draw(sc)
        sd.ellipse([10, 16, 70, 64], fill=(214, 216, 200, 230)); sd.ellipse([14, 20, 66, 60], fill=(232, 236, 222, 220))
        for k in range(4): sd.arc([14 + k * 4, 20 + k * 3, 66 - k * 4, 60 - k * 3], 200, 340, fill=(170, 180, 175, 200), width=1)
        sd.ellipse([20, 24, 40, 38], fill=(200, 230, 240, 140))
        sc = sc.resize((int(r * 2.6), int(r * 2.6))).rotate(rng.uniform(0, 360), Image.BICUBIC)
        img.alpha_composite(sc, (int(cx - r), int(cy - r)))
    return shadow(img, (3, 5), 3, 0.4)

def icon_windkey():
    """กุญแจทองเหลืองไขลาน (ปีกผีเสื้อ) — ปุ่มตั้งค่า"""
    S = 256; k = Image.new('RGBA', (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(k)
    dk, br, hi = (64, 40, 14, 255), (178, 134, 58, 255), (236, 200, 120, 255)
    for sx in (-1, 1):   # ปีกสองข้าง
        cx = 128 + sx * 62
        d.ellipse([cx - 52, 40, cx + 52, 150], fill=dk); d.ellipse([cx - 46, 46, cx + 46, 144], fill=br)
        d.ellipse([cx - 22, 74, cx + 22, 116], fill=(0, 0, 0, 0))
        d.arc([cx - 40, 52, cx + 40, 138], 200, 270, fill=hi, width=6)
    d.rectangle([108, 90, 148, 120], fill=br)
    d.rectangle([114, 118, 142, 236], fill=dk); d.rectangle([118, 118, 138, 232], fill=br); d.line([(123, 124), (123, 226)], fill=hi, width=3)
    d.rectangle([104, 144, 152, 158], fill=dk); d.rectangle([106, 146, 150, 156], fill=br)
    return shadow(k.rotate(-28, Image.BICUBIC), (4, 6), 4, 0.5)

def icon_seal():
    """ตราครั่งแดง ลายดอกประจำยามสีทอง — ปุ่มภาษา"""
    S = 256; img = Image.new('RGBA', (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    rng = random.Random(8)
    pts = [(128 + math.cos(a) * (96 + rng.uniform(-6, 6)), 128 + math.sin(a) * (96 + rng.uniform(-6, 6))) for a in np.linspace(0, math.tau, 26, endpoint=False)]
    d.polygon(pts, fill=(96, 14, 12, 255))
    d.ellipse([44, 44, 212, 212], fill=(160, 28, 22, 255)); d.ellipse([58, 58, 198, 198], fill=(128, 20, 16, 255))
    d.arc([50, 50, 206, 206], 200, 280, fill=(220, 90, 70, 255), width=6)
    g = (232, 186, 90, 255)
    for k in range(4):   # ประจำยาม: กลีบปลายแหลม 4 ทิศ
        a = k * math.pi / 2 + math.pi / 4; ca, sa = math.cos(a), math.sin(a)
        tip = (128 + ca * 62, 128 + sa * 62); l = (128 + ca * 24 - sa * 22, 128 + sa * 24 + ca * 22); r = (128 + ca * 24 + sa * 22, 128 + sa * 24 - ca * 22)
        d.polygon([(128, 128), l, tip, r], fill=g)
        d.polygon([(128, 128), (128 + ca * 20 - sa * 9, 128 + sa * 20 + ca * 9), (128 + ca * 44, 128 + sa * 44), (128 + ca * 20 + sa * 9, 128 + sa * 20 - ca * 9)], fill=(128, 20, 16, 255))
    d.ellipse([112, 112, 144, 144], fill=g); d.ellipse([120, 120, 136, 136], fill=(128, 20, 16, 255))
    return shadow(img, (4, 6), 4, 0.5)

def glow():
    """แสงตะเกียงน้ำมัน (วงรีส้มทอง) วางทับตอนชี้ปุ่ม"""
    W, H = 512, 160; yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    r = np.hypot((xx - W * 0.42) / (W * 0.5), (yy - H * 0.4) / (H * 0.55))
    a = np.clip(1 - r, 0, 1) ** 1.6 * 200
    return rgba(np.dstack([a * 0 + 255, a * 0 + 196, a * 0 + 110]), a)

def mote():
    S = 32; yy, xx = np.mgrid[0:S, 0:S].astype(np.float32); r = np.hypot(xx - 15.5, yy - 15.5) / 15.5
    a = np.clip(1 - r, 0, 1) ** 2 * 255
    return rgba(np.dstack([a * 0 + 255, a * 0 + 236, a * 0 + 200]), a)

plank(900, 210, 1).save(f'{OUT}/plank_a.png')
plank(900, 210, 2, tone=(178, 118, 62)).save(f'{OUT}/plank_b.png')
plank(900, 210, 3, tone=(166, 108, 56)).save(f'{OUT}/plank_c.png')
plank(900, 210, 4, tone=(174, 114, 60)).save(f'{OUT}/plank_d.png')
plank(560, 150, 5, ropes=(False, True), nails=True).save(f'{OUT}/plank_s.png')
prop_map().save(f'{OUT}/prop_map.png')
prop_key().save(f'{OUT}/prop_key.png')
stamp_red().save(f'{OUT}/stamp_red.png')
prop_lure().save(f'{OUT}/prop_bobber.png')
prop_lure_fish().save(f'{OUT}/prop_lure.png')
prop_photo().save(f'{OUT}/prop_photo.png')
prop_scales().save(f'{OUT}/prop_scales.png')
icon_windkey().save(f'{OUT}/icon_windkey.png')
icon_seal().save(f'{OUT}/icon_seal.png')
glow().save(f'{OUT}/glow.png')
mote().save(f'{OUT}/mote.png')
print('ok')
