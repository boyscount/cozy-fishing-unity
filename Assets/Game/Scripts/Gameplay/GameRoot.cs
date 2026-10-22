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
            if (director != null) director.Begin(w);
        }
    }
}
