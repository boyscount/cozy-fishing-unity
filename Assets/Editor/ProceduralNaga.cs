// พญานาคปูนปั้นลงรักปิดทองแบบริมโขง: ลำตัวท่อต่อเนื่องตามเส้นโค้ง มีเกล็ดลายทอง/เขียวหม่น ท้องสีอ่อน · ครีบหงอนเปลวไฟตามสันหลัง
//   หัวชูสูงแผ่พังพานห้าเศียร (เศียรกลางใหญ่ อีกสี่เศียรเรียงแผ่ซ้ายขวา) ปากอ้า เขี้ยว ตาแดง หงอนบนหัว
// สีจุดยอดใช้กับ CozyFishing/Surface + Use Vertex Color
using System.Collections.Generic;
using UnityEngine;

public static partial class ProceduralMeshes
{
    static Color NagaGold = new Color(0.82f, 0.6f, 0.2f), NagaGoldDark = new Color(0.55f, 0.38f, 0.14f),
        NagaGreen = new Color(0.36f, 0.42f, 0.22f), NagaBelly = new Color(0.93f, 0.82f, 0.56f), NagaRed = new Color(0.75f, 0.12f, 0.08f);
    /// โทนพญานาค: false = ปูนปั้นลงรักปิดทอง · true = หินเก่าตะไคร่จับ (ริมแก่งโขง)
    public static void NagaPalette(bool stone)
    {
        NagaGold = stone ? new Color(0.62f, 0.66f, 0.48f) : new Color(0.82f, 0.6f, 0.2f);
        NagaGoldDark = stone ? new Color(0.42f, 0.46f, 0.3f) : new Color(0.55f, 0.38f, 0.14f);
        NagaGreen = stone ? new Color(0.4f, 0.54f, 0.26f) : new Color(0.36f, 0.42f, 0.22f);
        NagaBelly = stone ? new Color(0.76f, 0.76f, 0.62f) : new Color(0.93f, 0.82f, 0.56f);
    }

    /// path = จุดกลางลำตัวจากหาง → คอ (ปลายสุดคือฐานคอ หัวงอกต่อจากตรงนั้นตามทิศ dir สุดท้าย)
    public static Mesh Naga(Vector3[] path, float tailR, float neckR, int heads = 5)
    {
        var bld = new Builder(1);
        const int Sides = 12;
        int n = path.Length;
        var prevRing = new int[Sides + 1];
        Vector3 up = Vector3.up;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            var fwd = (path[Mathf.Min(i + 1, n - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
            var side = Vector3.Cross(up, fwd).normalized; if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            var u2 = Vector3.Cross(fwd, side).normalized;
            float r = Mathf.Lerp(tailR, neckR, Mathf.SmoothStep(0, 1, Mathf.Clamp01(t * 1.3f)));
            var ring = new int[Sides + 1];
            for (int k = 0; k <= Sides; k++)
            {
                float a = k / (float)Sides * Mathf.PI * 2;
                float belly = Mathf.Clamp01(-Mathf.Cos(a));                                       // ด้านล่าง = ท้อง
                var off = side * Mathf.Sin(a) * r + u2 * Mathf.Cos(a) * r * (1 - 0.15f * belly);
                // เกล็ด: ลายตารางเยื้องตามความยาว (เข้ม/สว่างสลับ) ไล่ทองบนหลัง → เขียวหม่นข้างลำตัว → ท้องสีอ่อนเป็นปล้อง
                float scale = Mathf.Repeat(i * 0.9f + (k % 2) * 0.5f, 1f) < 0.5f ? 1f : 0.82f;
                var c = Color.Lerp(NagaGold, NagaGreen, Mathf.Clamp01(Mathf.Abs(Mathf.Sin(a)) * 0.8f - 0.1f)) * scale;
                if (belly > 0.55f) c = NagaBelly * (i % 2 == 0 ? 1f : 0.86f);
                ring[k] = bld.Add(path[i] + off, c);
            }
            if (i > 0) for (int k = 0; k < Sides; k++) bld.Quad(0, prevRing[k], ring[k], prevRing[k + 1], ring[k + 1]);
            prevRing = ring;
            // ครีบหงอนเปลวไฟตามสันหลัง ทุกสองข้อ
            if (i % 3 == 0 && i > 3 && i < n - 2)
            {
                var b0 = path[i] + u2 * r * 0.95f; float fh = r * (0.45f + 0.25f * t);   // ครีบเล็ก ไม่แย่งสายตาจากลำตัว
                bld.TwoSided(0, b0 - fwd * r * 0.5f, b0 + u2 * fh + fwd * r * 0.6f, b0 + fwd * r * 0.4f, NagaGoldDark);
            }
        }
        // คอชูขึ้น + พังพานห้าเศียร
        var baseP = path[n - 1]; var dirN = (path[n - 1] - path[n - 2]).normalized;
        var sideN = Vector3.Cross(Vector3.up, dirN).normalized;
        for (int h = 0; h < heads; h++)
        {
            float k = heads == 1 ? 0 : (h - (heads - 1) * 0.5f) / ((heads - 1) * 0.5f);   // -1..1
            bool main = Mathf.Abs(k) < 0.01f;
            float hs = main ? 1f : 0.72f - Mathf.Abs(k) * 0.08f;                               // เศียรกลางใหญ่สุด
            var neckTop = baseP + Vector3.up * neckR * (main ? 4.2f : 3.4f - Mathf.Abs(k) * 0.4f) + sideN * k * neckR * 2.6f + dirN * neckR * 0.6f;
            // คอโค้ง (ท่อสั้น 6 ข้อ)
            var nprev = new int[Sides + 1];
            for (int s = 0; s <= 6; s++)
            {
                float tt = s / 6f;
                var p = Vector3.Lerp(baseP, neckTop, tt) + Vector3.up * Mathf.Sin(tt * Mathf.PI) * neckR * 0.6f;
                var f = (neckTop - baseP).normalized; var sd = Vector3.Cross(Vector3.up + dirN * 0.01f, f).normalized; var uu = Vector3.Cross(f, sd);
                float rr = neckR * Mathf.Lerp(1f, 0.8f, tt) * hs;
                var ring = new int[Sides + 1];
                for (int q = 0; q <= Sides; q++)
                {
                    float a = q / (float)Sides * Mathf.PI * 2;
                    var c = Vector3.Dot(sd * Mathf.Sin(a) + uu * Mathf.Cos(a), dirN) > 0.3f ? NagaBelly : NagaGold * (q % 2 == 0 ? 1 : 0.85f);
                    ring[q] = bld.Add(p + sd * Mathf.Sin(a) * rr + uu * Mathf.Cos(a) * rr, c);
                }
                if (s > 0) for (int q = 0; q < Sides; q++) bld.Quad(0, nprev[q], ring[q], nprev[q + 1], ring[q + 1]);
                nprev = ring;
            }
            NagaHead(bld, neckTop, dirN, neckR * hs);
        }
        return bld.Build("Naga");
    }

    // หัวพญานาค: กะโหลกรี จมูกยื่น ขากรรไกรล่างอ้า เขี้ยวขาว ตาแดง หงอนสามแฉกเปลวไฟบนหัว
    static void NagaHead(Builder bld, Vector3 p, Vector3 fwd, float r)
    {
        fwd = (fwd + Vector3.up * 0.25f).normalized;
        var side = Vector3.Cross(Vector3.up, fwd).normalized; var up = Vector3.Cross(fwd, side).normalized;
        // กะโหลกบน: ทรงรีครึ่งบน 8x6
        int Lng = 8, Lat = 5; var idx = new int[Lng + 1, Lat + 1];
        for (int i = 0; i <= Lng; i++) for (int j = 0; j <= Lat; j++)
        {
            float u = i / (float)Lng, a = j / (float)Lat * Mathf.PI;                            // ครึ่งวงบน
            float len = Mathf.Lerp(-1.1f, 2.3f, u), w = Mathf.Sin(Mathf.Clamp01(u * 1.15f) * Mathf.PI * 0.85f + 0.25f) * (1.25f - u * 0.55f);
            var pos = p + fwd * len * r + side * Mathf.Cos(a) * w * r + up * (Mathf.Sin(a) * w * 0.85f * r + 0.05f * r);
            idx[i, j] = bld.Add(pos, (i + j) % 2 == 0 ? NagaGold : NagaGold * 0.86f);
        }
        for (int i = 0; i < Lng; i++) for (int j = 0; j < Lat; j++) bld.Quad(0, idx[i, j], idx[i + 1, j], idx[i, j + 1], idx[i + 1, j + 1]);
        // ปิดด้านล่างกะโหลก (เพดานปาก สีแดง)
        for (int i = 0; i < Lng; i++) bld.Quad(0, idx[i, Lat], idx[i + 1, Lat], idx[i, 0], idx[i + 1, 0]);
        var palate = new List<int>();
        // ขากรรไกรล่างอ้าลง
        var jawTip = p + fwd * 1.9f * r - up * 0.9f * r;
        foreach (var s in new[] { -1f, 1f })
            bld.TwoSided(0, p + side * s * 0.9f * r - up * 0.05f * r, jawTip + side * s * 0.25f * r, p + fwd * 0.2f * r + side * s * 0.6f * r - up * 0.5f * r, NagaGoldDark);
        bld.TwoSided(0, p + side * 0.8f * r - up * 0.1f * r, p - side * 0.8f * r - up * 0.1f * r, jawTip, NagaRed);       // ลิ้น/ปากในสีแดง
        // เขี้ยวบนสองซี่
        foreach (var s in new[] { -1f, 1f })
        {
            var f0 = p + fwd * 1.6f * r + side * s * 0.35f * r;
            bld.TwoSided(0, f0, f0 + fwd * 0.12f * r, f0 - up * 0.55f * r + fwd * 0.05f * r, Color.white);
        }
        // ตาแดงสองข้าง (ปิรามิดเล็ก)
        foreach (var s in new[] { -1f, 1f })
        {
            var e = p + fwd * 0.55f * r + side * s * 0.78f * r + up * 0.55f * r;
            bld.TwoSided(0, e - fwd * 0.18f * r, e + up * 0.16f * r, e + fwd * 0.18f * r, NagaRed);
            bld.TwoSided(0, e - fwd * 0.18f * r, e + fwd * 0.18f * r, e - up * 0.1f * r + side * s * 0.05f * r, NagaRed * 0.7f);
        }
        // หงอนเปลวไฟบนหัว: สามแฉกโค้งไปข้างหลัง + เคราใต้คาง
        for (int c = -1; c <= 1; c++)
        {
            var b = p + up * 0.75f * r + side * c * 0.35f * r - fwd * 0.2f * r;
            var tip = b + up * (1.5f - Mathf.Abs(c) * 0.4f) * r - fwd * 1.0f * r + side * c * 0.4f * r;
            bld.TwoSided(0, b - fwd * 0.4f * r, tip, b + fwd * 0.35f * r, NagaGoldDark);
            bld.TwoSided(0, b, tip + up * 0.2f * r - fwd * 0.3f * r, b - fwd * 0.6f * r, NagaGold);
        }
        bld.TwoSided(0, p + fwd * 1.0f * r - up * 0.4f * r, p + fwd * 0.4f * r - up * 1.3f * r, p + fwd * 0.1f * r - up * 0.4f * r, NagaGoldDark);
    }

    // ต้นไม้ใบกว้างริมโขง (ป่าเบญจพรรณ): ลำต้นแตกกิ่ง + พุ่มใบเป็นก้อนเมฆหลายก้อน · submesh 0 = ลำต้น, 1 = ใบ
    public static Mesh Broadleaf(int seed, float height)
    {
        var rnd = new System.Random(seed); float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var bld = new Builder(2);
        void Branch(Vector3 a, Vector3 b, float r0, float r1)
        {
            const int S = 6; var f = (b - a).normalized; var sd = Vector3.Cross(Vector3.up + new Vector3(0.01f, 0, 0), f).normalized; var uu = Vector3.Cross(f, sd);
            var r0i = new int[S + 1]; var r1i = new int[S + 1];
            for (int k = 0; k <= S; k++) { float an = k / (float)S * Mathf.PI * 2; var o = sd * Mathf.Sin(an) + uu * Mathf.Cos(an); r0i[k] = bld.Add(a + o * r0, new Color(0.36f, 0.27f, 0.2f)); r1i[k] = bld.Add(b + o * r1, new Color(0.42f, 0.32f, 0.24f)); }
            for (int k = 0; k < S; k++) bld.Quad(0, r0i[k], r1i[k], r0i[k + 1], r1i[k + 1]);
        }
        var top = new Vector3(R(-0.4f, 0.4f), height * 0.55f, R(-0.4f, 0.4f));
        Branch(Vector3.zero, top, height * 0.04f, height * 0.025f);
        var blobs = new List<(Vector3 c, float r)>();
        int nb = rnd.Next(4, 7);
        for (int b = 0; b < nb; b++)
        {
            float az = R(0, Mathf.PI * 2), out_ = R(0.15f, 0.32f) * height;
            var end = top + new Vector3(Mathf.Cos(az) * out_, R(0.12f, 0.32f) * height, Mathf.Sin(az) * out_);
            Branch(top, end, height * 0.022f, height * 0.01f);
            blobs.Add((end, R(0.16f, 0.24f) * height));
        }
        blobs.Add((top + Vector3.up * height * 0.3f, 0.26f * height));
        foreach (var (c, r) in blobs)   // ก้อนใบ: ทรงกลมหยัก (icosphere หยาบ + สุ่มนูน)
        {
            const int La = 6, Lo = 9; var id = new int[La + 1, Lo + 1];
            var baseC = Color.Lerp(new Color(0.16f, 0.3f, 0.1f), new Color(0.3f, 0.42f, 0.14f), R(0, 1));
            for (int i = 0; i <= La; i++) for (int j = 0; j <= Lo; j++)
            {
                float th = i / (float)La * Mathf.PI, ph = j / (float)Lo * Mathf.PI * 2;
                var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th) * 0.75f, Mathf.Sin(th) * Mathf.Sin(ph));
                float bump = 1 + 0.18f * Mathf.Sin(ph * 3 + seed) * Mathf.Sin(th * 4);
                id[i, j] = bld.Add(c + d * r * bump, baseC * (0.65f + 0.45f * (d.y * 0.5f + 0.5f)));
            }
            for (int i = 0; i < La; i++) for (int j = 0; j < Lo; j++) bld.Quad(1, id[i, j], id[i, j + 1], id[i + 1, j], id[i + 1, j + 1]);
        }
        return bld.Build("Broadleaf");
    }

    // เรือหางยาวริมโขง: ลำเรียวยาว หัวเชิดสูง ทาสีน้ำเงินคาดแดง เครื่องยนต์ท้าย + ก้านใบพัดยาว (ส่วนหนึ่งเป็นสีขาวตามจุดยอด)
    public static Mesh LongtailBoat(float length)
    {
        var bld = new Builder(1);
        const int N = 22, M = 8; var idx = new int[N + 1, M + 1];
        for (int i = 0; i <= N; i++)
        {
            float t = i / (float)N, z = (t - 0.5f) * length;
            float sn = Mathf.Max(0, Mathf.Sin(Mathf.Pow(t, 0.8f) * Mathf.PI));
            float w = 0.55f * Mathf.Pow(sn, 0.5f) + 0.01f, d = 0.45f * Mathf.Pow(sn, 0.4f);
            float sheer = 0.06f + Mathf.Pow(Mathf.Max(0, t - 0.75f) * 4f, 2.2f) * 0.9f;            // หัวเรือเชิดสูง
            for (int j = 0; j <= M; j++)
            {
                float a = (j / (float)M - 0.5f) * Mathf.PI;
                float hgt = sheer - Mathf.Cos(a) * d;
                var c = hgt > sheer - 0.1f ? new Color(0.72f, 0.14f, 0.1f) : new Color(0.12f, 0.3f, 0.55f);   // คาดแดงขอบบน ลำเรือน้ำเงิน
                idx[i, j] = bld.Add(new Vector3(Mathf.Sin(a) * w, hgt, z), c);
            }
        }
        for (int i = 0; i < N; i++) for (int j = 0; j < M; j++) bld.Quad(0, idx[i, j], idx[i + 1, j], idx[i, j + 1], idx[i + 1, j + 1]);
        // เครื่องยนต์ + ก้านหางยาว
        var eng = new Vector3(0, 0.35f, -length * 0.5f - 0.2f); var gray = new Color(0.3f, 0.3f, 0.32f);
        bld.TwoSided(0, eng + new Vector3(-0.2f, -0.15f, 0), eng + new Vector3(0.2f, -0.15f, 0), eng + new Vector3(0, 0.25f, 0), gray);
        bld.TwoSided(0, eng + new Vector3(0, -0.15f, 0.2f), eng + new Vector3(0, -0.15f, -0.2f), eng + new Vector3(0, 0.25f, 0), gray);
        var tail0 = eng + new Vector3(0, -0.1f, -0.1f); var tail1 = eng + new Vector3(0, -0.75f, -2.4f);
        bld.TwoSided(0, tail0 + Vector3.right * 0.04f, tail0 - Vector3.right * 0.04f, tail1, new Color(0.75f, 0.75f, 0.72f));
        bld.TwoSided(0, tail0 + Vector3.up * 0.04f, tail0 - Vector3.up * 0.04f, tail1, new Color(0.75f, 0.75f, 0.72f));
        return bld.Build("Longtail");
    }
}
