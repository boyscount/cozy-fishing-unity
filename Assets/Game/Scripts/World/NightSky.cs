using UnityEngine;

namespace CozyFishing
{
    // ฟ้ากลางคืนทุกด่าน: ดาวระยิบ + ทางช้างเผือก + พระจันทร์ดวงโตมีแสงฟุ้ง + แสงจันทร์ระยิบระยับเป็นทางบนผิวน้ำ
    // ค่อย ๆ จางเข้าตามความมืด (DayCycle.Night) · วันฟ้าครึ้ม/ฝนดาวน้อยลง · ดำน้ำ/ในถ้ำไม่แสดง
    public class NightSky : MonoBehaviour
    {
        const float R = 240f;   // รัศมีโดมฟ้า (กล้องมองไกลสุด 300)
        IWorld world; Camera cam;
        Transform dome, glitter;
        Material starMat, moonMat, glitMat;
        Renderer[] rends;
        bool cave;

        public void Init(IWorld w, Camera camera)
        {
            world = w; cam = camera;
            cave = w.StageId == "cave";
            var sh = Shader.Find("CozyFishing/NightSky");
            if (sh == null || cam == null) { enabled = false; return; }
            var dot = DotTex();
            starMat = new Material(sh) { mainTexture = dot }; starMat.SetFloat("_Twinkle", 2.2f);
            moonMat = new Material(sh) { mainTexture = MoonTex(), renderQueue = starMat.renderQueue + 1 };
            glitMat = new Material(sh) { mainTexture = dot }; glitMat.SetFloat("_Twinkle", 7f);

            var rnd = new System.Random(20261023);
            float yaw = (w.SpawnYaw + 28f) * Mathf.Deg2Rad, elev = 21f * Mathf.Deg2Rad;   // พระจันทร์อยู่ขวาหน้าจุดเกิด ต่ำพอให้เห็นในมุมกล้องปกติ
            var moonDir = new Vector3(Mathf.Sin(yaw) * Mathf.Cos(elev), Mathf.Sin(elev), Mathf.Cos(yaw) * Mathf.Cos(elev));

            // แสงจันทร์ส่องมาจากทิศพระจันทร์ (สูงกว่าดวงนิดให้เงาไม่ยาวเกิน) → ฉากย้อนแสงนวล ๆ
            DayCycle.MoonRot = Quaternion.LookRotation(-new Vector3(Mathf.Sin(yaw) * 0.75f, 0.66f, Mathf.Cos(yaw) * 0.75f));
            dome = new GameObject("NightSky").transform;
            var mb = new MeshBuilder();
            float Rn() => (float)rnd.NextDouble();
            float Gauss() { float u = Mathf.Max(1e-4f, Rn()), v = Rn(); return Mathf.Sqrt(-2 * Mathf.Log(u)) * Mathf.Cos(2 * Mathf.PI * v); }
            Color StarCol()
            {
                float k = Rn();
                return k < 0.6f ? Color.white : k < 0.82f ? new Color(0.75f, 0.85f, 1f) : new Color(1f, 0.86f, 0.68f);
            }
            // ดาวทั่วฟ้า
            for (int i = 0; i < 1700; i++)
            {
                float y = Mathf.Lerp(0.04f, 1f, Rn()), a = Rn() * Mathf.PI * 2, r = Mathf.Sqrt(1 - y * y);
                var d = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                float b = Mathf.Pow(Rn(), 3.2f);                                  // ดาวสว่างมีน้อย ดาวจาง ๆ มีมาก
                float horizon = Mathf.Clamp01(y * 5f);
                mb.Quad(d * R, d, 0.7f + b * 2.6f, StarCol() * (0.35f + b * 1.3f) * horizon, 0.02f + Rn());
            }
            // ทางช้างเผือก: แถบเฉียงพาดฟ้า ดาวหนาแน่น + หมอกแสงจาง ๆ ม่วงฟ้า
            var n = new Vector3(0.55f, 0.42f, 0.72f).normalized;
            var e1 = Vector3.Cross(n, Vector3.up).normalized; var e2 = Vector3.Cross(n, e1);
            for (int i = 0; i < 1100; i++)
            {
                float a = Rn() * Mathf.PI * 2;
                var d = (Mathf.Cos(a) * e1 + Mathf.Sin(a) * e2 + n * Gauss() * 0.07f).normalized;
                if (d.y < 0.05f) continue;
                float b = Mathf.Pow(Rn(), 4f);
                mb.Quad(d * R, d, 0.5f + b * 1.8f, StarCol() * (0.25f + b) * Mathf.Clamp01(d.y * 5f), 0.02f + Rn());
            }
            for (int i = 0; i < 160; i++)
            {
                float a = Rn() * Mathf.PI * 2;
                var d = (Mathf.Cos(a) * e1 + Mathf.Sin(a) * e2 + n * Gauss() * 0.045f).normalized;
                if (d.y < 0.06f) continue;
                var c = Color.Lerp(new Color(0.42f, 0.4f, 0.62f), new Color(0.55f, 0.5f, 0.45f), Rn()) * (0.05f + Rn() * 0.05f) * Mathf.Clamp01(d.y * 4f);
                mb.Quad(d * R, d, 14f + Rn() * 22f, c, 0);
            }
            var stars = Part("Stars", mb.Build(), starMat);
            // พระจันทร์ (ภาพรวมแสงฟุ้งรอบดวง)
            var mm = new MeshBuilder(); mm.Quad(moonDir * (R - 5), moonDir, 26f, Color.white, 0);
            var moon = Part("Moon", mm.Build(), moonMat);

            // แสงจันทร์บนผิวน้ำ: ประกายเล็ก ๆ เรียงเป็นทางจากใกล้ตัวไปหาพระจันทร์ กระพริบไม่พร้อมกัน
            glitter = new GameObject("MoonGlitter").transform;
            var gb = new MeshBuilder();
            var fwd = new Vector3(moonDir.x, 0, moonDir.z).normalized; var side = Vector3.Cross(Vector3.up, fwd);
            for (int i = 0; i < 420; i++)
            {
                float dist = 3f + Mathf.Pow(Rn(), 0.75f) * 120f;
                var p = fwd * dist + side * Gauss() * (0.35f + dist * 0.05f);
                float wdt = 0.12f + dist * 0.018f, len = 0.025f + dist * 0.0035f;
                var c = new Color(1f, 0.93f, 0.78f) * (0.35f + Rn() * 0.75f) * Mathf.Lerp(1f, 0.55f, dist / 120f);
                gb.Flat(p, side * wdt, fwd * len, c, 0.02f + Rn());
            }
            var glit = Part("Glitter", gb.Build(), glitMat); glit.transform.SetParent(glitter, false);
            if (cave) { moon.enabled = false; glit.enabled = false; }
            rends = new Renderer[] { stars, moon, glit };
            Show(false);
        }

        MeshRenderer Part(string name, Mesh m, Material mat)
        {
            var g = new GameObject(name); g.transform.SetParent(dome, false);
            g.AddComponent<MeshFilter>().sharedMesh = m;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return r;
        }

        bool shown = true;
        void Show(bool on)
        {
            if (shown == on) return; shown = on;
            foreach (var r in rends) if (r != null) r.gameObject.SetActive(on);
        }

        void LateUpdate()
        {
            if (cam == null) return;
            var cp = cam.transform.position;
            bool under = cp.y < world.WaterY - 0.05f;
            float night = DayCycle.Night;
            float stars = under ? 0 : night * DayCycle.Clear;
            float moon = under ? 0 : night * Mathf.Lerp(0.45f, 1f, DayCycle.Clear);   // เมฆบาง ๆ ยังเห็นพระจันทร์
            Show(night > 0.01f && !cave);
            if (!shown) return;
            starMat.SetFloat("_Fade", cave ? 0 : stars);
            moonMat.SetFloat("_Fade", moon);
            glitMat.SetFloat("_Fade", moon * 0.9f);
            dome.position = cp;   // อยู่ไกลสุดขอบฟ้าเสมอ (เดินไปไม่ถึง)
            glitter.position = new Vector3(cp.x, world.WaterY + 0.03f, cp.z);
        }

        // จุดแสงกลมฟุ้ง (ใช้กับดาวและประกายบนน้ำ)
        static Texture2D DotTex()
        {
            const int S = 64; var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S * 2 - 1, v = (y + 0.5f) / S * 2 - 1, r2 = u * u + v * v;
                float a = Mathf.Exp(-r2 * 18f) + Mathf.Exp(-r2 * 4f) * 0.25f;
                px[y * S + x] = new Color(1, 1, 1, Mathf.Clamp01(a * (1 - Mathf.Clamp01(r2))));
            }
            t.SetPixels(px); t.Apply(); return t;
        }

        // พระจันทร์เต็มดวง: ผิวมีทะเลจันทร์ (ลายเข้ม) ขอบมืดลงนิด + แสงฟุ้งสีฟ้านวลรอบดวง
        static Texture2D MoonTex()
        {
            const int S = 256; var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[S * S]; const float D = 0.3f;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S * 2 - 1, v = (y + 0.5f) / S * 2 - 1, r = Mathf.Sqrt(u * u + v * v);
                Color c;
                if (r < D)
                {
                    float z = Mathf.Sqrt(1 - (r / D) * (r / D));
                    float mare = Mathf.PerlinNoise(u * 6 + 3.1f, v * 6 + 7.7f) * 0.6f + Mathf.PerlinNoise(u * 15 + 1.3f, v * 15 + 2.9f) * 0.4f;
                    float shade = Mathf.Lerp(0.72f, 1f, Mathf.SmoothStep(0.62f, 0.38f, mare)) * (0.78f + 0.22f * z);
                    c = new Color(1f, 0.97f, 0.88f) * shade * 1.15f;
                    float edge = Mathf.Clamp01((D - r) * S * 0.5f); c = Color.Lerp(new Color(0.55f, 0.6f, 0.75f) * 0.5f, c, edge);   // ขอบเนียน
                }
                else
                {
                    float g = Mathf.Exp(-(r - D) * 9f) * 0.42f + Mathf.Exp(-(r - D) * 2.6f) * 0.12f;
                    c = new Color(0.72f, 0.8f, 1f) * g * (1 - Mathf.SmoothStep(0.85f, 1f, r));
                }
                c.a = 1; px[y * S + x] = c;
            }
            t.SetPixels(px); t.Apply(); return t;
        }

        // ตัวช่วยสร้างแผ่นภาพหลายแผ่นในเมชเดียว
        class MeshBuilder
        {
            readonly System.Collections.Generic.List<Vector3> v = new System.Collections.Generic.List<Vector3>();
            readonly System.Collections.Generic.List<Color> c = new System.Collections.Generic.List<Color>();
            readonly System.Collections.Generic.List<Vector2> uv = new System.Collections.Generic.List<Vector2>();
            readonly System.Collections.Generic.List<int> tri = new System.Collections.Generic.List<int>();
            /// แผ่นหันเข้าหาจุดศูนย์กลางโดม (ใช้กับของบนฟ้า)
            public void Quad(Vector3 p, Vector3 dir, float size, Color col, float phase)
            {
                var right = Vector3.Cross(Mathf.Abs(dir.y) > 0.98f ? Vector3.forward : Vector3.up, dir).normalized;
                var up = Vector3.Cross(dir, right);
                Flat(p, right * size * 0.5f, up * size * 0.5f, col, phase);
            }
            /// แผ่นตามแกนที่กำหนด (ครึ่งกว้าง a, ครึ่งสูง b)
            public void Flat(Vector3 p, Vector3 a, Vector3 b, Color col, float phase)
            {
                int i = v.Count; col.a = phase;
                v.Add(p - a - b); v.Add(p + a - b); v.Add(p + a + b); v.Add(p - a + b);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                for (int k = 0; k < 4; k++) c.Add(col);
                tri.Add(i); tri.Add(i + 2); tri.Add(i + 1); tri.Add(i); tri.Add(i + 3); tri.Add(i + 2);
            }
            public Mesh Build()
            {
                var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                m.SetVertices(v); m.SetColors(c); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateBounds();
                return m;
            }
        }
    }
}
