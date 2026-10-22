using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // หน้าจอภาษาไทยทั้งหมด (UI Toolkit + ฟอนต์ Mali + Advanced Text Generator ให้สระ/วรรณยุกต์วางถูก)
    // ป้ายจำนวนปลา · คำแนะนำ · แถบแรงเหวี่ยง · แถบแรงตึงสาย · แถบตีเบ็ด · ป้าย "!" · การ์ดปลา · ปุ่มกดหลัก (ทุกจอ) · จอยบนจอสัมผัส
    // สเกล: อ้างอิงจอสี่เหลี่ยม 1080 หน่วย เฉลี่ยกว้าง/สูง (match 0.5) → แนวตั้งและแนวนอนตัวหนังสือขนาดใกล้กัน ไม่ล้นจอ
    public class Hud : MonoBehaviour
    {
        public Camera cam;
        public FishPreview preview;
        public bool TouchUi { get; private set; }
        public bool CardOpen => card.style.display == DisplayStyle.Flex || (dex != null && dex.style.display == DisplayStyle.Flex);

        static readonly Color Paper = new Color(1f, 0.973f, 0.918f, 0.96f), Ink = new Color(0.2f, 0.15f, 0.11f), Green = new Color(0.36f, 0.6f, 0.27f),
            Gold = new Color(0.93f, 0.66f, 0.15f), Edge = new Color(0.55f, 0.42f, 0.25f, 0.55f);
        UIDocument doc;
        VisualElement root, hintRow, toastRow, fightRow, strikeRow, jumpBtn, hintBox, toastBox, powerBox, powerFill, fightBox, needle, zone, progFill, strikeBox, sMark, sZone, card, joy, knob, actBtn, alertBg;
        VisualElement cardImg, cardNew, dex, dexGrid, dexBtn; Label dexCount, dexTitle; string dexStage = "paddy";
        Label count, hint, toast, alert, sHits, cardName, cardStars, cardKg, cardPrice, cardDesc, actTxt, actKey;
        FontDefinition font, fontBold;
        float toastUntil;
        Action onCardClose;

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
        static VisualElement Box(VisualElement parent, Color bg, float radius = 28, bool border = true)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.backgroundColor = bg;
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = radius;
            if (border)
            {
                e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = 3;
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
            var countBox = Box(root, Paper, 999); Abs(countBox, 24, null, 24); Pad(countBox, 8, 26);
            count = L(countBox, "ปลา 0 ตัว · ฿0", 30, Ink);

            // ข้อความแจ้งสั้น ๆ: กลางจอด้านบน
            toastRow = CenterRow(96, null);
            toastBox = Box(toastRow, Paper, 28); Pad(toastBox, 12, 28); toastBox.style.maxWidth = 820;
            toast = L(toastBox, "", 32, Ink);

            // แถบแรงตึงสาย + ระยะปลา (ด้านบน ใต้ข้อความแจ้ง)
            fightRow = CenterRow(170, null);
            fightBox = Box(fightRow, Paper, 28); Pad(fightBox, 16, 26); MaxW(fightBox, 760);
            var fr = L(fightBox, "แรงตึงสาย (อย่าให้เข็มเข้าแดง)", 26, Ink); fr.style.unityTextAlign = TextAnchor.MiddleLeft;
            var tb = Bar(fightBox, 40, new Color(0.81f, 0.89f, 0.94f));
            zone = Box(tb, new Color(0.36f, 0.62f, 0.27f, 0.65f), 0, false); Abs(zone, null, null, 0, 0);
            var red = Box(tb, new Color(0.86f, 0.32f, 0.24f, 0.9f), 0, false); Abs(red, null, 0, 0, 0); red.style.width = Length.Percent(12);
            needle = Box(tb, Ink, 5, false); Abs(needle, null, null, -2, -2); needle.style.width = 12;
            var pr = L(fightBox, "ดึงปลาเข้าฝั่ง", 24, Ink); pr.style.unityTextAlign = TextAnchor.MiddleLeft; pr.style.marginTop = 10;
            var pb = Bar(fightBox, 22, new Color(0.91f, 0.86f, 0.75f)); progFill = Box(pb, Green, 11, false); progFill.style.height = Length.Percent(100);

            // แถบตีเบ็ดปลาหายาก
            strikeRow = CenterRow(170, null);
            strikeBox = Box(strikeRow, Paper, 28); Pad(strikeBox, 16, 26); MaxW(strikeBox, 760);
            strikeBox.style.borderTopWidth = strikeBox.style.borderBottomWidth = strikeBox.style.borderLeftWidth = strikeBox.style.borderRightWidth = 6;
            strikeBox.style.borderTopColor = strikeBox.style.borderBottomColor = strikeBox.style.borderLeftColor = strikeBox.style.borderRightColor = Gold;
            L(strikeBox, "ตีเบ็ดตอนเข็มอยู่ในโซนทอง!", 28, Ink);
            sHits = L(strikeBox, "", 24, Ink);
            var sb = Bar(strikeBox, 46, new Color(0.91f, 0.86f, 0.75f));
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
            BuildButton();
            if (TouchUi) { BuildJoystick(); BuildTouchExtras(); }
        }

        static void Shadow(Label l) { l.style.textShadow = new TextShadow { offset = new Vector2(0, 2), blurRadius = 6, color = new Color(0, 0, 0, 0.75f) }; }

        void BuildCard()
        {
            card = new VisualElement(); Abs(card, 0, 0, 0, 0); card.style.width = Length.Percent(100); card.style.height = Length.Percent(100);
            card.style.backgroundColor = new Color(0.1f, 0.16f, 0.13f, 0.55f);
            card.style.justifyContent = Justify.Center; card.style.alignItems = Align.Center; root.Add(card);
            var cb = Box(card, Paper, 44); Pad(cb, 30, 40); MaxW(cb, 720); cb.pickingMode = PickingMode.Position;
            cb.style.alignItems = Align.Stretch;
            L(cb, "ได้ปลาแล้ว!", 26, new Color(0.42f, 0.31f, 0.07f));
            // รูปปลา 3D หมุนช้า ๆ (จาก FishPreview) + ป้าย "ใหม่!" เมื่อเป็นชนิดแรกในสมุด
            cardImg = new VisualElement { pickingMode = PickingMode.Ignore }; cardImg.style.height = 220; cardImg.style.flexShrink = 0; cardImg.style.marginTop = 4;
            cardImg.style.backgroundColor = new Color(0.86f, 0.93f, 0.95f); cardImg.style.borderTopLeftRadius = cardImg.style.borderTopRightRadius = cardImg.style.borderBottomLeftRadius = cardImg.style.borderBottomRightRadius = 24;
            cardImg.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            cb.Add(cardImg);
            cardNew = Box(cardImg, new Color(0.86f, 0.27f, 0.2f), 999, false); Abs(cardNew, null, 12, 12); Pad(cardNew, 4, 18);
            L(cardNew, "ใหม่! บันทึกลงสมุด", 22, Color.white);
            cardName = L(cb, "", 52, Ink); cardName.style.marginTop = 6;
            cardStars = L(cb, "", 30, Gold);
            var stats = new VisualElement { pickingMode = PickingMode.Ignore }; stats.style.flexDirection = FlexDirection.Row; stats.style.justifyContent = Justify.Center; stats.style.marginTop = 14; stats.style.flexShrink = 0; cb.Add(stats);
            cardKg = L(Stat(stats), "", 34, Ink); cardPrice = L(Stat(stats), "", 34, new Color(0.42f, 0.31f, 0.07f));
            cardDesc = L(cb, "", 26, Ink, false); cardDesc.style.marginTop = 16; cardDesc.style.marginBottom = 22;
            var ok = new Button(CloseCard) { text = "เก็บใส่ข้องปลา" };
            ok.style.unityFontDefinition = fontBold; ok.style.unityTextGenerator = TextGeneratorType.Advanced;
            ok.style.fontSize = 34; ok.style.color = Color.white; ok.style.backgroundColor = Green; Pad(ok, 16, 50);
            ok.style.borderTopLeftRadius = ok.style.borderTopRightRadius = ok.style.borderBottomLeftRadius = ok.style.borderBottomRightRadius = 999;
            ok.style.borderTopWidth = ok.style.borderBottomWidth = ok.style.borderLeftWidth = ok.style.borderRightWidth = 0;
            ok.style.alignSelf = Align.Center; ok.style.flexShrink = 0;
            cb.Add(ok);
            card.style.display = DisplayStyle.None;
        }

        VisualElement Stat(VisualElement parent)
        {
            var b = Box(parent, new Color(0.96f, 0.91f, 0.8f), 24, false); Pad(b, 10, 24); b.style.marginLeft = b.style.marginRight = 8;
            return b;
        }

        // ปุ่มหลัก: จอสัมผัสใช้นิ้วกด · คอมพิวเตอร์คลิกเมาส์ค้างที่ปุ่มได้ หรือกด Space ก็ได้ (แสดงป้าย "Space")
        void BuildButton()
        {
            float size = TouchUi ? 230 : 170;
            actBtn = Box(root, Green, size / 2); Abs(actBtn, null, 40, null, 40);
            actBtn.style.width = actBtn.style.height = size; actBtn.pickingMode = PickingMode.Position;
            actBtn.style.justifyContent = Justify.Center; actBtn.style.alignItems = Align.Center;
            actBtn.style.borderTopColor = actBtn.style.borderBottomColor = actBtn.style.borderLeftColor = actBtn.style.borderRightColor = new Color(1, 1, 1, 0.85f);
            actBtn.style.borderTopWidth = actBtn.style.borderBottomWidth = actBtn.style.borderLeftWidth = actBtn.style.borderRightWidth = 6;
            actTxt = L(actBtn, "เหวี่ยง", TouchUi ? 44 : 36, Color.white); Shadow(actTxt);
            if (!TouchUi) { actKey = L(actBtn, "Space", 20, new Color(1, 1, 1, 0.85f)); actKey.style.marginTop = 2; }
            actBtn.RegisterCallback<PointerDownEvent>(e => { actBtn.CapturePointer(e.pointerId); GameInput.ButtonHeld = true; actBtn.style.scale = new Scale(new Vector3(0.94f, 0.94f, 1)); });
            actBtn.RegisterCallback<PointerUpEvent>(e => { actBtn.ReleasePointer(e.pointerId); GameInput.ButtonHeld = false; actBtn.style.scale = new Scale(Vector3.one); });
        }

        // จอสัมผัส: ลากนิ้วบนที่ว่างของจอ = หมุนกล้อง (อยู่ชั้นล่างสุด ไม่บังปุ่ม) + ปุ่มกระโดดเหนือปุ่มหลัก
        void BuildTouchExtras()
        {
            var pad = new VisualElement { pickingMode = PickingMode.Position }; Abs(pad, 0, 0, 0, 0);
            root.Insert(0, pad);
            Vector2 last = default;
            pad.RegisterCallback<PointerDownEvent>(e => { pad.CapturePointer(e.pointerId); last = e.localPosition; });
            pad.RegisterCallback<PointerMoveEvent>(e => { if (!pad.HasPointerCapture(e.pointerId)) return; var p = (Vector2)e.localPosition; GameInput.TouchLook += new Vector2(p.x - last.x, last.y - p.y); last = p; });
            pad.RegisterCallback<PointerUpEvent>(e => pad.ReleasePointer(e.pointerId));
            var jb = jumpBtn = Box(root, new Color(0.25f, 0.45f, 0.62f, 0.92f), 65); Abs(jb, null, 290, null, 60);
            jb.style.width = jb.style.height = 130; jb.pickingMode = PickingMode.Position; jb.style.justifyContent = Justify.Center;
            jb.style.borderTopColor = jb.style.borderBottomColor = jb.style.borderLeftColor = jb.style.borderRightColor = new Color(1, 1, 1, 0.85f);
            jb.style.borderTopWidth = jb.style.borderBottomWidth = jb.style.borderLeftWidth = jb.style.borderRightWidth = 5;
            var jl = L(jb, "กระโดด", 28, Color.white); Shadow(jl);
            jb.RegisterCallback<PointerDownEvent>(e => GameInput.JumpButton = true);
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
        static readonly (string id, string name)[] Stages = { ("paddy", "ทุ่งนา"), ("forest", "ป่าลึก"), ("falls", "น้ำตก"), ("beach", "ชายหาด"), ("sea", "ทะเล") };

        void BuildDex()
        {
            dexBtn = Box(root, Paper, 999); Abs(dexBtn, null, 24, 24); Pad(dexBtn, 8, 24); dexBtn.pickingMode = PickingMode.Position;
            dexCount = L(dexBtn, $"สมุดปลา {FishDex.Discovered}/{FishDatabase.All.Count}", 28, Ink);
            dexBtn.RegisterCallback<PointerDownEvent>(e => OpenDex());

            dex = new VisualElement(); Abs(dex, 0, 0, 0, 0); dex.style.backgroundColor = new Color(0.1f, 0.16f, 0.13f, 0.6f);
            dex.style.justifyContent = Justify.Center; dex.style.alignItems = Align.Center; root.Add(dex);
            var book = Box(dex, Paper, 40); Pad(book, 22, 26); MaxW(book, 980); book.style.maxHeight = Length.Percent(92); book.pickingMode = PickingMode.Position;
            var head = new VisualElement { pickingMode = PickingMode.Ignore }; head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center; head.style.flexShrink = 0; book.Add(head);
            dexTitle = L(head, "สมุดสะสมปลา", 36, Ink); dexTitle.style.flexGrow = 1; dexTitle.style.unityTextAlign = TextAnchor.MiddleLeft;
            var close = new Button(() => { dex.style.display = DisplayStyle.None; GameInput.Blocked = false; }) { text = "ปิด" };
            StyleButton(close, new Color(0.55f, 0.42f, 0.25f)); head.Add(close);
            var tabs = new VisualElement { pickingMode = PickingMode.Ignore }; tabs.style.flexDirection = FlexDirection.Row; tabs.style.flexWrap = Wrap.Wrap; tabs.style.marginTop = 10; tabs.style.flexShrink = 0; book.Add(tabs);
            foreach (var (id, name) in Stages)
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
            b.style.borderTopLeftRadius = b.style.borderTopRightRadius = b.style.borderBottomLeftRadius = b.style.borderBottomRightRadius = 999;
            b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 0;
            b.style.flexShrink = 0;
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
            foreach (var (id, name) in Stages)
            {
                var tab = dex.Q<Button>("tab-" + id);
                if (tab != null) tab.style.backgroundColor = id == dexStage ? Gold : new Color(0.36f, 0.6f, 0.27f, 0.75f);
            }
            foreach (var sp in FishDatabase.ForStage(dexStage))
            {
                total++;
                bool has = FishDex.Has(sp.id); if (has) got++;
                var cell = Box(dexGrid, has ? new Color(1f, 0.99f, 0.95f) : new Color(0.9f, 0.88f, 0.83f), 24); Pad(cell, 10, 10);
                cell.style.width = 280; cell.style.marginLeft = cell.style.marginRight = 8; cell.style.marginBottom = 14;
                var img = new VisualElement { pickingMode = PickingMode.Ignore }; img.style.height = 130; img.style.flexShrink = 0;
                img.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                if (preview != null) img.style.backgroundImage = Background.FromRenderTexture(preview.Icon(sp, !has));
                cell.Add(img);
                L(cell, has ? sp.name : "???", 28, Ink);
                L(cell, new string('*', 0) + $"{FishDatabase.Rarity[sp.stars]} · {sp.stars} ดาว", 20, Gold);
                L(cell, has ? $"ได้ {FishDex.Count(sp.id)} ตัว · หนักสุด {FishDex.BestKg(sp.id):0.00} กก." : (sp.stage == "paddy" ? "ยังไม่เคยตกได้" : "ด่านนี้ยังไม่เปิด"), 20, new Color(0.35f, 0.3f, 0.25f), false);
            }
            dexTitle.text = $"สมุดสะสมปลา · {got}/{total}";
            dexCount.text = $"สมุดปลา {FishDex.Discovered}/{FishDatabase.All.Count}";
        }

        // ---------- API ให้ระบบตกปลาเรียก ----------
        public void CloseCard() { if (!CardOpen) return; card.style.display = DisplayStyle.None; GameInput.Blocked = false; onCardClose?.Invoke(); }
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
            actBtn.style.backgroundColor = hot ? Gold : Green;
            actBtn.style.display = CardOpen ? DisplayStyle.None : DisplayStyle.Flex;
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
        int lastCount = -1, lastMoney = -1;
        public void SetCount(int n, int money)
        {
            if (n == lastCount && money == lastMoney) return;
            lastCount = n; lastMoney = money;
            count.text = $"ปลา {n} ตัว · ฿{money:N0}";
        }
        public void ShowCatch(Catch c, Action onClose, Texture image = null, bool isNew = false)
        {
            var sp = c.species;
            cardImg.style.backgroundImage = image is RenderTexture rt ? Background.FromRenderTexture(rt) : default;
            cardImg.style.display = image != null ? DisplayStyle.Flex : DisplayStyle.None;
            cardNew.style.display = isNew ? DisplayStyle.Flex : DisplayStyle.None;
            if (dexCount != null) dexCount.text = $"สมุดปลา {FishDex.Discovered}/{FishDatabase.All.Count}";
            cardName.text = sp.name;
            cardStars.text = $"{FishDatabase.Rarity[sp.stars]} · {sp.stars} ดาว";
            cardKg.text = $"{c.kg:0.00} กก.";
            cardPrice.text = $"฿{Mathf.RoundToInt(c.kg * sp.price):N0}";
            cardDesc.text = sp.desc;
            onCardClose = onClose;
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
            if (!float.IsNaN(root.layout.height)) cardImg.style.height = root.layout.height < 820 ? 130 : 220;
            hintRow.style.bottom = TouchUi && portrait ? 300 : 26;
            // จอแนวนอนเตี้ย: แถบสู้ปลา/ตีเบ็ดชิดขอบบนให้เห็นทุ่น · ข้อความแจ้งเลื่อนลงใต้แถบเมื่อแถบเปิดอยู่
            fightRow.style.top = strikeRow.style.top = portrait ? 170 : 16;
            bool panelUp = fightBox.style.display == DisplayStyle.Flex || strikeBox.style.display == DisplayStyle.Flex;
            toastRow.style.top = panelUp ? (portrait ? 420 : 250) : 96;
            if (dexBtn != null) dexBtn.style.display = CardOpen ? DisplayStyle.None : DisplayStyle.Flex;
            // การ์ดปลาเปิด: ซ่อนจอย/ปุ่มกระโดดไม่ให้ทับการ์ด
            var hide = CardOpen ? DisplayStyle.None : DisplayStyle.Flex;
            if (joy != null) joy.style.display = hide;
            if (jumpBtn != null) jumpBtn.style.display = hide;
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
