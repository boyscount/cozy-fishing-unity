// สร้างฉากด่านบ่อปลาทุ่งนา: พื้น/บ่อจากฟังก์ชันความสูงของเวอร์ชันเว็บ + โมเดลสมจริงจาก Poly Haven (CC0)
// + บ้าน/ท่า/ตัวละครจาก Blender (assets.glb) ใส่ลายผิว PBR · ฟ้า HDRI · post-processing
//   Unity.exe -batchmode -projectPath . -executeMethod SceneBuilder.CreatePaddy -quit   (ไม่ใส่ -nographics: ต้องใช้ GPU อบแสงฟ้า)
using System.Collections.Generic;
using System.IO;
using CozyFishing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class SceneBuilder
{
    const string ModelPath = "Assets/Game/Models/assets.glb";
    const string NatureDir = "Assets/Game/Models/Nature";
    const string TexDir = "Assets/Game/Textures/PBR";
    const string HdriPath = "Assets/Game/Textures/Sky/kloofendal_48d_partly_cloudy_puresky_2k.hdr";
    const string ScenePath = "Assets/Game/Scenes/Paddy.unity";
    const string MatDir = "Assets/Game/Materials";
    const string GenDir = "Assets/Game/Generated";

    // สีวัสดุแต่ละชื่อ (ชื่อเดียวกับวัสดุใน Blender) — โทนธรรมชาติลดความสดลงจากเวอร์ชันการ์ตูน
    static readonly Dictionary<string, (string hex, float moss, float sway)> Palette = new Dictionary<string, (string, float, float)>
    {
        { "Cliff", ("#8a7f70", 0.7f, 0) }, { "Rock", ("#86817a", 0.6f, 0) }, { "Stone", ("#8c8780", 0.28f, 0) }, { "Bark", ("#ffffff", 0, 0) },
        { "Pine", ("#2f5e3c", 0, 0.05f) }, { "Leaf", ("#4f7f35", 0, 0.07f) }, { "Leaf2", ("#5d8a3b", 0, 0) }, { "Skin", ("#d9a47e", 0, 0) },
        { "Shirt", ("#33507f", 0, 0) }, { "Pants", ("#34303a", 0, 0) }, { "Hat", ("#d8bb82", 0, 0) }, { "HatBand", ("#a54a36", 0, 0) },
        { "Rod", ("#c49f5c", 0, 0) }, { "Eye", ("#1a1a1a", 0, 0) }, { "Hair", ("#231c18", 0, 0) }, { "Wood", ("#ffffff", 0, 0) },
        { "WoodDark", ("#a08870", 0, 0) }, { "Fish", ("#ffffff", 0, 0) }, { "FishFin", ("#dddddd", 0, 0) }, { "BoatPaint", ("#2a7d93", 0, 0) },
        { "BoatRed", ("#a83d33", 0, 0) }, { "Ribbon", ("#e07fa8", 0, 0) }, { "Ribbon2", ("#e3c142", 0, 0) }, { "Metal", ("#55585e", 0, 0) },
        { "Roof", ("#ffffff", 0, 0) }, { "Window", ("#2e2218", 0, 0) }, { "Palm", ("#47803a", 0, 0.05f) }, { "PalmTrunk", ("#d8c8b0", 0, 0) },
        { "Karst", ("#b8b1a2", 0.9f, 0) }, { "Thatch", ("#ffffff", 0, 0) }, { "Clay", ("#8f5236", 0, 0) },
    };
    // วัสดุที่ใส่ลายผิวจริง (triplanar): ชื่อวัสดุ → (ชุดลาย Poly Haven, จำนวนลาย/เมตร)
    static readonly Dictionary<string, (string tex, float tiles)> Textured = new Dictionary<string, (string, float)>
    {
        { "Wood", ("weathered_brown_planks", 0.55f) }, { "WoodDark", ("weathered_brown_planks", 0.55f) },
        { "Thatch", ("thatch_roof_angled", 0.45f) }, { "Roof", ("thatch_roof_angled", 0.45f) },
        { "Bark", ("bark_brown_02", 0.8f) }, { "PalmTrunk", ("bark_brown_02", 0.7f) },
    };

    static Shader surfShader;
    static Material SurfMat(string name, Color c, float moss = 0, float sway = 0, bool vertexColor = false, bool instancing = false, float smooth = 0.15f, string tex = null, float tiles = 0.5f)
    {
        var path = $"{MatDir}/Surf_{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(surfShader); AssetDatabase.CreateAsset(m, path); }
        m.shader = surfShader;
        m.SetColor("_BaseColor", c);
        m.SetFloat("_MossAmount", moss);
        m.SetFloat("_Sway", sway);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_UseVertexColor", vertexColor ? 1 : 0);
        if (vertexColor) m.EnableKeyword("_VERTEX_COLOR"); else m.DisableKeyword("_VERTEX_COLOR");
        m.SetFloat("_UseTriplanar", tex != null ? 1 : 0);
        if (tex != null)
        {
            m.EnableKeyword("_TRIPLANAR");
            m.SetTexture("_TriMap", Tex(tex, "Diffuse")); m.SetTexture("_TriNormal", Tex(tex, "nor_gl")); m.SetFloat("_TriScale", tiles);
        }
        else m.DisableKeyword("_TRIPLANAR");
        m.enableInstancing = instancing;
        EditorUtility.SetDirty(m);
        return m;
    }
    static Texture2D Tex(string set, string map) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/{set}_{map}_1k.jpg");
    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

    public static void CreatePaddy()
    {
        Directory.CreateDirectory(MatDir); Directory.CreateDirectory(GenDir); Directory.CreateDirectory("Assets/Game/Scenes");
        AssetDatabase.Refresh();
        ConfigureTextures();
        surfShader = Shader.Find("CozyFishing/Surface");
        var waterShader = Shader.Find("CozyFishing/Water");
        var terrainShader = Shader.Find("CozyFishing/Terrain");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null || surfShader == null || waterShader == null || terrainShader == null) { Debug.LogError("[SceneBuilder] missing model or shaders"); EditorApplication.Exit(1); return; }
        ConfigureUrp();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var world = new GameObject("World").AddComponent<PaddyWorld>();

        // ---------- พื้นดิน (ลายหญ้า/โคลน/คันนา ผสมตามน้ำหนักในสีจุดยอด) ----------
        var terrain = new GameObject("Terrain");
        terrain.AddComponent<MeshFilter>().sharedMesh = SaveMesh(BuildTerrain(), "PaddyTerrain");
        var tr = terrain.AddComponent<MeshRenderer>();
        var tm = LoadOrCreate($"{MatDir}/Terrain_Paddy.mat", terrainShader);
        foreach (var (slot, set) in new[] { ("Grass", "leafy_grass"), ("Mud", "brown_mud_02"), ("Path", "grass_path_3") })
        {
            tm.SetTexture($"_{slot}Map", Tex(set, "Diffuse")); tm.SetTexture($"_{slot}Normal", Tex(set, "nor_gl")); tm.SetTexture($"_{slot}Rough", Tex(set, "Rough"));
        }
        tm.SetVector("_Tiles", new Vector4(0.3f, 0.35f, 0.45f, 0.03f));
        tm.SetColor("_GrassTint", Hex("#bcd995")); tm.SetColor("_MountTint", Hex("#7e9a78"));
        tr.sharedMaterial = tm;
        tr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- ผิวน้ำบ่อ + น้ำขังในแปลงนา (shader เดียวกัน ตั้งค่าต่างกัน) ----------
        var water = new GameObject("PondWater");
        water.AddComponent<MeshFilter>().sharedMesh = SaveMesh(Grid(28, 28, 70, new Vector3(0, PaddyWorld.Water, PaddyWorld.Pond.y)), "PondWater");
        var wm = LoadOrCreate($"{MatDir}/Water_Paddy.mat", waterShader);
        wm.SetColor("_Shallow", Hex("#c9dcb4")); wm.SetColor("_Deep", Hex("#2f4f3a"));
        wm.SetFloat("_DepthScale", 1.6f); wm.SetFloat("_Clarity", 0.5f); wm.SetFloat("_Glint", 1.5f); wm.SetFloat("_Refraction", 0.03f); wm.SetFloat("_Reflect", 0.85f); wm.SetFloat("_FoamWidth", 0.05f);
        var wr = water.AddComponent<MeshRenderer>(); wr.sharedMaterial = wm; wr.shadowCastingMode = ShadowCastingMode.Off;
        var paddyWater = new GameObject("PaddyWater");
        paddyWater.AddComponent<MeshFilter>().sharedMesh = SaveMesh(BuildPaddySheet(), "PaddySheet");
        var pwm = LoadOrCreate($"{MatDir}/Water_PaddyField.mat", waterShader);
        pwm.SetColor("_Shallow", Hex("#b8b48e")); pwm.SetColor("_Deep", Hex("#4a4a30"));
        pwm.SetFloat("_DepthScale", 0.5f); pwm.SetFloat("_Clarity", 0.6f); pwm.SetFloat("_Glint", 1f); pwm.SetFloat("_Waves", 0.004f);
        pwm.SetFloat("_FoamWidth", 0); pwm.SetFloat("_Refraction", 0.01f); pwm.SetFloat("_Reflect", 0.4f);
        var pr = paddyWater.AddComponent<MeshRenderer>(); pr.sharedMaterial = pwm; pr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- โมเดลจาก Blender ----------
        var colliders = world.colliders;
        GameObject Place(string node, float x, float z, float scale = 1, float yaw = 0, float? y = null, float collide = 0)
        {
            var src = FindDeep(model.transform, node);
            if (src == null) { Debug.LogWarning("[SceneBuilder] node not found: " + node); return null; }
            var go = Object.Instantiate(src.gameObject);
            go.name = node;
            go.transform.SetPositionAndRotation(new Vector3(x, y ?? PaddyWorld.Ground(x, z) - 0.05f, z), Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.one * scale;
            Retint(go);
            if (collide > 0) colliders.Add(new Vector3(x, z, collide * scale));
            return go;
        }
        // ---------- โมเดลสมจริงจาก Poly Haven (ใช้วัสดุ PBR ที่ glTFast สร้างจากไฟล์) ----------
        var natureRoot = new GameObject("Nature").transform;
        GameObject Nature(string file, float x, float z, float scale, float yaw, float collide = 0, bool shadows = true, float sink = 0.04f)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/{file}.glb");
            if (src == null) { Debug.LogWarning("[SceneBuilder] nature model missing: " + file); return null; }
            var go = Object.Instantiate(src, natureRoot);
            go.name = file;
            go.transform.SetPositionAndRotation(new Vector3(x, PaddyWorld.Ground(x, z) - sink, z), Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.one * scale;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (collide > 0) colliders.Add(new Vector3(x, z, collide));
            return go;
        }
        var rnd = new System.Random(808);
        float Rf(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        Place("Hut", -10.5f, -7.5f, 1.15f, 30, null, 1.6f);
        // ต้นไม้ใหญ่ (Jacaranda) · ตาลโตนดยังใช้โมเดลเดิม (Poly Haven ไม่มีต้นตาล)
        foreach (var (x, z, s) in new[] { (-7.5f, -9.5f, 0.3f), (11f, -6.5f, 0.27f), (14.5f, 6f, 0.25f), (-15f, 4f, 0.28f) })
            Nature("jacaranda_tree", x, z, s, Rf(0, 360), 0.7f, sink: 0.1f);
        foreach (var (x, z, s) in new[] { (8.5f, -9.5f, 1f), (-13.5f, -10.5f, 0.9f), (16f, -3f, 1.05f) })
            Place("Palm", x, z, s, Rf(0, 360), null, 0.5f);
        // พุ่มไม้
        foreach (var (x, z, s) in new[] { (-5.5f, -8.8f, 0.45f), (6f, -8.5f, 0.5f), (12.5f, 1f, 0.45f), (-12.5f, -1f, 0.5f), (-18f, 14f, 0.7f), (19f, 16f, 0.7f) })
            Nature("shrub_02", x, z, s, Rf(0, 360), 0.6f);
        // โขดหิน
        Nature("rock_moss_set_01", -8.8f, 1.5f, 0.45f, Rf(0, 360), 0.9f, sink: 0.12f);
        Nature("boulder_01", 9.5f, 3.5f, 0.9f, Rf(0, 360), 0.7f, sink: 0.15f);
        Nature("namaqualand_boulder_02", 3.8f, -4.4f, 0.55f, Rf(0, 360), 0.5f, sink: 0.1f);
        Nature("boulder_01", -6f, -3.6f, 0.6f, Rf(0, 360), 0.5f, sink: 0.15f);
        Nature("namaqualand_boulder_02", -9.6f, 4f, 0.6f, Rf(0, 360), 0.6f, sink: 0.1f);
        Nature("rock_moss_set_01", 8.6f, 8f, 0.35f, Rf(0, 360), 0.8f, sink: 0.12f);
        // ไม้เล็กรอบขอบบ่อ (แหวนดินระหว่างบ่อกับแปลงนา) — เว้นทางเดินจากจุดเกิดไปท่าน้ำ
        bool Clear(float x, float z)
        {
            float h = PaddyWorld.Ground(x, z);
            if (h < PaddyWorld.Water + 0.04f || PaddyWorld.Paddy(x, z) > 0.2f) return false;
            if (Mathf.Abs(x - 1.5f) < 1.6f && z < -3) return false;
            if (x > -4.6f && x < -1.8f && z < 2) return false;
            foreach (var c in colliders) if ((new Vector2(x, z) - new Vector2(c.x, c.y)).sqrMagnitude < (c.z + 0.6f) * (c.z + 0.6f)) return false;
            return true;
        }
        void Scatter(string file, int count, float sMin, float sMax, bool shadows, float rMin, float rMax)
        {
            for (int n = 0, tries = 0; n < count && tries < count * 40; tries++)
            {
                float th = Rf(0, Mathf.PI * 2), d = PaddyWorld.PondR(th) + Rf(rMin, rMax);
                float x = PaddyWorld.Pond.x + Mathf.Cos(th) * d, z = PaddyWorld.Pond.y - Mathf.Sin(th) * d;
                if (!Clear(x, z)) continue;
                Nature(file, x, z, Rf(sMin, sMax), Rf(0, 360), 0, shadows, 0.02f);
                n++;
            }
        }
        Scatter("fern_02", 10, 0.5f, 0.8f, true, 0.6f, 3.2f);
        Scatter("shrub_03", 8, 1.2f, 1.8f, false, 0.4f, 3f);
        Scatter("shrub_04", 10, 1.4f, 2.2f, false, 0.3f, 3f);
        Scatter("weed_plant_02", 10, 0.7f, 1.1f, false, 0.2f, 2.5f);
        Scatter("flower_empodium", 14, 0.8f, 1.2f, false, 0.5f, 3f);
        Scatter("stone_01", 14, 2.5f, 5f, false, -0.3f, 1.2f);

        // ท่าไม้: หาแนวฝั่งที่ x = -3.2 แล้วยื่นลงบ่อ
        float dx = -3.2f, sz = -8f;
        while (sz < 4 && PaddyWorld.Ground(dx, sz + 0.6f) >= PaddyWorld.Water - 0.02f) sz += 0.1f;
        float z0 = sz - 0.9f;
        Place("Dock", dx, z0, 1, 0, 0.27f);
        world.dock = new Rect(dx - 0.72f, z0 - 0.1f, 1.44f, 4.4f);

        // ใบบัว + ดอกบัวในบ่อ
        var pad = SurfMat("LotusPad", Hex("#46782e"), smooth: 0.55f); var lotus = SurfMat("Lotus", Hex("#e8a0b8"), smooth: 0.3f);
        foreach (var (cx, cz, r) in new[] { (5.5f, 10.5f, 2.6f), (-6f, 9f, 2f), (7f, 2f, 1.6f), (-8f, 4f, 1.4f), (0f, 14f, 1.8f) })
            for (int i = 0; i < 12; i++)
            {
                float x = cx + Rf(-1, 1) * r, z = cz + Rf(-1, 1) * r;
                if (PaddyWorld.Ground(x, z) >= PaddyWorld.Water - 0.02f) continue;
                var p = Prim(PrimitiveType.Cylinder, "LotusPad", new Vector3(x, PaddyWorld.Water + 0.015f, z), new Vector3(0.9f, 0.005f, 0.9f) * Rf(0.7f, 1.3f), pad);
                p.transform.rotation = Quaternion.Euler(0, Rf(0, 360), 0);
                if (rnd.NextDouble() < 0.3) Prim(PrimitiveType.Sphere, "Lotus", new Vector3(x + 0.15f, PaddyWorld.Water + 0.1f, z), new Vector3(0.22f, 0.18f, 0.22f), lotus);
            }

        // ---------- ตัวละคร + ปลาแม่แบบ ----------
        var fisher = Place("Fisher", 1.5f, -7.6f, 1, 0, 0);
        var player = fisher.AddComponent<PlayerController>();
        player.legL = FindDeep(fisher.transform, "LegL"); player.legR = FindDeep(fisher.transform, "LegR");
        player.armL = FindDeep(fisher.transform, "ArmL"); player.armR = FindDeep(fisher.transform, "ArmR");
        player.head = FindDeep(fisher.transform, "Head"); player.rodTip = FindDeep(fisher.transform, "RodTip");
        var fishTpl = Place("Fish", 0, 0, 1, 0, -50);
        fishTpl.SetActive(false);

        // ---------- แสง ฟ้า HDRI หมอก กล้อง post-processing ----------
        var sunGo = new GameObject("Sun");
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional; sun.color = Hex("#ffe9cc"); sun.intensity = 2.1f; sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.85f;
        sunGo.transform.rotation = Quaternion.Euler(42, 150, 0);
        RenderSettings.sun = sun;
        var sky = LoadOrCreate($"{MatDir}/Sky_Kloofendal.mat", Shader.Find("Skybox/Cubemap"));
        sky.SetTexture("_Tex", AssetDatabase.LoadAssetAtPath<Cubemap>(HdriPath));
        sky.SetFloat("_Exposure", 1.05f); sky.SetFloat("_Rotation", 150);
        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Skybox; RenderSettings.ambientIntensity = 1.1f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox; RenderSettings.defaultReflectionResolution = 256; RenderSettings.reflectionIntensity = 1;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = Hex("#c6d6dc");
        RenderSettings.fogStartDistance = 35; RenderSettings.fogEndDistance = 130;
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 40; cam.nearClipPlane = 0.3f; cam.farClipPlane = 300;
        cam.clearFlags = CameraClearFlags.Skybox;
        var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;
        camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        var rig = camGo.AddComponent<CameraRig>();
        rig.player = player;
        var volume = new GameObject("PostFx").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = BuildPostProfile();

        // ---------- ระบบเกม ----------
        var sys = new GameObject("Game");
        sys.AddComponent<WaterFx>();
        var foliage = sys.AddComponent<FoliageRenderer>();
        foliage.material = SurfMat("Foliage", Color.white, sway: 0.0f, vertexColor: true, instancing: true, smooth: 0.2f);
        var hud = new GameObject("Hud").AddComponent<Hud>();
        hud.cam = cam;
        var fishing = sys.AddComponent<FishingController>();
        fishing.player = player; fishing.hud = hud; fishing.fishTemplate = fishTpl;
        // ทุ่น (แดง-ขาว) · วงเล็ง · สายเบ็ด
        var bobber = new GameObject("Bobber");
        Prim(PrimitiveType.Sphere, "Top", Vector3.zero, Vector3.one * 0.22f, SurfMat("BobberRed", Hex("#d23a2c"), smooth: 0.6f)).transform.SetParent(bobber.transform, false);
        Prim(PrimitiveType.Sphere, "Bottom", new Vector3(0, -0.03f, 0), new Vector3(0.215f, 0.16f, 0.215f), SurfMat("BobberWhite", Hex("#f2f2f2"), smooth: 0.6f)).transform.SetParent(bobber.transform, false);
        Prim(PrimitiveType.Cylinder, "Stick", new Vector3(0, 0.17f, 0), new Vector3(0.024f, 0.08f, 0.024f), SurfMat("BobberRed", Hex("#d23a2c"), smooth: 0.6f)).transform.SetParent(bobber.transform, false);
        Prim(PrimitiveType.Sphere, "Glow", new Vector3(0, 0.26f, 0), Vector3.one * 0.056f, UnlitMat("BobberGlow", Hex("#c8ff5a"))).transform.SetParent(bobber.transform, false);
        var aim = Prim(PrimitiveType.Cylinder, "AimRing", new Vector3(0, -50, 0), new Vector3(0.8f, 0.005f, 0.8f), UnlitMat("Aim", Color.white));
        aim.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var line = new GameObject("FishingLine").AddComponent<LineRenderer>();
        line.widthMultiplier = 0.014f; line.sharedMaterial = UnlitMat("Line", Hex("#f8f8f0")); line.shadowCastingMode = ShadowCastingMode.Off;
        fishing.bobber = bobber; fishing.aim = aim; fishing.line = line;
        rig.fishing = fishing;
        var root = sys.AddComponent<GameRoot>();
        root.world = world; root.player = player; root.fishing = fishing; root.rig = rig;

        EditorSceneManager.SaveScene(scene, ScenePath);
        // อบแสงฟ้า (ambient SH + reflection) จาก HDRI — ไม่อบ lightmap
        Lightmapping.bakedGI = false; Lightmapping.realtimeGI = false;
        if (!Lightmapping.Bake()) Debug.LogWarning("[SceneBuilder] environment bake failed");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[SceneBuilder] saved " + ScenePath);
    }

    // ---------- ตัวช่วย ----------
    static VolumeProfile BuildPostProfile()
    {
        var path = $"{MatDir}/PostFx_Paddy.asset";
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) != null) AssetDatabase.DeleteAsset(path);
        var p = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(p, path);
        T Add<T>() where T : VolumeComponent { var c = p.Add<T>(true); c.name = typeof(T).Name; AssetDatabase.AddObjectToAsset(c, p); return c; }
        Add<Tonemapping>().mode.value = TonemappingMode.ACES;
        var bloom = Add<Bloom>(); bloom.threshold.value = 1.0f; bloom.intensity.value = 0.45f; bloom.scatter.value = 0.65f; bloom.highQualityFiltering.value = false;
        var ca = Add<ColorAdjustments>(); ca.postExposure.value = 0.35f; ca.contrast.value = 14; ca.saturation.value = 10;
        var wb = Add<WhiteBalance>(); wb.temperature.value = 4; wb.tint.value = 2;
        var smh = Add<ShadowsMidtonesHighlights>(); smh.shadows.value = new Vector4(0.95f, 0.98f, 1.06f, 0); smh.highlights.value = new Vector4(1.04f, 1.0f, 0.95f, 0);
        var vig = Add<Vignette>(); vig.intensity.value = 0.24f; vig.smoothness.value = 0.45f;
        EditorUtility.SetDirty(p);
        return p;
    }

    static void ConfigureTextures()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexDir }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            bool normal = path.Contains("_nor_gl_"), linear = path.Contains("_Rough_");
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = !normal && !linear;
            ti.maxTextureSize = 1024;
            ti.mipmapEnabled = true; ti.wrapMode = TextureWrapMode.Repeat; ti.anisoLevel = 4;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.SaveAndReimport();
        }
        var hi = (TextureImporter)AssetImporter.GetAtPath(HdriPath);
        if (hi != null)
        {
            hi.textureShape = TextureImporterShape.TextureCube;
            hi.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
            hi.maxTextureSize = 1024; hi.mipmapEnabled = true;
            hi.textureCompression = TextureImporterCompression.Compressed;
            hi.SaveAndReimport();
        }
    }

    static void Retint(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var key = (mats[i] ? mats[i].name : "").Replace(" (Instance)", "");
                if (Palette.TryGetValue(key, out var p))
                {
                    var tex = Textured.TryGetValue(key, out var t) ? t : (null, 0.5f);
                    mats[i] = SurfMat(key, Hex(p.hex), p.moss, p.sway, smooth: key == "Fish" ? 0.6f : 0.12f, tex: tex.Item1, tiles: tex.Item2);
                }
                else mats[i] = SurfMat("Default", mats[i] ? mats[i].color : Color.white);
            }
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.On;
        }
    }

    static Material LoadOrCreate(string path, Shader shader)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path) ?? CreateAsset(new Material(shader), path);
        m.shader = shader;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material UnlitMat(string name, Color c)
    {
        var m = LoadOrCreate($"{MatDir}/Unlit_{name}.mat", Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", c);
        return m;
    }

    static GameObject Prim(PrimitiveType t, string name, Vector3 pos, Vector3 scale, Material m)
    {
        var g = GameObject.CreatePrimitive(t);
        g.name = name;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.position = pos; g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    static T CreateAsset<T>(T obj, string path) where T : Object { AssetDatabase.CreateAsset(obj, path); return obj; }

    static Mesh SaveMesh(Mesh m, string name)
    {
        var path = $"{GenDir}/{name}.asset";
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (old != null) { EditorUtility.CopySerialized(m, old); return old; }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static Mesh Grid(float w, float d, int segs, Vector3 center)
    {
        var v = new List<Vector3>(); var tri = new List<int>();
        for (int j = 0; j <= segs; j++) for (int i = 0; i <= segs; i++) v.Add(center + new Vector3((i / (float)segs - 0.5f) * w, 0, (j / (float)segs - 0.5f) * d));
        for (int j = 0; j < segs; j++) for (int i = 0; i < segs; i++) { int a = j * (segs + 1) + i, b = a + segs + 1; tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
        var m = new Mesh { indexFormat = IndexFormat.UInt32 };
        m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static float SStep(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }

    // น้ำหนักลายผิวในสีจุดยอด (ดู Terrain.shader): r = โคลน, g = คันนา/ทางดิน, b = เปียก/ลึก, a = เนินไกล
    static Mesh BuildTerrain()
    {
        var m = Grid(110, 110, 200, new Vector3(0, 0, 8));
        var v = m.vertices; var cols = new Color[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            float x = v[i].x, z = v[i].z, h = PaddyWorld.Ground(x, z);
            v[i].y = h;
            float pw = PaddyWorld.Paddy(x, z), dw = PaddyWorld.DikeAt(x, z);
            float mud = pw * (1 - dw), path = pw * dw, wet = 0;
            // ทางเดินดินจากจุดเกิดลงไปท่าน้ำ
            path = Mathf.Max(path, SStep(1.5f, 0.6f, Mathf.Abs(x - 1.5f + Mathf.Sin(z * 0.5f) * 0.3f)) * SStep(-12f, -10f, z) * SStep(-3.5f, -5f, z));
            if (h < PaddyWorld.Water + 0.25f && pw < 0.5f)
            {
                mud = Mathf.Max(mud, SStep(PaddyWorld.Water + 0.25f, PaddyWorld.Water - 0.05f, h));
                wet = SStep(PaddyWorld.Water + 0.05f, PaddyWorld.Water - 1.6f, h);
            }
            wet = Mathf.Max(wet, pw * (1 - dw) * 0.45f);
            cols[i] = new Color(mud, path, wet, SStep(34, 40, z));
        }
        m.vertices = v; m.colors = cols; m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static Mesh BuildPaddySheet()
    {
        var v = new List<Vector3>(); var tri = new List<int>();
        for (float x = -55; x < 55; x += 1) for (float z = -39; z < 34; z += 1)
        {
            var q = new[] { new Vector3(x, 0, z), new Vector3(x + 1, 0, z), new Vector3(x + 1, 0, z + 1), new Vector3(x, 0, z + 1) };
            bool ok = true; foreach (var p in q) if (PaddyWorld.Paddy(p.x, p.z) < 0.3f) ok = false;
            if (!ok) continue;
            int b = v.Count;
            foreach (var p in q) v.Add(new Vector3(p.x, PaddyWorld.PaddyY, p.z));
            tri.AddRange(new[] { b, b + 3, b + 1, b + 1, b + 3, b + 2 });
        }
        var m = new Mesh { indexFormat = IndexFormat.UInt32 };
        m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // URP: depth + opaque texture (น้ำหักเห), HDR, เงานุ่ม
    static void ConfigureUrp()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
        {
            var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
            var so = new SerializedObject(a);
            void Set(string prop, System.Action<SerializedProperty> f) { var sp = so.FindProperty(prop); if (sp != null) f(sp); else Debug.LogWarning("[SceneBuilder] URP prop missing: " + prop); }
            Set("m_RequireDepthTexture", s => s.boolValue = true);
            Set("m_RequireOpaqueTexture", s => s.boolValue = true);
            Set("m_SupportsHDR", s => s.boolValue = true);
            Set("m_ShadowDistance", s => s.floatValue = 50);
            Set("m_SoftShadowsSupported", s => s.boolValue = true);
            if (a.name.StartsWith("Mobile"))
            {
                Set("m_RenderScale", s => s.floatValue = 1f);
                Set("m_ShadowCascadeCount", s => s.intValue = 2);
                Set("m_MainLightShadowmapResolution", s => s.intValue = 2048);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(a);
        }
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t) { var r = FindDeep(c, name); if (r != null) return r; }
        return null;
    }
}
