# วาดหน้าเช้าแบบสมุดบันทึกชาวประมง — python tools/make-journal-art.py
# หน้ากระดาษสมุดเย็บเชือก (ปกหนังซ้าย) · ประกาศกระดาษฉีก (สะอาด/เปื้อนคราบ) · ป้ายหมึกปั๊ม · สเก็ตช์ปลาลายเส้น · มือชี้ · ปากกาคอแร้ง · ไฟแช็กทองเหลือง · รูปสเก็ตช์ถ้ำหนีบคลิป
#   หลักการ: ประกาศ = กระดาษแบนติดหมุด/เทป ไม่มีขอบนูน · ปุ่ม = แผ่นไม้หนามีเงา (มีอยู่แล้ว)
import importlib.util, math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy.ndimage import gaussian_filter

src = open('tools/make-title-props.py', encoding='utf-8').read().split("\nplank(900, 210, 1)")[0]
tp = {}; exec(compile(src, 'make-title-props', 'exec'), tp)
noise, rgba, shadow, paper = tp['noise'], tp['rgba'], tp['shadow'], tp['paper']
OUT = 'Assets/Game/Resources/UI/Journal'
import os; os.makedirs(OUT, exist_ok=True)
INK = (62, 40, 22)

def save(img, name): img.save(f'{OUT}/{name}.png'); print(name, img.size)

def shadow_pad(p, pad, off=(3, 5), blur=4, op=0.45):
    big = Image.new('RGBA', (p.width + 2 * pad, p.height + 2 * pad), (0, 0, 0, 0)); big.alpha_composite(p, (pad, pad))
    return shadow(big, off, blur, op)

def page():
    """หน้ากระดาษสมุดสีน้ำตาลอ่อน คราบชา มุมเปื้อน · ซ้าย = สันปกหนัง + เชือกเย็บ (slice ซ้าย 96 · อื่น ๆ 60)"""
    W, H = 768, 1024; L = 70
    n = noise(H, W, 60, 3) * 0.6 + noise(H, W, 8, 4, 2) * 0.4
    col = np.dstack([224 + 0 * n, 206 + 0 * n, 166 + 0 * n]) * (0.86 + 0.2 * n[..., None])
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    edge = np.minimum(np.minimum(xx - L, W - 1 - xx), np.minimum(yy, H - 1 - yy))
    col *= (0.66 + 0.34 * np.clip(edge / 46, 0, 1))[..., None]                       # ขอบกระดาษหม่นลง
    for cx, cy, r in ((W * 0.8, H * 0.12, 70), (W * 0.25, H * 0.85, 90)):            # วงคราบแก้วชา
        d = np.hypot(xx - cx, (yy - cy) * 1.1); ring = np.exp(-((d - r) / 4) ** 2) * 0.12 + np.clip(1 - d / r, 0, 1) * 0.04
        col *= (1 - ring)[..., None]
    a = np.where(xx >= L, 255.0, 0)
    a *= np.clip((edge + 3 - noise(H, W, 6, 7) * 5), 0, 1)                            # ขอบฉีกเล็กน้อย
    img = rgba(col, a)
    # สันปกหนังสีน้ำตาลเข้ม ซ้าย
    lc = noise(H, L + 20, 10, 9) * 0.5 + 0.5
    leather = np.dstack([92 * lc, 56 * lc, 34 * lc]); la = np.full((H, L + 20), 255.0)
    la[:, L + 8:] = np.linspace(255, 0, 12)[None, :]
    img.alpha_composite(rgba(leather, la), (0, 0))
    d = ImageDraw.Draw(img)
    for y in range(40, H - 20, 64):                                                  # เชือกเย็บพันขอบ
        d.line([(L - 30, y), (L + 18, y + 10)], fill=(40, 26, 14, 255), width=9)
        d.line([(L - 30, y), (L + 18, y + 10)], fill=(178, 146, 98, 255), width=5)
        d.ellipse([L + 10, y + 4, L + 22, y + 16], fill=(70, 46, 24, 255))
    return img

def torn_note(w, h, seed, stains=False, tone=(234, 218, 182)):
    """ประกาศกระดาษขอบฉีกหยัก (slice 36) · stains = คราบเลือดแห้ง/สนิม (ประกาศเทศกาลที่ถ้ำ)"""
    p = np.asarray(paper(w, h, seed, tone)).astype(np.float32)
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    rng = np.random.default_rng(seed)
    jag = np.abs(np.sin(xx[0] * 0.21) * 3 + rng.random(w) * 6)                        # ขอบล่าง/บนฉีกหยักมากขึ้น
    for x in range(w):
        p[: int(jag[x] * 0.6), x, 3] = 0; p[h - int(jag[w - 1 - x]):, x, 3] = 0
    if stains:
        for _ in range(9):
            cx, cy, r = rng.random() * w, rng.random() * h, 6 + rng.random() * 22
            d = np.hypot(xx - cx, yy - cy) + noise(h, w, 6, seed + _) * 8
            s = np.clip(1 - d / r, 0, 1) ** 0.7 * 0.6
            p[..., 0] = p[..., 0] * (1 - s) + 128 * s; p[..., 1] = p[..., 1] * (1 - s) + 34 * s; p[..., 2] = p[..., 2] * (1 - s) + 28 * s
        for _ in range(26):                                                               # หยดกระเซ็นเล็ก ๆ
            cx, cy, r = rng.random() * w, rng.random() * h, 1.5 + rng.random() * 3
            s = np.clip(1 - np.hypot(xx - cx, yy - cy) / r, 0, 1) * 0.7
            p[..., 0] = p[..., 0] * (1 - s) + 120 * s; p[..., 1] = p[..., 1] * (1 - s) + 30 * s; p[..., 2] = p[..., 2] * (1 - s) + 26 * s
    return shadow_pad(Image.fromarray(np.clip(p, 0, 255).astype(np.uint8), 'RGBA'), 12)

def ink_label(w, h, seed, ink):
    """ป้ายกระดาษเล็ก กรอบหมึกวาดมือสองเส้น (slice 28) — ข้อมูลสถานะ ไม่ใช่ปุ่ม"""
    p = paper(w, h, seed, (236, 222, 190)); d = ImageDraw.Draw(p); rng = random.Random(seed)
    for k, inset in enumerate((7, 12)):
        pts = []
        for x0, y0, x1, y1 in ((inset, inset, w - inset, inset), (w - inset, inset, w - inset, h - inset), (w - inset, h - inset, inset, h - inset), (inset, h - inset, inset, inset)):
            for t in np.linspace(0, 1, 18): pts.append((x0 + (x1 - x0) * t + rng.uniform(-1.2, 1.2), y0 + (y1 - y0) * t + rng.uniform(-1.2, 1.2)))
        d.line(pts + [pts[0]], fill=ink + (230 if k == 0 else 150,), width=3 if k == 0 else 2)
    return shadow_pad(p, 8, (2, 3), 3, 0.35)

def fish_sketch(seed, w=300, h=150, deep=0.42, tail=0.22):
    """สเก็ตช์ปลาลายเส้นหมึกซีเปีย: โครงตัว ครีบ ตา เกล็ดขีดเส้น"""
    S = 2; img = Image.new('RGBA', (w * S, h * S), (0, 0, 0, 0)); d = ImageDraw.Draw(img); rng = random.Random(seed)
    cx, cy, L = w * S * 0.46, h * S * 0.5, w * S * 0.7
    top = [(cx - L / 2 + L * t, cy - math.sin(math.pi * t ** 0.8) * L * deep * 0.5) for t in np.linspace(0, 1, 40)]
    bot = [(cx - L / 2 + L * t, cy + math.sin(math.pi * t ** 0.85) * L * deep * 0.42) for t in np.linspace(0, 1, 40)]
    col = INK + (210,)
    for pts in (top, bot): d.line(pts, fill=col, width=3 * S // 2, joint='curve')
    tx = cx + L / 2; d.line([(tx, cy), (tx + L * tail, cy - L * tail * 0.8), (tx + L * tail * 0.8, cy), (tx + L * tail, cy + L * tail * 0.8), (tx, cy)], fill=col, width=3 * S // 2)
    d.line([(cx - L * 0.05, cy - L * deep * 0.48), (cx + L * 0.12, cy - L * deep * 0.78), (cx + L * 0.25, cy - L * deep * 0.42)], fill=col, width=2 * S)   # ครีบหลัง
    d.ellipse([cx - L * 0.38 - 6 * S, cy - L * 0.08 - 6 * S, cx - L * 0.38 + 6 * S, cy - L * 0.08 + 6 * S], outline=col, width=2 * S)
    d.arc([cx - L * 0.36, cy - L * deep * 0.4, cx - L * 0.18, cy + L * deep * 0.38], 300, 60, fill=col, width=2 * S)       # แก้ม
    for i in range(30):                                                               # เกล็ดขีด
        x = cx - L * 0.15 + rng.random() * L * 0.55; y = cy + (rng.random() - 0.5) * L * deep * 0.6
        d.arc([x, y, x + 9 * S, y + 7 * S], 200, 340, fill=INK + (140,), width=S)
    img = img.resize((w, h), Image.LANCZOS)
    return img.rotate(rng.uniform(-8, 8), Image.BICUBIC, expand=True)

def hand_sketch():
    """มือชี้ลายเส้น (สเก็ตช์ขอบหน้า)"""
    S = 2; W, H = 200, 120; img = Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0)); d = ImageDraw.Draw(img); col = INK + (190,)
    d.line([(10 * S, 80 * S), (70 * S, 62 * S), (150 * S, 48 * S), (190 * S, 44 * S), (186 * S, 56 * S), (120 * S, 66 * S)], fill=col, width=3 * S, joint='curve')
    d.line([(10 * S, 100 * S), (70 * S, 96 * S), (110 * S, 84 * S), (120 * S, 66 * S)], fill=col, width=3 * S, joint='curve')
    for k in range(3): d.arc([(80 + k * 14) * S, 70 * S, (100 + k * 14) * S, 96 * S], 0, 180, fill=col, width=2 * S)
    return img.resize((W, H), Image.LANCZOS)

def pen():
    """ปากกาคอแร้งหมึกซึม: ด้ามดำเงา แหวนทอง ปลายปากกาทอง (แนวตั้งเอียง)"""
    W, H = 120, 520; img = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    d.rounded_rectangle([42, 20, 78, 380], 16, fill=(28, 24, 22, 255)); d.line([(50, 30), (50, 370)], fill=(110, 100, 90, 160), width=4)
    for y in (120, 300, 380): d.rectangle([40, y, 80, y + 10], fill=(196, 160, 80, 255))
    d.polygon([(44, 390), (76, 390), (66, 470), (60, 500), (54, 470)], fill=(206, 170, 90, 255)); d.line([(60, 400), (60, 490)], fill=(90, 70, 30, 255), width=2)
    d.rectangle([56, 0, 64, 24], fill=(196, 160, 80, 255))
    img = img.rotate(-12, Image.BICUBIC, expand=True)
    return shadow_pad(img, 14, (6, 8), 6, 0.5)

def lighter():
    """ไฟแช็กทองเหลืองเก่า"""
    W, H = 110, 220; n = noise(H, W, 10, 31)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    shade = 0.7 + 0.45 * np.cos((xx / W - 0.35) * 3.0) * 0.5 + 0.15 * n
    col = np.dstack([200 * shade, 160 * shade, 80 * shade]); a = np.full((H, W), 255.0)
    img = rgba(col, a); d = ImageDraw.Draw(img)
    d.line([(4, 64), (W - 4, 64)], fill=(110, 80, 30, 255), width=3)
    for x in range(14, W - 10, 14): d.ellipse([x, 22, x + 8, 30], fill=(90, 64, 26, 255))
    mask = Image.new('L', (W, H), 0); ImageDraw.Draw(mask).rounded_rectangle([0, 0, W - 1, H - 1], 14, fill=255); img.putalpha(mask)
    img = img.rotate(14, Image.BICUBIC, expand=True)
    return shadow_pad(img, 14, (6, 8), 6, 0.5)

def cave_polaroid():
    """รูปสเก็ตช์ปากถ้ำบนกระดาษ หนีบด้วยคลิปดำ (คู่กับประกาศเทศกาล)"""
    W, H = 260, 300; p = np.asarray(paper(W, H, 41, (238, 228, 206))).astype(np.float32)
    img = Image.fromarray(p.astype(np.uint8), 'RGBA'); d = ImageDraw.Draw(img); col = INK + (220,)
    d.polygon([(30, 230), (60, 120), (110, 70), (170, 80), (220, 140), (236, 230)], outline=col, width=3)
    d.polygon([(90, 230), (100, 160), (130, 130), (165, 150), (180, 230)], fill=(52, 36, 24, 230))
    rng = random.Random(4)
    for i in range(40): x = 40 + rng.random() * 190; y = 90 + rng.random() * 130; d.line([(x, y), (x + rng.uniform(-14, 14), y + rng.uniform(4, 14))], fill=INK + (120,), width=2)
    for i in range(10): x, y = rng.random() * W, 180 + rng.random() * 110; r = 2 + rng.random() * 6; d.ellipse([x - r, y - r, x + r, y + r], fill=(128, 34, 28, 160))
    img = img.rotate(-6, Image.BICUBIC, expand=True)
    big = Image.new('RGBA', (img.width + 30, img.height + 60), (0, 0, 0, 0)); big.alpha_composite(shadow_pad(img, 10), (10, 40))
    d = ImageDraw.Draw(big); cx = big.width // 2 + 30                                  # คลิปหนีบกระดาษดำ
    d.polygon([(cx - 34, 46), (cx + 34, 46), (cx + 26, 78), (cx - 26, 78)], fill=(30, 30, 32, 255))
    for s in (-1, 1): d.line([(cx + s * 22, 48), (cx + s * 30, 6), (cx + s * 8, 6)], fill=(170, 170, 176, 255), width=5)
    return big

def stamp_shop():
    """ตราปั๊มหมึกแดงเอียง 'ร้านลุงส้ม' (กรอบอย่างเดียว ข้อความใส่ในเกม)"""
    W, H = 200, 84; img = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img); rng = random.Random(9)
    for k in range(2): d.rounded_rectangle([4 + k * 6, 4 + k * 6, W - 5 - k * 6, H - 5 - k * 6], 10, outline=(170, 34, 26, 200 - k * 60), width=4 - k)
    a = np.asarray(img).astype(np.float32); a[..., 3] *= np.clip(0.5 + noise(H, W, 4, 3), 0, 1)
    return Image.fromarray(a.astype(np.uint8), 'RGBA').rotate(-8, Image.BICUBIC, expand=True)

save(page(), 'page')
save(torn_note(560, 120, 11), 'note_a')
save(torn_note(560, 140, 12, tone=(240, 226, 196)), 'note_b')
save(torn_note(560, 150, 13, stains=True, tone=(240, 232, 214)), 'note_stained')
save(ink_label(380, 80, 21, (90, 56, 24)), 'label_ink')
save(ink_label(380, 80, 22, (40, 52, 110)), 'label_ink_blue')
save(fish_sketch(1, deep=0.36), 'sketch_fish_a')
save(fish_sketch(2, deep=0.62, tail=0.18), 'sketch_fish_b')
save(fish_sketch(3, deep=0.3, tail=0.25), 'sketch_fish_c')
save(hand_sketch(), 'sketch_hand')
save(pen(), 'prop_pen')
save(lighter(), 'prop_lighter')
save(cave_polaroid(), 'prop_cave')
save(stamp_shop(), 'stamp_shop')
