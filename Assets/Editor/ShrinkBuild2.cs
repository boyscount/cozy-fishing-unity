using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// ลดไฟล์โหลดรอบสอง (เป้า < 50 MB ก่อนเล่นด่านแรก)
//   Unity.exe -batchmode -buildTarget WebGL -projectPath . -executeMethod ShrinkBuild2.Run -quit
public static class ShrinkBuild2
{
    public static void Run()
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture", new[] { "Assets/Game/Models/Character/Textures", "Assets/Game/Textures/PBR", "Assets/Game/Textures/Sky", "Assets/Game/Resources/UI/Frames", "Assets/Game/Resources/UI/Stages", "Assets/Game/Resources/UI/Title" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
            var fn = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            int max = 0;
            // ตัวละคร: ผิวลำตัว (แขนที่เห็นชิดกล้องตอนตกปลามุมสายตา) คงคม · เสื้อ กางเกง ผม รองเท้า เห็นไกล → 512
            if (path.Contains("/Character/")) max = fn.Contains("_body_") ? 1024 : 512;
            // พื้นผิวด่าน: ภาพสีหลักคงไว้ · ภาพนูน/ความมัน/AO 512 (ตาแทบไม่เห็นต่าง)
            else if (path.Contains("/PBR/") && (fn.Contains("_nor") || fn.Contains("rough") || fn.Contains("_arm") || fn.Contains("_ao"))) max = 512;
            else if (path.Contains("/Sky/")) max = 512;            // ฟ้า HDR: โดนหมอก/เมฆบังอยู่แล้ว
            else if (fn == "shop_bg") max = 1024;                  // ฉากหลังร้าน (ขนาดภาพต้องเป็นเลขยกกำลังสอง: 1536 ใช้ไม่ได้ Unity ปัดกลับเป็น 2048)
            else if (path.Contains("/UI/Stages/")) max = 512;     // ภาพปกด่าน: แสดงเล็กบนกระดานเช้า/กระดานเลือกด่าน
            else if (fn == "board") max = 512;                     // ป้ายไม้หน้าแรก
            if (max == 0) continue;
            var ps = ti.GetPlatformTextureSettings("WebGL");
            if (ps.overridden && ps.maxTextureSize <= max) continue;
            ps.overridden = true; ps.maxTextureSize = max;
            if (ps.format == TextureImporterFormat.Automatic) ps.format = ti.textureType == TextureImporterType.NormalMap ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_6x6;
            ti.SetPlatformTextureSettings(ps); ti.SaveAndReimport(); n++;
        }
        // URP: ปิดฟีเจอร์ที่เกมไม่ได้ใช้ → แบบย่อยของเชดเดอร์น้อยลง (ไฟล์เล็กลง build เร็วขึ้น)
        foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets/Settings" }))
        {
            var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
            var so = new SerializedObject(a);
            foreach (var p in new[] { "m_SupportsTerrainHoles", "m_SupportsLightCookies", "m_AdditionalLightShadowsSupported" })
            {
                var sp = so.FindProperty(p); if (sp != null) sp.boolValue = false;
            }
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(a);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[ShrinkBuild2] {n} textures downsized");
    }
}
