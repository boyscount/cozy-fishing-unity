using UnityEngine;

namespace CozyFishing
{
    // กล้องมุมสูงแบบ diorama ตามตัวละคร (จอแนวตั้งถอยกล้องออก) — เทียบ updateCamera ของเวอร์ชันเว็บ
    // หมุนรอบตัวละครได้ (ViewYaw) ก้ม/เงยได้ (ต่ำสุดเห็นขอบฟ้า) และซูมด้วยล้อเมาส์ · ทิศเดินของผู้เล่นอิงมุมกล้อง
    // กล้องนุ่มแบบ Cinemachine: ตามด้วยสปริงหน่วง (SmoothDamp) · ซูมเข้าออกเองตามจังหวะตกปลา · สั่นสั้น ๆ ตอนปลากินเบ็ด/ตีเบ็ด/สายขาด
    public class CameraRig : MonoBehaviour
    {
        public PlayerController player;
        public FishingController fishing;
        IWorld world;
        Vector3 target;
        Camera cam;
        public static float ViewYaw;          // มุมกล้องรอบแกนตั้ง (องศา) — PlayerController ใช้แปลงทิศเดิน
        float pitch = 35f, zoom = 1f, shotZoom = 1f, shotZoomV;
        Vector3 posVel, lookVel, lookAt;
        static float shake;
        /// สั่นกล้องสั้น ๆ (0–1) · เรียกตอนปลากินเบ็ด ตีเบ็ด สายขาด
        public static void Impulse(float k) => shake = Mathf.Max(shake, k);
        bool fixedShot; Vector3 shotPos, shotLook;
        /// มุมกล้องตายตัว (ฉากที่บ้าน) · ClearOverride กลับไปตามตัวละคร
        public void Override(Vector3 pos, Vector3 look, bool sway = true) { fixedShot = true; shotSway = sway; shotPos = pos; shotLook = look; transform.position = pos; transform.LookAt(look); }
        bool shotSway = true;   // ฉากที่บ้านลอยไปมาเบา ๆ · มุมบุคคลที่หนึ่ง/ใต้น้ำไม่ลอย
        public void ClearOverride() => fixedShot = false;

        public void Init(IWorld w) { world = w; cam = GetComponent<Camera>(); ViewYaw = 0; Snap(); }

        Vector3 Desired()
        {
            var f = world.CamFocus(player.Pos);
            if (fishing != null && fishing.LineOut) { f = (f + new Vector2(fishing.BobberPos.x, fishing.BobberPos.z)) / 2 - new Vector2(0, 0.5f); }
            var b = world.CamBounds;
            return new Vector3(Mathf.Clamp(f.x, b.xMin, b.xMax), 0, Mathf.Clamp(f.y, b.yMin, b.yMax));
        }

        public void Snap() { target = Desired(); Place(1); }

        void LateUpdate()
        {
            if (fixedShot)
            {
                // ฉากที่บ้าน: กล้องลอยช้า ๆ เล็กน้อยให้ดูมีชีวิต
                float s = Time.time * 0.15f;
                transform.position = shotPos + (shotSway ? new Vector3(Mathf.Sin(s) * 0.25f, Mathf.Sin(s * 1.3f) * 0.08f, 0) : Vector3.zero);
                transform.LookAt(shotLook);
                return;
            }
            Place(Time.deltaTime);
        }

        void Place(float dt)
        {
            target = Vector3.Lerp(target, Desired(), Mathf.Min(1, dt * 2.5f));
            if (dt < 1)
            {
                var look = GameInput.Look;
                ViewYaw = Mathf.Repeat(ViewYaw + look.x, 360);
                pitch = Mathf.Clamp(pitch - look.y, 8, 70);
                zoom = Mathf.Clamp(zoom - GameInput.Zoom * 0.0012f, 0.45f, 1.35f);
            }
            float aspect = cam.aspect;
            float k = aspect < 0.9f ? 1.6f : aspect < 1.4f ? 1.2f : 1f;
            // ซูมตามจังหวะ: ง้างเบ็ดถอยออกนิด · รอปลา/ปลากินเบ็ด/สู้ปลา ดันเข้าใกล้ให้ลุ้น
            float want = 1f;
            if (fishing != null) switch (fishing.State)
                {
                    case FishState.Charging: want = 1.06f; break;
                    case FishState.Waiting: want = 0.93f; break;
                    case FishState.Bite: want = 0.88f; break;
                    case FishState.Strike: case FishState.Fight: want = 0.84f; break;
                    case FishState.Caught: want = 0.9f; break;
                }
            shotZoom = dt >= 1 ? want : Mathf.SmoothDamp(shotZoom, want, ref shotZoomV, 0.6f, 2f, dt);
            float dist = 18.3f * k * zoom * shotZoom * Mathf.Lerp(0.55f, 1f, Mathf.InverseLerp(8, 35, pitch));   // มุมต่ำเข้าใกล้ตัวละคร
            // มุมปกติโฟกัสค่อนไปทางบ่อ · หมุนกล้องไปด้านอื่นหรือก้มต่ำ → โฟกัสตัวละครแทน (ไม่ให้ตัวละครหลุดขอบจอ)
            float turned = Mathf.InverseLerp(10, 60, Mathf.Abs(Mathf.DeltaAngle(0, ViewYaw)));
            var focus = Vector3.Lerp(target, new Vector3(player.Pos.x, 0, player.Pos.z), Mathf.Max(turned, Mathf.InverseLerp(35, 10, pitch)));
            var pos = focus + Quaternion.Euler(pitch, ViewYaw, 0) * new Vector3(0, 0, -dist);
            var lookTarget = focus + Vector3.up * Mathf.Lerp(1.4f, 1.6f, Mathf.InverseLerp(35, 10, pitch));
            if (dt >= 1) { transform.position = pos; lookAt = lookTarget; posVel = lookVel = Vector3.zero; }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, pos, ref posVel, 0.18f, 60f, dt);   // สปริงหน่วง: ออกตัวนุ่ม หยุดนุ่ม ไม่สะดุด
                lookAt = Vector3.SmoothDamp(lookAt, lookTarget, ref lookVel, 0.12f, 60f, dt);
            }
            if (shake > 0.001f)   // สั่นสั้น ๆ แบบ noise (ไม่ใช่สุ่มกระตุก)
            {
                float t = Time.unscaledTime * 28f, a = shake * shake * 0.12f;
                transform.position += new Vector3(Mathf.PerlinNoise(t, 1.3f) - 0.5f, Mathf.PerlinNoise(2.7f, t) - 0.5f, 0) * a;
                shake = Mathf.MoveTowards(shake, 0, Time.unscaledDeltaTime * 2.2f);
            }
            transform.LookAt(lookAt);
        }
    }
}
