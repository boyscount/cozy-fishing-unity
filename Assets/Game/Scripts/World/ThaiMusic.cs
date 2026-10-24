using UnityEngine;

namespace CozyFishing
{
    // ดนตรีไทยสังเคราะห์เอง (ไม่ใช้ไฟล์เสียง): ระนาดเอกตีทำนองบันไดเสียงไทย 5 เสียง (ทาง เพียงออบน) + ฉิ่งฉับคุมจังหวะ + ฆ้องวงต่ำ ๆ
    // seed ต่างกัน = ทำนองต่างกัน (แต่ละด่านมีเพลงประจำ)
    public static class ThaiMusic
    {
        public static AudioClip Ranat(int seed, float seconds = 13f, float bpm = 96)
        {
            const int SR = 22050; int n = (int)(SR * seconds);
            var d = new float[n]; var rnd = new System.Random(seed);
            // บันไดเสียงไทยแบบ 5 เสียง (ประมาณ) เริ่มที่ D4
            float[] scale = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21 };
            float baseF = 293.66f;
            float beat = 60f / bpm, eighth = beat / 2;
            int idx = 4;
            // ระนาด: ตีคู่แปด (เสียงต่ำ+สูง) เสียงไม้กระทบไม้ สลายเร็ว · ลูกเล่น "สะบัด" บางจังหวะ
            for (float t = 0.2f; t < seconds - 0.6f; t += eighth)
            {
                int step = rnd.Next(-2, 3); if (rnd.NextDouble() < 0.15) step *= 2;
                idx = Mathf.Clamp(idx + step, 0, scale.Length - 1);
                float f = baseF * Mathf.Pow(2, scale[idx] / 12f);
                float vel = 0.55f + (float)rnd.NextDouble() * 0.35f * (Mathf.Repeat(t, beat) < 0.01f ? 1.2f : 1f);
                Bar(d, SR, t, f, vel); Bar(d, SR, t, f * 2, vel * 0.45f);
                if (rnd.NextDouble() < 0.12) { Bar(d, SR, t + eighth * 0.33f, f * Mathf.Pow(2, 2 / 12f), vel * 0.6f); Bar(d, SR, t + eighth * 0.66f, f, vel * 0.6f); }   // สะบัด
            }
            // ฉิ่ง (ตีเปิด กังวาน) – ฉับ (ตีปิด สั้น) สลับทุกจังหวะ
            int k = 0;
            for (float t = 0.2f; t < seconds - 0.3f; t += beat, k++) Ching(d, SR, t, k % 2 == 1, rnd);
            // ฆ้องวงใหญ่ต้นห้อง (ทุก 4 จังหวะ)
            for (float t = 0.2f; t < seconds - 1f; t += beat * 4) Gong(d, SR, t, baseF / 2 * Mathf.Pow(2, scale[rnd.Next(0, 5)] / 12f));
            // จางเข้า-ออก และกันเสียงแตก
            float peak = 0.001f; for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(d[i]));
            for (int i = 0; i < n; i++)
            {
                float tt = i / (float)SR, env = Mathf.Clamp01(tt / 0.6f) * Mathf.Clamp01((seconds - tt) / 1.5f);
                d[i] = d[i] / peak * 0.8f * env;
            }
            var c = AudioClip.Create("Ranat" + seed, n, 1, SR, false); c.SetData(d, 0); return c;
        }

        static void Bar(float[] d, int sr, float at, float f, float vel)
        {
            int s0 = (int)(at * sr), len = (int)(0.9f * sr);
            for (int i = 0; i < len && s0 + i < d.Length; i++)
            {
                float t = i / (float)sr;
                // ไม้ระนาด: เสียงหลัก + โอเวอร์โทนไม่ประสาน (2.76, 5.4) สลายเร็ว · เสียงไม้กระทบตอนต้น
                float v = Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-t * 7f)
                        + 0.35f * Mathf.Sin(2 * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-t * 16f)
                        + 0.12f * Mathf.Sin(2 * Mathf.PI * f * 5.4f * t) * Mathf.Exp(-t * 30f);
                d[s0 + i] += v * vel * 0.32f;
            }
        }
        static void Ching(float[] d, int sr, float at, bool chap, System.Random rnd)
        {
            int s0 = (int)(at * sr), len = (int)((chap ? 0.12f : 1.4f) * sr);
            for (int i = 0; i < len && s0 + i < d.Length; i++)
            {
                float t = i / (float)sr, decay = chap ? Mathf.Exp(-t * 40) : Mathf.Exp(-t * 3.2f);
                float v = Mathf.Sin(2 * Mathf.PI * 2637 * t) * 0.5f + Mathf.Sin(2 * Mathf.PI * 3951 * t) * 0.35f + Mathf.Sin(2 * Mathf.PI * 5274 * t) * 0.2f
                        + ((float)rnd.NextDouble() * 2 - 1) * 0.25f * Mathf.Exp(-t * 60);
                d[s0 + i] += v * decay * (chap ? 0.16f : 0.11f);
            }
        }
        static void Gong(float[] d, int sr, float at, float f)
        {
            int s0 = (int)(at * sr), len = (int)(2.4f * sr);
            for (int i = 0; i < len && s0 + i < d.Length; i++)
            {
                float t = i / (float)sr;
                float v = Mathf.Sin(2 * Mathf.PI * f * t + Mathf.Sin(2 * Mathf.PI * 3.1f * t) * 0.3f) * Mathf.Exp(-t * 1.4f) * Mathf.Min(1, t * 60)
                        + 0.3f * Mathf.Sin(2 * Mathf.PI * f * 2.02f * t) * Mathf.Exp(-t * 2.5f);
                d[s0 + i] += v * 0.22f;
            }
        }
    }
}
