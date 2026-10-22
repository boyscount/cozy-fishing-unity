# วาดไอคอนปุ่มบน HUD (เส้นขาว พื้นโปร่งใส 128px) — python Tools/make-icons.py
from PIL import Image, ImageDraw
OUT = 'Assets/Game/Resources/UI/Icons'
S = 512; W = 30   # วาดใหญ่แล้วย่อให้ขอบเนียน
def canvas(): img = Image.new('RGBA', (S, S), (0, 0, 0, 0)); return img, ImageDraw.Draw(img)
def save(img, name): img.resize((128, 128), Image.LANCZOS).save(f'{OUT}/{name}.png')
c = (255, 247, 230, 255)

# สมุดปลา: หนังสือเปิด + ปลาบนหน้าขวา
img, d = canvas()
d.line([(256, 140), (256, 400)], fill=c, width=W)
d.polygon([(256, 140), (90, 110), (90, 370), (256, 400)], outline=c, width=W)
d.polygon([(256, 140), (422, 110), (422, 370), (256, 400)], outline=c, width=W)
d.ellipse([(292, 205), (378, 265)], fill=c)
d.polygon([(372, 235), (408, 205), (408, 265)], fill=c)
for y in (190, 240, 290): d.line([(125, y - 15), (220, y)], fill=c, width=18)
save(img, 'fishbook')

# สมุดคดี: แว่นขยาย
img, d = canvas()
d.ellipse([(110, 100), (330, 320)], outline=c, width=W + 6)
d.line([(305, 300), (410, 405)], fill=c, width=W * 2)
d.arc([(150, 140), (290, 280)], 200, 260, fill=c, width=16)
save(img, 'casebook')

# กลับบ้าน: บ้านทรงไทยหลังคาจั่ว
img, d = canvas()
d.line([(80, 270), (256, 110), (432, 270)], fill=c, width=W + 4, joint='curve')
d.line([(140, 240), (140, 410), (372, 410), (372, 240)], fill=c, width=W, joint='curve')
d.rectangle([(220, 300), (292, 410)], fill=c)
save(img, 'home')
print('ok')

# ---------- ชุดไอคอนแทนตัวหนังสือบน HUD ----------
import math
def ring(d, box, w=W): d.ellipse(box, outline=c, width=w)

# ข้อง (ตะกร้าสานปากแคบ)
img, d = canvas()
d.polygon([(130, 180), (382, 180), (350, 430), (162, 430)], outline=c, width=W)
for x in (190, 256, 322): d.line([(x, 185), (x - (x - 256) * 0.25, 425)], fill=c, width=16)
for y in (260, 340): d.line([(130 + (y - 180) * 0.13, y), (382 - (y - 180) * 0.13, y)], fill=c, width=16)
d.arc([(150, 80), (362, 280)], 200, 340, fill=c, width=W)
save(img, 'basket')
# เหรียญ
img, d = canvas()
ring(d, [(86, 86), (426, 426)], W + 6); ring(d, [(150, 150), (362, 362)], 16)
d.text((0, 0), '', fill=c)
d.line([(222, 180), (222, 332)], fill=c, width=22)                       # ฿: ตัว B + เส้นตั้ง
d.arc([(170, 180), (300, 258)], 270, 450, fill=c, width=22); d.arc([(170, 254), (312, 332)], 270, 450, fill=c, width=22)
d.line([(222, 180), (236, 180)], fill=c, width=22); d.line([(222, 256), (240, 256)], fill=c, width=22); d.line([(222, 332), (242, 332)], fill=c, width=22)
d.line([(250, 150), (250, 362)], fill=c, width=14)
save(img, 'coin')
# ดวงอาทิตย์
def sun(d, cx, cy, r, rays=True):
    d.ellipse([(cx - r, cy - r), (cx + r, cy + r)], fill=c)
    if rays:
        for k in range(8):
            a = k * math.pi / 4
            d.line([(cx + math.cos(a) * (r + 40), cy + math.sin(a) * (r + 40)), (cx + math.cos(a) * (r + 95), cy + math.sin(a) * (r + 95))], fill=c, width=W)
img, d = canvas(); sun(d, 256, 256, 90); save(img, 'sun')
# พระจันทร์เสี้ยว
img, d = canvas()
d.ellipse([(110, 100), (410, 400)], fill=c); d.ellipse([(190, 70), (470, 350)], fill=(0, 0, 0, 0))
save(img, 'moon')
# เมฆ
def cloud(d, dx=0, dy=0, s=1.0):
    for (x, y, r) in [(180, 300, 80), (270, 250, 105), (360, 305, 75)]:
        d.ellipse([(dx + x * s - r * s, dy + y * s - r * s), (dx + x * s + r * s, dy + y * s + r * s)], fill=c)
    d.rectangle([(dx + 180 * s, dy + 300 * s), (dx + 360 * s, dy + 380 * s)], fill=c)
img, d = canvas(); cloud(d); save(img, 'cloud')
img, d = canvas(); sun(d, 330, 170, 70); cloud(d, -30, 40, 0.95); save(img, 'partly')
img, d = canvas(); cloud(d, 0, -60)
for x in (180, 260, 340): d.line([(x, 360), (x - 30, 450)], fill=c, width=W)
save(img, 'rain')
# ลม
img, d = canvas()
d.arc([(250, 110), (390, 250)], 180, 450, fill=c, width=W); d.line([(90, 250), (320, 250)], fill=c, width=W)
d.arc([(280, 250), (400, 370)], 270, 450 + 90, fill=c, width=W); d.line([(110, 330), (340, 330)], fill=c, width=W)
d.line([(70, 410), (260, 410)], fill=c, width=W)
save(img, 'wind')
# ปฏิทิน (วันที่)
img, d = canvas()
d.rounded_rectangle([(100, 120), (412, 420)], 30, outline=c, width=W); d.line([(100, 200), (412, 200)], fill=c, width=W)
for x in (170, 342): d.line([(x, 80), (x, 150)], fill=c, width=W)
save(img, 'calendar')

# ปุ่มหลัก: เหวี่ยง (คันเบ็ดกับเส้นโค้ง) · กรอสาย (รอก) · เก็บสาย (ลูกศรเข้าหาตัว) · ดึง (เบ็ดตะขอ) · รอ (จุดสามจุด) · ตีเบ็ด (สายฟ้า)
img, d = canvas()
d.line([(120, 430), (360, 110)], fill=c, width=W + 4)
d.arc([(250, 80), (480, 420)], 270, 420, fill=c, width=16)
d.ellipse([(400, 380), (450, 430)], fill=c)
save(img, 'cast')
img, d = canvas()
ring(d, [(110, 110), (402, 402)], W + 6); d.ellipse([(226, 226), (286, 286)], fill=c)
d.line([(256, 256), (380, 160)], fill=c, width=W); d.ellipse([(355, 120), (425, 190)], fill=c)
save(img, 'reel')
img, d = canvas()
d.line([(256, 90), (256, 330)], fill=c, width=W + 6)
d.arc([(150, 250), (362, 460)], 0, 180, fill=c, width=W + 6)
d.line([(150, 355), (195, 300)], fill=c, width=W)
save(img, 'hook')
img, d = canvas()
for x in (140, 256, 372): d.ellipse([(x - 36, 220), (x + 36, 292)], fill=c)
save(img, 'wait')
img, d = canvas()
d.polygon([(300, 60), (140, 290), (250, 290), (200, 452), (380, 210), (270, 210)], fill=c)
save(img, 'strike')
img, d = canvas()
d.line([(256, 420), (256, 120)], fill=c, width=W + 6); d.line([(150, 230), (256, 110), (362, 230)], fill=c, width=W + 6, joint='curve')
save(img, 'reelin')
# จอยเดิน: ลูกศรสี่ทิศ
img, d = canvas()
for a in range(4):
    ang = a * math.pi / 2; cx, cy = 256 + math.cos(ang) * 150, 256 + math.sin(ang) * 150
    p = [(cx + math.cos(ang) * 60, cy + math.sin(ang) * 60), (cx + math.cos(ang + 2.3) * 60, cy + math.sin(ang + 2.3) * 60), (cx + math.cos(ang - 2.3) * 60, cy + math.sin(ang - 2.3) * 60)]
    d.polygon(p, fill=c)
save(img, 'move')
print('hud icons ok')

# มุมกล้อง: ตา (บุคคลที่หนึ่ง) · ตาขีดฆ่า (กล้องหลังตัวละคร)
def eye(d):
    d.chord([(70, 150), (442, 470)], 200, 340, outline=c, width=W)
    d.chord([(70, 40), (442, 360)], 20, 160, outline=c, width=W)
    d.ellipse([(196, 196), (316, 316)], fill=c)
img, d = canvas(); eye(d); save(img, 'eye')
img, d = canvas(); eye(d); d.line([(100, 90), (412, 422)], fill=c, width=W + 4); save(img, 'eye_off')
print('eye ok')
