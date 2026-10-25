// สร้างด่านที่ผู้เล่นยืนบนเรือใหม่ (คุ้งน้ำมืด · ถ้ำ) ในการเปิด Unity ครั้งเดียว
//   Unity.exe -batchmode -projectPath . -executeMethod SceneBuilder.CreateBoatStages -quit
public static partial class SceneBuilder
{
    public static void CreateBoatStages()
    {
        CreateBend();
        CreateCave();
    }
}
