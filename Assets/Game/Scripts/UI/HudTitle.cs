using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // หน้าแรก (เมนูหลัก): ฉากเรือยามโพล้เพล้ + ใต้น้ำ · โลโก้ · ปุ่มป้ายไม้ — เริ่มการเดินทาง / ตามรอยต่อ / ตกปลาอิสระ / สมุดสะสมปลา / ตั้งค่า
    public partial class Hud
    {
        VisualElement title, settings, settingsBody, titleHead, titleLogo, titleGrid, titleArt; Button titleSettings;
        Button titleContinue; Sign titleContinueSign;
        Action<string> onTitle;

        void BuildTitle()
        {
            title = new VisualElement { pickingMode = PickingMode.Position }; Abs(title, 0, 0, 0, 0); root.Add(title);
            // ภาพหลัก (ชาวประมงบนเรือ · โลโก้ · ใต้น้ำ) คงสัดส่วนเต็มความสูง/กว้าง · ส่วนที่เหลือของจอเป็นภาพเดียวกันแบบเบลอมืด
            var art = Resources.Load<Texture2D>("UI/Frames/title_art");
            title.style.backgroundImage = art != null ? Resources.Load<Texture2D>("UI/Frames/title_blur") : Resources.Load<Texture2D>("UI/Frames/title_bg");
            title.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            if (art != null)
            {
                titleArt = new VisualElement { pickingMode = PickingMode.Ignore }; titleArt.style.position = Position.Absolute; title.Add(titleArt);
                titleArt.style.backgroundImage = art; titleArt.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            }
            title.style.backgroundColor = new Color(0.04f, 0.05f, 0.06f);
            // โลโก้ + คำโปรย มุมขวาบน
            var head = titleHead = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(head, null, 30, 24); head.style.alignItems = Align.Center; title.Add(head);
            var logo = titleLogo = new VisualElement { pickingMode = PickingMode.Ignore }; logo.style.width = 560; logo.style.height = 224;
            logo.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            logo.style.backgroundImage = Resources.Load<Texture2D>("UI/Frames/title_logo"); head.Add(logo);
            var tag = new VisualElement { pickingMode = PickingMode.Ignore }; Skin(tag, "plank", 22); Pad(tag, 10, 34); tag.style.marginTop = 4; head.Add(tag);
            L(tag, "ตกปลาเลี้ยงครอบครัว · สืบคดีลึกลับ · ไขความลับของปลาในทุ่งนาไทย", 22, new Color(0.22f, 0.12f, 0.04f));
            // ปุ่มป้ายไม้ 2 คอลัมน์ มุมขวาล่าง (จอแคบตัดลงเป็นคอลัมน์เดียวเอง)
            var grid = titleGrid = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(grid, null, 30, null, 30);
            grid.style.flexDirection = FlexDirection.Row; grid.style.flexWrap = Wrap.Wrap; grid.style.justifyContent = Justify.FlexEnd; grid.style.width = 640; (titleArt ?? title).Add(grid);
            if (titleArt != null) titleHead.style.display = DisplayStyle.None;   // ภาพหลักมีโลโก้กับคำโปรยในตัวแล้ว
            Button Btn(string main, string sub, string key, bool hot = false)
            {
                var b = new Button(() => onTitle?.Invoke(key)) { text = sub == null ? main : $"{main}\n<size=62%>{sub}</size>" };
                WoodButton(b, true); b.style.width = 300; b.style.marginLeft = 12; b.style.marginTop = 10; b.style.fontSize = 26; Pad(b, 8, 20); b.style.unityFontDefinition = fontTitle;
                if (hot) b.style.unityBackgroundImageTintColor = new Color(1f, 0.72f, 0.42f);
                grid.Add(b); return b;
            }
            if (titleArt == null)
            {
                Btn("เริ่มการเดินทาง", "เนื้อเรื่องใหม่", "new", true);
                titleContinue = Btn("ตามรอยต่อ", "", "continue");
                Btn("ตกปลาอิสระ", "เปิดทุกด่าน ไม่มีเนื้อเรื่อง", "free");
                Btn("สมุดสะสมปลา", "ปลาและความลับที่สะสมไว้", "dex");
                Btn("ตั้งค่า", null, "settings");
            }
            else
            {
                // ป้ายไม้ดิบ + ของประกอบเล่าเรื่อง (ดู HudTitleSign.cs)
                InitSignAudio();
                Sign S(string main, string sub, string plank, string key) { var sg = MakeSign(main, sub, plank, () => onTitle?.Invoke(key)); grid.Add(sg.b); return sg; }
                var sNew = S("เริ่มการเดินทาง", "เนื้อเรื่องใหม่", "plank_a", "new");
                Prop(sNew, "prop_map", -0.62f, -0.32f, 1.3f);                                  // แผนที่เก่าพับ หนีบหมุดไม้
                var sCont = titleContinueSign = S("ตามรอยต่อ", "", "plank_b", "continue"); titleContinue = sCont.b;
                Prop(sCont, "stamp_red", 0.1f, 0.1f, 0.8f, false, 0.8f);                       // ตราประทับแดงแบบเอกสารคดี
                Prop(sCont, "prop_key", -0.62f, -0.42f, 1.45f, true);                          // พวงกุญแจเก่า + พู่เชือกถัก
                var sFree = S("ตกปลาอิสระ", "เปิดทุกด่าน ไม่มีเนื้อเรื่อง", "plank_c", "free");
                Prop(sFree, "prop_lure", -0.78f, -0.18f, 1.25f);                               // เหยื่อปลอมทำมือ
                Prop(sFree, "prop_bobber", -0.86f, 0.28f, 1.05f, true);                        // ทุ่นลอย + ตะขอห้อยมุมป้าย
                var sDex = S("สมุดสะสมปลา", "ปลาและความลับที่สะสมไว้", "plank_d", "dex");
                Prop(sDex, "prop_scales", -0.42f, 0.18f, 0.85f);                               // เกล็ดปลาแห้ง
                Prop(sDex, "prop_photo", -1.02f, -0.62f, 1.3f, true);                          // ภาพถ่ายปลา + คลิปหนีบ
                var sSet = MakeSign("ตั้งค่า", null, "plank_s", () => onTitle?.Invoke("settings"), true); titleSettings = sSet.b;
                sSet.b.AddToClassList("small"); grid.Add(sSet.b);
                sSet.icon = Prop(sSet, "icon_windkey", 0, 0, 1).e; sSet.props.RemoveAt(sSet.props.Count - 1);   // กุญแจทองเหลืองไขลาน
                // สลับภาษาไทย/อังกฤษ (ป้ายแสดงภาษาที่จะเปลี่ยนไป) · ตราครั่งลายประจำยาม
                var sLang = MakeSign("English", null, "plank_s", () => Loc.Lang = Loc.English ? "th" : "en", true);
                sLang.b.AddToClassList("small"); grid.Add(sLang.b);
                sLang.icon = Prop(sLang, "icon_seal", 0, 0, 1).e; sLang.props.RemoveAt(sLang.props.Count - 1);
                sLang.main.schedule.Execute(() => sLang.main.text = Loc.English ? "ไทย" : "English").Every(200);
                title.schedule.Execute(TickSigns).Every(33);
            }
            title.style.display = DisplayStyle.None;
            title.RegisterCallback<GeometryChangedEvent>(e => LayoutTitle()); Loc.Changed += LayoutTitle;   // จอเปลี่ยนขนาด/หมุน → จัดภาพหลักและปุ่มใหม่
            LayoutTitle();

            // ตั้งค่า
            settings = Overlay(out var sb, 640);
            Skin(sb, "parchment", 20); sb.style.paddingTop = 26; sb.style.paddingBottom = 26; sb.style.paddingLeft = 34; sb.style.paddingRight = 34;
            var st = L(sb, "ตั้งค่า", 40, new Color(0.3f, 0.16f, 0.06f)); st.style.unityFontDefinition = fontTitle;
            settingsBody = new VisualElement { pickingMode = PickingMode.Ignore }; settingsBody.style.marginTop = 10; sb.Add(settingsBody);
            var close = new Button(() => Hide(settings)) { text = "ปิด" }; WoodButton(close, true); close.style.alignSelf = Align.Center; close.style.marginTop = 16; sb.Add(close);
        }

        /// เปิดหน้าแรก · info = ข้อความใต้ "ตามรอยต่อ" (null = ไม่มีเซฟ ซ่อนปุ่ม)
        public void ShowTitle(string info, Action<string> pick)
        {
            if (title == null) BuildTitle();
            onTitle = pick;
            titleContinue.style.display = info != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (info != null) { if (titleContinueSign != null) titleContinueSign.sub.text = info; else titleContinue.text = $"ตามรอยต่อ\n<size=62%>{info}</size>"; }
            if (fadeEl != null) { fadeEl.style.display = DisplayStyle.None; fadeEl.style.opacity = 0; }   // จอดำตอนเปิดเกมไม่บังหน้าแรก
            LayoutTitle(); Show(title); title.BringToFront();
            // สมุดปลา/ตั้งค่าที่เพิ่งเปิดจากหน้าแรกต้องอยู่บนสุด (ไม่งั้นหน้าแรกทับจนดูเหมือนกดไม่ติด)
            foreach (var m in new[] { dex, settings }) if (m != null && m.style.display.value == DisplayStyle.Flex) m.BringToFront();
        }
        // จอกว้าง: โลโก้มุมขวาบน ปุ่ม 2 คอลัมน์มุมขวาล่าง · จอแคบ: ทุกอย่างอยู่กลางจอ ปุ่มย่อให้พอดีความกว้าง
        void LayoutTitle()
        {
            if (titleHead == null) return;
            bool n = Narrow; float w = root.layout.width;
            if (titleArt != null)
            {
                // ภาพหลักพอดีจอ (คงสัดส่วน) · ปุ่มวางทับช่องป้ายไม้ในภาพ (กว้าง 50% กลาง · บน 77.5% ของภาพ)
                float h = title.layout.height, ar = 2560f / 2083f; w = title.layout.width;
                if (float.IsNaN(w) || float.IsNaN(h) || w < 1 || h < 1) return;
                float aw = Mathf.Min(w, h * ar), ah = aw / ar;
                bool portrait = h > w * 1.15f;   // จอตั้ง: ภาพไว้บน ปุ่มใหญ่ใต้ภาพตรงที่ว่าง
                titleArt.style.width = aw; titleArt.style.height = ah; titleArt.style.left = (w - aw) / 2; titleArt.style.top = portrait ? Mathf.Max(h * 0.04f, (h - ah - 330) / 2) : (h - ah) / 2;   // จอตั้ง: ภาพ+ปุ่มรวมกันอยู่กลางจอ
                if (portrait)
                {
                    float gw = Mathf.Min(w * 0.94f, 760);
                    var ps = titleGrid.style; ps.left = (aw - gw) / 2; ps.right = StyleKeyword.Auto; ps.width = gw; ps.top = ah * 0.78f; ps.bottom = StyleKeyword.Auto; ps.justifyContent = Justify.Center;
                    foreach (var sg in signs)
                    {
                        float sgw = gw * (sg.small ? 0.4f : 0.47f), bh = sg.small ? sgw / 3.9f : sgw / 3.3f;
                        sg.b.style.marginLeft = sg.b.style.marginRight = gw * 0.012f; sg.b.style.marginTop = sg.small ? 22 : 30;
                        SizeSign(sg, sgw, bh, 0.45f);
                    }
                    return;
                }
                // ป้ายใหญ่ 2 คอลัมน์ใต้คำโปรย · แถวล่าง: ตั้งค่า · ภาษา ป้ายเล็กคู่กันกลาง
                var gs = titleGrid.style; gs.left = aw * 0.16f; gs.right = StyleKeyword.Auto; gs.width = aw * 0.68f; gs.bottom = StyleKeyword.Auto;
                gs.justifyContent = Justify.Center; gs.top = ah * 0.682f;
                foreach (var sg in signs)
                {
                    float sgw = aw * (sg.small ? 0.18f : 0.29f), bh = sg.small ? ah * 0.052f : ah * 0.086f;
                    sg.b.style.marginLeft = sg.b.style.marginRight = aw * 0.022f; sg.b.style.marginTop = ah * (sg.small ? 0.016f : 0.02f);
                    SizeSign(sg, sgw, bh, 1f);
                }
                return;
            }
            // จอแนวตั้ง: เลื่อนภาพพื้นหลังให้เห็นชาวประมงบนเรือ (อยู่ฝั่งซ้ายของภาพ)
            title.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, Length.Percent(n ? 22 : 50));
            titleHead.style.left = n ? 0 : StyleKeyword.Auto; titleHead.style.right = n ? 0 : 30;
            titleHead.style.top = n ? 60 : 24;
            float lw = n ? Mathf.Min(560, w - 40) : 560;
            titleLogo.style.width = lw; titleLogo.style.height = lw * 0.4f;
            var tag = titleHead.ElementAt(1); tag.style.maxWidth = n ? w - 40 : StyleKeyword.None;
            titleGrid.style.left = n ? 0 : StyleKeyword.Auto; titleGrid.style.right = n ? 0 : 30; titleGrid.style.bottom = n ? 60 : 30;
            titleGrid.style.width = n ? StyleKeyword.Auto : 640;
            titleGrid.style.justifyContent = n ? Justify.Center : Justify.FlexEnd;
            float bw = n ? Mathf.Min(300, (w - 60) / 2f) : 300;
            foreach (var c in titleGrid.Children()) { c.style.width = bw; c.style.marginLeft = n ? 6 : 12; c.style.marginRight = n ? 6 : 0; }
        }

        public void HideTitle() { if (title != null) Hide(title); }
        public bool TitleOpen => Shown(title);

        public void OpenDexFromTitle() { dex.style.display = DisplayStyle.Flex; dex.BringToFront(); FillDex(); }

        public void ShowSettings()
        {
            settingsBody.Clear();
            void Toggle(string name, Func<bool> get, Action<bool> set)
            {
                var b = new Button(); b.text = $"{name}: {(get() ? "เปิด" : "ปิด")}";
                b.clicked += () => { set(!get()); b.text = $"{name}: {(get() ? "เปิด" : "ปิด")}"; };
                WoodButton(b, true); b.style.marginTop = 10; b.style.alignSelf = Align.Stretch; settingsBody.Add(b);
            }
            Toggle("มุมมองบุคคลที่หนึ่ง (เห็นมือถือคันเบ็ด)", () => FirstPersonView.Enabled, v => FirstPersonView.Enabled = v);
            Toggle("ภาพใต้น้ำตอนรอปลา", () => UnderwaterView.Enabled, v => UnderwaterView.Enabled = v);
            string BrightName(int v) => v < 0 ? "มืดลง" : v == 0 ? "ปกติ" : v == 1 ? "สว่าง" : "สว่างมาก";
            var bb = new Button(); bb.text = $"ความสว่างหน้าจอ: {BrightName(Atmosphere.Brightness)}";
            bb.clicked += () => { int v = Atmosphere.Brightness + 1; if (v > 2) v = -1; Atmosphere.Brightness = v; bb.text = $"ความสว่างหน้าจอ: {BrightName(v)}"; };
            WoodButton(bb, true); bb.style.marginTop = 10; bb.style.alignSelf = Align.Stretch; settingsBody.Add(bb);
            var lb = new Button(); lb.text = Loc.English ? "Language: English" : "ภาษา: ไทย";
            lb.clicked += () => { Loc.Lang = Loc.English ? "th" : "en"; lb.text = Loc.English ? "Language: English" : "ภาษา: ไทย"; };
            WoodButton(lb, true); lb.style.marginTop = 10; lb.style.alignSelf = Align.Stretch; settingsBody.Add(lb);
            Toggle("แสดงบันทึกภารกิจ", () => !questFolded, v => { questFolded = !v; try { PlayerPrefs.SetInt("abot.questFold", questFolded ? 1 : 0); } catch (Exception) { } lastQuest = null; });
            Show(settings); settings.BringToFront();
        }
    }
}
