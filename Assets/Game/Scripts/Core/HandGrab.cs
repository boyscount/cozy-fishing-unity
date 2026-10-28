using System;
using System.Linq;

namespace CozyFishing
{
    // งมปลาด้วยมือเปล่า (โพรงริมตลิ่งบ่อหลังบ้าน): คลำก่อนเห็น เดาเอาจากสัมผัสว่าเป็นอะไร แล้วเลือก "จับ" หรือ "ชักมือ"
    //   ของในโพรง: ปลาดุกนา ปลาหมอ (ต้องบีบให้พอดี) · ปูนา (อาจโดนหนีบ) · หอยกาบ (ลุ้นไข่มุก) · กิ่งหนาม/เศษแก้ว (บาดมือ) · ของโบราณ (เบาะแสคดี/ตะกรุดกันผี) · โคลนเปล่า
    //   งมได้วันละ PerDay โพรง · แผลที่มือสะสม → ช่องเขียวตอนบีบแคบลง · แผลเต็ม = พักก่อน หายเองวันรุ่งขึ้น
    public static class HandGrab
    {
        public const int PerDay = 12, MaxWound = 3;   // งมได้วันละ 12 โพรง (เดิม 6)
        public enum Kind { Fish, Crab, Clam, Thorn, Glass, Relic, Empty }
        public class Find { public Kind kind; public FishSpecies sp; public float kg; public int cm; public string feel; }

        static StoryState S => Story.S;
        public static bool StageOk(string stage) => stage == "paddy";
        static void Day() { if (S.grabDay != S.day) { S.grabDay = S.day; S.grabsUsed = 0; S.handWound = 0; } }
        public static int Left { get { Day(); return Math.Max(0, PerDay - S.grabsUsed); } }
        public static int Wound { get { Day(); return S.handWound; } }
        public static bool TooHurt => Wound >= MaxWound;
        public static void Use() { Day(); S.grabsUsed++; Story.Save(); }
        public static void Hurt() { Day(); S.handWound = Math.Min(MaxWound, S.handWound + 1); Story.Save(); }

        /// ช่องเขียวตอนบีบปลา (แคบลงตามแผล)
        public static float GripLow => 0.42f + Wound * 0.03f;
        public static float GripHigh => 0.76f - Wound * 0.03f;

        // สัมผัสที่มือ: ของต่างชนิดบางคู่รู้สึกคล้ายกัน (หอย/เศษแก้ว · ปู/กิ่งหนาม) ต้องเดาเอา
        static readonly (Kind kind, string id, int w, string[] feel)[] Table =
        {
            (Kind.Fish, "duk_na", 22, new[] { "ลื่น ๆ มีหนวดยาวปัดนิ้ว… ข้างหัวมีเงี่ยงแข็ง", "ตัวลื่นยาว ดิ้นวูบ ๆ หนวดกระดิก" }),
            (Kind.Fish, "mo", 14, new[] { "เกล็ดแข็งสากมือ ตัวเล็กดิ้นแรง", "แข็ง ๆ สาก ๆ ขยับไปมาในโพรง" }),
            (Kind.Crab, "pu_na", 18, new[] { "แข็งเป็นกระดอง มีขาเล็ก ๆ ไต่ข้อนิ้ว", "แข็ง ๆ มีอะไรแหลม ๆ ขยับได้" }),
            (Kind.Clam, "hoi_kab", 16, new[] { "แข็งเรียบ ปิดสนิท ฝังอยู่ในโคลน", "เย็น ๆ ผิวเรียบ ขอบโค้ง" }),
            (Kind.Thorn, null, 10, new[] { "แข็งเป็นกิ่ง ๆ มีอะไรแหลม ๆ", "เป็นแท่ง ๆ ขรุขระ มีหนามทิ่มนิ้ว" }),
            (Kind.Glass, null, 8, new[] { "เย็น ๆ ผิวเรียบ ขอบบาง ๆ", "แข็งเรียบ ขอบคมนิดหน่อย" }),
            (Kind.Relic, null, 4, new[] { "ก้อนแข็งเล็ก ๆ ผิวมีลวดลาย เหมือนดินเผา", "ของแข็งเล็ก ๆ มีรูร้อยเชือก" }),
            (Kind.Empty, null, 8, new[] { "มีแต่โคลนนิ่ม ๆ เย็นเฉียบ", "โพรงว่าง… มีแต่รากไม้เส้นเล็ก ๆ" }),
        };

        public static Find Roll(Random rng)
        {
            int sum = Table.Sum(t => t.w), r = rng.Next(sum);
            var row = Table[Table.Length - 1];
            foreach (var t in Table) { r -= t.w; if (r < 0) { row = t; break; } }
            var f = new Find { kind = row.kind, feel = row.feel[rng.Next(row.feel.Length)] };
            if (row.id != null && (f.sp = FishDatabase.Get(row.id)) != null)
            {
                double k = Math.Pow(rng.NextDouble(), 1.3);
                f.kg = (float)Math.Round(f.sp.kgMin + (f.sp.kgMax - f.sp.kgMin) * k, 2);
                f.cm = (int)Math.Round(f.sp.cmMin + (f.sp.cmMax - f.sp.cmMin) * k);
            }
            else if (row.id != null) f.kind = Kind.Empty;   // ไม่มีข้อมูลชนิดนี้ (ไม่ควรเกิด)
            return f;
        }

        public const float PearlChance = 0.1f; public const int PearlBaht = 250;
    }
}
