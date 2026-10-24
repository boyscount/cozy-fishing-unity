# ภาพกล่องถาม (ป้ายไม้ + ปุ่มแผ่นไม้) จากภาพตัวอย่างของผู้ใช้ — python tools/make-ask-art.py <ภาพตัวอย่าง> <Resources/UI dir>
#   ลบตัวหนังสือเดิมด้วย inpaint (เกมวาดข้อความใหม่ทับตรงนั้น) · ตัดพื้นหลังกลางคืนออกเป็นโปร่งใสด้วยสี (ไม้ = แดงมากกว่าน้ำเงิน)
import sys, os
import cv2
import numpy as np

src, out = sys.argv[1], sys.argv[2]
img = cv2.imread(src)                       # BGR 1024x572
H, W = img.shape[:2]
b, g, r = [img[:, :, i].astype(int) for i in range(3)]
lum = (r * 299 + g * 587 + b * 114) // 1000
sat = np.max(img, axis=2).astype(int) - np.min(img, axis=2).astype(int)

def inpaint(rects, cond, grow=3):
    global img
    m = np.zeros((H, W), np.uint8)
    for (x0, y0, x1, y1) in rects:
        sub = cond[y0:y1, x0:x1]
        m[y0:y1, x0:x1][sub] = 255
    m = cv2.dilate(m, np.ones((3, 3), np.uint8), iterations=grow)
    img = cv2.inpaint(img, m, 9, cv2.INPAINT_TELEA)

# ตัวหนังสือสีครีม: สว่างและสีไม่จัด
cream = (lum > 135) & (sat < 90)
inpaint([(180, 92, 870, 228)], cream, 3)                    # หัวข้อบนป้าย
inpaint([(400, 262, 680, 350)], cream, 3)                   # "กลับบ้าน"
inpaint([(300, 400, 800, 470)], (lum > 110) & (sat < 70), 3)   # "ตกต่อ (…)"
# ไอคอนบนปุ่ม: เก็บแยกไว้ก่อนลบ
house = img[248:345, 298:405].copy(); hook = img[388:468, 240:302].copy()
b0, g0, r0 = [img[:, :, i].astype(int) for i in range(3)]
inpaint([(298, 250, 405, 345)], np.ones((H, W), bool) & ~((g0 > r0 + 4) & (g0 > b0)), 1)   # ทุกอย่างที่ไม่ใช่สีเขียว (บ้าน+ตะเกียง)
inpaint([(240, 390, 302, 468)], (lum > 95), 2)                                              # ตะขอสีเทาอ่อน

def alpha_save(bgr, mask, path, feather=2):
    m = cv2.GaussianBlur(mask.astype(np.uint8) * 255, (0, 0), feather) if feather else mask.astype(np.uint8) * 255
    rgba = cv2.cvtColor(bgr, cv2.COLOR_BGR2BGRA); rgba[:, :, 3] = m
    cv2.imwrite(path, rgba)

# ---------- ป้ายไม้ทั้งแผ่น (รวมตะกร้าปลาด้านบนและเชือก) ----------
b, g, r = [img[:, :, i].astype(int) for i in range(3)]
wood = (r > b + 6) & (r + g + b > 70)
wood = cv2.morphologyEx(wood.astype(np.uint8), cv2.MORPH_CLOSE, np.ones((9, 9), np.uint8)).astype(bool)
# เติมรูข้างในป้าย: ทุกอย่างที่น้ำท่วมจากขอบภาพไม่ถึง = ป้าย
ff = (~wood).astype(np.uint8) * 255; ffm = np.zeros((H + 2, W + 2), np.uint8)
for seed in [(0, 0), (W - 1, 0), (0, H - 1), (W - 1, H - 1), (W // 2, H - 1), (60, 300), (980, 300)]:
    if ff[seed[1], seed[0]] == 255: cv2.floodFill(ff, ffm, seed, 128)
board = ff != 128
board = cv2.morphologyEx(board.astype(np.uint8), cv2.MORPH_OPEN, np.ones((5, 5), np.uint8)).astype(bool)
# ตะกร้าปลาด้านบน: ปลาสีเงินไม่ใช่สีไม้ → เอาส่วนที่สว่างพอในกรอบตะกร้ามารวมด้วย
bk = np.zeros((H, W), bool); bk[4:108, 440:598] = True
board |= bk & ((lum > 55) | wood)
def biggest(mask):
    n, lab, stats, _ = cv2.connectedComponentsWithStats(mask.astype(np.uint8), 8)
    if n <= 1: return mask
    k = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA])); return lab == k
board = biggest(board)
x0, y0, x1, y1 = 90, 0, 950, 552
alpha_save(img[y0:y1, x0:x1], board[y0:y1, x0:x1], os.path.join(os.path.dirname(os.path.abspath(__file__)), 'ref', 'ask_board_full.png'))
# ป้ายเปล่า (ไม่มีปุ่ม): ปิดแถวปุ่มด้วยแผ่นไม้จากแถวบนของป้ายเอง (ปุ่มจริงวาดทับอยู่แล้ว) แล้วเกลี่ยรอยต่อ
nb = img.copy(); patch = img[100:236, 118:830].astype(np.float32)
for top in (240, 372):
    hgt = min(patch.shape[0], 492 - top)
    dst = nb[top:top + hgt, 118:830].astype(np.float32); src = patch[:hgt]
    wgt = np.ones((hgt, 712), np.float32)
    fz = 14; ramp = np.linspace(0, 1, fz)
    wgt[:fz] *= ramp[:, None]; wgt[-fz:] *= ramp[::-1][:, None]; wgt[:, :fz] *= ramp[None, :]; wgt[:, -fz:] *= ramp[::-1][None, :]
    nb[top:top + hgt, 118:830] = (dst * (1 - wgt[..., None]) + src * wgt[..., None]).astype(np.uint8)
alpha_save(nb[y0:y1, x0:x1], board[y0:y1, x0:x1], os.path.join(out, 'Frames', 'ask_board.png'))

# ---------- ปุ่มแผ่นไม้ (ตัดตามสีของแผ่นไม้แต่ละแบบ) ----------
gb = (g > r + 2) & (g > b)                                  # แผ่นไม้เขียว
gb = cv2.morphologyEx(gb.astype(np.uint8), cv2.MORPH_CLOSE, np.ones((11, 11), np.uint8)).astype(bool)
cx0, cy0, cx1, cy1 = 205, 240, 825, 365
alpha_save(img[cy0:cy1, cx0:cx1], gb[cy0:cy1, cx0:cx1], os.path.join(out, 'Frames', 'ask_btn_main.png'), 1.2)
dk = np.zeros((H, W), bool); dk[380:488, 214:818] = True     # แผ่นไม้เข้ม: สี่เหลี่ยมขอบหยัก ตัดตามความสว่างกับพื้นป้ายรอบ ๆ
inner = (lum > 40) & (lum < 120) & (sat < 60)
dk &= cv2.morphologyEx(inner.astype(np.uint8), cv2.MORPH_CLOSE, np.ones((15, 15), np.uint8)).astype(bool)
alpha_save(img[378:490, 210:822], dk[378:490, 210:822], os.path.join(out, 'Frames', 'ask_btn_alt.png'), 1.2)

# ---------- ไอคอน ----------
hb, hg, hr = [house[:, :, i].astype(int) for i in range(3)]
alpha_save(house, ~((hg > hr + 4) & (hg > hb)), os.path.join(out, 'Icons', 'ask_home.png'), 0.8)
kl = (hook.astype(int).sum(axis=2) // 3) > 95
n, lab, stats, _ = cv2.connectedComponentsWithStats(kl.astype(np.uint8), 8)
kl = np.isin(lab, [i for i in range(1, n) if stats[i, cv2.CC_STAT_AREA] > 25])
alpha_save(hook, kl, os.path.join(out, 'Icons', 'ask_hook.png'), 0.6)
print('ok')
