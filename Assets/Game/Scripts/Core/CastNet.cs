using System;
using System.Collections.Generic;
using System.Linq;

namespace CozyFishing
{
    // แหทอด (ลุงส้มสอน): อีกวิธีจับปลานอกจากเบ็ด · ได้ปลาเล็กทีละหลายตัว ราคาต่อกิโลเท่าเดิม แต่ตัวเล็ก ได้เงินพอ ๆ กับเบ็ดหนึ่งตัว
    //   ทอดได้วันละ ThrowsPerDay ครั้ง (แหเปียกหนัก) · ใช้ได้เฉพาะด่านที่มีท่าน้ำ/แพ (เริ่มที่บ่อหลังบ้าน)
    //   ปลาที่ติดแห = ปลาดาวเดียวของด่าน (ปลาหายาก/ตัวโตยังต้องใช้เบ็ด) · ใส่ข้องรวมเป็นกองละชนิด (ไม่กินช่องข้องทีละตัว)
    public static class CastNet
    {
        public const string Id = "net_cast";
        public const int ThrowsPerDay = 5;
        static readonly string[] Stages = { "paddy" };

        static StoryState S => Story.S;
        public static bool Owned => Tackle.Has(Id);
        public static bool StageOk(string stage) => Array.IndexOf(Stages, stage) >= 0;
        /// ถือแหอยู่ (กดปุ่มสลับ เบ็ด/แห)
        public static bool Using => S.useNet && Owned && StageOk(S.stage ?? "");
        public static void Toggle() { S.useNet = !S.useNet; Story.Save(); }

        public static int ThrowsLeft { get { if (S.netDay != S.day) return ThrowsPerDay; return Math.Max(0, ThrowsPerDay - S.netThrows); } }
        public static void UseThrow() { if (S.netDay != S.day) { S.netDay = S.day; S.netThrows = 0; } S.netThrows++; Story.Save(); }

        // ---------- แกว่งแห ----------
        /// เข็มแกว่ง 0..1..0 ตามเวลาที่กดค้าง (รอบละ Period วิ)
        public const float Period = 1.1f;
        public static float Needle(float t) => 0.5f - 0.5f * (float)Math.Cos(t / Period * Math.PI * 2);
        public const float PerfectHalf = 0.09f, GoodHalf = 0.22f, ZoneCenter = 0.82f;   // ช่องเขียวอยู่ใกล้ยอดแกว่ง (ปล่อยตอนแหเหวี่ยงขึ้นสูงสุด)
        public enum Grade { Poor, Good, Perfect }
        public static Grade GradeOf(float needle)
        {
            float d = Math.Abs(needle - ZoneCenter);
            return d <= PerfectHalf ? Grade.Perfect : d <= GoodHalf ? Grade.Good : Grade.Poor;
        }
        /// แหกางกี่ส่วน: เต็มวง / ค่อนวง / พับครึ่ง
        public static float Open(Grade g) => g == Grade.Perfect ? 1f : g == Grade.Good ? 0.72f : 0.4f;
        public const float FullRadius = 2.3f;   // รัศมีแหกางเต็มวง (ม.)

        // ---------- ฝูงปลา ----------
        /// ชนิดปลาที่ติดแหได้ในด่านนี้ (ปลาดาวเดียว) สุ่มตามโอกาสเจอ
        public static List<FishSpecies> Pool(string stage) => FishDatabase.Rod(stage).Where(s => s.stars <= 1 && !s.rare).ToList();
        public static FishSpecies PickSpecies(List<FishSpecies> pool, Random rng)
        {
            int sum = pool.Sum(s => Math.Max(1, s.chance)); int r = rng.Next(sum);
            foreach (var s in pool) { r -= Math.Max(1, s.chance); if (r < 0) return s; }
            return pool[pool.Count - 1];
        }
        /// น้ำหนักปลาเล็กที่ติดแห (ค่อนไปทางตัวเล็กของชนิดนั้น)
        public static float RollKg(FishSpecies sp, Random rng) => (float)Math.Round(sp.kgMin + (sp.kgMax - sp.kgMin) * 0.6 * Math.Pow(rng.NextDouble(), 1.4), 2);

        // ---------- สาวแห ----------
        public const int Pulls = 3;
        public const float PullPeriod = 1.0f, PullWindow = 0.22f;
        /// 0..1 จังหวะสาวแห (ยอด = จังหวะที่ควรแตะ)
        public static float PullPulse(float t) => 1 - Math.Min(1f, Math.Abs((t % PullPeriod) - PullPeriod * 0.5f) / (PullPeriod * 0.5f));
        public static bool PullOnBeat(float t) => Math.Abs((t % PullPeriod) - PullPeriod * 0.5f) < PullWindow;

        // ---------- ได้ปลา ----------
        public class Haul { public FishSpecies sp; public int count; public float kg; }
        /// รวมปลาที่ติดแหเป็นกองละชนิด
        public static List<Haul> Group(IEnumerable<(FishSpecies sp, float kg)> fish) =>
            fish.GroupBy(f => f.sp.id).Select(g => new Haul { sp = g.First().sp, count = g.Count(), kg = (float)Math.Round(g.Sum(x => x.kg), 2) })
                .OrderByDescending(h => h.kg * Story.PriceOf(h.sp.id)).ToList();
        public static int ValueOf(Haul h) => Story.Value(new BasketFish { id = h.sp.id, kg = h.kg });

        /// ใส่ข้อง: กองละช่อง (ชนิดเดียวกันที่อยู่ในข้องแล้วรวมกองเดิม) · ข้องเต็ม = ปล่อยกองที่เหลือ · คืนจำนวนกองที่ใส่ไม่ได้
        public static int Keep(List<Haul> hauls)
        {
            int dropped = 0;
            foreach (var h in hauls)
            {
                var pile = S.basket.FirstOrDefault(b => b.id == h.sp.id && b.cm < 0);   // cm < 0 = กองปลาจากแห (เก็บจำนวนตัวเป็นค่าลบ)
                if (pile != null) { pile.kg = (float)Math.Round(pile.kg + h.kg, 2); pile.cm -= h.count; continue; }
                if (Story.BasketFull) { dropped++; continue; }
                S.basket.Add(new BasketFish { id = h.sp.id, kg = h.kg, cm = -h.count });
            }
            S.caught += hauls.Sum(h => h.count);
            Story.Save();
            return dropped;
        }
    }
}
