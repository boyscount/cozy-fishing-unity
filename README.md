# A Basket of Tomorrows (Unity)

ตะกร้าแห่งวันพรุ่งนี้ — เกมตกปลาเลี้ยงครอบครัว + สืบคดีลึกลับและความลับของปลาในทุ่งนา

เวอร์ชัน Unity ของเกมตกปลาเลี้ยงครอบครัว — ย้ายมาจากเวอร์ชันเว็บ Three.js
(https://boyscount.github.io/home-twin/fishing/ — ยังเปิดให้เล่นต่อ)

- Unity 6.6 (6000.6.7f1) · URP · ออกเป็น **WebGL** เป็นหลัก
- โมเดลปั้นด้วย Blender 5.2 (สคริปต์อยู่ใน repo home-twin: `fishing/tools/build_assets.py`) → `Assets/Game/Models/assets.glb` นำเข้าด้วย glTFast
- ตัวหนังสือไทยใช้ UI Toolkit + Advanced Text Generator (วางสระ/วรรณยุกต์ถูกต้อง)

## สั่งงานแบบไม่เปิดหน้าต่าง (batchmode)

```bash
U="/c/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe"
"$U" -batchmode -nographics -quit -projectPath . -executeMethod SceneBuilder.CreatePaddyTest   # สร้างฉากทดสอบ
"$U" -batchmode -nographics -quit -projectPath . -buildTarget WebGL -executeMethod ProjectTools.BuildWebGL   # build → Builds/WebGL
```

## แผน

1. ตั้งโปรเจกต์ + นำเข้าโมเดล + ทดสอบภาษาไทย + build WebGL ← **กำลังทำ**
2. วงจรตกปลาหลักในด่านบ่อปลาทุ่งนา: toon shader / น้ำ (Shader Graph), ตัวละคร, จอยสัมผัส, เหวี่ยงเบ็ด, มินิเกม, ตีเบ็ดปลาหายาก
3. ระบบด่านแบบข้อมูล (ScriptableObject): ปลา · ด่าน · ราคา — เพิ่มด่านใหม่ไม่ต้องแก้โค้ด
4. ย้ายเนื้อเรื่องรายวัน บ้าน มื้อเย็น ตลาด สมุดสะสม กลางคืน ตอนจบงานวัด
5. ด่านใหม่: แม่น้ำโขง (ปลาบึก) · ตลาดน้ำ · ป่าชายเลน · ทะเลบัวแดง · อ่างเก็บน้ำบนดอย · ไดหมึกกลางคืน
