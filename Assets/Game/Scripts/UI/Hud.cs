using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // หน้าจอภาษาไทยทั้งหมด (UI Toolkit + ฟอนต์ Mali + Advanced Text Generator ให้สระ/วรรณยุกต์วางถูก)
    // ป้ายจำนวนปลา · คำแนะนำ · แถบแรงเหวี่ยง · แถบแรงตึงสาย · แถบตีเบ็ด · ป้าย "!" · การ์ดปลา · จอยและปุ่มบนจอสัมผัส
    public class Hud : MonoBehaviour
    {
        public Camera cam;
        public bool TouchUi { get; private set; }
        public bool CardOpen => card.style.display == DisplayStyle.Flex;

        static readonly Color Paper = new Color(1f, 0.973f, 0.918f, 0.95f), Ink = new Color(0.23f, 0.18f, 0.14f), Green = new Color(0.42f, 0.66f, 0.31f), Gold = new Color(0.95f, 0.71f, 0.2f);
        UIDocument doc;
        VisualElement root, powerBox, powerFill, fightBox, needle, zone, progFill, strikeBox, sMark, sZone, card, joy, knob, actBtn;
        Label count, hint, toast, alert, sHits, cardName, cardStars, cardKg, cardPrice, cardDesc, actTxt;
        FontDefinition font, fontBold;
        float toastUntil;
        Action onCardClose;

        void Awake()
        {
            font = FontDefinition.FromFont(Resources.Load<Font>("Fonts/Mali-SemiBold"));
            fontBold = FontDefinition.FromFont(Resources.Load<Font>("Fonts/Mali-Bold"));
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1080, 1920);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = Screen.width > Screen.height ? 1f : 0f;
            doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            root = doc.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            TouchUi = Application.isMobilePlatform || Touchscreen.current != null;
            Build();
        }

        // ---------- สร้างหน้าจอ ----------
        Label L(VisualElement parent, string text, int size, Color color, bool bold = true)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.style.fontSize = size; l.style.color = color;
            l.style.unityFontDefinition = bold ? fontBold : font;
            l.style.unityTextGenerator = TextGeneratorType.Advanced;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            l.style.whiteSpace = WhiteSpace.Normal;
            parent?.Add(l);
            return l;
        }
        static VisualElement Box(VisualElement parent, Color bg, float radius = 36)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.backgroundColor = bg;
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = radius;
            parent?.Add(e);
            return e;
        }
        static void Abs(VisualElement e, float? left = null, float? right = null, float? top = null, float? bottom = null)
        {
            e.style.position = Position.Absolute;
            if (left.HasValue) e.style.left = left.Value; if (right.HasValue) e.style.right = right.Value;
            if (top.HasValue) e.style.top = top.Value; if (bottom.HasValue) e.style.bottom = bottom.Value;
        }
        static void Pad(VisualElement e, float v, float h) { e.style.paddingTop = e.style.paddingBottom = v; e.style.paddingLeft = e.style.paddingRight = h; }

        VisualElement Bar(VisualElement parent, float height, Color bg)
        {
            var b = Box(parent, bg, height / 2);
            b.style.height = height; b.style.overflow = Overflow.Hidden; b.style.marginTop = 10;
            return b;
        }

        void Build()
        {
            var countBox = Box(root, Paper, 999); Abs(countBox, 36, null, 50); Pad(countBox, 12, 34);
            count = L(countBox, "ปลา 0 ตัว · ฿0", 40, Ink);

            hint = L(null, "", 38, Ink);
            var hintBox = Box(root, Paper, 34); Abs(hintBox, 90, 90, null, TouchUi ? 330 : 60); Pad(hintBox, 16, 30);
            hintBox.Add(hint);
            hintBox.name = "hintBox";

            toast = L(null, "", 46, Ink);
            var toastBox = Box(root, Paper, 36); Abs(toastBox, 70, 70, 420); Pad(toastBox, 18, 30);
            toastBox.Add(toast); toastBox.name = "toastBox";

            // แถบแรงเหวี่ยง
            powerBox = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(powerBox, 200, 200, null, TouchUi ? 470 : 200); root.Add(powerBox);
            var pt = Bar(powerBox, 40, Paper); powerFill = Box(pt, Gold, 20); powerFill.style.height = Length.Percent(100);
            var pl = L(powerBox, "ปล่อย = เหวี่ยง · ค้างนาน = ไกล", 32, Ink);
            pl.style.marginTop = 8;

            // แถบแรงตึงสาย + ระยะปลา
            fightBox = Box(root, Paper, 36); Abs(fightBox, 70, 70, 190); Pad(fightBox, 22, 34);
            var fr = L(fightBox, "แรงตึงสาย", 34, Ink); fr.style.unityTextAlign = TextAnchor.MiddleLeft;
            var tb = Bar(fightBox, 48, new Color(0.81f, 0.89f, 0.94f));
            zone = Box(tb, new Color(0.42f, 0.66f, 0.31f, 0.6f), 0); Abs(zone, null, null, 0, 0);
            var red = Box(tb, new Color(0.88f, 0.38f, 0.29f, 0.85f), 0); Abs(red, null, 0, 0, 0); red.style.width = Length.Percent(12);
            needle = Box(tb, Ink, 6); Abs(needle, null, null, -4, -4); needle.style.width = 14;
            var pr = L(fightBox, "ดึงปลาเข้าฝั่ง", 32, Ink); pr.style.unityTextAlign = TextAnchor.MiddleLeft; pr.style.marginTop = 12;
            var pb = Bar(fightBox, 28, new Color(0.91f, 0.86f, 0.75f)); progFill = Box(pb, Green, 14); progFill.style.height = Length.Percent(100);

            // แถบตีเบ็ดปลาหายาก
            strikeBox = Box(root, Paper, 36); Abs(strikeBox, 70, 70, 190); Pad(strikeBox, 22, 34);
            strikeBox.style.borderTopWidth = strikeBox.style.borderBottomWidth = strikeBox.style.borderLeftWidth = strikeBox.style.borderRightWidth = 8;
            strikeBox.style.borderTopColor = strikeBox.style.borderBottomColor = strikeBox.style.borderLeftColor = strikeBox.style.borderRightColor = Gold;
            L(strikeBox, "ตีเบ็ดตอนเข็มอยู่ในโซนทอง!", 34, Ink);
            sHits = L(strikeBox, "", 32, Ink);
            var sb = Bar(strikeBox, 60, new Color(0.91f, 0.86f, 0.75f));
            sZone = Box(sb, Gold, 0); Abs(sZone, null, null, 0, 0);
            sMark = Box(sb, Ink, 6); Abs(sMark, null, null, -6, -6); sMark.style.width = 16;

            // ป้าย "!" เหนือหัวตัวละคร
            alert = L(null, "!", 80, Color.white);
            var alertBg = Box(root, Gold, 60); Abs(alertBg); alertBg.style.width = alertBg.style.height = 120; alertBg.Add(alert); alertBg.name = "alert";
            alert.style.flexGrow = 1;

            // การ์ดปลาที่ตกได้
            card = new VisualElement(); Abs(card, 0, 0, 0, 0); card.style.backgroundColor = new Color(0.16f, 0.24f, 0.2f, 0.4f);
            card.style.justifyContent = Justify.Center; card.style.alignItems = Align.Center; root.Add(card);
            var cb = Box(card, Paper, 60); Pad(cb, 40, 50); cb.style.width = Length.Percent(84); cb.pickingMode = PickingMode.Position;
            cardName = L(cb, "", 72, Ink); cardStars = L(cb, "", 46, Gold);
            var stats = new VisualElement { pickingMode = PickingMode.Ignore }; stats.style.flexDirection = FlexDirection.Row; stats.style.justifyContent = Justify.Center; stats.style.marginTop = 16; cb.Add(stats);
            cardKg = L(Stat(stats), "", 46, Ink); cardPrice = L(Stat(stats), "", 46, new Color(0.42f, 0.31f, 0.07f));
            cardDesc = L(cb, "", 34, Ink, false); cardDesc.style.marginTop = 20; cardDesc.style.marginBottom = 30;
            var ok = new Button(() => { card.style.display = DisplayStyle.None; GameInput.Blocked = false; onCardClose?.Invoke(); }) { text = "เก็บใส่ข้องปลา" };
            ok.style.unityFontDefinition = fontBold; ok.style.unityTextGenerator = TextGeneratorType.Advanced;
            ok.style.fontSize = 46; ok.style.color = Color.white; ok.style.backgroundColor = Green; Pad(ok, 22, 60);
            ok.style.borderTopLeftRadius = ok.style.borderTopRightRadius = ok.style.borderBottomLeftRadius = ok.style.borderBottomRightRadius = 999;
            ok.style.borderTopWidth = ok.style.borderBottomWidth = ok.style.borderLeftWidth = ok.style.borderRightWidth = 0;
            ok.style.alignSelf = Align.Center;
            cb.Add(ok);
            card.style.display = DisplayStyle.None;

            if (TouchUi) BuildTouch();
        }

        VisualElement Stat(VisualElement parent)
        {
            var b = Box(parent, new Color(0.96f, 0.91f, 0.8f), 30); Pad(b, 12, 30); b.style.marginLeft = b.style.marginRight = 10;
            return b;
        }

        void BuildTouch()
        {
            joy = Box(root, new Color(1, 0.97f, 0.92f, 0.35f), 160); Abs(joy, 60, null, null, 70);
            joy.style.width = joy.style.height = 320; joy.pickingMode = PickingMode.Position;
            knob = Box(joy, Paper, 75); Abs(knob, 85, null, 85); knob.style.width = knob.style.height = 150;
            joy.RegisterCallback<PointerDownEvent>(e => { joy.CapturePointer(e.pointerId); MoveKnob(e.localPosition); });
            joy.RegisterCallback<PointerMoveEvent>(e => { if (joy.HasPointerCapture(e.pointerId)) MoveKnob(e.localPosition); });
            joy.RegisterCallback<PointerUpEvent>(e => { joy.ReleasePointer(e.pointerId); GameInput.Joystick = Vector2.zero; knob.style.left = 85; knob.style.top = 85; });

            actBtn = Box(root, Green, 140); Abs(actBtn, null, 60, null, 80);
            actBtn.style.width = actBtn.style.height = 280; actBtn.pickingMode = PickingMode.Position;
            actBtn.style.justifyContent = Justify.Center;
            actTxt = L(actBtn, "เหวี่ยง", 52, Color.white);
            actBtn.RegisterCallback<PointerDownEvent>(e => { actBtn.CapturePointer(e.pointerId); GameInput.ButtonHeld = true; });
            actBtn.RegisterCallback<PointerUpEvent>(e => { actBtn.ReleasePointer(e.pointerId); GameInput.ButtonHeld = false; });
        }

        void MoveKnob(Vector3 local)
        {
            var d = new Vector2(local.x - 160, local.y - 160);
            d = Vector2.ClampMagnitude(d, 120);
            knob.style.left = 85 + d.x; knob.style.top = 85 + d.y;
            GameInput.Joystick = new Vector2(d.x / 120f, -d.y / 120f);
        }

        // ---------- API ให้ระบบตกปลาเรียก ----------
        string lastHint = null;
        public void Hint(string s)
        {
            if (s == lastHint) return;
            lastHint = s; hint.text = s;
            root.Q("hintBox").style.display = string.IsNullOrEmpty(s) ? DisplayStyle.None : DisplayStyle.Flex;
        }
        public void Toast(string s) { toast.text = s; toastUntil = Time.time + 1.8f; }
        public void ActionLabel(string txt, bool hot)
        {
            if (actBtn == null) return;
            actTxt.text = txt;
            actBtn.style.backgroundColor = hot ? Gold : Green;
        }
        public void Power(float v)
        {
            powerBox.style.display = v < 0 ? DisplayStyle.None : DisplayStyle.Flex;
            if (v >= 0) powerFill.style.width = Length.Percent(v * 100);
        }
        public void Tension(bool show, float tension, float prog, float zc, float zw, bool danger)
        {
            fightBox.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;
            needle.style.left = Length.Percent(Mathf.Clamp01(tension) * 100);
            zone.style.left = Length.Percent((zc - zw / 2) * 100); zone.style.width = Length.Percent(zw * 100);
            progFill.style.width = Length.Percent(Mathf.Clamp01(prog) * 100);
            fightBox.style.translate = new Translate(danger ? Mathf.Sin(Time.time * 60) * 6 : 0, 0);
        }
        public void Strike(bool show, float pos, float center, float width, int hits, int need, int misses)
        {
            strikeBox.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;
            sMark.style.left = Length.Percent(pos * 100);
            sZone.style.left = Length.Percent((center - width / 2) * 100); sZone.style.width = Length.Percent(width * 100);
            sHits.text = $"ตีโดน {hits}/{need}" + (misses > 0 ? "· พลาด 1" : "");
        }
        public void Alert(bool show, Vector3 world, bool rare)
        {
            var a = root.Q("alert");
            a.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show || cam == null) return;
            var sp = cam.WorldToScreenPoint(world);
            var p = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(sp.x, Screen.height - sp.y));
            a.style.left = p.x - 60; a.style.top = p.y - 60;
            alert.text = rare ? "!!" : "!";
        }
        int lastCount = -1, lastMoney = -1;
        public void SetCount(int n, int money)
        {
            if (n == lastCount && money == lastMoney) return;
            lastCount = n; lastMoney = money;
            count.text = $"ปลา {n} ตัว · ฿{money:N0}";
        }
        public void ShowCatch(Catch c, Action onClose)
        {
            var sp = c.species;
            cardName.text = sp.name;
            cardStars.text = $"{FishDatabase.Rarity[sp.stars]} · {sp.stars} ดาว";
            cardKg.text = $"{c.kg:0.00} กก.";
            cardPrice.text = $"฿{Mathf.RoundToInt(c.kg * sp.price):N0}";
            cardDesc.text = sp.desc;
            onCardClose = onClose;
            GameInput.Blocked = true;
            card.style.display = DisplayStyle.Flex;
        }

        void Update()
        {
            root.Q("toastBox").style.display = Time.time < toastUntil ? DisplayStyle.Flex : DisplayStyle.None;
            var m = Mouse.current;
            if (m != null && root.panel != null)
            {
                var mp = m.position.ReadValue();
                var p = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(mp.x, Screen.height - mp.y));
                GameInput.PointerOverUi = root.panel.Pick(p) != null;
            }
        }
    }
}
