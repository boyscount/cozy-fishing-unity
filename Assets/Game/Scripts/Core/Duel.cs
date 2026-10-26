using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CozyFishing
{
    // ท้าดวลผ่านลิงก์ (ไม่ต้องมีเซิร์ฟเวอร์): ทั้งสองคนเจอปลาชุดเดียวกันเป๊ะ (สุ่มจากเลขดวลเดียวกัน) 5 ตัว ที่ด่านเดียวกัน
    //   คะแนน = น้ำหนักรวมที่ดึงขึ้นได้ · เสมอกันวัดเวลาที่ใช้ · จบแล้วแชร์ลิงก์ที่ฝังคะแนนตัวเองไว้ให้เพื่อนเล่นแข่ง
    //   ลิงก์: ?duel=<เลข>.<ด่าน>&vs=<กก.>&by=<ชื่อ>
    public static class Duel
    {
        public const int Casts = 5;
        public static bool Active { get; private set; }
        public static int Seed { get; private set; }
        public static string Stage { get; private set; } = "paddy";
        public static float VsKg = -1; public static string VsName = "";
        public static readonly List<Catch> Lineup = new List<Catch>();
        public static int Index;            // ตัวที่กำลังสู้ (0..4)
        public static float TotalKg, StartTime = -1;
        public static readonly List<string> Log = new List<string>();
        public static bool Done => Active && Index >= Casts;

        /// อ่านลิงก์ดวลจาก URL (เรียกครั้งเดียวตอนเปิดเกม)
        public static void FromUrl()
        {
            var m = Regex.Match(GameUrl.Value, @"duel=(\d+)\.([a-z]+)");
            if (!m.Success) return;
            Start(int.Parse(m.Groups[1].Value), m.Groups[2].Value);
            var v = Regex.Match(GameUrl.Value, @"vs=([0-9.]+)"); if (v.Success) float.TryParse(v.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out VsKg);
            var b = Regex.Match(GameUrl.Value, @"by=([^&#]+)"); if (b.Success) VsName = Uri.UnescapeDataString(b.Groups[1].Value).Replace('+', ' ');
        }

        /// เริ่มดวล (สร้างใหม่จากในเกม หรือจากลิงก์) · ปลา 5 ตัวสุ่มจากเลขดวล — ทุกเครื่องได้ชุดเดียวกัน
        public static void Start(int seed, string stage)
        {
            Active = true; Seed = seed; Stage = Stages.Get(stage).id; Index = 0; TotalKg = 0; StartTime = -1; Log.Clear(); VsKg = -1; VsName = "";
            var rng = new System.Random(seed); Lineup.Clear();
            for (int i = 0; i < Casts; i++)
            {
                Catch c; int guard = 0;
                do c = FishRoller.Roll(Stage, i == Casts - 1, 2f, 0f, i % 2 == 0 ? 8f : 17f, rng, i == Casts - 1 ? 0.5f : 0f);   // ตัวสุดท้ายมีลุ้นปลาหายาก
                while ((c.IsJunk || c.species.id == Boss.Id) && ++guard < 20);
                Lineup.Add(c);
            }
        }
        public static Catch Next => Lineup[Mathf.Clamp(Index, 0, Lineup.Count - 1)];

        /// จบหนึ่งตัว (ได้หรือหลุด) · คืนข้อความสรุปความคืบหน้า
        public static string Resolve(bool caught, float kg, string name)
        {
            if (StartTime < 0) StartTime = Time.time;
            if (caught) TotalKg += kg;
            Log.Add(caught ? $"{name} {kg:0.00} กก." : $"{name} หลุด");
            Index++;
            Debug.Log($"[Duel] {Index}/{Casts} caught={caught} total={TotalKg:0.00}");
            return Done ? "" : $"ดวล: ตัวที่ {Index}/{Casts} · รวม {TotalKg:0.00} กก.";
        }
        public static float Seconds => StartTime < 0 ? 0 : Time.time - StartTime;
        public static void End() { Active = false; }

        public static string Link(string me)
        {
            var baseUrl = Application.absoluteURL ?? "https://boyscount.github.io/cozy-fishing-unity/";
            int q = baseUrl.IndexOfAny(new[] { '?', '#' }); if (q >= 0) baseUrl = baseUrl.Substring(0, q);
            return $"{baseUrl}?duel={Seed}.{Stage}&vs={TotalKg.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}&by={Uri.EscapeDataString(string.IsNullOrEmpty(me) ? "เพื่อน" : me)}";
        }
    }
}
