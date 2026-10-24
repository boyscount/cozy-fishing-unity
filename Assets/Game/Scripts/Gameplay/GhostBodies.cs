using System.Collections.Generic;
using UnityEngine;

namespace CozyFishing
{
    // ร่างผีสร้างด้วยโค้ด: ท่อเรียวต่อข้อ (ลำตัว แขน ขา คอ) + หัว + เส้นผม/เชือก/โซ่/รากไม้ · ใส่ shader ผีโปร่งแสงแล้วรายละเอียดหยาบไม่เด่น
    // พิกัดเฉพาะที่: y ขึ้น · หันหน้า +z · คืนตำแหน่งดวงตาไว้วางลูกตาเรืองแสง
    public static class GhostBodies
    {
        public enum Kind { Drowned, Preta, LotusMaiden, NetFiend, RootSiren, Headless, MudWraith, Hanging }

        class B
        {
            public readonly List<Vector3> v = new List<Vector3>(); public readonly List<int> t = new List<int>();
            public readonly List<Vector3> eyes = new List<Vector3>();
            public void Tube(IList<Vector3> pts, float r0, float r1, int sides = 7)
            {
                int prev = -1;
                for (int i = 0; i < pts.Count; i++)
                {
                    float u = pts.Count > 1 ? i / (pts.Count - 1f) : 0;
                    var tg = (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
                    if (tg.sqrMagnitude < 1e-6f) tg = Vector3.up;
                    var a = Vector3.Cross(tg, Mathf.Abs(tg.y) > 0.9f ? Vector3.right : Vector3.up).normalized; var b = Vector3.Cross(tg, a);
                    float r = Mathf.Lerp(r0, r1, u); int start = v.Count;
                    for (int k = 0; k <= sides; k++) { float an = k / (float)sides * Mathf.PI * 2; v.Add(pts[i] + (a * Mathf.Cos(an) + b * Mathf.Sin(an)) * r); }
                    if (prev >= 0) for (int k = 0; k < sides; k++) t.AddRange(new[] { prev + k, start + k, prev + k + 1, prev + k + 1, start + k, start + k + 1 });
                    prev = start;
                }
            }
            public void Seg(Vector3 a, Vector3 b, float r0, float r1) => Tube(new[] { a, Vector3.Lerp(a, b, 0.5f), b }, r0, r1);
            public void Limb(Vector3 a, Vector3 mid, Vector3 b, float r0, float r1) => Tube(Curve(a, mid, b, 8), r0, r1);
            public void Ball(Vector3 c, Vector3 r, int seg = 10)
            {
                int start = v.Count;
                for (int j = 0; j <= seg; j++)
                {
                    float th = j / (float)seg * Mathf.PI;
                    for (int i = 0; i <= seg; i++) { float ph = i / (float)seg * Mathf.PI * 2; v.Add(c + new Vector3(Mathf.Sin(th) * Mathf.Cos(ph) * r.x, Mathf.Cos(th) * r.y, Mathf.Sin(th) * Mathf.Sin(ph) * r.z)); }
                }
                for (int j = 0; j < seg; j++) for (int i = 0; i < seg; i++) { int a = start + j * (seg + 1) + i, b = a + seg + 1; t.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b }); }
            }
            public Mesh Build(string name)
            {
                var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
            }
        }
        static List<Vector3> Curve(Vector3 a, Vector3 m, Vector3 b, int n)
        {
            var l = new List<Vector3>();
            for (int i = 0; i <= n; i++) { float u = i / (float)n; l.Add((1 - u) * (1 - u) * a + 2 * u * (1 - u) * m + u * u * b); }
            return l;
        }
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        /// ร่างผีตามชนิด · eyes = ตำแหน่งลูกตา (เฉพาะที่)
        public static Mesh Build(Kind k, int seed, out Vector3[] eyes)
        {
            var b = new B(); var rnd = new System.Random(seed);
            float R(float x, float y) => x + (float)rnd.NextDouble() * (y - x);
            switch (k)
            {
                case Kind.Drowned:   // ร่างซีดคลานต่ำ ตัวบิดเกร็ง แขนยื่นไปข้างหน้า หัวเงยจ้อง
                    b.Tube(Curve(V(0, 0.42f, -0.55f), V(0.05f, 0.55f, -0.05f), V(0, 0.62f, 0.35f), 8), 0.13f, 0.16f);
                    b.Ball(V(0, 0.78f, 0.52f), V(0.13f, 0.15f, 0.14f));
                    b.Limb(V(-0.17f, 0.6f, 0.3f), V(-0.42f, 0.7f, 0.55f), V(-0.35f, 0.02f, 0.85f), 0.05f, 0.035f);
                    b.Limb(V(0.17f, 0.6f, 0.3f), V(0.45f, 0.5f, 0.4f), V(0.38f, 0.02f, 0.7f), 0.05f, 0.035f);
                    b.Limb(V(-0.1f, 0.42f, -0.55f), V(-0.35f, 0.15f, -0.4f), V(-0.4f, 0.02f, -1.1f), 0.06f, 0.04f);
                    b.Limb(V(0.1f, 0.42f, -0.55f), V(0.3f, 0.35f, -0.9f), V(0.25f, 0.02f, -1.25f), 0.06f, 0.04f);
                    for (int i = 0; i < 8; i++) b.Tube(Curve(V(R(-0.1f, 0.1f), 0.88f, 0.48f), V(R(-0.2f, 0.2f), 0.7f, 0.42f), V(R(-0.25f, 0.25f), 0.3f, R(0.4f, 0.6f)), 5), 0.012f, 0.004f, 4);
                    b.eyes.Add(V(-0.05f, 0.8f, 0.64f)); b.eyes.Add(V(0.05f, 0.8f, 0.64f));
                    break;
                case Kind.Preta:     // เปรต: สูงชะลูด ~6 ม. ขาเรียวยาว ซี่โครงโผล่ คอยาวโค้ง หัวเล็ก แขนยาวถึงเข่า
                    b.Limb(V(-0.25f, 0, 0), V(-0.3f, 1.4f, 0.15f), V(-0.18f, 2.8f, 0), 0.07f, 0.1f);
                    b.Limb(V(0.25f, 0, 0), V(0.32f, 1.4f, -0.1f), V(0.18f, 2.8f, 0), 0.07f, 0.1f);
                    b.Tube(Curve(V(0, 2.75f, 0), V(0, 3.6f, 0.05f), V(0, 4.4f, 0.12f), 8), 0.22f, 0.15f);
                    for (int i = 0; i < 6; i++) { float y = 3.4f + i * 0.16f; b.Tube(Curve(V(-0.02f, y, 0.2f), V(-0.24f, y + 0.02f, 0.05f), V(-0.12f, y, -0.14f), 5), 0.018f, 0.018f, 4); b.Tube(Curve(V(0.02f, y, 0.2f), V(0.24f, y + 0.02f, 0.05f), V(0.12f, y, -0.14f), 5), 0.018f, 0.018f, 4); }
                    b.Tube(Curve(V(0, 4.4f, 0.12f), V(0, 5.3f, 0.2f), V(0, 5.6f, 0.75f), 10), 0.07f, 0.06f);
                    b.Ball(V(0, 5.62f, 0.95f), V(0.2f, 0.24f, 0.24f));
                    b.Limb(V(-0.24f, 4.3f, 0.1f), V(-0.6f, 3.0f, 0.4f), V(-0.55f, 1.2f, 0.7f), 0.05f, 0.03f);
                    b.Limb(V(0.24f, 4.3f, 0.1f), V(0.65f, 3.2f, 0.2f), V(0.6f, 1.4f, 0.55f), 0.05f, 0.03f);
                    for (int f = 0; f < 4; f++) { b.Seg(V(-0.55f, 1.2f, 0.7f), V(-0.55f + (f - 1.5f) * 0.06f, 0.75f, 0.85f), 0.012f, 0.006f); b.Seg(V(0.6f, 1.4f, 0.55f), V(0.6f + (f - 1.5f) * 0.06f, 0.95f, 0.7f), 0.012f, 0.006f); }
                    b.eyes.Add(V(-0.07f, 5.68f, 1.15f)); b.eyes.Add(V(0.07f, 5.68f, 1.15f));
                    break;
                case Kind.LotusMaiden:   // หญิงสาวสไบขาด ลอยเหนือกอบัว ผมยาวปรกหลัง
                    b.Tube(new[] { V(0, 0.15f, 0), V(0, 0.55f, 0), V(0, 0.95f, 0) }, 0.32f, 0.14f, 10);   // ผ้าถุงบานล่าง
                    b.Tube(new[] { V(0, 0.95f, 0), V(0, 1.2f, 0.02f), V(0, 1.38f, 0) }, 0.15f, 0.12f);
                    b.Seg(V(0, 1.38f, 0), V(0, 1.48f, 0.02f), 0.05f, 0.05f);
                    b.Ball(V(0, 1.6f, 0.03f), V(0.1f, 0.13f, 0.11f));
                    b.Limb(V(-0.16f, 1.34f, 0), V(-0.24f, 1.05f, 0.08f), V(-0.2f, 0.78f, 0.12f), 0.035f, 0.025f);
                    b.Limb(V(0.16f, 1.34f, 0), V(0.25f, 1.05f, 0.05f), V(0.22f, 0.8f, 0.1f), 0.035f, 0.025f);
                    for (int i = 0; i < 14; i++) { float x = R(-0.12f, 0.12f); b.Tube(Curve(V(x, 1.7f, R(-0.06f, 0.06f)), V(x * 1.3f, 1.4f, -0.12f), V(x * 1.6f, R(0.6f, 0.9f), R(-0.2f, 0.05f)), 6), 0.016f, 0.006f, 4); }
                    for (int i = 0; i < 5; i++) { float x = R(-0.08f, 0.08f); b.Tube(Curve(V(x, 1.72f, 0.08f), V(x, 1.55f, 0.15f), V(x * 1.2f, 1.3f, 0.12f), 4), 0.012f, 0.005f, 4); }   // ผมปรกหน้า
                    b.eyes.Add(V(-0.035f, 1.62f, 0.13f)); b.eyes.Add(V(0.035f, 1.62f, 0.13f));
                    break;
                case Kind.NetFiend:      // ก้อนเงาดำปริ่มน้ำ พันแหอวนเก่า ห้อยเชือก
                    for (int i = 0; i < 4; i++) b.Ball(V(R(-0.3f, 0.3f), R(0.1f, 0.5f), R(-0.3f, 0.3f)), V(R(0.4f, 0.65f), R(0.35f, 0.55f), R(0.4f, 0.6f)), 9);
                    for (int i = 0; i < 22; i++) { float a = R(0, Mathf.PI * 2), r = R(0.3f, 0.7f); var top = V(Mathf.Cos(a) * r, R(0.4f, 0.9f), Mathf.Sin(a) * r); b.Tube(Curve(top, top + V(Mathf.Cos(a) * 0.3f, -0.4f, Mathf.Sin(a) * 0.3f), top + V(Mathf.Cos(a) * R(0.4f, 0.8f), -R(0.8f, 1.4f), Mathf.Sin(a) * R(0.4f, 0.8f)), 6), 0.02f, 0.008f, 4); }
                    for (int i = 0; i < 10; i++) { float a = i / 10f * Mathf.PI * 2; b.Tube(Curve(V(Mathf.Cos(a) * 0.7f, 0.2f, Mathf.Sin(a) * 0.7f), V(Mathf.Cos(a + 0.3f) * 0.75f, 0.7f, Mathf.Sin(a + 0.3f) * 0.75f), V(Mathf.Cos(a + 0.6f) * 0.4f, 1f, Mathf.Sin(a + 0.6f) * 0.4f), 6), 0.01f, 0.01f, 3); }   // ตาข่ายพันรอบ
                    b.eyes.Add(V(-0.12f, 0.75f, 0.5f)); b.eyes.Add(V(0.12f, 0.75f, 0.5f));
                    break;
                case Kind.RootSiren:     // นางพรายลอยคอ: ลำตัวโผล่จากน้ำ ช่วงล่างเป็นรากไม้แผ่ ผมยาวสยายบนผิวน้ำ
                    b.Tube(new[] { V(0, -0.2f, 0), V(0, 0.3f, 0.02f), V(0, 0.75f, 0) }, 0.16f, 0.13f);
                    b.Seg(V(0, 0.75f, 0), V(0, 0.86f, 0.02f), 0.05f, 0.05f);
                    b.Ball(V(0, 0.98f, 0.04f), V(0.1f, 0.13f, 0.11f));
                    b.Limb(V(-0.15f, 0.7f, 0), V(-0.35f, 0.45f, 0.2f), V(-0.45f, 0.05f, 0.45f), 0.035f, 0.025f);
                    b.Limb(V(0.15f, 0.7f, 0), V(0.38f, 0.5f, 0.15f), V(0.5f, 0.05f, 0.35f), 0.035f, 0.025f);
                    for (int i = 0; i < 8; i++) { float a = i / 8f * Mathf.PI * 2 + R(-0.2f, 0.2f); b.Tube(Curve(V(0, -0.15f, 0), V(Mathf.Cos(a) * 0.6f, -0.3f, Mathf.Sin(a) * 0.6f), V(Mathf.Cos(a) * R(1.3f, 2f), R(-0.6f, -0.2f), Mathf.Sin(a) * R(1.3f, 2f)), 8), 0.07f, 0.02f, 5); }
                    for (int i = 0; i < 24; i++) { float a = R(-2.6f, 2.6f) + Mathf.PI; b.Tube(Curve(V(0, 1.05f, 0), V(Mathf.Sin(a) * 0.4f, 0.4f, Mathf.Cos(a) * 0.4f - 0.1f), V(Mathf.Sin(a) * R(1.2f, 2.2f), 0.02f, Mathf.Cos(a) * R(1.2f, 2.2f)), 8), 0.014f, 0.006f, 4); }
                    b.eyes.Add(V(-0.035f, 1f, 0.14f)); b.eyes.Add(V(0.035f, 1f, 0.14f));
                    break;
                case Kind.Headless:      // ร่างไร้หัว ถือดาบสนิม โซ่ตรวนห้อยจากข้อมือ
                    b.Limb(V(-0.14f, 0, 0), V(-0.15f, 0.45f, 0.06f), V(-0.12f, 0.9f, 0), 0.07f, 0.09f);
                    b.Limb(V(0.14f, 0, 0), V(0.16f, 0.45f, -0.04f), V(0.12f, 0.9f, 0), 0.07f, 0.09f);
                    b.Tube(new[] { V(0, 0.9f, 0), V(0, 1.2f, 0.02f), V(0, 1.5f, 0) }, 0.2f, 0.19f);
                    b.Seg(V(0, 1.5f, 0), V(0, 1.58f, 0), 0.07f, 0.06f);   // ตอคอ
                    b.Limb(V(-0.22f, 1.45f, 0), V(-0.3f, 1.15f, 0.1f), V(-0.26f, 0.95f, 0.3f), 0.05f, 0.04f);
                    b.Limb(V(0.22f, 1.45f, 0), V(0.32f, 1.1f, 0.05f), V(0.3f, 0.85f, 0.15f), 0.05f, 0.04f);
                    b.Seg(V(-0.26f, 0.95f, 0.3f), V(-0.2f, 1.9f, 0.55f), 0.025f, 0.008f);   // ดาบ
                    b.Seg(V(-0.34f, 1.0f, 0.3f), V(-0.18f, 0.9f, 0.3f), 0.02f, 0.02f);    // โกร่ง
                    var chain = new List<Vector3>(); for (int i = 0; i <= 14; i++) { float u = i / 14f; chain.Add(V(0.3f + Mathf.Sin(u * 9) * 0.03f, 0.85f - u * 0.8f, 0.15f + u * 0.4f)); }
                    b.Tube(chain, 0.018f, 0.018f, 4);
                    break;
                case Kind.MudWraith:     // ครึ่งตัวบนโผล่จากเลน ผอมโซ เอื้อมมือมาข้างหน้า
                    b.Tube(Curve(V(0, -0.15f, -0.1f), V(0, 0.3f, 0.05f), V(0, 0.62f, 0.25f), 8), 0.15f, 0.13f);
                    b.Ball(V(0, 0.8f, 0.4f), V(0.11f, 0.13f, 0.12f));
                    b.Limb(V(-0.15f, 0.55f, 0.2f), V(-0.35f, 0.5f, 0.55f), V(-0.3f, 0.02f, 0.95f), 0.04f, 0.025f);
                    b.Limb(V(0.15f, 0.55f, 0.2f), V(0.32f, 0.35f, 0.6f), V(0.35f, 0.02f, 1.0f), 0.04f, 0.025f);
                    for (int i = 0; i < 6; i++) b.Tube(Curve(V(R(-0.1f, 0.1f), 0.9f, 0.38f), V(R(-0.15f, 0.15f), 0.75f, 0.32f), V(R(-0.18f, 0.18f), 0.45f, R(0.3f, 0.5f)), 4), 0.012f, 0.004f, 4);
                    b.eyes.Add(V(-0.04f, 0.82f, 0.51f)); b.eyes.Add(V(0.04f, 0.82f, 0.51f));
                    break;
                case Kind.Hanging:       // ชายแก่ผูกคอ ห้อยจากเชือก (จุดผูกอยู่บนสุด y = 3.4) แขนขาห้อยตก
                    b.Seg(V(0, 3.4f, 0), V(0, 1.72f, 0.02f), 0.018f, 0.018f);   // เชือกป่าน
                    b.Ball(V(0.03f, 1.62f, 0.03f), V(0.1f, 0.12f, 0.11f));
                    b.Seg(V(0, 1.5f, 0), V(0, 1.44f, 0), 0.05f, 0.05f);
                    b.Tube(new[] { V(0, 1.44f, 0), V(0, 1.15f, 0.01f), V(0, 0.86f, 0) }, 0.17f, 0.16f);
                    b.Limb(V(-0.19f, 1.4f, 0), V(-0.22f, 1.12f, 0.02f), V(-0.2f, 0.84f, 0.04f), 0.04f, 0.03f);
                    b.Limb(V(0.19f, 1.4f, 0), V(0.21f, 1.12f, -0.02f), V(0.19f, 0.84f, 0.03f), 0.04f, 0.03f);
                    b.Limb(V(-0.09f, 0.86f, 0), V(-0.1f, 0.45f, 0.02f), V(-0.08f, 0.02f, 0.05f), 0.06f, 0.045f);
                    b.Limb(V(0.09f, 0.86f, 0), V(0.1f, 0.45f, -0.02f), V(0.09f, 0.04f, 0.0f), 0.06f, 0.045f);
                    b.eyes.Add(V(-0.01f, 1.63f, 0.13f)); b.eyes.Add(V(0.07f, 1.6f, 0.12f));
                    break;
            }
            eyes = b.eyes.ToArray();
            return b.Build("Ghost_" + k);
        }
    }
}
