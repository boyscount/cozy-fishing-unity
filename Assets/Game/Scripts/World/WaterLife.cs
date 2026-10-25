using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozyFishing
{
    // ของบนผิวน้ำให้เห็นว่า "ตรงนี้คือน้ำ" แม้ในที่มืด:
    //  · กอผักตบชวา/ใบไม้/กิ่งไม้ลอยน้ำ กระเพื่อมขึ้นลงตามคลื่น ลอยวนช้า ๆ (ใบมันวาว รับแสงตะเกียง/ไฟฉายเป็นประกาย)
    //  · มอสเรืองแสงตามขอบตลิ่ง (ด่านมืด) — สว่างขึ้นเมื่อมืด เป็นเส้นบอกขอบน้ำกับฝั่ง
    //  · หิ่งห้อยบินต่ำเหนือน้ำใกล้ฝั่งตอนมืด
    public class WaterLife : MonoBehaviour
    {
        IWorld world;
        readonly List<Transform> floats = new List<Transform>(); readonly List<Vector4> floatData = new List<Vector4>();   // x,z ฐาน · phase · ความเร็วลอยวน
        ParticleSystem moss, flies; Material mossMat, flyMat; bool darkStage, alwaysDark;
        static readonly string[] Hyacinth = { "bend", "houseboat", "headman", "temple", "marsh" };
        static readonly string[] Debris = { "bend", "mangrove", "houseboat", "cave", "headman" };
        static readonly string[] Dark = { "bend", "mangrove", "cave", "houseboat", "headman", "marsh" };

        public static void Build(IWorld w) => new GameObject("WaterLife").AddComponent<WaterLife>().Init(w);

        void Init(IWorld w)
        {
            world = w; string id = w.StageId;
            darkStage = System.Array.IndexOf(Dark, id) >= 0; alwaysDark = id == "cave" || id == "bend";
            var rng = new System.Random(id.GetHashCode());
            var shore = ShorePoints(rng);
            var surf = Resources.Load<Material>("Rod/Cork");
            if (surf != null)
            {
                Material Col(Color c, float sm) { var m = new Material(surf); m.SetColor("_BaseColor", c); if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", sm); return m; }
                bool hy = System.Array.IndexOf(Hyacinth, id) >= 0, de = System.Array.IndexOf(Debris, id) >= 0;
                if (hy)
                {
                    var leaf = Col(new Color(0.2f, 0.42f, 0.14f), 0.82f); var flower = Col(new Color(0.72f, 0.6f, 0.95f), 0.4f);
                    var mesh = HyacinthMesh(rng);
                    foreach (var p in FloatSpots(rng, shore, 14, 0.25f)) AddFloat(mesh, new[] { leaf, flower }, p, 0.8f + (float)rng.NextDouble() * 0.6f, rng);
                }
                if (de)
                {
                    var wood = Col(new Color(0.3f, 0.22f, 0.14f), 0.35f); var dead = Col(new Color(0.55f, 0.4f, 0.16f), 0.5f);
                    var twig = TwigMesh(); var lf = LeafMesh();
                    foreach (var p in FloatSpots(rng, shore, 10, 0.15f)) AddFloat(twig, new[] { wood }, p, 0.7f + (float)rng.NextDouble() * 0.9f, rng);
                    foreach (var p in FloatSpots(rng, shore, 22, 0.05f)) AddFloat(lf, new[] { dead }, p, 0.8f + (float)rng.NextDouble() * 0.7f, rng);
                }
            }
            var sh = Shader.Find("CozyFishing/NightSky");
            if (darkStage && sh != null && shore.Count > 0)
            {
                // มอสเรืองแสง: จุดนิ่งเรียงตามขอบน้ำ สีเขียวฟ้าสลัว ไม่กะพริบ (a = 0)
                mossMat = new Material(sh) { mainTexture = Dot() }; mossMat.SetFloat("_Twinkle", 0.6f);
                moss = MakePS("GlowMoss", mossMat, shore.Count * 3);
                var ep = new ParticleSystem.EmitParams { startLifetime = 1e6f, velocity = Vector3.zero, applyShapeToPosition = false };
                foreach (var s in shore)
                    for (int k = 0; k < 3; k++)
                    {
                        float a = (float)rng.NextDouble() * 6.28f, r = (float)rng.NextDouble() * 0.5f;
                        float x = s.x + Mathf.Cos(a) * r, z = s.z + Mathf.Sin(a) * r;
                        ep.position = new Vector3(x, Mathf.Max(world.GroundH(x, z), world.WaterY) + 0.04f, z);
                        ep.startSize = 0.06f + (float)rng.NextDouble() * 0.12f;
                        bool blue = rng.NextDouble() < 0.35;
                        ep.startColor = blue ? new Color(0.3f, 0.75f, 1f, 0.15f + (float)rng.NextDouble() * 0.2f) : new Color(0.45f, 1f, 0.55f, 0.15f + (float)rng.NextDouble() * 0.2f);
                        moss.Emit(ep, 1);
                    }
                // หิ่งห้อยต่ำ ๆ เหนือน้ำริมฝั่ง: ลอยวนช้า กะพริบ
                flyMat = new Material(sh) { mainTexture = Dot() }; flyMat.SetFloat("_Twinkle", 3.5f);
                flies = MakePS("WaterFireflies", flyMat, 160);
                var fm = flies.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f); fm.startSpeed = 0; fm.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
                var nz = flies.noise; nz.enabled = true; nz.strength = 0.35f; nz.frequency = 0.25f; nz.scrollSpeed = 0.15f;
                var col = flies.colorOverLifetime; col.enabled = true;
                var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(1, 0.85f), new GradientAlphaKey(0, 1) });
                col.color = g;
                shoreForFlies = shore;
            }
        }

        List<Vector3> shoreForFlies; float flyAcc;

        ParticleSystem MakePS(string name, Material m, int max)
        {
            var ps = new GameObject(name).AddComponent<ParticleSystem>(); ps.transform.SetParent(transform, false);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = max; main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSpeed = 0;
            var em = ps.emission; em.rateOverTime = 0;
            var sh = ps.shape; sh.enabled = false;
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        // ขอบน้ำ: จุดน้ำตื้นที่ติดฝั่ง ในพื้นที่กล้อง (ขยายออกนิดหน่อย)
        List<Vector3> ShorePoints(System.Random rng)
        {
            var b = world.CamBounds; var pts = new List<Vector3>();
            float step = 0.9f;
            for (float x = b.xMin - 6; x <= b.xMax + 6; x += step)
                for (float z = b.yMin - 6; z <= b.yMax + 6; z += step)
                {
                    float jx = x + ((float)rng.NextDouble() - 0.5f) * step, jz = z + ((float)rng.NextDouble() - 0.5f) * step;
                    float d = world.DepthAt(jx, jz);
                    if (d <= 0 || d > 0.12f) continue;
                    bool nearLand = world.DepthAt(jx + 0.6f, jz) <= 0 || world.DepthAt(jx - 0.6f, jz) <= 0 || world.DepthAt(jx, jz + 0.6f) <= 0 || world.DepthAt(jx, jz - 0.6f) <= 0;
                    if (nearLand) pts.Add(new Vector3(jx, world.WaterY, jz));
                }
            return pts;
        }

        // จุดลอยน้ำ: น้ำลึกพอ ค่อนไปทางริมฝั่ง (ผักตบชอบเกาะขอบ) ไม่ทับท่า/ตัวผู้เล่น
        List<Vector3> FloatSpots(System.Random rng, List<Vector3> shore, int n, float minDepth)
        {
            var res = new List<Vector3>(); var b = world.CamBounds; var spawn = world.Spawn;
            for (int tries = 0; tries < n * 40 && res.Count < n; tries++)
            {
                Vector3 p;
                if (shore.Count > 0 && rng.NextDouble() < 0.7) { var s = shore[rng.Next(shore.Count)]; float a = (float)rng.NextDouble() * 6.28f, r = 0.8f + (float)rng.NextDouble() * 2.5f; p = new Vector3(s.x + Mathf.Cos(a) * r, 0, s.z + Mathf.Sin(a) * r); }
                else p = new Vector3(Mathf.Lerp(b.xMin - 4, b.xMax + 4, (float)rng.NextDouble()), 0, Mathf.Lerp(b.yMin - 4, b.yMax + 4, (float)rng.NextDouble()));
                if (world.DepthAt(p.x, p.z) < minDepth || world.OnSolid(p.x, p.z)) continue;
                if (new Vector2(p.x - spawn.x, p.z - spawn.z).sqrMagnitude < 2.5f * 2.5f) continue;
                bool clash = false; foreach (var q in res) if ((q - p).sqrMagnitude < 0.8f) { clash = true; break; }
                if (clash) continue;
                res.Add(p);
            }
            return res;
        }

        void AddFloat(Mesh mesh, Material[] mats, Vector3 p, float scale, System.Random rng)
        {
            var go = new GameObject("Float"); go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterials = mats; r.shadowCastingMode = ShadowCastingMode.Off;
            go.transform.localScale = Vector3.one * scale;
            go.transform.position = new Vector3(p.x, world.WaterY, p.z);
            go.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0);
            floats.Add(go.transform); floatData.Add(new Vector4(p.x, p.z, (float)rng.NextDouble() * 6.28f, 0.03f + (float)rng.NextDouble() * 0.05f));
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < floats.Count; i++)
            {
                var d = floatData[i]; var tr = floats[i];
                // ลอยวนเป็นวงเล็ก ๆ ช้า ๆ + กระเพื่อมขึ้นลง + เอียงตามคลื่น
                float x = d.x + Mathf.Sin(t * d.w + d.z) * 0.35f, z = d.y + Mathf.Cos(t * d.w * 0.8f + d.z) * 0.35f;
                if (world.DepthAt(x, z) < 0.03f) { x = d.x; z = d.y; }
                float bob = Mathf.Sin(t * 1.3f + d.z) * 0.012f + Mathf.Sin(t * 2.1f + d.z * 2) * 0.006f;
                tr.position = new Vector3(x, world.WaterY + 0.005f + bob, z);
                var e = tr.eulerAngles;
                tr.rotation = Quaternion.Euler(Mathf.Sin(t * 1.1f + d.z) * 3f, e.y + Time.deltaTime * d.w * 25f, Mathf.Cos(t * 0.9f + d.z) * 3f);
            }
            if (moss == null) return;
            float dark = alwaysDark ? 1 : Mathf.Clamp01(DayCycle.Night * 1.2f + (darkStage ? 0.25f : 0));
            mossMat.SetFloat("_Fade", dark * 0.9f);
            flyMat.SetFloat("_Fade", Mathf.Clamp01(DayCycle.Night * 1.3f + (alwaysDark ? 0.5f : 0)));
            // ปล่อยหิ่งห้อยใกล้ ๆ กล้องตามขอบน้ำ
            flyAcc += Time.deltaTime * 14;
            var cam = Camera.main;
            while (flyAcc >= 1 && cam != null && shoreForFlies.Count > 0)
            {
                flyAcc -= 1;
                var s = shoreForFlies[Random.Range(0, shoreForFlies.Count)];
                if ((s - cam.transform.position).sqrMagnitude > 22 * 22) continue;
                var ep = new ParticleSystem.EmitParams { applyShapeToPosition = false, position = s + new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(0.15f, 1.1f), Random.Range(-1.5f, 1.5f)) };
                ep.startColor = new Color(0.85f, 1f, 0.45f, Random.Range(0.05f, 1f));
                flies.Emit(ep, 1);
            }
        }

        // ---------- รูปทรง ----------
        static void Disc(List<Vector3> v, List<int> tri, Vector3 c, Vector3 fwd, Vector3 up, float w, float l, int seg = 10)
        {
            // สองหน้า: หน้าหลังต้องมีจุดยอดของตัวเอง — ถ้าใช้จุดร่วมกัน RecalculateNormals หักล้างกันเป็นศูนย์
            // → แสงคำนวณได้ NaN บน WebGL แล้ว bloom ลามทั้งจอเป็นสีขาว
            var side = Vector3.Cross(up, fwd).normalized;
            for (int face = 0; face < 2; face++)
            {
                int b = v.Count; v.Add(c);
                for (int i = 0; i <= seg; i++)
                {
                    float a = i / (float)seg * Mathf.PI * 2;
                    v.Add(c + fwd * (Mathf.Cos(a) * l) + side * (Mathf.Sin(a) * w) + up * (Mathf.Cos(a) * 0.01f));
                }
                for (int i = 1; i <= seg; i++)
                    if (face == 0) { tri.Add(b); tri.Add(b + i); tri.Add(b + i + 1); }
                    else { tri.Add(b); tri.Add(b + i + 1); tri.Add(b + i); }
            }
        }

        static Mesh Finish(List<Vector3> v, params List<int>[] subs)
        {
            var m = new Mesh(); m.SetVertices(v); m.subMeshCount = subs.Length;
            for (int i = 0; i < subs.Length; i++) m.SetTriangles(subs[i], i);
            m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        // ผักตบชวา: ใบกลมมันวาวตั้งเฉียงรอบกอ 6–8 ใบ + ช่อดอกม่วงอ่อนตรงกลาง
        static Mesh HyacinthMesh(System.Random rng)
        {
            var v = new List<Vector3>(); var leaf = new List<int>(); var fl = new List<int>();
            int n = 7;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2 + (float)rng.NextDouble() * 0.4f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                float tilt = 0.6f + (float)rng.NextDouble() * 0.5f, s = 0.11f + (float)rng.NextDouble() * 0.05f;
                var fwd = (dir * Mathf.Cos(tilt) + Vector3.up * Mathf.Sin(tilt)).normalized;
                var up = Vector3.Cross(fwd, Vector3.Cross(Vector3.up, dir)).normalized;
                Disc(v, leaf, dir * 0.07f + fwd * s + Vector3.up * 0.02f, fwd, up, s * 0.85f, s);
            }
            for (int i = 0; i < 6; i++)
            {
                float y = 0.2f + i * 0.035f, a = i * 2.1f;
                Disc(v, fl, new Vector3(Mathf.Cos(a) * 0.025f, y, Mathf.Sin(a) * 0.025f), new Vector3(Mathf.Cos(a), 0.4f, Mathf.Sin(a)).normalized, Vector3.up, 0.025f, 0.03f, 6);
            }
            return Finish(v, leaf, fl);
        }

        static Mesh LeafMesh()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            Disc(v, t, Vector3.up * 0.005f, Vector3.forward, Vector3.up, 0.045f, 0.09f, 8);
            return Finish(v, t);
        }

        // กิ่งไม้ลอยน้ำ: แท่งยาวสี่เหลี่ยมเฉียง ๆ มีกิ่งแขนงหนึ่งกิ่ง
        static Mesh TwigMesh()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            void Bar(Vector3 a, Vector3 b, float r)
            {
                var d = (b - a).normalized; var s = Vector3.Cross(Vector3.up, d).normalized * r; var u = Vector3.up * r;
                int o = v.Count;
                v.AddRange(new[] { a - s, a + u, a + s, b - s, b + u, b + s });
                int[] q = { 0, 3, 4, 0, 4, 1, 1, 4, 5, 1, 5, 2, 2, 5, 3, 2, 3, 0 };
                foreach (var k in q) t.Add(o + k);
            }
            Bar(new Vector3(-0.45f, 0, 0), new Vector3(0.45f, 0, 0.05f), 0.025f);
            Bar(new Vector3(0.05f, 0, 0.02f), new Vector3(0.28f, 0, 0.22f), 0.014f);
            return Finish(v, t);
        }

        static Texture2D dot;
        static Texture2D Dot()
        {
            if (dot != null) return dot;
            int n = 32; dot = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f).magnitude / (n / 2f);
                    float a = Mathf.Clamp01(1 - d); a = a * a * (0.6f + 0.4f * a);
                    dot.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            dot.Apply(); return dot;
        }
    }
}
