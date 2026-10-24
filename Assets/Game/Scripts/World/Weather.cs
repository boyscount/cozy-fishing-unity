using UnityEngine;
using UnityEngine.Rendering;

namespace CozyFishing
{
    // สภาพอากาศ + เสียงบรรยากาศประจำด่าน (เสียงสังเคราะห์เองทั้งหมด ไม่มีไฟล์เสียง):
    //  · วันฝนพรำ (ดวง "bad"): สายฝนรอบกล้อง · วงน้ำกระเพื่อมเฉพาะบนผิวน้ำ · เสียงฝนวน · หมอกหนาขึ้น · ฝนหนักบางช่วงมีฟ้าแลบ-ฟ้าร้อง
    //    มองใต้น้ำ: ไม่เห็นสายฝน เสียงฝนอู้อี้เบาลง
    //  · ถ้ำ: ไม่มีฝน แต่มีหยดน้ำติ๋งก้องถ้ำ + ลมหวีดต่ำ ๆ และเสียงประกอบทุกเสียงก้อง (Sfx.Echo)
    //    WebGL ไม่รองรับ AudioReverbFilter จึงคำนวณเสียงก้องใส่ข้อมูลเสียงล่วงหน้าแทน
    //  · ทดสอบ: ?rain บังคับวันฝน
    public class Weather : MonoBehaviour
    {
        const int SR = 22050;
        IWorld world; Camera cam; bool cave;
        ParticleSystem drops, rings; Material dropMat, ringMat;
        AudioSource rainSrc, thunderSrc, dripSrc, windSrc;
        AudioClip thunderClip; AudioClip[] dripClips;
        Light flash; float flashT = -1, nextBolt, thunderAt = -1, nextDrip;
        float baseFogStart, baseFogEnd, ringAcc, k;
        System.Random rng = new System.Random(11);

        static bool inCave; static Weather current;
        public static bool Raining => !inCave && Story.CurrentLuck.rain;

        public void Init(IWorld w, Camera c)
        {
            current = this; world = w; cam = c; cave = w.StageId == "cave"; inCave = cave;
            Sfx.Echo = cave;
            if (GameUrl.Value.Contains("rain") && !GameUrl.Value.Contains("norain")) Story.S.luck = "bad";
            baseFogStart = RenderSettings.fogStartDistance; baseFogEnd = RenderSettings.fogEndDistance;
            if (cave) InitCave(); else InitRain();
        }

        void OnDestroy()
        {
            if (current != this) return;   // ด่านใหม่โหลดมาก่อนด่านเก่าถูกทำลาย → อย่าไปล้างค่าของด่านใหม่
            current = null; Sfx.Echo = false; inCave = false;
            RenderSettings.fogStartDistance = baseFogStart; RenderSettings.fogEndDistance = baseFogEnd;
        }

        // ================= ฝน =================
        void InitRain()
        {
            var sh = Shader.Find("CozyFishing/NightSky");
            if (sh == null) return;
            dropMat = new Material(sh) { mainTexture = StreakTex() };
            ringMat = new Material(sh) { mainTexture = RingTex() };
            // สายฝน: ตกจากกล่องเหนือกล้อง ยืดตามความเร็วเป็นเส้น เอียงตามลมนิด ๆ
            drops = new GameObject("RainDrops").AddComponent<ParticleSystem>();
            drops.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = drops.main; m.loop = true; m.playOnAwake = false; m.maxParticles = 4000; m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = 1.5f; m.startSpeed = new ParticleSystem.MinMaxCurve(15f, 19f); m.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.03f);
            m.startColor = new Color(0.72f, 0.77f, 0.84f, 0f);   // a = 0 → ไม่กระพริบ (ตามเชดเดอร์)
            var sh2 = drops.shape; sh2.shapeType = ParticleSystemShapeType.Box; sh2.scale = new Vector3(34, 34, 1); sh2.rotation = new Vector3(90, 0, 0);
            var em = drops.emission; em.rateOverTime = 0;
            var vel = drops.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(1.6f); vel.y = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(0.6f);
            var pr = drops.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = dropMat; pr.renderMode = ParticleSystemRenderMode.Stretch;
            pr.velocityScale = 0.045f; pr.lengthScale = 1f; pr.shadowCastingMode = ShadowCastingMode.Off; pr.receiveShadows = false;
            drops.Play();
            // วงน้ำ: แผ่นแบนบนผิวน้ำ ขยายแล้วจางหาย (ปล่อยเองทีละวงเฉพาะจุดที่เป็นน้ำ)
            rings = new GameObject("RainRings").AddComponent<ParticleSystem>();
            rings.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var rm = rings.main; rm.loop = true; rm.playOnAwake = false; rm.maxParticles = 900; rm.simulationSpace = ParticleSystemSimulationSpace.World;
            rm.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.85f); rm.startSpeed = 0; rm.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
            var rem = rings.emission; rem.rateOverTime = 0;
            var rsz = rings.sizeOverLifetime; rsz.enabled = true; rsz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.25f, 1, 1f));
            var rcol = rings.colorOverLifetime; rcol.enabled = true;
            rcol.color = new Gradient { colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.black, 1) }, alphaKeys = new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0, 1) } };
            var rr = rings.GetComponent<ParticleSystemRenderer>(); rr.sharedMaterial = ringMat; rr.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            rr.shadowCastingMode = ShadowCastingMode.Off; rr.receiveShadows = false;
            rings.Play();
            // เสียง
            rainSrc = gameObject.AddComponent<AudioSource>(); rainSrc.loop = true; rainSrc.playOnAwake = false; rainSrc.spatialBlend = 0; rainSrc.volume = 0;
            rainSrc.clip = RainLoop(); rainSrc.Play();
            thunderSrc = gameObject.AddComponent<AudioSource>(); thunderSrc.playOnAwake = false; thunderSrc.spatialBlend = 0;
            thunderClip = ThunderClip();
            // ฟ้าแลบ: แสงขาวอมฟ้าวาบทั้งฉาก
            flash = new GameObject("Lightning").AddComponent<Light>(); flash.type = LightType.Directional; flash.shadows = LightShadows.None;
            flash.color = new Color(0.78f, 0.85f, 1f); flash.intensity = 0; flash.transform.rotation = Quaternion.Euler(60, 30, 0);
            nextBolt = Time.time + 12 + (float)rng.NextDouble() * 20;
        }

        void Update()
        {
            if (cam == null) return;
            if (cave) { UpdateCave(); return; }
            if (drops == null) return;
            float target = Raining ? 0.75f + 0.25f * Mathf.Sin(Time.time * 0.07f) * Mathf.Sin(Time.time * 0.19f + 1.3f) : 0f;   // ฝนหนัก-เบาเป็นระลอก
            k = Mathf.MoveTowards(k, target, Time.deltaTime * 0.15f);
            var cp = cam.transform.position;
            bool under = cp.y < world.WaterY - 0.05f;
            // สายฝนตามกล้อง (ปล่อยมากน้อยตามความหนัก) · มืดค่ำสายฝนจางลงตามแสง
            drops.transform.position = cp + Vector3.up * 13f + cam.transform.forward * 6f - new Vector3(1.6f, 0, 0.6f) * 0.8f;
            var em = drops.emission; em.rateOverTime = under ? 0 : 3300f * k;
            float lit = Mathf.Lerp(0.35f, 1f, 1 - DayCycle.Night) + (flashT >= 0 ? 1.2f : 0);
            dropMat.SetFloat("_Fade", lit); ringMat.SetFloat("_Fade", lit * 0.8f);
            // วงน้ำบนผิวน้ำรอบจุดที่มอง
            if (!under && k > 0.02f)
            {
                var f = cam.transform.forward; f.y = 0; f = f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward;
                var center = cp + f * 9f;
                ringAcc += Time.deltaTime * 140f * k;
                var ep = new ParticleSystem.EmitParams();
                for (int tries = 0; ringAcc >= 1 && tries < 40; tries++)
                {
                    ringAcc -= 1;
                    float a = (float)rng.NextDouble() * Mathf.PI * 2, r = Mathf.Sqrt((float)rng.NextDouble()) * 13f;
                    float x = center.x + Mathf.Cos(a) * r, z = center.z + Mathf.Sin(a) * r;
                    if (world.DepthAt(x, z) < 0.05f) continue;
                    ep.position = new Vector3(x, world.WaterY + 0.03f, z); ep.applyShapeToPosition = false;
                    rings.Emit(ep, 1);
                }
            }
            // หมอกหนาขึ้นเมื่อฝนตก
            RenderSettings.fogStartDistance = baseFogStart * Mathf.Lerp(1f, 0.45f, k);
            RenderSettings.fogEndDistance = baseFogEnd * Mathf.Lerp(1f, 0.62f, k);
            // เสียงฝน (ใต้น้ำอู้อี้)
            rainSrc.volume = Mathf.Lerp(rainSrc.volume, k * (under ? 0.3f : 0.62f), Time.deltaTime * 2f);
            rainSrc.pitch = Mathf.Lerp(rainSrc.pitch, under ? 0.72f : 1f, Time.deltaTime * 3f);
            // ฟ้าแลบ → ฟ้าร้องตามมา (ไกลห่างกันตามระยะ)
            if (k > 0.6f && Time.time > nextBolt && flashT < 0)
            {
                flashT = 0; thunderAt = Time.time + 0.6f + (float)rng.NextDouble() * 2.2f;
                nextBolt = Time.time + 22 + (float)rng.NextDouble() * 40;
            }
            if (flashT >= 0)
            {
                flashT += Time.deltaTime;
                float fl = flashT < 0.08f ? 1 : flashT < 0.16f ? 0.25f : flashT < 0.26f ? 0.8f : Mathf.Max(0, 0.8f - (flashT - 0.26f) * 3f);
                flash.intensity = fl * (under ? 0.6f : 1.6f);
                if (flashT > 0.6f) { flashT = -1; flash.intensity = 0; }
            }
            if (thunderAt > 0 && Time.time >= thunderAt)
            {
                thunderAt = -1; thunderSrc.pitch = 0.85f + (float)rng.NextDouble() * 0.3f;
                thunderSrc.PlayOneShot(thunderClip, under ? 0.4f : 0.85f); Sfx.Buzz(160); CameraRig.Impulse(0.25f);
            }
        }

        // ================= ถ้ำ =================
        void InitCave()
        {
            dripSrc = gameObject.AddComponent<AudioSource>(); dripSrc.playOnAwake = false; dripSrc.spatialBlend = 0;
            dripClips = new AudioClip[4];
            for (int i = 0; i < dripClips.Length; i++)
            {
                float f0 = 1150 + i * 330, len = 0.25f; int n = (int)(SR * len); var d = new float[n];
                for (int s = 0; s < n; s++)
                {
                    float t = s / (float)SR;
                    d[s] = Mathf.Sin(2 * Mathf.PI * (f0 + 900 * Mathf.Exp(-t * 60)) * t) * Mathf.Exp(-t * 22) * Mathf.Min(1, t * 900) * 0.5f;   // ติ๋ง: เสียงสูงไถลลงสั้น ๆ
                }
                dripClips[i] = Sfx.Clip("drip", Sfx.Reverb(d, 1.25f, 0.6f));
            }
            windSrc = gameObject.AddComponent<AudioSource>(); windSrc.loop = true; windSrc.playOnAwake = false; windSrc.spatialBlend = 0; windSrc.volume = 0.22f;
            windSrc.clip = CaveWind(); windSrc.Play();
            nextDrip = Time.time + 1;
        }

        void UpdateCave()
        {
            if (dripSrc == null || Time.time < nextDrip) return;
            nextDrip = Time.time + 0.7f + (float)rng.NextDouble() * 2.8f;
            dripSrc.panStereo = (float)rng.NextDouble() * 1.6f - 0.8f; dripSrc.pitch = 0.9f + (float)rng.NextDouble() * 0.25f;
            dripSrc.PlayOneShot(dripClips[rng.Next(dripClips.Length)], 0.25f + (float)rng.NextDouble() * 0.35f);
        }

        // ================= สังเคราะห์เสียง/ภาพ =================
        // เสียงฝน 6 วิ วนต่อกันเนียน: เสียงซ่าทุ้ม + ซ่าแหลม + เม็ดฝนกระทบน้ำเป็นจุด ๆ
        AudioClip RainLoop()
        {
            float len = 6f, xf = 0.8f; int n = (int)(SR * len), nx = (int)(SR * xf), tot = n + nx;
            var d = new float[tot]; float lo = 0, lo2 = 0, hp = 0, prev = 0;
            for (int i = 0; i < tot; i++)
            {
                float w = (float)rng.NextDouble() * 2 - 1;
                lo += (w - lo) * 0.08f; lo2 += (lo - lo2) * 0.3f; hp = 0.92f * (hp + w - prev); prev = w;
                float t = i / (float)SR, swell = 0.85f + 0.15f * Mathf.Sin(t * 2.1f) * Mathf.Sin(t * 0.9f + 1);
                d[i] = (lo2 * 2.2f + hp * 0.16f) * swell;
            }
            for (int j = 0; j < 520; j++)   // เม็ดฝนใกล้ ๆ
            {
                int s0 = rng.Next(tot - 2000); float f0 = 1800 + (float)rng.NextDouble() * 3200, g = 0.04f + (float)rng.NextDouble() * 0.1f;
                for (int s = 0; s < 1600; s++) { float t = s / (float)SR; d[s0 + s] += Mathf.Sin(2 * Mathf.PI * f0 * t) * Mathf.Exp(-t * 180) * g; }
            }
            var o = new float[n];   // เอาหางทับหัวแบบค่อย ๆ เฟด → วนลูปไม่มีรอยต่อ
            for (int i = 0; i < n; i++) o[i] = d[i];
            for (int i = 0; i < nx; i++) { float a = i / (float)nx; o[i] = d[i] * a + d[n + i] * (1 - a); }
            Normalize(o, 0.8f);
            return Sfx.Clip("RainLoop", o);
        }

        // ฟ้าร้อง: เปรี้ยง! แล้วครืนยาวต่ำ ๆ
        AudioClip ThunderClip()
        {
            int n = SR * 5; var d = new float[n]; float a = 0, b = 0, c = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR, w = (float)rng.NextDouble() * 2 - 1;
                a += (w - a) * 0.02f; b += (a - b) * 0.05f; c += (w - c) * 0.35f;
                float roll = Mathf.Min(1, t * 6) * Mathf.Exp(-t * 0.75f) * (0.65f + 0.35f * Mathf.Sin(t * 5.3f) * Mathf.Sin(t * 1.7f + 2));
                float crack = c * Mathf.Exp(-t * 14) * 0.6f;
                d[i] = b * 9f * roll + crack;
            }
            Normalize(d, 0.95f);
            return Sfx.Clip("Thunder", d);
        }

        // ลมในถ้ำ: เสียงหวีดทุ้ม ๆ ขึ้นลงช้า ๆ (วนเนียน)
        AudioClip CaveWind()
        {
            float len = 8f, xf = 1f; int n = (int)(SR * len), nx = (int)(SR * xf), tot = n + nx;
            var d = new float[tot]; float lo = 0, lo2 = 0;
            for (int i = 0; i < tot; i++)
            {
                float t = i / (float)SR, w = (float)rng.NextDouble() * 2 - 1;
                lo += (w - lo) * 0.012f; lo2 += (lo - lo2) * 0.04f;
                d[i] = lo2 * (0.6f + 0.4f * Mathf.Sin(t * 0.8f) * Mathf.Sin(t * 0.33f + 0.7f)) + Mathf.Sin(2 * Mathf.PI * 62 * t) * 0.02f;
            }
            var o = new float[n];
            for (int i = 0; i < n; i++) o[i] = d[i];
            for (int i = 0; i < nx; i++) { float a = i / (float)nx; o[i] = d[i] * a + d[n + i] * (1 - a); }
            Normalize(o, 0.7f);
            return Sfx.Clip("CaveWind", o);
        }

        static void Normalize(float[] d, float peak)
        {
            float m = 0.0001f; foreach (var v in d) m = Mathf.Max(m, Mathf.Abs(v));
            float g = peak / m; for (int i = 0; i < d.Length; i++) d[i] *= g;
        }

        static Texture2D StreakTex()
        {
            var t = new Texture2D(8, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) for (int x = 0; x < 8; x++)
                {
                    float u = Mathf.Abs(x - 3.5f) / 3.5f, v = y / 63f;
                    float a = Mathf.Clamp01(1 - u * u) * Mathf.Sin(v * Mathf.PI) * (0.4f + 0.6f * v);
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            t.Apply(); return t;
        }

        static Texture2D RingTex()
        {
            const int S = 64; var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float dx = (x - S / 2 + 0.5f) / (S / 2f), dy = (y - S / 2 + 0.5f) / (S / 2f), r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Exp(-Mathf.Pow((r - 0.82f) / 0.07f, 2)) + Mathf.Exp(-Mathf.Pow((r - 0.55f) / 0.06f, 2)) * 0.35f;
                    t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(a) * 0.55f));
                }
            t.Apply(); return t;
        }
    }
}
