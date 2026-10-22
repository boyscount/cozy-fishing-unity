using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozyFishing
{
    // ต้นข้าวในแปลงนา + กอหญ้าบนคันดิน วาดด้วย GPU instancing (เทียบ InstancedMesh ของเวอร์ชันเว็บ)
    public class FoliageRenderer : MonoBehaviour
    {
        public Material material;        // CozyFishing/Toon + Use Vertex Color + Sway + Enable GPU Instancing
        public int seed = 808;
        public int maxGrass = 9000;
        Mesh riceMesh, grassMesh;
        readonly List<Matrix4x4[]> riceBatches = new List<Matrix4x4[]>(), grassBatches = new List<Matrix4x4[]>();
        RenderParams rp;

        void Start()
        {
            bool mobile = Application.isMobilePlatform;
            riceMesh = Tuft(4, 0.06f, 0.6f, new Color(0.31f, 0.6f, 0.23f), new Color(0.78f, 0.88f, 0.44f));
            grassMesh = Tuft(3, 0.05f, 0.42f, new Color(0.37f, 0.6f, 0.21f), new Color(0.77f, 0.89f, 0.48f));
            var rnd = new System.Random(seed);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var rice = new List<Matrix4x4>();
            float step = mobile ? 0.6f : 0.42f;
            for (float x = -30; x < 30; x += step)
                for (float z = -18; z < 32; z += step)
                {
                    float px = x + R(-0.06f, 0.06f), pz = z + R(-0.06f, 0.06f);
                    if (PaddyWorld.Paddy(px, pz) < 0.9f || PaddyWorld.DikeAt(px, pz) > 0.3f) continue;
                    if (Mathf.Abs(px - 1.5f) < 2.2f && pz < -3) continue;          // ทางเดินเข้าบ่อ
                    float s = R(1.2f, 1.6f);
                    rice.Add(Matrix4x4.TRS(new Vector3(px, PaddyWorld.PaddyY - 0.06f, pz), Quaternion.Euler(0, R(0, 360), 0), new Vector3(s, s * R(0.85f, 1.2f), s)));
                }
            var grass = new List<Matrix4x4>();
            int target = mobile ? maxGrass / 2 : maxGrass;
            for (int t = 0; t < target * 3 && grass.Count < target; t++)
            {
                float x = R(-24, 24), z = R(-16, 26);
                float h = PaddyWorld.Ground(x, z);
                if (h < PaddyWorld.Water + 0.12f || PaddyWorld.Paddy(x, z) > 0.3f) continue;
                float s = R(0.7f, 1.35f);
                grass.Add(Matrix4x4.TRS(new Vector3(x, h - 0.02f, z), Quaternion.Euler(0, R(0, 360), 0), new Vector3(s, s * R(0.8f, 1.3f), s)));
            }
            Split(rice, riceBatches); Split(grass, grassBatches);
            rp = new RenderParams(material) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true, worldBounds = new Bounds(Vector3.zero, Vector3.one * 200) };
        }

        static void Split(List<Matrix4x4> all, List<Matrix4x4[]> batches)
        {
            for (int i = 0; i < all.Count; i += 1023) batches.Add(all.GetRange(i, Mathf.Min(1023, all.Count - i)).ToArray());
        }

        void Update()
        {
            foreach (var b in riceBatches) Graphics.RenderMeshInstanced(rp, riceMesh, 0, b);
            foreach (var b in grassBatches) Graphics.RenderMeshInstanced(rp, grassMesh, 0, b);
        }

        // กอใบแคบ ๆ หลายใบ สีเข้มที่โคน อ่อนที่ปลาย (normal ชี้ขึ้นให้แสงสม่ำเสมอแบบการ์ตูน)
        static Mesh Tuft(int blades, float w, float h, Color root, Color tip)
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var n = new List<Vector3>(); var tri = new List<int>();
            for (int b = 0; b < blades; b++)
            {
                var rot = Quaternion.Euler(0, b * 180f / blades + 15, (b - (blades - 1) / 2f) * 10f);
                int i0 = v.Count;
                Vector3[] p = { new Vector3(-w, 0, 0), new Vector3(w, 0, 0), new Vector3(-w * 0.6f, h * 0.55f, 0.05f), new Vector3(w * 0.6f, h * 0.55f, 0.05f), new Vector3(0, h, 0.14f) };
                foreach (var q in p) { v.Add(rot * q); c.Add(Color.Lerp(root, tip, q.y / h)); n.Add(Vector3.up); }
                tri.AddRange(new[] { i0, i0 + 2, i0 + 1, i0 + 1, i0 + 2, i0 + 3, i0 + 2, i0 + 4, i0 + 3, i0 + 1, i0 + 2, i0, i0 + 3, i0 + 2, i0 + 1, i0 + 3, i0 + 4, i0 + 2 });
            }
            var m = new Mesh { name = "Tuft" };
            m.SetVertices(v); m.SetColors(c); m.SetNormals(n); m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
