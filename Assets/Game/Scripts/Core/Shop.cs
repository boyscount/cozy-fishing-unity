using System;
using System.Collections.Generic;

namespace CozyFishing
{
    // ร้านโชห่วยในหมู่บ้าน: ที่ใช้เงินหลังทำเป้าหมายครอบครัวครบ
    //   เบ็ดและรอก (อัปเกรดถาวร) · เหยื่อ (ซื้อเป็นห่อ ใช้ทีละตัวเมื่อได้ปลา) · ของกันผี · ของในบ้าน (ครอบครัวสุขขึ้นทุกเช้า)
    public class ShopItem
    {
        public string id, cat, name, desc;
        public int price, level;          // level: อัปเกรดขั้นที่เท่าไร (คัน/รอก) · 0 = ไม่ใช่ขั้น
        public int pack;                  // เหยื่อ: ได้กี่ตัวต่อห่อ · สายสิญจน์: 1
        public string slot;               // ทุ่น สาย เบ็ด (Tackle.cs): ช่องที่ใส่
        public int day = 1;               // ปลดล็อกในร้านตั้งแต่วันที่เท่าไรของเนื้อเรื่อง
    }

    public static class Shop
    {
        public const string Rods = "rod", Bait = "bait", Ghost = "ghost", Home = "home", TackleCat = "tackle";

        public static readonly ShopItem[] Items =
        {
            new ShopItem { id = "rod1", cat = Rods, level = 1, price = 600, name = "คันเบ็ดไฟเบอร์", desc = "เหวี่ยงไกล 22 ม. · สายตึงช้าลงนิดหน่อย" },
            new ShopItem { id = "rod2", cat = Rods, level = 2, price = 2200, name = "คันคาร์บอนเบา", desc = "เหวี่ยงไกล 26 ม. · สู้ปลาใหญ่ได้ดีขึ้น" },
            new ShopItem { id = "rod3", cat = Rods, level = 3, price = 6000, name = "คันเบ็ดครูช่างฝีมือ", desc = "เหวี่ยงไกล 30 ม. · สายตึงช้าที่สุด" },
            new ShopItem { id = "reel1", cat = Rods, level = 1, price = 500, name = "รอกสปินนิ่ง", desc = "กรอสายเร็วขึ้น 15%" },
            new ShopItem { id = "reel2", cat = Rods, level = 2, price = 1800, name = "รอกเบทคาสติ้ง", desc = "กรอสายเร็วขึ้น 30%" },
            new ShopItem { id = "reel3", cat = Rods, level = 3, price = 5000, name = "รอกทองเหลืองโบราณ", desc = "กรอสายเร็วขึ้น 45%" },
            new ShopItem { id = "shrimp", cat = Bait, pack = 10, price = 40, name = "กุ้งเป็น (10 ตัว)", desc = "ปลาหายากและปลาดาวสูงชอบ โอกาสเจอสูงขึ้น" },
            new ShopItem { id = "bran", cat = Bait, pack = 10, price = 20, name = "รำข้าวปั้น (10 ก้อน)", desc = "ปลากินเหยื่อเร็วขึ้นราวหนึ่งในสาม" },
            new ShopItem { id = "frog", cat = Bait, pack = 10, price = 60, name = "กบน้อย (10 ตัว)", desc = "ล่อปลาตัวใหญ่ น้ำหนักเฉลี่ยมากขึ้น" },
            // ทุ่น สาย เบ็ด (ใส่ได้อย่างละชิ้น · ชิ้นแรกของแต่ละช่องมีติดตัวตั้งแต่ต้น)
            new ShopItem { id = "float_feather", cat = TackleCat, slot = Tackle.Float, price = 0, name = "ทุ่นขนไก่ของพ่อ", desc = "ทุ่นไม้ไผ่เสียบขนไก่ ใช้ได้ทุกที่ ไม่เด่นอะไรเป็นพิเศษ" },
            new ShopItem { id = "float_quill", cat = TackleCat, slot = Tackle.Float, day = 2, price = 250, name = "ทุ่นปากกาขนห่าน", desc = "ไวมาก ปลาแค่ตอดก็จุ่มให้เห็น · เวลาวัดเบ็ดนานขึ้น 25% · น้ำเชี่ยวโดนกระแสลาก" },
            new ShopItem { id = "float_foam", cat = TackleCat, slot = Tackle.Float, day = 3, price = 300, name = "ทุ่นโฟมลูกกลม", desc = "นิ่งกลางน้ำเชี่ยว · ด่านแก่งและแม่น้ำโขง เวลาวัดเบ็ดนานขึ้น 30%" },
            new ShopItem { id = "float_glow", cat = TackleCat, slot = Tackle.Float, day = 5, price = 450, name = "ทุ่นไฟเรืองแสง", desc = "ปลายทุ่นเรืองแสง เห็นชัดตอนมืด · กลางคืนเวลาวัดเบ็ดนานขึ้น 35%" },
            new ShopItem { id = "line_mono", cat = TackleCat, slot = Tackle.Line, price = 0, name = "สายเอ็นธรรมดา", desc = "สายเอ็นใส ๆ ทั่วไป ใช้ได้ทุกที่" },
            new ShopItem { id = "line_fluoro", cat = TackleCat, slot = Tackle.Line, day = 6, price = 500, name = "สายฟลูออโรใส", desc = "แทบมองไม่เห็นในน้ำ · ด่านน้ำใส ปลากินเร็วขึ้น 25%" },
            new ShopItem { id = "line_pe", cat = TackleCat, slot = Tackle.Line, day = 7, price = 900, name = "สายถัก PE", desc = "เหนียวมาก สายตึงช้าลง 18% สู้ปลาใหญ่สบาย · หนา ปลาน้ำใสระแวง" },
            new ShopItem { id = "hook_std", cat = TackleCat, slot = Tackle.Hook, price = 0, name = "เบ็ดเบอร์ 8", desc = "ตัวเบ็ดขนาดกลาง ใช้ได้กับปลาทุกขนาด" },
            new ShopItem { id = "hook_sharp", cat = TackleCat, slot = Tackle.Hook, day = 4, price = 350, name = "เบ็ดคมกริบ", desc = "ตีเบ็ดปลาหายากง่ายขึ้น · โซนทองกว้างขึ้น 35%" },
            new ShopItem { id = "hook_big", cat = TackleCat, slot = Tackle.Hook, day = 8, price = 600, name = "เบ็ดเบอร์ใหญ่", desc = "ปลาที่ติดตัวโตขึ้น · ปลาเล็กกินไม่เข้า รอนานขึ้น 15%" },
            new ShopItem { id = "lure_none", cat = TackleCat, slot = Tackle.Lure, price = 0, name = "ไม่ใช้เหยื่อปลอม", desc = "ใช้เหยื่อสดกับทุ่นตามปกติ (ไส้เดือน กุ้ง รำข้าว กบ)" },
            new ShopItem { id = "lure_frog", cat = TackleCat, slot = Tackle.Lure, day = 3, price = 350, name = "กบยาง", desc = "แตะ รอ แตะ ให้กบกระโดด · ปลานักล่าตัวใหญ่ชอบ · ใช้ได้ไม่หมด" },
            new ShopItem { id = "lure_spinner", cat = TackleCat, slot = Tackle.Lure, day = 5, price = 450, name = "สปินเนอร์ใบหมุน", desc = "กดค้างกรอสม่ำเสมอ · ปลาว่องไวชอบ · ใช้ได้ไม่หมด" },
            new ShopItem { id = "lure_spoon", cat = TackleCat, slot = Tackle.Lure, day = 7, price = 700, name = "ช้อนโลหะ", desc = "กรอเร็วแล้วปล่อยให้พลิ้ว · ปลาดาวสูงเข้าบ่อยขึ้นมาก · ใช้ได้ไม่หมด" },
            new ShopItem { id = "lure_swim", cat = TackleCat, slot = Tackle.Lure, day = 9, price = 1100, name = "ปลายางส่ายหาง", desc = "แตะถี่ ๆ สม่ำเสมอ · ปลาตัวโตชอบมาก · ใช้ได้ไม่หมด" },
            new ShopItem { id = CastNet.Id, cat = TackleCat, slot = Tackle.Net, day = 4, price = 500, name = "แหทอดถักมือ", desc = "ทอดได้ปลาเล็กทีละหลายตัว · ใช้ที่บ่อหลังบ้าน (ยืนท่าน้ำ) · วันละ 5 ครั้ง · สลับเบ็ด/แหด้วยปุ่มมุมขวาบน" },
            new ShopItem { id = "reel_drag", cat = TackleCat, slot = Tackle.Drag, day = 6, price = 800, name = "ชุดเบรกรอก", desc = "สายตึงจนเกือบขาด รอกคลายสายให้เอง ปลาได้ระยะคืนนิดหน่อยแต่สายไม่ขาด" },
            new ShopItem { id = "uwlight", cat = Rods, price = 700, name = "ไฟล่อใต้น้ำ", desc = "แท่งไฟเรืองแสงผูกเหนือเหยื่อ · มองเห็นเหยื่อและปลาใต้น้ำชัดขึ้นมาก · ปลากินเหยื่อเร็วขึ้นนิดหน่อย" },
            new ShopItem { id = "lamp", cat = Ghost, price = 800, name = "ตะเกียงน้ำมันอย่างดี", desc = "แสงสว่างกว้างขึ้น · สติลดช้าลง 15%" },
            new ShopItem { id = "torch", cat = Ghost, price = 600, name = "ไฟฉายถ่านก้อนใหญ่", desc = "ส่องไกลขึ้น · ถ่านอยู่ได้นานขึ้นเกือบเท่าตัว" },
            new ShopItem { id = "amulet", cat = Ghost, pack = 1, price = 150, name = "สายสิญจน์", desc = "ดึงสติกลับมาได้หนึ่งครั้งเมื่อสติหมด (มีได้ 5 เส้น)" },
            new ShopItem { id = "merit", cat = Ghost, price = 300, name = "ทำบุญที่วัด", desc = "คืนนี้ผีทุกด่านดุน้อยลงครึ่งหนึ่ง" },
            new ShopItem { id = "net", cat = Home, price = 400, name = "มุ้งผืนใหม่", desc = "ครอบครัวสุขขึ้นทุกเช้า" },
            new ShopItem { id = "fan", cat = Home, price = 900, name = "พัดลมตั้งพื้น", desc = "ครอบครัวสุขขึ้นทุกเช้า" },
            new ShopItem { id = "cupboard", cat = Home, price = 1200, name = "ตู้กับข้าวมุ้งลวด", desc = "ครอบครัวสุขขึ้นทุกเช้า" },
            new ShopItem { id = "radio", cat = Home, price = 1500, name = "วิทยุทรานซิสเตอร์", desc = "ครอบครัวสุขขึ้นทุกเช้า · ฟังข่าวพยากรณ์อากาศ" },
            new ShopItem { id = "shelf", cat = Home, price = 2500, name = "ตู้โชว์สมุดสะสมปลา", desc = "ครอบครัวสุขขึ้นทุกเช้า · ลูกภูมิใจในพ่อ" },
            // ของชิ้นใหญ่ช่วงท้ายเกม (รายได้ด่านไกลสูงขึ้นมาก ต้องมีเป้าหมายให้เก็บเงินต่อ)
            new ShopItem { id = "fridge", cat = Home, price = 6500, name = "ตู้เย็นประตูเดียว", desc = "ครอบครัวสุขขึ้นทุกเช้า · เก็บปลาไว้กินได้นาน" },
            new ShopItem { id = "paint", cat = Home, price = 8000, name = "ทาสีบ้านใหม่ทั้งหลัง", desc = "ครอบครัวสุขขึ้นทุกเช้า · บ้านดูอบอุ่นขึ้น" },
            new ShopItem { id = "tv", cat = Home, price = 9500, name = "โทรทัศน์ขาวดำ", desc = "ครอบครัวสุขขึ้นทุกเช้า · เพื่อนบ้านมาดูละครด้วย" },
            new ShopItem { id = "tutor", cat = Home, price = 15000, name = "ส่งลูกเรียนพิเศษในเมือง", desc = "ครอบครัวสุขขึ้นทุกเช้า · ลูกได้เรียนสูงอย่างที่ฝัน" },
            new ShopItem { id = "veranda", cat = Home, price = 24000, name = "ต่อเติมชานบ้านริมน้ำ", desc = "ครอบครัวสุขขึ้นทุกเช้า · นั่งดูพระอาทิตย์ตกด้วยกัน" },
        };

        static StoryState S => Story.S;
        public static ShopItem Get(string id) => Array.Find(Items, i => i.id == id);

        // ---------- สถานะของที่มี ----------
        public static int Owned(ShopItem it) => it.cat == TackleCat ? (Tackle.Has(it.id) ? 1 : 0) : it.id switch
        {
            "shrimp" => S.baitShrimp, "bran" => S.baitBran, "frog" => S.baitFrog, "amulet" => S.amulets,
            _ when it.id.StartsWith("rod") => S.rod >= it.level ? 1 : 0,
            _ when it.id.StartsWith("reel") => S.reel >= it.level ? 1 : 0,
            "merit" => S.meritDay == S.day ? 1 : 0,
            _ => S.items.Contains(it.id) ? 1 : 0,
        };

        /// ซื้อได้ไหม · reason = เหตุผลที่ซื้อไม่ได้ (null = ได้)
        public static bool CanBuy(ShopItem it, out string reason)
        {
            reason = null;
            if (it.cat == TackleCat) { reason = Tackle.Has(it.id) ? "มีแล้ว" : !Tackle.Unlocked(it) ? $"ลุงยังไม่มีของ (มีวันที่ {it.day})" : S.money < it.price ? "เงินไม่พอ" : null; return reason == null; }
            if (it.id.StartsWith("rod") && S.rod != it.level - 1) reason = S.rod >= it.level ? "มีแล้ว" : "ต้องมีคันขั้นก่อนหน้า";
            else if (it.id.StartsWith("reel") && S.reel != it.level - 1) reason = S.reel >= it.level ? "มีแล้ว" : "ต้องมีรอกขั้นก่อนหน้า";
            else if (it.id == "amulet" && S.amulets >= 5) reason = "มีครบ 5 เส้นแล้ว";
            else if (it.id == "merit" && S.meritDay == S.day) reason = "ทำบุญวันนี้แล้ว";
            else if (it.cat == Home || it.id == "lamp" || it.id == "torch" || it.id == "uwlight") { if (S.items.Contains(it.id)) reason = "มีแล้ว"; }
            if (reason == null && S.money < it.price) reason = "เงินไม่พอ";
            return reason == null;
        }

        public static bool Buy(ShopItem it)
        {
            if (!CanBuy(it, out _)) return false;
            S.money -= it.price;
            if (it.cat == TackleCat) { Tackle.Give(it.id); Story.Save(); Tackle.Equip(it.id); return true; }   // ซื้อแล้วใส่ให้เลย
            switch (it.id)
            {
                case "shrimp": S.baitShrimp += it.pack; if (S.bait == "worm") S.bait = "shrimp"; break;
                case "bran": S.baitBran += it.pack; if (S.bait == "worm") S.bait = "bran"; break;
                case "frog": S.baitFrog += it.pack; if (S.bait == "worm") S.bait = "frog"; break;
                case "amulet": S.amulets++; break;
                case "merit": S.meritDay = S.day; break;
                default:
                    if (it.id.StartsWith("rod")) S.rod = it.level;
                    else if (it.id.StartsWith("reel")) S.reel = it.level;
                    else S.items.Add(it.id);
                    if (it.cat == Home) S.happy = Math.Min(100, S.happy + 5);   // ซื้อของเข้าบ้าน: ทุกคนดีใจทันที
                    break;
            }
            Story.Save();
            return true;
        }

        // ---------- เหยื่อ ----------
        public static int BaitLeft(string b) => b == "shrimp" ? S.baitShrimp : b == "bran" ? S.baitBran : b == "frog" ? S.baitFrog : 999;
        public static string BaitName(string b) => b == "shrimp" ? "กุ้งเป็น" : b == "bran" ? "รำข้าวปั้น" : b == "frog" ? "กบน้อย" : "ไส้เดือน";
        public static string ActiveBait => Lures.Active ? "worm" : BaitLeft(S.bait) > 0 ? S.bait : "worm";   // ใส่เหยื่อปลอม: เหยื่อสดไม่มีผลและไม่ถูกใช้
        public static void SelectBait(string b) { S.bait = b; Story.Save(); }
        /// ใช้เหยื่อหนึ่งตัวเมื่อได้ปลา · คืนค่า true = เหยื่อหมดพอดี (สลับกลับเป็นไส้เดือน)
        public static bool UseBait()
        {
            switch (ActiveBait)
            {
                case "shrimp": S.baitShrimp--; break;
                case "bran": S.baitBran--; break;
                case "frog": S.baitFrog--; break;
                default: return false;
            }
            bool empty = BaitLeft(S.bait) <= 0;
            if (empty) S.bait = "worm";
            Story.Save();
            return empty;
        }

        // ---------- ผลของอุปกรณ์ ----------
        public static float CastDist => 18f + 4f * S.rod;                                        // 18 / 22 / 26 / 30 ม.
        public static float RodTension => 1f - 0.12f * S.rod;                                    // สายตึงช้าลงเมื่อคันดีขึ้น
        public static float ReelSpeed => 1f + 0.15f * S.reel;                                    // กรอสายได้ระยะมากขึ้น
        public static float BiteMul => (ActiveBait == "bran" ? 0.65f : 1f) * (HasUwLight ? 0.9f : 1f) * Tackle.BiteMul * Skills.BiteMul;   // รำข้าว: ปลากินเร็วขึ้น · ไฟล่อใต้น้ำ: เร็วขึ้นนิด
        public static bool HasUwLight => S.items.Contains("uwlight");
        public static float RareMul => (ActiveBait == "shrimp" ? 1.7f : 1f) * Skills.RareMul;                       // กุ้งเป็น: ปลาหายาก/ดาวสูง
        public static float SizeBoost => (ActiveBait == "frog" ? 0.22f : 0f) + Tackle.SizeBoost;  // กบ / เบ็ดใหญ่: ตัวใหญ่ขึ้น
        public static float SanityDrainMul => (S.items.Contains("lamp") ? 0.85f : 1f) * (S.meritDay == S.day ? 0.5f : 1f);
        public static float BatteryDrainMul => S.items.Contains("torch") ? 0.55f : 1f;
        public static float FlashRange => S.items.Contains("torch") ? 30f : 22f;
        public static float LanternRange => S.items.Contains("lamp") ? 11f : 7f;

        /// ของในบ้าน: ครอบครัวสุขขึ้นวันละ 1 ต่อชิ้น (เรียกตอนเช้า)
        public static void MorningHappiness()
        {
            int n = 0; foreach (var it in Items) if (it.cat == Home && S.items.Contains(it.id)) n++;
            if (n > 0) S.happy = Math.Min(100, S.happy + n);
        }
    }
}
