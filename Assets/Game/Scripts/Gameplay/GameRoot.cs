using UnityEngine;

namespace CozyFishing
{
    // จุดเริ่มของฉาก: ผูกด่าน ตัวละคร ระบบตกปลา และกล้องเข้าด้วยกัน
    public class GameRoot : MonoBehaviour
    {
        public MonoBehaviour world;     // PaddyWorld หรือ StageWorld (ต้องเป็น IWorld)
        public PlayerController player;
        public FishingController fishing;
        public CameraRig rig;
        public StoryDirector director;

        void Start()
        {
            Application.targetFrameRate = 60;
            DynamicGI.UpdateEnvironment();   // แสงฟ้ารอบข้างจาก HDRI (กันกรณีข้อมูลอบแสงไม่มากับ build)
            var w = (IWorld)world;
            player.Init(w);
            fishing.Init(w);
            rig.Init(w);
            gameObject.AddComponent<PondLife>().Init(w, fishing);          // ฝูงปลาว่ายใต้น้ำ
            gameObject.AddComponent<UnderwaterView>().Init(w, fishing, rig); // มองใต้น้ำตอนรอปลากินเหยื่อ
            gameObject.AddComponent<FirstPersonView>().Init(fishing, rig);    // มุมบุคคลที่หนึ่งตอนเหวี่ยง/สู้ปลา/ยกปลาโชว์
            gameObject.AddComponent<Atmosphere>().Init(w, rig.GetComponent<Camera>());   // ปรับภาพตามเวลา ลำแสง ดวงอาทิตย์เรือง ละอองแสง/หิ่งห้อย หมอกเหนือน้ำ
            gameObject.AddComponent<NightSky>().Init(w, rig.GetComponent<Camera>());   // ดาว ทางช้างเผือก พระจันทร์ แสงจันทร์บนน้ำ
            gameObject.AddComponent<Weather>().Init(w, rig.GetComponent<Camera>());      // วันฝน: สายฝน วงน้ำ เสียงฝน ฟ้าร้อง · ถ้ำ: หยดน้ำ ลม เสียงก้อง
            gameObject.AddComponent<GhostSystem>().Init(w, fishing, director); // ตกปลากลางคืน: ผีประจำด่าน + เกจสติ + ไฟฉาย
            if (director != null) director.Begin(w);
        }
    }
}
