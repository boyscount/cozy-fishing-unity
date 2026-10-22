// เครื่องมือสั่งงานโปรเจกต์ผ่าน batchmode (ไม่ต้องเปิดหน้าต่าง Unity)
//   Unity.exe -batchmode -projectPath . -executeMethod ProjectTools.AddPackages
//   Unity.exe -batchmode -projectPath . -executeMethod ProjectTools.BuildWebGL -quit
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public static class ProjectTools
{
    static AddAndRemoveRequest addReq;

    // เพิ่มแพ็กเกจที่เกมต้องใช้ (รอจนติดตั้งเสร็จแล้วค่อยปิด Unity)
    public static void AddPackages()
    {
        addReq = Client.AddAndRemove(new[] { "com.unity.cloud.gltfast", "com.unity.test-framework" }, null);
        EditorApplication.update += WaitAdd;
    }

    static void WaitAdd()
    {
        if (!addReq.IsCompleted) return;
        EditorApplication.update -= WaitAdd;
        if (addReq.Status == StatusCode.Success)
        {
            foreach (var p in addReq.Result) Debug.Log($"[ProjectTools] package {p.name} {p.version}");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("[ProjectTools] add packages failed: " + addReq.Error?.message);
            EditorApplication.Exit(1);
        }
    }

    // Build WebGL สำหรับเปิดบน GitHub Pages (Pages ไม่ส่ง header Content-Encoding จึงต้องเปิด decompression fallback)
    public static void BuildWebGL()
    {
        PlayerSettings.companyName = "boyscount";
        PlayerSettings.productName = "ตกปลาสบายใจ";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.showDiagnostics = false;
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.High);

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) { Debug.LogError("[ProjectTools] no scenes in build settings"); EditorApplication.Exit(1); return; }
        var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "WebGL"));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        });
        var s = report.summary;
        Debug.Log($"[ProjectTools] build {s.result} size={s.totalSize / 1024 / 1024}MB time={s.totalTime} out={outDir}");
        EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
    }
}
