using UnityEditor;
using UnityEditor.SceneManagement;

// อัดคลิปโปรโมต 4K จากเอดิเตอร์ (โหมดเล่น) — รันแบบ batch ไม่ต้องเปิดหน้าต่าง:
//   set COZY_PROMO=<out>|<วินาที>|<จำนวนปลา>  set COZY_FLAGS=bot&stage=temple&intro&hour=16.5&slowclock
//   Unity -batchmode -projectPath . -executeMethod PromoCaptureEditor.Run   (ไม่ใส่ -quit · เกมสั่งปิดเองเมื่ออัดเสร็จ)
public static class PromoCaptureEditor
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Game/Scenes/Paddy.unity");
        EditorApplication.EnterPlaymode();
    }
}
