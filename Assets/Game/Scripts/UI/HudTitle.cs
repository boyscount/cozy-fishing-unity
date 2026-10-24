using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // หน้าแรก (เมนูหลัก): ฉากเรือยามโพล้เพล้ + ใต้น้ำ · โลโก้ · ปุ่มป้ายไม้ — เริ่มการเดินทาง / ตามรอยต่อ / ตกปลาอิสระ / สมุดสะสมปลา / ตั้งค่า
    public partial class Hud
    {
        VisualElement title, settings, settingsBody, titleHead, titleLogo, titleGrid, titleArt; Button titleSettings;
        Button titleContinue;
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
            Btn("เริ่มการเดินทาง", "เนื้อเรื่องใหม่", "new", true);
            titleContinue = Btn("ตามรอยต่อ", "", "continue");
            Btn("ตกปลาอิสระ", "เปิดทุกด่าน ไม่มีเนื้อเรื่อง", "free");
            Btn("สมุดสะสมปลา", "ปลาและความลับที่สะสมไว้", "dex");
            if (titleArt == null) Btn("ตั้งค่า", null, "settings");
            else
            {
                titleSettings = new Button(() => onTitle?.Invoke("settings")) { text = "ตั้งค่า" }; WoodButton(titleSettings, true);
                titleSettings.style.unityFontDefinition = fontTitle; titleSettings.style.fontSize = 24; Pad(titleSettings, 8, 24);
                titleSettings.AddToClassList("small"); titleGrid.Add(titleSettings); ButtonIcon(titleSettings, "gear", 26, new Color(0.3f, 0.17f, 0.06f));
                // สลับภาษาไทย/อังกฤษ (ปุ่มแสดงภาษาที่จะเปลี่ยนไป)
                var langBtn = new Button(() => Loc.Lang = Loc.English ? "th" : "en"); WoodButton(langBtn, true);
                langBtn.AddToClassList("small"); titleGrid.Add(langBtn); ButtonIcon(langBtn, "globe", 26, new Color(0.3f, 0.17f, 0.06f));
                langBtn.schedule.Execute(() => langBtn.text = Loc.English ? "ไทย" : "English").Every(200);
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
            if (info != null) titleContinue.text = $"ตามรอยต่อ\n<size=62%>{info}</size>";
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
                    foreach (var c in titleGrid.Children())
                    {
                        bool sm = c.ClassListContains("small");
                        c.style.width = gw * (sm ? 0.36f : 0.47f); c.style.marginLeft = c.style.marginRight = gw * 0.012f; c.style.marginTop = sm ? 14 : 18;
                        c.style.fontSize = sm ? 22 : 28; Pad(c, sm ? 15 : 16, 10); c.style.unityTextAlign = TextAnchor.MiddleCenter;
                        if (sm) c.style.paddingLeft = 44;   // เว้นที่ไอคอน
                        c.style.flexShrink = 0; c.style.minHeight = sm ? 62 : 0;
                    }
                    return;
                }
                var gs = titleGrid.style; gs.left = aw * 0.22f; gs.right = StyleKeyword.Auto; gs.width = aw * 0.56f; gs.top = ah * 0.755f; gs.bottom = StyleKeyword.Auto;
                gs.justifyContent = Justify.Center; gs.top = ah * 0.715f;
                float fs = Mathf.Clamp(ah * 0.03f, 16, 30) * (Loc.English ? 0.88f : 1f);
                foreach (var c in titleGrid.Children())
                {
                    bool small = c.ClassListContains("small");   // แถวล่าง: ตั้งค่า · ภาษา ปุ่มเล็กคู่กันกลาง
                    c.style.width = aw * (small ? 0.16f : 0.256f); c.style.marginLeft = c.style.marginRight = aw * 0.008f;
                    c.style.marginTop = ah * (small ? 0.01f : 0.011f); c.style.fontSize = small ? fs * 0.78f : fs;
                    Pad(c, ah * (small ? 0.011f : 0.013f), 8); c.style.unityTextAlign = TextAnchor.MiddleCenter;
                    if (small) c.style.paddingLeft = 44;   // เว้นที่ไอคอน
                    c.style.flexShrink = 0; c.style.minHeight = small ? fs * 0.78f * 1.5f + ah * 0.022f : 0;
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
            var lb = new Button(); lb.text = Loc.English ? "Language: English" : "ภาษา: ไทย";
            lb.clicked += () => { Loc.Lang = Loc.English ? "th" : "en"; lb.text = Loc.English ? "Language: English" : "ภาษา: ไทย"; };
            WoodButton(lb, true); lb.style.marginTop = 10; lb.style.alignSelf = Align.Stretch; settingsBody.Add(lb);
            Toggle("แสดงบันทึกภารกิจ", () => !questFolded, v => { questFolded = !v; try { PlayerPrefs.SetInt("abot.questFold", questFolded ? 1 : 0); } catch (Exception) { } lastQuest = null; });
            Show(settings); settings.BringToFront();
        }
    }
}
