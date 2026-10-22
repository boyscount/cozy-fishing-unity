// โมเดลที่ Poly Haven ไม่มี สร้างด้วยโค้ดให้ดูสมจริง: ต้นตาลโตนด (ใบพัดจีบ) · ใบบัว (มีรอยแหว่ง เส้นใบ ขอบงอน) · ดอกบัว (กลีบซ้อน 2 ชั้น)
// สีจุดยอดเป็นค่า linear ใช้กับ CozyFishing/Surface + Use Vertex Color
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class ProceduralMeshes
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
    public static Mesh LotusFlower(int seed, bool bud)
    {
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
}
