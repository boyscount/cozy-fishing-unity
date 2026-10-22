using UnityEngine;

namespace CozyFishing
{
    // จุดเริ่มของฉาก: ผูกด่าน ตัวละคร ระบบตกปลา และกล้องเข้าด้วยกัน
    public class GameRoot : MonoBehaviour
    {
        public PaddyWorld world;
        public PlayerController player;
        public FishingController fishing;
        public CameraRig rig;

        void Start()
        {
            Application.targetFrameRate = 60;
            player.Init(world);
            fishing.Init(world);
            rig.Init(world);
        }
    }
}
