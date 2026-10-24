using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// รายงานขนาดของในไฟล์โหลดเริ่มเกม (โมเดล/ภาพ ที่ใช้ร่วมหลายด่าน + Resources) — หาว่าอะไรกินที่มากสุดก่อนลดขนาด
//   Unity.exe -batchmode -projectPath . -executeMethod SizeReport.Run -quit  → Logs/size_report.txt
public static class SizeReport
{
    public static void Run()
    {
        var rows = new System.Collections.Generic.List<(long size, string what)>();
        foreach (var dir in new[] { "Assets/Game/Models", "Assets/Game/Resources", "Assets/Game/Textures" })
            foreach (var guid in AssetDatabase.FindAssets("", new[] { dir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path)) continue;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (o == null) continue;
                    if (!(o is Texture || o is Mesh || o is AnimationClip || o is AudioClip)) continue;
                    long s = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(o);
                    string extra = o is Texture2D t ? $" {t.width}x{t.height} {t.format}" : o is Mesh m ? $" {m.vertexCount}v" : "";
                    rows.Add((s, $"{s / 1024,8} KB  {o.GetType().Name,-14} {path} :: {o.name}{extra}"));
                }
            }
        File.WriteAllLines("Logs/size_report.txt", rows.OrderByDescending(r => r.size).Take(80).Select(r => r.what));
        Debug.Log("[SizeReport] done");
    }
}

// บีบอัดภาพ UI/รูปปลา/ฟ้า ที่เดิมเก็บแบบไม่บีบอัด (RGB24/RGBA32) → ASTC บน WebGL · ภาพ UI ไม่ต้องมี mipmap
//   Unity.exe -batchmode -projectPath . -executeMethod CompressTools.CompressUi -quit
public static class CompressTools
{
    public static void CompressUi()
    {
        int n = 0;
        foreach (var (dir, fmt, max, mips) in new[] {
            ("Assets/Game/Resources/UI/Frames", TextureImporterFormat.ASTC_4x4, 2048, false),
            ("Assets/Game/Resources/UI/Title", TextureImporterFormat.ASTC_4x4, 1024, false),
            ("Assets/Game/Resources/UI/Stages", TextureImporterFormat.ASTC_6x6, 1024, false),
            ("Assets/Game/Resources/UI/Portraits", TextureImporterFormat.ASTC_4x4, 512, false),
            ("Assets/Game/Resources/Fish", TextureImporterFormat.ASTC_6x6, 512, true),
            ("Assets/Game/Textures/Sky", TextureImporterFormat.ASTC_HDR_6x6, 1024, true),   // ฟ้า HDR: ต้องใช้ ASTC แบบ HDR ไม่งั้นแสงฟ้าโดนตัด
            ("Assets/Game/Models/Character/Textures", TextureImporterFormat.ASTC_6x6, 1024, true) })
            foreach (var guid in AssetDatabase.FindAssets("t:Texture", new[] { dir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
                var ps = ti.GetPlatformTextureSettings("WebGL");
                ps.overridden = true; ps.format = fmt; ps.maxTextureSize = max;
                ti.SetPlatformTextureSettings(ps);
                if (ti.textureCompression == TextureImporterCompression.Uncompressed) ti.textureCompression = TextureImporterCompression.CompressedHQ;
                if (!mips && ti.textureType != TextureImporterType.NormalMap) ti.mipmapEnabled = false;
                ti.SaveAndReimport(); n++;
            }
        Debug.Log($"[CompressTools] {n} textures");
    }

    // ตัวละคร (FBX): บีบอัด mesh ระดับกลาง + ท่าทางแบบ Optimal → ไฟล์โหลดเริ่มเกมเล็กลง
    public static void CompressModels()
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Game/Models/Character" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is ModelImporter mi)) continue;
            mi.meshCompression = ModelImporterMeshCompression.Medium;
            mi.animationCompression = ModelImporterAnimationCompression.Optimal;
            mi.SaveAndReimport(); n++;
        }
        CompressUi();
        Debug.Log($"[CompressTools] {n} models");
    }
}
