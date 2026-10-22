using UnityEngine;

namespace CozyFishing
{
    // ฉากเปิดเกม (ฉากเดียวในไฟล์เกมหลัก): โหลดของใช้ร่วม + ฉากบ้านจาก AssetBundle ระหว่างที่จอโหลดหน้าเว็บยังแสดงอยู่
    public class Boot : MonoBehaviour
    {
        void Start() => StageLoader.Go("Paddy", null, err => StageLoader.BootFailed(err));
    }
}
