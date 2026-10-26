using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CozyFishing
{
    // ภารกิจรายวัน + ปลาเด่นประจำวัน: ผูกกับวันที่จริง (เวลาไทย) ไม่ใช่วันในเกม → กลับมาเล่นทุกวันมีเป้าใหม่
    //   สุ่มจากวันที่ (คนที่เปิดด่านเท่ากันได้ชุดเดียวกัน) · สร้างครั้งแรกที่เปิดในวันนั้นแล้วเก็บไว้ ไม่เปลี่ยนกลางวันแม้ปลดด่านใหม่
    //   รางวัลเป็นเงินเข้ากระเป๋าโหมดที่เล่นอยู่ · ความคืบหน้าใช้ร่วมกันทั้งโหมดเนื้อเรื่องและตกปลาอิสระ
    public static class Daily
    {
        const string Key = "cozy.daily.v1";
        public const int AllBonus = 150;

        [Serializable]
        public class Quest
        {
            public string kind, stage;   // count · kg · big · stars · stage · species
            public int target, prog, reward;
            public bool done;
            public List<string> seen = new List<string>();   // species: ชนิดที่ตกได้แล้ววันนี้
        }

        [Serializable]
        public class State
        {
            public string date, featured;
            public bool featDone, allDone;
            public List<Quest> quests = new List<Quest>();
        }

        static State s;
        /// เพิ่มทุกครั้งที่ความคืบหน้าเปลี่ยน (แผงที่เปิดอยู่รู้ว่าต้องวาดใหม่)
        public static int Version { get; private set; }
        public static State S { get { Ensure(); return s; } }

        /// วันที่ไทยตอนนี้ (ทดสอบ: ?dailydate=2026-10-27)
        public static string Today
        {
            get
            {
                var u = GameUrl.Value; int i = u.IndexOf("dailydate=", StringComparison.Ordinal);
                if (i >= 0 && u.Length >= i + 20) return u.Substring(i + 10, 10);
                return DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd");
            }
        }
        /// เวลาที่เหลือก่อนเที่ยงคืน (เวลาไทย)
        public static TimeSpan UntilReset { get { var th = DateTime.UtcNow.AddHours(7); return th.Date.AddDays(1) - th; } }

        static void Ensure()
        {
            var today = Today;
            if (s != null && s.date == today) return;
            if (s == null)
            {
                try { var json = PlayerPrefs.GetString(Key, ""); if (json.Length > 0) s = JsonUtility.FromJson<State>(json); } catch (Exception) { s = null; }
            }
            if (s == null || s.date != today) { s = Generate(today); Save(); }
        }
        public static void Reload() { s = null; }   // ดึงเซฟจากคลาวด์แล้ว
        static void Save() { try { PlayerPrefs.SetString(Key, JsonUtility.ToJson(s)); PlayerPrefs.Save(); } catch (Exception) { } Cloud.MarkDirty(); }

        static int Seed(string date) { unchecked { int h = 17; foreach (var c in date) h = h * 31 + c; return h; } }

        public static State Generate(string date)
        {
            var rng = new System.Random(Seed(date));
            var open = Stages.All.Where(st => Stages.Unlocked(st.id)).Select(st => st.id).ToList();
            var st = new State { date = date };
            // ปลาเด่น: ★2–3 ไม่ใช่ปลาหายาก จากด่านที่เปิดแล้ว (ไม่มี → ★1)
            var pool = FishDatabase.All.Where(f => open.Contains(f.stage) && !f.rare && f.stars >= 2 && f.stars <= 3).OrderBy(f => f.id).ToList();
            if (pool.Count == 0) pool = FishDatabase.All.Where(f => open.Contains(f.stage) && !f.rare).OrderBy(f => f.id).ToList();
            st.featured = pool.Count > 0 ? pool[rng.Next(pool.Count)].id : null;
            // ภารกิจ 3 อย่างไม่ซ้ำแบบ
            var kinds = new List<string> { "count", "kg", "big", "stars", "species" };
            if (open.Count > 1) kinds.Add("stage");
            for (int i = 0; i < 3 && kinds.Count > 0; i++)
            {
                var k = kinds[rng.Next(kinds.Count)]; kinds.Remove(k);
                var q = new Quest { kind = k };
                switch (k)
                {
                    case "count": q.target = new[] { 5, 7, 10 }[rng.Next(3)]; q.reward = q.target * 15; break;
                    case "kg": q.target = new[] { 2, 3, 5 }[rng.Next(3)]; q.reward = q.target * 30; break;          // รวมน้ำหนัก (กก.) เก็บความคืบหน้าเป็นกรัม
                    case "big": q.target = new[] { 1, 2 }[rng.Next(2)]; q.reward = 60 + q.target * 40; break;         // ตัวเดียวหนักอย่างน้อย (กก.)
                    case "stars": q.target = new[] { 1, 2, 3 }[rng.Next(3)]; q.reward = q.target * 50; break;
                    case "species": q.target = 3; q.reward = 90; break;
                    case "stage": var others = open.Where(o => o != "paddy").ToList(); q.stage = others[rng.Next(others.Count)]; q.target = 3; q.reward = 100; break;
                }
                st.quests.Add(q);
            }
            return st;
        }

        public static string Text(Quest q) => q.kind switch
        {
            "count" => $"ตกปลาให้ได้ {q.target} ตัว",
            "kg" => $"ตกปลารวมน้ำหนัก {q.target} กก.",
            "big" => $"ตกปลาตัวเดียวหนัก {q.target} กก. ขึ้นไป",
            "stars" => $"ตกปลาระดับ 2 ดาวขึ้นไป {q.target} ตัว",
            "species" => $"ตกปลาไม่ซ้ำชนิด {q.target} ชนิด",
            "stage" => $"ตกปลาที่{Stages.Get(q.stage).name} {q.target} ตัว",
            _ => q.kind,
        };
        /// ความคืบหน้าแบบอ่านง่าย เช่น "3/5" หรือ "1.2/3 กก."
        public static string Progress(Quest q) => q.kind == "kg" ? $"{Math.Min(q.prog, q.target * 1000) / 1000f:0.0}/{q.target} กก." : q.kind == "big" ? (q.done ? "สำเร็จ" : "ยังไม่ได้") : $"{Math.Min(q.prog, q.target)}/{q.target}";
        public static float Fraction(Quest q) => q.done ? 1 : q.kind == "kg" ? Mathf.Clamp01(q.prog / (q.target * 1000f)) : q.kind == "big" ? 0 : Mathf.Clamp01((float)q.prog / q.target);
        public static int DoneCount => S.quests.Count(q => q.done);
        public static int FeaturedReward(FishSpecies f) => f == null ? 0 : Mathf.Clamp(f.price * 2, 60, 300);   // ไม่เกิน 300: หลังคา 1,500 ยังต้องตกปลาหลายวัน
        public static string TimeHint(string time) => time switch { "morning" => "ชุมช่วงเช้า", "evening" => "ชุมช่วงเย็น", "night" => "ออกหากินกลางคืน", _ => "ตกได้ทั้งวัน" };

        /// ได้ปลา 1 ตัว → อัปเดตภารกิจ · คืนข้อความรางวัลที่เพิ่งได้ (ว่าง = ไม่มีอะไรสำเร็จ)
        public static List<string> OnCatch(string stage, FishSpecies sp, float kg)
        {
            var msgs = new List<string>();
            if (sp == null) return msgs;
            var st = S; int money = 0;
            if (!st.featDone && sp.id == st.featured)
            {
                st.featDone = true; int r = FeaturedReward(sp); money += r;
                msgs.Add($"ปลาเด่นวันนี้! +{Story.Baht(r)} · เกล็ดทอง +2"); Cosmetics.Earn(2, "ปลาเด่น");
            }
            foreach (var q in st.quests)
            {
                if (q.done) continue;
                switch (q.kind)
                {
                    case "count": q.prog++; break;
                    case "kg": q.prog += Mathf.RoundToInt(kg * 1000); break;
                    case "big": if (kg >= q.target) q.prog = 1; break;
                    case "stars": if (sp.stars >= 2) q.prog++; break;
                    case "species": if (!q.seen.Contains(sp.id)) { q.seen.Add(sp.id); q.prog = q.seen.Count; } break;
                    case "stage": if (stage == q.stage) q.prog++; break;
                }
                bool reached = q.kind == "kg" ? q.prog >= q.target * 1000 : q.kind == "big" ? q.prog >= 1 : q.prog >= q.target;
                if (reached) { q.done = true; money += q.reward; msgs.Add($"ภารกิจสำเร็จ: {Text(q)} +{Story.Baht(q.reward)}"); }
            }
            if (!st.allDone && st.quests.Count > 0 && st.quests.All(q => q.done))
            {
                st.allDone = true; money += AllBonus;
                msgs.Add($"ครบทุกภารกิจวันนี้! โบนัส +{Story.Baht(AllBonus)} · เกล็ดทอง +5"); Cosmetics.Earn(5, "ภารกิจครบ");
            }
            Version++;
            if (money > 0) { Story.S.money += money; Story.S.earned += money; Story.Save(); }
            Save();
            if (msgs.Count > 0) Debug.Log("[Daily] " + string.Join(" · ", msgs));
            return msgs;
        }

        public static void ResetForTests(State st = null) { s = st; }
    }
}
