using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CozyFishing
{
    // คัตซีนด้วย Unity Timeline: ไฟล์ .playable อยู่ที่ Resources/Cutscenes (สร้างด้วย Tools ▸ Cozy ▸ Build Cutscenes · เปิดแก้ในหน้าต่าง Timeline ได้)
    //   แทร็กของเกมเอง (ไม่ต้องผูกกับวัตถุในฉาก เพราะทุกด่านสร้างจากโค้ด): ทุกแทร็กผูกกับ CutsceneRig ซึ่งบอกตำแหน่งจุดอ้างอิง
    //   · กล้อง (CamShot): ตำแหน่ง/จุดมอง = จุดอ้างอิง + ระยะห่างในทิศของฉาก (z = ทิศหลักของคัตซีน) · ซูม · เอียงกล้อง · ช็อตซ้อนกัน = ตัดแบบเฟด
    //   · บทพูด/ชื่อเรื่อง (Caption) · เสียง (Cue) · เหตุการณ์ (Event: พลุ กล้องสั่น โคมลอย) · ตะเกียงวูบ (Flicker) · สโลโมชั่น (TimeScale)
    //   เล่นด้วยเวลาจริง (unscaled) จึงทำสโลโมชั่นได้โดยคัตซีนไม่ช้าตาม
    public class CutsceneRig : MonoBehaviour
    {
        public readonly Dictionary<string, Transform> anchors = new Dictionary<string, Transform>();
        public Quaternion frame = Quaternion.identity;   // ทิศหลักของคัตซีน (แกน z ของระยะห่าง)
        public Camera cam;
        public Hud hud;
        public Action<string> onEvent;
        public Action<string> onCue;
        Pose cam0;

        public void SnapCamera() { if (cam != null) cam0 = new Pose(cam.transform.position, cam.transform.rotation); }
        public void Anchor(string name, Transform t) { if (t != null) anchors[name] = t; }

        /// จุดอ้างอิง + ระยะห่าง · "cam0" = มุมกล้องตอนเริ่มคัตซีน (ระยะห่างคิดในทิศของกล้อง)
        public Vector3 Point(string anchor, Vector3 off)
        {
            if (anchor == "cam0") return cam0.position + cam0.rotation * off;
            return (anchors.TryGetValue(anchor ?? "", out var t) && t != null ? t.position : Vector3.zero) + frame * off;
        }
        public Vector3 LookPoint(string anchor, Vector3 off)
        {
            if (anchor == "cam0") return cam0.position + cam0.rotation * (Vector3.forward * 20 + off);
            return Point(anchor, off);
        }
    }
}
