using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace CozyFishing
{
    // โหมดเนื้อเรื่อง (ย้ายจาก story.js ของเวอร์ชันเว็บ): วันต่อวัน · ดวงประจำวัน · ข้องปลา · ราคาตลาด · เงิน · ความสุขครอบครัว · ซ่อมหลังคา
    //  ไม่ผูกกับ Unity (ยกเว้นบันทึกลง PlayerPrefs) → ทดสอบใน EditMode ได้

    [Serializable]
    public class BasketFish { public string id; public float kg; public int cm; }

    public class Luck
    {
        public string id, text; public float p, bite, junk, sun; public bool rain;
    }

    public class Goal { public string text, done; public int cost; }

    public enum StoryPhase { New, Morning, Fishing, Evening }

    [Serializable]
    public class StoryState
    {
        public int v = 1, day = 1, money, happy = 70, goal;
        public float time = Story.DayStart;
        public string luck = "normal", hot, phase = "New";
        public bool dead;                                   // บางวันฝนตกหนัก ปลาไม่กินเหยื่อจริง ๆ
        public List<BasketFish> basket = new List<BasketFish>();
        public List<string> marketIds = new List<string>();
        public List<float> marketMul = new List<float>();
        public bool bigBasket;
        public string stage = "paddy";                     // ด่านที่กำลังตกปลาอยู่
        public List<string> unlocked = new List<string>();  // ด่านที่ซื้อทางไปแล้ว (บ่อทุ่งนาเปิดเสมอ)
        public int caught, junk, sold, grilled, empty, earned;
        public int amulets;                                  // สายสิญจน์/เหรียญขวัญถุง (ตกได้กลางคืน) กันผี
        // ร้านโชห่วย (Shop.cs): ขั้นคัน/รอก · เหยื่อที่ใช้อยู่ + จำนวนคงเหลือ · วันที่ทำบุญ · ของถาวร (ตะเกียง ไฟฉาย ของในบ้าน)
        public int rod, reel, baitShrimp, baitBran, baitFrog, meritDay = -1;
        public string bait = "worm";
        public List<string> items = new List<string>();
        public bool glimpsed;                                // เห็นผีแวบ ๆ ครั้งแรกแล้ว (เย็นวันแรก)
    }

    public class EveningResult
    {
        public float fed; public int earned, dh, nGrill, nSell; public string mood;
        public List<BasketFish> grilledFish = new List<BasketFish>();
    }

    public static class Story
    {
        public const float DayStart = 6, DayEnd = 18, HoursPerSec = 12f / 420f;   // หนึ่งวัน = 7 นาทีจริง
        public const float NeedKg = 1.2f;                                          // ครอบครัวต้องการปลาเท่านี้ถึงจะอิ่ม
        public const float HotMul = 1.8f;
        const string Key = "cozy.story.v1";

        public static readonly Luck[] Lucks =
        {
            new Luck { id = "great", p = 0.15f, text = "ฟ้าใส ลมเย็น — วันนี้ปลาชุมมาก!", bite = 0.6f, junk = 0.04f, sun = 1.0f },
            new Luck { id = "normal", p = 0.5f, text = "อากาศดี ปลากินเหยื่อตามปกติ", bite = 1f, junk = 0.08f, sun = 0.9f },
            new Luck { id = "slow", p = 0.23f, text = "ฟ้าครึ้ม ปลาไม่ค่อยกินเหยื่อ", bite = 1.8f, junk = 0.16f, sun = 0.55f },
            new Luck { id = "bad", p = 0.12f, text = "ฝนพรำ น้ำขุ่น… วันนี้อาจไม่ได้ปลาเลย", bite = 3.4f, junk = 0.4f, sun = 0.35f, rain = true },
        };

        public static readonly Goal[] Goals =
        {
            new Goal { text = "ซ่อมหลังคาบ้านก่อนหน้าฝน", cost = 1500, done = "หลังคาใหม่แล้ว! ฝนตกก็ไม่รั่วอีกต่อไป" },
            new Goal { text = "ซื้อจักรยานให้ลูกปั่นไปโรงเรียน", cost = 3000, done = "ลูกปั่นจักรยานไปโรงเรียนอย่างภูมิใจ" },
            new Goal { text = "พาครอบครัวไปเที่ยวงานวัด", cost = 5000, done = "ทั้งบ้านได้ชิงช้าสวรรค์ ขนมสายไหม และรอยยิ้มเต็มหน้า" },
        };

        public static readonly Dictionary<string, (string who, string say)[]> EveningLines = new Dictionary<string, (string, string)[]>
        {
            { "full", new[] { ("แม่", "ปลาปิ้งหอมไปทั้งซอยเลยพ่อ อิ่มแปล้กันทั้งบ้าน"), ("ลูก", "พ่อเก่งที่สุดในโลก! พรุ่งนี้ไปด้วยได้ไหม") } },
            { "some", new[] { ("แม่", "วันนี้พอได้กินกันบ้าง แบ่งกันคนละนิดก็อร่อยนะ"), ("ลูก", "อยากกินอีกตัวจัง… พรุ่งนี้พ่อตกตัวใหญ่ ๆ นะ") } },
            { "sold", new[] { ("ลูก", "ขายหมดเลยเหรอพ่อ… ไม่เป็นไร กินข้าวกับน้ำพริกก็ได้"), ("แม่", "เก็บเงินไว้ซ่อมบ้านก็ดี แต่อย่าลืมเผื่อท้องตัวเองบ้างนะ") } },
            { "empty", new[] { ("แม่", "วันนี้ไม่ได้ปลาเลยเหรอ… ไม่เป็นไรนะพ่อ แม่ต้มไข่กับผักลวกไว้แล้ว"), ("ลูก", "พรุ่งนี้ปลาต้องกินเหยื่อแน่ ๆ พ่อสู้ ๆ!") } },
        };

        public static readonly (string who, string say)[] Intro =
        {
            ("แม่", "พ่อจ๋า เมื่อคืนฝนตก หลังคารั่วอีกแล้วนะ"),
            ("ลูก", "พ่อ ๆ ไปตกปลามาปิ้งกินกันเถอะ หนูหิว!"),
            ("พ่อ", "ได้เลย วันนี้พ่อจะไปตกปลาที่บ่อกลางทุ่งนา"),
            ("แม่", "ได้ปลามาเยอะ ๆ ก็แบ่งปิ้งกินบ้าง ขายตลาดบ้าง เก็บเงินซ่อมหลังคานะ"),
            ("พ่อ", "แต่บางวันปลาก็ไม่กินเหยื่อนะ… ไม่เป็นไร ค่อย ๆ เก็บไปทีละวัน"),
        };

        public static StoryState S { get; private set; } = Load();

        // ---------- บันทึก ----------
        static StoryState Load()
        {
            try
            {
                var json = PlayerPrefs.GetString(Key, "");
                if (!string.IsNullOrEmpty(json)) { var d = JsonUtility.FromJson<StoryState>(json); if (d != null && d.v == 1) return d; }
            }
            catch (Exception) { /* เซฟเสีย → เริ่มใหม่ */ }
            var s = new StoryState();
            RollMarket(s, GameUrl.Rng());
            return s;
        }
        const string FreeKey = "cozy.free.v1";
        /// โหมดตกปลาอิสระ: ทุกด่านเปิด ไม่มีเนื้อเรื่อง/คดี · บันทึกแยกจากเนื้อเรื่อง
        public static bool FreePlay { get; private set; }
        public static bool HasStorySave => S.day > 1 || Phase != StoryPhase.New;
        public static void Save() { try { PlayerPrefs.SetString(FreePlay ? FreeKey : Key, JsonUtility.ToJson(S)); PlayerPrefs.Save(); } catch (Exception) { } Cloud.MarkDirty(); }
        /// ดึงเซฟจากคลาวด์ลงเครื่องแล้ว → อ่านใหม่ (เรียกตอนยังไม่เริ่มเล่นเท่านั้น)
        public static void ReloadFromPrefs() { if (!FreePlay) S = Load(); }
        public static void EnterFreePlay()
        {
            FreePlay = true;
            StoryState f = null;
            try { var json = PlayerPrefs.GetString(FreeKey, ""); if (!string.IsNullOrEmpty(json)) f = JsonUtility.FromJson<StoryState>(json); } catch (Exception) { }
            if (f == null) { f = new StoryState(); RollMarket(f, GameUrl.Rng()); }
            f.unlocked = Stages.All.Select(st => st.id).ToList();
            f.phase = StoryPhase.Morning.ToString(); f.basket.Clear(); f.time = DayStart; f.bigBasket = true;
            S = f; Save();
        }
        public static void ExitFreePlay() { if (!FreePlay) return; Save(); FreePlay = false; S = Load(); }
        public static void Reset(Random rng = null) { S = new StoryState(); RollMarket(S, rng ?? GameUrl.Rng()); Save(); }
        public static void UseForTests(StoryState s) => S = s;
        public static StoryPhase Phase { get => Enum.TryParse<StoryPhase>(S.phase, out var p) ? p : StoryPhase.New; set { S.phase = value.ToString(); Save(); } }

        // ---------- ราคาตลาด: เปลี่ยนทุกเช้า ±35% และมีปลา "ราคาพิเศษ" 1 ชนิด (x1.8) ----------
        public static void RollMarket(StoryState s, Random rng)
        {
            s.marketIds.Clear(); s.marketMul.Clear();
            foreach (var sp in FishDatabase.All) { s.marketIds.Add(sp.id); s.marketMul.Add((float)Math.Round(0.65 + rng.NextDouble() * 0.7, 2)); }
            var pool = FishDatabase.ForStage("paddy").Where(sp => !sp.rare).ToArray();
            s.hot = pool.Length > 0 ? pool[rng.Next(pool.Length)].id : null;
        }
        static float MarketMul(string id) { int i = S.marketIds.IndexOf(id); return i >= 0 ? S.marketMul[i] : 1f; }
        public static int PriceOf(string id)
        {
            var sp = FishDatabase.Get(id);
            return sp == null ? 0 : (int)Math.Round(sp.price * MarketMul(id) * (S.hot == id ? HotMul : 1));
        }
        // ปลาตัวโตมาก (เกิน 5 กก.) ราคาต่อกิโลลดหลั่น: ปลาตำนาน 100 กก. ยังเป็นรางวัลใหญ่ แต่ไม่ทำให้เงินล้นจนของในร้านไร้ค่า
        public static float SellKg(float kg) => kg <= 5f ? kg : 5f + (float)Math.Pow(kg - 5f, 0.6);
        public static int Value(BasketFish f) => (int)Math.Round(SellKg(f.kg) * PriceOf(f.id));
        public static int BasketValue => S.basket.Sum(Value);
        public static string Trend(string id) { var m = MarketMul(id) * (S.hot == id ? HotMul : 1); return m >= 1.12f ? "up" : m <= 0.88f ? "down" : "flat"; }

        // ---------- ข้อง / ดวง ----------
        public static int Capacity => S.bigBasket ? 14 : 8;
        public static bool BasketFull => S.basket.Count >= Capacity;
        public static Luck CurrentLuck => Lucks.FirstOrDefault(l => l.id == S.luck) ?? Lucks[1];
        public static float BiteMul => CurrentLuck.bite * (S.happy >= 75 ? 0.9f : 1f);
        public static float JunkChance => CurrentLuck.junk;
        public static int CheapestIndex() { int bi = 0; for (int i = 1; i < S.basket.Count; i++) if (Value(S.basket[i]) < Value(S.basket[bi])) bi = i; return bi; }

        /// เช้าวันใหม่: สุ่มราคาตลาด + ดวง (วันแรกอากาศดีเสมอ)
        public static void RollDay(Random rng)
        {
            RollMarket(S, rng);
            S.luck = "normal";
            if (S.day > 1)
            {
                double r = rng.NextDouble();
                foreach (var l in Lucks) { r -= l.p; if (r <= 0) { S.luck = l.id; break; } }
            }
            S.dead = S.luck == "bad" && rng.NextDouble() < 0.4;
            S.time = DayStart;
        }

        public static void AddFish(BasketFish f) { S.basket.Add(f); S.caught++; Save(); }
        /// ข้องเต็ม: เก็บตัวใหม่แทนตัวที่ราคาถูกสุด · คืนตัวที่ถูกปล่อย
        public static BasketFish SwapCheapest(BasketFish f) { int i = CheapestIndex(); var old = S.basket[i]; S.basket[i] = f; S.caught++; Save(); return old; }
        public static BasketFish Release(int i) { var f = S.basket[i]; S.basket.RemoveAt(i); Save(); return f; }

        /// ค่าเริ่มต้นของมื้อเย็น: ปิ้งตัวที่ราคาต่อกิโลถูกสุดก่อนจนพอให้ครอบครัวอิ่ม ที่เหลือขาย
        public static bool[] DefaultGrill()
        {
            var grill = new bool[S.basket.Count];
            float fed = 0;
            foreach (var i in Enumerable.Range(0, S.basket.Count).OrderBy(i => FishDatabase.Get(S.basket[i].id)?.price ?? 0))
            {
                if (fed >= NeedKg) break;
                grill[i] = true; fed += S.basket[i].kg;
            }
            return grill;
        }

        /// ผลของมื้อเย็น: grill[i] = true ปิ้ง / false ขาย
        public static EveningResult SettleEvening(bool[] grill)
        {
            var r = new EveningResult();
            for (int i = 0; i < S.basket.Count; i++)
            {
                var f = S.basket[i];
                if (i < grill.Length && grill[i]) { r.fed += f.kg; r.nGrill++; r.grilledFish.Add(f); }
                else { r.earned += Value(f); r.nSell++; }
            }
            if (S.basket.Count == 0) { r.dh = -8; r.mood = "empty"; S.empty++; }
            else if (r.fed >= NeedKg) { r.dh = 12; r.mood = "full"; }
            else if (r.fed > 0) { r.dh = 4; r.mood = "some"; }
            else { r.dh = -5; r.mood = "sold"; }
            S.happy = Math.Max(0, Math.Min(100, S.happy + r.dh));
            S.money += r.earned; S.earned += r.earned; S.sold += r.nSell; S.grilled += r.nGrill;
            S.basket.Clear();
            Save();
            return r;
        }

        public static Goal CurrentGoal => S.goal < Goals.Length ? Goals[S.goal] : null;
        public static bool RoofFixed => S.goal >= 1;
        public static bool CanPayGoal => CurrentGoal != null && S.money >= CurrentGoal.cost && (S.goal < 2 || Mystery.AllSolved);   // งานวัดต้องไขคดีตามแผนที่ยายอิ่มครบทุกบทก่อน
        public static bool PayGoal() { if (!CanPayGoal) return false; S.money -= CurrentGoal.cost; S.goal++; Save(); return true; }

        public static void Sleep(Random rng) { S.day++; RollDay(rng); Shop.MorningHappiness(); Phase = StoryPhase.Morning; }

        public static string FmtTime(float h) { h %= 24; return $"{(int)h:00}:{(int)((h % 1) * 60):00}"; }   // หลังเที่ยงคืน (เวลาเกิน 24) แสดงเป็น 00:xx, 01:xx …
        public static string Baht(int n) => "฿" + n.ToString("N0");
    }
}
