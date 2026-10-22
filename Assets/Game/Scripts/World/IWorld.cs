using UnityEngine;

namespace CozyFishing
{
    // สิ่งที่ตัวละคร กล้อง และระบบตกปลาต้องรู้จากด่าน (ทุกด่านใช้สัญญาเดียวกัน — เทียบ world API ของเวอร์ชันเว็บ)
    public interface IWorld
    {
        string StageId { get; }
        float WaterY { get; }
        float GroundH(float x, float z);
        float StandY(float x, float z);
        bool Walkable(float x, float z, float hop = 0);   // hop = ความสูงที่ตัวละครลอยจากพื้น (กระโดดข้ามของเตี้ยได้)
        bool CanCast(float x, float z);
        bool OnSolid(float x, float z);
        float DepthAt(float x, float z);
        Vector3 Spawn { get; }
        float SpawnYaw { get; }
        Vector3 Special { get; }
        float SpecialRadius { get; }
        Vector2 CamFocus(Vector3 player);
        Rect CamBounds { get; }
    }
}
