// สร้างฉากด่านบ่อปลาทุ่งนาจากโมเดล Blender (assets.glb) + พื้น/บ่อที่สร้างจากฟังก์ชันความสูงของเวอร์ชันเว็บ
//   Unity.exe -batchmode -projectPath . -executeMethod SceneBuilder.CreatePaddy -quit
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
    const string ScenePath = "Assets/Game/Scenes/Paddy.unity";
    const string MatDir = "Assets/Game/Materials";
    const string GenDir = "Assets/Game/Generated";

    // สีวัสดุแต่ละชื่อ (ชื่อเดียวกับวัสดุใน Blender) — เทียบตาราง MAT ใน main.js ของเวอร์ชันเว็บ
    static readonly Dictionary<string, (string hex, float moss, float sway)> Palette = new Dictionary<string, (string, float, float)>
    {
        { "Cliff", ("#9a8b78", 0.7f, 0) }, { "Rock", ("#958f84", 0.6f, 0) }, { "Stone", ("#9a958c", 0.28f, 0) }, { "Bark", ("#6e4d35", 0, 0) },
        { "Pine", ("#3b7a4f", 0, 0.05f) }, { "Leaf", ("#64a644", 0, 0.07f) }, { "Leaf2", ("#74b04a", 0, 0) }, { "Skin", ("#f3c6a0", 0, 0) },
        { "Shirt", ("#3b5a96", 0, 0) }, { "Pants", ("#3d3843", 0, 0) }, { "Hat", ("#ebcd8e", 0, 0) }, { "HatBand", ("#c0573e", 0, 0) },
        { "Rod", ("#d9b56c", 0, 0) }, { "Eye", ("#1a1a1a", 0, 0) }, { "Hair", ("#2c231e", 0, 0) }, { "Wood", ("#b08257", 0, 0) },
        { "WoodDark", ("#75553a", 0, 0) }, { "Fish", ("#ffffff", 0, 0) }, { "FishFin", ("#dddddd", 0, 0) }, { "BoatPaint", ("#2f8fa8", 0, 0) },
        { "BoatRed", ("#c0453a", 0, 0) }, { "Ribbon", ("#ff8fc0", 0, 0) }, { "Ribbon2", ("#ffd84a", 0, 0) }, { "Metal", ("#55585e", 0, 0) },
        { "Roof", ("#a5543c", 0, 0) }, { "Window", ("#3a2a1c", 0, 0) }, { "Palm", ("#5aa840", 0, 0.05f) }, { "PalmTrunk", ("#9a7a56", 0, 0) },
        { "Karst", ("#c9c2b2", 0.9f, 0) }, { "Thatch", ("#c9a45e", 0, 0) }, { "Clay", ("#a0583a", 0, 0) },
    };

    static Shader toonShader;
    static Material ToonMat(string name, Color c, float moss = 0, float sway = 0, bool vertexColor = false, bool instancing = false)
    {
        var path = $"{MatDir}/Toon_{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(toonShader); AssetDatabase.CreateAsset(m, path); }
        m.shader = toonShader;
        m.SetColor("_BaseColor", c);
        m.SetFloat("_MossAmount", moss);
        m.SetFloat("_Sway", sway);
        m.SetFloat("_UseVertexColor", vertexColor ? 1 : 0);
        if (vertexColor) m.EnableKeyword("_VERTEX_COLOR"); else m.DisableKeyword("_VERTEX_COLOR");
        m.enableInstancing = instancing;
        EditorUtility.SetDirty(m);
        return m;
    }
    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

    public static void CreatePaddy()
    {
        Directory.CreateDirectory(MatDir); Directory.CreateDirectory(GenDir); Directory.CreateDirectory("Assets/Game/Scenes");
        AssetDatabase.Refresh();
        toonShader = Shader.Find("CozyFishing/Toon");
        var waterShader = Shader.Find("CozyFishing/Water");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null || toonShader == null || waterShader == null) { Debug.LogError("[SceneBuilder] missing model or shaders"); EditorApplication.Exit(1); return; }
        ConfigureUrp();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var world = new GameObject("World").AddComponent<PaddyWorld>();

        // ---------- พื้นดิน (สีตามความสูง/แปลงนา/คันนา) ----------
        var terrain = new GameObject("Terrain");
        terrain.AddComponent<MeshFilter>().sharedMesh = SaveMesh(BuildTerrain(), "PaddyTerrain");
        var tr = terrain.AddComponent<MeshRenderer>();
        tr.sharedMaterial = ToonMat("Terrain", Color.white, vertexColor: true);
        tr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- ผิวน้ำบ่อ ----------
        var water = new GameObject("PondWater");
        water.AddComponent<MeshFilter>().sharedMesh = SaveMesh(Grid(28, 28, 70, new Vector3(0, PaddyWorld.Water, PaddyWorld.Pond.y)), "PondWater");
        var wmPath = $"{MatDir}/Water_Paddy.mat";
        var wm = AssetDatabase.LoadAssetAtPath<Material>(wmPath) ?? CreateAsset(new Material(waterShader), wmPath);
        wm.shader = waterShader;
        wm.SetColor("_Shallow", Hex("#9ccf8a")); wm.SetColor("_Deep", Hex("#3d7a5c"));
        wm.SetFloat("_DepthScale", 1.9f); wm.SetFloat("_Glint", 0.8f);
        var wr = water.AddComponent<MeshRenderer>(); wr.sharedMaterial = wm; wr.shadowCastingMode = ShadowCastingMode.Off;
        // น้ำขังในแปลงนา (แผ่นเดียวใต้ต้นข้าว — ระดับเดียวกับ PaddyY ซ่อนอยู่ใต้ดินที่เป็นคันนาและบ่อ)
        var paddyWater = new GameObject("PaddyWater");
        paddyWater.AddComponent<MeshFilter>().sharedMesh = SaveMesh(BuildPaddySheet(), "PaddySheet");
        var pwm = ToonMat("PaddyWater", Hex("#8fbfae"));
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
        Place("Hut", -10.5f, -7.5f, 1.15f, 30, null, 1.6f);
        foreach (var (n, x, z, s) in new[] { ("Tree1", -7.5f, -9.5f, 1.3f), ("Tree2", 11f, -6.5f, 1.2f), ("Tree1", 14.5f, 6f, 1.1f), ("Tree2", -15f, 4f, 1.2f), ("Palm", 8.5f, -9.5f, 1f), ("Palm", -13.5f, -10.5f, 0.9f), ("Palm", 16f, -3f, 1.05f), ("Pine1", -18f, 14f, 1.2f), ("Pine2", 19f, 16f, 1.1f) })
            Place(n, x, z, s, Random.Range(0, 360f), null, 0.5f);
        foreach (var (x, z, s) in new[] { (-5.5f, -8.8f, 0.8f), (6f, -8.5f, 0.9f), (12.5f, 1f, 0.8f), (-12.5f, -1f, 0.9f) }) Place("Bush", x, z, s, Random.Range(0, 360f), null, 0.6f);
        int bi = 0;
        foreach (var (x, z, s) in new[] { (-8.8f, 1.5f, 0.6f), (9.5f, 3.5f, 0.7f), (3.8f, -4.4f, 0.4f), (-6f, -3.6f, 0.45f), (-9.6f, 4f, 0.5f), (8.6f, 8f, 0.6f) })
            Place("Boulder" + (1 + bi++ % 5), x, z, s * 1.3f, Random.Range(0, 360f), PaddyWorld.Ground(x, z) - 0.1f, 0.8f);
        // ท่าไม้: หาแนวฝั่งที่ x = -3.2 แล้วยื่นลงบ่อ
        float dx = -3.2f, sz = -8f;
        while (sz < 4 && PaddyWorld.Ground(dx, sz + 0.6f) >= PaddyWorld.Water - 0.02f) sz += 0.1f;
        float z0 = sz - 0.9f;
        Place("Dock", dx, z0, 1, 0, 0.27f);
        world.dock = new Rect(dx - 0.72f, z0 - 0.1f, 1.44f, 4.4f);

        // ใบบัว + ดอกบัวในบ่อ
        var pad = ToonMat("LotusPad", Hex("#5c9e3c")); var lotus = ToonMat("Lotus", Hex("#f6a8c0"));
        var rnd = new System.Random(808);
        foreach (var (cx, cz, r) in new[] { (5.5f, 10.5f, 2.6f), (-6f, 9f, 2f), (7f, 2f, 1.6f), (-8f, 4f, 1.4f), (0f, 14f, 1.8f) })
            for (int i = 0; i < 12; i++)
            {
                float x = cx + (float)(rnd.NextDouble() * 2 - 1) * r, z = cz + (float)(rnd.NextDouble() * 2 - 1) * r;
                if (PaddyWorld.Ground(x, z) >= PaddyWorld.Water - 0.02f) continue;
                var p = Prim(PrimitiveType.Cylinder, "LotusPad", new Vector3(x, PaddyWorld.Water + 0.015f, z), new Vector3(0.9f, 0.005f, 0.9f) * (0.7f + (float)rnd.NextDouble() * 0.6f), pad);
                p.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360, 0);
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

        // ---------- แสง ฟ้า หมอก กล้อง ----------
        var sunGo = new GameObject("Sun");
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional; sun.color = Hex("#fff1d6"); sun.intensity = 1.45f; sun.shadows = LightShadows.Soft;
        sunGo.transform.rotation = Quaternion.Euler(45, 150, 0);
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Hex("#dff3ff") * 0.85f; RenderSettings.ambientEquatorColor = Hex("#b9d4b0") * 0.8f; RenderSettings.ambientGroundColor = Hex("#6b8a4a") * 0.7f;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = Hex("#eaf6ee");
        RenderSettings.fogStartDistance = 40; RenderSettings.fogEndDistance = 120;
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 40; cam.nearClipPlane = 0.3f; cam.farClipPlane = 300;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Hex("#9fd2ef");
        camGo.AddComponent<UniversalAdditionalCameraData>();
        var rig = camGo.AddComponent<CameraRig>();
        rig.player = player;

        // ---------- ระบบเกม ----------
        var sys = new GameObject("Game");
        sys.AddComponent<WaterFx>();
        var foliage = sys.AddComponent<FoliageRenderer>();
        foliage.material = ToonMat("Foliage", Color.white, sway: 0.0f, vertexColor: true, instancing: true);
        var hud = new GameObject("Hud").AddComponent<Hud>();
        hud.cam = cam;
        var fishing = sys.AddComponent<FishingController>();
        fishing.player = player; fishing.hud = hud; fishing.fishTemplate = fishTpl;
        // ทุ่น (แดง-ขาว) · วงเล็ง · สายเบ็ด
        var bobber = new GameObject("Bobber");
        Prim(PrimitiveType.Sphere, "Top", Vector3.zero, Vector3.one * 0.22f, ToonMat("BobberRed", Hex("#e2493a"))).transform.SetParent(bobber.transform, false);
        Prim(PrimitiveType.Sphere, "Bottom", new Vector3(0, -0.03f, 0), new Vector3(0.215f, 0.16f, 0.215f), ToonMat("BobberWhite", Color.white)).transform.SetParent(bobber.transform, false);
        Prim(PrimitiveType.Cylinder, "Stick", new Vector3(0, 0.17f, 0), new Vector3(0.024f, 0.08f, 0.024f), ToonMat("BobberRed", Hex("#e2493a"))).transform.SetParent(bobber.transform, false);
        Prim(PrimitiveType.Sphere, "Glow", new Vector3(0, 0.26f, 0), Vector3.one * 0.056f, UnlitMat("BobberGlow", Hex("#c8ff5a"))).transform.SetParent(bobber.transform, false);
        var aim = Prim(PrimitiveType.Cylinder, "AimRing", new Vector3(0, -50, 0), new Vector3(0.8f, 0.005f, 0.8f), UnlitMat("Aim", Color.white));
        aim.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var line = new GameObject("FishingLine").AddComponent<LineRenderer>();
        line.widthMultiplier = 0.018f; line.sharedMaterial = UnlitMat("Line", Hex("#f8f8f0")); line.shadowCastingMode = ShadowCastingMode.Off;
        fishing.bobber = bobber; fishing.aim = aim; fishing.line = line;
        rig.fishing = fishing;
        var root = sys.AddComponent<GameRoot>();
        root.world = world; root.player = player; root.fishing = fishing; root.rig = rig;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[SceneBuilder] saved " + ScenePath);
    }

    // ---------- ตัวช่วย ----------
    static void Retint(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var n = mats[i] ? mats[i].name : "";
                var key = n.Replace(" (Instance)", "");
                if (Palette.TryGetValue(key, out var p)) mats[i] = ToonMat(key, Hex(p.hex), p.moss, p.sway);
                else mats[i] = ToonMat("Default", mats[i] ? mats[i].color : Color.white);
            }
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.On;
        }
    }

    static Material UnlitMat(string name, Color c)
    {
        var path = $"{MatDir}/Unlit_{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path) ?? CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Unlit")), path);
        m.SetColor("_BaseColor", c);
        EditorUtility.SetDirty(m);
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

    static Mesh BuildTerrain()
    {
        var m = Grid(110, 110, 200, new Vector3(0, 0, 8));
        var v = m.vertices; var cols = new Color[v.Length];
        Color grass = Hex("#8cc152"), dry = Hex("#b7c25e"), mud = Hex("#9a8560"), bed = Hex("#6c7a4c"), deep = Hex("#44583c"), paddyMud = Hex("#7d7650"), dike = Hex("#9ab85a"), mount = Hex("#6f9a72");
        for (int i = 0; i < v.Length; i++)
        {
            float x = v[i].x, z = v[i].z, h = PaddyWorld.Ground(x, z);
            v[i].y = h;
            float n = Mathf.Sin(x * 0.4f + Mathf.Sin(-z * 0.3f) * 2) * 0.5f + 0.5f;
            var c = Color.Lerp(grass, dry, n * 0.35f);
            float pw = PaddyWorld.Paddy(x, z), dw = PaddyWorld.DikeAt(x, z);
            if (pw > 0) c = Color.Lerp(Color.Lerp(c, paddyMud, pw * (1 - dw)), dike, pw * dw);
            if (h < PaddyWorld.Water + 0.25f && pw < 0.5f)
            {
                c = Color.Lerp(c, mud, SStep(PaddyWorld.Water + 0.25f, PaddyWorld.Water - 0.1f, h));
                c = Color.Lerp(c, bed, SStep(PaddyWorld.Water - 0.1f, PaddyWorld.Water - 0.9f, h));
                c = Color.Lerp(c, deep, SStep(PaddyWorld.Water - 0.9f, PaddyWorld.Water - 2.1f, h));
            }
            if (z > 34) c = Color.Lerp(c, mount, SStep(34, 40, z));
            cols[i] = c;
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

    static void ConfigureUrp()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
        {
            var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
            a.supportsCameraDepthTexture = true;
            a.shadowDistance = 60;
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
