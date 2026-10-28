using UnityEngine;

namespace CozyFishing
{
    // ตัวละครชาวประมงหมวกงอบ: เดินบนพื้นตามฟังก์ชันความสูงของด่าน (เทียบ updatePlayer ของเวอร์ชันเว็บ)
    // มี animator (ตัวละคร Mixamo) → ใช้ท่าจริงตาม Speed/Pose · ไม่มี → ขยับแขนขาโมเดล low-poly แบบเดิม
    public class PlayerController : MonoBehaviour
    {
        public Transform legL, legR, armL, armR, head, rodTip;
        public Animator animator;
        public int Pose;                         // FishingController ตั้ง: 0 เดิน/ยืน, 1 ท่าเหวี่ยง/กรอสาย (คุมด้วย CastT), 3 ถือคันรอปลา
        public float CastT;                      // ตำแหน่งในท่า "เหวี่ยงเบ็ด+กรอสาย" (0..1) — ดู CharacterSetup
        public float jumpSpeed = 5.6f, gravity = 15f;
        public bool Airborne => hop > 0.001f || jumpVel > 0;
        static readonly int SpeedId = Animator.StringToHash("Speed"), PoseId = Animator.StringToHash("Pose"), CastId = Animator.StringToHash("CastT");
        float hop, jumpVel;
        public float speed = 3.6f;
        public float Yaw;
        public Vector3 Pos;
        public bool CanWalk = true, CanTurn = true;
        public bool Riding;                      // ยืนบนแพ (Raft.cs วางตำแหน่งให้ทุกเฟรม) · ไม่เดินเอง หันได้
        public float ArmRTarget;                 // ฝั่ง FishingController ตั้งท่ามือขวา (เหวี่ยง/ถือคัน/ดึง)
        public float ArmLTarget = float.NaN;
        IWorld world;
        float vel, phase, armRX, armLX;

        public static float JumpLeadIn = 0.72f;  // ข้ามช่วงย่อตัวของท่ากระโดด Mixamo (เท้าพ้นพื้นที่ 0.75 วิ ลงพื้น 1.42 วิ) ให้ตัวลอยทันทีที่กด

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
                float want = Mathf.Atan2(mv.x, mv.y) * Mathf.Rad2Deg + CameraRig.ViewYaw;   // ทิศจอยอิงมุมกล้อง
                Yaw = Mathf.LerpAngle(Yaw, want, Mathf.Min(1, dt * (CanWalk ? 12 : 5)));
            }
            // กระโดด (เฉพาะตอนเดินเล่น ไม่ใช่ระหว่างตกปลา)
            if (GameInput.JumpPressed && CanWalk && !Airborne && !Riding)
            {
                jumpVel = jumpSpeed;
                if (animator != null) animator.CrossFadeInFixedTime("Jump", 0.08f, 0, JumpLeadIn);
            }
            if (Airborne)
            {
                hop += jumpVel * dt; jumpVel -= gravity * dt;
                if (hop <= 0) { hop = 0; jumpVel = 0; }
            }
            float target = CanWalk && !Riding ? speed * mv.magnitude : 0;
            vel += (target - vel) * Mathf.Min(1, dt * 10);
            if (vel > 0.01f)
            {
                var f = Forward * vel * dt;
                float nx = Pos.x + f.x, nz = Pos.z + f.z;
                bool stuck = !world.Walkable(Pos.x, Pos.z, hop);   // ตกลงมาบนหิน → ให้เดินออกได้
                if (stuck || world.Walkable(nx, nz, hop)) { Pos.x = nx; Pos.z = nz; }
                else if (world.Walkable(nx, Pos.z, hop)) Pos.x = nx;
                else if (world.Walkable(Pos.x, nz, hop)) Pos.z = nz;
                else vel = 0;
            }
            if (!Riding) Pos.y += (world.StandY(Pos.x, Pos.z) - Pos.y) * Mathf.Min(1, dt * 15);

            if (animator != null)
            {
                animator.SetFloat(SpeedId, vel * 0.6f, 0.1f, dt);   // ท่าเดิน Mixamo ก้าวช้ากว่าความเร็วเกม เล่นเร็วขึ้นตาม blend
                animator.SetInteger(PoseId, Pose);
                animator.SetFloat(CastId, CastT);
                animator.speed = Pose == 0 && !Airborne ? Mathf.Lerp(1f, 1.5f, Mathf.InverseLerp(1.5f, speed, vel)) : 1f;
                transform.position = Pos;   // ความสูงกระโดดอยู่ในท่า Mixamo แล้ว (hop ใช้เช็กข้ามหินเท่านั้น)
                transform.rotation = Quaternion.Euler(0, Yaw, 0);
                return;
            }

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
