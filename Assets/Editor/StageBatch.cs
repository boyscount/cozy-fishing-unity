// สร้างฉากหลายด่านในการเปิด Unity ครั้งเดียว (ประหยัดเวลาเปิดโปรเจกต์)
//   Unity.exe -batchmode -projectPath . -executeMethod SceneBuilder.CreateNightStages -quit
public static partial class SceneBuilder
{
    public static void CreateNightStages()
    {
        CreateBend();
        CreateHouseboat();
        CreateMarsh();
        CreateTemple();
    }
}
