using UnityEngine;

namespace CozyFishing
{
    // มุมบุคคลที่หนึ่ง (แบบเกมตกปลาซิม): เล็ง/เหวี่ยง/ตีเบ็ด/สู้ปลา กล้องอยู่ที่ตาตัวละคร เห็นมือถือคันเบ็ดกับสายพุ่งไปที่ทุ่น
    // ได้ปลา → ยกปลาขึ้นโชว์ตรงหน้า (ห้อยจากสาย หางสะบัด) มือซ้ายเอื้อมจับ · การ์ด "เก็บ/ปล่อย" ย้ายไปด้านล่างจอ
    // รอทุ่นจมใช้ UnderwaterView · เดินเล่นใช้กล้องปกติ · ปิด/เปิดได้ที่ปุ่มรูปตา (จำไว้ใน PlayerPrefs)
    public class FirstPersonView : MonoBehaviour
    {
        const string PrefKey = "abot.fp";
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(PrefKey, 1) == 1;
            set { PlayerPrefs.SetInt(PrefKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }
        public static bool Active { get; private set; }

        FishingController fishing; CameraRig rig; PlayerController player; Hud hud;
        Transform headBone; HandIK ik; Camera cam;
        bool active; Vector3 camPos; Quaternion camRot; float savedNear, savedFov, showT;
        Vector3 showFrom;

        public void Init(FishingController f, CameraRig r)
        {
            fishing = f; rig = r; player = f.player; hud = f.hud;
            cam = r.GetComponent<Camera>();
            if (player.animator != null)
            {
                headBone = player.animator.GetBoneTransform(HumanBodyBones.Head);
                ik = player.animator.GetComponent<HandIK>() ?? player.animator.gameObject.AddComponent<HandIK>();
                ik.rodTip = player.rodTip;
            }
        }

        bool Want()
        {
            if (!Enabled || headBone == null) return false;
            var s = fishing.State;
            // ง้าง (Charging) ใช้กล้องหลังตัวละคร: ท่าง้างยกสองแขนบังหน้า · ปล่อยเหวี่ยงแล้วค่อยเข้ามุมสายตา
            return s == FishState.Casting || s == FishState.Strike || s == FishState.Fight || s == FishState.Caught;
        }

        // ตำแหน่งตา (เหวี่ยงอยู่: ยื่นหน้าอีกนิด ให้แขนที่สะบัดอยู่ข้างหลังกล้อง)
        Vector3 Eye
        {
            get
            {
                var s = fishing.State;
                bool windup = s == FishState.Charging || s == FishState.Casting;
                return headBone.position + player.transform.forward * (windup ? 0.2f : 0.16f) + Vector3.up * 0.05f;
            }
        }

        void Update()
        {
            if (fishing == null) return;
            bool want = Want();
            if (want && !active) Enter(); else if (!want && active) Exit();
            if (!active) return;
            float dt = Time.deltaTime;
            var s = fishing.State;
            var fwd = player.transform.forward; fwd.y = 0; fwd.Normalize();
            Vector3 look = s switch
            {
                FishState.Charging => fishing.AimPoint + Vector3.up * 0.4f,
                FishState.Casting => fishing.CastTarget + Vector3.up * 0.5f,   // มองจุดที่ทุ่นจะตก ไม่แหงนตามทุ่นขึ้นฟ้า
                FishState.Caught => Eye + fwd * 3f + Vector3.down * 0.35f,
                _ => fishing.BobberPos + Vector3.up * 0.25f,
            };
            camPos = Vector3.Lerp(camPos, Eye, Mathf.Min(1, dt * 14));
            var want3 = Quaternion.LookRotation((look - camPos).sqrMagnitude > 1e-4f ? look - camPos : fwd, Vector3.up);
            camRot = Quaternion.Slerp(camRot, want3, Mathf.Min(1, dt * (s == FishState.Fight ? 4f : 7f)));
            // สู้ปลา: กล้องสั่นตามแรงดึงนิด ๆ
            var shake = s == FishState.Fight ? new Vector3(Mathf.PerlinNoise(Time.time * 9, 0) - 0.5f, Mathf.PerlinNoise(0, Time.time * 9) - 0.5f, 0) * 0.012f : Vector3.zero;
            rig.Override(camPos + camRot * shake, camPos + camRot * Vector3.forward, false);

            hud.Showcase3D = s == FishState.Caught;
            if (s == FishState.Caught && fishing.CaughtGo != null) Showcase(dt);
            else if (ik != null) ik.weight = Mathf.MoveTowards(ik.weight, 0, dt * 4);
        }

        // ยกปลาโชว์: ปลาลอยจากผิวน้ำเข้ามาตรงหน้ากล้อง หัวชี้ขึ้น ด้านข้างหันหากล้อง แกว่งเบา ๆ
        void Showcase(float dt)
        {
            var g = fishing.CaughtGo.transform;
            if (!fishing.ExternalShowcase)
            {
                fishing.ExternalShowcase = true; showT = 0; showFrom = g.position;
                g.localScale = Vector3.one * Mathf.Clamp(fishing.CaughtCm / 100f * 1.1f, 0.16f, 1.7f);   // ขนาดจริง (มุมกล้องหลังตัวละครขยายให้เห็นชัด) · ปลายักษ์ยาวเกินแขนจำกัดไว้ไม่ให้ล้นจอ
            }
            showT += dt;
            var c = cam.transform;
            float len = g.lossyScale.x;                                  // โมเดลยาว 1 หน่วย → scale = ความยาวจริง (ม.)
            // แบบเกมตกปลาซิม: มือซ้ายชูสายขึ้นทางซ้ายของจอ ปลาห้อยใต้มือ (แขนมาจากมุมซ้ายล่าง ไม่บังกลางจอ)
            float dist = Mathf.Max(0.6f, len * 1.2f);
            var hand = c.position + c.forward * dist - c.right * (0.2f + len * 0.15f) + c.up * 0.1f;
            var hold = hand - c.up * (len * 0.5f + 0.05f);               // กลางตัวปลาอยู่ใต้มือครึ่งตัว
            float k = Mathf.SmoothStep(0, 1, Mathf.Min(1, showT / 0.6f));
            g.position = Vector3.Lerp(showFrom, hold, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.4f;
            // หัวปลา (+X ของโมเดล) ชี้ขึ้น · หน้า (+Z) หันเข้าหากล้อง · แกว่งรอบแกนมอง + บิดตัวช้า ๆ
            var baseRot = Quaternion.LookRotation(-c.forward, c.right);
            g.rotation = Quaternion.AngleAxis(Mathf.Sin(showT * 2.3f) * 9f, c.forward) * Quaternion.AngleAxis(Mathf.Sin(showT * 1.3f) * 22f, c.up) * baseRot;
            if (ik != null)
            {
                ik.target = Vector3.Lerp(g.TransformPoint(0.5f, 0, 0), hand, 0.5f) + c.up * 0.04f;   // มือซ้ายกำสายเหนือปากปลา
                ik.weight = Mathf.MoveTowards(ik.weight, k, dt * 3);
            }
        }

        void LateUpdate()
        {
            // หลัง Animator: ย่อหัวให้เล็กจนมองไม่เห็น (ผม หมวก ติดไปด้วย) ไม่บังกล้องที่ตา
            if (active && headBone != null) headBone.localScale = Vector3.one * 0.001f;
        }

        void Enter()
        {
            active = Active = true;
            camPos = Eye; camRot = cam.transform.rotation;
            savedNear = cam.nearClipPlane; savedFov = cam.fieldOfView;
            cam.nearClipPlane = 0.03f; cam.fieldOfView = 55;
        }

        void Exit()
        {
            active = Active = false;
            if (headBone != null) headBone.localScale = Vector3.one;
            cam.nearClipPlane = savedNear; cam.fieldOfView = savedFov;
            if (ik != null) ik.weight = 0;
            fishing.ExternalShowcase = false; hud.Showcase3D = false;
            rig.ClearOverride(); rig.Snap();   // กลับมุมปกติทันที ไม่ให้กล้องลอยผ่านหัวตัวละคร
        }
    }
}
