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
