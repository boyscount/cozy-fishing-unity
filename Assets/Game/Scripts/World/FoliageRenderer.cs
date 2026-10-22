using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozyFishing
{
    // ต้นข้าวในแปลงนา + กอหญ้าบนดิน วาดด้วย GPU instancing (เทียบ InstancedMesh ของเวอร์ชันเว็บ)
    // ใบสร้างเป็นเส้นโค้งเรียวหลายท่อน หลายแบบสุ่มสลับกัน — shader CozyFishing/Grass ทำลมพัด/สีต่างกอ/แสงทะลุใบ
    public class FoliageRenderer : MonoBehaviour
    {
        public Material material;        // CozyFishing/Grass + Enable GPU Instancing
        public int seed = 808;
        public int maxGrass = 7000;
        public StageWorld stage;          // ด่านทั่วไป: ไม่มีนาข้าว · หญ้าขึ้นบนดินที่ไม่ชันและไม่จมน้ำ
        const int Variants = 3;
        readonly Mesh[] riceMeshes = new Mesh[Variants], grassMeshes = new Mesh[Variants];
        readonly List<Matrix4x4>[] rice = new List<Matrix4x4>[Variants], grass = new List<Matrix4x4>[Variants];
        readonly List<(Mesh mesh, Matrix4x4[] m)> batches = new List<(Mesh, Matrix4x4[])>();
        RenderParams rp;

        void Start()
        {
            bool mobile = Application.isMobilePlatform;
            var rnd = new System.Random(seed);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            for (int i = 0; i < Variants; i++)
            {
                // ข้าว: ใบยาวโค้งออกจากกอแคบ ๆ สีเขียวสด ปลายอมเหลือง
                riceMeshes[i] = Clump(rnd, mobile ? 10 : 13, 0.03f, 0.5f, 0.8f, 0.016f, 0.55f,
                    new Color(0.10f, 0.20f, 0.05f), new Color(0.27f, 0.48f, 0.12f), new Color(0.58f, 0.66f, 0.26f));
                // หญ้า: ใบสั้นถี่ กอกว้าง สีเขียวมะกอก ปลายแห้ง
                grassMeshes[i] = Clump(rnd, mobile ? 10 : 14, 0.13f, 0.14f, 0.34f, 0.008f, 0.35f,
                    new Color(0.12f, 0.17f, 0.06f), new Color(0.30f, 0.40f, 0.14f), new Color(0.62f, 0.60f, 0.32f));
                rice[i] = new List<Matrix4x4>(); grass[i] = new List<Matrix4x4>();
            }
            float step = mobile ? 0.62f : 0.48f;
            bool generic = stage != null;
            if (!generic)
            for (float x = -40; x < 40; x += step)
                for (float z = -26; z < 40; z += step)
                {
                    float px = x + R(-0.1f, 0.1f), pz = z + R(-0.1f, 0.1f);
                    if (PaddyWorld.Paddy(px, pz) < 0.9f || PaddyWorld.DikeAt(px, pz) > 0.3f) continue;
                    float s = R(0.95f, 1.25f);
                    rice[rnd.Next(Variants)].Add(Matrix4x4.TRS(new Vector3(px, PaddyWorld.PaddyY - 0.05f, pz), Quaternion.Euler(0, R(0, 360), 0), new Vector3(s, s * R(0.9f, 1.15f), s)));
                }
            int target = mobile ? maxGrass / 2 : maxGrass;
            for (int t = 0, n = 0; t < target * 3 && n < target; t++)
            {
                var area = generic ? new Rect(stage.walkBounds.x - 6, stage.walkBounds.y - 6, stage.walkBounds.width + 12, stage.walkBounds.height + 12) : new Rect(-26, -18, 52, 46);
                float x = R(area.xMin, area.xMax), z = R(area.yMin, area.yMax);
                float h = generic ? stage.GroundH(x, z) : PaddyWorld.Ground(x, z);
                if (generic)
                {
                    float slope = Mathf.Abs(stage.GroundH(x + 0.6f, z) - h) + Mathf.Abs(stage.GroundH(x, z + 0.6f) - h);
                    if (h < stage.waterY + 0.08f || slope > 0.45f || stage.OnSolid(x, z)) continue;
                }
                else if (h < PaddyWorld.Water + 0.08f || PaddyWorld.Paddy(x, z) > 0.3f) continue;
                float s = R(0.7f, 1.3f);
                grass[rnd.Next(Variants)].Add(Matrix4x4.TRS(new Vector3(x, h - 0.02f, z), Quaternion.Euler(0, R(0, 360), 0), new Vector3(s, s * R(0.8f, 1.3f), s)));
                n++;
            }
            for (int i = 0; i < Variants; i++) { Split(riceMeshes[i], rice[i]); Split(grassMeshes[i], grass[i]); }
            rp = new RenderParams(material) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true, worldBounds = new Bounds(Vector3.zero, Vector3.one * 200) };
        }

        void Split(Mesh mesh, List<Matrix4x4> all)
        {
            for (int i = 0; i < all.Count; i += 1023) batches.Add((mesh, all.GetRange(i, Mathf.Min(1023, all.Count - i)).ToArray()));
        }

        void Update()
        {
            foreach (var (mesh, m) in batches) Graphics.RenderMeshInstanced(rp, mesh, 0, m);
        }

        // กอใบเรียว: แต่ละใบ 3 ท่อน กว้างที่โคนเรียวแหลมที่ปลาย โค้งออกด้านนอกตามน้ำหนัก (arch) และบิดเล็กน้อย
        // สีจุดยอด rgb = สีใบไล่จากโคน→กลาง→ปลาย (ค่า linear), a = ตำแหน่งตามความยาวใบ (ใช้ทำลมพัด/เงาโคนกอ)
        static Mesh Clump(System.Random rnd, int blades, float baseR, float lenMin, float lenMax, float width, float arch, Color root, Color mid, Color tip)
        {
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var v = new List<Vector3>(); var c = new List<Color>(); var n = new List<Vector3>(); var tri = new List<int>();
            const int Seg = 3;
            for (int b = 0; b < blades; b++)
            {
                float ang = R(0, Mathf.PI * 2), r0 = R(0, baseR);
                var dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                var side = new Vector3(-dir.z, 0, dir.x);
                var start = dir * r0;
                float len = R(lenMin, lenMax), lean = R(0.15f, 0.5f), bend = arch * R(0.6f, 1.3f), twist = R(-0.6f, 0.6f);
                float shade = R(0.85f, 1.1f);
                int i0 = v.Count;
                for (int s = 0; s <= Seg; s++)
                {
                    float t = s / (float)Seg;
                    // ทิศใบ: เริ่มตั้งขึ้นเอียงออก แล้วโค้งลงตามความยาว (พาราโบลา)
                    var p = start + dir * (lean * t * len + bend * t * t * len * 0.5f) + Vector3.up * (len * t - bend * t * t * len * 0.35f);
                    var tangent = (dir * (lean + bend * t) + Vector3.up * (1 - bend * t * 0.7f)).normalized;
                    var sd = Quaternion.AngleAxis(twist * 60 * t, tangent) * side;
                    var nor = Vector3.Cross(sd, tangent).normalized;
                    float w = width * (1 - t * 0.85f);
                    var col = (t < 0.5f ? Color.Lerp(root, mid, t * 2) : Color.Lerp(mid, tip, (t - 0.5f) * 2)) * shade;
                    col = col.linear; col.a = t;
                    if (s < Seg) { v.Add(p - sd * w); v.Add(p + sd * w); c.Add(col); c.Add(col); n.Add(nor); n.Add(nor); }
                    else { v.Add(p); c.Add(col); n.Add(nor); }
                }
                for (int s = 0; s < Seg - 1; s++) { int a = i0 + s * 2; tri.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 }); }
                int last = i0 + (Seg - 1) * 2; tri.AddRange(new[] { last, last + 2, last + 1 });
            }
            var m = new Mesh { name = "Clump" };
            m.SetVertices(v); m.SetColors(c); m.SetNormals(n); m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
