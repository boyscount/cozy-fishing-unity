using UnityEditor;
using UnityEngine;

// ลดไฟล์โหลดรอบสาม: ภาพ UI (ฉากหลัง ป้ายไม้ กระดาษ ร้าน ปกด่าน ภาพหน้าตัวละคร) ใช้แค่บนจอ 2D
//   ASTC 6x6 (เล็กกว่า 4x4 ครึ่งกว่า ภาพวาดตาแทบไม่เห็นต่าง) + ไม่ต้องมี mipmap (ไม่ถูกย่อไกล ๆ แบบพื้นผิว 3D)
//   ไอคอนเล็กคงไว้ 4x4 ให้ขอบคม
//   Unity.exe -batchmode -buildTarget WebGL -projectPath . -executeMethod ShrinkBuild3.Run -quit
public static class ShrinkBuild3
{
    public static void Run()
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture", new[] { "Assets/Game/Resources/UI/Frames", "Assets/Game/Resources/UI/Title", "Assets/Game/Resources/UI/Shop", "Assets/Game/Resources/UI/Stages", "Assets/Game/Resources/UI/Portraits", "Assets/Game/Resources/UI/Journal" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
            var ps = ti.GetPlatformTextureSettings("WebGL");
            bool changed = ti.mipmapEnabled || !ps.overridden || ps.format != TextureImporterFormat.ASTC_6x6;
            if (!changed) continue;
            ti.mipmapEnabled = false;
            if (!ps.overridden) { ps.overridden = true; ps.maxTextureSize = ti.maxTextureSize; }
            ps.format = TextureImporterFormat.ASTC_6x6;
            ti.SetPlatformTextureSettings(ps); ti.SaveAndReimport(); n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[ShrinkBuild3] {n} UI textures → ASTC 6x6, no mips");
    }
}
