using UnityEngine;

// หินงอกหินย้อยรูปทรงธรรมชาติ: แกนบิดเล็กน้อย รัศมีขรุขระเป็นปุ่มปม มีชั้นคราบหินปูนพอกเป็นวง ๆ (flowstone) · ปลายแหลมไม่สมมาตร
public static partial class ProceduralMeshes
{
    // down = หินย้อย (ห้อยจากจุดยึดลงล่าง) · ไม่งั้นหินงอก (จากพื้นขึ้นบน) · ฐานจมลงในหิน 0.4 เท่าของรัศมี
    public static Mesh Speleothem(int seed, float len, float r0, bool down, Color c0, Color c1)
    {
        var rnd = new System.Random(seed);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var bld = new Builder(1);
        const int rings = 14, sides = 10;
        float s = down ? -1 : 1, bendX = R(-0.25f, 0.25f) * len, bendZ = R(-0.25f, 0.25f) * len, ph = R(0, 10);
        int prev = -1;
        for (int j = 0; j <= rings; j++)
        {
            float t = j / (float)rings;
            var axis = new Vector3(bendX * t * t, (t * len - r0 * 0.4f * (1 - t)) * s, bendZ * t * t);
            float taper = Mathf.Pow(1 - t, 1.25f) + 0.02f;
            float band = 1 + 0.18f * Mathf.Max(0, Mathf.Sin(t * 22 + ph)) + 0.08f * Mathf.Sin(t * 47 + ph * 2);   // ชั้นคราบหินปูนพอกเป็นวง
            var col = Color.Lerp(c0, c1, t * 0.8f + R(-0.08f, 0.08f));
            int start = bld.v.Count;
            for (int k = 0; k <= sides; k++)
            {
                float a = k / (float)sides * Mathf.PI * 2;
                float rr = r0 * taper * band * (1 + 0.22f * Mathf.PerlinNoise(a * 1.3f + ph, t * 6 + seed) - 0.11f);   // ขรุขระไม่กลม
                bld.Add(axis + new Vector3(Mathf.Cos(a) * rr, 0, Mathf.Sin(a) * rr * R(0.8f, 1f)), col);
            }
            if (prev >= 0) for (int k = 0; k < sides; k++) { if (down) bld.Quad(0, prev + k + 1, start + k + 1, prev + k, start + k); else bld.Quad(0, prev + k, start + k, prev + k + 1, start + k + 1); }
            prev = start;
        }
        return bld.Build("Speleothem");
    }
}


// ต้นสนเขา (สนสองใบ/สามใบ): ลำต้นสูง กิ่งเป็นชั้น ๆ ปลายกิ่งพุ่มใบเข็มแบนเป็นแพ ยอดไม่สมมาตร — เห็นเป็นเงาตัดฟ้ายามค่ำ
public static partial class ProceduralMeshes
{
    public static Mesh MountainPine(int seed, float height)
    {
        var rnd = new System.Random(seed);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var bld = new Builder(2);
        Color bark0 = new Color(0.32f, 0.24f, 0.18f), bark1 = new Color(0.42f, 0.32f, 0.22f);
        float lx = R(-0.6f, 0.6f), lz = R(-0.6f, 0.6f);
        Vector3 At(float y) => new Vector3(lx * (y / height) * (y / height) * 2, y, lz * (y / height) * (y / height) * 2);
        var trunk = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i <= 10; i++) trunk.Add(At(i / 10f * height));
        Tube(bld, 0, trunk, height * 0.022f, height * 0.006f, bark0, bark1, 7);
        int tiers = rnd.Next(5, 8);
        for (int k = 0; k < tiers; k++)
        {
            float y = height * Mathf.Lerp(0.45f, 0.97f, k / (float)(tiers - 1)), len = height * Mathf.Lerp(0.26f, 0.08f, k / (float)tiers) * R(0.7f, 1.2f);
            int arms = rnd.Next(2, 4);
            for (int a = 0; a < arms; a++)
            {
                float az = R(0, Mathf.PI * 2); var dir = new Vector3(Mathf.Cos(az), R(0.05f, 0.35f), Mathf.Sin(az)).normalized;
                var p0 = At(y); var p1 = p0 + dir * len;
                Tube(bld, 0, new[] { p0, (p0 + p1) * 0.5f + Vector3.up * len * 0.08f, p1 }, height * 0.008f, height * 0.003f, bark0, bark1, 4);
                for (int l = 0; l < 28; l++)
                {
                    var c = p1 + new Vector3(R(-1, 1) * len * 0.45f, R(-0.15f, 0.25f) * len * 0.3f, R(-1, 1) * len * 0.45f);
                    var f = new Vector3(R(-1, 1), R(-0.2f, 0.2f), R(-1, 1)).normalized * len * 0.22f; var s = Vector3.Cross(f, Vector3.up).normalized * len * 0.07f;
                    var col = Color.Lerp(new Color(0.1f, 0.16f, 0.12f), new Color(0.18f, 0.24f, 0.16f), R(0, 1));
                    bld.TwoSided(1, c - f, c + s, c + f, col); bld.TwoSided(1, c - f, c + f, c - s, col);
                }
            }
        }
        return bld.Build("MountainPine");
    }
}


// หลังคาสังกะสีลอนเก่าขึ้นสนิม: สองลาดสันตามแกน Z · ลอนคลื่น · แผ่นทรุดยวบกลาง · คราบสนิมเป็นหย่อม · บางแผ่นหลุดหาย
public static partial class ProceduralMeshes
{
    public static Mesh TinRoof(int seed, float length, float halfW, float rise)
    {
        var rnd = new System.Random(seed);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var bld = new Builder(1);
        int sheets = Mathf.Max(3, Mathf.RoundToInt(length / 0.8f));
        for (int side = -1; side <= 1; side += 2)
            for (int sh = 0; sh < sheets; sh++)
            {
                if (rnd.NextDouble() < 0.1) continue;   // แผ่นหลุดหาย เห็นโครงไม้
                float z0 = -length * 0.5f + sh * length / sheets, z1 = z0 + length / sheets + 0.04f, lift = R(-0.03f, 0.05f);
                const int nu = 14, nv = 5;
                int b0 = bld.v.Count;
                for (int j = 0; j <= nv; j++)
                    for (int i = 0; i <= nu; i++)
                    {
                        float u = i / (float)nu, t = j / (float)nv, z = Mathf.Lerp(z0, z1, u);
                        float x = side * halfW * t, y = rise * (1 - t) + lift;
                        y -= Mathf.Sin(t * Mathf.PI) * 0.12f * Mathf.Sin(u * Mathf.PI) * R(0.5f, 1.5f);   // แผ่นยวบ
                        y += Mathf.Sin(u * Mathf.PI * 9) * 0.025f;                                        // ลอนคลื่น
                        float n = Mathf.PerlinNoise(z * 1.7f + seed, x * 1.7f + side * 5), rustK = Mathf.Clamp01((n - 0.35f) * 2.2f);
                        var col = Color.Lerp(new Color(0.55f, 0.56f, 0.55f), new Color(0.46f, 0.24f, 0.12f), rustK) * R(0.85f, 1f);
                        bld.Add(new Vector3(x, y, z), col);
                    }
                for (int j = 0; j < nv; j++)
                    for (int i = 0; i < nu; i++) { int a = b0 + j * (nu + 1) + i, c = a + nu + 1; bld.Quad(0, a, c, a + 1, c + 1); }
            }
        return bld.Build("TinRoof");
    }
}
