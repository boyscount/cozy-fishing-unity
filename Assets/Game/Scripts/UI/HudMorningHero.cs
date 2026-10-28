using UnityEngine;
using UnityEngine.UIElements;
using static CozyFishing.HudKit;

namespace CozyFishing
{
    // หน้าเช้า (จอกว้าง): ภาพกระท่อมพรานเป็นพื้นหลัง (UI/Morning/morning_hero · ลบตัวหนังสือในภาพออกแล้ว) แล้ววางข้อความ/ปุ่มจริงทับตามตำแหน่งในภาพ
    //   ป้ายไม้ "เช้าวันที่ N" · แถบกระดาษ อากาศ / ราคาพิเศษ / เทศกาล · ป้ายไม้ กระดานปลาใหญ่ · การ์ด 4 ใบ (ย่ามพราน วิชาพราน แต่งกาย ลานประลอง)
    //   กระดานดำ = ร้านลุงส้ม · ลูกศรมุมขวาล่าง = ไปเลือกที่ตกปลา · ตำแหน่งทุกชิ้นเป็นสัดส่วนของภาพ 1376×768 ฟอนต์ย่อขยายตามความกว้างภาพ
    public partial class Hud
    {
        const float HeroW = 1376f, HeroH = 768f;

        void BuildMorningHero(VisualElement parent, System.Action goFishing)
        {
            var s = Story.S;
            var hero = new VisualElement { pickingMode = PickingMode.Ignore }; hero.style.width = Length.Percent(100); hero.style.flexShrink = 0;
            WebArt.Load(this, "morning_hero.jpg", t => hero.style.backgroundImage = t);   // โหลดจากหน้าเว็บตอนแสดงครั้งแรก (ไม่นับรวมไฟล์เกม)
            hero.style.backgroundColor = new Color(0.16f, 0.1f, 0.05f); hero.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
            parent.Add(hero);
            var sized = new System.Collections.Generic.List<(Label l, float k)>();   // ฟอนต์ = ความกว้างภาพ × k
            VisualElement Box(float x0, float y0, float x1, float y1, bool click = false)
            {
                var e = new VisualElement { pickingMode = click ? PickingMode.Position : PickingMode.Ignore }; e.style.position = Position.Absolute;
                e.style.left = Length.Percent(x0 / HeroW * 100); e.style.top = Length.Percent(y0 / HeroH * 100);
                e.style.width = Length.Percent((x1 - x0) / HeroW * 100); e.style.height = Length.Percent((y1 - y0) / HeroH * 100);
                e.style.justifyContent = Justify.Center; e.style.alignItems = Align.Center; hero.Add(e); return e;
            }
            Label Txt(VisualElement p, string t, float k, Color c, bool title = true)
            {
                var l = L(p, t, 20, c); if (title) l.style.unityFontDefinition = fontTitle;
                if (Loc.English) k *= 0.74f;   // ภาษาอังกฤษยาวกว่า ย่อฟอนต์ไม่ให้ล้นแถบ
                l.style.whiteSpace = WhiteSpace.Normal; l.style.unityTextAlign = TextAnchor.MiddleCenter; sized.Add((l, k)); return l;
            }
            VisualElement Click(float x0, float y0, float x1, float y1, System.Action act)
            {
                var b = Box(x0, y0, x1, y1, true);
                b.RegisterCallback<PointerUpEvent>(e => act());
                b.RegisterCallback<PointerEnterEvent>(e => b.style.backgroundColor = new Color(1f, 0.9f, 0.6f, 0.12f));
                b.RegisterCallback<PointerLeaveEvent>(e => b.style.backgroundColor = Color.clear);
                Radius(b, 10); return b;
            }
            var ink = new Color(0.24f, 0.13f, 0.05f);

            // ป้ายไม้ชื่อวัน
            Txt(Box(512, 78, 868, 150), Story.FreePlay ? "ตกปลาอิสระ" : $"เช้าวันที่ {s.day}", 0.04f, new Color(0.22f, 0.11f, 0.03f)).style.whiteSpace = WhiteSpace.NoWrap;
            // แถบอากาศ (ไอคอนวาดใหม่ตามอากาศจริง)
            var lk = Story.CurrentLuck;
            var wIco = Ico(Box(318, 188, 372, 240), lk.rain ? "rain" : lk.sun > 0.8f ? "sun" : lk.sun > 0.45f ? "partly" : "cloud", 1); wIco.style.width = wIco.style.height = Length.Percent(80); wIco.style.unityBackgroundImageTintColor = new Color(0.36f, 0.22f, 0.1f);
            Txt(Box(385, 180, 1075, 244), lk.text, 0.024f, ink);
            // ราคาพิเศษของแม่ค้า
            var hot = s.hot != null ? FishDatabase.Get(s.hot) : null;
            Txt(Box(372, 250, 1060, 302), hot != null && !Story.FreePlay ? $"วันนี้แม่ค้ารับซื้อ{(FishDex.Has(hot.id) ? hot.name : "ปลาลึกลับ")}ราคาพิเศษ {Story.Baht(Story.PriceOf(hot.id))}/กก." : "วันนี้ตลาดรับซื้อปลาราคาปกติ", 0.021f, new Color(0.42f, 0.16f, 0.04f));
            // เทศกาลปลาใหญ่ + ภาพด่านหนีบคลิป
            Ico(Box(325, 335, 370, 385), "trophy", 1).style.width = Length.Percent(90);
            Txt(Box(382, 318, 1030, 410), Weekly.CanJoin ? $"เทศกาลปลาใหญ่สัปดาห์นี้ที่{Weekly.Stage.name} · รางวัลสูงสุด {Story.Baht(Weekly.TierReward[2])}" : $"เทศกาลปลาใหญ่สัปดาห์นี้ที่{Weekly.Stage.name} · ร่วมได้ในโหมดตกปลาอิสระ", 0.022f, new Color(0.3f, 0.12f, 0.06f));
            var fp = Box(1044, 304, 1116, 420); fp.style.backgroundImage = Weekly.Stage.id == "cave" ? JTex("prop_cave") : Weekly.Stage.Art; fp.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover); fp.style.opacity = 0.9f;
            // กระดานปลาใหญ่
            var bb = Click(388, 442, 954, 548, () => OpenBoard());
            var bcol = Box(505, 452, 945, 538); bcol.style.alignItems = Align.FlexStart;
            var bt1 = Txt(bcol, "กระดานปลาใหญ่", 0.03f, new Color(1f, 0.82f, 0.45f)); bt1.style.unityTextAlign = TextAnchor.MiddleLeft; Shadow(bt1);
            var bt2 = Txt(bcol, Cloud.Ready ? "อันดับปลาตัวหนักสุดประจำสัปดาห์" : "อันดับประจำสัปดาห์ (ออฟไลน์อยู่)", 0.017f, new Color(0.96f, 0.86f, 0.66f), false); bt2.style.unityTextAlign = TextAnchor.MiddleLeft;
            // การ์ด 4 ใบ: แตะทั้งใบได้ · ชื่อใต้ไอคอน
            (float x0, float x1, string name, System.Action act)[] cards =
            {
                (298, 468, "ย่ามพราน", ShowGear), (498, 668, "วิชาพราน" + (s.skillPts > 0 ? $" (+{s.skillPts})" : ""), ShowSkills),
                (698, 870, "แต่งกาย", OpenWardrobe), (900, 1072, "ลานประลอง", StartDuelFromMorning),
            };
            foreach (var cd in cards)
            {
                Click(cd.x0, 578, cd.x1, 708, cd.act);
                Txt(Box(cd.x0, 646, cd.x1, 694), cd.name, 0.0185f, ink).style.whiteSpace = WhiteSpace.NoWrap;
            }
            if (s.skillPts > 0)   // ตราครั่งแดง: มีแต้มทักษะรอเรียน
            {
                var seal = Box(616, 554, 694, 630); seal.style.backgroundImage = Resources.Load<Texture2D>("UI/Icons/seal"); seal.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                Txt(seal, $"+{s.skillPts}", 0.016f, new Color(1f, 0.9f, 0.8f));
            }
            // กระดานดำ = ร้านลุงส้ม
            Click(935, 26, 1215, 172, ShowShop);
            var shopName = Txt(Box(975, 38, 1160, 84), "ร้านลุงส้ม", 0.024f, new Color(0.94f, 0.93f, 0.88f)); Shadow(shopName); shopName.style.whiteSpace = WhiteSpace.NoWrap;
            var news = Tackle.PendingTeach();
            Txt(Box(1080, 232, 1290, 272), news.Count > 0 ? "ลุงส้มมีของใหม่!" : "คลิกเพื่อเปิดร้าน", 0.015f, news.Count > 0 ? new Color(1f, 0.95f, 0.5f) : new Color(1f, 0.66f, 0.3f));
            // ลูกศรมุมขวาล่าง: ไปเลือกที่ตกปลา
            Click(1128, 660, 1200, 760, goFishing);

            hero.RegisterCallback<GeometryChangedEvent>(e =>
            {
                float w = hero.layout.width; if (float.IsNaN(w) || w < 10) return;
                float h = w * HeroH / HeroW; if (Mathf.Abs(hero.resolvedStyle.height - h) > 0.5f) hero.style.height = h;
                foreach (var (l, k) in sized) l.style.fontSize = Mathf.Max(11, w * k);
            });
        }
    }
}
