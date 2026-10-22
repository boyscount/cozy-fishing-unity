using UnityEngine;
using UnityEngine.InputSystem;

namespace CozyFishing
{
    // รวมอินพุตทุกแบบ: คีย์บอร์ด (WASD/ลูกศร/Space) · เมาส์ (คลิกค้างบนฉาก) · จอยและปุ่มบนจอสัมผัส (ตั้งค่าจาก Hud)
    public static class GameInput
    {
        public static Vector2 Joystick;          // จากจอยบนจอ (-1..1)
        public static bool ButtonHeld;           // ปุ่ม 🎣 บนจอ
        public static bool PointerOverUi;        // Hud บอกว่าเมาส์อยู่บน UI (ไม่นับเป็นการคลิกฉาก)
        public static bool Blocked;              // มีหน้าต่างเปิดอยู่ (การ์ดปลา ฯลฯ)

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
                return ButtonHeld || (k != null && (k.spaceKey.isPressed || k.eKey.isPressed))
                    || (m != null && m.leftButton.isPressed && !PointerOverUi && Touchscreen.current == null);
            }
        }
    }
}
