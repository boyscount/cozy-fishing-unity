using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // ร้านลุงส้ม (เปิดจากกระดานหน้าเช้า): ภาพร้านไม้มีลุงยืนหลังเคาน์เตอร์เต็มจอ · แผงรายการสินค้าซ้าย (ขอบทอง) · กล่องรายละเอียด + ปุ่มซื้อ
    //   กล่องคำพูดลุงส้มด้านล่าง (พูดตามหมวด/ของที่เลือก) · ปุ่มกลับมุมซ้ายล่าง
    public partial class Hud
    {
        VisualElement shop, shopPanel, shopList, shopTabs, shopDetail; Label shopMoney, shopSay, dName, dDesc, dPrice; Button dBuy;
        string shopCat = Shop.Rods; ShopItem shopSel;
        static readonly Color ShopGold = new Color(0.86f, 0.7f, 0.38f), PanelBg = new Color(0.07f, 0.06f, 0.045f, 0.9f);

        static void GoldFrame(VisualElement e, float w = 2)
        {
            e.style.backgroundColor = PanelBg;
            e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = w;
            e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = new Color(ShopGold.r, ShopGold.g, ShopGold.b, 0.85f);
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = 6;
        }

        // ปุ่มในร้านให้รู้ว่ากดได้: ชี้แล้วขยายนิด พื้นสว่างขึ้น ขอบทองเรือง · กดแล้วยุบลง (จอสัมผัสเห็นตอนนิ้วแตะ)
        static void Hoverable(VisualElement e, Color hoverBg)
        {
            e.AddToClassList("pressable");   // มีผลตอบสนองของตัวเองแล้ว ไม่ต้องใส่แบบทั่วไปซ้ำ
            e.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("scale"), new StylePropertyName("background-color"), new StylePropertyName("border-top-color"), new StylePropertyName("border-bottom-color"), new StylePropertyName("border-left-color"), new StylePropertyName("border-right-color") };
            e.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(0.12f) };
            StyleColor bg = default, bc = default; bool over = false; var hoverBorder = new Color(1f, 0.88f, 0.45f);
            void On()
            {
                if (over || !e.enabledInHierarchy) return; over = true;
                bg = e.style.backgroundColor; bc = e.style.borderTopColor;
                e.style.backgroundColor = hoverBg; e.style.scale = new Scale(Vector3.one * 1.04f);
                e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = hoverBorder;
            }
            void Off()
            {
                if (!over) return; over = false;
                e.style.scale = new Scale(Vector3.one);
                // คืนสีเดิมเฉพาะเมื่อยังเป็นสี hover อยู่ · ถ้าระหว่างนั้นปุ่มถูกตั้งเป็น "เลือกอยู่" (แท็บที่กด) อย่าไปทับสีนั้น
                if (e.style.backgroundColor == hoverBg) e.style.backgroundColor = bg;
                if (e.style.borderTopColor == hoverBorder) e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = bc;
            }
            e.RegisterCallback<PointerEnterEvent>(_ => On());
            e.RegisterCallback<PointerLeaveEvent>(_ => Off());
            e.RegisterCallback<PointerDownEvent>(_ => { On(); if (e.enabledInHierarchy) e.style.scale = new Scale(Vector3.one * 0.96f); }, TrickleDown.TrickleDown);
            e.RegisterCallback<PointerUpEvent>(_ => { if (over) e.style.scale = new Scale(Vector3.one * 1.04f); });
            e.RegisterCallback<PointerCancelEvent>(_ => Off());
        }

        void BuildShop()
        {
            shop = new VisualElement { pickingMode = PickingMode.Position }; Abs(shop, 0, 0, 0, 0); root.Add(shop);
            shop.style.backgroundColor = new Color(0.05f, 0.04f, 0.03f);
            shop.style.backgroundImage = Resources.Load<Texture2D>("UI/Frames/shop_bg"); shop.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            shop.style.display = DisplayStyle.None;

            // ---------- แผงรายการสินค้า ----------
            shopPanel = new VisualElement { pickingMode = PickingMode.Position }; GoldFrame(shopPanel, 3); shopPanel.style.position = Position.Absolute;
            shopPanel.style.paddingTop = 12; shopPanel.style.paddingBottom = 12; shopPanel.style.paddingLeft = 14; shopPanel.style.paddingRight = 14; shop.Add(shopPanel);
            var buy = L(shopPanel, "ซื้อของ", 30, ShopGold); buy.style.unityFontDefinition = fontTitle;
            var name = L(shopPanel, "ร้านลุงส้ม", 26, new Color(1f, 0.92f, 0.78f)); name.style.unityFontDefinition = fontTitle; name.style.marginTop = 2;
            var mrow = new VisualElement { pickingMode = PickingMode.Ignore }; mrow.style.flexDirection = FlexDirection.Row; mrow.style.justifyContent = Justify.SpaceBetween; mrow.style.marginTop = 8;
            mrow.style.borderBottomWidth = 1; mrow.style.borderBottomColor = new Color(ShopGold.r, ShopGold.g, ShopGold.b, 0.5f); mrow.style.paddingBottom = 6; shopPanel.Add(mrow);
            var fl = L(mrow, "เงินเก็บ", 22, new Color(0.95f, 0.9f, 0.8f)); fl.style.unityTextAlign = TextAnchor.MiddleLeft;
            shopMoney = L(mrow, "", 22, ShopGold);
            shopTabs = new VisualElement(); shopTabs.style.flexDirection = FlexDirection.Row; shopTabs.style.flexWrap = Wrap.Wrap; shopTabs.style.marginTop = 8; shopPanel.Add(shopTabs);
            foreach (var (cat, label) in new[] { (Shop.Rods, "เบ็ดและรอก"), (Shop.Bait, "เหยื่อ"), (Shop.Ghost, "กันผี"), (Shop.Home, "ของในบ้าน") })
            {
                string c = cat;
                var tb = new Button(() => { shopCat = c; shopSel = null; FillShop(); }) { text = label }; StyleButton(tb, Color.clear); GoldFrame(tb, 1);
                tb.style.fontSize = 17; tb.style.color = new Color(0.95f, 0.88f, 0.72f); Pad(tb, 5, 10); tb.style.marginRight = 5; tb.style.marginTop = 4; tb.name = c;
                Hoverable(tb, new Color(0.38f, 0.28f, 0.1f, 0.95f));
                shopTabs.Add(tb);
            }
            var sv = new ScrollView(ScrollViewMode.Vertical); sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden; sv.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            sv.style.flexGrow = 1; sv.style.flexShrink = 1; sv.style.marginTop = 8; shopPanel.Add(sv);
            shopList = new VisualElement(); sv.Add(shopList);

            // ---------- กล่องรายละเอียด + ปุ่มซื้อ ----------
            shopDetail = new VisualElement { pickingMode = PickingMode.Position }; GoldFrame(shopDetail, 2); shopDetail.style.position = Position.Absolute;
            shopDetail.style.paddingTop = 12; shopDetail.style.paddingBottom = 12; shopDetail.style.paddingLeft = 16; shopDetail.style.paddingRight = 16; shop.Add(shopDetail);
            dArt = new VisualElement { pickingMode = PickingMode.Ignore }; dArt.style.width = dArt.style.height = 130; dArt.style.alignSelf = Align.Center; dArt.style.marginBottom = 6;
            dArt.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain); shopDetail.Add(dArt);
            dName = L(shopDetail, "", 24, ShopGold); dName.style.unityTextAlign = TextAnchor.MiddleLeft; dName.style.unityFontDefinition = fontTitle;
            dDesc = L(shopDetail, "", 18, new Color(0.95f, 0.9f, 0.82f), false); dDesc.style.unityTextAlign = TextAnchor.MiddleLeft; dDesc.style.marginTop = 4;
            dPrice = L(shopDetail, "", 20, new Color(1f, 0.85f, 0.5f)); dPrice.style.unityTextAlign = TextAnchor.MiddleLeft; dPrice.style.marginTop = 6;
            dBuy = new Button(() => BuySelected()) { text = "ซื้อ" }; StyleButton(dBuy, new Color(0.18f, 0.42f, 0.2f)); dBuy.style.marginTop = 10; dBuy.style.fontSize = 22; Pad(dBuy, 8, 20);
            dBuy.style.borderTopWidth = dBuy.style.borderBottomWidth = dBuy.style.borderLeftWidth = dBuy.style.borderRightWidth = 1; dBuy.style.borderTopColor = dBuy.style.borderBottomColor = dBuy.style.borderLeftColor = dBuy.style.borderRightColor = ShopGold;
            Hoverable(dBuy, new Color(0.26f, 0.56f, 0.28f));
            shopDetail.Add(dBuy);

            // ---------- คำพูดลุงส้ม ----------
            var say = new VisualElement { pickingMode = PickingMode.Ignore }; GoldFrame(say, 2); say.style.position = Position.Absolute; say.name = "say";
            say.style.flexDirection = FlexDirection.Row; say.style.alignItems = Align.Center; say.style.paddingTop = say.style.paddingBottom = 8; say.style.paddingLeft = 8; say.style.paddingRight = 16; shop.Add(say);
            var face = new VisualElement { pickingMode = PickingMode.Ignore }; face.style.width = face.style.height = 86; face.style.flexShrink = 0; face.style.marginRight = 14;
            face.style.backgroundImage = Resources.Load<Texture2D>("UI/Portraits/lungsom"); face.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            face.style.borderTopWidth = face.style.borderBottomWidth = face.style.borderLeftWidth = face.style.borderRightWidth = 1; face.style.borderTopColor = face.style.borderBottomColor = face.style.borderLeftColor = face.style.borderRightColor = ShopGold;
            say.Add(face);
            shopSay = L(say, "", 20, new Color(0.96f, 0.92f, 0.84f), false); shopSay.style.flexShrink = 1; shopSay.style.unityTextAlign = TextAnchor.MiddleLeft;

            var back = new Button(() => { shop.style.display = DisplayStyle.None; GameInput.Blocked = ModalOpen || CardOpen; RefreshMorning(); }) { text = "กลับ" };
            StyleButton(back, Color.clear); GoldFrame(back, 2); back.style.position = Position.Absolute; back.style.left = 16; back.style.bottom = 14; back.style.color = new Color(1f, 0.9f, 0.75f); back.style.fontSize = 22; Pad(back, 6, 18);
            back.name = "back"; Hoverable(back, new Color(0.3f, 0.22f, 0.08f, 0.9f)); shop.Add(back);
            shop.RegisterCallback<GeometryChangedEvent>(_ => LayoutShop());
        }

        // จอกว้าง: แผงซ้าย กล่องรายละเอียดข้าง ๆ คำพูดล่าง · จอแคบ: แผงบนเต็มกว้าง รายละเอียดใต้แผง คำพูดล่างสุด
        void LayoutShop()
        {
            if (shop == null) return;
            float w = shop.layout.width, h = shop.layout.height; if (float.IsNaN(w) || w < 1) return;
            bool n = Narrow || w < h;
            var say = shop.Q("say");
            var ps = shopPanel.style; var ds = shopDetail.style; var ss = say.style;
            shop.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, Length.Percent(n ? 68 : 50));   // จอตั้ง: เลื่อนภาพให้เห็นลุงส้มหลังเคาน์เตอร์
            if (!n)
            {
                ps.left = w * 0.04f; ps.top = h * 0.05f; ps.width = Mathf.Min(460, w * 0.36f); ps.height = h * 0.7f; ps.right = StyleKeyword.Auto; ps.bottom = StyleKeyword.Auto;
                ds.left = w * 0.04f + Mathf.Min(460, w * 0.36f) + 18; ds.top = h * 0.36f; ds.width = Mathf.Min(360, w * 0.28f); ds.right = StyleKeyword.Auto; ds.bottom = StyleKeyword.Auto;
                ss.left = w * 0.25f; ss.right = w * 0.2f; ss.bottom = 16; ss.top = StyleKeyword.Auto;
            }
            else
            {
                ps.left = 12; ps.right = 12; ps.top = 12; ps.width = StyleKeyword.Auto; ps.height = h * 0.46f; ps.bottom = StyleKeyword.Auto;
                ds.left = 12; ds.right = 12; ds.top = h * 0.46f + 20; ds.width = StyleKeyword.Auto; ds.bottom = StyleKeyword.Auto;
                ss.left = 12; ss.right = 12; ss.bottom = 70; ss.top = StyleKeyword.Auto;
            }
        }

        public void ShowShop()
        {
            if (shop == null) BuildShop();
            FillShop(); shop.style.display = DisplayStyle.Flex; shop.BringToFront(); GameInput.Blocked = true; LayoutShop();
        }

        VisualElement dArt;
        // รูปสีของสินค้า (Resources/UI/Shop/<id>) · ไม่มีรูปใช้ไอคอนเส้นแทน
        static Texture2D ShopArt(ShopItem it) => Resources.Load<Texture2D>("UI/Shop/" + it.id) ?? Resources.Load<Texture2D>("UI/Icons/" + ShopIcon(it));
        static string ShopIcon(ShopItem it) => it.id switch
        {
            _ when it.id.StartsWith("rod") => "cast", _ when it.id.StartsWith("reel") => "reel",
            "lamp" => "fire", "torch" => "flashlight", "amulet" => "seal", "merit" => "incense",
            _ => it.cat == Shop.Bait ? "hook" : "home",
        };

        static string ShopLine(string cat, ShopItem sel)
        {
            if (sel != null && sel.id == "amulet") return "จะไปน้ำลึกตอนกลางคืนรึ? เอาสายสิญจน์ติดตัวไป ผีจะไม่กล้าเข้าใกล้";
            if (sel != null && sel.id == "merit") return "ทำบุญกรวดน้ำให้เขาเสียหน่อย คืนนี้จะได้ตกปลาสบายใจ";
            return cat switch
            {
                Shop.Rods => "คันดี ๆ เหวี่ยงไกล ปลาใหญ่ก็ไม่หลุดมือนะหลาน",
                Shop.Bait => "กุ้งเป็นเพิ่งช้อนมาเมื่อเช้า ปลาหายากชอบนักแล",
                Shop.Ghost => "ตกปลากลางคืนต้องมีของกันตัวไว้ ลุงเห็นมาเยอะแล้ว",
                _ => "ซื้อของเข้าบ้านบ้าง ลูกเมียจะได้ยิ้มออก",
            };
        }

        void FillShop()
        {
            var s = Story.S;
            shopMoney.text = Story.Baht(s.money);
            foreach (var tb in shopTabs.Children()) { tb.style.backgroundColor = tb.name == shopCat ? new Color(0.45f, 0.33f, 0.12f, 0.95f) : PanelBg; }
            shopList.Clear();
            if (shopSel == null || shopSel.cat != shopCat) shopSel = System.Array.Find(Shop.Items, i => i.cat == shopCat);
            if (shopCat == Shop.Bait)
            {
                var cur = L(shopList, $"เหยื่อที่ใช้อยู่: {Shop.BaitName(Shop.ActiveBait)}", 18, new Color(0.9f, 0.85f, 0.7f), false); cur.style.unityTextAlign = TextAnchor.MiddleLeft; cur.style.marginBottom = 4;
            }
            foreach (var it in Shop.Items)
            {
                if (it.cat != shopCat) continue;
                var item = it;
                var row = new Button(() => { shopSel = item; FillShop(); }) { text = "" }; StyleButton(row, Color.clear); GoldFrame(row, item == shopSel ? 2 : 1);
                if (item == shopSel) { row.style.borderTopColor = row.style.borderBottomColor = row.style.borderLeftColor = row.style.borderRightColor = new Color(1f, 0.85f, 0.4f); row.style.backgroundColor = new Color(0.25f, 0.18f, 0.06f, 0.95f); }
                row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 5; Pad(row, 7, 10);
                var ic = new VisualElement { pickingMode = PickingMode.Ignore }; ic.style.width = ic.style.height = 46; ic.style.marginRight = 10; ic.style.flexShrink = 0;
                ic.style.backgroundImage = ShopArt(item); ic.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain); row.Add(ic);
                var nm = L(row, item.name, 20, new Color(0.97f, 0.92f, 0.82f), false); nm.style.flexGrow = 1; nm.style.flexShrink = 1; nm.style.unityTextAlign = TextAnchor.MiddleLeft;
                bool owned = !Shop.CanBuy(item, out var why) && (why == "มีแล้ว" || why == "ทำบุญวันนี้แล้ว");
                var pr = L(row, owned ? "มีแล้ว" : Story.Baht(item.price), 19, owned ? new Color(0.6f, 0.9f, 0.6f) : ShopGold); pr.style.flexShrink = 0; pr.style.marginLeft = 8;
                Hoverable(row, new Color(0.3f, 0.22f, 0.08f, 0.95f));
                shopList.Add(row);
            }
            // กล่องรายละเอียดของที่เลือก
            var sel = shopSel;
            shopDetail.style.display = sel != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (sel != null)
            {
                dName.text = sel.name; dArt.style.backgroundImage = ShopArt(sel); dArt.style.width = dArt.style.height = Narrow || shop.layout.width < shop.layout.height ? 84 : 130;
                string state = sel.cat == Shop.Bait ? $" · มี {Shop.Owned(sel)}" : sel.id == "amulet" ? $" · มี {s.amulets}/5" : "";
                dDesc.text = sel.desc + state;
                bool ok = Shop.CanBuy(sel, out var why);
                dPrice.text = ok ? $"ราคา {Story.Baht(sel.price)}" : $"ราคา {Story.Baht(sel.price)} · {why}";
                dBuy.text = sel.cat == Shop.Bait && Shop.Owned(sel) > 0 && Shop.ActiveBait != sel.id ? "ซื้อเพิ่ม" : "ซื้อ"; dBuy.SetEnabled(ok);
                dBuy.style.opacity = ok ? 1 : 0.45f;
                var useB = shopDetail.Q<Button>("use");
                if (useB != null) useB.RemoveFromHierarchy();
                if (sel.cat == Shop.Bait && Shop.Owned(sel) > 0 && Shop.ActiveBait != sel.id)
                {
                    var u = new Button(() => { Shop.SelectBait(sel.id); FillShop(); }) { text = "ใช้เหยื่อนี้" }; u.name = "use"; StyleButton(u, new Color(0.35f, 0.28f, 0.14f)); u.style.marginTop = 6; u.style.fontSize = 18; Pad(u, 6, 16);
                    Hoverable(u, new Color(0.48f, 0.38f, 0.18f));
                    shopDetail.Add(u);
                }
            }
            shopSay.text = $"ลุงส้ม: {ShopLine(shopCat, sel)}";
        }

        public bool ShopOpen => shop != null && shop.style.display == DisplayStyle.Flex;

        // ถ่ายคลิป (shopdemo): เปิดร้าน ไล่เลือกของทีละหมวด กดซื้อให้ดู แล้วออกไปตกปลาต่อ
        System.Collections.IEnumerator ShopDemo(System.Action done)
        {
            Story.S.money = Mathf.Max(Story.S.money, 9000);
            ShowShop(); yield return new WaitForSeconds(1.8f);
            foreach (var (cat, id) in new[] { (Shop.Rods, "rod1"), (Shop.Rods, "reel1"), (Shop.Bait, "shrimp"), (Shop.Ghost, "lamp"), (Shop.Home, "radio") })
            {
                shopCat = cat; shopSel = Shop.Get(id); FillShop(); yield return new WaitForSeconds(1.4f);
                BuySelected(); yield return new WaitForSeconds(1.1f);
            }
            yield return new WaitForSeconds(0.8f);
            shop.style.display = DisplayStyle.None; GameInput.Blocked = ModalOpen || CardOpen; RefreshMorning();
            done?.Invoke();
        }

        void BuySelected()
        {
            var it = shopSel; if (it == null || !Shop.Buy(it)) return;
            Sfx.Coin();
            Toast(it.id == "merit" ? "ถวายสังฆทาน กรวดน้ำให้ดวงวิญญาณ… คืนนี้ใจสงบขึ้น" : $"ซื้อ{it.name}แล้ว!");
            FillShop();
        }
    }
}
