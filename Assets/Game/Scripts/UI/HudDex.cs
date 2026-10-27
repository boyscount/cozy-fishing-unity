using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static CozyFishing.HudKit;

namespace CozyFishing
{
    // สมุดบันทึกพรานปลา: หนังสือกางสองหน้า
    //   หน้าซ้าย = สารบัญ (สะสมแล้วกี่ชนิด · รายชื่อที่ตกปลาพร้อมจำนวน "ครบ ★" · ตรายศพรานปลา)
    //   หน้าขวา = ปลาประจำถิ่นของที่ที่เลือก การ์ด 2×2 พลิกหน้าได้ · ปลาที่ยังไม่เจอเป็นเงาดำ + คำใบ้ว่าออกหากินตอนไหน
    //   จอแคบ (มือถือแนวตั้ง): สารบัญย่อเหลือแถบ "◀ ชื่อที่ ▶" ด้านบน หน้าปลาอยู่ข้างล่าง
    public partial class Hud
    {
        VisualElement dex, dexBtn, dexBody; Label dexCount; string dexStage = "paddy"; int dexPage;
        static readonly Color DexInk = new Color(0.24f, 0.14f, 0.06f), DexInkSoft = new Color(0.42f, 0.3f, 0.17f), DexRed = new Color(0.72f, 0.14f, 0.1f);

        static (string id, string name)[] DexStages => CozyFishing.Stages.All.Select(s => (s.id, s.name)).ToArray();
        static int DexTotal => FishDatabase.All.Count(sp => CozyFishing.Stages.All.Any(st => st.id == sp.stage));

        void BuildDex()
        {
            dexBtn = IconButton("fishbook", new Color(0.85f, 0.7f, 0.38f), OpenDex, out dexCount);
            dexCount.text = $"{FishDex.Discovered}/{DexTotal}";

            dex = new VisualElement(); Abs(dex, 0, 0, 0, 0); dex.style.backgroundColor = new Color(0.06f, 0.08f, 0.06f, 0.72f);
            dex.style.justifyContent = Justify.Center; dex.style.alignItems = Align.Center; root.Add(dex);
            var book = Box(dex, Color.clear, 0, false); book.name = "book"; book.pickingMode = PickingMode.Position;
            book.style.maxWidth = 1180; book.style.width = Length.Percent(96); book.style.maxHeight = Length.Percent(96); book.style.flexShrink = 1;
            Skin(book, "book", 40);

            // หัว: ป้ายไม้ชื่อสมุด + ตราครั่ง "ปิด"
            var head = new VisualElement { name = "head", pickingMode = PickingMode.Ignore }; head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center; head.style.justifyContent = Justify.Center; head.style.flexShrink = 0; book.Add(head);
            var sign = new VisualElement { name = "sign", pickingMode = PickingMode.Ignore }; Skin(sign, "plank", 22); sign.style.flexShrink = 1; head.Add(sign);
            var t = L(sign, "สมุดบันทึกพรานปลา", 34, new Color(0.22f, 0.12f, 0.04f)); t.name = "title"; t.style.unityFontDefinition = fontTitle;
            var close = new VisualElement { name = "close", pickingMode = PickingMode.Position }; close.style.flexShrink = 0;
            Abs(close, null, 0, null);   // ตราครั่งแดงมีคำว่า "ปิด" ในภาพอยู่แล้ว
            close.style.backgroundImage = Resources.Load<Texture2D>("UI/Icons/seal"); head.Add(close);
            close.RegisterCallback<PointerUpEvent>(e => { dex.style.display = DisplayStyle.None; GameInput.Blocked = ModalOpen; });

            dexBody = new VisualElement { pickingMode = PickingMode.Ignore }; dexBody.style.flexGrow = 1; dexBody.style.flexShrink = 1; dexBody.style.minHeight = 0; book.Add(dexBody);
            dex.style.display = DisplayStyle.None;
        }

        void OpenDex()
        {
            if (CardOpen) return;
            GameInput.Blocked = true;
            dex.style.display = DisplayStyle.Flex;
            FillDex();
        }

        // ยศพรานปลาตามจำนวนที่สะสม (ตราประทับแดงหน้าซ้าย)
        static string DexRank(float pct) =>
            pct >= 1f ? "ปรมาจารย์แห่งสายน้ำ" : pct >= 0.75f ? "พรานปลาผู้เชี่ยวชาญ" : pct >= 0.5f ? "พรานปลาชำนาญการ" : pct >= 0.25f ? "พรานปลาฝึกหัด" : "พรานปลามือใหม่";

        // คำใบ้ของปลาที่ยังไม่เจอ: ออกหากินช่วงไหน
        static string DexHint(FishSpecies sp)
        {
            if (!CozyFishing.Stages.Unlocked(sp.stage)) return "ที่ตกปลานี้ยังไม่เปิด";
            switch (sp.time)
            {
                case "night": return "ออกหากินตอนกลางคืน";
                case "morning": return "ชอบออกหากินตอนเช้า";
                case "evening": return "ออกหากินตอนเย็น";
                default: return sp.stars >= 4 ? "หายาก ต้องอดทนรอ" : "พบได้ทั้งวัน";
            }
        }

        void FillDex()
        {
            bool n = Narrow;
            var book = dex.Q("book");
            book.style.paddingTop = n ? 22 : 30; book.style.paddingBottom = n ? 22 : 30; book.style.paddingLeft = n ? 30 : 48; book.style.paddingRight = n ? 30 : 48;
            var sign = dex.Q("sign"); Pad(sign, n ? 6 : 8, n ? 22 : 40);
            dex.Q<Label>("title").style.fontSize = n ? 24 : 34;
            var close = dex.Q("close"); close.style.width = close.style.height = n ? 58 : 76;
            dexBody.Clear();
            dexBody.style.flexDirection = n ? FlexDirection.Column : FlexDirection.Row; dexBody.style.marginTop = n ? 8 : 12;

            var stages = DexStages;
            int allGot = FishDex.Discovered, allTotal = DexTotal;
            float pct = allTotal > 0 ? allGot / (float)allTotal : 0;
            (int got, int total) Count(string id) { int g = 0, tt = 0; foreach (var sp in FishDatabase.ForStage(id)) { tt++; if (FishDex.Has(sp.id)) g++; } return (g, tt); }

            // ---------- หน้าซ้าย: สารบัญ ----------
            var left = new VisualElement { pickingMode = PickingMode.Ignore }; dexBody.Add(left);
            if (!n) { left.style.width = Length.Percent(36); left.style.paddingRight = 26; left.style.flexShrink = 0; }
            var sum = L(left, $"สะสมแล้ว {allGot}/{allTotal} ชนิด ({Mathf.RoundToInt(pct * 100)}%)", n ? 19 : 22, DexInk);
            sum.style.unityTextAlign = TextAnchor.MiddleLeft;
            var bar = new VisualElement { pickingMode = PickingMode.Ignore }; bar.style.height = 10; bar.style.marginTop = 6; bar.style.flexShrink = 0;
            bar.style.backgroundColor = new Color(0.4f, 0.28f, 0.14f, 0.25f); Radius(bar, 5); left.Add(bar);
            var fill = new VisualElement { pickingMode = PickingMode.Ignore }; fill.style.height = Length.Percent(100); fill.style.width = Length.Percent(pct * 100);
            fill.style.backgroundColor = new Color(0.78f, 0.52f, 0.18f); Radius(fill, 5); bar.Add(fill);

            if (n)
            {   // จอแคบ: แถบเลือกที่ตกปลา ◀ ชื่อ ▶
                int idx = System.Array.FindIndex(stages, s => s.id == dexStage); if (idx < 0) idx = 0;
                var row = new VisualElement { pickingMode = PickingMode.Ignore }; row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 8; row.style.flexShrink = 0; left.Add(row);
                Button Arrow(string s, int d)
                {
                    var b = new Button(() => { dexStage = stages[(idx + d + stages.Length) % stages.Length].id; dexPage = 0; FillDex(); }) { text = s };
                    WoodButton(b, true); b.style.fontSize = 22; Pad(b, 6, 16); b.style.minHeight = 44; b.style.minWidth = 54; row.Add(b); return b;
                }
                Arrow("◀", -1);
                var (g, tt) = Count(dexStage);
                var mid = L(row, $"{stages[idx].name}\n<size=80%>{(g == tt ? $"ครบ {tt}/{tt} ★" : $"{g}/{tt} ชนิด")}</size>", 21, DexInk);
                mid.style.flexGrow = 1; mid.style.flexShrink = 1; mid.style.unityFontDefinition = fontTitle;
                Arrow("▶", 1);
            }
            else
            {
                var hd = L(left, "สารบัญที่ตกปลา", 22, DexInkSoft); hd.style.unityTextAlign = TextAnchor.MiddleLeft; hd.style.marginTop = 14; hd.style.marginBottom = 4;
                var list = new ScrollView(ScrollViewMode.Vertical); list.style.flexGrow = 1; list.style.flexShrink = 1; list.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                StyleScroller(list); left.Add(list);
                foreach (var (id, name) in stages)
                {
                    var (g, tt) = Count(id); bool sel = id == dexStage, open = CozyFishing.Stages.Unlocked(id), full = tt > 0 && g == tt;
                    string sid = id;
                    var b = new Button(() => { dexStage = sid; dexPage = 0; FillDex(); }) { text = "" };
                    StyleButton(b, sel ? new Color(0.62f, 0.38f, 0.14f, 0.28f) : Color.clear); Pad(b, 7, 10); Radius(b, 8);
                    b.style.flexDirection = FlexDirection.Row; b.style.alignItems = Align.Center; b.style.marginBottom = 2;
                    b.style.borderBottomWidth = 1; b.style.borderBottomColor = new Color(0.4f, 0.28f, 0.14f, 0.25f);
                    if (!sel) Hoverable(b, new Color(0.62f, 0.38f, 0.14f, 0.14f));
                    var mark = L(b, sel ? "▶" : "", 16, DexRed); mark.style.width = 18;
                    var nm = L(b, name, Loc.English ? 19 : 21, open ? DexInk : DexInkSoft); nm.style.flexGrow = 1; nm.style.flexShrink = 1; nm.style.unityTextAlign = TextAnchor.MiddleLeft;
                    if (!open) { var lk = Ico(b, "lock", 18); lk.style.marginLeft = 4; lk.style.opacity = 0.7f; }
                    var ct = L(b, full ? $"{g}/{tt} ครบ ★" : $"{g}/{tt}", 19, full ? new Color(0.7f, 0.42f, 0.04f) : DexInkSoft); ct.style.marginLeft = 6;
                    list.Add(b);
                }
            }
            // ตรายศพรานปลา (ตราประทับแดง เอียงนิด ๆ)
            var stamp = new VisualElement { pickingMode = PickingMode.Ignore }; stamp.style.alignSelf = n ? Align.Center : Align.FlexStart;
            stamp.style.marginTop = n ? 8 : 14; stamp.style.flexShrink = 0; Pad(stamp, n ? 4 : 6, n ? 12 : 16); Radius(stamp, 6);
            stamp.style.borderTopWidth = stamp.style.borderBottomWidth = stamp.style.borderLeftWidth = stamp.style.borderRightWidth = 3;
            stamp.style.borderTopColor = stamp.style.borderBottomColor = stamp.style.borderLeftColor = stamp.style.borderRightColor = new Color(DexRed.r, DexRed.g, DexRed.b, 0.85f);
            stamp.style.rotate = new Rotate(-4); left.Add(stamp);
            var rk = L(stamp, $"ยศ: {DexRank(pct)}", n ? 17 : 21, new Color(DexRed.r, DexRed.g, DexRed.b, 0.92f)); rk.style.unityFontDefinition = fontTitle;
            if (n) { left.style.flexShrink = 0; left.Insert(2, stamp); stamp.style.marginTop = 6; stamp.style.marginBottom = 2; }   // จอแคบ: ตรายศใต้แถบสะสม ก่อนแถบเลือกที่

            // ---------- หน้าขวา: ปลาประจำถิ่น 2×2 ----------
            var right = new VisualElement { pickingMode = PickingMode.Ignore }; right.style.flexGrow = 1; right.style.flexShrink = 1; right.style.minHeight = 0; dexBody.Add(right);
            if (!n) { right.style.paddingLeft = 26; right.style.borderLeftWidth = 2; right.style.borderLeftColor = new Color(0.4f, 0.28f, 0.14f, 0.18f); }
            else right.style.marginTop = 8;
            var fish = FishDatabase.ForStage(dexStage).ToList();
            int pages = Mathf.Max(1, Mathf.CeilToInt(fish.Count / 4f)); dexPage = Mathf.Clamp(dexPage, 0, pages - 1);
            var stName = stages.FirstOrDefault(s => s.id == dexStage).name ?? "";
            if (!n) { var rt = L(right, $"ปลาประจำถิ่น · {stName}", 24, DexInk); rt.style.unityFontDefinition = fontTitle; rt.style.unityTextAlign = TextAnchor.MiddleLeft; rt.style.marginBottom = 6; }

            var sv = new ScrollView(ScrollViewMode.Vertical); sv.style.flexGrow = 1; sv.style.flexShrink = 1; sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden; StyleScroller(sv); right.Add(sv);
            var grid = new VisualElement { pickingMode = PickingMode.Ignore }; grid.style.flexDirection = FlexDirection.Row; grid.style.flexWrap = Wrap.Wrap; grid.style.justifyContent = Justify.SpaceBetween; sv.Add(grid);
            for (int i = dexPage * 4; i < Mathf.Min(fish.Count, dexPage * 4 + 4); i++) grid.Add(DexCard(fish[i], n));

            // ตัวพลิกหน้า << หน้า 1 / 2 >>
            var pager = new VisualElement { pickingMode = PickingMode.Ignore }; pager.style.flexDirection = FlexDirection.Row; pager.style.justifyContent = Justify.Center; pager.style.alignItems = Align.Center; pager.style.marginTop = 6; pager.style.flexShrink = 0; right.Add(pager);
            Button Flip(string s, int d)
            {
                var b = new Button(() => { dexPage += d; FillDex(); }) { text = s };
                WoodButton(b, true); b.style.fontSize = 20; Pad(b, 4, 16); b.style.minHeight = 40; b.style.minWidth = 56;
                bool ok = dexPage + d >= 0 && dexPage + d < pages; b.SetEnabled(ok); b.style.opacity = ok ? 1 : 0.35f; pager.Add(b); return b;
            }
            Flip("◀◀", -1);
            var pl = L(pager, $"หน้า {dexPage + 1} / {pages}", 20, DexInk); pl.style.marginLeft = pl.style.marginRight = 14;
            Flip("▶▶", 1);

            dexCount.text = $"{allGot}/{allTotal}";
            TextLocalizer.Now();
        }

        VisualElement DexCard(FishSpecies sp, bool n)
        {
            bool has = FishDex.Has(sp.id);
            var cell = new VisualElement { pickingMode = PickingMode.Ignore }; Skin(cell, "parchment", 18); Pad(cell, n ? 6 : 8, n ? 8 : 12);
            cell.style.width = Length.Percent(49); cell.style.marginBottom = n ? 6 : 8; cell.style.flexShrink = 0;
            if (!has) cell.style.unityBackgroundImageTintColor = new Color(0.86f, 0.84f, 0.8f);
            // ภาพวาดปลา (ยังไม่เจอ = เงาดำ)
            var img = new VisualElement { pickingMode = PickingMode.Ignore }; img.style.height = n ? 64 : 84; img.style.flexShrink = 0;
            img.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            if (sp.Photo != null) { img.style.backgroundImage = Background.FromTexture2D(sp.Photo); img.style.unityBackgroundImageTintColor = has ? Color.white : new Color(0.05f, 0.04f, 0.03f, 0.88f); }
            else if (preview != null) img.style.backgroundImage = Background.FromRenderTexture(preview.Icon(sp, !has));
            cell.Add(img);
            var nm = L(cell, has ? sp.name : "??? (ไม่ทราบชื่อ)", n ? 17 : 22, has ? DexInk : DexInkSoft); nm.style.unityFontDefinition = fontTitle; nm.style.marginTop = 2;
            if (has)
            {
                var rr = new VisualElement { pickingMode = PickingMode.Ignore }; rr.style.flexDirection = FlexDirection.Row; rr.style.justifyContent = Justify.Center; rr.style.alignItems = Align.Center; rr.style.flexWrap = Wrap.Wrap; cell.Add(rr);
                for (int k = 0; k < 4; k++) Ico(rr, k < sp.stars ? "star_on" : "star_off", n ? 13 : 16);
                L(rr, $"นน. {FishDex.BestKg(sp.id):0.0#} กก.", n ? 13 : 17, DexInkSoft, false).style.marginLeft = 6;
                L(rr, $"ตกได้ {FishDex.Count(sp.id)} ตัว", n ? 13 : 17, DexInkSoft, false).style.marginLeft = 8;
                if (!n && sp.Credit != null) { var cr = L(cell, sp.Credit, 11, new Color(0.45f, 0.36f, 0.26f), false); cr.style.marginTop = 1; }   // เครดิตภาพถ่าย (CC BY / BY-SA ต้องให้เครดิต)
            }
            else L(cell, $"({DexHint(sp)})", n ? 14 : 18, DexInkSoft, false).style.marginTop = 2;
            // ความลับของปลา: ปลดล็อกแล้วเห็นข้อความ · ยังไม่ปลดล็อกเห็นเงื่อนไข
            var sec = Mystery.SecretOf(sp.id);
            if (sec != null && has)
            {
                bool known = Mystery.SecretKnown(sp.id);
                var sb = Box(cell, known ? new Color(0.3f, 0.16f, 0.1f, 0.88f) : new Color(0.22f, 0.17f, 0.12f, 0.72f), 8, false); Pad(sb, 2, 8); sb.style.marginTop = 3;
                L(sb, known ? "ความลับ: " + sec.text : "ความลับ: ??? (" + sec.hint + ")", n ? 11 : 14, known ? new Color(1f, 0.85f, 0.6f) : new Color(0.9f, 0.84f, 0.74f), false);
            }
            return cell;
        }
    }
}
