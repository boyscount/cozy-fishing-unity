using System;
using System.Collections.Generic;

namespace CozyFishing
{
    // อุปกรณ์ปลายสาย (ร้านลุงส้ม แท็บ "ทุ่น สาย เบ็ด"): ทุ่น · สาย · ตัวเบ็ด ใส่ได้อย่างละชิ้น + เบรกรอก (มีแล้วใช้ตลอด)
    //   ทุกชิ้นมีผลจริงตอนตกปลา และเห็นในน้ำ (ทุ่นเปลี่ยนรูปทรง สายหนา/บาง)
    //   ปลดล็อกทีละชิ้นตามวันในเนื้อเรื่อง · เข้าร้านครั้งแรกหลังปลดล็อก ลุงส้มสอนวิธีใช้ (ทุ่นปากกาแถมให้ลองฟรี)
    public class TackleTeach { public string id; public string[] lines; public bool gift; }

    public static class Tackle
    {
        public const string Float = "float", Line = "line", Hook = "hook", Drag = "drag", Lure = "lure", Net = "net";   // Net = แหทอด (CastNet.cs) มีแล้วสลับใช้ด้วยปุ่มบนจอ
        public const string DefFloat = "float_feather", DefLine = "line_mono", DefHook = "hook_std";

        // ด่านน้ำไหลแรง: ทุ่นเบาโดนกระแสลาก สัญญาณปลากินเห็นสั้นลง · ด่านน้ำใส: ปลาเห็นสายชัด ระแวง
        static readonly string[] FastWater = { "rapids", "mekong" };
        static readonly string[] ClearWater = { "reservoir", "squid" };

        static StoryState S => Story.S;
        static string Stage => S.stage ?? "";
        static bool Fast => Array.IndexOf(FastWater, Stage) >= 0;
        static bool Clear => Array.IndexOf(ClearWater, Stage) >= 0;
        static bool Night => S.time >= GhostSystem.NightFrom || S.time < 5f;

        public static event Action Changed;

        // ---------- ที่มี / ที่ใช้อยู่ ----------
        public static bool IsDefault(string id) => id == DefFloat || id == DefLine || id == DefHook || id == Lures.None;
        public static bool Has(string id) => IsDefault(id) || (S.tackle != null && S.tackle.Contains(id));
        public static string Equipped(string slot) => slot switch
        {
            Float => Has(S.tFloat) ? S.tFloat : DefFloat,
            Line => Has(S.tLine) ? S.tLine : DefLine,
            Lure => Has(S.tLure) ? S.tLure : Lures.None,
            _ => Has(S.tHook) ? S.tHook : DefHook,
        };
        public static bool IsEquipped(ShopItem it) => it.slot == Drag || it.slot == Net ? Has(it.id) : Equipped(it.slot) == it.id;
        public static void Equip(string id)
        {
            var it = Shop.Get(id); if (it == null || !Has(id)) return;
            if (it.slot == Float) S.tFloat = id; else if (it.slot == Line) S.tLine = id; else if (it.slot == Hook) S.tHook = id; else if (it.slot == Lure) S.tLure = id;
            Story.Save(); Changed?.Invoke();
        }
        public static void Give(string id)
        {
            if (S.tackle == null) S.tackle = new List<string>();
            if (!S.tackle.Contains(id)) S.tackle.Add(id);
        }
        public static bool HasDrag => Has("reel_drag");

        // ---------- ผลต่อการตกปลา ----------
        /// เวลาที่ทุ่นจมให้เห็นก่อนปลาคายเหยื่อ (คูณกับค่าของพันธุ์ปลา)
        public static float BiteWindowMul
        {
            get
            {
                var f = Equipped(Float);
                float k = f switch { "float_quill" => 1.25f, "float_glow" => Night ? 1.35f : 1f, _ => 1f };
                if (Fast) k *= f == "float_foam" ? 1.3f : 0.8f;   // น้ำเชี่ยว: ทุ่นโฟมนิ่ง ทุ่นอื่นโดนกระแสลาก
                return k;
            }
        }
        /// ทุ่นไวจมลึกตอนปลาตอดเหยื่อ (เห็นสัญญาณก่อนกินจริง) · ทุ่นโฟมจมตื้น
        public static float NibbleDepth => Equipped(Float) switch { "float_quill" => 1.9f, "float_foam" => 0.7f, "float_glow" => 1.2f, _ => 1f };
        public static int ExtraNibbles => Equipped(Float) == "float_quill" ? 1 : 0;
        /// ตัวคูณเวลารอปลากิน (น้อย = กินเร็ว)
        public static float BiteMul
        {
            get
            {
                float k = Equipped(Line) switch { "line_fluoro" => Clear ? 0.75f : 0.92f, "line_pe" => Clear ? 1.12f : 1f, _ => 1f };   // สายถักหนา ปลาน้ำใสเห็น
                if (Equipped(Hook) == "hook_big") k *= 1.15f;   // เบ็ดใหญ่ ปลาเล็กกินไม่เข้า
                return k;
            }
        }
        public static float SizeBoost => Equipped(Hook) == "hook_big" ? 0.12f : 0f;
        /// โซนทองตอนตีเบ็ดปลาหายาก (เบ็ดคมติดง่าย)
        public static float StrikeWidthMul => Equipped(Hook) == "hook_sharp" ? 1.35f : 1f;
        /// สายตึงช้าลง (สาย PE รับแรงได้มากกว่า)
        public static float LineTensionMul => Equipped(Line) switch { "line_pe" => 0.82f, "line_fluoro" => 0.96f, _ => 1f };
        /// ความหนาสายที่เห็น
        public static float LineWidth => Equipped(Line) switch { "line_pe" => 1.7f, "line_fluoro" => 0.6f, _ => 1f };

        // ---------- ปลดล็อกตามเนื้อเรื่อง + ลุงส้มสอน ----------
        public static bool Unlocked(ShopItem it) => S.day >= it.day;

        static readonly TackleTeach[] Teach =
        {
            new TackleTeach { id = "float_quill", gift = true, lines = new[] {
                "หลาน ๆ มานี่ก่อน ลุงมีของมาให้ลอง — ทุ่นปากกาขนห่าน เบาหวิว",
                "ปลาแค่ตอดเหยื่อ ทุ่นนี้ก็จุ่มให้เห็นแล้ว พอเห็นทุ่นกระตุกถี่ ๆ ให้เตรียมนิ้วไว้ อีกอึดใจปลากินจริง",
                "แถมให้อันหนึ่ง ลุงใส่ให้แล้ว ไปลองดูนะ" } },
            new TackleTeach { id = "float_foam", lines = new[] {
                "จะไปแก่งน้ำเชี่ยวรึ? ทุ่นเบา ๆ โดนน้ำลากจนดูไม่ออกว่าปลากินหรือยัง",
                "ทุ่นโฟมลูกนี้นิ่งกลางกระแส เห็นจมชัดกว่า ไปแก่งหรือแม่น้ำโขงต้องมีติดตัว" } },
            new TackleTeach { id = "hook_sharp", lines = new[] {
                "ปลาใหญ่ปากแข็ง ตีเบ็ดพลาดทีเดียวมันสะบัดหลุด",
                "เบ็ดคมกริบนี่ ตีโดนง่ายกว่าเดิมเยอะ ใช้กับปลาหายากที่ต้องตีให้ตรงจังหวะ" } },
            new TackleTeach { id = "float_glow", lines = new[] {
                "ตกปลากลางคืนมองทุ่นไม่ค่อยเห็นใช่ไหมล่ะ",
                "ทุ่นไฟเรืองแสงนี่ มืดแค่ไหนก็เห็นจม มีเวลาวัดเบ็ดนานขึ้นตอนกลางคืน" } },
            new TackleTeach { id = "line_fluoro", lines = new[] {
                "อ่างเก็บน้ำน้ำใสแจ๋ว ปลามันเห็นสายเอ็นแล้วไม่กล้ากิน",
                "สายฟลูออโรใสเกือบมองไม่เห็นในน้ำ ปลาน้ำใสกินเร็วขึ้นมาก" } },
            new TackleTeach { id = "reel_drag", lines = new[] {
                "ลุงเห็นหลานสายขาดบ่อย มานี่ ลุงสอนเรื่องเบรกรอก",
                "ใส่ชุดเบรกนี้แล้ว พอสายตึงจนจะขาด รอกจะคลายสายให้เอง ปลาได้ระยะคืนไปหน่อย แต่สายไม่ขาด",
                "จำไว้ ปลาดึงแรงก็ผ่อนให้มัน อย่าฝืน" } },
            new TackleTeach { id = "lure_frog", gift = true, lines = new[] {
                "หลาน ลุงจะสอนของใหม่ เหยื่อปลอม! กบยางตัวนี้ไม่ต้องรอทุ่นจม หลานต้องทำให้มันดูเหมือนกบจริง",
                "เหวี่ยงไปแล้ว แตะทีหนึ่งกบจะกระโดด รอสักอึดใจค่อยแตะอีกที ปลาช่อนปลาชะโดชอบนัก",
                "ดูแถบ ปลาสนใจ บนจอ ลากถูกจังหวะแถบจะขึ้น เต็มเมื่อไหร่ปลาฮุบ ค่อยวัดเบ็ด",
                "ตัวนี้ลุงให้ ใส่ไว้แล้ว อยากกลับไปใช้ไส้เดือนก็เลือก ไม่ใช้เหยื่อปลอม ในร้านได้" } },
            new TackleTeach { id = CastNet.Id, gift = true, lines = new[] {
                "หลาน มาดูนี่ แหทอดของลุงเอง ถักมากับมือ ใช้ตอนปลาเล็กมันรวมฝูงกันเยอะ ๆ",
                "อยู่ที่บ่อหลังบ้าน กดปุ่มแหมุมขวาบนเพื่อถือแห ดูน้ำไว้ ตรงไหนปลาผุดยุบยิบ นั่นแหละฝูงมัน",
                "กดค้างแกว่งแห ปล่อยตอนเข็มเข้าช่องเขียว แหจะกางเต็มวง แล้วสาวแหเข้ามาทีละจังหวะ อย่ารีบ ปลาจะลอดหนี",
                "แหได้ปลาเล็กทีละหลายตัว แต่ทอดได้วันละห้าครั้งพอ ปลาตัวโต ๆ ยังต้องใช้เบ็ดนะหลาน ผืนนี้ลุงให้",
                "แพไม้ไผ่ข้างท่าน้ำก็ยืมไปใช้ได้ ขึ้นแพแล้วถ่อไปดักฝูงปลากลางบ่อ ทอดแหจากแพได้ เบ็ดก็ได้" } },
            new TackleTeach { id = "lure_spinner", lines = new[] {
                "สปินเนอร์ใบหมุน กดค้างกรอไปเรื่อย ๆ อย่าหยุด ใบมันจะวาวล่อปลาว่องไว",
                "หยุดกรอเมื่อไหร่ ปลาก็หมดความสนใจ" } },
            new TackleTeach { id = "lure_spoon", lines = new[] {
                "ช้อนโลหะนี่ต้องกรอเร็วสักพักแล้วปล่อยให้มันพลิ้วจม ปลาดาวสูงชอบนัก",
                "กรอสั้นไปหรือนานไปมันไม่พลิ้ว จับจังหวะให้ดีนะหลาน" } },
            new TackleTeach { id = "lure_swim", lines = new[] {
                "ปลายางตัวนี้แตะถี่ ๆ สม่ำเสมอ หางมันจะส่ายเหมือนปลาจริง",
                "ปลาตัวโต ๆ ชอบไล่กินปลาเล็ก ใช้ตอนอยากได้ปลาหนัก" } },
            new TackleTeach { id = "line_pe", lines = new[] {
                "อยากสู้ปลาตัวโตต้องสายถัก PE เส้นเล็กแต่เหนียวกว่าเอ็นหลายเท่า",
                "แต่มันหนาเห็นชัด ปลาน้ำใสจะระแวงหน่อย เลือกใช้ให้ถูกที่นะหลาน" } },
            new TackleTeach { id = "hook_big", lines = new[] {
                "เบ็ดเบอร์ใหญ่ ปลาเล็กกินไม่เข้า รอนานขึ้นหน่อย",
                "แต่ตัวที่ติดมาส่วนใหญ่ตัวโต เหมาะกับวันที่อยากได้ปลาหนัก ๆ ไปขาย" } },
        };

        /// ของที่ปลดล็อกแล้วแต่ลุงยังไม่ได้สอน (เรียงตามวันที่ปลดล็อก)
        public static List<TackleTeach> PendingTeach()
        {
            var list = new List<TackleTeach>();
            if (S.taught == null) S.taught = new List<string>();
            foreach (var t in Teach)
            {
                var it = Shop.Get(t.id);
                if (it != null && Unlocked(it) && !S.taught.Contains(t.id)) list.Add(t);
            }
            return list;
        }
        public static void MarkTaught(TackleTeach t)
        {
            if (!S.taught.Contains(t.id)) S.taught.Add(t.id);
            if (t.gift) { Give(t.id); Equip(t.id); }
            Story.Save();
        }

        /// ข้อความสั้นใต้ชื่อสินค้า: บอกว่าเหมาะกับที่ไหน
        public static string Summary => $"ทุ่น: {Shop.Get(Equipped(Float)).name} · สาย: {Shop.Get(Equipped(Line)).name} · เบ็ด: {Shop.Get(Equipped(Hook)).name}" + (HasDrag ? " · มีเบรกรอก" : "") + (Equipped(Lure) != Lures.None ? $" · เหยื่อปลอม: {Shop.Get(Equipped(Lure)).name}" : "");
    }
}
