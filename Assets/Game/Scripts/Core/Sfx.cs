using System.Runtime.InteropServices;
using UnityEngine;

namespace CozyFishing
{
    // เสียงประกอบสังเคราะห์เอง (ไม่ต้องมีไฟล์เสียง) + สั่นมือถือ
    //   ทุ่นตกน้ำ · ปลาตอด · ตีเบ็ด · ปลาขึ้นน้ำ · สายขาด · เหรียญ (ซื้อของ)
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
            var c = AudioClip.Create("sfx", n, 1, SR, false); c.SetData(d, 0); return c;
        }
        static void Play(AudioClip c, float vol, float pitchJitter = 0.06f)
        {
            Ensure(); if (c == null) return;
            src.pitch = 1 + Random.Range(-pitchJitter, pitchJitter); src.PlayOneShot(c, vol);
        }

        public static void Plop() => Play(plop, 0.7f);
        public static void Nibble() { Play(nibble, 0.6f, 0.15f); }
        public static void Bite() { Play(strike, 0.5f); Buzz(70); }
        public static void Strike() { Play(strike, 0.8f); Buzz(120); }
        public static void Splash() { Play(splash, 0.8f); Buzz(40); }
        public static void Snap() { Play(snap, 0.8f); Buzz(220); }
        public static void Coin() => Play(coin, 0.6f, 0.02f);
        public static void GhostBuzz() => Buzz(320);
    }
}
