using UnityEngine;

namespace CozyFishing
{
    // อนุภาคที่ออกเฉพาะบางช่วงเวลา (หิ่งห้อยยามโพล้เพล้-กลางคืน · หมอกยามเช้า): นอกช่วงปิดการปล่อย ตัวที่ลอยอยู่ค่อย ๆ จางหายเอง
    [RequireComponent(typeof(ParticleSystem))]
    public class DuskOnly : MonoBehaviour
    {
        public float from = 16.6f, to = 6.3f;
        ParticleSystem ps;
        void Awake() => ps = GetComponent<ParticleSystem>();
        void Update()
        {
            float h = Story.S.time;
            var em = ps.emission;
            em.enabled = from < to ? h >= from && h < to : h >= from || h < to;   // from < to = ช่วงในวันเดียว (หมอกเช้า) · ไม่งั้นข้ามเที่ยงคืน (หิ่งห้อย)
        }
    }
}
