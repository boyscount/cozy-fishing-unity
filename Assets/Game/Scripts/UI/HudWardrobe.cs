using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // ตู้แต่งตัว: ซ้าย = หุ่นจำลองใส่ของที่เลือก (หมวก เสื้อ) + ทุ่น สายเบ็ด กรอบการ์ด · ขวา = แท็บตามช่อง แต่ละชิ้นมีรูปตัวอย่าง
    //   แตะรายการ = ลองใส่ดูบนหุ่นก่อน · ปุ่มขวา = ซื้อ/ใส่
    public partial class Hud
    {
        VisualElement wardrobe, wardList, wardTabs, wardPanel, wardBody, wardPreview; Label wardScales, wardPreviewName; string wardSlot = Cosmetics.Hat;
        Cosmetics.Item wardTry;   // ชิ้นที่กำลังลองดู (ยังไม่ได้ซื้อก็ลองได้)

        static Texture2D WTex(string n) => Resources.Load<Texture2D>("UI/Wardrobe/" + n);
        static Color ShirtShow(Cosmetics.Item it) => it.color == Color.white ? new Color(0.37f, 0.53f, 0.77f) : it.color;   // เสื้อตัวเดิม (ไม่ย้อมในเกม) แสดงสีน้ำเงินตามโมเดล

        /// รูปไอคอนของแต่ละชิ้น (ชั้นภาพย้อมสี)
        VisualElement ItemIcon(Cosmetics.Item it, float size)
        {
            var box = new VisualElement { pickingMode = PickingMode.Ignore }; box.style.width = box.style.height = size; box.style.flexShrink = 0;
            void Layer(string tex, Color c, float scale = 1f, float dy = 0)
            {
                var l = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(l, 0, 0, 0, 0); l.style.backgroundImage = WTex(tex);
                l.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain); l.style.unityBackgroundImageTintColor = c;
                if (scale != 1f) { l.style.scale = new Scale(Vector3.one * scale); l.style.translate = new Translate(0, Length.Percent(dy)); }
                box.Add(l);
            }
            switch (it.slot)
            {
                case Cosmetics.Hat: Layer("doll_hat", it.color, 2.6f, 62); Layer("doll_hatband", it.color2, 2.6f, 62); break;
                case Cosmetics.Shirt: Layer("doll_shirt", ShirtShow(it), 1.9f, 10); break;
                case Cosmetics.Bobber: Layer("ico_bobber", it.color); Layer("ico_bobber_tip", it.color2); break;
                case Cosmetics.Line: Layer("ico_rod", Color.white); Layer("ico_line", it.color); break;
                default: Layer("ico_frame_in", Color.white); Layer("ico_frame", it.color); break;
            }
            return box;
        }

        void BuildWardrobe()
        {
            wardrobe = new VisualElement { pickingMode = PickingMode.Position }; Abs(wardrobe, 0, 0, 0, 0); root.Add(wardrobe);
            wardrobe.style.backgroundColor = new Color(0.03f, 0.02f, 0.01f, 0.85f); wardrobe.style.alignItems = Align.Center; wardrobe.style.justifyContent = Justify.Center;
            wardrobe.style.display = DisplayStyle.None;
            var panel = wardPanel = new VisualElement { pickingMode = PickingMode.Position }; GoldFrame(panel, 3); panel.style.backgroundColor = new Color(0.12f, 0.08f, 0.04f, 0.97f);
            panel.style.maxWidth = 1000; panel.style.maxHeight = Length.Percent(94); Pad(panel, 12, 16); wardrobe.Add(panel);
            var head = new VisualElement(); head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center; head.style.justifyContent = Justify.SpaceBetween; panel.Add(head);
            var t = L(head, "ตู้แต่งตัว", 30, ShopGold); t.style.unityFontDefinition = fontTitle;
            var sc = new VisualElement(); sc.style.flexDirection = FlexDirection.Row; sc.style.alignItems = Align.Center; head.Add(sc);
            Ico(sc, "star_on", 26).style.marginRight = 6; wardScales = L(sc, "", 22, new Color(1f, 0.86f, 0.45f));
            var close = new Button(() => { wardrobe.style.display = DisplayStyle.None; GameInput.Blocked = ModalOpen || CardOpen; }) { text = "ปิด" };
            StyleButton(close, Color.clear); GoldFrame(close, 2); close.style.fontSize = 20; close.style.color = new Color(1f, 0.9f, 0.75f); Pad(close, 4, 18); close.style.marginLeft = 12;
            Hoverable(close, new Color(0.3f, 0.22f, 0.08f, 0.9f)); sc.Add(close);
            wardBody = new VisualElement(); wardBody.style.flexGrow = 1; wardBody.style.flexShrink = 1; wardBody.style.marginTop = 8; panel.Add(wardBody);
            // ซ้าย: หุ่นจำลอง
            var left = new VisualElement(); left.style.alignItems = Align.Center; left.style.flexShrink = 0; Pad(left, 8, 8); Radius(left, 10);
            left.style.backgroundColor = new Color(0.25f, 0.18f, 0.1f, 0.85f); wardBody.Add(left);
            wardPreview = new VisualElement { pickingMode = PickingMode.Ignore }; left.Add(wardPreview);
            wardPreviewName = L(left, "", 16, new Color(1f, 0.92f, 0.78f), false); wardPreviewName.style.whiteSpace = WhiteSpace.Normal; wardPreviewName.style.unityTextAlign = TextAnchor.MiddleCenter;
            // ขวา: แท็บ + รายการ
            var right = new VisualElement(); right.style.flexGrow = 1; right.style.flexShrink = 1; right.style.minWidth = 0; wardBody.Add(right);
            wardTabs = new VisualElement(); wardTabs.style.flexDirection = FlexDirection.Row; wardTabs.style.flexWrap = Wrap.Wrap; right.Add(wardTabs);
            foreach (var slot in Cosmetics.Slots)
            {
                var sl = slot;
                var tb = new Button(() => { wardSlot = sl; wardTry = null; FillWardrobe(); }) { text = Cosmetics.SlotName(slot) }; tb.name = slot; StyleButton(tb, Color.clear); GoldFrame(tb, 1);
                tb.style.fontSize = 17; tb.style.color = new Color(0.95f, 0.88f, 0.72f); Pad(tb, 5, 12); tb.style.marginRight = 5; tb.style.marginBottom = 5;
                Hoverable(tb, new Color(0.38f, 0.28f, 0.1f, 0.95f)); wardTabs.Add(tb);
            }
            var sv = new ScrollView(ScrollViewMode.Vertical); sv.style.flexGrow = 1; sv.style.flexShrink = 1; sv.verticalScrollerVisibility = ScrollerVisibility.Hidden; sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden; right.Add(sv);
            wardList = new VisualElement(); sv.Add(wardList);
            var note = L(panel, "สวยอย่างเดียว ไม่เพิ่มพลัง · เกล็ดทองได้จากภารกิจรายวัน ปลาเด่น เทศกาล บอส และการดวล", 14, new Color(0.82f, 0.76f, 0.64f), false); note.style.whiteSpace = WhiteSpace.Normal; note.style.marginTop = 6;
        }

        public void OpenWardrobe()
        {
            if (wardrobe == null) BuildWardrobe();
            wardrobe.style.display = DisplayStyle.Flex; wardrobe.BringToFront(); GameInput.Blocked = true; wardTry = null;
            FillWardrobe();
        }

        // หุ่นจำลอง: หมวก/เสื้อตามที่ใส่อยู่ (หรือชิ้นที่กำลังลอง) · ใต้หุ่นมีทุ่น สายเบ็ด กรอบการ์ดที่ใช้อยู่
        void FillPreview(bool portrait)
        {
            wardPreview.Clear();
            Cosmetics.Item Pick(string slot) => wardTry != null && wardTry.slot == slot ? wardTry : Cosmetics.Equipped(slot);
            var hat = Pick(Cosmetics.Hat); var shirt = Pick(Cosmetics.Shirt);
            float dw = portrait ? 120 : 170, dh = dw * 520f / 360f;
            var doll = new VisualElement { pickingMode = PickingMode.Ignore }; doll.style.width = dw; doll.style.height = dh; wardPreview.Add(doll);
            void Layer(string tex, Color c) { var l = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(l, 0, 0, 0, 0); l.style.backgroundImage = WTex(tex); l.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain); l.style.unityBackgroundImageTintColor = c; doll.Add(l); }
            Layer("doll_base", Color.white); Layer("doll_shirt", ShirtShow(shirt)); Layer("doll_hat", hat.color); Layer("doll_hatband", hat.color2);
            var gear = new VisualElement { pickingMode = PickingMode.Ignore }; gear.style.flexDirection = FlexDirection.Row; gear.style.justifyContent = Justify.Center; gear.style.marginTop = 4; wardPreview.Add(gear);
            foreach (var s in new[] { Cosmetics.Bobber, Cosmetics.Line, Cosmetics.Card }) { var ic = ItemIcon(Pick(s), portrait ? 36 : 44); ic.style.marginLeft = ic.style.marginRight = 3; gear.Add(ic); }
            wardPreviewName.text = wardTry != null ? $"กำลังลอง: {wardTry.name}" + (Cosmetics.Owns(wardTry) ? "" : $" ({wardTry.price} เกล็ด)") : "ชุดที่ใส่อยู่";
        }

        void FillWardrobe()
        {
            bool portrait = root.layout.height > root.layout.width && root.layout.width > 0;
            wardPanel.style.width = Length.Percent(Narrow ? 96 : 80);
            wardBody.style.flexDirection = portrait ? FlexDirection.Column : FlexDirection.Row;
            wardBody.ElementAt(0).style.marginRight = portrait ? 0 : 12; wardBody.ElementAt(0).style.marginBottom = portrait ? 8 : 0;
            wardScales.text = $"เกล็ดทอง {Cosmetics.Scales}";
            FillPreview(portrait);
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
                bool own = Cosmetics.Owns(it), wearing = eq.id == it.id, canBuy = own || Cosmetics.Scales >= it.price, trying = wardTry == it;
                var row = new VisualElement { pickingMode = PickingMode.Position }; row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginBottom = 6; Pad(row, 6, 10); Radius(row, 8);
                row.style.backgroundColor = trying ? new Color(0.42f, 0.3f, 0.1f, 0.95f) : wearing ? new Color(0.22f, 0.32f, 0.12f, 0.9f) : new Color(0f, 0f, 0f, 0.3f);
                row.RegisterCallback<PointerDownEvent>(e => { wardTry = item; FillWardrobe(); });   // แตะ = ลองใส่บนหุ่น
                wardList.Add(row);
                var ic = ItemIcon(it, Narrow ? 52 : 60); ic.style.marginRight = 10; Radius(ic, 8); ic.style.backgroundColor = new Color(0.32f, 0.24f, 0.14f, 0.9f); row.Add(ic);
                var col = new VisualElement { pickingMode = PickingMode.Ignore }; col.style.flexGrow = 1; col.style.flexShrink = 1; col.style.minWidth = 0; row.Add(col);
                var nm = L(col, it.name, Narrow ? 18 : 20, new Color(0.97f, 0.92f, 0.82f)); nm.style.unityTextAlign = TextAnchor.MiddleLeft; nm.style.whiteSpace = WhiteSpace.Normal;
                var ds = L(col, it.desc, 14, new Color(0.85f, 0.8f, 0.68f), false); ds.style.unityTextAlign = TextAnchor.MiddleLeft; ds.style.whiteSpace = WhiteSpace.Normal;
                var b = new Button(() =>
                {
                    if (Cosmetics.Owns(item)) Cosmetics.Equip(item);
                    else if (!Cosmetics.Buy(item)) { Toast("เกล็ดทองไม่พอ · ทำภารกิจรายวันหรือไปเทศกาลปลาใหญ่เพื่อสะสม"); return; }
                    else { Sfx.Coin(); Toast($"ได้{item.name}แล้ว!"); }
                    wardTry = null; FillWardrobe();
                }) { text = wearing ? "ใส่อยู่" : own ? "ใส่" : canBuy ? $"ซื้อ {it.price}" : $"{it.price} เกล็ด" };
                StyleButton(b, wearing ? new Color(0.25f, 0.42f, 0.12f) : own ? new Color(0.62f, 0.44f, 0.12f) : canBuy ? new Color(0.72f, 0.36f, 0.1f) : new Color(0.3f, 0.26f, 0.22f));
                b.style.color = canBuy || wearing ? Color.white : new Color(0.78f, 0.72f, 0.66f); b.style.fontSize = Narrow ? 16 : 18; Pad(b, 8, 12); b.style.flexShrink = 0; b.style.marginLeft = 8; b.style.minWidth = Narrow ? 84 : 110; Radius(b, 8);
                b.SetEnabled(!wearing && canBuy); b.style.opacity = 1;
                row.Add(b);
            }
        }
    }
}
