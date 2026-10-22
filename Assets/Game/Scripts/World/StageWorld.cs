using System.Collections.Generic;
using UnityEngine;

namespace CozyFishing
{
    // ด่านทั่วไป: ความสูงพื้นอบไว้เป็นตาราง (SceneBuilder สร้างจากฟังก์ชันของแต่ละด่าน) + ระดับน้ำ + ท่าน้ำ/หินที่ยืนได้ + สิ่งกีดขวาง
    // ใช้แทนการเขียนคลาสโลกใหม่ทุกด่าน — ตรรกะเดินได้/เหวี่ยงได้เหมือนบ่อทุ่งนา
    public class StageWorld : MonoBehaviour, IWorld
    {
        public string stageId = "rapids";
        public float waterY = -0.3f;
        // ตารางความสูง: จุด (i, j) อยู่ที่ origin + (i, j) * cell
        public Vector2 origin; public float cell = 0.5f; public int nx, nz;
        public float[] heights;
        public List<Rect> docks = new List<Rect>();          // พื้นที่ยืนได้เหนือน้ำ (ท่าไม้ แพ หินแบน)
        public List<float> dockY = new List<float>();
        public List<Vector4> colliders = new List<Vector4>(); // x, z, รัศมี, ความสูง
        public Rect walkBounds = new Rect(-20, -15, 40, 35);
        public Vector3 spawn; public float spawnYaw;
        public Vector3 special; public float specialRadius = 3;
        public Rect camBounds = new Rect(-10, -8, 20, 18);
        public Vector2 camBias; public float camBiasK = 0.4f;   // กล้องเอนไปทางจุดน้ำหลักของด่าน

        public string StageId => stageId;
        public float WaterY => waterY;
        public Vector3 Spawn => spawn;
        public float SpawnYaw => spawnYaw;
        public Vector3 Special => special;
        public float SpecialRadius => specialRadius;
        public Rect CamBounds => camBounds;
        public Vector2 CamFocus(Vector3 p) => Vector2.Lerp(new Vector2(p.x, p.z), camBias, camBiasK);

        public float GroundH(float x, float z)
        {
            if (heights == null || heights.Length == 0) return 0;
            float fx = Mathf.Clamp((x - origin.x) / cell, 0, nx - 1.001f), fz = Mathf.Clamp((z - origin.y) / cell, 0, nz - 1.001f);
            int i = (int)fx, j = (int)fz; float u = fx - i, v = fz - j;
            float h00 = heights[j * nx + i], h10 = heights[j * nx + i + 1], h01 = heights[(j + 1) * nx + i], h11 = heights[(j + 1) * nx + i + 1];
            return Mathf.Lerp(Mathf.Lerp(h00, h10, u), Mathf.Lerp(h01, h11, u), v);
        }

        int DockAt(float x, float z) { for (int i = 0; i < docks.Count; i++) if (docks[i].Contains(new Vector2(x, z))) return i; return -1; }
        public bool OnSolid(float x, float z) => DockAt(x, z) >= 0;
        bool InWater(float x, float z) => GroundH(x, z) < waterY - 0.02f;
        public bool CanCast(float x, float z) => InWater(x, z) && !OnSolid(x, z);
        public float DepthAt(float x, float z) => waterY - GroundH(x, z);
        public float StandY(float x, float z) { int d = DockAt(x, z); return d >= 0 ? dockY[d] : Mathf.Max(GroundH(x, z), waterY); }

        public bool Walkable(float x, float z, float hop = 0)
        {
            if (!walkBounds.Contains(new Vector2(x, z))) return false;
            if (OnSolid(x, z)) return true;
            if (GroundH(x, z) < waterY + 0.05f) return false;
            foreach (var c in colliders) if (hop < c.w - 0.15f && (x - c.x) * (x - c.x) + (z - c.y) * (z - c.y) < c.z * c.z) return false;
            return true;
        }
    }
}
