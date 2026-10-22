using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // หน้าจอภาษาไทยทั้งหมด (UI Toolkit + ฟอนต์ Mali + Advanced Text Generator ให้สระ/วรรณยุกต์วางถูก)
    // ป้ายจำนวนปลา · คำแนะนำ · แถบแรงเหวี่ยง · แถบแรงตึงสาย · แถบตีเบ็ด · ป้าย "!" · การ์ดปลา · ปุ่มกดหลัก (ทุกจอ) · จอยบนจอสัมผัส
    // สเกล: อ้างอิงจอสี่เหลี่ยม 1080 หน่วย เฉลี่ยกว้าง/สูง (match 0.5) → แนวตั้งและแนวนอนตัวหนังสือขนาดใกล้กัน ไม่ล้นจอ
    public partial class Hud : MonoBehaviour
    {
        public Camera cam;
        public FishPreview preview;
        public bool TouchUi { get; private set; }
        public bool CardOpen => card.style.display == DisplayStyle.Flex || (dex != null && dex.style.display == DisplayStyle.Flex);

        // โทนเกมตกปลาแบบซิม: แผงกระจกสีเข้มโปร่งแสง · ตัวหนังสือขาว · สีส้มเน้นปุ่มสำคัญ · เขียวสำหรับค่าที่ดี
        static readonly Color Paper = new Color(0.06f, 0.08f, 0.1f, 0.72f), Ink = new Color(0.95f, 0.96f, 0.97f), Green = new Color(0.24f, 0.7f, 0.42f),
            Gold = new Color(0.96f, 0.55f, 0.16f), Edge = new Color(1f, 1f, 1f, 0.14f),
            Glass2 = new Color(0.13f, 0.16f, 0.19f, 0.92f), Glass3 = new Color(0.2f, 0.23f, 0.27f, 0.92f), Sub = new Color(0.72f, 0.76f, 0.8f),
            Money = new Color(1f, 0.82f, 0.38f), Track = new Color(1f, 1f, 1f, 0.12f), Gray = new Color(0.33f, 0.36f, 0.4f, 0.95f);
        UIDocument doc;
        VisualElement root, hintRow, toastRow, fightRow, strikeRow, jumpBtn, hintBox, toastBox, powerBox, powerFill, fightBox, needle, zone, progFill, strikeBox, sMark, sZone, card, joy, knob, actBtn, alertBg;
        VisualElement cardImg, cardNew, dex, dexGrid, dexBtn; Label dexCount, dexTitle; string dexStage = "paddy";
        Label count, hint, toast, alert, sHits, cardName, cardStars, cardKg, cardPrice, cardDesc, actTxt, actKey;
        FontDefinition font, fontBold;
        float toastUntil;
        Action onCardOk, onCardAlt;
        Button cardOk, cardAlt;
        VisualElement countBoxRef;

        void Awake()
        {
            font = FontDefinition.FromFont(Resources.Load<Font>("Fonts/Mali-SemiBold"));
            fontBold = FontDefinition.FromFont(Resources.Load<Font>("Fonts/Mali-Bold"));
            // PanelSettings สร้างไว้เป็น asset (SceneBuilder) — build จะได้ธีม + ข้อมูล ICU ตัดคำภาษาไทย
            var panel = Resources.Load<PanelSettings>("UI/HudPanel");
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/CozyTheme");
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(1080, 1080);
                panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panel.match = 0.5f;
            }
            doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            root = doc.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            // รากต้องเต็มจอเสมอ (ไม่งั้นของที่ยึดขอบล่าง/ขวาหลุดออกนอกจอ)
            root.style.position = Position.Absolute; root.style.left = root.style.top = root.style.right = root.style.bottom = 0;
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
            l.style.flexShrink = 0;
            l.style.marginTop = l.style.marginBottom = 0;
            parent?.Add(l);
            return l;
        }
        static void Radius(VisualElement e, float r) { e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r; }
        static void Border(VisualElement e, float w, Color c)
        {
            e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = w;
            e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = c;
        }
        static VisualElement Box(VisualElement parent, Color bg, float radius = 12, bool border = true)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.backgroundColor = bg;
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = radius;
            if (border)
            {
                e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = 1.5f;
                e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = Edge;
            }
            e.style.flexShrink = 0;
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
        // แถวเต็มกว้างที่จัดลูกไว้กลางจอ (กล่องข้างในกว้างไม่เกิน maxW)
        VisualElement CenterRow(float? top, float? bottom)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(row, 0, 0, top, bottom);
            row.style.alignItems = Align.Center;
            root.Add(row);
            return row;
        }
        static void MaxW(VisualElement e, float w) { e.style.maxWidth = w; e.style.width = Length.Percent(92); }

        VisualElement Bar(VisualElement parent, float height, Color bg)
        {
            var b = Box(parent, bg, height / 2, false);
            b.style.height = height; b.style.overflow = Overflow.Hidden; b.style.marginTop = 8;
            return b;
        }

        void Build()
        {
            var countBox = countBoxRef = Box(root, Paper, 12); Abs(countBox, 24, null, 24); Pad(countBox, 6, 18);
            count = L(countBox, "ปลา 0 ตัว · ฿0", 26, Ink);

            // ข้อความแจ้งสั้น ๆ: กลางจอด้านบน
            toastRow = CenterRow(96, null);
            toastBox = Box(toastRow, Paper, 28); Pad(toastBox, 12, 28); toastBox.style.maxWidth = 820;
            toast = L(toastBox, "", 32, Ink);

            // เกจแนวตั้งด้านขวา (แบบเกมตกปลาซิม): แรงตึงสาย (แท่งไล่สีเขียว→เหลือง→แดง มีกรอบโซนปลอดภัย) + ระยะดึงปลาเข้าฝั่ง
            fightRow = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(fightRow, null, 30, 150, null); root.Add(fightRow);
            fightBox = Box(fightRow, Paper, 12); Pad(fightBox, 10, 12); fightBox.style.flexDirection = FlexDirection.Row;
            tensionSegs = Meter(fightBox, "สาย"); progSegs = Meter(fightBox, "ดึงเข้า");
            zone = new VisualElement(); needle = new VisualElement(); progFill = new VisualElement();

            // แถบตีเบ็ดปลาหายาก
            strikeRow = CenterRow(170, null);
            strikeBox = Box(strikeRow, Paper, 28); Pad(strikeBox, 16, 26); MaxW(strikeBox, 760);
            strikeBox.style.borderTopWidth = strikeBox.style.borderBottomWidth = strikeBox.style.borderLeftWidth = strikeBox.style.borderRightWidth = 6;
            strikeBox.style.borderTopColor = strikeBox.style.borderBottomColor = strikeBox.style.borderLeftColor = strikeBox.style.borderRightColor = Gold;
            L(strikeBox, "ตีเบ็ดตอนเข็มอยู่ในโซนทอง!", 28, Ink);
            sHits = L(strikeBox, "", 24, Ink);
            var sb = Bar(strikeBox, 46, Track);
            sZone = Box(sb, Gold, 0, false); Abs(sZone, null, null, 0, 0);
            sMark = Box(sb, Ink, 5, false); Abs(sMark, null, null, -2, -2); sMark.style.width = 14;

            // แถบแรงเหวี่ยง + คำแนะนำ (ด้านล่าง เว้นที่ให้ปุ่ม)
            float bottomGap = TouchUi ? 300 : 40;
            hintRow = CenterRow(null, bottomGap);
            powerBox = new VisualElement { pickingMode = PickingMode.Ignore }; MaxW(powerBox, 560); powerBox.style.marginBottom = 12; hintRow.Add(powerBox);
            var pt = Bar(powerBox, 34, new Color(1, 1, 1, 0.85f)); powerFill = Box(pt, Gold, 17, false); powerFill.style.height = Length.Percent(100);
            var pl = L(powerBox, "ค้างนาน = เหวี่ยงไกล · ปล่อย = เหวี่ยง", 24, Color.white);
            pl.style.marginTop = 6; Shadow(pl);
            hintBox = Box(hintRow, Paper, 26); Pad(hintBox, 12, 26); hintBox.style.maxWidth = TouchUi ? 640 : 760;
            hint = L(hintBox, "", 28, Ink);

            // ป้าย "!" เหนือหัวตัวละคร
            alertBg = Box(root, Gold, 48); Abs(alertBg); alertBg.style.width = alertBg.style.height = 96; alertBg.style.justifyContent = Justify.Center;
            alertBg.style.borderTopColor = alertBg.style.borderBottomColor = alertBg.style.borderLeftColor = alertBg.style.borderRightColor = Color.white;
            alertBg.style.borderTopWidth = alertBg.style.borderBottomWidth = alertBg.style.borderLeftWidth = alertBg.style.borderRightWidth = 5;
            alert = L(alertBg, "!", 60, Color.white);

            BuildCard();
            BuildDex();
            BuildStory();
            BuildButton();
            if (TouchUi) { BuildJoystick(); BuildTouchExtras(); }
        }

        VisualElement[] tensionSegs, progSegs;
        Label lineNum, lineUnit;
        // เกจแท่งแนวตั้ง 18 ช่อง (ช่องล่างสุด = ค่าน้อย)
        VisualElement[] Meter(VisualElement parent, string label)
        {
            var col = new VisualElement { pickingMode = PickingMode.Ignore }; col.style.alignItems = Align.Center; col.style.marginLeft = col.style.marginRight = 6; parent.Add(col);
            var stack = new VisualElement { pickingMode = PickingMode.Ignore }; stack.style.flexDirection = FlexDirection.ColumnReverse; col.Add(stack);
            var segs = new VisualElement[18];
            for (int i = 0; i < segs.Length; i++)
            {
                var s = new VisualElement { pickingMode = PickingMode.Ignore };
                s.style.width = 40; s.style.height = 13; s.style.marginTop = 3;
                s.style.borderLeftWidth = s.style.borderRightWidth = 3;
                stack.Add(s); segs[i] = s;
            }
            var l = L(col, label, 18, Sub); l.style.marginTop = 6;
            return segs;
        }

        /// ระยะสาย (เมตร) มุมขวาล่างเหนือปุ่มหลัก · ค่าติดลบ = ซ่อน
        public void LineDistance(float m)
        {
            if (lineNum == null) return;
            bool show = m >= 0 && !CardOpen && !ModalOpen;
            lineNum.parent.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show) lineNum.text = Mathf.RoundToInt(m).ToString();
        }

        static void Shadow(Label l) { l.style.textShadow = new TextShadow { offset = new Vector2(0, 2), blurRadius = 6, color = new Color(0, 0, 0, 0.75f) }; }

        void BuildCard()
        {
            card = new VisualElement(); Abs(card, 0, 0, 0, 0); card.style.width = Length.Percent(100); card.style.height = Length.Percent(100);
            card.style.backgroundColor = new Color(0.02f, 0.03f, 0.04f, 0.55f);
            card.style.justifyContent = Justify.Center; card.style.alignItems = Align.Center; root.Add(card);
            var cb = Box(card, Paper, 16); Pad(cb, 20, 26); MaxW(cb, 980); cb.pickingMode = PickingMode.Position;
            cb.style.alignItems = Align.Stretch;
            // ภาพปลา 3D หมุนช้า ๆ เต็มแถบ + ป้าย "ใหม่!" มุมขวา
            cardImg = new VisualElement { pickingMode = PickingMode.Ignore }; cardImg.style.height = 300; cardImg.style.flexShrink = 0;
            cardImg.style.backgroundColor = new Color(0.1f, 0.18f, 0.22f, 0.9f);
            cardImg.style.borderTopLeftRadius = cardImg.style.borderTopRightRadius = cardImg.style.borderBottomLeftRadius = cardImg.style.borderBottomRightRadius = 10;
            cardImg.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            cb.Add(cardImg);
            cardNew = Box(cardImg, Gold, 6, false); Abs(cardNew, null, 12, 12); Pad(cardNew, 4, 14);
            L(cardNew, "ใหม่! บันทึกลงสมุด", 20, Color.white);
            var head = new VisualElement { pickingMode = PickingMode.Ignore }; head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.FlexEnd; head.style.marginTop = 12; head.style.flexShrink = 0; cb.Add(head);
            var names = new VisualElement { pickingMode = PickingMode.Ignore }; names.style.flexGrow = 1; head.Add(names);
            cardStars = L(names, "", 22, Gold); cardStars.style.unityTextAlign = TextAnchor.MiddleLeft;
            cardName = L(names, "", 56, Ink); cardName.style.unityTextAlign = TextAnchor.MiddleLeft;
            var stats = new VisualElement { pickingMode = PickingMode.Ignore }; stats.style.flexDirection = FlexDirection.Row; stats.style.flexShrink = 0; head.Add(stats);
            cardKg = L(Stat(stats), "", 30, Ink); cardPrice = L(Stat(stats), "", 30, Money);
            cardDesc = L(cb, "", 22, Sub, false); cardDesc.style.marginTop = 10; cardDesc.style.unityTextAlign = TextAnchor.MiddleLeft;
            var btns = new VisualElement { pickingMode = PickingMode.Ignore }; btns.style.flexDirection = FlexDirection.Row; btns.style.justifyContent = Justify.Center; btns.style.marginTop = 16; btns.style.flexShrink = 0; cb.Add(btns);
            cardAlt = new Button(() => CloseCard(true)) { text = "ปล่อย" };
            StyleButton(cardAlt, Gray); cardAlt.style.minWidth = 240; cardAlt.style.fontSize = 30; btns.Add(cardAlt);
            cardOk = new Button(() => CloseCard(false)) { text = "เก็บ" };
            StyleButton(cardOk, Gold); cardOk.style.minWidth = 300; cardOk.style.fontSize = 30; cardOk.style.marginLeft = 14; btns.Add(cardOk);
            card.style.display = DisplayStyle.None;
        }

        VisualElement Stat(VisualElement parent)
        {
            var b = Box(parent, Glass3, 10, false); Pad(b, 8, 18); b.style.marginLeft = 10;
            return b;
        }

        // ปุ่มหลัก: จอสัมผัสใช้นิ้วกด · คอมพิวเตอร์คลิกเมาส์ค้างที่ปุ่มได้ หรือกด Space ก็ได้ (แสดงป้าย "Space")
        void BuildButton()
        {
            float size = TouchUi ? 230 : 170;
            var lineBox = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(lineBox, null, 40, null, 40 + size + 10); lineBox.style.alignItems = Align.FlexEnd; root.Add(lineBox);
            lineNum = L(lineBox, "0", 64, Ink); Shadow(lineNum);
            lineUnit = L(lineBox, "ระยะสาย (ม.)", 18, Sub); Shadow(lineUnit);
            lineBox.style.display = DisplayStyle.None;
            actBtn = Box(root, Paper, size / 2); Abs(actBtn, null, 40, null, 40);
            actBtn.style.width = actBtn.style.height = size; actBtn.pickingMode = PickingMode.Position;
            actBtn.style.justifyContent = Justify.Center; actBtn.style.alignItems = Align.Center;
            actBtn.style.borderTopColor = actBtn.style.borderBottomColor = actBtn.style.borderLeftColor = actBtn.style.borderRightColor = Gold;
            actBtn.style.borderTopWidth = actBtn.style.borderBottomWidth = actBtn.style.borderLeftWidth = actBtn.style.borderRightWidth = 7;
            actTxt = L(actBtn, "เหวี่ยง", TouchUi ? 44 : 36, Color.white); Shadow(actTxt);
            if (!TouchUi) { actKey = L(actBtn, "Space", 20, new Color(1, 1, 1, 0.85f)); actKey.style.marginTop = 2; }
            actBtn.RegisterCallback<PointerDownEvent>(e => { actBtn.CapturePointer(e.pointerId); GameInput.ButtonHeld = true; actBtn.style.scale = new Scale(new Vector3(0.94f, 0.94f, 1)); });
            actBtn.RegisterCallback<PointerUpEvent>(e => { actBtn.ReleasePointer(e.pointerId); GameInput.ButtonHeld = false; actBtn.style.scale = new Scale(Vector3.one); });
        }

        // จอสัมผัส: ลากนิ้วบนที่ว่างของจอ = หมุนกล้อง (อยู่ชั้นล่างสุด ไม่บังปุ่ม)
        void BuildTouchExtras()
        {
            var pad = new VisualElement { pickingMode = PickingMode.Position }; Abs(pad, 0, 0, 0, 0);
            root.Insert(0, pad);
            Vector2 last = default;
            pad.RegisterCallback<PointerDownEvent>(e => { pad.CapturePointer(e.pointerId); last = e.localPosition; });
            pad.RegisterCallback<PointerMoveEvent>(e => { if (!pad.HasPointerCapture(e.pointerId)) return; var p = (Vector2)e.localPosition; GameInput.TouchLook += new Vector2(p.x - last.x, last.y - p.y); last = p; });
            pad.RegisterCallback<PointerUpEvent>(e => pad.ReleasePointer(e.pointerId));
            // (ปุ่มกระโดดบนจอเอาออกแล้ว — แทบไม่ได้ใช้ · คอมยังกด Space กระโดดได้)
        }

        void BuildJoystick()
        {
            joy = Box(root, new Color(1, 0.97f, 0.92f, 0.3f), 130); Abs(joy, 40, null, null, 40);
            joy.style.width = joy.style.height = 260; joy.pickingMode = PickingMode.Position;
            joy.style.borderTopColor = joy.style.borderBottomColor = joy.style.borderLeftColor = joy.style.borderRightColor = new Color(1, 1, 1, 0.6f);
            knob = Box(joy, Paper, 60); Abs(knob, 70, null, 70); knob.style.width = knob.style.height = 120;
            var kl = L(knob, "เดิน", 26, Ink); kl.style.flexGrow = 1;
            joy.RegisterCallback<PointerDownEvent>(e => { joy.CapturePointer(e.pointerId); MoveKnob(e.localPosition); });
            joy.RegisterCallback<PointerMoveEvent>(e => { if (joy.HasPointerCapture(e.pointerId)) MoveKnob(e.localPosition); });
            joy.RegisterCallback<PointerUpEvent>(e => { joy.ReleasePointer(e.pointerId); GameInput.Joystick = Vector2.zero; knob.style.left = 70; knob.style.top = 70; });
        }

        void MoveKnob(Vector3 local)
        {
            var d = new Vector2(local.x - 130, local.y - 130);
            d = Vector2.ClampMagnitude(d, 100);
            knob.style.left = 70 + d.x; knob.style.top = 70 + d.y;
            GameInput.Joystick = new Vector2(d.x / 100f, -d.y / 100f);
        }

        // ---------- สมุดสะสมปลา: แท็บตามด่าน · ตกได้แล้วเห็นรูป/ชื่อ/ตัวหนักสุด · ยังไม่เคยตก = เงาดำ "???" ----------
        static (string id, string name)[] DexStages => System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(CozyFishing.Stages.All, s => (s.id, s.name)));
        static int DexTotal => System.Linq.Enumerable.Count(FishDatabase.All, sp => System.Linq.Enumerable.Any(CozyFishing.Stages.All, st => st.id == sp.stage));

        // แถบปุ่มไอคอนมุมขวาบน (สมุดปลา · สมุดคดี · กลับบ้าน) เรียงจากขวาไปซ้าย ปุ่มที่ซ่อนอยู่ไม่เว้นช่อง
        VisualElement iconBar;
        VisualElement IconBar
        {
            get
            {
                if (iconBar != null) return iconBar;
                iconBar = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(iconBar, null, 18, 18);
                iconBar.style.flexDirection = FlexDirection.RowReverse; iconBar.style.alignItems = Align.FlexStart;
                root.Add(iconBar);
                return iconBar;
            }
        }
        // ปุ่มกลม: ไอคอน (Resources/UI/Icons/<icon>.png) + ป้ายตัวเลขเล็ก ๆ ใต้ปุ่ม
        VisualElement IconButton(string icon, Color ring, System.Action click, out Label badge)
        {
            var col = new VisualElement { pickingMode = PickingMode.Position };
            col.style.alignItems = Align.Center; col.style.marginLeft = 14; col.style.width = 84;
            var disc = Box(col, new Color(0.08f, 0.1f, 0.09f, 0.72f), 38, false);
            disc.style.width = disc.style.height = 76; disc.style.alignItems = Align.Center; disc.style.justifyContent = Justify.Center;
            Border(disc, 3, ring);
            var img = new VisualElement { pickingMode = PickingMode.Ignore };
            img.style.width = img.style.height = 46; img.style.backgroundImage = Resources.Load<Texture2D>("UI/Icons/" + icon);
            disc.Add(img);
            badge = L(col, "", 19, Color.white); Shadow(badge); badge.style.marginTop = 3; badge.style.whiteSpace = WhiteSpace.NoWrap;
            col.RegisterCallback<PointerDownEvent>(e => click());
            IconBar.Add(col);
            return col;
        }

        void BuildDex()
        {
            dexBtn = IconButton("fishbook", new Color(0.85f, 0.7f, 0.38f), OpenDex, out dexCount);
            dexCount.text = $"{FishDex.Discovered}/{DexTotal}";

            dex = new VisualElement(); Abs(dex, 0, 0, 0, 0); dex.style.backgroundColor = new Color(0.1f, 0.16f, 0.13f, 0.6f);
            dex.style.justifyContent = Justify.Center; dex.style.alignItems = Align.Center; root.Add(dex);
            var book = Box(dex, Paper, 40); Pad(book, 22, 26); MaxW(book, 980); book.style.maxHeight = Length.Percent(92); book.pickingMode = PickingMode.Position;
            var head = new VisualElement { pickingMode = PickingMode.Ignore }; head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center; head.style.flexShrink = 0; book.Add(head);
            dexTitle = L(head, "สมุดสะสมปลา", 36, Ink); dexTitle.style.flexGrow = 1; dexTitle.style.unityTextAlign = TextAnchor.MiddleLeft;
            var close = new Button(() => { dex.style.display = DisplayStyle.None; GameInput.Blocked = false; }) { text = "ปิด" };
            StyleButton(close, Gray); head.Add(close);
            var tabs = new VisualElement { pickingMode = PickingMode.Ignore }; tabs.style.flexDirection = FlexDirection.Row; tabs.style.flexWrap = Wrap.Wrap; tabs.style.marginTop = 10; tabs.style.flexShrink = 0; book.Add(tabs);
            foreach (var (id, name) in DexStages)
            {
                var tab = new Button(() => { dexStage = id; FillDex(); }) { text = name, name = "tab-" + id };
                StyleButton(tab, Green); tab.style.fontSize = 24; Pad(tab, 6, 18); tab.style.marginRight = 8; tab.style.marginBottom = 6;
                tabs.Add(tab);
            }
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.style.flexGrow = 1; scroll.style.flexShrink = 1; scroll.style.marginTop = 8; book.Add(scroll);
            dexGrid = new VisualElement(); dexGrid.style.flexDirection = FlexDirection.Row; dexGrid.style.flexWrap = Wrap.Wrap; dexGrid.style.justifyContent = Justify.Center;
            scroll.Add(dexGrid);
            dex.style.display = DisplayStyle.None;
        }

        void StyleButton(Button b, Color bg)
        {
            b.style.unityFontDefinition = fontBold; b.style.unityTextGenerator = TextGeneratorType.Advanced;
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

        void OpenDex()
        {
            if (CardOpen) return;
            GameInput.Blocked = true;
            dex.style.display = DisplayStyle.Flex;
            FillDex();
        }

        void FillDex()
        {
            dexGrid.Clear();
            int got = 0, total = 0;
            foreach (var (id, name) in DexStages)
            {
                var tab = dex.Q<Button>("tab-" + id);
                if (tab != null) tab.style.backgroundColor = id == dexStage ? Gold : new Color(0.36f, 0.6f, 0.27f, 0.75f);
            }
            foreach (var sp in FishDatabase.ForStage(dexStage))
            {
                total++;
                bool has = FishDex.Has(sp.id); if (has) got++;
                var cell = Box(dexGrid, has ? Glass2 : Glass3, 24); Pad(cell, 10, 10);
                cell.style.width = 280; cell.style.marginLeft = cell.style.marginRight = 8; cell.style.marginBottom = 14;
                var img = new VisualElement { pickingMode = PickingMode.Ignore }; img.style.height = 130; img.style.flexShrink = 0;
                img.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                if (preview != null) img.style.backgroundImage = Background.FromRenderTexture(preview.Icon(sp, !has));
                cell.Add(img);
                L(cell, has ? sp.name : "???", 28, Ink);
                L(cell, new string('*', 0) + $"{FishDatabase.Rarity[sp.stars]} · {sp.stars} ดาว", 20, Gold);
                L(cell, has ? $"ได้ {FishDex.Count(sp.id)} ตัว · หนักสุด {FishDex.BestKg(sp.id):0.00} กก." : (CozyFishing.Stages.Unlocked(sp.stage) ? "ยังไม่เคยตกได้" : "ด่านนี้ยังไม่เปิด"), 20, Sub, false);
                // ความลับของปลา: ปลดล็อกแล้วเห็นข้อความ · ยังไม่ปลดล็อกเห็นเงื่อนไข
                var sec = Mystery.SecretOf(sp.id);
                if (sec != null)
                {
                    bool known = Mystery.SecretKnown(sp.id);
                    var sb = Box(cell, known ? new Color(0.24f, 0.2f, 0.34f, 0.92f) : Glass3, 14, false); Pad(sb, 6, 8); sb.style.marginTop = 6;
                    L(sb, known ? "ความลับ: " + sec.text : "ความลับ: ??? (" + sec.hint + ")", 18, known ? new Color(0.82f, 0.75f, 1f) : Sub, false);
                }
            }
            dexTitle.text = $"สมุดสะสมปลา · {got}/{total}";
            dexCount.text = $"{FishDex.Discovered}/{DexTotal}";
        }

        // ---------- API ให้ระบบตกปลาเรียก ----------
        public bool CatchCardOpen => card.style.display == DisplayStyle.Flex;
        /// ปิดการ์ดปลา: alt = กดปุ่มที่สอง (ปล่อยคืนน้ำ)
        public void CloseCard(bool alt = false)
        {
            if (!CatchCardOpen) return;
            card.style.display = DisplayStyle.None;
            GameInput.Blocked = ModalOpen;
            (alt ? onCardAlt : onCardOk)?.Invoke();
        }
        string lastHint = null;
        public void Hint(string s)
        {
            if (s == lastHint) return;
            lastHint = s; hint.text = s;
            hintBox.style.display = string.IsNullOrEmpty(s) ? DisplayStyle.None : DisplayStyle.Flex;
        }
        public void Toast(string s) { toast.text = s; toastUntil = Time.time + 2.2f; }
        public void ActionLabel(string txt, bool hot)
        {
            actTxt.text = txt;
            actBtn.style.backgroundColor = hot ? Gold : Paper;   // จังหวะสำคัญ (ปลากินเบ็ด/ตีเบ็ด) ปุ่มเป็นสีส้มเต็ม
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
            int n = tensionSegs.Length;
            for (int i = 0; i < n; i++)
            {
                float v = (i + 0.5f) / n;
                bool lit = v <= Mathf.Clamp01(tension);
                var c = v > 0.88f ? new Color(0.92f, 0.26f, 0.2f) : v > 0.65f ? new Color(0.98f, 0.8f, 0.2f) : new Color(0.3f, 0.85f, 0.45f);
                tensionSegs[i].style.backgroundColor = lit ? c : new Color(c.r, c.g, c.b, 0.16f);
                bool inZone = Mathf.Abs(v - zc) < zw / 2;   // กรอบขาว = โซนที่ควรรักษาเข็มไว้
                var bc = inZone ? new Color(1, 1, 1, 0.75f) : new Color(0, 0, 0, 0);
                tensionSegs[i].style.borderLeftColor = tensionSegs[i].style.borderRightColor = bc;
                progSegs[i].style.backgroundColor = v <= Mathf.Clamp01(prog) ? new Color(0.35f, 0.75f, 1f) : new Color(0.35f, 0.75f, 1f, 0.14f);
            }
            fightBox.style.translate = new Translate(danger ? Mathf.Sin(Time.time * 60) * 5 : 0, 0);
        }
        public void Strike(bool show, float pos, float center, float width, int hits, int need, int misses)
        {
            strikeBox.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;
            sMark.style.left = Length.Percent(pos * 100);
            sZone.style.left = Length.Percent((center - width / 2) * 100); sZone.style.width = Length.Percent(width * 100);
            sHits.text = $"ตีโดน {hits}/{need}" + (misses > 0 ? " · พลาด 1" : "");
        }
        public void Alert(bool show, Vector3 world, bool rare)
        {
            alertBg.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show || cam == null) return;
            var sp = cam.WorldToScreenPoint(world);
            var p = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(sp.x, Screen.height - sp.y));
            alertBg.style.left = p.x - 48; alertBg.style.top = p.y - 48;
            alert.text = rare ? "!!" : "!";
        }
        /// การ์ด "ได้ปลาแล้ว!" · okText/onOk = ปุ่มหลัก · altText/onAlt = ปุ่มที่สอง (ข้องเต็ม: ปล่อยคืนน้ำ)
        public void ShowCatch(Catch c, Texture image, bool isNew, string okText, Action onOk, string altText = null, Action onAlt = null, string note = null)
        {
            var sp = c.species;
            cardImg.style.backgroundImage = image is RenderTexture rt ? Background.FromRenderTexture(rt) : default;
            cardImg.style.display = image != null ? DisplayStyle.Flex : DisplayStyle.None;
            cardNew.style.display = isNew ? DisplayStyle.Flex : DisplayStyle.None;
            if (dexCount != null) dexCount.text = $"{FishDex.Discovered}/{DexTotal}";
            cardName.text = sp.name;
            cardStars.text = $"{FishDatabase.Rarity[sp.stars]} · {sp.stars} ดาว";
            cardKg.text = $"{c.kg:0.00} กก.";
            cardPrice.text = Story.Baht(Story.Value(new BasketFish { id = sp.id, kg = c.kg })) + (Story.S.hot == sp.id ? " ราคาพิเศษ!" : "");
            cardDesc.text = string.IsNullOrEmpty(note) ? sp.desc : note + "\n" + sp.desc;
            cardOk.text = okText; onCardOk = onOk;
            cardAlt.text = altText ?? ""; onCardAlt = onAlt;
            cardAlt.style.display = altText != null ? DisplayStyle.Flex : DisplayStyle.None;
            cardAlt.SetEnabled(altText != null);
            GameInput.Blocked = true;
            card.style.display = DisplayStyle.Flex;
            actBtn.style.display = DisplayStyle.None;
            toastUntil = 0;
        }

        void Update()
        {
            toastBox.style.display = Time.time < toastUntil && !CardOpen ? DisplayStyle.Flex : DisplayStyle.None;
            // จอแนวนอนบนมือถือ: คำแนะนำอยู่ล่างสุดระหว่างจอยกับปุ่ม (ไม่บังตัวละคร) · จอแนวตั้ง: อยู่เหนือปุ่ม
            bool portrait = Screen.height > Screen.width;
            // จอเตี้ย (มือถือแนวนอน): ลดความสูงรูปปลาบนการ์ดไม่ให้การ์ดล้นจอ
            if (!float.IsNaN(root.layout.height)) cardImg.style.height = root.layout.height < 820 ? 150 : 300;
            hintRow.style.bottom = TouchUi && portrait ? 300 : 26;
            // จอแนวนอนเตี้ย: แถบสู้ปลา/ตีเบ็ดชิดขอบบนให้เห็นทุ่น · ข้อความแจ้งเลื่อนลงใต้แถบเมื่อแถบเปิดอยู่
            strikeRow.style.top = portrait ? 170 : 16;
            fightRow.style.top = portrait ? 260 : 150;   // เกจแนวตั้งด้านขวา ใต้ปุ่มสมุด
            bool panelUp = strikeBox.style.display == DisplayStyle.Flex;
            bool narrow = !float.IsNaN(root.layout.width) && root.layout.width < 1500;   // จอแคบ: ข้อความแจ้งเลื่อนลงใต้แผงเวลา/อากาศ
            toastRow.style.top = panelUp ? (portrait ? 420 : 250) : narrow ? 170 : 96;
            // ตกปลาอยู่และไม่มีหน้าต่างเปิด → แสดงปุ่มควบคุม · อยู่บ้าน/มีการ์ด/หน้าต่าง → ซ่อน
            bool play = !CardOpen && !ModalOpen && Story.Phase == StoryPhase.Fishing;
            var hide = play ? DisplayStyle.Flex : DisplayStyle.None;
            if (dexBtn != null) dexBtn.style.display = CardOpen || ModalOpen ? DisplayStyle.None : DisplayStyle.Flex;
            if (joy != null) joy.style.display = hide;
            if (jumpBtn != null) jumpBtn.style.display = hide;
            actBtn.style.display = hide;
            hintRow.style.display = hide;
            UpdateStoryUi();
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
