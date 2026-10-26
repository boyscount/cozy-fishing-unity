using System.Collections.Generic;
using UnityEngine;

namespace CozyFishing
{
    // ด่านเรือไดหมึก: เรือโคลงตามคลื่น (โครงเรือ + กล้องเอียงตาม) · ไฟเขียวล่อหมึกกะพริบตามจังหวะไดหมึก · คลื่นลูกใหญ่ซัดเป็นระยะ
    [DefaultExecutionOrder(31000)]   // หลัง CameraRig วางกล้องแล้ว ค่อยเอียงทับ
    public class SquidSea : MonoBehaviour
    {
        Transform boat; Vector3 boatPos; Quaternion boatRot;
        readonly List<Light> lamps = new List<Light>(); readonly List<float> lampBase = new List<float>();
        readonly List<Renderer> bulbs = new List<Renderer>(); MaterialPropertyBlock mpb;
        Camera cam; float swellAt, swellT = -1;
        public static float Roll { get; private set; }   // องศาเอียงของเรือตอนนี้ (ระบบอื่นอ่านได้)

        public static void Build(IWorld w) => new GameObject("SquidSea").AddComponent<SquidSea>().Init();

        void Init()
        {
            var b = GameObject.Find("SquidBoat"); if (b != null) { boat = b.transform; boatPos = boat.position; boatRot = boat.rotation; }
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.name == "SquidLamp") { lamps.Add(l); lampBase.Add(l.intensity); }
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (r.name.StartsWith("SquidBulb")) bulbs.Add(r);
            mpb = new MaterialPropertyBlock(); cam = Camera.main; swellAt = Time.time + 12;
        }

        void LateUpdate()
        {
            float tm = Time.time;
            // คลื่นลูกใหญ่ทุก 15–25 วิ: โคลงแรงขึ้น 4 วิ
            if (tm > swellAt) { swellT = tm; swellAt = tm + Random.Range(15f, 25f); }
            float swell = swellT > 0 ? Mathf.Clamp01(1 - Mathf.Abs(tm - swellT - 2f) / 2f) : 0;
            float roll = Mathf.Sin(tm * 0.9f) * 2.2f + Mathf.Sin(tm * 2.3f + 1) * 0.6f + Mathf.Sin(tm * 1.7f) * 4.5f * swell;
            float pitch = Mathf.Sin(tm * 0.7f + 2) * 1.4f + Mathf.Sin(tm * 1.5f) * 2.5f * swell;
            float heave = Mathf.Sin(tm * 0.8f) * 0.08f + Mathf.Sin(tm * 1.6f) * 0.12f * swell;
            Roll = roll;
            if (boat != null) { boat.rotation = boatRot * Quaternion.Euler(pitch, 0, roll); boat.position = boatPos + Vector3.up * heave; }
            if (cam == null) cam = Camera.main;
            if (cam != null && !UnderwaterView.Diving) { cam.transform.rotation = cam.transform.rotation * Quaternion.Euler(pitch * 0.5f, 0, roll * 0.6f); cam.transform.position += Vector3.up * heave * 0.8f; }
            // ไฟล่อหมึก: สว่างวาบตามจังหวะไดหมึก (บอกจังหวะให้ผู้เล่นแตะ)
            float pulse = FishingController.JigPulse;
            for (int i = 0; i < lamps.Count; i++) lamps[i].intensity = lampBase[i] * (0.55f + 0.9f * pulse);
            var c = new Color(0.3f, 1f, 0.4f) * (0.9f + 1.1f * pulse);   // สว่างเกินจะกลายเป็นสีขาว
            foreach (var r in bulbs) { r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", c); mpb.SetColor("_Color", c); r.SetPropertyBlock(mpb); }
        }
    }
}
