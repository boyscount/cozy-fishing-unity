// ต้นสนสามใบบนดอย: ลำต้นตรงเรียวสีน้ำตาลแดง + พุ่มใบเป็นชั้น ๆ ทรงกรวยไม่สมมาตร (กิ่งยื่นแผ่ ปลายห้อยนิด ๆ)
// submesh 0 = ลำต้น, 1 = ใบ · สีจุดยอดใช้กับ CozyFishing/Surface + Use Vertex Color
using UnityEngine;

public static partial class ProceduralMeshes
{
    public static Mesh Pine(int seed, float height)
    {
        var rnd = new System.Random(seed);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var bld = new Builder(2);
        // ลำต้น: ทรงกระบอกเรียว โค้งเอียงนิดหน่อย
        const int Seg = 8, Sides = 7;
        float lean = R(-0.04f, 0.04f), baseR = height * 0.022f;
        var prev = new int[Sides + 1];
        for (int s = 0; s <= Seg; s++)
        {
            float t = s / (float)Seg;
            var c = new Vector3(lean * height * t * t, height * t, 0);
            float r = Mathf.Lerp(baseR, baseR * 0.15f, t);
            var col = Color.Lerp(new Color(0.36f, 0.22f, 0.14f), new Color(0.5f, 0.32f, 0.2f), t);
            var cur = new int[Sides + 1];
            for (int k = 0; k <= Sides; k++) { float a = k / (float)Sides * Mathf.PI * 2; cur[k] = bld.Add(c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r, col); }
            if (s > 0) for (int k = 0; k < Sides; k++) bld.Quad(0, prev[k], cur[k], prev[k + 1], cur[k + 1]);
            prev = cur;
        }
        // พุ่มใบ: วงกิ่งเป็นชั้นจากโคนพุ่ม (~35% ความสูง) ถึงยอด · ชั้นล่างกว้าง ชั้นบนแคบ · แต่ละกิ่งเป็นแผ่นใบหนาสองหน้า ห้อยลงที่ปลาย
        float crownFrom = height * R(0.3f, 0.42f);
        int tiers = Mathf.Max(5, (int)(height * 0.9f));
        for (int t = 0; t < tiers; t++)
        {
            float u = t / (float)(tiers - 1);
            float y = Mathf.Lerp(crownFrom, height * 0.97f, u);
            float reach = Mathf.Lerp(height * 0.24f, height * 0.04f, Mathf.Pow(u, 0.85f)) * R(0.85f, 1.15f);
            int arms = Mathf.Max(4, (int)Mathf.Lerp(9, 4, u)) + rnd.Next(0, 2);
            float spin = R(0, Mathf.PI * 2);
            var cen = new Vector3(lean * height * (y / height) * (y / height), y, 0);
            for (int a = 0; a < arms; a++)
            {
                float az = spin + a / (float)arms * Mathf.PI * 2 + R(-0.25f, 0.25f);
                var dir = new Vector3(Mathf.Cos(az), 0, Mathf.Sin(az));
                float len = reach * R(0.75f, 1.1f), w = len * R(0.42f, 0.55f), droop = len * R(0.18f, 0.35f);
                var side = Vector3.Cross(dir, Vector3.up).normalized;
                var p0 = cen + Vector3.up * len * 0.12f;
                var mid = cen + dir * len * 0.55f + Vector3.down * droop * 0.25f;
                var tip = cen + dir * len + Vector3.down * droop;
                var dark = Color.Lerp(new Color(0.07f, 0.16f, 0.09f), new Color(0.12f, 0.24f, 0.12f), R(0, 1));
                var light = dark * R(1.35f, 1.7f);
                bld.TwoSided(1, p0, mid + side * w * 0.5f, tip, light);
                bld.TwoSided(1, p0, tip, mid - side * w * 0.5f, light);
                bld.TwoSided(1, mid + side * w * 0.5f + Vector3.down * w * 0.18f, tip, mid - side * w * 0.5f + Vector3.down * w * 0.18f, dark);   // ใต้กิ่งเข้มกว่า
            }
        }
        return bld.Build("Pine");
    }
}
