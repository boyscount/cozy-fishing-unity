// เครื่องมือสั่งงานโปรเจกต์ผ่าน batchmode (ไม่ต้องเปิดหน้าต่าง Unity)
//   Unity.exe -batchmode -projectPath . -executeMethod ProjectTools.AddPackages
//   Unity.exe -batchmode -projectPath . -executeMethod ProjectTools.BuildWebGL -quit
//   Unity.exe -batchmode -projectPath . -executeMethod ProjectTools.Snapshot -quit   (ภาพฉากจากมุมกล้องเกม → Builds/snap_*.png)
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEditor.SceneManagement;

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
        PlayerSettings.productName = "A Basket of Tomorrows";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;   // Brotli ใช้แรมตอน build มากเกิน (เครื่องเหลือแรมน้อยแล้ว build ล้ม)
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.showDiagnostics = false;
        PlayerSettings.WebGL.template = "PROJECT:Cozy";   // หน้าเว็บเต็มจอ รองรับมือถือ (Assets/WebGLTemplates/Cozy)
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

    // ถ่ายภาพฉากจากมุมกล้องเกม (แนวนอน + แนวตั้ง) ด้วยคุณภาพระดับเดียวกับ WebGL — ใช้เทียบภาพโดยไม่ต้อง build
    public static void Snapshot()
    {
        var sceneName = System.Environment.GetEnvironmentVariable("SNAP_SCENE");
        EditorSceneManager.OpenScene(string.IsNullOrEmpty(sceneName) ? EditorBuildSettings.scenes[0].path : $"Assets/Game/Scenes/{sceneName}.unity");
        QualitySettings.SetQualityLevel(0, true);
        ShaderUtil.allowAsyncCompilation = false;
        var cam = Camera.main;
        // ต้นข้าว/หญ้าวาดตอนเล่น: เรียก Start + Update ของ FoliageRenderer ให้ส่งคำสั่งวาดก่อนถ่าย
        var fol = Object.FindAnyObjectByType<CozyFishing.FoliageRenderer>();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        if (fol != null) typeof(CozyFishing.FoliageRenderer).GetMethod("Start", flags)?.Invoke(fol, null);
        Shader.SetGlobalVector("_CozyWind", new Vector4(3, 0.9f, 0, 0));
        // ตัวละคร Mixamo: เล่นท่า "ถือคันรอปลา" ไปครึ่งวินาทีให้เห็นท่าจริงในภาพ
        var player = Object.FindAnyObjectByType<CozyFishing.PlayerController>();
        if (player != null && player.animator != null)
        {
            player.animator.Rebind();
            player.animator.SetInteger("Pose", System.Environment.GetEnvironmentVariable("SNAP_POSE") == "walk" ? 0 : 3);
            player.animator.SetFloat("Speed", System.Environment.GetEnvironmentVariable("SNAP_POSE") == "walk" ? 2.2f : 0);
            for (int i = 0; i < 30; i++) player.animator.Update(1 / 30f);
        }
        Shader.SetGlobalFloat("_CozyDim", 1);
        // รูปปลาจากกล้อง FishPreview (ปกติ + เงาดำ) ไว้ตรวจมุม/ขนาด
        var pv = Object.FindAnyObjectByType<CozyFishing.FishPreview>();
        if (pv != null)
        {
            typeof(CozyFishing.FishPreview).GetMethod("Awake", flags)?.Invoke(pv, null);
            Directory.CreateDirectory("Builds");
            int idx = 0;
            foreach (var sp in CozyFishing.FishDatabase.ForStage("paddy"))
            {
                foreach (var sil in new[] { false, true })
                {
                    var rt = pv.Icon(sp, sil); pv.Icon(sp, sil);
                    RenderTexture.active = rt;
                    var tx = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false); tx.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tx.Apply();
                    File.WriteAllBytes($"Builds/fish_{idx}_{(sil ? "s" : "c")}.png", tx.EncodeToPNG());
                    RenderTexture.active = null;
                }
                idx++;
            }
        }
        var gr = Object.FindAnyObjectByType<CozyFishing.GameRoot>();
        var iw = gr != null ? gr.world as CozyFishing.IWorld : null;
        var f2 = iw != null ? iw.CamFocus(iw.Spawn) : new Vector2(1.125f, -2.16f);
        var focus = new Vector3(f2.x, 0, f2.y);
        if (player != null && iw != null) { player.transform.position = new Vector3(iw.Spawn.x, iw.StandY(iw.Spawn.x, iw.Spawn.z), iw.Spawn.z); player.transform.rotation = Quaternion.Euler(0, iw.SpawnYaw, 0); }
        Directory.CreateDirectory("Builds");
        foreach (var (name, w, h, k) in new[] { ("land", 1600, 900, 1f), ("port", 900, 1600, 1.6f), ("close", 1600, 900, 0f) })
        {
            if (k > 0) { cam.transform.position = focus + new Vector3(0, 10.5f * k, -15f * k); cam.transform.LookAt(focus + Vector3.up * 1.4f); }
            else if (iw != null && iw.StageId != "paddy") { var sp = iw.Spawn; cam.transform.position = sp + new Vector3(-7f, 3.2f, -4.5f); cam.transform.LookAt(sp + new Vector3(3f, 1.2f, 6f)); }   // มุมต่ำมองข้ามแม่น้ำ
            else if (System.Environment.GetEnvironmentVariable("SNAP_CLOSE") == "fisher") { cam.transform.position = new Vector3(5.5f, 2.6f, -12.2f); cam.transform.LookAt(new Vector3(1.5f, 1.2f, -7.4f)); }   // ซูมดูตัวละคร
            else { cam.transform.position = new Vector3(4f, 2.2f, -11f); cam.transform.LookAt(new Vector3(10f, 3.2f, -6f)); }   // มุมต่ำดูต้นตาล/ต้นไม้/ทุ่งข้าวใกล้ ๆ
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            cam.targetTexture = rt;
            cam.aspect = (float)w / h;
            if (fol != null) typeof(CozyFishing.FoliageRenderer).GetMethod("Update", flags)?.Invoke(fol, null);
            cam.Render(); cam.Render();   // รอบแรกให้ SRP Batcher อัปโหลดค่าวัสดุก่อน
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            File.WriteAllBytes($"Builds/snap_{name}.png", tex.EncodeToPNG());
            cam.targetTexture = null; RenderTexture.active = null;
            Debug.Log($"[ProjectTools] snapshot Builds/snap_{name}.png");
        }
    }
}
