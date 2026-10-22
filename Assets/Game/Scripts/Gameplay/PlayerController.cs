using UnityEngine;

namespace CozyFishing
{
    // ตัวละครชาวประมงหมวกงอบ: เดินบนพื้นตามฟังก์ชันความสูงของด่าน + ท่าทางแบบขยับแขนขา (เทียบ updatePlayer ของเวอร์ชันเว็บ)
    public class PlayerController : MonoBehaviour
    {
        public Transform legL, legR, armL, armR, head, rodTip;
        public float speed = 3.6f;
        public float Yaw;
        public Vector3 Pos;
        public bool CanWalk = true, CanTurn = true;
        public float ArmRTarget;                 // ฝั่ง FishingController ตั้งท่ามือขวา (เหวี่ยง/ถือคัน/ดึง)
        public float ArmLTarget = float.NaN;
        IWorld world;
        float vel, phase, armRX, armLX;

        public Vector3 Forward => new Vector3(Mathf.Sin(Yaw * Mathf.Deg2Rad), 0, Mathf.Cos(Yaw * Mathf.Deg2Rad));

        public void Init(IWorld w)
        {
            world = w;
            Pos = w.Spawn; Pos.y = w.StandY(Pos.x, Pos.z);
            Yaw = w.SpawnYaw;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var mv = GameInput.Move;
            if (mv.sqrMagnitude > 0.0025f && CanTurn)
            {
                float want = Mathf.Atan2(mv.x, mv.y) * Mathf.Rad2Deg;
                Yaw = Mathf.LerpAngle(Yaw, want, Mathf.Min(1, dt * (CanWalk ? 12 : 5)));
            }
            float target = CanWalk ? speed * mv.magnitude : 0;
            vel += (target - vel) * Mathf.Min(1, dt * 10);
            if (vel > 0.01f)
            {
                var f = Forward * vel * dt;
                float nx = Pos.x + f.x, nz = Pos.z + f.z;
                if (world.Walkable(nx, nz)) { Pos.x = nx; Pos.z = nz; }
                else if (world.Walkable(nx, Pos.z)) Pos.x = nx;
                else if (world.Walkable(Pos.x, nz)) Pos.z = nz;
                else vel = 0;
            }
            Pos.y += (world.StandY(Pos.x, Pos.z) - Pos.y) * Mathf.Min(1, dt * 15);

            float walk = Mathf.Min(1, vel / 3);
            phase += dt * vel * 2.8f;
            float sw = Mathf.Sin(phase) * 40f * walk;
            legL.localRotation = Quaternion.Euler(sw, 0, 0);
            legR.localRotation = Quaternion.Euler(-sw, 0, 0);
            float armLTo = float.IsNaN(ArmLTarget) ? -sw * 0.8f : ArmLTarget;
            armLX += (armLTo - armLX) * Mathf.Min(1, dt * 10);
            armL.localRotation = Quaternion.Euler(armLX, 0, 0);
            float armRTo = CanWalk ? sw * 0.3f + ArmRTarget : ArmRTarget;
            armRX += (armRTo - armRX) * Mathf.Min(1, dt * 14);
            armR.localRotation = Quaternion.Euler(armRX, 0, 0);

            transform.position = Pos + Vector3.up * Mathf.Abs(Mathf.Sin(phase)) * 0.06f * walk;
            transform.rotation = Quaternion.Euler(0, Yaw, 0);
        }

    }
}
