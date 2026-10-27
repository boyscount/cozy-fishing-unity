using System;
using UnityEngine;
using UnityEngine.UIElements;
using static CozyFishing.HudKit;

namespace CozyFishing
{
    // ก่อนนอน: ห้องนอนบ้านไม้สมัยก่อน (มุ้ง ตะเกียง พัดลม หน้าต่างมองทุ่งนายามค่ำ) · ป้ายไม้ไผ่ "ราตรีมาเยือน…"
    // นอนหลับ · ตรวจตราบ้าน/สวดมนต์แผ่เมตตา (ได้สายสิญจน์ คืนละครั้ง) · แอบออกไปตกปลาตอนดึก (อันตราย) · บันทึกสมุดคดี (เซฟ)
    public partial class Hud
    {
        VisualElement bed, bedRow; Button bedPray, bedSleep, bedSneak, bedSave; Label bedSign;
        Action<string> onBed;

        void BuildBed()
        {
            bed = new VisualElement { pickingMode = PickingMode.Position }; Abs(bed, 0, 0, 0, 0); root.Add(bed);
            bed.style.backgroundColor = new Color(0.05f, 0.04f, 0.035f);
            bed.style.backgroundImage = Resources.Load<Texture2D>("UI/Frames/bedroom_bg"); bed.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            bed.style.justifyContent = Justify.Center; bed.style.alignItems = Align.Center;
            var brown = new Color(0.24f, 0.13f, 0.05f);
            var col = new VisualElement { pickingMode = PickingMode.Ignore }; col.style.alignItems = Align.Center; col.style.width = Length.Percent(92); col.style.maxWidth = 900; bed.Add(col);
            // ป้ายกระดาษในกรอบไม้ไผ่
            var sign = new VisualElement { pickingMode = PickingMode.Ignore }; Skin(sign, "bamboo_paper", 26); col.Add(sign);
            sign.style.paddingTop = 26; sign.style.paddingBottom = 26; sign.style.paddingLeft = 56; sign.style.paddingRight = 56;
            var st = bedSign = L(sign, "ราตรีมาเยือน…\nได้เวลาพักผ่อน", 44, brown); st.style.unityFontDefinition = fontTitle;
            Button Plank(VisualElement parent, string text, Color tint, string key)
            {
                var b = new Button(() => onBed?.Invoke(key)) { text = text }; WoodButton(b, true); b.style.unityBackgroundImageTintColor = tint;
                b.style.unityFontDefinition = fontTitle; b.style.fontSize = 30; Pad(b, 14, 34); parent.Add(b);
                b.style.unityTextAlign = TextAnchor.MiddleCenter; b.style.whiteSpace = WhiteSpace.Normal; b.style.flexShrink = 0; return b;
            }
            // นอนหลับ: ปุ่มส้มใหญ่ใต้ป้าย + ไอคอนพระจันทร์
            var sleep = bedSleep = Plank(col, "นอนหลับ (ตื่นรุ่งสาง)", new Color(1f, 0.62f, 0.3f), "sleep");
            sleep.style.marginTop = -6; sleep.style.fontSize = 34; sleep.style.paddingRight = 74; sleep.style.color = new Color(0.98f, 0.94f, 0.85f); Shadow2(sleep);
            var mi = Ico(sleep, "moon", 40); Abs(mi, null, 22, 10);
            var row = bedRow = new VisualElement { pickingMode = PickingMode.Ignore }; row.style.flexDirection = FlexDirection.Row; row.style.justifyContent = Justify.Center; row.style.alignItems = Align.Stretch; row.style.marginTop = 18; row.style.width = Length.Percent(100); col.Add(row);
            bedPray = Plank(row, "ตรวจตราบ้าน /\nสวดมนต์แผ่เมตตา", new Color(0.78f, 0.76f, 0.72f), "pray"); bedPray.style.marginLeft = bedPray.style.marginRight = 10; bedPray.style.marginTop = 8;
            ButtonIcon(bedPray, "incense", 40, new Color(0.32f, 0.2f, 0.1f));
            // แอบออกไป: แผ่นไม้สีเข้มอมแดง + ป้ายแดง (อันตราย) + ไอคอนผี
            var sneak = bedSneak = new Button(() => onBed?.Invoke("sneak")) { text = "แอบออกไปตกปลาตอนดึก\n<color=#ff6a55>(อันตราย)</color>" };
            StyleButton(sneak, new Color(0.16f, 0.07f, 0.07f, 0.94f)); sneak.style.unityFontDefinition = fontTitle; sneak.style.fontSize = 28; Pad(sneak, 12, 30);
            Border(sneak, 3, new Color(0.55f, 0.18f, 0.12f)); sneak.style.color = new Color(1f, 0.92f, 0.82f);
            sneak.style.marginLeft = sneak.style.marginRight = 10; sneak.style.marginTop = 8; sneak.style.paddingLeft = 64; row.Add(sneak);
            sneak.style.unityTextAlign = TextAnchor.MiddleCenter; sneak.style.whiteSpace = WhiteSpace.Normal; sneak.style.flexShrink = 0;
            ButtonIcon(sneak, "ghost", 40, Color.white);
            // บันทึกสมุดคดี (เซฟเกม)
            var save = bedSave = Plank(col, "บันทึกสมุดคดี\n<size=70%>(เซฟเกม)</size>", new Color(0.95f, 0.88f, 0.72f), "save");
            save.style.marginTop = 18; save.style.fontSize = 24; save.style.paddingLeft = 70;
            var cb = Ico(save, "casebook", 42); Abs(cb, 16, null, 10);
            bed.style.display = DisplayStyle.None;
        }

        static void Shadow2(Button b) => b.style.textShadow = new TextShadow { offset = new Vector2(0, 2), blurRadius = 4, color = new Color(0.2f, 0.08f, 0.02f, 0.8f) };

        /// หน้าก่อนนอน · pick ได้ "sleep" / "pray" / "sneak" / "save" (pray กับ save หน้าจอยังเปิดอยู่)
        public void ShowBedtime(bool canPray, Action<string> pick)
        {
            if (bed == null) BuildBed();
            onBed = pick;
            bedPray.SetEnabled(canPray); bedPray.style.opacity = canPray ? 1 : 0.55f;
            // ขนาดตามจอ: ปุ่มกว้างตามสัดส่วน ตัวหนังสือย่อเองบนจอแคบ (ไม่ล้นป้ายไม้)
            // ขนาดตายตัว (หน่วยของหน้าจอ UI) · จอแคบ: ปุ่มเรียงลงมาทีละปุ่ม กว้างเต็มแถว
            bool n = Narrow;
            bedSign.style.fontSize = n ? 40 : 44;
            bedSleep.style.fontSize = n ? 30 : 32; bedSleep.style.width = n ? 600 : 560;
            bedRow.style.flexDirection = n ? FlexDirection.Column : FlexDirection.Row; bedRow.style.alignItems = Align.Center;
            foreach (var b in new[] { bedPray, bedSneak }) { b.style.fontSize = n ? 26 : 26; b.style.width = n ? 600 : 400; b.style.marginLeft = b.style.marginRight = n ? 0 : 10; b.style.marginTop = n ? 12 : 8; }
            bedSave.style.fontSize = 22; bedSave.style.width = n ? 380 : 320;
            // ความสูงตายตัว: กันปุ่มยุบเหลือเส้นบาง (ระบบวัดความสูงตัวหนังสือหลายบรรทัดไม่ทันบางจอ)
            bedSleep.style.height = 74; bedPray.style.height = bedSneak.style.height = 96; bedSave.style.height = 72;
            foreach (var b in new[] { bedSleep, bedPray, bedSneak, bedSave }) { b.style.paddingTop = b.style.paddingBottom = 6; b.style.unityTextAlign = TextAnchor.MiddleCenter; }
            Show(bed);
            if (FishingController.Bot) StartCoroutine(BotClick(bed, 2.5f, () => onBed?.Invoke("sleep")));
        }
        public void HideBed() { if (bed != null) Hide(bed); }
    }
}
