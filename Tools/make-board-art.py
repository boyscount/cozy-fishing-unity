# วาดกระดานไม้หน้าเลือกที่ตกปลา — python tools/make-board-art.py
# กระดานไม้ขอบกรอบ ตะปูมุม · คานป้ายหัว · กระดาษโน้ต · การ์ดกระดาษขอบฉีก/ไหม้/คราบน้ำ · เทปกาว · ป้ายไม้สลักนูน+เชือก · เชือกลูกรอก · ลูกศรไม้ไผ่
import importlib.util, math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy.ndimage import gaussian_filter, distance_transform_edt

spec = importlib.util.spec_from_file_location('tp', 'tools/make-title-props.py')
# ใช้ฟังก์ชันจากสคริปต์ป้ายหน้าแรก (ไม่รันส่วนบันทึกไฟล์ซ้ำ)
src = open('tools/make-title-props.py', encoding='utf-8').read().split("\nplank(900, 210, 1)")[0]
tp = {}; exec(compile(src, 'make-title-props', 'exec'), tp)
noise, rgba, shadow, plank, paper = tp['noise'], tp['rgba'], tp['shadow'], tp['plank'], tp['paper']
OUT = tp['OUT']

def wood_tex(H, W, seed, tone, vertical=False, freq=0.3):
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    if vertical: yy, xx = xx, yy
    warp = noise(H, W, 120, seed) * 30 + noise(H, W, 30, seed + 1) * 6
    g = np.sin((yy + warp) * freq) * 0.5 + 0.5
    st = gaussian_filter(np.random.default_rng(seed).random((H, W)), (18, 0.6) if vertical else (0.6, 18)) * 4 - 1.5
    lum = 0.74 + 0.14 * g + 0.25 * (st - 0.5) - 0.2 * noise(H, W, 80, seed + 3)
    return np.dstack([lum * tone[0], lum * tone[1], lum * tone[2]])

def board():
    """กระดานไม้แผ่นใหญ่: ไม้แนวตั้งหลายแผ่น + กรอบไม้หนา + ตะปูมุม (9 ส่วน slice 96)"""
    S = 1024; F = 70
    col = np.zeros((S, S, 3))
    x = 0; i = 0; rng = random.Random(4)
    while x < S:   # แผ่นไม้แนวตั้ง
        w = rng.randint(150, 220); t = wood_tex(S, w, 30 + i, (120, 82, 46), True, 0.25) * rng.uniform(0.85, 1.05)
        t[:, :3] *= 0.6; t[:, -3:] *= 0.55   # ร่องระหว่างแผ่น
        col[:, x:x + w] = t[:, :min(w, S - x)]; x += w; i += 1
    yy, xx = np.mgrid[0:S, 0:S]
    # กรอบไม้แนวนอน/ตั้ง
    top = wood_tex(F, S, 50, (110, 72, 40)); col[:F] = top; col[-F:] = wood_tex(F, S, 51, (104, 68, 38))
    side = wood_tex(S, F, 52, (108, 70, 38), True); col[:, :F] = np.where((yy[:, :F] > F) & (yy[:, :F] < S - F), 1, 0)[..., None] * side + np.where((yy[:, :F] > F) & (yy[:, :F] < S - F), 0, 1)[..., None] * col[:, :F]
    side2 = wood_tex(S, F, 53, (108, 70, 38), True); m = ((yy[:, -F:] > F) & (yy[:, -F:] < S - F))[..., None]; col[:, -F:] = m * side2 + (~m) * col[:, -F:]
    # เงาด้านในกรอบ
    inner = np.minimum(np.minimum(xx - F, S - F - xx), np.minimum(yy - F, S - F - yy)).astype(np.float32)
    col *= np.where(inner > 0, 0.55 + 0.45 * np.clip(inner / 40, 0, 1), 1)[..., None]
    edge = np.minimum(np.minimum(xx, S - 1 - xx), np.minimum(yy, S - 1 - yy)).astype(np.float32)
    col *= (0.6 + 0.4 * np.clip(edge / 12, 0, 1))[..., None]
    stain = np.clip(noise(S, S, 140, 7) - 0.55, 0, 1) * 1.8
    col = col * (1 - stain[..., None] * 0.35)
    a = np.full((S, S), 255.0)
    img = rgba(col, a); d = ImageDraw.Draw(img)
    for cx, cy in ((F / 2, F / 2), (S - F / 2, F / 2), (F / 2, S - F / 2), (S - F / 2, S - F / 2)):
        r = 11
        d.ellipse([cx - r - 1, cy - r + 2, cx + r + 1, cy + r + 3], fill=(20, 12, 6, 220)); d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(92, 58, 36, 255))
        d.ellipse([cx - 6, cy - 7, cx + 2, cy - 1], fill=(170, 120, 80, 255))
    return img

def note():
    """กระดาษโน้ตขอบฉีก (slice 30)"""
    p = paper(512, 150, 61, (230, 214, 172)); return shadow_pad(p, 12)

def shadow_pad(p, pad):
    big = Image.new('RGBA', (p.width + 2 * pad, p.height + 2 * pad), (0, 0, 0, 0)); big.alpha_composite(p, (pad, pad))
    return shadow(big, (3, 5), 4, 0.45)

def card():
    """การ์ดด่าน: กระดาษข่อยขอบฉีก คราบน้ำจาง มุมไหม้ (slice 48)"""
    W, H = 640, 300; p = paper(W, H, 71, (226, 206, 160))
    a = np.asarray(p).astype(np.float32)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    for cx, cy, r in ((W * 0.72, H * 0.3, 70), (W * 0.18, H * 0.75, 50)):   # วงคราบน้ำ
        d = np.hypot(xx - cx, (yy - cy) * 1.2); ring = np.exp(-((d - r) / 5) ** 2) * 0.1 + np.clip(1 - d / r, 0, 1) * 0.05
        a[..., :3] *= (1 - ring)[..., None]
    for cx, cy in ((W - 1, 0), (0, H - 1)):   # มุมไหม้เกรียม
        d = np.hypot(xx - cx, yy - cy) + noise(H, W, 14, 72) * 30
        burn = np.clip(1 - (d - 30) / 40, 0, 1); a[..., :3] *= (1 - burn * 0.75)[..., None]
        a[..., 3] *= np.clip((d - 18) / 8, 0, 1)
    p = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), 'RGBA')
    return shadow_pad(p, 14)

def tape():
    W, H = 150, 46; n = noise(H, W, 10, 81)
    col = np.dstack([180 + 30 * n, 150 + 24 * n, 104 + 18 * n]); a = np.full((H, W), 200.0)
    xx = np.arange(W)
    for side in (0, 1):   # ปลายเทปฉีกหยัก
        jag = (np.sin(np.arange(H) * 1.7) * 3 + np.random.default_rng(side).random(H) * 4).astype(int)
        for y in range(H):
            if side == 0: a[y, :jag[y] + 2] = 0
            else: a[y, W - jag[y] - 2:] = 0
    return rgba(col, a)

def go_sign():
    """ป้ายไม้สลักนูน ผูกเชือกป่าน + ลูกศรชี้ทางด้านขวา"""
    p = plank(380, 110, 91, tone=(150, 100, 56), ropes=(False, False), nails=False)
    d = ImageDraw.Draw(p); W, H = p.size
    # ขอบนูน: เส้นสว่างบน เส้นเข้มล่าง
    d.rounded_rectangle([58, 54, W - 58, H - 58], 10, outline=(80, 50, 24, 200), width=3)
    d.line([(62, 56), (W - 62, 56)], fill=(220, 180, 120, 120), width=2)
    # ลูกศรแกะ
    ax = W - 96; ay = H // 2 - 2
    d.polygon([(ax, ay - 18), (ax + 26, ay), (ax, ay + 18)], fill=(70, 40, 18, 255)); d.polygon([(ax + 2, ay - 14), (ax + 22, ay), (ax + 2, ay + 14)], fill=(120, 76, 38, 255))
    # เชือกป่านโยงจากมุมบนขึ้นไป
    for x0 in (90, W - 90):
        d.line([(x0, 0), (x0, 58)], fill=(60, 40, 20, 255), width=7); d.line([(x0, 0), (x0, 58)], fill=(176, 140, 90, 255), width=4)
        d.ellipse([x0 - 8, 50, x0 + 8, 66], fill=(150, 116, 70, 255), outline=(70, 46, 22, 255), width=2)
    return p

def rope_v():
    """เชือกถักแนวตั้ง + ลูกรอกไม้บน/ล่าง (ยืดตามความสูง)"""
    W, H = 72, 1024; img = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    for y in range(70, H - 70, 14):
        d.ellipse([W / 2 - 9, y - 9, W / 2 + 9, y + 9], fill=(70, 48, 26, 255))
    for y in range(70, H - 70, 14):
        c = 166 + (y // 14 % 3) * 8
        d.ellipse([W / 2 - 7, y - 8, W / 2 + 7, y + 6], fill=(c, int(c * 0.8), int(c * 0.52), 255)); d.line([(W / 2 - 5, y - 4), (W / 2 + 5, y + 3)], fill=(110, 80, 46, 255), width=2)
    for cy in (40, H - 40):   # ลูกรอก
        d.ellipse([W / 2 - 30, cy - 30, W / 2 + 30, cy + 30], fill=(54, 34, 16, 255)); d.ellipse([W / 2 - 25, cy - 25, W / 2 + 25, cy + 25], fill=(140, 96, 54, 255))
        d.ellipse([W / 2 - 9, cy - 9, W / 2 + 9, cy + 9], fill=(80, 80, 84, 255)); d.arc([W / 2 - 22, cy - 22, W / 2 + 22, cy + 22], 200, 260, fill=(200, 160, 110, 255), width=3)
    return img

def bamboo_arrow():
    """ลูกศรหัวไม้ไผ่ (ชี้ขึ้น · หมุนเอาเองตอนใช้ชี้ลง)"""
    S = 128; img = Image.new('RGBA', (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    d.polygon([(64, 10), (112, 70), (84, 70), (84, 118), (44, 118), (44, 70), (16, 70)], fill=(70, 70, 30, 255))
    d.polygon([(64, 18), (104, 66), (80, 66), (80, 114), (48, 114), (48, 66), (24, 66)], fill=(176, 160, 84, 255))
    d.line([(48, 92), (80, 92)], fill=(110, 96, 40, 255), width=4); d.line([(58, 30), (40, 58)], fill=(230, 220, 150, 255), width=3)
    return shadow(img, (2, 4), 3, 0.5)

board().save(f'{OUT}/board.png')
plank(860, 120, 21, tone=(166, 116, 66), ropes=(False, False), nails=True).save(f'{OUT}/beam.png')
note().save(f'{OUT}/note.png')
card().save(f'{OUT}/card.png')
tape().save(f'{OUT}/tape.png')
go_sign().save(f'{OUT}/go_sign.png')
rope_v().save(f'{OUT}/rope_v.png')
bamboo_arrow().save(f'{OUT}/bamboo_arrow.png')
print('ok')
