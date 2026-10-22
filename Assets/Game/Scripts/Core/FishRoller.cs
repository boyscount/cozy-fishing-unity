using System;
using System.Linq;

namespace CozyFishing
{
    // สุ่มปลาที่มากินเหยื่อ — ย้ายมาจาก rollFish() ของเวอร์ชันเว็บ
    //  · ใกล้จุดพิเศษ = เจอตัวหายากบ่อยขึ้น · น้ำลึก = ปลาใหญ่ขึ้น · ปลาบางชนิดชอบกินเหยื่อตามช่วงเวลา
    public static class FishRoller
    {
        public static readonly string[] Junk = { "รองเท้าแตะเก่า", "กิ่งไม้ผุ", "ถุงพลาสติก", "กระป๋องเปล่า", "สาหร่ายพันเบ็ด" };

        public static string Period(float hour) => hour < 9.5f ? "morning" : hour >= 18.5f ? "night" : hour >= 15.5f ? "evening" : "day";

        public static Catch Roll(string stage, bool nearSpecial, float depth, float junkChance, float hour, Random rng)
        {
            if (rng.NextDouble() < junkChance) return new Catch { junk = Junk[rng.Next(Junk.Length)] };
            var pool = FishDatabase.ForStage(stage).ToArray();
            if (pool.Length == 0) return new Catch { junk = Junk[0] };
            var per = Period(hour);
            var d = Math.Min(depth, 3f);
            var w = pool.Select(s => s.chance
                * (s.rare ? (nearSpecial ? 5f : 0.6f) : 1f)
                * (s.stars >= 3 ? 0.6f + d * 0.5f : 1f)
                * (nearSpecial && s.stars >= 2 ? 1.4f : 1f)
                * (s.time == "any" ? 1f : s.time == per ? 2.2f : 0.6f)).ToArray();
            var r = rng.NextDouble() * w.Sum();
            var sp = pool[0];
            for (int i = 0; i < w.Length; i++) { r -= w[i]; if (r <= 0) { sp = pool[i]; break; } }
            var t = (float)Math.Min(1, Math.Pow(rng.NextDouble(), 1.6) * (0.8 + d * 0.25));
            return new Catch
            {
                species = sp,
                kg = (float)Math.Round(sp.kgMin + (sp.kgMax - sp.kgMin) * t, 2),
                cm = (int)Math.Round(sp.cmMin + (sp.cmMax - sp.cmMin) * t),
            };
        }
    }
}
