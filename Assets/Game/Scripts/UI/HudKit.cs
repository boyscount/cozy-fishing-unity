using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // เครื่องมือสร้างหน้าจอที่ทุกแผงใช้ร่วมกัน (Hud และแผงแยกอย่าง ShopPanel, WardrobePanel)
    //   ตัวช่วยที่ไม่ต้องใช้ฟอนต์เป็น static (ไฟล์ Hud ใช้ผ่าน using static) · ตัวที่ต้องใช้ฟอนต์อยู่ในอินสแตนซ์ที่ Hud สร้างหลังโหลดฟอนต์
    sealed class HudKit
    {
        public static readonly Color ShopGold = new Color(0.86f, 0.7f, 0.38f), PanelBg = new Color(0.07f, 0.06f, 0.045f, 0.9f);

        public readonly FontDefinition Font, FontBold, FontTitle;

        public HudKit(FontDefinition font, FontDefinition fontBold, FontDefinition fontTitle) { Font = font; FontBold = fontBold; FontTitle = fontTitle; }

        public Label L(VisualElement parent, string text, int size, Color color, bool bold = true)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.style.fontSize = size; l.style.color = color;
            l.style.unityFontDefinition = bold ? FontBold : Font;
            l.style.unityTextGenerator = TextGeneratorType.Advanced;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.flexShrink = 0;
            l.style.marginTop = l.style.marginBottom = 0;
            parent?.Add(l);
            return l;
        }

        public void StyleButton(Button b, Color bg)
        {
            b.style.unityFontDefinition = FontBold; b.style.unityTextGenerator = TextGeneratorType.Advanced;
            b.style.fontSize = 28; b.style.color = Color.white; b.style.backgroundColor = bg; Pad(b, 8, 26);
            b.style.borderTopLeftRadius = b.style.borderTopRightRadius = b.style.borderBottomLeftRadius = b.style.borderBottomRightRadius = 10;
            b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 0;
            b.style.flexShrink = 0;
            // นิ้วแตะ: กดแล้วยกนิ้วบนปุ่ม = กด (Clickable ของ Button บางครั้งไม่รับแตะบนเว็บมือถือ) · เมาส์ใช้ระบบเดิม
            b.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.pointerType == UnityEngine.UIElements.PointerType.mouse) return;
                b.CapturePointer(e.pointerId);
                e.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);
            b.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.pointerType == UnityEngine.UIElements.PointerType.mouse || !b.HasPointerCapture(e.pointerId)) return;
                b.ReleasePointer(e.pointerId);
                e.StopImmediatePropagation();
                if (!b.enabledInHierarchy || !b.worldBound.Contains(e.position)) return;
                using (var ev = NavigationSubmitEvent.GetPooled()) { ev.target = b; b.SendEvent(ev); }
            }, TrickleDown.TrickleDown);
        }

        // ปุ่มป้ายไม้เก่า (หน้าเช้า)
        public void WoodButton(Button b, bool active)
        {
            StyleButton(b, Color.clear);
            Skin(b, "plank", 22); b.style.color = active ? new Color(0.22f, 0.12f, 0.04f) : new Color(0.4f, 0.34f, 0.28f);
            b.style.unityBackgroundImageTintColor = active ? Color.white : new Color(0.7f, 0.68f, 0.65f);
            Pad(b, 14, 30); b.style.fontSize = 26;
        }

        public static void Radius(VisualElement e, float r) { e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r; }
        public static void Abs(VisualElement e, float? left = null, float? right = null, float? top = null, float? bottom = null)
        {
            e.style.position = Position.Absolute;
            if (left.HasValue) e.style.left = left.Value; if (right.HasValue) e.style.right = right.Value;
            if (top.HasValue) e.style.top = top.Value; if (bottom.HasValue) e.style.bottom = bottom.Value;
        }
        public static void Pad(VisualElement e, float v, float h) { e.style.paddingTop = e.style.paddingBottom = v; e.style.paddingLeft = e.style.paddingRight = h; }

        // ไอคอนเล็ก (Resources/UI/Icons/<name>.png)
        public static VisualElement Ico(VisualElement parent, string name, float size)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.width = e.style.height = size; e.style.flexShrink = 0;
            e.style.backgroundImage = Resources.Load<Texture2D>("UI/Icons/" + name);
            parent.Add(e);
            return e;
        }

        // ผิวกรอบจากภาพ (Resources/UI/Frames/<name>) แบบ 9 ส่วน: ขอบไม่ยืด ตรงกลางยืดตามขนาดกล่อง
        public static void Skin(VisualElement e, string frame, int slice)
        {
            var t = Resources.Load<Texture2D>("UI/Frames/" + frame);
            if (t == null) return;
            e.style.backgroundImage = t; e.style.backgroundColor = Color.clear;
            e.style.unitySliceLeft = e.style.unitySliceRight = e.style.unitySliceTop = e.style.unitySliceBottom = slice;
            e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = 0;
        }

        // แผงไม้ขอบทอง (ร้าน ตู้แต่งตัว กระดาน ภารกิจ)
        public static void GoldFrame(VisualElement e, float w = 2)
        {
            e.style.backgroundColor = PanelBg;
            e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = w;
            e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = new Color(ShopGold.r, ShopGold.g, ShopGold.b, 0.85f);
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = 6;
        }

        // ปุ่มในร้านให้รู้ว่ากดได้: ชี้แล้วขยายนิด พื้นสว่างขึ้น ขอบทองเรือง · กดแล้วยุบลง (จอสัมผัสเห็นตอนนิ้วแตะ)
        public static void Hoverable(VisualElement e, Color hoverBg)
        {
            e.AddToClassList("pressable");   // มีผลตอบสนองของตัวเองแล้ว ไม่ต้องใส่แบบทั่วไปซ้ำ
            e.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("scale"), new StylePropertyName("background-color"), new StylePropertyName("border-top-color"), new StylePropertyName("border-bottom-color"), new StylePropertyName("border-left-color"), new StylePropertyName("border-right-color") };
            e.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(0.12f) };
            StyleColor bg = default, bc = default; bool over = false; var hoverBorder = new Color(1f, 0.88f, 0.45f);
            void On()
            {
                if (over || !e.enabledInHierarchy) return; over = true;
                bg = e.style.backgroundColor; bc = e.style.borderTopColor;
                e.style.backgroundColor = hoverBg; e.style.scale = new Scale(Vector3.one * 1.04f);
                e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = hoverBorder;
            }
            void Off()
            {
                if (!over) return; over = false;
                e.style.scale = new Scale(Vector3.one);
                // คืนสีเดิมเฉพาะเมื่อยังเป็นสี hover อยู่ · ถ้าระหว่างนั้นปุ่มถูกตั้งเป็น "เลือกอยู่" (แท็บที่กด) อย่าไปทับสีนั้น
                if (e.style.backgroundColor == hoverBg) e.style.backgroundColor = bg;
                if (e.style.borderTopColor == hoverBorder) e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = bc;
            }
            e.RegisterCallback<PointerEnterEvent>(_ => On());
            e.RegisterCallback<PointerLeaveEvent>(_ => Off());
            e.RegisterCallback<PointerDownEvent>(_ => { On(); if (e.enabledInHierarchy) e.style.scale = new Scale(Vector3.one * 0.96f); }, TrickleDown.TrickleDown);
            e.RegisterCallback<PointerUpEvent>(_ => { if (over) e.style.scale = new Scale(Vector3.one * 1.04f); });
            e.RegisterCallback<PointerCancelEvent>(_ => Off());
        }
    }
}
