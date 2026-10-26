using System;
using System.Linq;
using UnityEngine;

namespace CozyFishing
{
    // อีเวนต์ "เทศกาลปลาใหญ่" ประจำสัปดาห์: ทุกสัปดาห์หมุนด่านเทศกาลไปตามลำดับ (ทุกคนเห็นด่านเดียวกัน)
    //   ปลาตัวหนักสุดของเราที่ด่านนั้นในสัปดาห์นี้ → รางวัลขั้นทองแดง/เงิน/ทอง (ครั้งเดียวต่อขั้นต่อสัปดาห์) · อันดับจริงดูบนกระดาน bigfish_<ด่าน>
    //   สัปดาห์ = จันทร์ 00:00 UTC (07:00 เวลาไทย) ตรงกับรอบรีเซ็ตกระดานใน Unity Cloud
    //   ด่านที่ยังไม่ปลดในเนื้อเรื่อง ร่วมได้ผ่านโหมดตกปลาอิสระ (เปิดทุกด่าน)
    public static class Weekly
    {
        const string Key = "cozy.weekly.v1";
        public static readonly string[] TierName = { "ทองแดง", "เงิน", "ทอง" };
        public static readonly int[] TierReward = { 100, 250, 500 };
        static readonly float[] TierFrac = { 0.2f, 0.4f, 0.65f };

        [Serializable] class State { public string week; public float best; public string bestFish; public bool[] got = new bool[3]; }
        static State s;

        static DateTime Now { get { var u = GameUrl.Value; int i = u.IndexOf("weekdate=", StringComparison.Ordinal); if (i >= 0 && u.Length >= i + 19 && DateTime.TryParseExact(u.Substring(i + 9, 10), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d)) return d.AddHours(12); return DateTime.UtcNow; } }   // ทดสอบ: ?weekdate=2026-11-09
        static DateTime WeekStart { get { var n = Now.Date; int back = ((int)n.DayOfWeek + 6) % 7; return n.AddDays(-back); } }   // จันทร์ (UTC)
        public static string WeekKey => WeekStart.ToString("yyyy-MM-dd");
        public static TimeSpan Left => WeekStart.AddDays(7) - Now;
        static int WeekIndex => (int)((WeekStart - new DateTime(2026, 1, 5)).TotalDays / 7);   // 5 ม.ค. 2026 = จันทร์

        // หมุนตามลำดับด่าน โดยยึดสัปดาห์ 26 ต.ค. 2026 (index 42) = ถ้ำ (ลำดับที่ 6) ไว้ · เพิ่มด่านใหม่ท้ายรายการแล้วสัปดาห์ปัจจุบันไม่เปลี่ยน
        public static StageDef Stage { get { var all = Stages.All; int i = WeekIndex - 42 + 6; return all[((i % all.Length) + all.Length) % all.Length]; } }
        public static bool CanJoin => Stages.Unlocked(Stage.id);

        /// เกณฑ์ (กก.) แต่ละขั้น จากปลาตัวโตสุดปกติของด่าน (ไม่นับปลาหายาก) ปัดทีละ 0.1
        public static float Tier(int i)
        {
            var max = FishDatabase.ForStage(Stage.id).Where(f => !f.rare).Select(f => f.kgMax).DefaultIfEmpty(2f).Max();
            return Mathf.Max(0.1f, Mathf.Round(max * TierFrac[i] * 10) / 10f);
        }

        static State S
        {
            get
            {
                if (s == null) { try { var j = PlayerPrefs.GetString(Key, ""); if (j.Length > 0) s = JsonUtility.FromJson<State>(j); } catch (Exception) { s = null; } }
                if (s == null || s.week != WeekKey) { s = new State { week = WeekKey }; Save(); }
                if (s.got == null || s.got.Length != 3) s.got = new bool[3];
                return s;
            }
        }
        static void Save() { try { PlayerPrefs.SetString(Key, JsonUtility.ToJson(s)); PlayerPrefs.Save(); } catch (Exception) { } Cloud.MarkDirty(); }
        public static void Reload() { s = null; }
        public static float Best => S.best;
        public static bool Got(int i) => S.got[i];
        public static int Tiers => S.got.Count(g => g);

        /// ได้ปลา → ถ้าเป็นด่านเทศกาลและหนักกว่าเดิม อัปเดตสถิติ · คืนข้อความรางวัลขั้นที่เพิ่งผ่าน
        public static System.Collections.Generic.List<string> OnCatch(string stage, FishSpecies sp, float kg)
        {
            var msgs = new System.Collections.Generic.List<string>();
            if (sp == null || stage != Stage.id) return msgs;
            var st = S;
            if (kg > st.best) { st.best = kg; st.bestFish = sp.id; }
            int money = 0;
            for (int i = 0; i < 3; i++)
                if (!st.got[i] && st.best >= Tier(i)) { st.got[i] = true; money += TierReward[i]; msgs.Add($"เทศกาลปลาใหญ่ ขั้น{TierName[i]}! +{Story.Baht(TierReward[i])}"); }
            if (money > 0) { Story.S.money += money; Story.S.earned += money; Story.Save(); Debug.Log("[Weekly] " + string.Join(" · ", msgs)); }
            Save();
            return msgs;
        }
    }
}
