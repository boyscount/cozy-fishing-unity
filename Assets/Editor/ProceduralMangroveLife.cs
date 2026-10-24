using System.Collections.Generic;
using UnityEngine;

// ของประกอบป่าชายเลน: กอรากโกงกางพันกันหนา (ไม่มีลำต้น) · เปลือกหอย · ปูก้ามดาบ · ปลาตีน · กะโหลกสัตว์ · ขยะติดราก · ใบไม้/ฟองลอยน้ำ · ข้องกับถังน้ำ
public static partial class ProceduralMeshes
{
    static readonly Color Mud = new Color(0.33f, 0.28f, 0.21f), MudDry = new Color(0.52f, 0.47f, 0.4f);

    // กอรากค้ำยันโค้งพันกันเป็นกรงริมตลิ่ง: รากหลายสิบเส้นจากตอสั้น ๆ โค้งขึ้นแล้วดิ่งปักเลน แตกแขนงซ้อน · ปลายรากเปื้อนโคลน (คราบแห้งสีซีดช่วงกลาง)
    public static Mesh MangroveRoots(int seed, float spread)
    {
        var rnd = new System.Random(seed);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var bld = new Builder(1);
        Color bark = new Color(0.5f, 0.44f, 0.36f);
        int stubs = rnd.Next(2, 4);
        for (int s = 0; s < stubs; s++)
        {
            var hub = new Vector3(R(-spread, spread) * 0.4f, R(0.9f, 1.8f), R(-spread, spread) * 0.4f);
            Tube(bld, 0, new[] { hub + Vector3.down * 0.2f, hub + Vector3.up * 0.5f, hub + new Vector3(R(-0.3f, 0.3f), 1.1f, R(-0.3f, 0.3f)) }, 0.13f, 0.06f, bark, bark * 0.9f, 6);
            int roots = rnd.Next(12, 18);
            for (int k = 0; k < roots; k++)
            {
                float az = k / (float)roots * Mathf.PI * 2 + R(-0.4f, 0.4f), reach = R(0.9f, spread);
                var dir = new Vector3(Mathf.Cos(az), 0, Mathf.Sin(az));
                Vector3 p0 = hub + Vector3.up * R(-0.1f, 0.5f), p1 = hub + dir * reach * R(0.25f, 0.55f) + Vector3.up * R(0.2f, 0.9f), p2 = dir * reach + new Vector3(hub.x, -0.45f, hub.z);
                var pts = new List<Vector3>();
                for (int i = 0; i <= 12; i++)
                {
                    float t = i / 12f; var p = (1 - t) * (1 - t) * p0 + 2 * t * (1 - t) * p1 + t * t * p2;
                    p += new Vector3(Mathf.Sin(t * 9 + k), 0, Mathf.Cos(t * 7 + k)) * 0.05f;   // รากบิดเป็นปุ่มปม
                    pts.Add(p);
                }
                RootTube(bld, pts, R(0.05f, 0.085f), bark, rnd);
                if (rnd.NextDouble() < 0.7)   // แขนงย่อย พันไขว้กับรากข้าง ๆ
                {
                    var m = pts[rnd.Next(4, 9)]; float az2 = az + R(-1f, 1f); var d2 = new Vector3(Mathf.Cos(az2), 0, Mathf.Sin(az2));
                    Vector3 q1 = m + d2 * R(0.3f, 0.7f) + Vector3.up * R(0f, 0.3f), q2 = m + d2 * R(0.9f, 1.7f); q2.y = -0.45f;
                    var ps = new List<Vector3>();
                    for (int i = 0; i <= 8; i++) { float t = i / 8f; ps.Add((1 - t) * (1 - t) * m + 2 * t * (1 - t) * q1 + t * t * q2); }
                    RootTube(bld, ps, 0.04f, bark, rnd);
                }
            }
        }
        return bld.Build("MangroveRoots");
    }

    // รากหนึ่งเส้น: เปลือกด้านบน → คราบโคลนแห้งซีดช่วงกลาง → โคลนเปียกเข้มที่ปลายจุ่มเลน
    static void RootTube(Builder bld, IList<Vector3> pts, float r, Color bark, System.Random rnd)
    {
        int prevStart = -1; const int sides = 7;   // 7 ด้าน: มองใกล้จากใต้น้ำไม่เป็นแท่งเหลี่ยม
        for (int i = 0; i < pts.Count; i++)
        {
            float t = i / (float)(pts.Count - 1);
            var tg = (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
            var a = Vector3.Cross(tg, Mathf.Abs(tg.y) > 0.9f ? Vector3.right : Vector3.up).normalized; var b = Vector3.Cross(tg, a);
            float rr = r * Mathf.Lerp(1f, 0.7f, t) * (1 + 0.15f * Mathf.Sin(i * 2.3f));
            float h = pts[i].y;
            var col = h < 0.15f ? Color.Lerp(Mud, MudDry, Mathf.Clamp01((h + 0.4f) / 0.55f)) : Color.Lerp(MudDry, bark, Mathf.Clamp01((h - 0.15f) / 0.5f));
            int start = bld.v.Count;
            for (int k = 0; k <= sides; k++) { float an = k / (float)sides * Mathf.PI * 2; bld.Add(pts[i] + (a * Mathf.Cos(an) + b * Mathf.Sin(an)) * rr, col); }
            if (prevStart >= 0) for (int k = 0; k < sides; k++) bld.Quad(0, prevStart + k, start + k, prevStart + k + 1, start + k + 1);
            prevStart = start;
        }
    }

    // ทรงรี (ใช้ทำตัวปู เปลือกหอย ทุ่น ฯลฯ): lathe ครึ่งวงกลมแล้วย่อ/ขยายแกน
    public static Mesh Blob(Color col, int segs = 8, int rings = 5)
    {
        var prof = new Vector2[rings + 1];
        for (int j = 0; j <= rings; j++) { float a = j / (float)rings * Mathf.PI; prof[j] = new Vector2(Mathf.Sin(a) * 0.5f, -Mathf.Cos(a) * 0.5f); }
        prof[0].x = 0.001f; prof[rings].x = 0.001f;
        return Lathe(prof, segs, col);
    }

    // ข้องไม้ไผ่สาน: ป่องกลาง คอคอด ปากบานเล็กน้อย
    public static Mesh FishBasket()
    {
        var c = new Color(0.66f, 0.55f, 0.34f);
        return Lathe(new[] { new Vector2(0.001f, 0), new Vector2(0.12f, 0.01f), new Vector2(0.17f, 0.1f), new Vector2(0.16f, 0.22f), new Vector2(0.08f, 0.32f), new Vector2(0.06f, 0.36f), new Vector2(0.075f, 0.4f) }, 12, c);
    }

    // ถังสังกะสีบุบ
    public static Mesh Bucket()
    {
        var c = new Color(0.55f, 0.56f, 0.56f);
        return Lathe(new[] { new Vector2(0.001f, 0), new Vector2(0.12f, 0), new Vector2(0.13f, 0.02f), new Vector2(0.15f, 0.26f), new Vector2(0.155f, 0.28f), new Vector2(0.14f, 0.28f), new Vector2(0.125f, 0.03f) }, 12, c, 0.3f);
    }
}
