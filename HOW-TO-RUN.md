# วิธีเปิด · รัน · build · ขึ้นเว็บ (ตะกร้าแห่งวันพรุ่งนี้)

โฟลเดอร์โปรเจกต์: `C:\Users\MP25BD7K\Documents\cozy-fishing-unity`
เว็บที่เล่นได้จริง: https://boyscount.github.io/cozy-fishing-unity/

---

## สิ่งที่ต้องมีในเครื่อง

| โปรแกรม | ใช้ทำอะไร | ตรวจว่ามีแล้ว |
|---|---|---|
| Unity Hub + Unity **6000.6.7f1** (มีโมดูล WebGL Build Support) | แก้เกม / build | เปิด Unity Hub → Installs |
| Python 3 | เปิดเว็บเซิร์ฟเวอร์ในเครื่อง | พิมพ์ `python --version` ใน PowerShell |
| Git | เก็บประวัติงาน / ขึ้นเว็บ | พิมพ์ `git --version` |

---

## 1. เล่นเกมในเครื่อง (localhost:5190)

> ต้อง build ก่อนอย่างน้อยหนึ่งครั้ง (ข้อ 3) ให้มีโฟลเดอร์ `Builds\WebGL`

**วิธีง่ายสุด**

1. เปิด File Explorer ไปที่โฟลเดอร์โปรเจกต์
2. ดับเบิลคลิก **`run-local.bat`**
3. เบราว์เซอร์จะเปิด http://localhost:5190/ ให้เอง
4. เล่นเสร็จ: ปิดหน้าต่างดำ (หรือกด `Ctrl+C` ในหน้าต่างนั้น)

**วิธีพิมพ์คำสั่งเอง** (PowerShell)

```powershell
cd C:\Users\MP25BD7K\Documents\cozy-fishing-unity\Builds\WebGL
python -m http.server 5190
```

แล้วเปิด http://localhost:5190/

> ทำไมต้องมีเซิร์ฟเวอร์: เกม WebGL เปิดด้วยการดับเบิลคลิก `index.html` ตรง ๆ ไม่ได้ (เบราว์เซอร์บล็อกการโหลดไฟล์) จึงใช้เว็บเซิร์ฟเวอร์เล็ก ๆ ที่มากับ Python

**ถ้าขึ้นว่าพอร์ต 5190 ถูกใช้อยู่** แปลว่ามีเซิร์ฟเวอร์เปิดค้างอยู่แล้ว — เปิด http://localhost:5190/ ได้เลย หรือเปลี่ยนเลขพอร์ต เช่น `python -m http.server 5191`

### เปิดบนมือถือ (ต่อ Wi-Fi วงเดียวกับคอม)

1. ใน PowerShell พิมพ์ `ipconfig` → ดูเลข **IPv4 Address** เช่น `192.168.1.20`
2. บนมือถือเปิด `http://192.168.1.20:5190/`
3. ถ้า Windows ถามเรื่อง Firewall → กด **Allow** (Private network)

### ทางลัดสำหรับทดสอบ (ต่อท้าย URL)

| ต่อท้าย | ผล |
|---|---|
| `?stage=mekong` | เข้าด่านนั้นทันที (`paddy` `rapids` `temple` `marsh` `houseboat` `bend` `cave` `mangrove` `headman` `reservoir` `squid` `mekong`) |
| `?bot` | ให้บอทเล่นเอง (ทดสอบ) |
| `?bot&fastcatch` | บอทเล่น + ข้ามมินิเกมดึงปลา |
| `?bot&boss&stage=mekong` | บังคับปลาบึกยักษ์โผล่ทุกครั้ง (ทดสอบบอส) |
| `?lang=en` | ภาษาอังกฤษ |
| `?online` | เชื่อมระบบออนไลน์ตอนทดสอบ (ปกติบอท/เอดิเตอร์ออฟไลน์) |

ใช้หลายอันพร้อมกันได้ เช่น `http://localhost:5190/?stage=squid&bot`

---

## 2. แก้เกมใน Unity Editor

1. เปิด **Unity Hub** → **Projects** → **Add** (หรือ Open) → เลือกโฟลเดอร์ `cozy-fishing-unity` → เวอร์ชัน `6000.6.7f1`
2. เปิดครั้งแรกจะช้า (5–15 นาที) เพราะต้องนำเข้าไฟล์ทั้งหมด
3. แถบ **Project** → `Assets/Game/Scenes` → ดับเบิลคลิกฉาก
   - `Paddy.unity` = บ้าน + บ่อทุ่งนา (ฉากเริ่มเกม)
   - ฉากอื่น = แต่ละด่าน
4. กด **▶ Play** ด้านบน เล่นใน Editor ได้ทันที

**โครงสร้างโฟลเดอร์ที่ใช้บ่อย**

| โฟลเดอร์ | เนื้อหา |
|---|---|
| `Assets/Game/Scripts` | โค้ดเกม (Core = ระบบ, Gameplay = ตกปลา, UI = หน้าจอ, World = ฉาก/บรรยากาศ) |
| `Assets/Editor` | สคริปต์สร้างด่าน (`Stage*.cs`) · build (`ProjectTools.cs`) |
| `Assets/Game/Resources/Data/species.json` | ข้อมูลปลาทุกชนิด |
| `Assets/Game/Resources/Fish` | รูปปลา + `credits.txt` (เครดิตภาพ) |
| `Tools` | สคริปต์ช่วย (วาดภาพ UI, ทดสอบอัตโนมัติ `qa/qa.mjs`) |

---

## 3. Build เกมใหม่หลังแก้งาน

> **ปิด Unity Editor ของโปรเจกต์นี้ก่อน** (เปิดค้างไว้ build ไม่ได้)

**วิธีง่ายสุด:** ดับเบิลคลิก **`build-webgl.bat`** → รอ 10–15 นาที → ขึ้น `Build Finished, Result: Success` = เสร็จ

**build แบบเร็ว (สำหรับทดสอบ ไฟล์ใหญ่กว่า):** เปิด PowerShell ในโฟลเดอร์โปรเจกต์แล้วพิมพ์

```powershell
.\build-webgl.bat fast
```

ผลลัพธ์อยู่ที่ `Builds\WebGL` → เล่นทดสอบด้วยข้อ 1

ดูความคืบหน้า / หาสาเหตุถ้า build ล้ม: เปิดไฟล์ `Logs\build_local.log` แล้วค้นคำว่า `error`

**สั่งเองแบบเต็ม (Git Bash)**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe" -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod ProjectTools.BuildWebGL -logFile Logs/build.log
```

แก้แค่ฉาก/ด่าน (ไม่แตะโค้ด) build เฉพาะไฟล์ด่านได้เร็วกว่า: `-executeMethod ProjectTools.BuildBundlesOnly`

---

## 4. ทดสอบอัตโนมัติก่อนขึ้นเว็บ

เปิดเซิร์ฟเวอร์ข้อ 1 ไว้ แล้วใน Git Bash:

```bash
QA_PAR=2 QA_STAGES=paddy,mekong node <โฟลเดอร์สกิล browser-automation>/browser.mjs about:blank --script Tools/qa/qa.mjs --timeout 1800000
```

ผล: `PASS/FAIL` ต่อด่านต่ออุปกรณ์ + ภาพหน้าจอใน `Tools/qa/out/shots`
(ไม่ระบุ `QA_STAGES` = ทดสอบทุกด่าน · `QA_DEVICES=desktop,phone,pad` เลือกขนาดจอ)

---

## 5. ขึ้นเว็บจริง (GitHub Pages)

เว็บเสิร์ฟจาก branch **`gh-pages`** ของ repo `boyscount/cozy-fishing-unity`

1. build ตัวจริงให้เสร็จ (ข้อ 3 แบบปกติ ไม่ใช่ fast)
2. ก๊อปไฟล์ใน `Builds\WebGL` (โฟลเดอร์ `Build`, `Bundles`, `TemplateData`, ไฟล์ `index.html`) ไปไว้ใน branch `gh-pages`
   - **เก็บไฟล์ใน `Build` ของรอบก่อนไว้ด้วย** (มือถือที่ยังจำหน้าเก่าในแคชจะได้เปิดไม่พัง) แล้วค่อยลบรอบที่เก่ากว่านั้น
3. commit แล้ว push ขึ้น `gh-pages`
4. รอ 1–3 นาที แล้วเปิด https://boyscount.github.io/cozy-fishing-unity/ (ปิดแท็บเดิมแล้วเปิดใหม่)

> ตอนนี้ Claude เป็นคนทำขั้นนี้ให้ — ถ้าอยากกดเองครั้งเดียวจบ ขอให้ทำ `deploy.bat` เพิ่มได้

---

## 6. ระบบออนไลน์ (Unity Cloud)

โปรเจกต์ผูกกับ Unity Cloud: org `npooseedin` · environment `production`

กระดานคะแนนที่ต้องมี (Leaderboards):

| ID | ตั้งค่า |
|---|---|
| `bigfish_<ด่าน>` ทุกด่าน (เช่น `bigfish_paddy`, `bigfish_mekong`) | Best score · เรียงมากไปน้อย (Descending) · รีเซ็ตทุกสัปดาห์ |
| `quickstrike` (กระดานมือไว) | Best score · **เรียงน้อยไปมาก (Ascending)** · รีเซ็ตทุกสัปดาห์ |

เพิ่มด่านใหม่ → ต้องสร้าง `bigfish_<id ด่าน>` ใหม่ทุกครั้ง

Cloud Save เปิดใช้แล้ว (เก็บเซฟเนื้อเรื่อง สมุดปลา คดี ภารกิจรายวัน เทศกาล บอส)

---

## ปัญหาที่เจอบ่อย

| อาการ | ทางแก้ |
|---|---|
| หน้าเว็บขาว/ค้างหน้าโหลด | ปิดแท็บแล้วเปิดใหม่ (กด `Ctrl+Shift+R`) · ดู Console ในเบราว์เซอร์ (F12) |
| build ล้ม `Scripts have compiler errors` | เปิด `Logs\build_local.log` ค้น `error CS` จะบอกไฟล์และบรรทัด |
| build ช้ามาก/ค้าง | ปิด Unity Editor ทุกหน้าต่างของโปรเจกต์นี้ก่อน · build ครั้งแรกหลังแก้ฉากจะนานกว่าปกติ |
| มือถือเปิดเว็บในเครื่องไม่ได้ | ต้องต่อ Wi-Fi วงเดียวกัน · อนุญาต Python ใน Windows Firewall |
| คะแนนไม่ขึ้นกระดาน | ตรวจว่าสร้าง Leaderboard ID ถูกต้องใน Unity Cloud แล้ว · ตอนทดสอบด้วย `?bot` ระบบออนไลน์ปิดอยู่ (ใส่ `?online`) |
