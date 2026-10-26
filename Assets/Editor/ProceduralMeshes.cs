// โมเดลที่ Poly Haven ไม่มี สร้างด้วยโค้ดให้ดูสมจริง: ต้นตาลโตนด (ใบพัดจีบ) · ใบบัว (มีรอยแหว่ง เส้นใบ ขอบงอน) · ดอกบัว (กลีบซ้อน 2 ชั้น)
// สีจุดยอดเป็นค่า linear ใช้กับ CozyFishing/Surface + Use Vertex Color
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class ProceduralMeshes
{
    class Builder
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Color> c = new List<Color>();
        public readonly List<List<int>> subs = new List<List<int>>();
        public Builder(int submeshes) { for (int i = 0; i < submeshes; i++) subs.Add(new List<int>()); }
        public int Add(Vector3 p, Color col) { v.Add(p); c.Add(col.linear); return v.Count - 1; }
        public void Tri(int sub, int a, int b, int d) { subs[sub].AddRange(new[] { a, b, d }); }
        public void Quad(int sub, int a, int b, int d, int e) { Tri(sub, a, b, d); Tri(sub, b, e, d); }
        // สามเหลี่ยมสองหน้า: แยกจุดยอดแต่ละหน้า ไม่งั้น normal หักล้างกันเป็นศูนย์ (ดำสนิท)
        public void TwoSided(int sub, Vector3 a, Vector3 b, Vector3 d, Color col)
        {
            Tri(sub, Add(a, col), Add(b, col), Add(d, col));
            Tri(sub, Add(a, col), Add(d, col), Add(b, col));
        }
        public Mesh Build(string name)
        {
            var m = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetColors(c); m.subMeshCount = subs.Count;
            for (int i = 0; i < subs.Count; i++) m.SetTriangles(subs[i], i);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }

    // ต้นตาลโตนด: ลำต้นตรงสูงชะลูดผิวขรุขระ + ทรงพุ่มกลมของใบพัด (palmate) จีบเป็นริ้ว ปลายแตกเป็นแฉก
    // submesh 0 = ลำต้น/ก้านใบ, 1 = ใบ
    public static Mesh SugarPalm(int seed, float height)
    {
        var rnd = new System.Random(seed);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var bld = new Builder(2);
        // ---- ลำต้น ----
        const int Sides = 10, Rings = 14;
        var lean = new Vector3(R(-0.25f, 0.25f), 0, R(-0.25f, 0.25f));
        Vector3 Axis(float t) => Vector3.up * height * t + lean * t * t;
        int[,] ring = new int[Rings + 1, Sides + 1];
        for (int j = 0; j <= Rings; j++)
        {
            float t = j / (float)Rings;
            float r = Mathf.Lerp(0.3f, 0.2f, t) + (t < 0.08f ? (0.08f - t) * 2.2f : 0) + 0.015f * Mathf.Sin(t * 60);   // โคนบาน + วงแหวนรอยใบเก่า
            for (int i = 0; i <= Sides; i++)
            {
                float a = i / (float)Sides * Mathf.PI * 2;
                var col = Color.Lerp(new Color(0.32f, 0.28f, 0.24f), new Color(0.42f, 0.36f, 0.30f), Mathf.PerlinNoise(i * 0.7f, j * 0.9f));
                ring[j, i] = bld.Add(Axis(t) + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r, col);
            }
        }
        for (int j = 0; j < Rings; j++) for (int i = 0; i < Sides; i++) bld.Quad(0, ring[j, i], ring[j + 1, i], ring[j, i + 1], ring[j + 1, i + 1]);
        var top = Axis(1);
        // ---- ใบพัด ----
        int fronds = 24;
        for (int f = 0; f < fronds; f++)
        {
            float az = f * 2.39996f + R(-0.2f, 0.2f);                  // มุมทองคำ กระจายรอบต้น
            float elev = Mathf.Lerp(70, -45, f / (float)(fronds - 1)) * Mathf.Deg2Rad + R(-0.12f, 0.12f);
            var dir = new Vector3(Mathf.Cos(az) * Mathf.Cos(elev), Mathf.Sin(elev), Mathf.Sin(az) * Mathf.Cos(elev)).normalized;
            float age = Mathf.InverseLerp(fronds * 0.6f, fronds, f);   // ใบล่างแก่ อมเหลือง/น้ำตาล
            // ก้านใบ (4 เหลี่ยมบาง ๆ)
            float petLen = R(1.0f, 1.4f);
            var side = Vector3.Cross(dir, Vector3.up).normalized; if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            var upv = Vector3.Cross(side, dir).normalized;
            var p0 = top + dir * 0.1f; var p1 = p0 + dir * petLen - Vector3.up * petLen * 0.12f * (1 - dir.y);
            var stalk = new Color(0.36f, 0.42f, 0.2f);
            int[] s0 = new int[4], s1 = new int[4];
            for (int k = 0; k < 4; k++)
            {
                float a = k * Mathf.PI / 2; var o = (side * Mathf.Cos(a) + upv * Mathf.Sin(a)) * 0.03f;
                s0[k] = bld.Add(p0 + o, stalk); s1[k] = bld.Add(p1 + o * 0.6f, stalk);
            }
            for (int k = 0; k < 4; k++) bld.Quad(0, s0[k], s1[k], s0[(k + 1) % 4], s1[(k + 1) % 4]);
            // แผ่นใบ: รังสี K เส้นกาง ~210° รอบปลายก้าน แต่ละริ้ว (ระหว่างรังสี) มีจุดยอดของตัวเองให้เห็นรอยจีบชัด
            const int K = 36; float span = 210 * Mathf.Deg2Rad, Rf = R(1.1f, 1.45f);
            var fwd = (dir + Vector3.up * 0.25f).normalized;
            var rgt = Vector3.Cross(fwd, upv).normalized; var nrm = Vector3.Cross(rgt, fwd).normalized;
            float[] radii = { 0.08f, 0.45f, 0.78f, 1f };
            Vector3 RayPt(int k, float rr, bool tipNotch)
            {
                float a = -span / 2 + span * k / K;
                var d = fwd * Mathf.Cos(a) + rgt * Mathf.Sin(a);
                float droop = rr * rr * 0.35f * Rf * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(a)));
                return p1 + d * rr * Rf - Vector3.up * droop;
            }
            var inner = Color.Lerp(new Color(0.20f, 0.33f, 0.12f), new Color(0.38f, 0.36f, 0.16f), age);
            var outer = Color.Lerp(new Color(0.34f, 0.47f, 0.17f), new Color(0.50f, 0.40f, 0.20f), age);
            for (int k = 0; k < K; k++)
            {
                float shade = R(0.88f, 1.08f);
                var idx = new int[radii.Length, 3];
                for (int j = 0; j < radii.Length; j++)
                {
                    float rr = radii[j];
                    var col = Color.Lerp(inner, outer, rr) * shade;
                    var a = RayPt(k, rr, false); var b = RayPt(k + 1, rr, false);
                    if (j == radii.Length - 1)   // ปลายริ้วแตกเป็นแฉก: ดึงขอบเข้าหากลางริ้ว + ยื่นปลายไม่เท่ากัน
                    {
                        var mid = (a + b) / 2; float ext = R(0.0f, 0.18f) * Rf;
                        var outDir = (mid - p1).normalized;
                        a = Vector3.Lerp(a, mid, 0.35f) + outDir * ext; b = Vector3.Lerp(b, mid, 0.35f) + outDir * ext;
                    }
                    // สันกลางริ้วยกขึ้น (ใบพัดตาลพับเป็นจีบรูปตัว V) สีสว่างกว่าขอบเล็กน้อย
                    var ridge = (a + b) / 2 + nrm * 0.045f * rr * Rf;
                    idx[j, 0] = bld.Add(a, col * 0.9f); idx[j, 1] = bld.Add(ridge, col * 1.08f); idx[j, 2] = bld.Add(b, col * 0.9f);
                }
                for (int j = 0; j < radii.Length - 1; j++)
                {
                    bld.Quad(1, idx[j, 0], idx[j + 1, 0], idx[j, 1], idx[j + 1, 1]);
                    bld.Quad(1, idx[j, 1], idx[j + 1, 1], idx[j, 2], idx[j + 1, 2]);
                }
            }
        }
        return bld.Build("SugarPalm");
    }

    // ใบบัว: วงกลมมีรอยแหว่งถึงกลางใบ · เส้นใบรัศมี · ขอบงอนขึ้นเล็กน้อย · กลางใบบุ๋ม
    public static Mesh LotusPad(int seed)
    {
        var rnd = new System.Random(seed);
        var bld = new Builder(1);
        const int Seg = 36, Rings = 4;
        float notch = 14 * Mathf.Deg2Rad, wob = (float)rnd.NextDouble() * 6;
        var center = bld.Add(new Vector3(0, -0.01f, 0), new Color(0.55f, 0.62f, 0.32f));
        var idx = new int[Rings + 1, Seg + 1];
        for (int j = 1; j <= Rings; j++)
            for (int i = 0; i <= Seg; i++)
            {
                float a = notch + (Mathf.PI * 2 - notch * 2) * i / Seg;
                float r = j / (float)Rings * (1 + 0.04f * Mathf.Sin(a * 5 + wob));
                float vein = Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 11)), 24) * 0.12f * (1 - r * 0.5f);
                var col = Color.Lerp(new Color(0.36f, 0.50f, 0.20f), new Color(0.20f, 0.36f, 0.12f), r * r) * (1 + vein);
                if (j == Rings) col = Color.Lerp(col, new Color(0.45f, 0.30f, 0.18f), 0.25f);   // ขอบใบอมแดงน้ำตาล
                float y = 0.06f * Mathf.Pow(r, 6) - 0.01f * (1 - r);
                idx[j, i] = bld.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r), col);
            }
        for (int i = 0; i < Seg; i++)
        {
            bld.Tri(0, center, idx[1, i + 1], idx[1, i]);
            for (int j = 1; j < Rings; j++) bld.Quad(0, idx[j, i], idx[j + 1, i], idx[j, i + 1], idx[j + 1, i + 1]);
        }
        // ด้านใต้ไม่ต้องมี (มองจากด้านบนเสมอ) — กลับ winding ให้หันขึ้น
        var m = bld.Build("LotusPad");
        var tris = m.triangles; for (int t = 0; t < tris.Length; t += 3) (tris[t + 1], tris[t + 2]) = (tris[t + 2], tris[t + 1]);
        m.triangles = tris; m.RecalculateNormals();
        return m;
    }

    // ดอกบัว: กลีบโค้งเป็นแอ่ง 2 ชั้น (ชั้นในตั้งกว่า) ชมพูปลายกลีบ ขาวโคนกลีบ + ฝักกลางสีเหลือง
    /// โมเดลที่ปั้นใน Blender (สีที่จุดยอด) ถ้ามี — คืนสำเนา mesh ให้บันทึกเป็น asset ของด่านได้
    static Mesh FromBlender(string file)
    {
        var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Game/Models/Nature/{file}.glb");
        var mf = go != null ? go.GetComponentInChildren<MeshFilter>() : null;
        if (mf == null || mf.sharedMesh == null || mf.sharedMesh.colors.Length == 0) return null;
        var m = Object.Instantiate(mf.sharedMesh); m.name = file; return m;
    }

    public static Mesh LotusFlower(int seed, bool bud)
    {
        var fromBlender = FromBlender(bud ? "lotus_bud" : "lotus_flower");   // ดอกบัวปั้นใน Blender: กลีบ 3 ชั้นทรงถ้วย ฝักบัว + เกสร
        if (fromBlender != null) return fromBlender;
        var rnd = new System.Random(seed);
        var bld = new Builder(1);
        void Petal(float az, float tilt, float len, float wid)
        {
            const int U = 4, V = 5;
            var idx = new int[U + 1, V + 1];
            var d = new Vector3(Mathf.Cos(az), 0, Mathf.Sin(az)); var s = new Vector3(-d.z, 0, d.x);
            for (int vv = 0; vv <= V; vv++)
                for (int u = 0; u <= U; u++)
                {
                    float t = vv / (float)V, x = (u / (float)U - 0.5f) * 2;
                    float w = wid * Mathf.Sin(Mathf.PI * Mathf.Lerp(0.15f, 1, t)) * (1 - t * 0.35f);   // กลีบรูปไข่ ปลายแหลม
                    float tl = tilt + t * 0.35f;                                                       // ปลายกลีบบานออก
                    var along = d * Mathf.Sin(tl) + Vector3.up * Mathf.Cos(tl);
                    var p = along * len * t + s * x * w + d * (-x * x * w * 0.5f);                    // แอ่งกลีบ (cup)
                    var col = Color.Lerp(new Color(0.98f, 0.93f, 0.90f), new Color(0.93f, 0.42f, 0.62f), Mathf.Pow(t, 1.4f) * (1 - 0.3f * x * x));
                    idx[u, vv] = bld.Add(p, col);
                }
            for (int vv = 0; vv < V; vv++) for (int u = 0; u < U; u++) bld.Quad(0, idx[u, vv], idx[u, vv + 1], idx[u + 1, vv], idx[u + 1, vv + 1]);   // วาดสองหน้าด้วย Cull Off
        }
        float o = (float)rnd.NextDouble();
        if (bud) { for (int i = 0; i < 5; i++) Petal(i * 1.2566f + o, 0.12f, 1f, 0.3f); }
        else
        {
            for (int i = 0; i < 8; i++) Petal(i * 0.785f + o, 0.95f, 0.9f, 0.32f);
            for (int i = 0; i < 6; i++) Petal(i * 1.047f + o + 0.3f, 0.45f, 0.85f, 0.3f);
            // ฝักบัว
            int c = bld.Add(new Vector3(0, 0.22f, 0), new Color(0.85f, 0.78f, 0.25f));
            var rim = new int[9];
            for (int i = 0; i <= 8; i++) { float a = i * Mathf.PI / 4; rim[i] = bld.Add(new Vector3(Mathf.Cos(a) * 0.14f, 0.18f, Mathf.Sin(a) * 0.14f), new Color(0.78f, 0.70f, 0.2f)); }
            for (int i = 0; i < 8; i++) bld.Tri(0, c, rim[i + 1], rim[i]);
        }
        return bld.Build(bud ? "LotusBud" : "LotusFlower");
    }

    // กอไผ่: ลำไผ่ 9–13 ลำ โค้งออกจากกอ มีข้อปล้อง (สีเข้มกว่า) · ใบเรียวแหลมห้อยเป็นพุ่มช่วงบน
    // submesh 0 = ลำไผ่, 1 = ใบ
    public static Mesh Bamboo(int seed, float height)
    {
        var rnd = new System.Random(seed);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var bld = new Builder(2);
        int culms = rnd.Next(9, 14);
        for (int c = 0; c < culms; c++)
        {
            float az = R(0, Mathf.PI * 2), r0 = R(0, 0.45f), h = height * R(0.7f, 1.05f), lean = R(0.12f, 0.4f), rad = R(0.035f, 0.06f);
            var dir = new Vector3(Mathf.Cos(az), 0, Mathf.Sin(az));
            var basePos = dir * r0;
            Vector3 P(float t) => basePos + dir * (lean * h * t * t) + Vector3.up * (h * t - lean * h * t * t * 0.25f);
            const int Seg = 14, Sides = 6;
            var prev = new int[Sides + 1];
            for (int sgi = 0; sgi <= Seg; sgi++)
            {
                float t = sgi / (float)Seg;
                var p = P(t);
                bool node = sgi % 2 == 0;                                  // ข้อปล้องทุก 2 ช่วง
                float rr = rad * (1 - t * 0.45f) * (node ? 1.12f : 1f);
                var col = Color.Lerp(new Color(0.42f, 0.5f, 0.2f), new Color(0.62f, 0.62f, 0.3f), t) * (node ? 0.72f : 1f);
                var cur = new int[Sides + 1];
                for (int k = 0; k <= Sides; k++) { float a = k / (float)Sides * Mathf.PI * 2; cur[k] = bld.Add(p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * rr, col); }
                if (sgi > 0) for (int k = 0; k < Sides; k++) bld.Quad(0, prev[k], cur[k], prev[k + 1], cur[k + 1]);
                prev = cur;
            }
            // ใบ: กลุ่มใบตามข้อช่วงบน ห้อยลงเล็กน้อย
            int leaves = rnd.Next(26, 40);
            for (int l = 0; l < leaves; l++)
            {
                float t = R(0.45f, 1f);
                var p = P(t);
                float la = R(0, Mathf.PI * 2), len = R(0.22f, 0.38f), w = len * 0.13f;
                var ld = new Vector3(Mathf.Cos(la), -R(0.15f, 0.7f), Mathf.Sin(la)).normalized;
                var side = Vector3.Cross(ld, Vector3.up).normalized * w;
                var colA = Color.Lerp(new Color(0.18f, 0.36f, 0.1f), new Color(0.35f, 0.52f, 0.16f), R(0, 1));
                var colB = colA * 1.25f;
                int a0 = bld.Add(p, colA), a1 = bld.Add(p + ld * len * 0.5f + side, colB), a2 = bld.Add(p + ld * len, colB), a3 = bld.Add(p + ld * len * 0.5f - side, colB);
                bld.Tri(1, a0, a1, a2); bld.Tri(1, a0, a2, a3);
            }
        }
        return bld.Build("Bamboo");
    }

    // ทรงกลึง (เจดีย์ บัวหัวเสา): หมุนเส้นโปรไฟล์ (รัศมี, สูง) จากล่างขึ้นบนรอบแกน Y · segs = 4 ได้ฐานสี่เหลี่ยม
    public static Mesh Lathe(Vector2[] profile, int segs, Color col, float twist = 0)
    {
        var bld = new Builder(1);
        for (int j = 0; j < profile.Length; j++)
            for (int i = 0; i <= segs; i++)
            {
                float a = i / (float)segs * Mathf.PI * 2 + twist;
                bld.Add(new Vector3(Mathf.Cos(a) * profile[j].x, profile[j].y, Mathf.Sin(a) * profile[j].x), col);
            }
        for (int j = 0; j < profile.Length - 1; j++)
            for (int i = 0; i < segs; i++)
            {
                int a0 = j * (segs + 1) + i, b0 = a0 + segs + 1;
                bld.Quad(0, a0, b0, a0 + 1, b0 + 1);
            }
        return bld.Build("Lathe");
    }

    // หลังคาจั่วทรงไทย 1 ตับ: สันตามแกน X ยาว length · ลาดลงสองข้างถึงชายคา ±halfW · สูงจากชายคาถึงสัน rise
    // ลาดแอ่นเว้าแบบหลังคาโบสถ์ + ปลายชายคางอนขึ้น · มีแผ่นใต้หลังคา · submesh 0 = กระเบื้อง, 1 = ใบระกาไม้ปิดทอง (ฟันเลื่อยตามขอบจั่ว)
    // pediment = true ใส่หน้าบันสามเหลี่ยมปิดหัวท้าย (submesh 1)
    public static Mesh ThaiRoof(float length, float halfW, float rise, bool pediment)
    {
        var bld = new Builder(2);
        const int N = 10;
        float L = length * 0.5f;
        float Y(float t) => rise * (1 - t) - rise * 0.16f * Mathf.Sin(t * Mathf.PI) + (t > 0.85f ? (t - 0.85f) * rise * 0.5f : 0);   // เว้า + งอนชายคา
        Color tile = Color.white, wood = Color.white;
        for (int side = -1; side <= 1; side += 2)
        {
            int[] top = new int[(N + 1) * 2], bot = new int[(N + 1) * 2];
            for (int k = 0; k <= N; k++)
            {
                float t = k / (float)N, z = side * halfW * t, y = Y(t);
                top[k * 2] = bld.Add(new Vector3(-L, y, z), tile); top[k * 2 + 1] = bld.Add(new Vector3(L, y, z), tile);
                bot[k * 2] = bld.Add(new Vector3(-L, y - 0.09f, z), tile); bot[k * 2 + 1] = bld.Add(new Vector3(L, y - 0.09f, z), tile);
            }
            for (int k = 0; k < N; k++)
            {
                int a = k * 2, b = (k + 1) * 2;
                if (side > 0) { bld.Quad(0, top[a], top[b], top[a + 1], top[b + 1]); bld.Quad(0, bot[a + 1], bot[b + 1], bot[a], bot[b]); }
                else { bld.Quad(0, top[a + 1], top[b + 1], top[a], top[b]); bld.Quad(0, bot[a], bot[b], bot[a + 1], bot[b + 1]); }
            }
            // ใบระกา: แผ่นฟันเลื่อยตั้งบนขอบจั่วทั้งสองปลาย (สองหน้า)
            for (int e = -1; e <= 1; e += 2)
            {
                float x = e * (L + 0.04f);
                for (int k = 0; k < N; k++)
                {
                    float t0 = k / (float)N, t1 = (k + 1) / (float)N, z0 = side * halfW * t0, z1 = side * halfW * t1;
                    float y0 = Y(t0), y1 = Y(t1), zm = (z0 + z1) * 0.5f, ym = (y0 + y1) * 0.5f + 0.32f;
                    Vector3 p0 = new Vector3(x, y0 - 0.06f, z0), p1 = new Vector3(x, y1 - 0.06f, z1), q0 = new Vector3(x, y0 + 0.1f, z0), q1 = new Vector3(x, y1 + 0.1f, z1), tip = new Vector3(x, ym, zm);
                    bld.TwoSided(1, p0, q0, p1, wood); bld.TwoSided(1, p1, q0, q1, wood); bld.TwoSided(1, q0, tip, q1, wood);
                }
            }
        }
        if (pediment)
            for (int e = -1; e <= 1; e += 2)
            {
                float x = e * (L - 0.05f);
                var c = new Vector3(x, Y(0.82f) - 0.1f, 0);
                for (int k = 0; k < N; k++)
                {
                    float t0 = k / (float)N, t1 = (k + 1) / (float)N;
                    for (int sd = -1; sd <= 1; sd += 2)
                        bld.TwoSided(1, c, new Vector3(x, Y(t0) - 0.1f, sd * halfW * t0 * 0.82f), new Vector3(x, Y(t1) - 0.1f, sd * halfW * t1 * 0.82f), wood);
                }
            }
        return bld.Build("ThaiRoof");
    }

    // ช่อฟ้า / หางหงส์: ท่อเรียวโค้งขึ้นแล้วงอปลายออกด้านนอก (+X) เหมือนจะงอยปากนก
    public static Mesh Chofa(float size)
    {
        var bld = new Builder(1);
        const int Seg = 16, Sides = 6;
        Vector3 P(float s) => new Vector3(0.18f * s + 0.32f * Mathf.Sin(s * Mathf.PI * 0.75f) * s, 0.95f * s - 0.3f * s * s * s, 0) * size;
        var prev = new int[Sides + 1];
        for (int i = 0; i <= Seg; i++)
        {
            float s = i / (float)Seg, r = size * 0.075f * (1 - s * 0.85f);
            var p = P(s); var tng = (P(Mathf.Min(1, s + 0.02f)) - P(Mathf.Max(0, s - 0.02f))).normalized;
            var n1 = Vector3.Cross(tng, Vector3.forward).normalized;
            var cur = new int[Sides + 1];
            for (int k = 0; k <= Sides; k++) { float a = k / (float)Sides * Mathf.PI * 2; cur[k] = bld.Add(p + (n1 * Mathf.Cos(a) + Vector3.forward * Mathf.Sin(a)) * r, Color.white); }
            if (i > 0) for (int k = 0; k < Sides; k++) bld.Quad(0, prev[k], cur[k], prev[k + 1], cur[k + 1]);
            prev = cur;
        }
        return bld.Build("Chofa");
    }

    // กอกก/ธูปฤาษีริมหนอง: ใบเรียวยาวโค้งออกจากโคน หลายสิบใบ บางต้นมีฝักธูปฤาษีสีน้ำตาลบนก้านตรง
    public static Mesh Reeds(int seed, float height)
    {
        var rnd = new System.Random(seed);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var bld = new Builder(1);
        int blades = rnd.Next(26, 40);
        for (int b = 0; b < blades; b++)
        {
            float az = R(0, Mathf.PI * 2), h = height * R(0.55f, 1.05f), bend = R(0.15f, 0.55f), w = R(0.018f, 0.035f);
            var dir = new Vector3(Mathf.Cos(az), 0, Mathf.Sin(az)); var side = new Vector3(-dir.z, 0, dir.x);
            var root = dir * R(0, 0.25f);
            var c0 = Color.Lerp(new Color(0.3f, 0.36f, 0.16f), new Color(0.45f, 0.5f, 0.22f), R(0, 1));
            var c1 = rnd.NextDouble() < 0.3 ? new Color(0.62f, 0.55f, 0.32f) : c0 * 1.35f;   // ปลายใบแห้งบ้าง
            const int Seg = 7;   // ใบโค้งเนียนขึ้น
            int pl = -1, pr = -1;
            for (int i = 0; i <= Seg; i++)
            {
                float t = i / (float)Seg, ww = w * (1 - t * 0.9f);
                var p = root + Vector3.up * h * t + dir * bend * h * t * t * 0.5f - Vector3.up * bend * h * t * t * 0.18f;
                var col = Color.Lerp(c0 * 0.7f, c1, t);
                int l = bld.Add(p - side * ww, col), r = bld.Add(p + side * ww, col);
                if (i > 0) bld.Quad(0, pl, l, pr, r);
                pl = l; pr = r;
            }
            if (b % 9 == 0)   // ฝักธูปฤาษี
            {
                var top = root + Vector3.up * h * 1.05f;
                var brown = new Color(0.32f, 0.2f, 0.1f);
                var stalk = new Color(0.4f, 0.45f, 0.22f);
                for (int k = 0; k < 4; k++)   // ก้านตรงจากโคนถึงฝัก
                {
                    float a0 = k / 4f * Mathf.PI * 2, a1 = (k + 1) / 4f * Mathf.PI * 2;
                    Vector3 o0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 0.01f, o1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 0.01f;
                    bld.Quad(0, bld.Add(root + o0, stalk * 0.7f), bld.Add(top + o0 + Vector3.up * 0.3f, stalk), bld.Add(root + o1, stalk * 0.7f), bld.Add(top + o1 + Vector3.up * 0.3f, stalk));
                }
                // ฝักกลมมนหัวท้าย (8 ด้าน 3 ช่วง) + ยอดแหลมเล็ก ๆ เหนือฝัก
                float[] ry = { 0f, 0.03f, 0.19f, 0.22f }; float[] rr = { 0.018f, 0.032f, 0.032f, 0.016f };
                for (int k = 0; k < 8; k++)
                {
                    float a0 = k / 8f * Mathf.PI * 2, a1 = (k + 1) / 8f * Mathf.PI * 2;
                    var u0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)); var u1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                    for (int q = 0; q < 3; q++)
                    {
                        var cq = brown * (q == 1 ? 1.1f : 0.85f);
                        bld.Quad(0, bld.Add(top + u0 * rr[q] + Vector3.up * ry[q], cq), bld.Add(top + u0 * rr[q + 1] + Vector3.up * ry[q + 1], cq), bld.Add(top + u1 * rr[q] + Vector3.up * ry[q], cq), bld.Add(top + u1 * rr[q + 1] + Vector3.up * ry[q + 1], cq));
                    }
                }
            }
        }
        return bld.Build("Reeds");
    }

    // ผ้าแพรผูกศาล: แถบผ้ายาวจากจุดผูก (y = 0) ไปถึงปลาย (y = length) — วางวัตถุกลับหัวให้ห้อยลง ไหวลมมากขึ้นตามความยาว
    public static Mesh Ribbon(int seed, Color col, float length)
    {
        var rnd = new System.Random(seed);
        var bld = new Builder(1);
        const int Seg = 10;
        float w = 0.035f, ph = (float)rnd.NextDouble() * 6;
        int pl = -1, pr = -1;
        for (int i = 0; i <= Seg; i++)
        {
            float t = i / (float)Seg;
            var p = new Vector3(Mathf.Sin(t * 5 + ph) * 0.04f * t, length * t, Mathf.Cos(t * 3 + ph) * 0.05f * t);
            var c = col * (0.85f + 0.15f * Mathf.Sin(t * 9));
            int l = bld.Add(p + new Vector3(-w, 0, 0), c), r = bld.Add(p + new Vector3(w, 0, 0), c);
            if (i > 0) bld.Quad(0, pl, l, pr, r);
            pl = l; pr = r;
        }
        return bld.Build("Ribbon");
    }

    // พวงมาลัยดาวเรือง: ดอกกลม ๆ เรียงเป็นวง (รัศมี radius) สีส้มเหลือง
    public static Mesh Garland(int seed, float radius, int flowers)
    {
        var rnd = new System.Random(seed);
        var bld = new Builder(1);
        for (int f = 0; f < flowers; f++)
        {
            float a = f / (float)flowers * Mathf.PI * 2, r = radius * 0.11f;
            var c = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a * 2) * radius * 0.04f, Mathf.Sin(a) * radius);
            var col = rnd.NextDouble() < 0.7 ? new Color(0.98f, 0.55f, 0.08f) : new Color(1f, 0.82f, 0.15f);
            var d = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            int[] ix = new int[6];
            for (int k = 0; k < 6; k++) ix[k] = bld.Add(c + d[k] * r, col * (k == 1 ? 0.6f : 1f));
            // ทรงแปดหน้า
            int[,] faces = { { 0, 4, 3 }, { 0, 3, 5 }, { 0, 5, 2 }, { 0, 2, 4 }, { 1, 3, 4 }, { 1, 5, 3 }, { 1, 2, 5 }, { 1, 4, 2 } };
            for (int k = 0; k < 8; k++) bld.Tri(0, ix[faces[k, 0]], ix[faces[k, 1]], ix[faces[k, 2]]);
        }
        return bld.Build("Garland");
    }

    // เรือพายไม้: ลำตัวโค้งรูปตัว U หัวท้ายเรียวแหลม ขอบเรือสูงขึ้นที่หัวท้าย (ใช้วัสดุสองหน้า เห็นท้องเรือด้านใน) · ยาว length ตามแกน Z
    public static Mesh Rowboat(float length, float width, float depth)
    {
        var bld = new Builder(1);
        const int N = 18, M = 10;
        var idx = new int[N + 1, M + 1];
        for (int i = 0; i <= N; i++)
        {
            float t = i / (float)N, z = (t - 0.5f) * length;
            float sn = Mathf.Max(0, Mathf.Sin(t * Mathf.PI));                   // sin(π) ออกมาติดลบนิด ๆ → Pow ได้ NaN
            float w = width * 0.5f * Mathf.Pow(sn, 0.55f) + 0.01f;
            float d = depth * Mathf.Pow(sn, 0.35f);
            float sheer = 0.12f * (2 * t - 1) * (2 * t - 1);                     // ขอบเรือแอ่นขึ้นที่หัวท้าย
            for (int j = 0; j <= M; j++)
            {
                float a = (j / (float)M - 0.5f) * Mathf.PI;                       // ข้างซ้าย → กระดูกงู → ข้างขวา
                var col = Color.white * (0.85f + 0.15f * Mathf.Cos(a));
                idx[i, j] = bld.Add(new Vector3(Mathf.Sin(a) * w, sheer - Mathf.Cos(a) * d, z), col);
            }
        }
        for (int i = 0; i < N; i++) for (int j = 0; j < M; j++) bld.Quad(0, idx[i, j], idx[i + 1, j], idx[i, j + 1], idx[i + 1, j + 1]);
        return bld.Build("Rowboat");
    }

    // หลังคาจากมุงซ้อนชั้น: จั่วสองข้าง สันตามแกน Z ยาว length · แต่ละข้างมุงสามชั้นซ้อนเหลื่อม ชายล่างแต่ละชั้นหลุดลุ่ยไม่เท่ากัน
    // มีความหนา (ผิวบน/ล่าง/ขอบ) · สีฟางซีดตามจุดยอด บางหย่อมดำคล้ำ/มีตะไคร่ (ใช้คู่กับลายจากแบบ triplanar)
    public static Mesh ThatchRoof(int seed, float length, float halfW, float rise)
    {
        var rnd = new System.Random(seed);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var bld = new Builder(1);
        const int C = 16, Bands = 3;
        float L = length * 0.5f;
        for (int side = -1; side <= 1; side += 2)
            for (int band = 0; band < Bands; band++)
            {
                float t0 = Mathf.Max(0, band / (float)Bands - 0.06f), t1 = (band + 1) / (float)Bands;
                float lift = 0.05f * (Bands - band);                                   // ชั้นบนนูนกว่าชั้นล่าง
                int[] topA = new int[C + 1], topB = new int[C + 1], botA = new int[C + 1], botB = new int[C + 1];
                for (int c = 0; c <= C; c++)
                {
                    float z = -L + 2 * L * c / C;
                    float fray = band == Bands - 1 ? R(0.02f, 0.2f) : R(0f, 0.08f);  // ชายล่างหลุดลุ่ย (ชั้นล่างสุดมากสุด)
                    float tb = t1 + fray / Mathf.Max(halfW, 0.1f);
                    var straw = Color.Lerp(new Color(0.62f, 0.56f, 0.44f), new Color(0.45f, 0.4f, 0.32f), R(0, 1));
                    if (rnd.NextDouble() < 0.12) straw = Color.Lerp(straw, new Color(0.36f, 0.4f, 0.3f), 0.6f);   // ตะไคร่
                    Vector3 P(float t, float off) => new Vector3(side * halfW * t, rise * (1 - t) + lift + off, z);
                    topA[c] = bld.Add(P(t0, 0.06f), straw); topB[c] = bld.Add(P(tb, -0.02f), straw * 0.8f);
                    botA[c] = bld.Add(P(t0, -0.06f), straw * 0.55f); botB[c] = bld.Add(P(tb, -0.1f), straw * 0.5f);
                }
                for (int c = 0; c < C; c++)
                {
                    if (side > 0)
                    {
                        bld.Quad(0, topA[c], topA[c + 1], topB[c], topB[c + 1]);
                        bld.Quad(0, botA[c + 1], botA[c], botB[c + 1], botB[c]);
                        bld.Quad(0, topB[c], topB[c + 1], botB[c], botB[c + 1]);
                    }
                    else
                    {
                        bld.Quad(0, topA[c + 1], topA[c], topB[c + 1], topB[c]);
                        bld.Quad(0, botA[c], botA[c + 1], botB[c], botB[c + 1]);
                        bld.Quad(0, topB[c + 1], topB[c], botB[c + 1], botB[c]);
                    }
                }
            }
        return bld.Build("ThatchRoof");
    }

    // ผักตบชวา: ใบกลมมันตั้งขึ้นเป็นกอ ก้านป่อง · บางกอมีช่อดอกม่วงอ่อน
    public static Mesh Hyacinth(int seed)
    {
        var rnd = new System.Random(seed);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var bld = new Builder(1);
        int leaves = rnd.Next(6, 10);
        for (int l = 0; l < leaves; l++)
        {
            float az = R(0, Mathf.PI * 2), tilt = R(0.5f, 1.1f), r = R(0.07f, 0.11f);
            var dir = new Vector3(Mathf.Cos(az), 0, Mathf.Sin(az));
            var c = dir * R(0.04f, 0.12f) + Vector3.up * R(0.12f, 0.22f);
            var nrm = (dir * Mathf.Sin(tilt) + Vector3.up * Mathf.Cos(tilt)).normalized;
            var u = Vector3.Cross(nrm, Vector3.up).normalized; var w = Vector3.Cross(u, nrm);
            var col = Color.Lerp(new Color(0.18f, 0.38f, 0.12f), new Color(0.32f, 0.5f, 0.18f), R(0, 1));
            const int S = 9;
            for (int k = 0; k < S; k++)
            {
                float a0 = k / (float)S * Mathf.PI * 2, a1 = (k + 1) / (float)S * Mathf.PI * 2;
                bld.TwoSided(0, c, c + (u * Mathf.Cos(a0) + w * Mathf.Sin(a0)) * r, c + (u * Mathf.Cos(a1) + w * Mathf.Sin(a1)) * r, col);
            }
            bld.TwoSided(0, Vector3.zero, c - w * r * 0.8f, dir * 0.03f, col * 0.75f);   // ก้านป่องลงน้ำ
        }
        if (rnd.NextDouble() < 0.45)   // ช่อดอกม่วง
        {
            var lilac = new Color(0.72f, 0.6f, 0.85f);
            for (int f = 0; f < 7; f++)
            {
                var p = new Vector3(R(-0.025f, 0.025f), 0.28f + f * 0.025f, R(-0.025f, 0.025f));
                bld.TwoSided(0, p + new Vector3(-0.025f, 0, 0), p + new Vector3(0.025f, 0, 0), p + new Vector3(0, 0.03f, 0.01f), lilac);
            }
        }
        return bld.Build("Hyacinth");
    }

    // ท่อเรียวตามเส้นโค้ง (ราก กิ่ง เถาวัลย์) · รัศมีไล่จาก r0 ถึง r1
    static void Tube(Builder bld, int sub, System.Collections.Generic.IList<Vector3> pts, float r0, float r1, Color c0, Color c1, int sides = 6)
    {
        int prevStart = -1;
        for (int i = 0; i < pts.Count; i++)
        {
            float t = i / (float)(pts.Count - 1);
            var tg = (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
            var a = Vector3.Cross(tg, Mathf.Abs(tg.y) > 0.9f ? Vector3.right : Vector3.up).normalized; var b = Vector3.Cross(tg, a);
            float r = Mathf.Lerp(r0, r1, t); var col = Color.Lerp(c0, c1, t);
            int start = bld.v.Count;
            for (int k = 0; k <= sides; k++) { float an = k / (float)sides * Mathf.PI * 2; bld.Add(pts[i] + (a * Mathf.Cos(an) + b * Mathf.Sin(an)) * r, col); }
            if (prevStart >= 0) for (int k = 0; k < sides; k++) bld.Quad(sub, prevStart + k, start + k, prevStart + k + 1, start + k + 1);
            prevStart = start;
        }
    }

    // ต้นไทรรัด (ไทรย้อย): รากใหญ่หลายสิบเส้นแผ่ออกจากโคนลงดิน/น้ำ บิดเกลียวรวมเป็นลำต้น ยอดแตกกิ่งใหญ่ + รากอากาศห้อยจากกิ่งลงมา
    // submesh 0 = ลำต้น/ราก/กิ่ง, 1 = รากอากาศเส้นเล็ก (ใส่วัสดุไหวลมได้)
    public static Mesh Banyan(int seed, float height)
    {
        var rnd = new System.Random(seed);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var bld = new Builder(2);
        Color bark0 = new Color(0.36f, 0.32f, 0.26f), bark1 = new Color(0.5f, 0.46f, 0.38f);
        int strands = rnd.Next(14, 20);
        var tips = new System.Collections.Generic.List<Vector3>();
        for (int s = 0; s < strands; s++)
        {
            float az = s / (float)strands * Mathf.PI * 2 + R(-0.2f, 0.2f), spread = R(1.6f, 3.4f), tw = R(0.6f, 1.4f) * (rnd.NextDouble() < 0.5 ? 1 : -1);
            var pts = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i <= 18; i++)
            {
                float t = i / 18f;
                float rad = Mathf.Lerp(spread, 0.35f + R(0, 0.12f), Mathf.SmoothStep(0, 1, Mathf.Min(1, t * 2.2f)));   // แผ่ที่โคน → รวบเป็นลำ
                float y = t < 0.3f ? Mathf.Lerp(-0.6f, 0.9f, t / 0.3f) * (1 - Mathf.Pow(1 - t / 0.3f, 2) * 0.4f) : Mathf.Lerp(0.9f, height, (t - 0.3f) / 0.7f);
                float a = az + tw * t * 2.2f;
                pts.Add(new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad));
            }
            Tube(bld, 0, pts, R(0.09f, 0.16f), R(0.07f, 0.12f), bark0 * R(0.85f, 1.1f), bark1 * R(0.85f, 1.1f));
        }
        // กิ่งใหญ่แผ่ออกจากยอด
        int branches = rnd.Next(5, 8);
        for (int b = 0; b < branches; b++)
        {
            float az = R(0, Mathf.PI * 2), len = R(3f, 5.5f), rise = R(0.4f, 1.8f);
            var dir = new Vector3(Mathf.Cos(az), 0, Mathf.Sin(az));
            var pts = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i <= 10; i++) { float t = i / 10f; pts.Add(new Vector3(0, height - 0.6f, 0) + dir * len * t + Vector3.up * (rise * t - 0.8f * t * t)); }
            Tube(bld, 0, pts, 0.32f, 0.06f, bark0, bark1);
            for (int k = 3; k <= 10; k += rnd.Next(1, 3)) tips.Add(pts[k]);
        }
        // รากอากาศ: เส้นเล็กห้อยตรงลงมาจากกิ่ง บางเส้นยาวถึงพื้น
        foreach (var tp in tips)
            for (int k = 0; k < rnd.Next(1, 4); k++)
            {
                var top = tp + new Vector3(R(-0.3f, 0.3f), 0, R(-0.3f, 0.3f)); float drop = R(1.5f, top.y + 0.4f);
                Tube(bld, 1, new[] { top, top + Vector3.down * drop * 0.5f + new Vector3(R(-0.1f, 0.1f), 0, R(-0.1f, 0.1f)), top + Vector3.down * drop }, 0.025f, 0.012f, bark1 * 0.9f, bark1 * 0.7f, 4);
            }
        return bld.Build("Banyan");
    }

    // เถาวัลย์ห้อยโยง: สายโค้งหย่อนระหว่างจุดยึดสองจุดบนเรือนยอด + เส้นห้อยดิ่งลงมา (ยาว width ตามแกน X สูง height)
    public static Mesh Vines(int seed, float width, float height)
    {
        var rnd = new System.Random(seed);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var bld = new Builder(1);
        Color c0 = new Color(0.28f, 0.3f, 0.18f), c1 = new Color(0.4f, 0.38f, 0.24f);
        for (int l = 0; l < rnd.Next(4, 7); l++)
        {
            var a = new Vector3(R(-width * 0.5f, 0), height + R(-0.5f, 0.5f), R(-1, 1)); var b = new Vector3(R(0, width * 0.5f), height + R(-0.5f, 0.5f), R(-1, 1));
            float sag = R(1.2f, 3.5f);
            var pts = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i <= 14; i++) { float t = i / 14f; pts.Add(Vector3.Lerp(a, b, t) + Vector3.down * sag * 4 * t * (1 - t)); }
            Tube(bld, 0, pts, R(0.025f, 0.05f), R(0.025f, 0.05f), c0, c1, 5);
        }
        for (int h = 0; h < rnd.Next(6, 12); h++)
        {
            var top = new Vector3(R(-width * 0.5f, width * 0.5f), height, R(-1.2f, 1.2f)); float drop = R(1f, height * 0.85f);
            Tube(bld, 0, new[] { top, top + new Vector3(R(-0.15f, 0.15f), -drop * 0.5f, R(-0.15f, 0.15f)), top + Vector3.down * drop }, 0.022f, 0.012f, c0, c1, 4);
        }
        return bld.Build("Vines");
    }
    // ต้นโกงกาง: ลำต้นตั้งบนรากค้ำยันโค้งเป็นซุ้มหลายสิบเส้น (แตกแขนงซ้อนกันเหมือนกรงเล็บ) · ยอดแตกกิ่ง + พุ่มใบมันเขียวเข้มเป็นกระจุก
    // leaves = false → ต้นโกงกางตาย (เหลือแต่รากกับกิ่งแห้งสีซีด) · submesh 0 = เปลือก/ราก/กิ่ง, 1 = ใบ
    public static Mesh Mangrove(int seed, float height, bool leaves)
    {
        var rnd = new System.Random(seed);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var bld = new Builder(2);
        Color bark0 = leaves ? new Color(0.58f, 0.52f, 0.44f) : new Color(0.7f, 0.67f, 0.62f), bark1 = leaves ? new Color(0.72f, 0.66f, 0.56f) : new Color(0.85f, 0.83f, 0.78f);
        var lean = new Vector3(R(-0.25f, 0.25f), 0, R(-0.25f, 0.25f));
        Vector3 TrunkAt(float y) => new Vector3(lean.x * y * 0.3f, y, lean.z * y * 0.3f);
        var trunk = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i <= 8; i++) trunk.Add(TrunkAt(Mathf.Lerp(0.8f, height * 0.62f, i / 8f)));
        Tube(bld, 0, trunk, 0.2f, 0.13f, bark0, bark1, 8);
        // รากค้ำยัน: ออกจากลำต้นช่วง 0.9–2.6 ม. โค้งขึ้นนิดแล้วดิ่งลงปักเลน · บางเส้นแตกแขนงออกไปอีกชั้น
        int roots = rnd.Next(13, 19);
        for (int k = 0; k < roots; k++)
        {
            float az = k / (float)roots * Mathf.PI * 2 + R(-0.35f, 0.35f), hs = R(0.7f, 2.4f), reach = R(1.1f, 2.6f) * (0.7f + hs * 0.2f);
            var dir = new Vector3(Mathf.Cos(az), 0, Mathf.Sin(az));
            Vector3 p0 = TrunkAt(hs) + dir * 0.12f, p1 = TrunkAt(hs) + dir * reach * R(0.3f, 0.6f) + Vector3.up * R(0.25f, 0.8f) + new Vector3(R(-0.3f, 0.3f), 0, R(-0.3f, 0.3f)), p2 = dir * reach + new Vector3(R(-0.4f, 0.4f), -0.45f, R(-0.4f, 0.4f));
            var pts = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i <= 12; i++) { float t = i / 12f; pts.Add((1 - t) * (1 - t) * p0 + 2 * t * (1 - t) * p1 + t * t * p2); }
            RootTube(bld, pts, R(0.06f, 0.09f), bark0, rnd);   // ปลายรากเปื้อนโคลน
            if (rnd.NextDouble() < 0.55)
            {
                var m = pts[rnd.Next(5, 9)]; float az2 = az + R(-0.7f, 0.7f); var d2 = new Vector3(Mathf.Cos(az2), 0, Mathf.Sin(az2));
                Vector3 q1 = m + d2 * R(0.4f, 0.8f) + Vector3.up * R(0f, 0.25f), q2 = m + d2 * R(1f, 1.8f); q2.y = -0.4f;
                var p2s = new System.Collections.Generic.List<Vector3>();
                for (int i = 0; i <= 8; i++) { float t = i / 8f; p2s.Add((1 - t) * (1 - t) * m + 2 * t * (1 - t) * q1 + t * t * q2); }
                RootTube(bld, p2s, 0.045f, bark0, rnd);
            }
        }
        // กิ่งบน + พุ่มใบ
        var top = TrunkAt(height * 0.6f);
        int branches = rnd.Next(4, 7);
        for (int b = 0; b < branches; b++)
        {
            float az = b / (float)branches * Mathf.PI * 2 + R(-0.4f, 0.4f), len = R(1.4f, 2.8f) * (leaves ? 1 : 0.8f);
            var dir = new Vector3(Mathf.Cos(az), R(0.5f, 1.1f), Mathf.Sin(az)).normalized;
            var pts = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i <= 6; i++) { float t = i / 6f; pts.Add(top + dir * len * t + Vector3.down * 0.25f * t * t + Vector3.down * R(0, 0.4f) * (leaves ? 0 : t)); }
            Tube(bld, 0, pts, 0.1f, 0.03f, bark0, bark1, 5);
            var clusters = new System.Collections.Generic.List<Vector3> { pts[6] };
            for (int tw = 0; tw < 3; tw++)
            {
                var from = pts[rnd.Next(2, 6)]; var d2 = (dir + Random3(rnd) * 0.9f).normalized; d2.y = Mathf.Abs(d2.y) * 0.6f + 0.2f;
                var end = from + d2 * R(0.7f, 1.4f);
                Tube(bld, 0, new[] { from, (from + end) * 0.5f + Vector3.up * 0.1f, end }, 0.035f, 0.012f, bark0, bark1, 4);
                clusters.Add(end);
            }
            if (!leaves) continue;
            foreach (var c in clusters)
                for (int l = 0; l < 55; l++)
                {
                    var ctr = c + Vector3.Scale(Random3(rnd), new Vector3(1.05f, 0.55f, 1.05f)) + Vector3.up * 0.2f;
                    var fwd = Random3(rnd).normalized; var side = Vector3.Cross(fwd, Vector3.up).normalized * 0.075f; var f = fwd * 0.15f;
                    var col = Color.Lerp(new Color(0.17f, 0.28f, 0.12f), new Color(0.32f, 0.42f, 0.18f), R(0, 1));
                    bld.TwoSided(1, ctr - f, ctr + side, ctr + f, col); bld.TwoSided(1, ctr - f, ctr + f, ctr - side, col);
                }
        }
        return bld.Build("Mangrove");
    }
    static Vector3 Random3(System.Random r) { Vector3 v; do v = new Vector3((float)r.NextDouble() * 2 - 1, (float)r.NextDouble() * 2 - 1, (float)r.NextDouble() * 2 - 1); while (v.sqrMagnitude > 1 || v.sqrMagnitude < 0.01f); return v; }
}
