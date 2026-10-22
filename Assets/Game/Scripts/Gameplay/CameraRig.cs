using UnityEngine;

namespace CozyFishing
{
    // กล้องมุมสูงแบบ diorama ตามตัวละคร (จอแนวตั้งถอยกล้องออก) — เทียบ updateCamera ของเวอร์ชันเว็บ
    public class CameraRig : MonoBehaviour
    {
        public PlayerController player;
        public FishingController fishing;
        IWorld world;
        Vector3 target;
        Camera cam;

        public void Init(IWorld w) { world = w; cam = GetComponent<Camera>(); Snap(); }

        Vector3 Desired()
        {
            var f = world.CamFocus(player.Pos);
            if (fishing != null && fishing.LineOut) { f = (f + new Vector2(fishing.BobberPos.x, fishing.BobberPos.z)) / 2 - new Vector2(0, 0.5f); }
            var b = world.CamBounds;
            return new Vector3(Mathf.Clamp(f.x, b.xMin, b.xMax), 0, Mathf.Clamp(f.y, b.yMin, b.yMax));
        }

        public void Snap() { target = Desired(); Place(1); }

        void LateUpdate() => Place(Time.deltaTime);

        void Place(float dt)
        {
            target = Vector3.Lerp(target, Desired(), Mathf.Min(1, dt * 2.5f));
            float aspect = cam.aspect;
            float k = aspect < 0.9f ? 1.6f : aspect < 1.4f ? 1.2f : 1f;
            var pos = target + new Vector3(0, 10.5f * k, -15f * k);
            transform.position = Vector3.Lerp(transform.position, pos, dt >= 1 ? 1 : Mathf.Min(1, dt * 3));
            transform.LookAt(target + Vector3.up * 1.4f);
        }
    }
}
