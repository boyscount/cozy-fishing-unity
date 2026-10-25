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
        FontDefinition font, fontBold, fontTitle;
        float toastUntil;
        Action onCardOk, onCardAlt;
        Button cardOk, cardAlt;
        VisualElement countBoxRef;

        void Awake()
        {
            font = FontDefinition.FromFont(Resources.Load<Font>("Fonts/Mali-SemiBold"));
            fontBold = FontDefinition.FromFont(Resources.Load<Font>("Fonts/Mali-Bold"));
            var charm = Resources.Load<Font>("Fonts/Charm-Bold");   // หัวข้อ/ชื่อคน: ฟอนต์ไทยแบบตัวเขียนโบราณ อ่านง่าย
            fontTitle = fontBold;   // ใช้ฟอนต์ Mali ชุดเดียวทั้งเกม (ไทย/อังกฤษหน้าตาเหมือนกันหมด) · Charm เก็บไว้เผื่อใช้ภายหลัง
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
            gameObject.AddComponent<TextLocalizer>().Init(root);   // แปลข้อความบนจอเป็นภาษาที่เลือก (ไทย/อังกฤษ)
            // หมุนจอ/เปลี่ยนขนาด: จัดหน้าแรกใหม่ · หน้าเช้าวาดใหม่ตามความกว้าง
            root.RegisterCallback<GeometryChangedEvent>(e =>
            {
                bool n = Narrow; if (n == lastNarrow) return; lastNarrow = n;
                // สมุดปลา: จอแคบปุ่มด่านเล็กลง เรียงได้หลายปุ่มต่อแถว (เดิมแถวละปุ่ม กินครึ่งจอ)
                if (dexTabs != null) foreach (var tb in dexTabs.Children()) { tb.style.fontSize = n ? 18 : 24; Pad(tb, n ? 6 : 10, n ? 14 : 30); tb.style.marginRight = n ? 5 : 8; tb.style.marginBottom = n ? 5 : 6; tb.style.minHeight = n ? 38 : 0; }
                if (iconBar != null)
                {   // จอแคบ: จอสัมผัสไม่ย่อปุ่ม (นิ้วกดยาก) แต่ขึ้นแถวใหม่แทน · เมาส์ย่อลงนิดหน่อยเหมือนเดิม
                    iconBar.style.scale = new Scale(Vector3.one * (n && !TouchUi ? 0.78f : 1f)); iconBar.style.transformOrigin = new TransformOrigin(Length.Percent(100), 0);
                    iconBar.style.flexWrap = n ? Wrap.Wrap : Wrap.NoWrap; iconBar.style.maxWidth = n ? new StyleLength(Length.Percent(62)) : new StyleLength(StyleKeyword.None);
                }
                if (hintBox != null) hintBox.style.maxWidth = n ? Length.Percent(74) : new StyleLength(TouchUi ? 680 : 780);
                cardNameSize = n ? 44 : 62; if (cardName != null) FitCardName();
                LayoutCard(n);
                if (lineNum != null && actBtn != null)
                {   // ป้ายระยะสาย/ความลึก: จอกว้างอยู่ซ้ายของปุ่มหลัก · จอแคบ (มือถือแนวตั้ง) ที่ไม่พอ → อยู่เหนือปุ่มหลักแทน
                    var lb = lineNum.parent; float sz = actBtn.resolvedStyle.width > 0 ? actBtn.resolvedStyle.width : (TouchUi ? 230 : 170);
                    bool roomLeft = root.layout.width - (40 + sz + 22) > 520;
                    lb.style.right = roomLeft ? 40 + sz + 22 : 30; lb.style.bottom = roomLeft ? 40 + sz * 0.5f - 26 : 40 + sz + 34;
                }
                LayoutTitle();
                if (Shown(morning)) RefreshMorning();
            });
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
        /// จอแคบ (มือถือแนวตั้ง · iPad แนวตั้ง): ความกว้างหน้าจอ UI น้อยกว่า 1000 หน่วย → จัดแบบซ้อนแนวตั้ง
        bool Narrow => root != null && root.layout.width > 0 && root.layout.width < 1000;
        bool lastNarrow;

        VisualElement Bar(VisualElement parent, float height, Color bg)
        {
            var b = Box(parent, bg, height / 2, false);
            b.style.height = height; b.style.overflow = Overflow.Hidden; b.style.marginTop = 8;
            return b;
        }

        Label moneyLbl;
        // มุมบุคคลที่หนึ่งถือปลาโชว์อยู่: การ์ดย้ายลงล่าง พื้นหลังจางลง ไม่ต้องมีรูปปลาในการ์ด (เห็นตัวจริงตรงหน้า)
        bool showcase3D;
        public bool Showcase3D
        {
            get => showcase3D;
            set
            {
                if (showcase3D == value || card == null) return;
                showcase3D = value;
                card.style.justifyContent = value ? Justify.FlexEnd : Justify.Center;
                card.style.alignItems = value ? Align.FlexEnd : Align.Center;        // การ์ดชิดขวา เว้นซ้ายให้มือกับปลา
                card.style.paddingRight = value ? 18 : 0;
                card.ElementAt(0).style.maxWidth = value ? Length.Percent(Narrow ? 96 : 58) : new StyleLength(980);   // จอแคบ: การ์ดเต็มกว้างด้านล่าง ปลาอยู่ครึ่งบน
                card.style.paddingBottom = value ? 24 : 0;
                card.style.backgroundColor = new Color(0.02f, 0.03f, 0.04f, value ? 0.12f : 0.55f);
                if (value) cardImg.style.display = DisplayStyle.None;
            }
        }
        VisualElement actIco;
        // ไอคอนเล็ก (Resources/UI/Icons/<name>.png)
        static VisualElement Ico(VisualElement parent, string name, float size)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.width = e.style.height = size; e.style.flexShrink = 0;
            e.style.backgroundImage = Resources.Load<Texture2D>("UI/Icons/" + name);
            parent.Add(e);
            return e;
        }
        /// ไอคอนด้านซ้ายในปุ่ม (ย้อมสีเดียวกับตัวหนังสือ) · เว้นที่ให้ข้อความ
        static VisualElement ButtonIcon(Button b, string name, float size, Color tint)
        {
            var ic = Ico(b, name, size); ic.style.position = Position.Absolute; ic.style.left = 12; ic.style.top = Length.Percent(50); ic.style.marginTop = -size / 2;
            ic.style.unityBackgroundImageTintColor = tint; b.style.paddingLeft = size + 20; return ic;
        }
        static void SetIco(VisualElement e, string name) => e.style.backgroundImage = Resources.Load<Texture2D>("UI/Icons/" + name);
        // ผิวกรอบจากภาพ (Resources/UI/Frames/<name>) แบบ 9 ส่วน: ขอบไม่ยืด ตรงกลางยืดตามขนาดกล่อง
        static void Skin(VisualElement e, string frame, int slice)
        {
            var t = Resources.Load<Texture2D>("UI/Frames/" + frame);
            if (t == null) return;
            e.style.backgroundImage = t; e.style.backgroundColor = Color.clear;
            e.style.unitySliceLeft = e.style.unitySliceRight = e.style.unitySliceTop = e.style.unitySliceBottom = slice;
            e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = 0;
        }

        void Build()
        {
            // ข้อง + เงิน แบบไอคอน (ตะกร้า 1/8 · เหรียญ ฿0)
            var countBox = countBoxRef = Box(root, Paper, 12); Abs(countBox, 24, null, 24); Pad(countBox, 10, 22); Skin(countBox, "wood", 18);   // ป้ายไม้เก่า
            countBox.style.flexDirection = FlexDirection.Row; countBox.style.alignItems = Align.Center;
            Ico(countBox, "basket", 38);   // ข้องใส่ปลา
            count = L(countBox, "0/8", 28, Ink); count.style.marginLeft = 6;
            Ico(countBox, "coin", 32).style.marginLeft = 16;   // เหรียญโบราณ
            moneyLbl = L(countBox, "฿0", 28, Money); moneyLbl.style.marginLeft = 6;

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
            hintBox = Box(hintRow, Paper, 26); Pad(hintBox, 14, 44); hintBox.style.maxWidth = TouchUi ? 680 : 780; Skin(hintBox, "plank", 22);
            hint = L(hintBox, "", 28, new Color(0.22f, 0.13f, 0.05f));

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
            bool deep = DepthReading >= 0;
            if (deep) m = DepthReading;
            bool show = m >= 0 && !CardOpen && !ModalOpen;
            lineNum.parent.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;
            lineNum.text = deep ? m.ToString("0.0") : Mathf.RoundToInt(m).ToString();
            lineNum.style.fontSize = deep ? 24 : 32;
            lineUnit.text = Loc.T(deep ? "ม. ลึก" : "ม.");   // ตั้งทุกเฟรม → แปลตรงนี้เลย (ตัวแปลอัตโนมัติไล่ตามไม่ทัน ไทยจะกะพริบ)
        }
        /// UnderwaterView ตั้งตอนดำลงใต้น้ำ (ความลึกน้ำตรงทุ่น) · -1 = แสดงระยะสายตามปกติ
        public float DepthReading = -1;

        static void Shadow(Label l) { l.style.textShadow = new TextShadow { offset = new Vector2(0, 2), blurRadius = 6, color = new Color(0, 0, 0, 0.75f) }; }

        void BuildCard()
        {
            card = new VisualElement(); Abs(card, 0, 0, 0, 0); card.style.width = Length.Percent(100); card.style.height = Length.Percent(100);
            card.style.backgroundColor = new Color(0.02f, 0.03f, 0.04f, 0.55f);
            card.style.justifyContent = Justify.Center; card.style.alignItems = Align.Center; root.Add(card);
            var cb = Box(card, Paper, 16); MaxW(cb, 980); cb.pickingMode = PickingMode.Position;
            Skin(cb, "bamboo_paper", 26); cb.style.paddingTop = 28; cb.style.paddingBottom = 30; cb.style.paddingLeft = 36; cb.style.paddingRight = 34;
            cb.style.alignItems = Align.Stretch;
            // ภาพปลา 3D หมุนช้า ๆ เต็มแถบ + ป้าย "ใหม่!" มุมขวา
            cardImg = new VisualElement { pickingMode = PickingMode.Ignore }; cardImg.style.height = 300; cardImg.style.flexShrink = 0;
            cardImg.style.backgroundColor = new Color(0.1f, 0.18f, 0.22f, 0.9f);
            cardImg.style.borderTopLeftRadius = cardImg.style.borderTopRightRadius = cardImg.style.borderBottomLeftRadius = cardImg.style.borderBottomRightRadius = 10;
            cardImg.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            cb.Add(cardImg);
            cardNew = Box(cardImg, Gold, 6, false); Abs(cardNew, null, 12, 12); Pad(cardNew, 4, 14);
            L(cardNew, "ใหม่! บันทึกลงสมุด", 20, Color.white);
            var head = cardHead = new VisualElement { pickingMode = PickingMode.Ignore }; head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.FlexEnd; head.style.marginTop = 12; head.style.flexShrink = 0; cb.Add(head);
            head.style.flexWrap = Wrap.Wrap;   // ชื่อปลายาวมาก (ปลากระเบนราหูน้ำจืด): ป้ายน้ำหนัก/ราคาลงไปบรรทัดใหม่ ไม่ทับชื่อ
            var names = new VisualElement { pickingMode = PickingMode.Ignore }; names.style.flexGrow = 1; names.style.flexShrink = 0; head.Add(names);   // ไม่หด: ชื่อยาวแล้วป้ายราคาขึ้นบรรทัดใหม่แทน (หดแล้วชื่อโดนตัดท้าย)
            var rarRow = new VisualElement { pickingMode = PickingMode.Ignore }; rarRow.style.flexDirection = FlexDirection.Row; rarRow.style.alignItems = Align.Center; names.Add(rarRow);
            cardStars = L(rarRow, "", 24, new Color(0.62f, 0.32f, 0.08f)); cardStars.style.unityTextAlign = TextAnchor.MiddleLeft; cardStars.style.whiteSpace = WhiteSpace.NoWrap;
            cardStarIco = new VisualElement[4];   // ดาวไม้ (ฟอนต์ไทยไม่มีตัว ★)
            for (int i = 0; i < 4; i++) { cardStarIco[i] = Ico(rarRow, "star_off", 26); cardStarIco[i].style.marginLeft = i == 0 ? 10 : 2; }
            cardName = L(names, "", 62, new Color(0.2f, 0.11f, 0.04f)); cardName.style.unityTextAlign = TextAnchor.MiddleLeft; cardName.style.unityFontDefinition = fontTitle;
            cardName.style.whiteSpace = WhiteSpace.NoWrap;
            var stats = new VisualElement { pickingMode = PickingMode.Ignore }; stats.style.flexDirection = FlexDirection.Row; stats.style.flexShrink = 0; head.Add(stats);
            var kgBox = Stat(stats); Skin(kgBox, "wood", 18); var prBox = Stat(stats); Skin(prBox, "wood", 18);
            cardKg = L(kgBox, "", 30, new Color(1f, 0.93f, 0.78f)); cardPrice = L(prBox, "", 30, Money);
            // พบเบาะแส: แถบแว่นขยายกระพริบเหนือคำอธิบาย
            cardNote = new VisualElement { pickingMode = PickingMode.Ignore }; cardNote.style.flexDirection = FlexDirection.Row; cardNote.style.alignItems = Align.Center; cardNote.style.marginTop = 10;
            cardNote.style.backgroundColor = new Color(0.55f, 0.16f, 0.08f, 0.14f); Pad(cardNote, 6, 12); Radius(cardNote, 10); cb.Add(cardNote);
            Ico(cardNote, "magnifier", 34);
            cardNoteTxt = L(cardNote, "", 22, new Color(0.55f, 0.12f, 0.06f)); cardNoteTxt.style.unityTextAlign = TextAnchor.MiddleLeft; cardNoteTxt.style.marginLeft = 8; cardNoteTxt.style.flexShrink = 1;
            cardDesc = L(cb, "", 22, new Color(0.3f, 0.2f, 0.1f), false); cardDesc.style.marginTop = 10; cardDesc.style.unityTextAlign = TextAnchor.MiddleLeft;
            var btns = new VisualElement { pickingMode = PickingMode.Ignore }; btns.style.flexDirection = FlexDirection.Row; btns.style.justifyContent = Justify.Center; btns.style.marginTop = 16; btns.style.flexShrink = 0; cb.Add(btns);
            cardAlt = new Button(() => CloseCard(true)) { text = "ปล่อย" };
            StyleButton(cardAlt, Gray); cardAlt.style.minWidth = 240; cardAlt.style.fontSize = 30; btns.Add(cardAlt);
            Skin(cardAlt, "plank", 22); cardAlt.style.unityBackgroundImageTintColor = new Color(0.72f, 0.7f, 0.68f); cardAlt.style.color = new Color(0.2f, 0.16f, 0.12f); Pad(cardAlt, 14, 30);
            cardOk = new Button(() => CloseCard(false)) { text = "เก็บ" };
            StyleButton(cardOk, Gold); cardOk.style.minWidth = 300; cardOk.style.fontSize = 30; cardOk.style.marginLeft = 14; btns.Add(cardOk);
            Skin(cardOk, "plank", 22); cardOk.style.unityBackgroundImageTintColor = new Color(1f, 0.62f, 0.32f); cardOk.style.color = new Color(0.22f, 0.08f, 0.02f); Pad(cardOk, 14, 30);
            cardOk.style.paddingLeft = 66; var okIco = Ico(cardOk, "basket", 40); Abs(okIco, 16, null, 8);   // ข้องจิ๋วหน้าปุ่มเก็บ
            card.style.display = DisplayStyle.None;
        }

        VisualElement cardNote, cardHead; Label cardNoteTxt; VisualElement[] cardStarIco;
        // จอแคบ: น้ำหนัก/ราคาลงไปอยู่ใต้ชื่อปลา (ไม่ทับชื่อ)
        void LayoutCard(bool n)
        {
            if (cardHead == null) return;
            cardHead.style.flexDirection = n ? FlexDirection.Column : FlexDirection.Row;
            cardHead.style.alignItems = n ? Align.FlexStart : Align.FlexEnd;
            var stats = cardHead.ElementAt(cardHead.childCount - 1); stats.style.marginTop = n ? 6 : 0; stats.ElementAt(0).style.marginLeft = n ? 0 : 10;
            // ปุ่มปล่อย/เก็บ: จอแคบตัวเล็กลง ไม่ให้ข้อความยาว (อังกฤษ) ล้นไปทับไอคอนข้อง
            cardAlt.style.fontSize = cardOk.style.fontSize = n ? 24 : 30;
            cardAlt.style.minWidth = n ? 150 : 240; cardOk.style.minWidth = n ? 220 : 300;
            // ความกว้างตายตัวบนจอแคบ (ขนาดที่วัดจากข้อความไทยก่อนแปลเล็กเกิน ข้อความอังกฤษล้นปุ่ม)
            cardAlt.style.width = n ? 190 : StyleKeyword.Auto; cardOk.style.width = n ? 370 : StyleKeyword.Auto;
            cardAlt.style.paddingRight = cardOk.style.paddingRight = n ? 18 : 30; cardAlt.style.paddingLeft = n ? 18 : 30;
            cardAlt.style.flexShrink = cardOk.style.flexShrink = 0;
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
            // ระยะสาย/ความลึก: ป้ายเล็กทางซ้ายของปุ่มหลัก (เดิมอยู่เหนือปุ่ม ทับปลายเกจสู้ปลากับกล่องบันทึก)
            var lineBox = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(lineBox, null, 40 + size + 22, null, 40 + size * 0.5f - 26); lineBox.style.alignItems = Align.Center;
            // ป้ายเล็กแนวนอน (ตัวเลข + หน่วย) ไม่กินที่จอ
            lineBox.style.flexDirection = FlexDirection.Row; lineBox.style.alignItems = Align.FlexEnd;
            lineBox.style.paddingTop = lineBox.style.paddingBottom = 10; lineBox.style.paddingLeft = lineBox.style.paddingRight = 16; Skin(lineBox, "ropeframe", 20); root.Add(lineBox);
            lineNum = L(lineBox, "0", 32, new Color(1f, 0.92f, 0.75f)); Shadow(lineNum);
            lineUnit = L(lineBox, "ม.", 16, new Color(0.95f, 0.85f, 0.65f)); Shadow(lineUnit); lineUnit.style.marginLeft = 4; lineUnit.style.marginBottom = 3;
            lineBox.style.display = DisplayStyle.None;
            actRing = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(actRing, null, 40 - 16, null, 40 - 16);
            actRing.style.width = actRing.style.height = size + 32; actRing.generateVisualContent += DrawRing; root.Add(actRing);
            actBtn = Box(root, Paper, size / 2); Abs(actBtn, null, 40, null, 40);
            actBtn.style.width = actBtn.style.height = size; actBtn.pickingMode = PickingMode.Position;
            actBtn.style.justifyContent = Justify.Center; actBtn.style.alignItems = Align.Center;
            Skin(actBtn, "act_wood", 0); actBtn.style.unitySliceLeft = actBtn.style.unitySliceRight = actBtn.style.unitySliceTop = actBtn.style.unitySliceBottom = 0;
            strikeTag = L(root, "วัดเบ็ด!", TouchUi ? 40 : 32, new Color(1f, 0.85f, 0.4f)); Shadow(strikeTag); Abs(strikeTag, null, 40 + size + 24, null, 40 + size * 0.38f);
            strikeTag.style.unityFontDefinition = fontTitle; strikeTag.style.display = DisplayStyle.None;
            actIco = Ico(actBtn, "cast", size * 0.46f);
            actTxt = L(actBtn, "", TouchUi ? 34 : 28, Color.white); Shadow(actTxt); actTxt.style.display = DisplayStyle.None;
            if (!TouchUi) { actKey = L(actBtn, "Space", 20, new Color(1, 1, 1, 0.85f)); actKey.style.marginTop = 2; }
            actBtn.RegisterCallback<PointerDownEvent>(e => { actBtn.CapturePointer(e.pointerId); GameInput.ButtonHeld = true; actBtn.style.scale = new Scale(new Vector3(0.94f, 0.94f, 1)); });
            actBtn.RegisterCallback<PointerUpEvent>(e => { actBtn.ReleasePointer(e.pointerId); GameInput.ButtonHeld = false; actBtn.style.scale = new Scale(Vector3.one); });
        }

        // วงเกจรอบปุ่มหลัก: ปกติวงส้มจาง ๆ · จังหวะสำคัญ (ปลากิน/ตีเบ็ด) ไล่ส้ม→เขียวเต็มวงและกระพริบ
        VisualElement actRing; Label strikeTag; bool ringHot;
        void DrawRing(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D; var r = actRing.contentRect; var c = r.center; float rad = r.width * 0.5f - 7;
            p.lineWidth = 9; p.lineCap = LineCap.Round;
            p.strokeColor = new Color(0.1f, 0.07f, 0.04f, 0.55f); p.BeginPath(); p.Arc(c, rad, 0, 360); p.Stroke();
            if (!ringHot) { p.strokeColor = new Color(0.95f, 0.55f, 0.18f, 0.75f); p.BeginPath(); p.Arc(c, rad, 135, 405); p.Stroke(); return; }
            p.strokeColor = new Color(0.98f, 0.55f, 0.15f); p.BeginPath(); p.Arc(c, rad, 100, 260); p.Stroke();
            p.strokeColor = new Color(0.3f, 0.9f, 0.35f); p.BeginPath(); p.Arc(c, rad, 260, 460); p.Stroke();
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
            Skin(joy, "compass", 0); joy.style.opacity = 0.85f;
            knob = Box(joy, Paper, 60); Abs(knob, 70, null, 70); knob.style.width = knob.style.height = 120; Skin(knob, "knob", 0);
            knob.style.alignItems = Align.Center; knob.style.justifyContent = Justify.Center;
            Ico(knob, "move", 70).style.unityBackgroundImageTintColor = new Color(1, 1, 1, 0.7f);
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
            float d = TouchUi ? 100 : 80;   // จอสัมผัส: ปุ่มใหญ่ขึ้นให้นิ้วกดโดนง่าย
            col.style.alignItems = Align.Center; col.style.marginLeft = TouchUi ? 8 : 14; col.style.width = d + 4; col.style.marginBottom = 4;
            var disc = Box(col, new Color(0.08f, 0.1f, 0.09f, 0.72f), 38, false);
            disc.style.width = disc.style.height = d; disc.style.alignItems = Align.Center; disc.style.justifyContent = Justify.Center;
            Skin(disc, "btn_wood", 0);   // แผ่นไม้กลมขอบทองเหลือง (สีวงเดิมไม่ใช้แล้ว)
            var img = new VisualElement { pickingMode = PickingMode.Ignore };
            img.style.width = img.style.height = TouchUi ? 58 : 46; img.style.backgroundImage = Resources.Load<Texture2D>("UI/Icons/" + icon);
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
            var book = Box(dex, Paper, 40); MaxW(book, 1040); book.style.maxHeight = Length.Percent(94); book.pickingMode = PickingMode.Position;
            Skin(book, "book", 40); book.style.paddingTop = 34; book.style.paddingBottom = 34; book.style.paddingLeft = 52; book.style.paddingRight = 52;
            var head = new VisualElement { pickingMode = PickingMode.Ignore }; head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center; head.style.flexShrink = 0; book.Add(head);
            dexTitle = L(head, "สมุดสะสมปลา", 42, new Color(0.22f, 0.12f, 0.05f)); dexTitle.style.flexGrow = 1; dexTitle.style.unityTextAlign = TextAnchor.MiddleLeft; dexTitle.style.unityFontDefinition = fontTitle;
            var close = new VisualElement { pickingMode = PickingMode.Position }; close.style.width = close.style.height = 84;   // ตราครั่งแดง "ปิด"
            close.style.backgroundImage = Resources.Load<Texture2D>("UI/Icons/seal"); head.Add(close);
            close.RegisterCallback<PointerUpEvent>(e => { dex.style.display = DisplayStyle.None; GameInput.Blocked = false; });
            var tabs = dexTabs = new VisualElement { pickingMode = PickingMode.Ignore }; tabs.style.flexDirection = FlexDirection.Row; tabs.style.flexWrap = Wrap.Wrap; tabs.style.marginTop = 10; tabs.style.flexShrink = 0; book.Add(tabs);
            foreach (var (id, name) in DexStages)
            {
                var tab = new Button(() => { dexStage = id; FillDex(); }) { text = name, name = "tab-" + id };
                StyleButton(tab, Green); tab.style.fontSize = 24; Pad(tab, 10, 30); tab.style.marginRight = 8; tab.style.marginBottom = 6;
                Skin(tab, "tab_bamboo", 8); tab.style.color = new Color(0.24f, 0.15f, 0.06f);
                tabs.Add(tab);
            }
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden; scroll.style.flexGrow = 1; scroll.style.flexShrink = 1; scroll.style.marginTop = 8; book.Add(scroll);
            dexGrid = new VisualElement(); dexGrid.style.flexDirection = FlexDirection.Row; dexGrid.style.flexWrap = Wrap.Wrap; dexGrid.style.justifyContent = Justify.Center;
            scroll.Add(dexGrid);
            dex.style.display = DisplayStyle.None;
        }

        VisualElement dexTabs;
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
                if (tab != null) tab.style.unityBackgroundImageTintColor = id == dexStage ? new Color(1f, 0.6f, 0.3f) : Color.white;
            }
            foreach (var sp in FishDatabase.ForStage(dexStage))
            {
                total++;
                bool has = FishDex.Has(sp.id); if (has) got++;
                var cell = new VisualElement { pickingMode = PickingMode.Ignore }; dexGrid.Add(cell); Skin(cell, "parchment", 18); Pad(cell, 14, 14);
                cell.style.width = 290; cell.style.marginLeft = cell.style.marginRight = 8; cell.style.marginBottom = 16;
                var photo = new VisualElement { pickingMode = PickingMode.Ignore }; photo.style.backgroundColor = new Color(0.96f, 0.94f, 0.88f); Pad(photo, 8, 8); photo.style.flexShrink = 0; cell.Add(photo);
                var clip = Ico(cell, "paperclip", 34); Abs(clip, 14, null, -8); clip.style.height = 74;
                var img = new VisualElement { pickingMode = PickingMode.Ignore }; img.style.height = 120; img.style.flexShrink = 0;
                img.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                if (sp.Photo != null) { img.style.backgroundImage = Background.FromTexture2D(sp.Photo); img.style.unityBackgroundImageTintColor = has ? Color.white : new Color(0, 0, 0, 0.85f); }   // ยังไม่เคยตก = เงาดำ
                else if (preview != null) img.style.backgroundImage = Background.FromRenderTexture(preview.Icon(sp, !has));
                photo.Add(img);
                if (!has) { var lk = Ico(cell, "lock", 44); Abs(lk, null, 10, 96); lk.style.height = 56; }
                var ink = new Color(0.24f, 0.14f, 0.06f);
                var nm = L(cell, has ? sp.name : "???", 32, ink); nm.style.unityFontDefinition = fontTitle; nm.style.marginTop = 6; nm.style.whiteSpace = WhiteSpace.NoWrap;
                var rr = new VisualElement { pickingMode = PickingMode.Ignore }; rr.style.flexDirection = FlexDirection.Row; rr.style.justifyContent = Justify.Center; rr.style.alignItems = Align.Center; cell.Add(rr);
                L(rr, FishDatabase.Rarity[sp.stars], 20, new Color(0.6f, 0.32f, 0.08f)).style.marginRight = 6;
                for (int k = 0; k < 4; k++) Ico(rr, k < sp.stars ? "star_on" : "star_off", 20);
                L(cell, has ? $"ได้ {FishDex.Count(sp.id)} ตัว · หนักสุด {FishDex.BestKg(sp.id):0.00} กก." : (CozyFishing.Stages.Unlocked(sp.stage) ? "ยังไม่เคยตกได้" : "ด่านนี้ยังไม่เปิด"), 20, new Color(0.35f, 0.24f, 0.12f), false);
                if (has && sp.Credit != null) L(cell, sp.Credit, 14, new Color(0.45f, 0.36f, 0.26f), false).style.marginTop = 2;   // เครดิตภาพถ่าย (CC BY / BY-SA ต้องให้เครดิต)
                // ความลับของปลา: ปลดล็อกแล้วเห็นข้อความ · ยังไม่ปลดล็อกเห็นเงื่อนไข
                var sec = Mystery.SecretOf(sp.id);
                if (sec != null)
                {
                    bool known = Mystery.SecretKnown(sp.id);
                    var sb = Box(cell, known ? new Color(0.3f, 0.16f, 0.1f, 0.9f) : new Color(0.22f, 0.17f, 0.12f, 0.82f), 12, false); Pad(sb, 6, 10); sb.style.marginTop = 6;
                    L(sb, known ? "ความลับ: " + sec.text : "ความลับ: ??? (" + sec.hint + ")", 18, known ? new Color(1f, 0.85f, 0.6f) : new Color(0.9f, 0.84f, 0.74f), false);
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
        /// ปุ่มหลัก: ไอคอนตามจังหวะ · ข้อความสั้นแสดงเฉพาะจังหวะสำคัญ (ปลากินเบ็ด/ตีเบ็ด)
        public void ActionLabel(string txt, bool hot, string icon = null)
        {
            if (icon != null) SetIco(actIco, icon);
            actIco.style.display = icon != null ? DisplayStyle.Flex : DisplayStyle.None;
            actTxt.text = txt;
            actTxt.style.display = string.IsNullOrEmpty(txt) ? DisplayStyle.None : DisplayStyle.Flex;
            actBtn.style.unityBackgroundImageTintColor = hot ? new Color(1.25f, 1f, 0.7f) : Color.white;   // จังหวะสำคัญ: ไม้สว่างขึ้น + วงเกจเขียว + ป้าย "วัดเบ็ด!"
            if (hot != ringHot) { ringHot = hot; actRing.MarkDirtyRepaint(); }
            strikeTag.style.display = hot && actBtn.style.display != DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
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
        // ชื่อปลายาว (ปลาช่อนงูเห่า · ปลาผีเสื้อถ้ำตาบอด) ย่อฟอนต์ไม่ให้ทับป้ายน้ำหนัก/ราคา
        float cardNameSize = 62;
        void FitCardName() { int len = cardName.text?.Length ?? 0; cardName.style.fontSize = cardNameSize * Mathf.Clamp(9f / Mathf.Max(9, len), 0.62f, 1f); }   // ไม่เล็กจนอ่านยาก · ยาวกว่านั้นป้ายราคาขึ้นบรรทัดใหม่แทน

        // ทุกปุ่มบอกได้ว่ากดได้: ชี้แล้วขยายนิด + สว่างขึ้น · กดแล้วยุบ (จอสัมผัสเห็นตอนแตะ) — สแกนหาปุ่มใหม่เป็นระยะ ไม่ต้องตามใส่ทีละที่
        float nextPressScan;
        const string PressTag = "pressable";
        static void Pressable(VisualElement e)
        {
            if (e.ClassListContains(PressTag)) return;
            e.AddToClassList(PressTag);
            if (e.style.transitionDuration.keyword == StyleKeyword.Null || e.style.transitionDuration.value == null || e.style.transitionDuration.value.Count == 0)
            {
                e.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("scale"), new StylePropertyName("opacity") };
                e.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(0.1f) };
            }
            bool over = false;   // ไม่แตะสี/tint (หลายปุ่มตั้งสีเฉพาะไว้ เช่นปุ่มร้านสีส้ม ไฟฉายเปิด/ปิด) ใช้ขนาดบอกอย่างเดียว
            e.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (!e.enabledInHierarchy) return; over = true;
                if (e.style.scale.value.value.x < 1.01f) e.style.scale = new Scale(Vector3.one * 1.05f);
            });
            e.RegisterCallback<PointerLeaveEvent>(_ => { if (!over) return; over = false; e.style.scale = new Scale(Vector3.one); });
            e.RegisterCallback<PointerDownEvent>(_ => { if (e.enabledInHierarchy) e.style.scale = new Scale(Vector3.one * 0.94f); }, TrickleDown.TrickleDown);
            e.RegisterCallback<PointerUpEvent>(_ => e.style.scale = new Scale(Vector3.one * (over ? 1.05f : 1f)));
            e.RegisterCallback<PointerCancelEvent>(_ => { over = false; e.style.scale = new Scale(Vector3.one); });
        }

        public void ShowCatch(Catch c, Texture image, bool isNew, string okText, Action onOk, string altText = null, Action onAlt = null, string note = null)
        {
            var sp = c.species;
            // ภาพถ่ายปลาจริงคมกว่าภาพเรนเดอร์ · ไม่มีภาพถ่ายค่อยใช้ภาพเรนเดอร์โมเดล
            if (sp != null && sp.Photo != null) cardImg.style.backgroundImage = Background.FromTexture2D(sp.Photo);
            else cardImg.style.backgroundImage = image is RenderTexture rt ? Background.FromRenderTexture(rt) : default;
            cardImg.style.display = !showcase3D && (image != null || sp?.Photo != null) ? DisplayStyle.Flex : DisplayStyle.None;
            cardNew.style.display = isNew ? DisplayStyle.Flex : DisplayStyle.None;
            if (dexCount != null) dexCount.text = $"{FishDex.Discovered}/{DexTotal}";
            cardName.text = sp.name; FitCardName();
            cardStars.text = FishDatabase.Rarity[sp.stars];
            for (int i = 0; i < cardStarIco.Length; i++) SetIco(cardStarIco[i], i < sp.stars ? "star_on" : "star_off");
            cardKg.text = $"{c.kg:0.00} กก.";
            cardPrice.text = Story.Baht(Story.Value(new BasketFish { id = sp.id, kg = c.kg })) + (Story.S.hot == sp.id ? " ราคาพิเศษ!" : "");
            cardDesc.text = sp.desc;
            cardNoteTxt.text = note ?? ""; cardNote.style.display = string.IsNullOrEmpty(note) ? DisplayStyle.None : DisplayStyle.Flex;
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
            Cloud.Tick();
            if (Time.unscaledTime >= nextPressScan) { nextPressScan = Time.unscaledTime + 0.4f; root.Query<Button>().ForEach(Pressable); }
            toastBox.style.display = Time.time < toastUntil && !CardOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (CardOpen && cardNote != null && cardNote.style.display == DisplayStyle.Flex) cardNote.style.opacity = 0.65f + 0.35f * Mathf.Sin(Time.time * 5f);
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
            toastRow.style.top = panelUp ? (portrait ? 420 : 250) : narrow ? (portrait ? 250 : 170) : 96;   // แนวตั้ง: ใต้แผงเวลา/อากาศ ไม่ทับกัน
            toastBox.style.maxWidth = portrait ? new StyleLength(Length.Percent(62)) : new StyleLength(820);   // แนวตั้ง: ไม่ล้นไปทับเกจสายเบ็ดด้านขวา
            if (nightWarn != null) nightWarn.style.maxWidth = Length.Percent(portrait ? 62 : 88);
            // ตกปลาอยู่และไม่มีหน้าต่างเปิด → แสดงปุ่มควบคุม · อยู่บ้าน/มีการ์ด/หน้าต่าง → ซ่อน
            bool play = !CardOpen && !ModalOpen && Story.Phase == StoryPhase.Fishing;
            var hide = play ? DisplayStyle.Flex : DisplayStyle.None;
            if (dexBtn != null) dexBtn.style.display = CardOpen || ModalOpen ? DisplayStyle.None : DisplayStyle.Flex;
            if (joy != null) joy.style.display = hide;
            if (jumpBtn != null) jumpBtn.style.display = hide;
            actBtn.style.display = hide; actRing.style.display = hide; if (hide == DisplayStyle.None) strikeTag.style.display = DisplayStyle.None;
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
