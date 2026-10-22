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
