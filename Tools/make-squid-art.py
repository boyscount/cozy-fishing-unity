# วาดภาพหมึก/ปลาด่านเรือไดหมึก (แบบภาพประกอบ ตัดพื้นหลังแล้ว หัว/ปลายลำตัวหันซ้าย กว้าง 512) — python tools/make-squid-art.py
#   ใช้แทนภาพถ่ายในสมุดปลา + พองเป็นโมเดลด้วย FishPhotoTools (FISH_IDS=...)
import math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy.ndimage import gaussian_filter

OUT = 'Assets/Game/Resources/Fish'
S = 2   # วาดใหญ่สองเท่าแล้วย่อ ขอบเนียน

def canvas(w, h): return Image.new('RGBA', (w * S, h * S), (0, 0, 0, 0))

def shade(img, base, belly, spots=None, seed=0, stripes=0, glow=None):
    """เติมสีตามรูปทรง (alpha) : ไล่เข้มด้านบน สว่างด้านท้อง + ขอบมืด + จุดสีผิว/ลาย"""
    a = np.asarray(img)[..., 3].astype(np.float32) / 255
    h, w = a.shape
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    cols = np.where(a.sum(0) > 0)[0]; rows = np.where(a.sum(1) > 0)[0]
    top, bot = rows.min(), rows.max()
    # ระยะในแนวตั้งของแต่ละคอลัมน์ → ไล่หลังเข้ม-ท้องอ่อน
    colTop = np.argmax(a > 0.5, axis=0).astype(np.float32); colBot = (h - 1 - np.argmax(a[::-1] > 0.5, axis=0)).astype(np.float32)
    span = np.maximum(colBot - colTop, 1)
    v = np.clip((yy - colTop[None, :]) / span[None, :], 0, 1)
    b = np.array(base, np.float32); be = np.array(belly, np.float32)
    col = b[None, None, :] * (1 - v[..., None] ** 1.4) + be[None, None, :] * (v[..., None] ** 1.4)
    edge = gaussian_filter(a, 6 * S)
    col *= (0.55 + 0.45 * np.clip(edge * 1.6, 0, 1))[..., None]                        # ขอบมืดให้ดูมีมิติ
    rng = np.random.default_rng(seed)
    if spots:
        n = np.zeros((h, w), np.float32)
        for _ in range(spots[1]):
            cx, cy, r = rng.random() * w, rng.random() * h, (1.5 + rng.random() * spots[2]) * S
            n += np.clip(1 - np.hypot(xx - cx, yy - cy) / r, 0, 1)
        n = np.clip(n, 0, 1) * (1 - v * 0.7)
        sc = np.array(spots[0], np.float32); col = col * (1 - n[..., None] * 0.8) + sc * n[..., None] * 0.8
    if stripes:
        st = (np.sin(xx / (stripes * S) + np.sin(yy / (9 * S)) * 1.4) > 0.55) * (1 - v) * 0.5
        col *= (1 - st[..., None] * 0.6)
    hl = np.exp(-((v - 0.22) / 0.08) ** 2) * 0.25 * a                                    # แสงสะท้อนเป็นแถบบนหลัง
    col += hl[..., None] * 255
    if glow is not None: col = col * 0.85 + np.array(glow, np.float32) * 0.15
    out = np.dstack([np.clip(col, 0, 255), a * 255]).astype(np.uint8)
    return Image.fromarray(out, 'RGBA')

def finish(img, name):
    img = img.resize((img.width // S, img.height // S), Image.LANCZOS)
    bb = img.getbbox(); img = img.crop((max(0, bb[0] - 6), max(0, bb[1] - 6), bb[2] + 6, bb[3] + 6))
    k = 512 / img.width; img = img.resize((512, int(img.height * k)), Image.LANCZOS)
    img.save(f'{OUT}/{name}.png'); print(name, img.size)

def arm(d, x0, y0, length, ang, w0, wave, seed, club=False):
    rng = random.Random(seed); pts = []
    for i in range(30):
        t = i / 29; a = ang + math.sin(t * 5 + seed) * wave * t
        x0 += math.cos(a) * length / 30; y0 += math.sin(a) * length / 30; pts.append((x0, y0, w0 * (1 - t * 0.85)))
    for (xa, ya, wa), (xb, yb, wb) in zip(pts, pts[1:]):
        d.line([(xa, ya), (xb, yb)], fill=(255, 255, 255, 255), width=max(2, int(wa)))
        d.ellipse([xb - wb / 2, yb - wb / 2, xb + wb / 2, yb + wb / 2], fill=(255, 255, 255, 255))
    if club: xb, yb, _ = pts[-1]; d.ellipse([xb - 9 * S, yb - 5 * S, xb + 9 * S, yb + 5 * S], fill=(255, 255, 255, 255))

def eye(img, x, y, r, iris=(30, 24, 20)):
    d = ImageDraw.Draw(img)
    d.ellipse([x - r, y - r, x + r, y + r], fill=(220, 214, 190, 255)); d.ellipse([x - r * 0.62, y - r * 0.62, x + r * 0.62, y + r * 0.62], fill=iris + (255,))
    d.ellipse([x - r * 0.5, y - r * 0.55, x - r * 0.1, y - r * 0.15], fill=(255, 255, 255, 230))

def squid(name, W=560, H=200, base=(200, 150, 140), belly=(245, 222, 214), spot=(150, 60, 50), seed=1, giant=False):
    img = canvas(W, H); d = ImageDraw.Draw(img); cy = H * S * 0.45
    mx0, mx1, mh = 20 * S, 300 * S, (46 if not giant else 52) * S
    body = [(mx0 + (mx1 - mx0) * t, cy - mh * math.sin(math.pi * (0.08 + 0.92 * t) ** 0.6 * 0.5)) for t in np.linspace(0, 1, 40)]
    body += [(mx0 + (mx1 - mx0) * t, cy + mh * 0.9 * math.sin(math.pi * (0.08 + 0.92 * t) ** 0.6 * 0.5)) for t in np.linspace(1, 0, 40)]
    d.polygon(body, fill=(255, 255, 255, 255))
    fin = 36 * S if not giant else 24 * S                                                    # ครีบรอบลำตัว (หมึกกล้วยครีบกว้างยาวตลอดตัว)
    d.polygon([(mx0 + 10 * S, cy), (mx0 + 120 * S, cy - mh - fin * 0.6), (mx1 - 30 * S, cy - mh * 0.95), (mx1 - 30 * S, cy + mh * 0.85), (mx0 + 120 * S, cy + mh * 0.9 + fin * 0.6)], fill=(255, 255, 255, 170))
    d.ellipse([mx1 - 14 * S, cy - 34 * S, mx1 + 46 * S, cy + 30 * S], fill=(255, 255, 255, 255))   # หัว
    for k in range(8): arm(d, mx1 + 36 * S, cy - 14 * S + k * 4 * S, (110 + (k % 3) * 18) * S, (k - 3.5) * 0.09, 12 * S, 0.5, seed + k)
    for k in range(2): arm(d, mx1 + 36 * S, cy - 4 * S + k * 8 * S, 190 * S, (-0.12 if k == 0 else 0.14), 6 * S, 0.7, seed + 20 + k, club=True)
    img = shade(img, base, belly, (spot, 900 if not giant else 600, 3.5), seed, glow=(255, 120, 90) if giant else None)
    eye(img, mx1 + 14 * S, cy - 6 * S, 13 * S)
    finish(img, name)

def cuttle(name):
    W, H = 460, 220; img = canvas(W, H); d = ImageDraw.Draw(img); cy = H * S * 0.48
    d.ellipse([20 * S, cy - 74 * S, 300 * S, cy + 70 * S], fill=(255, 255, 255, 180))           # ระบายครีบรอบตัว
    d.ellipse([34 * S, cy - 60 * S, 290 * S, cy + 56 * S], fill=(255, 255, 255, 255))
    d.ellipse([268 * S, cy - 34 * S, 336 * S, cy + 30 * S], fill=(255, 255, 255, 255))
    for k in range(8): arm(d, 326 * S, cy - 14 * S + k * 4 * S, (70 + (k % 3) * 10) * S, (k - 3.5) * 0.14, 12 * S, 0.4, 40 + k)
    img = shade(img, (150, 120, 90), (232, 214, 190), ((70, 50, 40), 500, 3), 7, stripes=7)
    eye(img, 302 * S, cy - 10 * S, 12 * S, iris=(60, 60, 30))
    finish(img, name)

def octopus(name):
    W, H = 500, 300; img = canvas(W, H); d = ImageDraw.Draw(img); cy = H * S * 0.42
    d.ellipse([20 * S, cy - 70 * S, 190 * S, cy + 60 * S], fill=(255, 255, 255, 255))           # หัวโป่ง
    d.ellipse([160 * S, cy - 30 * S, 230 * S, cy + 40 * S], fill=(255, 255, 255, 255))
    rng = random.Random(3)
    for k in range(8):
        arm(d, 200 * S, cy + (k - 3.5) * 8 * S, (180 + rng.random() * 70) * S, (k - 3.5) * 0.22, 22 * S, 1.6 if k % 2 else -1.4, 60 + k)
    img = shade(img, (170, 70, 52), (236, 170, 140), ((110, 34, 26), 700, 4), 11)
    dd = ImageDraw.Draw(img)                                                                 # ปุ่มดูดตามแขน
    eye(img, 176 * S, cy - 8 * S, 12 * S, iris=(70, 50, 20))
    finish(img, name)

def barracuda(name):
    W, H = 620, 150; img = canvas(W, H); d = ImageDraw.Draw(img); cy = H * S * 0.5
    L0, L1, hh = 14 * S, 520 * S, 34 * S
    top = [(L0 + (L1 - L0) * t, cy - hh * max(0.28, math.sin(math.pi * min(1, t * 1.25) ** 0.7) * (1 - t * 0.45))) for t in np.linspace(0, 1, 50)]
    bot = [(L0 + (L1 - L0) * t, cy + hh * 0.9 * max(0.28, math.sin(math.pi * min(1, t * 1.25) ** 0.7) * (1 - t * 0.45))) for t in np.linspace(1, 0, 50)]
    d.polygon(top + bot, fill=(255, 255, 255, 255))
    d.polygon([(L1 - 20 * S, cy - 9 * S), (L1 + 80 * S, cy - 50 * S), (L1 + 50 * S, cy), (L1 + 80 * S, cy + 50 * S), (L1 - 20 * S, cy + 9 * S)], fill=(255, 255, 255, 255))   # หางแฉก
    for x, s in ((230, -1), (380, -1), (390, 1)): d.polygon([(x * S, cy + s * 26 * S), ((x + 26) * S, cy + s * 56 * S), ((x + 44) * S, cy + s * 20 * S)], fill=(255, 255, 255, 255))
    img = shade(img, (90, 110, 120), (236, 238, 232), seed=5, stripes=0)
    a = np.asarray(img).astype(np.float32); yy, xx = np.mgrid[0:a.shape[0], 0:a.shape[1]]
    bars = ((np.sin(xx / (22 * S)) > 0.6) & (yy < cy) & (xx > 120 * S) & (xx < 480 * S)) * 0.35        # แถบดำพาดหลัง
    a[..., :3] *= (1 - bars[..., None]); a[..., 0] = np.where((xx > L1 - 10 * S) & (a[..., 3] > 0), a[..., 0] * 0.6 + 230 * 0.4, a[..., 0])   # หางเหลือง
    a[..., 1] = np.where((xx > L1 - 10 * S) & (a[..., 3] > 0), a[..., 1] * 0.6 + 200 * 0.4, a[..., 1])
    img = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), 'RGBA'); d = ImageDraw.Draw(img)
    d.line([(L0 + 4 * S, cy + 2 * S), (L0 + 70 * S, cy + 8 * S)], fill=(40, 40, 40, 220), width=3 * S)   # ปากยาว
    eye(img, L0 + 58 * S, cy - 8 * S, 9 * S, iris=(30, 30, 20))
    finish(img, name)

squid('muekkluay', seed=1)
cuttle('muekkradong')
octopus('mueksai')
barracuda('plasak')
squid('muekyak', base=(170, 60, 50), belly=(230, 160, 140), spot=(110, 20, 16), seed=9, giant=True)
