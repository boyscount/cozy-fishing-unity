using UnityEngine;
using UnityEngine.InputSystem;

namespace CozyFishing
{
    // รวมอินพุตทุกแบบ: คีย์บอร์ด (WASD/ลูกศร เดิน · Space กระโดด · E/F/Enter ตกปลา) · เมาส์ (คลิกค้างบนฉาก = ตกปลา, คลิกขวาลาก = หมุนกล้อง, ล้อ = ซูม)
    // · จอสัมผัส: จอย/ปุ่มตกปลา/ปุ่มกระโดด/ลากที่ว่างเพื่อหมุนกล้อง (ตั้งค่าจาก Hud)
    public static class GameInput
    {
        public static Vector2 Joystick;          // จากจอยบนจอ (-1..1)
        public static bool ButtonHeld;           // ปุ่ม 🎣 บนจอ
        public static bool PointerOverUi;        // Hud บอกว่าเมาส์อยู่บน UI (ไม่นับเป็นการคลิกฉาก)
        public static bool Blocked;              // มีหน้าต่างเปิดอยู่ (การ์ดปลา ฯลฯ)
        public static bool JumpButton;           // ปุ่มกระโดดบนจอ (กดครั้งเดียว Hud ตั้ง true, PlayerController รับแล้วล้าง)
        public static Vector2 TouchLook;         // ระยะลากนิ้วบนที่ว่างของจอในเฟรมนี้ (พิกเซลของ panel)

        public static bool JumpPressed
        {
            get
            {
                if (Blocked) { JumpButton = false; return false; }
                var k = Keyboard.current;
                bool j = JumpButton || (k != null && k.spaceKey.wasPressedThisFrame);
                JumpButton = false;
                return j;
            }
        }

        // หมุนกล้อง (องศา/เฟรม): คลิกขวาหรือปุ่มกลางลาก · ลากนิ้วบนที่ว่าง · Q/R
        public static Vector2 Look
        {
            get
            {
                var v = TouchLook * 0.25f; TouchLook = Vector2.zero;
                if (Blocked) return Vector2.zero;
                var m = Mouse.current;
                if (m != null && (m.rightButton.isPressed || m.middleButton.isPressed)) v += m.delta.ReadValue() * 0.25f;
                var k = Keyboard.current;
                if (k != null) v.x += ((k.rKey.isPressed ? 1 : 0) - (k.qKey.isPressed ? 1 : 0)) * 120 * UnityEngine.Time.deltaTime;
                return v;
            }
        }
        public static float Zoom { get { var m = Mouse.current; return m == null || Blocked ? 0 : m.scroll.ReadValue().y; } }

        public static Vector2 Move
        {
            get
            {
                if (Blocked) return Vector2.zero;
                if (Joystick.sqrMagnitude > 0.0025f) return Vector2.ClampMagnitude(Joystick, 1);
                var k = Keyboard.current;
                if (k == null) return Vector2.zero;
                float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
                float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0);
                return Vector2.ClampMagnitude(new Vector2(x, y), 1);
            }
        }

        public static bool ActionHeld
        {
            get
            {
                if (Blocked) return false;
                var k = Keyboard.current; var m = Mouse.current;
                return ButtonHeld || (k != null && (k.eKey.isPressed || k.fKey.isPressed || k.enterKey.isPressed))
                    || (m != null && m.leftButton.isPressed && !PointerOverUi && Touchscreen.current == null);
            }
        }
    }
}
