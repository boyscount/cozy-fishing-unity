using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;
using static CozyFishing.HudKit;

namespace CozyFishing
{
    // ท้าดวลผ่านลิงก์ + หน้าอุปกรณ์ของฉัน
    public partial class Hud
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CozyShareLink(string title, string text, string url);
#else
        static void CozyShareLink(string title, string text, string url) { GUIUtility.systemCopyBuffer = url; Debug.Log("[Share] link " + url); }
#endif

        /// เริ่มดวลใหม่จากหน้าเช้า: สุ่มเลขดวล ด่านที่ไปล่าสุด → เข้าโหมดตกปลาอิสระแล้วออกเดินทาง
        void StartDuelFromMorning()
        {
            var stage = Stages.Get(Story.S.stage).id;
            Duel.Start(Random.Range(100000, 999999), stage);
            if (!Story.FreePlay) Story.EnterFreePlay();
            Story.S.stage = Duel.Stage;
            Hide(morning);
            Toast($"ดวล 5 ตัวที่{Stages.Get(Duel.Stage).name} · น้ำหนักรวมยิ่งมากยิ่งดี แล้วส่งลิงก์ให้เพื่อนเล่นแข่ง");
            onMorningGo?.Invoke(Duel.Stage);
        }

        /// ครบห้าตัว: สรุปผล (เทียบกับคะแนนเพื่อนถ้ามาจากลิงก์) + แชร์ลิงก์ท้า
        public void ShowDuelResult()
        {
            Debug.Log($"[Duel] result active={Duel.Active} total={Duel.TotalKg:0.00} vs={Duel.VsKg:0.00}");
            if (!Duel.Active) return;
            string me = Cloud.PlayerName.Split('#')[0];
            string log = string.Join(" · ", Duel.Log);
            string head;
            if (Duel.VsKg >= 0)
            {
                string who = string.IsNullOrEmpty(Duel.VsName) ? "เพื่อน" : Duel.VsName;
                head = Duel.TotalKg > Duel.VsKg + 0.004f ? $"ชนะ! คุณ {Duel.TotalKg:0.00} กก. · {who} {Duel.VsKg:0.00} กก."
                     : Duel.TotalKg < Duel.VsKg - 0.004f ? $"แพ้ไปนิด… คุณ {Duel.TotalKg:0.00} กก. · {who} {Duel.VsKg:0.00} กก."
                     : $"เสมอ! ทั้งคู่ {Duel.TotalKg:0.00} กก.";
            }
            else head = $"ดวลจบแล้ว! น้ำหนักรวม {Duel.TotalKg:0.00} กก.";
            Sfx.Coin();
            bool won = Duel.VsKg >= 0 && Duel.TotalKg > Duel.VsKg + 0.004f;
            head += " · " + Cosmetics.Earn(won ? 3 : 1, won ? "ชนะดวล" : "ดวลจบ");
            var link = Duel.Link(me);
            Ask($"{head}\n{log}\nส่งลิงก์ให้เพื่อน แล้วดูว่าใครตกปลาชุดเดียวกันได้หนักกว่า",
                ("ส่งลิงก์ท้าเพื่อน", () => { CozyShareLink("ท้าดวลตกปลา!", $"ฉันตกได้ {Duel.TotalKg:0.00} กก. ใน 5 ตัว ที่{Stages.Get(Duel.Stage).name} มาดวลกัน!", link); Duel.End(); }),
                ("ตกปลาต่อ", () => Duel.End()));
        }

        /// หน้าอุปกรณ์ของฉัน: การ์ดรูปของแต่ละชิ้น + แถบค่าพลังเทียบกับขั้นสูงสุด + ปุ่มไปร้าน
        VisualElement gear, gearList;
        void ShowGear()
        {
            if (gear == null)
            {
                gear = new VisualElement { pickingMode = PickingMode.Position }; Abs(gear, 0, 0, 0, 0); root.Add(gear);
                gear.style.backgroundColor = new Color(0.03f, 0.02f, 0.01f, 0.85f); gear.style.alignItems = Align.Center; gear.style.justifyContent = Justify.Center;
                var panel = new VisualElement { pickingMode = PickingMode.Position }; panel.name = "panel"; GoldFrame(panel, 3); panel.style.backgroundColor = new Color(0.12f, 0.08f, 0.04f, 0.97f);
                panel.style.maxWidth = 900; panel.style.maxHeight = Length.Percent(94); Pad(panel, 12, 16); gear.Add(panel);
                var head = new VisualElement(); head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center; head.style.justifyContent = Justify.SpaceBetween; panel.Add(head);
                var t = L(head, "อุปกรณ์ของฉัน", 30, ShopGold); t.style.unityFontDefinition = fontTitle;
                var close = new Button(() => { gear.style.display = DisplayStyle.None; GameInput.Blocked = ModalOpen || CardOpen; }) { text = "ปิด" };
                StyleButton(close, Color.clear); GoldFrame(close, 2); close.style.fontSize = 20; close.style.color = new Color(1f, 0.9f, 0.75f); Pad(close, 4, 18);
                Hoverable(close, new Color(0.3f, 0.22f, 0.08f, 0.9f)); head.Add(close);
                var sv = new ScrollView(ScrollViewMode.Vertical); sv.style.flexGrow = 1; sv.style.flexShrink = 1; sv.style.marginTop = 8; sv.verticalScrollerVisibility = ScrollerVisibility.Hidden; panel.Add(sv);
                gearList = new VisualElement(); sv.Add(gearList);
                var shop = new Button(() => { gear.style.display = DisplayStyle.None; ShowShop(); }) { text = "ไปร้านลุงส้ม อัปเกรดอุปกรณ์" };
                WoodButton(shop, true); shop.style.alignSelf = Align.Center; shop.style.marginTop = 8; shop.style.fontSize = 22; panel.Add(shop);
            }
            gear.Q("panel").style.width = Length.Percent(Narrow ? 96 : 70);
            gear.style.display = DisplayStyle.Flex; gear.BringToFront(); GameInput.Blocked = true;
            gearList.Clear();
            var s = Story.S;
            // การ์ดหนึ่งใบ: รูป · ชื่อ · คำอธิบาย · แถบค่าพลัง (ตอนนี้ / ขั้นสูงสุด)
            void Card(Texture2D art, string icon, string title, string sub, (string label, float now, string val)[] bars, Color tint)
            {
                var c = new VisualElement(); c.style.flexDirection = FlexDirection.Row; c.style.alignItems = Align.Center; c.style.marginBottom = 8; Pad(c, 8, 10); Radius(c, 10);
                c.style.backgroundColor = tint; gearList.Add(c);
                var pic = new VisualElement { pickingMode = PickingMode.Ignore }; pic.style.width = pic.style.height = Narrow ? 64 : 80; pic.style.flexShrink = 0; pic.style.marginRight = 12;
                pic.style.backgroundImage = art != null ? art : Resources.Load<Texture2D>("UI/Icons/" + icon); pic.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain); c.Add(pic);
                var col = new VisualElement(); col.style.flexGrow = 1; col.style.flexShrink = 1; col.style.minWidth = 0; c.Add(col);
                var tl = L(col, title, Narrow ? 19 : 22, new Color(1f, 0.93f, 0.8f)); tl.style.unityTextAlign = TextAnchor.MiddleLeft; tl.style.unityFontDefinition = fontTitle;
                var sl = L(col, sub, 14, new Color(0.88f, 0.82f, 0.7f), false); sl.style.unityTextAlign = TextAnchor.MiddleLeft; sl.style.whiteSpace = WhiteSpace.Normal;
                foreach (var (label, now, val) in bars)
                {
                    var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginTop = 4; col.Add(r);
                    var lb = L(r, label, 14, new Color(0.95f, 0.88f, 0.72f), false); lb.style.width = Narrow ? 92 : 120; lb.style.unityTextAlign = TextAnchor.MiddleLeft; lb.style.flexShrink = 0;
                    var bar = new VisualElement(); bar.style.flexGrow = 1; bar.style.height = 12; bar.style.backgroundColor = new Color(0, 0, 0, 0.45f); Radius(bar, 6); r.Add(bar);
                    var fill = new VisualElement(); fill.style.height = Length.Percent(100); fill.style.width = Length.Percent(Mathf.Clamp01(now) * 100); Radius(fill, 6);
                    fill.style.backgroundColor = now >= 0.99f ? new Color(0.5f, 0.85f, 0.35f) : new Color(0.95f, 0.7f, 0.28f); bar.Add(fill);
                    L(r, val, 14, new Color(1f, 0.86f, 0.5f)).style.marginLeft = 8;
                }
            }
            string[] rods = { "คันไม้ไผ่ของพ่อ", "คันเบ็ดไฟเบอร์", "คันคาร์บอนเบา", "คันเบ็ดครูช่างฝีมือ" };
            string[] reels = { "รอกมือ", "รอกสปินนิ่ง", "รอกเบทคาสติ้ง", "รอกทองเหลืองโบราณ" };
            var dark = new Color(0f, 0f, 0f, 0.3f);
            Card(s.rod > 0 ? Resources.Load<Texture2D>("UI/Shop/rod" + s.rod) : null, "cast", $"คันเบ็ด: {rods[s.rod]}", s.rod >= 3 ? "ขั้นสูงสุดแล้ว" : $"ขั้นถัดไป: {rods[s.rod + 1]} (ร้านลุงส้ม)",
                new[] { ("เหวี่ยงไกล", s.rod / 3f, $"{Shop.CastDist:0} ม."), ("สายตึงช้าลง", s.rod / 3f, $"{(1 - Shop.RodTension) * 100:0}%") }, dark);
            Card(s.reel > 0 ? Resources.Load<Texture2D>("UI/Shop/reel" + s.reel) : null, "reel", $"รอก: {reels[s.reel]}", s.reel >= 3 ? "ขั้นสูงสุดแล้ว" : $"ขั้นถัดไป: {reels[s.reel + 1]} (ร้านลุงส้ม)",
                new[] { ("กรอสายเร็วขึ้น", s.reel / 3f, $"+{(Shop.ReelSpeed - 1) * 100:0}%") }, dark);
            var b = Shop.ActiveBait;
            Card(b == "worm" ? null : Resources.Load<Texture2D>("UI/Shop/" + b), "hook", $"เหยื่อ: {Shop.BaitName(b)}",
                (b == "shrimp" ? "ปลาหายาก/ดาวสูงเข้าบ่อยขึ้น" : b == "bran" ? "ปลากินเร็วขึ้น" : b == "frog" ? "ปลาตัวใหญ่ขึ้น" : "เหยื่อพื้นฐาน ใช้ได้ไม่จำกัด") + (b == "worm" ? "" : $" · เหลือ {Shop.BaitLeft(b)}"),
                new (string, float, string)[0], dark);
            // ทุ่น สาย เบ็ด ที่ใส่อยู่ (เปลี่ยนได้ในร้านลุงส้ม แท็บ "ทุ่น สาย เบ็ด")
            foreach (var slot in new[] { Tackle.Float, Tackle.Line, Tackle.Hook })
            {
                var t = Shop.Get(Tackle.Equipped(slot));
                Card(Resources.Load<Texture2D>("UI/Shop/" + t.id), "hook", $"{(slot == Tackle.Float ? "ทุ่น" : slot == Tackle.Line ? "สาย" : "ตัวเบ็ด")}: {t.name}", t.desc, new (string, float, string)[0], dark);
            }
            if (Tackle.HasDrag) Card(Resources.Load<Texture2D>("UI/Shop/reel_drag"), "reel", "ชุดเบรกรอก", Shop.Get("reel_drag").desc, new (string, float, string)[0], dark);
            Card(null, "eye", "ไฟล่อใต้น้ำ", Shop.HasUwLight ? "มีแล้ว · มองเห็นใต้น้ำชัดขึ้น ปลากินเร็วขึ้นนิด" : "ยังไม่มี · ซื้อที่ร้านลุงส้ม ฿700",
                new[] { ("มองเห็นใต้น้ำ", Shop.HasUwLight ? 1f : 0.4f, Shop.HasUwLight ? "ชัดมาก" : "ปกติ") }, dark);
            Card(null, "trophy", "สู้ปลาบึกยักษ์แม่โขง", Boss.GearReady ? "อุปกรณ์พร้อมแล้ว! ไปด่านแก่งอาถรรพ์พญานาคได้เลย" : "ต้องมีคันคาร์บอนเบา + รอกเบทคาสติ้งขึ้นไป",
                new[] { ("ความพร้อม", Mathf.Clamp01((Mathf.Min(s.rod, 2) + Mathf.Min(s.reel, 2)) / 4f), Boss.GearReady ? "พร้อม" : $"{Mathf.Min(s.rod, 2) + Mathf.Min(s.reel, 2)}/4") },
                Boss.GearReady ? new Color(0.22f, 0.32f, 0.12f, 0.9f) : dark);
        }
    }
}
