using System.Runtime.InteropServices;
using UnityEngine;

namespace CozyFishing
{
    // เสียงประกอบสังเคราะห์เอง (ไม่ต้องมีไฟล์เสียง) + สั่นมือถือ
    //   ทุ่นตกน้ำ · ปลาตอด · ตีเบ็ด · ปลาขึ้นน้ำ · สายขาด · เหรียญ (ซื้อของ)
    //   ในถ้ำ (Echo) ทุกเสียงก้อง: WebGL ไม่มีฟิลเตอร์เสียง จึงคำนวณเสียงก้องใส่ข้อมูลเสียงไว้ล่วงหน้า (ทำครั้งแรกที่ใช้ แล้วเก็บไว้)
    public static class Sfx
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CozyVibrate(int ms);
#else
        static void CozyVibrate(int ms) { }
#endif
        public static void Buzz(int ms) { try { CozyVibrate(ms); } catch (System.Exception) { } }

        static AudioSource src;
        static AudioClip plop, nibble, strike, splash, snap, coin;
        static readonly System.Collections.Generic.Dictionary<AudioClip, float[]> dry = new System.Collections.Generic.Dictionary<AudioClip, float[]>();
        static readonly System.Collections.Generic.Dictionary<AudioClip, AudioClip> echoed = new System.Collections.Generic.Dictionary<AudioClip, AudioClip>();
        public static bool Echo;   // ตั้งโดย Weather ตามด่าน (ถ้ำ = true)
        const int SR = 22050;

        static void Ensure()
        {
            if (src != null) return;
            var go = new GameObject("Sfx"); Object.DontDestroyOnLoad(go);
            src = go.AddComponent<AudioSource>(); src.playOnAwake = false; src.spatialBlend = 0;
            plop = Make(0.35f, t => (Mathf.Sin(2 * Mathf.PI * (520 - t * 900) * t) * Mathf.Exp(-t * 14) + Noise() * Mathf.Exp(-t * 40) * 0.4f) * 0.5f);
            nibble = Make(0.12f, t => Mathf.Sin(2 * Mathf.PI * 820 * t) * Mathf.Exp(-t * 45) * 0.35f);
            strike = Make(0.3f, t => (Mathf.Sin(2 * Mathf.PI * (140 - t * 200) * t) * Mathf.Exp(-t * 12) + Noise() * Mathf.Exp(-t * 30) * 0.3f) * 0.6f);
            splash = Make(0.9f, t => Noise() * Mathf.Exp(-t * 4.5f) * (0.6f + 0.4f * Mathf.Sin(t * 60)) * 0.45f);
            snap = Make(0.5f, t => (Mathf.Sin(2 * Mathf.PI * (1800 - t * 3000) * t) * Mathf.Exp(-t * 9) + Noise() * Mathf.Exp(-t * 60)) * 0.35f);
            coin = Make(0.5f, t => (Mathf.Sin(2 * Mathf.PI * 1320 * t) + 0.6f * Mathf.Sin(2 * Mathf.PI * 1980 * t) * (t > 0.08f ? 1 : 0)) * Mathf.Exp(-t * 7) * 0.3f);
        }
        static System.Random rng = new System.Random(3);
        static float Noise() => (float)rng.NextDouble() * 2 - 1;
        static AudioClip Make(float len, System.Func<float, float> f)
        {
            int n = (int)(SR * len); var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = f(i / (float)SR);
            var c = Clip("sfx", d); dry[c] = d; return c;
        }
        public static AudioClip Clip(string name, float[] d)
        {
            var c = AudioClip.Create(name, d.Length, 1, SR, false); c.SetData(d, 0); return c;
        }

        // เสียงก้องถ้ำ: เสียงสะท้อนผนังชัด ๆ 2 ครั้ง + หางก้องแบบ Schroeder (comb 4 ตัวมีหน่วงเสียงแหลม + allpass 2 ตัว)
        //   size = ขนาดถ้ำ (คูณระยะหน่วง) · wet = ความดังของเสียงก้อง · หางยาวขึ้น ~2 วิ
        public static float[] Reverb(float[] d, float size = 1.6f, float wet = 0.5f)
        {
            int tail = (int)(SR * 2.0f * Mathf.Clamp(size, 0.5f, 2f)), n = d.Length + tail;
            var x = new float[n]; System.Array.Copy(d, x, d.Length);
            var y = new float[n];
            foreach (var (ms, g) in new[] { (170f, 0.32f), (340f, 0.16f) })   // เสียงสะท้อนผนังถ้ำ
            {
                int dl = (int)(SR * ms * size / 1000f);
                for (int i = dl; i < n; i++) y[i] += x[i - dl] * g;
            }
            var wetBuf = new float[n];
            foreach (var ms in new[] { 29.7f, 37.1f, 41.1f, 43.7f })
            {
                int dl = (int)(SR * ms * size / 1000f); var buf = new float[dl]; int k = 0; float lp = 0;
                for (int i = 0; i < n; i++)
                {
                    float o = buf[k]; lp = o * 0.6f + lp * 0.4f;   // ผนังหินดูดเสียงแหลมไปบ้าง
                    buf[k] = x[i] + lp * 0.8f; k = (k + 1) % dl; wetBuf[i] += o * 0.25f;
                }
            }
            foreach (var ms in new[] { 5f, 1.7f })
            {
                int dl = Mathf.Max(1, (int)(SR * ms / 1000f)); var buf = new float[dl]; int k = 0;
                for (int i = 0; i < n; i++) { float b = buf[k], v = wetBuf[i] + b * 0.7f; buf[k] = v; k = (k + 1) % dl; wetBuf[i] = b - v * 0.7f; }
            }
            float pk = 0.0001f, pkIn = 0.0001f;
            foreach (var v in d) pkIn = Mathf.Max(pkIn, Mathf.Abs(v));
            for (int i = 0; i < n; i++) { y[i] += x[i] + wetBuf[i] * wet; pk = Mathf.Max(pk, Mathf.Abs(y[i])); }
            float gain = Mathf.Min(1f, pkIn * 1.1f / pk); for (int i = 0; i < n; i++) y[i] *= gain;
            return y;
        }
        public static AudioClip Echoed(AudioClip c)
        {
            if (c == null || !dry.TryGetValue(c, out var d)) return c;
            if (!echoed.TryGetValue(c, out var e)) { e = Clip(c.name + "_echo", Reverb(d)); echoed[c] = e; }
            return e;
        }
        static void Play(AudioClip c, float vol, float pitchJitter = 0.06f)
        {
            Ensure(); if (c == null) return;
            if (Echo) c = Echoed(c);
            src.pitch = 1 + Random.Range(-pitchJitter, pitchJitter); src.PlayOneShot(c, vol);
        }

        public static void Plop() => Play(plop, 0.7f);
        public static void Nibble() { Play(nibble, 0.6f, 0.15f); }
        public static void Bite() { Play(strike, 0.5f); Buzz(70); CameraRig.Impulse(0.35f); }
        public static void Strike() { Play(strike, 0.8f); Buzz(120); CameraRig.Impulse(0.55f); }
        public static void Splash() { Play(splash, 0.8f); Buzz(40); }
        public static void Snap() { Play(snap, 0.8f); Buzz(220); CameraRig.Impulse(0.7f); }
        public static void Coin() => Play(coin, 0.6f, 0.02f);
        public static void GhostBuzz() => Buzz(320);
    }
}
