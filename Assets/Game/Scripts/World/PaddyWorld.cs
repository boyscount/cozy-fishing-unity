using System.Collections.Generic;
using UnityEngine;

namespace CozyFishing
{
    // ด่านบ่อปลาทุ่งนา: บ่อใหญ่กลางทุ่งข้าว (ย้ายฟังก์ชันความสูงมาจาก stages/paddy.js ของเวอร์ชันเว็บ)
    // พิกัด: เวอร์ชันเว็บใช้ระบบมือขวา (three.js) → Unity กลับแกน z (zUnity = -zWeb) เพื่อให้มุมกล้องเหมือนเดิม
    public class PaddyWorld : MonoBehaviour, IWorld
    {
        public static readonly Vector2 Pond = new Vector2(0, 6);     // (x, zUnity)
        public const float Water = -0.3f, PaddyY = -0.12f, Dike = 2.6f;
        public Rect dock = new Rect(-3.92f, -0.1f, 1.44f, 4.3f);       // พื้นที่ท่าไม้ (x, z, w, h) — SceneBuilder ตั้งค่าตามจริง
        public float dockY = 0.27f;
        public List<Vector4> colliders = new List<Vector4>();          // x, z, รัศมี, ความสูง (หินเตี้ยกระโดดข้ามได้)

        public string StageId => "paddy";
        public float WaterY => Water;
        public Vector3 Spawn => new Vector3(1.5f, 0, -7.6f);
        public float SpawnYaw => 0f;
        public Vector3 Special => new Vector3(5.5f, Water, 10.5f);
        public float SpecialRadius => 3f;
        public Rect CamBounds => new Rect(-12, -9, 24, 21);
        public Vector2 CamFocus(Vector3 p) => new Vector2(p.x * 0.75f, p.z * 0.6f + Pond.y * 0.4f);

        static float SStep(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }
        public static float PondR(float th) => 10.5f + 1.3f * Mathf.Sin(2 * th + 0.5f) + 0.8f * Mathf.Sin(3 * th + 1.7f) + 0.4f * Mathf.Sin(5 * th);

        // ฟังก์ชันเดิมรับพิกัดเว็บ (zWeb) — ห่อให้ทุกเมธอดรับพิกัด Unity
        static float PaddyW(float x, float zw)
        {
            float dx = x - Pond.x, dz = zw + Pond.y, d = Mathf.Sqrt(dx * dx + dz * dz);
            return SStep(3.2f, 4.4f, d - PondR(Mathf.Atan2(dz, dx)));
        }
        static float DikeW(float x, float zw)
        {
            float fx = Mathf.Abs(((x + 100) % Dike) - Dike / 2), fz = Mathf.Abs(((zw + 100) % (Dike * 1.3f)) - Dike * 0.65f);
            return Mathf.Max(SStep(Dike / 2 - 0.22f, Dike / 2 - 0.08f, fx), SStep(Dike * 0.65f - 0.22f, Dike * 0.65f - 0.08f, fz));
        }
        static float GroundWeb(float x, float zw)
        {
            float dx = x - Pond.x, dz = zw + Pond.y, d = Mathf.Sqrt(dx * dx + dz * dz);
            float e = PondR(Mathf.Atan2(dz, dx)) - d;
            float h = 0.12f + 0.06f * Mathf.Sin(x * 0.3f) * Mathf.Cos(zw * 0.27f);
            h -= 2.3f * SStep(-0.3f, 4f, e);
            float pw = PaddyW(x, zw);
            if (pw > 0) h = Mathf.Lerp(h, PaddyY - 0.08f + DikeW(x, zw) * 0.32f, pw);
            h += Mathf.Min(5, Mathf.Max(0, -zw - 40) * 0.45f) * (0.6f + 0.4f * Mathf.Sin(x * 0.11f + 1));
            return h;
        }

        public static float Ground(float x, float z) => GroundWeb(x, -z);
        public static float Paddy(float x, float z) => PaddyW(x, -z);
        public static float DikeAt(float x, float z) => DikeW(x, -z);

        public float GroundH(float x, float z) => Ground(x, z);
        public bool OnDock(float x, float z) => dock.Contains(new Vector2(x, z));
        public bool OnSolid(float x, float z) => OnDock(x, z);
        bool InPond(float x, float z) => Ground(x, z) < Water - 0.02f && Paddy(x, z) < 0.5f;
        public bool CanCast(float x, float z) => InPond(x, z) && !OnDock(x, z);
        public float DepthAt(float x, float z) => Water - Ground(x, z);
        public float StandY(float x, float z) => OnDock(x, z) ? dockY : Mathf.Max(Ground(x, z), Paddy(x, z) > 0.5f ? PaddyY - 0.05f : Water);

        public bool Walkable(float x, float z, float hop = 0)
        {
            if (x < -22 || x > 22 || z < -15 || z > 22) return false;
            if (OnDock(x, z)) return true;
            if (Ground(x, z) < Water + 0.05f && Paddy(x, z) < 0.5f) return false;
            foreach (var c in colliders) if (hop < c.w - 0.15f && (x - c.x) * (x - c.x) + (z - c.y) * (z - c.y) < c.z * c.z) return false;
            return true;
        }
    }
}
