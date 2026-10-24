using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozyFishing
{
    // งานวัดยามค่ำ (ฉากจบ): ชิงช้าสวรรค์ไฟระยิบหมุนช้า ๆ · ไฟประดับหลากสีระหว่างซุ้ม · ไฟทองตามหลังคาโบสถ์ · พลุ · โคมลอย
    //   ทั้งหมดเป็นจุดไฟ (particle แบบ billboard) จึงเบามากและสวยตอนกลางคืน · พิกัดอิง center + frame (z = ทิศจากบ้านไปงานวัด)
    public class FairFx : MonoBehaviour
    {
        public const float WheelR = 9f, HubH = 12f;
        public static readonly Vector3 WheelLocal = new Vector3(-6, 0, 6);
        Vector3 center; Quaternion frame;
        ParticleSystem lights, sparks; ParticleSystem.Particle[] buf = new ParticleSystem.Particle[1600];
        float wheelA;
        readonly List<(Vector3 p, Color c, float s)> statics = new List<(Vector3, Color, float)>();
        readonly List<(Vector3 p, Vector3 v, float ph)> lanterns = new List<(Vector3, Vector3, float)>();
        readonly List<(float at, Vector3 from, Vector3 to, Color c, bool big)> rockets = new List<(float, Vector3, Vector3, Color, bool)>();
        AudioSource audioSrc; AudioClip boom, crackle;
        System.Random rng = new System.Random(21);
        float R() => (float)rng.NextDouble();

        public Vector3 W(Vector3 local) => center + frame * local;
        public Vector3 Hub => W(WheelLocal + Vector3.up * HubH);

        public void Init(Vector3 c, Quaternion f)
        {
            center = c; frame = f;
            var sh = Shader.Find("CozyFishing/NightSky");
            var glow = GlowTex();
            lights = MakePS("FairLights", new Material(sh) { mainTexture = glow }, false);
            sparks = MakePS("Fireworks", new Material(sh) { mainTexture = glow }, true);
            Color[] fair = { new Color(1f, 0.25f, 0.2f), new Color(1f, 0.8f, 0.2f), new Color(0.3f, 1f, 0.4f), new Color(0.35f, 0.55f, 1f), new Color(1f, 0.4f, 0.85f) };
            // ไฟประดับระหว่างเสา (ห้อยโค้งลง)
            var poles = new[] { new Vector3(4, 4.2f, -6), new Vector3(12, 4.6f, -3), new Vector3(20, 4.2f, 2), new Vector3(16, 4.4f, 12), new Vector3(6, 4.2f, 16), new Vector3(-2, 4.5f, -6) };
            for (int i = 0; i < poles.Length; i++)
            {
                Vector3 a = poles[i], b = poles[(i + 1) % poles.Length];
                for (int k = 0; k <= 22; k++) { float u = k / 22f; var p = Vector3.Lerp(a, b, u) + Vector3.down * Mathf.Sin(u * Mathf.PI) * 1.2f; statics.Add((p, fair[k % fair.Length] * 1.1f, 0.45f)); }
            }
            // โบสถ์: เส้นไฟทองตามหลังคาซ้อนสองชั้น + ช่อฟ้า
            var tc = new Vector3(13, 0, 22); var gold = new Color(1f, 0.75f, 0.32f);
            foreach (var (w, h0, h1) in new[] { (9f, 6f, 11f), (6.5f, 9.5f, 14.5f) })
                for (int k = 0; k <= 26; k++) { float u = k / 26f, x = Mathf.Lerp(-w, w, u), y = Mathf.Lerp(h0, h1, 1 - Mathf.Abs(u * 2 - 1)); statics.Add((tc + new Vector3(x, y, 0), gold, 0.42f)); }
            for (int k = 0; k < 14; k++) statics.Add((tc + new Vector3(Mathf.Lerp(-8.5f, 8.5f, k / 13f), 6f, 0), gold * 0.8f, 0.36f));
            statics.Add((tc + new Vector3(0, 15.6f, 0), gold * 1.3f, 0.8f));
            // ซุ้มร้านค้า: แสงส้มนวลบนพื้น
            for (int k = 0; k < 9; k++) statics.Add((new Vector3(-4 + k * 3.2f, 1.6f, -5 + Mathf.Sin(k) * 3), new Color(1f, 0.55f, 0.22f) * 0.55f, 3.2f));
            // เสียงพลุ
            audioSrc = gameObject.AddComponent<AudioSource>(); audioSrc.playOnAwake = false; audioSrc.spatialBlend = 0;
            boom = Synth(1.6f, (t, n) => (Mathf.Sin(2 * Mathf.PI * (70 - t * 25) * t) * Mathf.Exp(-t * 5) + n * Mathf.Exp(-t * 18) * 0.5f) * 0.9f);
            crackle = Synth(1.4f, (t, n) => (R() < 0.012f * Mathf.Exp(-t * 2) ? (R() * 2 - 1) : 0) + n * 0.04f * Mathf.Exp(-t * 3));
        }

        ParticleSystem MakePS(string name, Material m, bool fireworks)
        {
            var ps = new GameObject(name).AddComponent<ParticleSystem>(); ps.transform.SetParent(transform, false);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = 1600; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 0;
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            if (fireworks)
            {
                main.gravityModifier = 0.32f; main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
                var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.drag = 0.9f; lv.multiplyDragByParticleSize = false; lv.multiplyDragByParticleVelocity = true;
                var col = ps.colorOverLifetime; col.enabled = true;
                col.color = new Gradient { colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 0.5f), new GradientColorKey(Color.black, 1) }, alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) } };
                r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.06f; r.lengthScale = 1.5f;
            }
            ps.Play(); return ps;
        }

        /// พลุ 1 ลูก (เหตุการณ์จาก Timeline) · big = ลูกใหญ่ช่วงท้าย
        public void Firework(bool big)
        {
            var from = W(new Vector3(-10 + R() * 26, 1, 4 + R() * 16));
            var to = from + Vector3.up * (big ? 34 + R() * 8 : 24 + R() * 10) + Random.insideUnitSphere * 3;
            var palette = new[] { new Color(1f, 0.82f, 0.35f), new Color(1f, 0.3f, 0.25f), new Color(0.4f, 1f, 0.5f), new Color(0.45f, 0.65f, 1f), new Color(1f, 0.5f, 0.9f), Color.white };
            rockets.Add((Time.unscaledTime + 0.7f, from, to, palette[rng.Next(palette.Length)], big));
            // หางจรวดพุ่งขึ้น
            var ep = new ParticleSystem.EmitParams { position = from, velocity = (to - from) / 0.7f + Vector3.down * 0.5f, startSize = 0.35f, startLifetime = 0.7f, startColor = new Color(1f, 0.85f, 0.6f, 0f), applyShapeToPosition = false };
            sparks.Emit(ep, 1);
        }

        public void Lanterns()
        {
            for (int i = 0; i < 46; i++)
            {
                var p = W(new Vector3(-16 + R() * 34, 0.8f + R() * 2.5f, -18 + R() * 34));
                lanterns.Add((p, new Vector3(R() * 0.5f - 0.1f, 0.55f + R() * 0.55f, R() * 0.3f), 0.05f + R() * 0.9f));
            }
        }

        void Burst(Vector3 at, Color c, bool big)
        {
            int n = big ? 220 : 140; float sp = big ? 13f : 10f;
            var c2 = Color.Lerp(c, Color.white, 0.5f);
            for (int i = 0; i < n; i++)
            {
                var v = Random.onUnitSphere * sp * (0.85f + R() * 0.3f);
                var ep = new ParticleSystem.EmitParams { position = at, velocity = v, startSize = big ? 0.42f : 0.34f, startColor = new Color((i % 5 == 0 ? c2 : c).r * 1.4f, (i % 5 == 0 ? c2 : c).g * 1.4f, (i % 5 == 0 ? c2 : c).b * 1.4f, R() * 0.9f), applyShapeToPosition = false };
                sparks.Emit(ep, 1);
            }
            audioSrc.pitch = big ? 0.85f : 1f + R() * 0.15f;
            audioSrc.PlayOneShot(boom, big ? 0.9f : 0.65f); audioSrc.PlayOneShot(crackle, 0.35f);
            if (big) CameraRig.Impulse(0.15f);
        }

        void LateUpdate()
        {
            if (lights == null) return;
            float dt = Time.unscaledDeltaTime;
            for (int i = rockets.Count - 1; i >= 0; i--) if (Time.unscaledTime >= rockets[i].at) { Burst(rockets[i].to, rockets[i].c, rockets[i].big); rockets.RemoveAt(i); }
            wheelA += dt * 0.13f;
            int n = 0;
            void Add(Vector3 p, Color c, float size)
            {
                if (n >= buf.Length) return;
                ref var q = ref buf[n++];
                q.position = p; q.startColor = c; q.startSize = size; q.remainingLifetime = 10; q.startLifetime = 10; q.velocity = Vector3.zero;
            }
            foreach (var s in statics) Add(W(s.p), s.c, s.s);
            // ชิงช้าสวรรค์: ขอบวงล้อ + ซี่ล้อ + กระเช้า (ไฟส้มดวงใหญ่) + เสาค้ำสองขา
            var hub = WheelLocal + Vector3.up * HubH;
            for (int k = 0; k < 44; k++) { float a = wheelA + k * Mathf.PI * 2 / 44; Add(W(hub + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * WheelR), k % 2 == 0 ? new Color(1f, 0.85f, 0.45f, 0.3f) : new Color(1f, 0.35f, 0.55f, 0.7f), 0.5f); }
            for (int s = 0; s < 8; s++)
            {
                float a = wheelA + s * Mathf.PI / 4;
                for (int k = 1; k <= 6; k++) Add(W(hub + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * WheelR * k / 7f), new Color(0.5f, 0.75f, 1f) * 0.8f, 0.3f);
                Add(W(hub + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * WheelR + Vector3.down * 0.9f), new Color(1f, 0.6f, 0.25f), 0.9f);
            }
            Add(W(hub), new Color(1f, 0.9f, 0.6f), 1.1f);
            for (int k = 0; k <= 10; k++) { float u = k / 10f; Add(W(Vector3.Lerp(hub, WheelLocal + new Vector3(-5, 0, 0), u)), new Color(1f, 0.7f, 0.35f) * 0.6f, 0.28f); Add(W(Vector3.Lerp(hub, WheelLocal + new Vector3(5, 0, 0), u)), new Color(1f, 0.7f, 0.35f) * 0.6f, 0.28f); }
            // โคมลอย: ลอยขึ้นช้า ๆ ส่าย ๆ
            for (int i = 0; i < lanterns.Count; i++)
            {
                var l = lanterns[i]; l.p += (frame * l.v + new Vector3(Mathf.Sin(Time.unscaledTime * 0.7f + l.ph * 9) * 0.15f, 0, 0)) * dt; lanterns[i] = l;
                Add(l.p, new Color(1f, 0.6f, 0.25f, l.ph), 0.75f);
            }
            lights.SetParticles(buf, n);
        }

        AudioClip Synth(float len, System.Func<float, float, float> f)
        {
            const int SR = 22050; int cnt = (int)(SR * len); var d = new float[cnt];
            for (int i = 0; i < cnt; i++) d[i] = Mathf.Clamp(f(i / (float)SR, R() * 2 - 1), -1, 1);
            return Sfx.Clip("fw", d);
        }

        static Texture2D GlowTex()
        {
            const int S = 64; var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float dx = (x - S / 2 + 0.5f) / (S / 2f), dy = (y - S / 2 + 0.5f) / (S / 2f), r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(Mathf.Exp(-r * r * 9) * 1.2f + Mathf.Exp(-r * r * 2.2f) * 0.35f) * Mathf.Clamp01((1 - r) * 4);
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            t.Apply(); return t;
        }
    }
}
