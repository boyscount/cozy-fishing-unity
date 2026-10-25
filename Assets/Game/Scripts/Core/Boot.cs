using UnityEngine;

namespace CozyFishing
{
    // ฉากเปิดเกม (ฉากเดียวในไฟล์เกมหลัก): โหลดของใช้ร่วม + ฉากบ้านจาก AssetBundle ระหว่างที่จอโหลดหน้าเว็บยังแสดงอยู่
    public class Boot : MonoBehaviour
    {
        void Start()
        {
            _ = Cloud.Init();   // เข้าระบบออนไลน์แบบไม่ระบุตัวตน + ดึงเซฟคลาวด์ ระหว่างโหลดด่านแรก (ไม่รอ · ออฟไลน์ก็เล่นได้)
            StageLoader.Go("Paddy", null, err => StageLoader.BootFailed(err));
        }
    }
}
