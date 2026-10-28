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
        // FAST=1: build ทดสอบแบบเร็ว (แปลงโค้ดไม่ปรับแต่ง ไฟล์ใหญ่/ช้ากว่านิด) · ปกติ: เล็กที่สุดสำหรับขึ้นเว็บ
        bool fast = System.Environment.GetEnvironmentVariable("FAST") == "1";
        SetWasmOptimization(fast);
        PlayerSettings.SetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget.WebGL, fast ? Il2CppCompilerConfiguration.Debug : Il2CppCompilerConfiguration.Release);
        PlayerSettings.WebGL.compressionFormat = System.Environment.GetEnvironmentVariable("GZIP") == "1" ? WebGLCompressionFormat.Gzip : WebGLCompressionFormat.Brotli;   // Brotli เล็กกว่า Gzip ~20% (GZIP=1 ถ้าแรมไม่พอตอน build)
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.nameFilesAsHashes = true;   // ชื่อไฟล์เกมเปลี่ยนทุกครั้งที่เนื้อหาเปลี่ยน: มือถือที่ยังถือ index.html เก่าในแคชจะไม่ดึงไฟล์ใหม่มาปนกับไฟล์เก่าจนเปิดไม่ขึ้น
        // ภาพในเกมบีบอัดแบบ ASTC (มือถือ iPhone/Android ถอดได้ในชิปเอง) · แบบเดิม DXT มือถือต้องแตกเป็นภาพเต็มในแรมจนแรมเต็ม เบราว์เซอร์รีโหลดวนเปิดไม่ขึ้น
        EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;
        PlayerSettings.WebGL.showDiagnostics = false;
        PlayerSettings.SplashScreen.show = false; PlayerSettings.SplashScreen.showUnityLogo = false;   // ไม่ใช้โลโก้ Unity ตอนเปิด (มีจอโหลดของเกมเองแล้ว · ลดไฟล์ ~2.7MB)
        PlayerSettings.WebGL.template = "PROJECT:Cozy";   // หน้าเว็บเต็มจอ รองรับมือถือ (Assets/WebGLTemplates/Cozy)
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
        // ตัดโค้ดเอนจินที่ไม่ใช้ (ตัวเกมเล็กลง): ปลอดภัยเพราะส่ง assetBundleManifestPath ให้ตัวตัดรู้ว่าฉากใน bundle ใช้คลาสอะไร (เดิมปิดไว้เพราะเคย error Avatar ID 90 ตอนยังไม่ส่ง manifest)
        PlayerSettings.stripEngineCode = System.Environment.GetEnvironmentVariable("NO_STRIP") != "1";

        var all = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (all.Length == 0) { Debug.LogError("[ProjectTools] no scenes in build settings"); EditorApplication.Exit(1); return; }
        // ไฟล์เกมหลักมีแค่ฉาก Boot เปล่า ๆ · ทุกฉากด่าน (รวมบ้าน) เป็น AssetBundle + bundle ของใช้ร่วม (CozyFishing.StageLoader)
        var scenes = new[] { MakeBootScene() };
        var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "WebGL"));
        var bundleDir = Path.Combine(outDir, "Bundles");
        // สร้าง bundle ในโฟลเดอร์แคชถาวร (Library/StageBundles) → ครั้งต่อไปสร้างใหม่เฉพาะด่านที่เปลี่ยน (แก้แค่โค้ด/UI แทบไม่เสียเวลา)
        var cacheDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "BundleCache"));
        if (!BuildStageBundles(all, cacheDir)) { EditorApplication.Exit(1); return; }
        if (Directory.Exists(bundleDir)) Directory.Delete(bundleDir, true);
        Directory.CreateDirectory(bundleDir);
        foreach (var f in Directory.GetFiles(cacheDir))
            if (!f.EndsWith(".manifest") && Path.GetFileName(f) != "BundleCache") File.Copy(f, Path.Combine(bundleDir, Path.GetFileName(f)), true);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            assetBundleManifestPath = Path.Combine(cacheDir, "BundleCache.manifest"),   // อย่าตัดคลาส engine ที่ฉากใน bundle ใช้ (เช่น Avatar/Animator)
            scenes = scenes,
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = (System.Environment.GetEnvironmentVariable("CLEAN_BUILD") == "1" ? BuildOptions.CleanBuildCache : BuildOptions.None) | (System.Environment.GetEnvironmentVariable("DEV") == "1" ? BuildOptions.Development : BuildOptions.None),   // DEV=1: บิลด์ทดสอบที่พิมพ์ log/exception ออกคอนโซล
        });
        var s = report.summary;
        Debug.Log($"[ProjectTools] build {s.result} size={s.totalSize / 1024 / 1024}MB time={s.totalTime} out={outDir}");
        // ขนาดแยกตามไฟล์ต้นทาง (ไว้หาว่าอะไรกินที่ใน .data)
        var top = report.packedAssets.SelectMany(p => p.contents).GroupBy(c => c.sourceAssetPath).Select(g => (path: g.Key, kb: g.Sum(c => (long)c.packedSize) / 1024)).OrderByDescending(t => t.kb).Take(60);
        foreach (var t in top) Debug.Log($"[ProjectTools] size {t.kb,8} KB  {t.path}");
        EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
    }

    // ตัวเลือกแปลงโค้ด WebAssembly (อยู่ใน UnityEditor.WebGL.Extensions — เรียกผ่าน reflection ไม่ต้องอ้าง assembly ตรง)
    static void SetWasmOptimization(bool fast)
    {
        var t = System.Type.GetType("UnityEditor.WebGL.UserBuildSettings, UnityEditor.WebGL.Extensions");
        var e = System.Type.GetType("UnityEditor.WebGL.WasmCodeOptimization, UnityEditor.WebGL.Extensions");
        var p = t?.GetProperty("codeOptimization", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        if (p == null || e == null) { Debug.LogWarning("[ProjectTools] codeOptimization not found"); return; }
        p.SetValue(null, System.Enum.Parse(e, fast ? "BuildTimes" : "DiskSize"));
        Debug.Log("[ProjectTools] wasm optimization = " + (fast ? "BuildTimes" : "DiskSize"));
    }

    // แก้แค่ด่าน (ฉาก/โมเดล/ภาพ): สร้างเฉพาะ AssetBundle ของด่านแล้วก๊อปไปที่ Builds/WebGL/Bundles · ไม่แปลงโค้ดใหม่ (เร็วกว่าหลายเท่า)
    //   Unity.exe -batchmode -quit -buildTarget WebGL -projectPath . -executeMethod ProjectTools.BuildBundlesOnly
    public static void BuildBundlesOnly()
    {
        EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;
        var all = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "WebGL"));
        var bundleDir = Path.Combine(outDir, "Bundles");
        var cacheDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "BundleCache"));
        if (!Directory.Exists(Path.Combine(outDir, "Build"))) { Debug.LogError("[ProjectTools] no player build yet — run BuildWebGL once first"); EditorApplication.Exit(1); return; }
        if (!BuildStageBundles(all, cacheDir)) { EditorApplication.Exit(1); return; }
        Directory.CreateDirectory(bundleDir);
        foreach (var f in Directory.GetFiles(cacheDir))
            if (!f.EndsWith(".manifest") && Path.GetFileName(f) != "BundleCache") File.Copy(f, Path.Combine(bundleDir, Path.GetFileName(f)), true);
        Debug.Log("[ProjectTools] bundles only: done");
        EditorApplication.Exit(0);
    }

    // ด่านละหนึ่ง bundle (ชื่อ = ชื่อฉากตัวเล็ก) + index.json เก็บแฮชไว้ให้เบราว์เซอร์แคช/โหลดใหม่เมื่อด่านเปลี่ยน
    static bool BuildStageBundles(string[] scenePaths, string dir)
    {
        if (System.Environment.GetEnvironmentVariable("CLEAN_BUILD") == "1" && Directory.Exists(dir)) Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);
        // ของที่ใช้ตั้งแต่ 2 ฉากขึ้นไป → bundle "common" (โหลดครั้งเดียว) ไม่งั้นแต่ละด่านจะก๊อปตัวละคร/ฟ้า/พื้นผิวไปเองทั้งชุด
        //   ของที่ด่านแรก (ทุ่งนา) ใช้ → common / common-models / common-textures (โหลดก่อนเข้าเกม)
        //   ที่เหลือแยกตามชุดด่านที่ใช้ → common-s-<ด่าน>-<ด่าน> โหลดเมื่อไปด่านนั้นครั้งแรก (index.json บอกด้วย deps.<ด่าน>)
        var uses = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.SortedSet<string>>();
        foreach (var sp in scenePaths)
            foreach (var d in AssetDatabase.GetDependencies(sp, true))
            {
                if (d == sp || d.EndsWith(".cs") || d.EndsWith(".unity") || d.Contains("/Resources/") || d.StartsWith("Packages/com.unity.render-pipelines")) continue;
                if (!uses.TryGetValue(d, out var set)) uses[d] = set = new System.Collections.Generic.SortedSet<string>();
                set.Add(Path.GetFileNameWithoutExtension(sp).ToLowerInvariant());
            }
        var shared = uses.Where(kv => kv.Value.Count >= 2).OrderBy(kv => kv.Key).ToArray();
        string Group(System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.SortedSet<string>> kv) =>
            kv.Value.Contains(FirstStage) ? CommonGroup(kv.Key) : "common-s-" + string.Join("-", kv.Value);
        var builds = scenePaths.Select(p => new AssetBundleBuild { assetBundleName = Path.GetFileNameWithoutExtension(p).ToLowerInvariant(), assetNames = new[] { p } })
            .Concat(shared.GroupBy(Group).Select(g => new AssetBundleBuild { assetBundleName = g.Key, assetNames = g.Select(kv => kv.Key).ToArray() })).ToArray();
        Debug.Log($"[ProjectTools] common bundle: {shared.Length} shared assets");
        var manifest = BuildPipeline.BuildAssetBundles(dir, builds, BuildAssetBundleOptions.None, BuildTarget.WebGL);   // LZMA: ไฟล์เล็กสุด (เบราว์เซอร์แคชเป็น LZ4 ให้เอง)
        if (manifest == null) { Debug.LogError("[ProjectTools] bundle build failed"); return false; }
        var entries = builds.Select(b => $"\"{b.assetBundleName}\":\"{manifest.GetAssetBundleHash(b.assetBundleName)}\"")
            .Concat(scenePaths.Select(p => Path.GetFileNameWithoutExtension(p).ToLowerInvariant()).Select(sc => $"\"deps.{sc}\":\"{string.Join("|", manifest.GetAllDependencies(sc))}\""));
        File.WriteAllText(Path.Combine(dir, "index.json"), "{" + string.Join(",", entries) + "}");
        foreach (var b in builds) Debug.Log($"[ProjectTools] bundle {b.assetBundleName} {new FileInfo(Path.Combine(dir, b.assetBundleName)).Length / 1024 / 1024}MB");
        return true;
    }

    // แบ่งของใช้ร่วมเป็นหลายไฟล์ (ไม่ให้ไฟล์ไหนเกิน 100MB ของ GitHub): โมเดล/ท่าทาง · พื้นผิว/ภาพ · ที่เหลือ (วัสดุ shader mesh)
    const string FirstStage = "paddy";
    static string CommonGroup(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".glb" || ext == ".gltf" || ext == ".fbx" || ext == ".anim" || ext == ".controller") return "common-models";
        if (ext == ".jpg" || ext == ".png" || ext == ".hdr" || ext == ".exr" || ext == ".tga") return "common-textures";
        return "common";
    }

    // ฉาก Boot: กล้องจอดำ + CozyFishing.Boot (สร้างใหม่ทุกครั้งที่ build)
    static string MakeBootScene()
    {
        const string path = "Assets/Game/Scenes/Boot.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.04f, 0.06f, 0.05f);
        new GameObject("Boot").AddComponent<CozyFishing.Boot>();
        EditorSceneManager.SaveScene(scene, path);
        return path;
    }

    // ถ่ายภาพฉากจากมุมกล้องเกม (แนวนอน + แนวตั้ง) ด้วยคุณภาพระดับเดียวกับ WebGL — ใช้เทียบภาพโดยไม่ต้อง build
    // ถ่ายภาพทุกด่าน (มุมกว้าง + มุมต่ำ) → Builds/snaps/<ด่าน>_land.png / _close.png
    static string snapScene, snapPrefix = "snap_";
    public static void SnapAll()
    {
        Directory.CreateDirectory("Builds/snaps");
        foreach (var n in new[] { "Paddy", "Rapids", "Temple", "Marsh", "Houseboat", "Bend", "Cave", "Mangrove", "Headman" })
        {
            if (!File.Exists($"Assets/Game/Scenes/{n}.unity")) continue;
            snapScene = n; snapPrefix = $"snaps/{n}_";
            try { Snapshot(); } catch (System.Exception e) { Debug.LogError($"[ProjectTools] snap {n}: {e.Message}"); }
        }
        snapScene = null; snapPrefix = "snap_";
    }

    public static void Snapshot()
    {
        var sceneName = snapScene ?? System.Environment.GetEnvironmentVariable("SNAP_SCENE");
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
        if (pv != null && snapScene == null)
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
            if (snapScene != null && name == "port") continue;
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
            File.WriteAllBytes($"Builds/{snapPrefix}{name}.png", tex.EncodeToPNG());
            cam.targetTexture = null; RenderTexture.active = null;
            Debug.Log($"[ProjectTools] snapshot Builds/snap_{name}.png");
        }
    }
}
