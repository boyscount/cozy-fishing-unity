using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozyFishing
{
    // กอกล้วยตานี (ด่านบ่อทุ่งนา) — บ้านของ "นางตานีใต้กอกล้วย" อยู่ริมฝั่งตรงข้ามบ่อ มองเห็นได้ขณะตกปลา
    //   สร้างจากโค้ดทั้งหมด: ลำต้นเทียมเรียวมีกาบ · ใบใหญ่โค้งลู่ลง ฉีกเป็นริ้วตามธรรมชาติ (ลายใบ+รอยฉีกวาดเป็นภาพแล้วตัดขอบด้วย alpha)
    //   ใบแห้งสีน้ำตาลห้อยแนบลำต้น · เครือกล้วยดิบห้อยหัวปลีสีม่วงแดง · หน่อเล็ก ๆ รอบกอ · ไหวตามลมด้วยเชดเดอร์ใบไม้ของเกม
    //   GhostSpot = จุดที่ผีตานีโผล่ (หน้ากอกล้วย หันหาบ่อ)
    public static class BananaGrove
    {
        public static bool HasSpot; public static Vector3 GhostSpot;
        static Texture2D leafTex, dryTex;

        public static void Build(IWorld w)
        {
            HasSpot = false;
            var pw = Object.FindAnyObjectByType<PaddyWorld>();
            var leafBase = Resources.Load<Material>("Materials/BananaLeaf"); var surf = Resources.Load<Material>("Rod/Cork");
            if (leafBase == null || surf == null) return;
            if (leafTex == null) { leafTex = LeafTex(false); dryTex = LeafTex(true); }
            var leaf = new Material(leafBase) { mainTexture = leafTex }; leaf.SetTexture("_BaseMap", leafTex);
            var dry = new Material(leafBase); dry.SetTexture("_BaseMap", dryTex); dry.SetFloat("_Translucency", 0.4f);
            Material Col(Color c, float sm = 0.25f) { var m = new Material(surf); m.SetColor("_BaseColor", c); if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", sm); return m; }
            var stem = Col(new Color(0.44f, 0.52f, 0.27f)); var stemOld = Col(new Color(0.42f, 0.36f, 0.22f));
            var fruit = Col(new Color(0.38f, 0.56f, 0.16f), 0.45f); var heart = Col(new Color(0.42f, 0.1f, 0.16f), 0.4f);

            // หาริมฝั่งตรงข้ามบ่อ (เดินจากกลางบ่อออกไปจนเจอดินแห้งที่ไม่ใช่นาข้าว) · หลบสิ่งของเดิมในฉาก
            var pond = new Vector2(PaddyWorld.Pond.x, PaddyWorld.Pond.y);
            Vector3 center = Vector3.zero; bool found = false;
            foreach (var deg in new[] { 90f, 78f, 102f, 66f, 114f, 54f, 126f })
            {
                var d2 = new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
                for (float r = 8; r < 18; r += 0.25f)
                {
                    var p = pond + d2 * r;
                    if (w.GroundH(p.x, p.y) > w.WaterY + 0.12f && PaddyWorld.Paddy(p.x, p.y) < 0.3f)
                    {
                        var c = pond + d2 * (r + 1.3f);
                        bool clear = true;
                        if (pw != null) foreach (var col in pw.colliders) if ((new Vector2(col.x, col.y) - c).sqrMagnitude < (col.z + 2.2f) * (col.z + 2.2f)) { clear = false; break; }
                        if (clear && PaddyWorld.Paddy(c.x, c.y) < 0.6f) { center = new Vector3(c.x, 0, c.y); found = true; }
                        break;
                    }
                }
                if (found) break;
            }
            if (!found) return;
            var toPond = new Vector3(pond.x - center.x, 0, pond.y - center.z).normalized;
            var root = new GameObject("BananaGrove").transform;
            var rnd = new System.Random(77);
            float R() => (float)rnd.NextDouble();
            // (ระยะจากกลางกอ, มุม, ความสูง, มีเครือ)
            var plants = new List<(float r, float a, float h, bool bunch)>
            {
                (0.2f, 0f, 3.2f, true), (1.3f, 60f, 2.8f, false), (1.4f, 170f, 3.0f, true), (1.5f, 280f, 2.6f, false),
                (2.3f, 120f, 1.9f, false), (2.2f, 220f, 1.6f, false), (2.5f, 330f, 1.3f, false), (0.9f, 250f, 0.9f, false), (2.6f, 30f, 0.8f, false),
            };
            foreach (var (pr, pa, ph, bunch) in plants)
            {
                var off = Quaternion.Euler(0, pa + R() * 20, 0) * Vector3.forward * pr;
                var pos = center + off; pos.y = w.GroundH(pos.x, pos.z) - 0.05f;
                var go = new GameObject("Banana"); go.transform.SetParent(root, false); go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(0, R() * 360, 0);
                Plant(go, ph, bunch, rnd, leaf, dry, stem, stemOld, fruit, heart);
                if (pw != null && ph > 1.5f) pw.colliders.Add(new Vector4(pos.x, pos.z, 0.35f, 3f));
            }
            GhostSpot = center + toPond * 1.6f; GhostSpot.y = w.GroundH(GhostSpot.x, GhostSpot.z); HasSpot = true;
        }

        static void Plant(GameObject go, float h, bool bunch, System.Random rnd, Material leaf, Material dry, Material stem, Material stemOld, Material fruit, Material heart)
        {
            float R() => (float)rnd.NextDouble();
            float stemH = h * 0.62f, r0 = 0.07f + h * 0.04f;
            var lean = new Vector3((R() - 0.5f) * 0.12f, 1, (R() - 0.5f) * 0.12f).normalized;
            var top = lean * stemH;
            Part(go, "Stem", StemMesh(stemH, r0, lean), h > 2.5f && R() < 0.4f ? stemOld : stem, false);
            // ใบสด: แผ่ออกรอบยอด ใบอ่อนตั้งชัน ใบแก่ลู่ลง · ใบแห้งห้อยแนบลำต้น
            var lm = new List<CombineInstance>(); var dm = new List<CombineInstance>();
            int n = h < 1.2f ? 4 : 6 + rnd.Next(3);
            for (int i = 0; i < n; i++)
            {
                float yaw = i * 360f / n + R() * 30, age = i / (float)Mathf.Max(1, n - 1);
                float L = h * (0.6f + R() * 0.15f) * (h < 1.2f ? 0.9f : 0.75f), Wd = L * 0.24f;
                float rise = Mathf.Lerp(1.1f, 0.25f, age);   // ใบอ่อนชี้ขึ้น ใบแก่ลู่
                float u0 = (rnd.Next(4)) * 0.25f;            // ลายใบคนละช่องในภาพ (รอยฉีกต่างกัน)
                lm.Add(new CombineInstance { mesh = LeafMesh(L, Wd, rise, 0.9f + age * 0.6f, u0), transform = Matrix4x4.TRS(top, Quaternion.Euler(0, yaw, 0), Vector3.one) });
            }
            if (h > 1.5f)
                for (int i = 0; i < 2 + rnd.Next(2); i++)
                {
                    float yaw = R() * 360, L = h * 0.5f;
                    dm.Add(new CombineInstance { mesh = LeafMesh(L, L * 0.2f, -0.2f, 3.2f, rnd.Next(4) * 0.25f), transform = Matrix4x4.TRS(top * (0.75f + R() * 0.15f), Quaternion.Euler(0, yaw, 0), Vector3.one) });
                }
            Part(go, "Leaves", Combine(lm), leaf, true);
            if (dm.Count > 0) Part(go, "DryLeaves", Combine(dm), dry, true);
            if (!bunch) return;
            // เครือกล้วย: ก้านโค้งห้อยจากยอด · หวีกล้วยดิบเรียงลง · หัวปลีปลายเครือ
            var fm = new List<CombineInstance>(); var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere); var sph = tmp.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(tmp);
            var dir = Quaternion.Euler(0, R() * 360, 0) * Vector3.forward;
            Vector3 Stalk(float u) => top + dir * (0.25f + 0.35f * Mathf.Sin(u * 1.6f)) + Vector3.down * (u * 1.25f - 0.1f);
            for (int k = 0; k < 10; k++) { var a = Stalk(k / 10f); var b = Stalk((k + 1) / 10f); fm.Add(new CombineInstance { mesh = sph, transform = Matrix4x4.TRS((a + b) / 2, Quaternion.FromToRotation(Vector3.up, b - a), new Vector3(0.05f, (b - a).magnitude * 0.6f, 0.05f)) }); }
            for (int hand = 0; hand < 5; hand++)
            {
                float u = 0.25f + hand * 0.1f; var c = Stalk(u);
                for (int f = 0; f < 9; f++)
                {
                    float ang = f * 360f / 9 + hand * 17;
                    var o = Quaternion.Euler(0, ang, 0) * Vector3.forward;
                    var p = c + o * 0.11f + Vector3.up * 0.06f;
                    var rot = Quaternion.LookRotation(o) * Quaternion.Euler(-60, 0, 0);   // ผลกล้วยงอนขึ้น
                    fm.Add(new CombineInstance { mesh = sph, transform = Matrix4x4.TRS(p, rot, new Vector3(0.045f, 0.045f, 0.17f)) });
                }
            }
            Part(go, "Bunch", Combine(fm, true), fruit, false);
            var hp = Stalk(1.02f);
            var hm = new List<CombineInstance> { new CombineInstance { mesh = sph, transform = Matrix4x4.TRS(hp, Quaternion.FromToRotation(Vector3.up, Stalk(1.02f) - Stalk(0.9f)), new Vector3(0.13f, 0.24f, 0.13f)) } };
            Part(go, "Heart", Combine(hm, true), heart, false);
        }

        static void Part(GameObject parent, string name, Mesh m, Material mat, bool leaves)
        {
            var g = new GameObject(name); g.transform.SetParent(parent.transform, false);
            g.AddComponent<MeshFilter>().sharedMesh = m;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = leaves ? ShadowCastingMode.TwoSided : ShadowCastingMode.On;
        }

        static Mesh Combine(List<CombineInstance> parts, bool recalc = false)
        {
            var m = new Mesh { name = "BananaPart" }; m.indexFormat = IndexFormat.UInt32; m.CombineMeshes(parts.ToArray(), true, true);
            if (recalc) m.RecalculateNormals();
            m.RecalculateBounds(); return m;
        }

        // ลำต้นเทียม: ทรงกระบอกเรียว โคนป่อง มีร่องกาบตามยาว
        static Mesh StemMesh(float H, float r0, Vector3 lean)
        {
            const int S = 12, N = 10; var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int i = 0; i <= N; i++)
            {
                float u = i / (float)N, r = r0 * (1.25f - 0.55f * u) * (1 + 0.25f * Mathf.Exp(-u * 12));
                var c = lean * H * u;
                for (int s = 0; s <= S; s++)
                {
                    float a = s * Mathf.PI * 2 / S, groove = 1 - 0.06f * Mathf.Abs(Mathf.Sin(a * 3));
                    v.Add(c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r * groove); uv.Add(new Vector2(s / (float)S, u));
                }
            }
            for (int i = 0; i < N; i++) for (int s = 0; s < S; s++)
                {
                    int a = i * (S + 1) + s, b = a + S + 1;
                    tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
            var m = new Mesh(); m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        // ใบกล้วย: ก้านใบ → แผ่นใบยาวพับเป็นร่องตามเส้นกลางใบ · rise > 0 ชี้ขึ้นก่อนลู่ · droop = ความงอปลาย · u0 = ช่องลายในภาพ (กว้าง 0.25)
        static Mesh LeafMesh(float L, float W, float rise, float droop, float u0)
        {
            const int N = 12; var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int i = 0; i <= N; i++)
            {
                float u = i / (float)N;
                var c = new Vector3(0, L * (rise * u - droop * 0.45f * u * u), L * u * 0.95f);
                float wv = u < 0.16f ? W * 0.12f : W;
                float fold = W * 0.18f;   // ใบพับเป็นตัววี
                v.Add(c + new Vector3(-wv, -fold, 0)); v.Add(c); v.Add(c + new Vector3(wv, -fold, 0));
                uv.Add(new Vector2(u0, u)); uv.Add(new Vector2(u0 + 0.125f, u)); uv.Add(new Vector2(u0 + 0.25f, u));
            }
            for (int i = 0; i < N; i++) for (int k = 0; k < 2; k++)
                {
                    int a = i * 3 + k, b = a + 3;
                    tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
            var m = new Mesh(); m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        // ภาพลายใบ 4 แบบเรียงกันตามแนวนอน: รูปใบรีปลายมน · เส้นกลางใบสีอ่อน · เส้นใบขนานเฉียง · รอยฉีกเป็นริ้วจากขอบเข้าหาเส้นกลาง
        static Texture2D LeafTex(bool dry)
        {
            const int Wd = 256, Ht = 256; var t = new Texture2D(Wd, Ht, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var rnd = new System.Random(dry ? 5 : 3);
            var px = new Color[Wd * Ht];
            for (int cell = 0; cell < 4; cell++)
            {
                // รอยฉีก: แต่ละช่องมีจำนวนต่างกัน (ช่อง 0 ใบสวย → ช่อง 3 ฉีกมาก)
                var tears = new List<(float v, float side, float depth)>();
                int nt = dry ? 10 : cell * 4 + 1;
                for (int k = 0; k < nt; k++) tears.Add((0.22f + (float)rnd.NextDouble() * 0.74f, rnd.Next(2) == 0 ? -1 : 1, 0.35f + (float)rnd.NextDouble() * 0.6f));
                for (int y = 0; y < Ht; y++)
                {
                    float v = y / (float)(Ht - 1);
                    float half = v < 0.16f ? 0.06f : Mathf.Sqrt(Mathf.Clamp01(Mathf.Sin(Mathf.Clamp01((v - 0.14f) / 0.86f) * Mathf.PI * 0.92f + 0.12f))) * (v > 0.9f ? Mathf.Lerp(1, 0.55f, (v - 0.9f) / 0.1f) : 1);
                    for (int x = 0; x < Wd / 4; x++)
                    {
                        float s = (x / (float)(Wd / 4 - 1)) * 2 - 1, as_ = Mathf.Abs(s);
                        float a = as_ < half ? 1 : 0;
                        foreach (var (tv, side, depth) in tears)   // ริ้วฉีกตามแนวเส้นใบ (เฉียงออกจากเส้นกลาง)
                            if (Mathf.Sign(s) == side && as_ > 1 - depth && Mathf.Abs(v - (tv + as_ * 0.05f)) < 0.006f) a = 0;
                        float vein = 0.5f + 0.5f * Mathf.Sin((v * 90 - as_ * 9) * Mathf.PI);
                        Color c = dry ? Color.Lerp(new Color(0.47f, 0.35f, 0.2f), new Color(0.62f, 0.5f, 0.3f), vein * 0.4f + (float)rnd.NextDouble() * 0.1f)
                                      : Color.Lerp(new Color(0.22f, 0.42f, 0.11f), new Color(0.32f, 0.55f, 0.17f), vein * 0.35f + 0.15f * Mathf.Sin(v * 7 + cell));
                        if (as_ < 0.05f) c = dry ? new Color(0.6f, 0.5f, 0.32f) : new Color(0.62f, 0.72f, 0.36f);            // เส้นกลางใบ
                        if (!dry && as_ > half - 0.06f && as_ < half) c = Color.Lerp(c, new Color(0.55f, 0.5f, 0.2f), 0.35f);  // ขอบใบเหลืองนิด ๆ
                        if (v < 0.16f) c = dry ? new Color(0.45f, 0.34f, 0.2f) : new Color(0.5f, 0.6f, 0.28f);              // ก้านใบ
                        c.a = a; px[y * Wd + cell * (Wd / 4) + x] = c;
                    }
                }
            }
            t.SetPixels(px); t.Apply(true); return t;
        }
    }
}
