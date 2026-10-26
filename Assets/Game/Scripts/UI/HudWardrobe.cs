using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // ตู้แต่งตัว: แท็บตามช่อง (หมวก เสื้อ ทุ่น สายเบ็ด กรอบการ์ด) · ของแต่ละชิ้นมีวงสีตัวอย่าง ราคาเกล็ดทอง ปุ่มซื้อ/ใส่
    public partial class Hud
    {
        VisualElement wardrobe, wardList, wardTabs; Label wardScales; string wardSlot = Cosmetics.Hat;

        void BuildWardrobe()
        {
            wardrobe = new VisualElement { pickingMode = PickingMode.Position }; Abs(wardrobe, 0, 0, 0, 0); root.Add(wardrobe);
            wardrobe.style.backgroundColor = new Color(0.03f, 0.02f, 0.01f, 0.82f); wardrobe.style.alignItems = Align.Center; wardrobe.style.justifyContent = Justify.Center;
            wardrobe.style.display = DisplayStyle.None;
            var panel = new VisualElement { pickingMode = PickingMode.Position }; GoldFrame(panel, 3); panel.style.backgroundColor = new Color(0.12f, 0.08f, 0.04f, 0.97f);
            panel.style.width = Length.Percent(Narrow ? 94 : 58); panel.style.maxWidth = 880; panel.style.maxHeight = Length.Percent(92); Pad(panel, 14, 20); wardrobe.Add(panel);
            var t = L(panel, "ตู้แต่งตัว", Narrow ? 30 : 36, ShopGold); t.style.unityFontDefinition = fontTitle;
            wardScales = L(panel, "", 20, new Color(1f, 0.86f, 0.45f)); wardScales.style.marginTop = 2;
            var note = L(panel, "ของแต่งสวยอย่างเดียว ไม่เพิ่มพลัง · เกล็ดทองได้จากภารกิจรายวัน ปลาเด่น เทศกาล บอส และการดวล", 15, new Color(0.88f, 0.82f, 0.7f), false); note.style.whiteSpace = WhiteSpace.Normal;
            wardTabs = new VisualElement(); wardTabs.style.flexDirection = FlexDirection.Row; wardTabs.style.flexWrap = Wrap.Wrap; wardTabs.style.marginTop = 8; panel.Add(wardTabs);
            foreach (var slot in Cosmetics.Slots)
            {
                var sl = slot;
                var tb = new Button(() => { wardSlot = sl; FillWardrobe(); }) { text = Cosmetics.SlotName(slot) }; tb.name = slot; StyleButton(tb, Color.clear); GoldFrame(tb, 1);
                tb.style.fontSize = 17; tb.style.color = new Color(0.95f, 0.88f, 0.72f); Pad(tb, 5, 12); tb.style.marginRight = 5; tb.style.marginTop = 4;
                Hoverable(tb, new Color(0.38f, 0.28f, 0.1f, 0.95f)); wardTabs.Add(tb);
            }
            var sv = new ScrollView(ScrollViewMode.Vertical); sv.style.flexGrow = 1; sv.style.flexShrink = 1; sv.style.marginTop = 8; sv.verticalScrollerVisibility = ScrollerVisibility.Hidden; panel.Add(sv);
            wardList = new VisualElement(); sv.Add(wardList);
            var close = new Button(() => { wardrobe.style.display = DisplayStyle.None; GameInput.Blocked = ModalOpen || CardOpen; }) { text = "ปิด" };
            StyleButton(close, Color.clear); GoldFrame(close, 2); close.style.alignSelf = Align.Center; close.style.marginTop = 10; close.style.fontSize = 22; close.style.color = new Color(1f, 0.9f, 0.75f); Pad(close, 6, 28);
            Hoverable(close, new Color(0.3f, 0.22f, 0.08f, 0.9f)); panel.Add(close);
        }

        public void OpenWardrobe()
        {
            if (wardrobe == null) BuildWardrobe();
            wardrobe.style.display = DisplayStyle.Flex; wardrobe.BringToFront(); GameInput.Blocked = true;
            FillWardrobe();
        }

        void FillWardrobe()
        {
            wardScales.text = $"เกล็ดทองที่มี: {Cosmetics.Scales}";
            foreach (var tb in wardTabs.Children())
            {
                bool on = tb.name == wardSlot;
                tb.style.backgroundColor = on ? new Color(0.55f, 0.4f, 0.12f, 1f) : (StyleColor)Color.clear; tb.style.color = on ? Color.white : new Color(0.95f, 0.88f, 0.72f);
            }
            wardList.Clear();
            var eq = Cosmetics.Equipped(wardSlot);
            foreach (var it in Cosmetics.Items)
            {
                if (it.slot != wardSlot) continue;
                var item = it;
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 6; Pad(row, 8, 12); Radius(row, 8);
                row.style.backgroundColor = eq.id == it.id ? new Color(0.35f, 0.26f, 0.08f, 0.9f) : new Color(0f, 0f, 0f, 0.28f); wardList.Add(row);
                var sw = new VisualElement(); sw.style.width = sw.style.height = 40; sw.style.flexShrink = 0; sw.style.marginRight = 12; Radius(sw, 20);   // วงสีตัวอย่าง (สองสีถ้ามี)
                sw.style.backgroundColor = it.color; sw.style.borderBottomWidth = sw.style.borderTopWidth = sw.style.borderLeftWidth = sw.style.borderRightWidth = 5;
                sw.style.borderBottomColor = sw.style.borderTopColor = sw.style.borderLeftColor = sw.style.borderRightColor = it.color2.a > 0 ? it.color2 : new Color(0.3f, 0.2f, 0.1f); row.Add(sw);
                var col = new VisualElement(); col.style.flexGrow = 1; col.style.flexShrink = 1; row.Add(col);
                var nm = L(col, it.name, 20, new Color(0.97f, 0.92f, 0.82f)); nm.style.unityTextAlign = TextAnchor.MiddleLeft;
                var ds = L(col, it.desc, 15, new Color(0.85f, 0.8f, 0.68f), false); ds.style.unityTextAlign = TextAnchor.MiddleLeft; ds.style.whiteSpace = WhiteSpace.Normal;
                bool own = Cosmetics.Owns(it);
                string label = eq.id == it.id ? "ใส่อยู่" : own ? "ใส่" : $"ซื้อ {it.price} เกล็ด";
                var b = new Button(() =>
                {
                    if (Cosmetics.Owns(item)) Cosmetics.Equip(item);
                    else if (!Cosmetics.Buy(item)) { Toast("เกล็ดทองไม่พอ · ทำภารกิจรายวันหรือไปเทศกาลปลาใหญ่เพื่อสะสม"); return; }
                    else { Sfx.Coin(); Toast($"ได้{item.name}แล้ว!"); }
                    FillWardrobe();
                }) { text = label };
                WoodButton(b, eq.id != it.id); b.style.fontSize = 18; Pad(b, 8, 16); b.style.flexShrink = 0; b.style.marginLeft = 8;
                b.SetEnabled(eq.id != it.id && (own || Cosmetics.Scales >= it.price));
                row.Add(b);
            }
        }
    }
}
