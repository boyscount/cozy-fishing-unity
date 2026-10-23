// ส่วนที่ใช้ร่วมกันทุกด่าน (ตัวละคร กล้อง แสง ระบบเกม) + ฉากด่านใหม่
//   Unity.exe -batchmode -projectPath . -executeMethod SceneBuilder.CreateAll -quit   (สร้างทุกฉาก + ตั้ง Build Settings)
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CozyFishing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static partial class SceneBuilder
{
    // ลำดับฉากใน build (ฉากแรก = บ้าน/บ่อทุ่งนา)
    static readonly string[] StageScenes = { "Paddy", "Rapids", "Temple", "Marsh", "Houseboat", "Bend", "Cave", "Mangrove" };

    public static void CreateAll()
    {
        CreatePaddy();
        CreateRapids();
        CreateTemple();
        CreateMarsh();
        CreateHouseboat();
        CreateBend();
        CreateCave();
        CreateMangrove();
    }

    // บรรยากาศแสงของแต่ละด่าน
    public class Look
    {
        public string sun = "#ffe9cc"; public float sunIntensity = 2.1f; public Vector2 sunRot = new Vector2(42, 150);
        public string fog = "#c6d6dc"; public float fogStart = 35, fogEnd = 130; public float exposure = 1.05f, ambient = 1.1f;
        public string skyId, skyTint;   // ตั้งค่า = ใช้วัสดุฟ้าแยกของด่าน (เช่นฟ้าครึ้มหมอกเทา) ไม่ไปแก้ฟ้าที่ด่านอื่นใช้ร่วม
    }

    public class GameplayRefs { public StoryDirector director; public Hud hud; public PlayerController player; public Light sun; public Camera cam; }

    static GameObject PlaceNode(GameObject model, string node, Vector3 pos, float yaw = 0, float scale = 1)
    {
        var src = FindDeep(model.transform, node);
        if (src == null) { Debug.LogWarning("[SceneBuilder] node not found: " + node); return null; }
        var go = Object.Instantiate(src.gameObject);
        go.name = node;
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        go.transform.localScale = Vector3.one * scale;
        Retint(go);
        return go;
    }

    // ตัวละคร (Mixamo) · ปลาแม่แบบ · แสง/ฟ้า/หมอก · กล้อง + post · HUD · ระบบตกปลา · กล้องรูปปลา · ลำดับวัน
    static GameplayRefs BuildGameplay(MonoBehaviour world, GameObject model, Vector3 spawn, Look look, StageWorld stage)
    {
        var r = new GameplayRefs();
        var fisher = PlaceNode(model, "Fisher", spawn);
        var player = r.player = fisher.AddComponent<PlayerController>();
        player.legL = FindDeep(fisher.transform, "LegL"); player.legR = FindDeep(fisher.transform, "LegR");
        player.armL = FindDeep(fisher.transform, "ArmL"); player.armR = FindDeep(fisher.transform, "ArmR");
        player.head = FindDeep(fisher.transform, "Head"); player.rodTip = FindDeep(fisher.transform, "RodTip");
        if (File.Exists(CharacterSetup.ModelPath)) { CharacterSetup.Configure(); player.animator = CharacterSetup.Attach(fisher); }
        var fishTpl = PlaceNode(model, "Fish", new Vector3(0, -50, 0));
        fishTpl.SetActive(false);

        var sunGo = new GameObject("Sun");
        var sun = r.sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional; sun.color = Hex(look.sun); sun.intensity = look.sunIntensity; sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.85f;
        sunGo.transform.rotation = Quaternion.Euler(look.sunRot.x, look.sunRot.y, 0);
        RenderSettings.sun = sun;
        Material sky;
        if (look.skyId == "Overcast")
        {
            // ฟ้าครึ้มทั้งผืน: ฟ้าไล่สีเทาเรียบ ๆ (HDRI เป็นฟ้าใส ย้อมสีแล้วยังฟ้า · procedural sky ออกส้มเหมือนเย็น)
            var skyName = look.skyTint == "#76808a" || look.skyTint == null ? "Sky_Overcast" : "Sky_Overcast" + look.skyTint.Replace("#", "_");   // แยกวัสดุตามสี ไม่ทับฟ้าของด่านอื่น
            sky = LoadOrCreate($"{MatDir}/{skyName}.mat", Shader.Find("CozyFishing/SkyGradient"));
            var fogC = Hex(look.fog);
            sky.SetColor("_Horizon", fogC); sky.SetColor("_Ground", fogC * 0.9f);
            sky.SetColor("_Zenith", Hex(look.skyTint ?? "#7a8490"));
        }
        else
        {
            sky = LoadOrCreate($"{MatDir}/Sky_{look.skyId ?? "Kloofendal"}.mat", Shader.Find("Skybox/Cubemap"));
            sky.SetTexture("_Tex", AssetDatabase.LoadAssetAtPath<Cubemap>(HdriPath));
            sky.SetFloat("_Exposure", look.exposure); sky.SetFloat("_Rotation", 150);
        }
        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Skybox; RenderSettings.ambientIntensity = look.ambient;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox; RenderSettings.defaultReflectionResolution = 256; RenderSettings.reflectionIntensity = 1;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = Hex(look.fog);
        RenderSettings.fogStartDistance = look.fogStart; RenderSettings.fogEndDistance = look.fogEnd;
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = r.cam = camGo.AddComponent<Camera>();
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

        var sys = new GameObject("Game");
        sys.AddComponent<WaterFx>();
        var foliage = sys.AddComponent<FoliageRenderer>();
        var gm = LoadOrCreate($"{MatDir}/Grass_Field.mat", Shader.Find("CozyFishing/Grass"));
        gm.enableInstancing = true;
        foliage.material = gm; foliage.stage = stage;
        BuildHudPanel();
        var hud = r.hud = new GameObject("Hud").AddComponent<Hud>();
        hud.cam = cam;
        var fishing = sys.AddComponent<FishingController>();
        fishing.player = player; fishing.hud = hud; fishing.fishTemplate = fishTpl;
        var pvGo = new GameObject("FishPreview");
        var pvCam = pvGo.AddComponent<Camera>();
        pvCam.clearFlags = CameraClearFlags.SolidColor; pvCam.backgroundColor = new Color(0, 0, 0, 0);
        pvCam.fieldOfView = 30; pvCam.nearClipPlane = 0.1f; pvCam.farClipPlane = 10; pvCam.cullingMask = 1 << FishPreview.Layer;
        var pvData = pvGo.AddComponent<UniversalAdditionalCameraData>(); pvData.renderPostProcessing = false; pvData.renderShadows = false;
        var preview = pvGo.AddComponent<FishPreview>(); preview.cam = pvCam; preview.fishing = fishing;
        fishing.preview = preview; hud.preview = preview;
        cam.cullingMask &= ~(1 << FishPreview.Layer);
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
        var cycle = sys.AddComponent<DayCycle>(); cycle.sun = sun;
        var director = r.director = sys.AddComponent<StoryDirector>();
        director.fishing = fishing; director.player = player; director.rig = rig; director.hud = hud; director.sky = cycle;
        hud.director = director; fishing.director = director; root.director = director;
        return r;
    }

    // บันทึกฉาก + อบแสงฟ้า + ตั้ง Build Settings ตามฉากที่มีอยู่
    static void SaveStage(Scene scene, string path)
    {
        EditorSceneManager.SaveScene(scene, path);
        Lightmapping.bakedGI = false; Lightmapping.realtimeGI = false;
        Lightmapping.Clear(); Lightmapping.ClearDiskCache();   // แคชจากการอบที่ไม่มี GPU (-nographics) ทำให้แสงรอบทิศเป็น NaN → ฉากดำทั้งฉาก
        if (!Lightmapping.Bake()) Debug.LogWarning("[SceneBuilder] environment bake failed");
        if (float.IsNaN(RenderSettings.ambientProbe[0, 0])) Debug.LogError("[SceneBuilder] ambient probe is NaN (bake without GPU?)");
        EditorSceneManager.SaveScene(scene, path);
        EditorBuildSettings.scenes = StageScenes.Select(n => $"Assets/Game/Scenes/{n}.unity").Where(File.Exists).Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("[SceneBuilder] saved " + path);
    }

    static Material TerrainMat(string name, (string grass, string mud, string path) sets, Vector4 tiles, string grassTint, string mountTint)
    {
        var tm = LoadOrCreate($"{MatDir}/Terrain_{name}.mat", Shader.Find("CozyFishing/Terrain"));
        foreach (var (slot, set) in new[] { ("Grass", sets.grass), ("Mud", sets.mud), ("Path", sets.path) })
        {
            tm.SetTexture($"_{slot}Map", Tex(set, "Diffuse")); tm.SetTexture($"_{slot}Normal", Tex(set, "nor_gl")); tm.SetTexture($"_{slot}Rough", Tex(set, "Rough"));
        }
        tm.SetVector("_Tiles", tiles);
        tm.SetColor("_GrassTint", Hex(grassTint)); tm.SetColor("_MountTint", Hex(mountTint));
        return tm;
    }

    static Material ParticleMat(string name, Color c)
    {
        var src = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
        var path = $"{MatDir}/Particle_{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, path); }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        EditorUtility.SetDirty(m);
        return m;
    }

    static ParticleSystem Particles(string name, Vector3 pos, Vector3 box, Material mat, float rate, Vector2 size, Vector2 life, Vector2 speed, Color color, float gravity = 0)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y); main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y); main.startColor = color; main.gravityModifier = gravity;
        main.maxParticles = 400; main.simulationSpace = ParticleSystemSimulationSpace.World; main.prewarm = true; main.loop = true;
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        var em = ps.emission; em.rateOverTime = rate;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = box; sh.rotation = new Vector3(-90, 0, 0);
        var col = ps.colorOverLifetime; col.enabled = true;
        var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.25f), new GradientAlphaKey(0, 1) });
        col.color = gr;
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0.6f, 1, 1.4f));
        var pr = go.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = mat; pr.shadowCastingMode = ShadowCastingMode.Off;
        return ps;
    }

    // ======================================================================
    // ด่าน: แก่งหินตาปลา — แม่น้ำไหลซ้าย→ขวา ช่วงแก่งน้ำตื้นหินโผล่ · วังน้ำลึกท้ายแก่ง · ฝั่งไกลเป็นเนินป่าและหน้าผา
    //      โขดหินยักษ์มีรอยดวงตาปลาเฝ้าวังน้ำ · ผู้เล่นยืนลานหญ้าฝั่งใกล้ / หินแบนที่ก้าวลงไปในน้ำ
    // ======================================================================
    const float RW = -0.3f;
    static float RZc(float x) => 5f + 1.8f * Mathf.Sin(x * 0.11f + 0.5f);
    static float RHw(float x) => 4.0f + 0.7f * Mathf.Sin(x * 0.17f + 1f) + Mathf.Clamp((x - 6f) * 0.35f, 0, 3f);
    static float RapidsGround(float x, float z)
    {
        float zc = RZc(x), hw = RHw(x), d = z - zc, a = Mathf.Abs(d) / hw;
        float n = Mathf.PerlinNoise(x * 0.18f + 3, z * 0.18f + 7) - 0.5f, n2 = Mathf.PerlinNoise(x * 0.6f + 11, z * 0.6f + 5) - 0.5f;
        float bankNear = 0.18f + n * 0.5f + n2 * 0.12f;
        float up = Mathf.Max(0, d - hw);
        float cliff = SStep(-3f, 1f, x) * SStep(11f, 7f, x);                       // หน้าผาหลังหินตาปลา
        float bankFar = 0.25f + up * (0.38f + cliff * 0.5f) + Mathf.Max(0, up - 4) * 0.3f + n * 1.1f + n2 * 0.3f;
        bankFar = Mathf.Min(bankFar, 7f + n * 2);
        float bank = d < 0 ? bankNear : bankFar;
        float depth = 0.45f + 1.5f * (1 - a * a) + SStep(5, 9, x) * 1.0f;
        float rapid = SStep(-6.5f, -4f, x) * SStep(2.5f, 0.5f, x);                  // ช่วงแก่ง: ตื้น
        depth = Mathf.Lerp(depth, 0.4f + n2 * 0.3f, rapid);
        float bed = RW - depth + n2 * 0.15f;
        return Mathf.Lerp(bed, bank, SStep(0.85f, 1.25f, a));
    }

    public static void CreateRapids()
    {
        const string path = "Assets/Game/Scenes/Rapids.unity";
        Directory.CreateDirectory(MatDir); Directory.CreateDirectory(GenDir);
        AssetDatabase.Refresh();
        ConfigureTextures();
        surfShader = Shader.Find("CozyFishing/Surface");
        var waterShader = Shader.Find("CozyFishing/Water");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        ConfigureUrp();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var rnd = new System.Random(4242);
        float Rf(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        // ---------- โลก: อบความสูงลงตาราง ----------
        var world = new GameObject("World").AddComponent<StageWorld>();
        world.stageId = "rapids"; world.waterY = RW;
        world.origin = new Vector2(-40, -22); world.cell = 0.5f; world.nx = 161; world.nz = 121;
        world.heights = new float[world.nx * world.nz];
        for (int j = 0; j < world.nz; j++) for (int i = 0; i < world.nx; i++) world.heights[j * world.nx + i] = RapidsGround(world.origin.x + i * world.cell, world.origin.y + j * world.cell);
        world.walkBounds = new Rect(-17, -12, 34, 32);
        float sx = -1f, sz = RZc(sx) - RHw(sx) - 1.9f;
        world.spawn = new Vector3(sx, 0, sz); world.spawnYaw = 0;
        float ex = 3.6f, ez = RZc(ex) + RHw(ex) - 0.2f;
        world.special = new Vector3(ex, RW, ez - 2.4f); world.specialRadius = 3.2f;
        world.camBounds = new Rect(-12, -8, 24, 16); world.camBias = new Vector2(1.5f, RZc(1.5f)); world.camBiasK = 0.35f;

        // ---------- พื้นดิน: หญ้าป่า / กรวดท้องน้ำ / หน้าผาหิน ----------
        var terrain = new GameObject("Terrain");
        var m = Grid(80, 60, 160, new Vector3(0, 0, 8));
        var v = m.vertices; var cols = new Color[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            float x = v[i].x, z = v[i].z, h = RapidsGround(x, z);
            v[i].y = h;
            float slope = Mathf.Abs(RapidsGround(x + 0.5f, z) - h) + Mathf.Abs(RapidsGround(x, z + 0.5f) - h);
            float gravel = SStep(RW + 0.45f, RW + 0.05f, h);
            float rock = Mathf.Max(SStep(0.35f, 0.75f, slope), SStep(2.5f, 4f, h) * 0.6f);
            float wet = SStep(RW + 0.05f, RW - 1.2f, h);
            cols[i] = new Color(gravel, rock, wet, 0);
        }
        m.vertices = v; m.colors = cols; m.RecalculateNormals(); m.RecalculateBounds();
        terrain.AddComponent<MeshFilter>().sharedMesh = SaveMesh(m, "RapidsTerrain");
        var tr = terrain.AddComponent<MeshRenderer>();
        tr.sharedMaterial = TerrainMat("Rapids", ("leafy_grass", "rocky_gravel", "rock_face_04"), new Vector4(0.3f, 0.55f, 0.16f, 0.03f), "#a9c48a", "#7e9a78");
        tr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- แม่น้ำไหลเชี่ยว ----------
        var water = new GameObject("RiverWater");
        water.AddComponent<MeshFilter>().sharedMesh = SaveMesh(Grid(80, 40, 140, new Vector3(0, RW, 6)), "RapidsWater");
        var wm = LoadOrCreate($"{MatDir}/Water_Rapids.mat", waterShader);
        wm.SetColor("_Shallow", Hex("#c4dccd")); wm.SetColor("_Deep", Hex("#21423c"));
        wm.SetFloat("_DepthScale", 1.3f); wm.SetFloat("_Clarity", 0.6f); wm.SetFloat("_Glint", 1.3f); wm.SetFloat("_Refraction", 0.045f);
        wm.SetFloat("_Reflect", 0.7f); wm.SetFloat("_Waves", 0.035f); wm.SetFloat("_FoamWidth", 0.22f); wm.SetVector("_Flow", new Vector4(0.95f, 0.05f, 0, 0));
        wm.SetVector("_RapidZone", new Vector4(-6.5f, 2.2f, 0.85f, 0));   // ช่วงแก่ง x -6.5..2.2: ฟองขาวเป็นริ้วไหลตามน้ำ
        var wr = water.AddComponent<MeshRenderer>(); wr.sharedMaterial = wm; wr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- ธรรมชาติ ----------
        var natureRoot = new GameObject("Nature").transform;
        GameObject Nature(string file, float x, float z, float scale, float yaw, float collide = 0, bool shadows = true, float sink = 0.04f, float height = 99, float? y = null)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/{file}.glb");
            if (src == null) return null;
            var go = Object.Instantiate(src, natureRoot);
            go.name = file;
            go.transform.SetPositionAndRotation(new Vector3(x, (y ?? RapidsGround(x, z)) - sink, z), Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.one * scale;
            foreach (var rr in go.GetComponentsInChildren<Renderer>())
            {
                rr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                var mats = rr.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = LeafMat(file, mats[i]) ?? mats[i];
                rr.sharedMaterials = mats;
            }
            if (collide > 0) world.colliders.Add(new Vector4(x, z, collide, height));
            return go;
        }

        // แก่ง: หินโผล่เป็นแนวขวางลำน้ำ (น้ำแตกฟองรอบหิน)
        for (int i = 0; i < 14; i++)
        {
            float x = Rf(-6f, 1.8f), zc = RZc(x), hw = RHw(x);
            float z = zc + Rf(-0.85f, 0.85f) * hw;
            Nature(i % 3 == 0 ? "namaqualand_boulder_02" : "boulder_01", x, z, Rf(0.45f, 1.05f), Rf(0, 360), 0, true, 0.25f, 99, RW - 0.25f);
        }
        // หินก้อนใหญ่มีมอสริมตลิ่งทั้งสองฝั่ง
        foreach (var (x, side, s) in new[] { (-12f, -1f, 0.5f), (-8.5f, 1f, 0.6f), (8f, -1f, 0.45f), (12f, 1f, 0.55f), (-3f, 1f, 0.5f), (13f, -1f, 0.4f) })
        {
            float z = RZc(x) + side * (RHw(x) + 0.6f);
            Nature("rock_moss_set_01", x, z, s, Rf(0, 360), side < 0 ? 1.2f : 0, true, 0.15f, 0.8f);
        }
        // หินแบนก้าวลงไปในน้ำ (ยืนเหวี่ยงเบ็ดได้)
        float stx = -7.2f;
        for (int k = 0; k < 2; k++)
        {
            float z = RZc(stx) - RHw(stx) + 0.55f + k * 1.15f, x = stx + k * 0.35f;
            var st = Nature("namaqualand_boulder_02", x, z, 1, Rf(0, 360), 0, true, 0, 99, RW - 0.08f);
            if (st != null) st.transform.localScale = new Vector3(0.62f, 0.42f, 0.62f);
            world.docks.Add(new Rect(x - 0.55f, z - 0.55f, 1.1f, 1.1f)); world.dockY.Add(RW + 0.28f);
        }

        // ---------- หินตาปลา: โขดหินยักษ์ริมฝั่งไกล มีรอยดวงตาจ้องลงวังน้ำ ----------
        var eyeRock = Nature("boulder_01", ex, ez, 4.2f, 180 + 15, 2.6f, true, 0.7f, 99, RW + 0.2f);
        if (eyeRock != null)
        {
            var b = new Bounds(eyeRock.transform.position, Vector3.zero);
            foreach (var rr in eyeRock.GetComponentsInChildren<Renderer>()) b.Encapsulate(rr.bounds);
            var face = new Vector3(b.center.x - 0.2f, b.min.y + b.size.y * 0.58f, b.min.z + 0.55f);
            var eye = new GameObject("FishEye"); eye.transform.position = face;
            const float E = 1.6f;   // ขนาดดวงตา — ฝังเรียบไปกับหิน (แบนมาก) ม่านตาสีทองเรืองนิด ๆ
            Prim(PrimitiveType.Sphere, "Brow", face + new Vector3(0, 0.32f, 0.02f) * E, new Vector3(1.3f, 0.35f, 0.42f) * E, SurfMat("EyeBrow", Hex("#6f6656"), 0.35f, smooth: 0.08f)).transform.SetParent(eye.transform, true);
            Prim(PrimitiveType.Sphere, "Sclera", face, new Vector3(1.05f, 0.6f, 0.2f) * E, SurfMat("EyeSclera", Hex("#d9cfa8"), 0.15f, smooth: 0.3f)).transform.SetParent(eye.transform, true);
            var iris = SurfMat("EyeIris", Hex("#b8861e"), smooth: 0.7f); iris.SetColor("_EmissionColor", new Color(0.35f, 0.22f, 0.02f));
            Prim(PrimitiveType.Sphere, "Iris", face + new Vector3(0, -0.01f, -0.07f) * E, new Vector3(0.55f, 0.55f, 0.1f) * E, iris).transform.SetParent(eye.transform, true);
            Prim(PrimitiveType.Sphere, "Pupil", face + new Vector3(0, -0.01f, -0.11f) * E, new Vector3(0.16f, 0.4f, 0.06f) * E, SurfMat("EyePupil", Hex("#0d0b08"), smooth: 0.95f)).transform.SetParent(eye.transform, true);
        }

        // ป่าบนเนินฝั่งไกล · พุ่มไม้ · ไผ่ · เฟิร์นริมน้ำ
        foreach (var (x, off, s) in new[] { (-14f, 6f, 0.3f), (-6f, 7.5f, 0.27f), (9f, 8f, 0.32f), (15f, 5.5f, 0.28f), (-1f, 10f, 0.3f), (-10f, 11f, 0.33f), (4f, 12f, 0.3f), (12f, 12.5f, 0.34f), (-17f, 10f, 0.3f) })
            Nature("jacaranda_tree", x, RZc(x) + RHw(x) + off, s, Rf(0, 360), 0.8f, true, 0.2f);
        foreach (var (x, off) in new[] { (-10f, 2.5f), (-4f, 3.2f), (7f, 3f), (12f, 2.2f), (-15f, 2f) })
            Nature("shrub_02", x, RZc(x) + RHw(x) + off, Rf(0.4f, 0.6f), Rf(0, 360), 0.6f);
        var bambooCulm = SurfMat("BambooCulm", Color.white, vertexColor: true, smooth: 0.45f);
        var bambooLeaf = SurfMat("BambooLeaf", Color.white, sway: 0.06f, swayStart: 3f, vertexColor: true, smooth: 0.3f, cull: CullMode.Off);
        int bi = 0;
        foreach (var (x, side, off) in new[] { (-13f, -1f, 2.2f), (-9.5f, -1f, 3.6f), (6.5f, -1f, 2.6f), (11f, -1f, 3.4f), (-11f, 1f, 1.6f), (4f, 1f, 4.5f), (14f, 1f, 1.8f), (-6f, 1f, 3f), (9f, 1f, 2.4f), (-15f, 1f, 4f) })
        {
            float z = RZc(x) + side * (RHw(x) + off);
            var go = new GameObject("Bamboo");
            go.transform.SetPositionAndRotation(new Vector3(x, RapidsGround(x, z) - 0.1f, z), Quaternion.Euler(0, Rf(0, 360), 0));
            go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.Bamboo(70 + bi, Rf(6.5f, 9f)), "Bamboo" + bi++);
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { bambooCulm, bambooLeaf };
            world.colliders.Add(new Vector4(x, z, 0.9f, 99));
        }
        for (int i = 0; i < 18; i++)
        {
            float x = Rf(-16, 16), side = i % 3 == 0 ? 1 : -1, z = RZc(x) + side * (RHw(x) + Rf(0.3f, 2.5f));
            if (RapidsGround(x, z) < RW + 0.05f || Mathf.Abs(x - sx) < 1.5f && side < 0) continue;
            Nature(i % 2 == 0 ? "fern_02" : "weed_plant_02", x, z, Rf(0.55f, 0.9f), Rf(0, 360), 0, i % 2 == 0, 0.02f);
        }
        for (int i = 0; i < 10; i++)
        {
            float x = Rf(-16, 16), z = RZc(x) - RHw(x) - Rf(1.5f, 6f);
            if (Mathf.Abs(x - sx) < 1.2f) continue;
            Nature(i % 2 == 0 ? "flower_empodium" : "shrub_04", x, z, Rf(0.9f, 1.6f), Rf(0, 360), 0, false, 0.02f);
        }

        // ละอองน้ำเหนือแก่ง + หมอกจางเหนือวังน้ำ
        var spray = ParticleMat("Spray", new Color(1, 1, 1, 0.5f));
        Particles("RapidsSpray", new Vector3(-2.2f, RW + 0.05f, RZc(-2.2f)), new Vector3(7.5f, RHw(-2.2f) * 1.6f, 0.1f), spray, 45, new Vector2(0.4f, 1.1f), new Vector2(1.2f, 2f), new Vector2(0.3f, 0.9f), new Color(1, 1, 1, 0.38f));
        Particles("PoolMist", new Vector3(8f, RW + 0.4f, RZc(8f)), new Vector3(10f, RHw(8f) * 1.6f, 0.1f), spray, 6, new Vector2(3f, 5.5f), new Vector2(5f, 8f), new Vector2(0.05f, 0.2f), new Color(1, 1, 1, 0.09f));

        // ---------- ตัวละคร กล้อง แสง ระบบเกม ----------
        var look = new Look { sun = "#ffe0b8", sunIntensity = 1.85f, sunRot = new Vector2(36, 125), fog = "#a7b8b0", fogStart = 16, fogEnd = 85, exposure = 1.0f, ambient = 1.05f };
        world.spawn.y = RapidsGround(world.spawn.x, world.spawn.z);
        BuildGameplay(world, model, world.spawn, look, world);
        SaveStage(scene, path);
    }
}
