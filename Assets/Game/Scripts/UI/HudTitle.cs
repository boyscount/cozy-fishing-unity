using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // หน้าแรก (เมนูหลัก): ฉากเรือยามโพล้เพล้ + ใต้น้ำ · โลโก้ · ปุ่มป้ายไม้ — เริ่มการเดินทาง / ตามรอยต่อ / ตกปลาอิสระ / สมุดสะสมปลา / ตั้งค่า
    public partial class Hud
    {
        VisualElement title, settings, settingsBody;
        Button titleContinue;
        Action<string> onTitle;

        void BuildTitle()
        {
            title = new VisualElement { pickingMode = PickingMode.Position }; Abs(title, 0, 0, 0, 0); root.Add(title);
            title.style.backgroundImage = Resources.Load<Texture2D>("UI/Frames/title_bg"); title.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            title.style.backgroundColor = new Color(0.04f, 0.05f, 0.06f);
            // โลโก้ + คำโปรย มุมขวาบน
            var head = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(head, null, 30, 24); head.style.alignItems = Align.Center; title.Add(head);
            var logo = new VisualElement { pickingMode = PickingMode.Ignore }; logo.style.width = 560; logo.style.height = 224;
            logo.style.backgroundImage = Resources.Load<Texture2D>("UI/Frames/title_logo"); head.Add(logo);
            var tag = new VisualElement { pickingMode = PickingMode.Ignore }; Skin(tag, "plank", 22); Pad(tag, 10, 34); tag.style.marginTop = 4; head.Add(tag);
            L(tag, "ตกปลาเลี้ยงครอบครัว · สืบคดีลึกลับ · ไขความลับของปลาในทุ่งนาไทย", 22, new Color(0.22f, 0.12f, 0.04f));
            // ปุ่มป้ายไม้ 2 คอลัมน์ มุมขวาล่าง (จอแคบตัดลงเป็นคอลัมน์เดียวเอง)
            var grid = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(grid, null, 30, null, 30);
            grid.style.flexDirection = FlexDirection.Row; grid.style.flexWrap = Wrap.Wrap; grid.style.justifyContent = Justify.FlexEnd; grid.style.width = 640; title.Add(grid);
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
            Btn("ตั้งค่า", null, "settings");
            title.style.display = DisplayStyle.None;

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
            Show(title); title.BringToFront();
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
            Toggle("แสดงบันทึกภารกิจ", () => !questFolded, v => { questFolded = !v; try { PlayerPrefs.SetInt("abot.questFold", questFolded ? 1 : 0); } catch (Exception) { } lastQuest = null; });
            Show(settings); settings.BringToFront();
        }
    }
}
