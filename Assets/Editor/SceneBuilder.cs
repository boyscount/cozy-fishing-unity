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
using UnityEngine.UIElements;

public static partial class SceneBuilder
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
        { "Shirt", ("#5f86c4", 0, 0) }, { "Pants", ("#8a8c9c", 0, 0) }, { "Hat", ("#f0e2c0", 0, 0) }, { "HatBand", ("#a54a36", 0, 0) },
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
        { "Shirt", ("rough_linen", 3f) }, { "Pants", ("denim_fabric", 3f) }, { "Hat", ("reed_roof_04", 2.2f) },
    };

    static Shader surfShader;
    static Material SurfMat(string name, Color c, float moss = 0, float sway = 0, bool vertexColor = false, bool instancing = false, float smooth = 0.15f, string tex = null, float tiles = 0.5f, float swayStart = 1.5f, CullMode cull = CullMode.Back)
    {
        var path = $"{MatDir}/Surf_{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(surfShader); AssetDatabase.CreateAsset(m, path); }
        m.shader = surfShader;
        m.SetColor("_BaseColor", c);
        m.SetFloat("_MossAmount", moss);
        m.SetFloat("_Sway", sway); m.SetFloat("_SwayStart", swayStart); m.SetFloat("_Cull", (float)cull);
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
            if (collide > 0) colliders.Add(new Vector4(x, z, collide * scale, 99));
            return go;
        }
        // ---------- โมเดลสมจริงจาก Poly Haven (ใช้วัสดุ PBR ที่ glTFast สร้างจากไฟล์) ----------
        var natureRoot = new GameObject("Nature").transform;
        GameObject Nature(string file, float x, float z, float scale, float yaw, float collide = 0, bool shadows = true, float sink = 0.04f, float height = 99)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/{file}.glb");
            if (src == null) { Debug.LogWarning("[SceneBuilder] nature model missing: " + file); return null; }
            var go = Object.Instantiate(src, natureRoot);
            go.name = file;
            go.transform.SetPositionAndRotation(new Vector3(x, PaddyWorld.Ground(x, z) - sink, z), Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.one * scale;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = LeafMat(file, mats[i]) ?? mats[i];
                r.sharedMaterials = mats;
            }
            if (collide > 0) colliders.Add(new Vector4(x, z, collide, height));
            return go;
        }
        var rnd = new System.Random(808);
        float Rf(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        var hut = Place("Hut", -10.5f, -7.5f, 1.15f, 30, null, 1.6f);
        // ต้นไม้ใหญ่ (Jacaranda) · ตาลโตนดยังใช้โมเดลเดิม (Poly Haven ไม่มีต้นตาล)
        foreach (var (x, z, s) in new[] { (-7.5f, -9.5f, 0.3f), (11f, -6.5f, 0.27f), (14.5f, 6f, 0.25f), (-15f, 4f, 0.28f) })
            Nature("jacaranda_tree", x, z, s, Rf(0, 360), 0.7f, sink: 0.1f);
        // ตาลโตนดสร้างด้วยโค้ด (ProceduralMeshes.SugarPalm): ลำต้นลายเปลือกไม้จริง + ใบพัดจีบไหวตามลม
        var palmTrunk = SurfMat("SugarPalmTrunk", Hex("#b8a890"), tex: "bark_brown_02", tiles: 0.9f, cull: CullMode.Off);
        var palmLeaf = SurfMat("SugarPalmLeaf", Color.white, sway: 0.05f, swayStart: 6f, vertexColor: true, smooth: 0.35f, cull: CullMode.Off);
        int pi = 0;
        foreach (var (x, z, h) in new[] { (8.5f, -9.5f, 7.5f), (-13.5f, -10.5f, 8.2f), (16f, -3f, 7f), (-17f, -6f, 8.8f), (18.5f, 9f, 7.8f) })
        {
            var go = new GameObject("SugarPalm");
            go.transform.SetPositionAndRotation(new Vector3(x, PaddyWorld.Ground(x, z) - 0.1f, z), Quaternion.Euler(0, Rf(0, 360), 0));
            go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.SugarPalm(31 + pi, h), "SugarPalm" + pi++);
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { palmTrunk, palmLeaf };
            colliders.Add(new Vector4(x, z, 0.5f, 99));
        }
        // พุ่มไม้
        foreach (var (x, z, s) in new[] { (-5.5f, -8.8f, 0.45f), (6f, -8.5f, 0.5f), (12.5f, 1f, 0.45f), (-12.5f, -1f, 0.5f), (-18f, 14f, 0.7f), (19f, 16f, 0.7f) })
            Nature("shrub_02", x, z, s, Rf(0, 360), 0.6f);
        // โขดหิน
        Nature("rock_moss_set_01", -8.8f, 1.5f, 0.45f, Rf(0, 360), 0.9f, sink: 0.12f, height: 0.75f);
        Nature("boulder_01", 9.5f, 3.5f, 0.9f, Rf(0, 360), 0.7f, sink: 0.15f, height: 0.85f);
        Nature("namaqualand_boulder_02", 3.8f, -4.4f, 0.55f, Rf(0, 360), 0.5f, sink: 0.1f, height: 0.45f);
        Nature("boulder_01", -6f, -3.6f, 0.6f, Rf(0, 360), 0.5f, sink: 0.15f, height: 0.55f);
        Nature("namaqualand_boulder_02", -9.6f, 4f, 0.6f, Rf(0, 360), 0.6f, sink: 0.1f, height: 0.5f);
        Nature("rock_moss_set_01", 8.6f, 8f, 0.35f, Rf(0, 360), 0.8f, sink: 0.12f, height: 0.6f);
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
        var pad = SurfMat("LotusPad", Color.white, vertexColor: true, smooth: 0.4f);
        var lotus = SurfMat("Lotus", Color.white, vertexColor: true, smooth: 0.25f, cull: CullMode.Off);
        var padMeshes = new[] { SaveMesh(ProceduralMeshes.LotusPad(1), "LotusPad1"), SaveMesh(ProceduralMeshes.LotusPad(2), "LotusPad2"), SaveMesh(ProceduralMeshes.LotusPad(3), "LotusPad3") };
        var flowerMesh = SaveMesh(ProceduralMeshes.LotusFlower(4, false), "LotusFlower"); var budMesh = SaveMesh(ProceduralMeshes.LotusFlower(5, true), "LotusBud");
        GameObject MeshObj(string name, Mesh mesh, Material mat, Vector3 pos, float yaw, float scale, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0)); go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }
        foreach (var (cx, cz, r) in new[] { (5.5f, 10.5f, 2.6f), (-6f, 9f, 2f), (7f, 2f, 1.6f), (-8f, 4f, 1.4f), (0f, 14f, 1.8f) })
            for (int i = 0; i < 12; i++)
            {
                float x = cx + Rf(-1, 1) * r, z = cz + Rf(-1, 1) * r;
                if (PaddyWorld.Ground(x, z) >= PaddyWorld.Water - 0.02f) continue;
                MeshObj("LotusPad", padMeshes[rnd.Next(3)], pad, new Vector3(x, PaddyWorld.Water + 0.02f, z), Rf(0, 360), Rf(0.35f, 0.6f), false);
                double fr = rnd.NextDouble();
                if (fr < 0.25) MeshObj("Lotus", flowerMesh, lotus, new Vector3(x + 0.2f, PaddyWorld.Water + Rf(0.05f, 0.25f), z), Rf(0, 360), Rf(0.22f, 0.3f), true);
                else if (fr < 0.38) MeshObj("LotusBud", budMesh, lotus, new Vector3(x + 0.2f, PaddyWorld.Water + Rf(0.1f, 0.35f), z), Rf(0, 360), Rf(0.16f, 0.22f), true);
            }

        // ---------- ตัวละคร กล้อง แสง ระบบเกม (ใช้ร่วมทุกด่าน) ----------
        var g = BuildGameplay(world, model, new Vector3(1.5f, 0, -7.6f), new Look(), null);
        g.director.roofLeak = BuildRoofLeak(hut);
        SaveStage(scene, ScenePath);
    }

    // ---------- ตัวช่วย ----------
    // หลังคารั่ว: หยดน้ำไหลจากรูหลังคาลงถังสังกะสี + แอ่งน้ำบนพื้น (StoryDirector ซ่อนเมื่อจ่ายค่าซ่อมแล้ว)
    static GameObject BuildRoofLeak(GameObject hut)
    {
        var leak = new GameObject("RoofLeak");
        var b = new Bounds(hut.transform.position, Vector3.zero);
        foreach (var r in hut.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        var floor = new Vector3(b.center.x + 0.35f, PaddyWorld.Ground(b.center.x, b.center.z) + 0.02f, b.center.z - 0.2f);
        leak.transform.position = floor;
        // ถังรองน้ำ
        var bucket = Prim(PrimitiveType.Cylinder, "Bucket", floor + new Vector3(0, 0.17f, 0), new Vector3(0.38f, 0.17f, 0.38f), SurfMat("Bucket", Hex("#8d949b"), smooth: 0.65f));
        bucket.transform.SetParent(leak.transform, true);
        var inner = Prim(PrimitiveType.Cylinder, "BucketWater", floor + new Vector3(0, 0.3f, 0), new Vector3(0.33f, 0.005f, 0.33f), SurfMat("BucketWater", Hex("#3d5a63"), smooth: 0.95f));
        inner.transform.SetParent(leak.transform, true);
        // แอ่งน้ำเปียกรอบถัง
        var puddle = Prim(PrimitiveType.Cylinder, "Puddle", floor + new Vector3(0.25f, 0.0f, 0.1f), new Vector3(1.3f, 0.004f, 0.9f), SurfMat("Puddle", Hex("#2e3a36"), smooth: 0.92f));
        puddle.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        puddle.transform.SetParent(leak.transform, true);
        // หยดน้ำ (particle) จากใต้หลังคาลงถัง · อีกจุดหยดลงพื้น
        foreach (var (off, rate) in new[] { (new Vector3(0, 0, 0), 7f), (new Vector3(0.6f, 0, 0.3f), 3.5f) })
        {
            var go = new GameObject("Drips");
            go.transform.SetParent(leak.transform, false);
            go.transform.position = new Vector3(floor.x + off.x, b.max.y - 0.55f, floor.z + off.z);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main; main.startLifetime = 0.55f; main.startSpeed = 0; main.startSize = 0.035f; main.gravityModifier = 1.6f;
            main.startColor = new Color(0.78f, 0.88f, 0.95f, 0.85f); main.maxParticles = 40; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.03f;
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch; pr.lengthScale = 4; pr.velocityScale = 0.04f;
            pr.sharedMaterial = UnlitMat("Drip", new Color(0.8f, 0.9f, 1f));
        }
        return leak;
    }

    // PanelSettings ของ HUD เป็น asset ใน Resources: มีธีม (ไม่งั้น layout เพี้ยน) และให้ editor แนบข้อมูล ICU ตัดคำไทยเข้า build
    static void BuildHudPanel()
    {
        const string path = "Assets/Game/Resources/UI/HudPanel.asset";
        var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
        if (ps == null) { ps = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(ps, path); }
        ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/Game/Resources/UI/CozyTheme.tss");
        ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        ps.referenceResolution = new Vector2Int(1080, 1080);
        ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        ps.match = 0.5f;
        EditorUtility.SetDirty(ps);
        AssetDatabase.SaveAssets();
    }

    // ใบไม้ของโมเดล Poly Haven → shader CozyFishing/Leaves (ไหวตามลม แสงทะลุใบ) — กิ่ง/ลำต้นใช้วัสดุ PBR เดิมของ glTFast
    // ค่าไหว (เมตรในโลก) ตามขนาดต้น: ต้นไม้ใหญ่ไหวมาก พืชเล็กไหวน้อย
    static readonly Dictionary<string, (float sway, float height, float flutter)> LeafWind = new Dictionary<string, (float, float, float)>
    {
        { "jacaranda_tree", (0.22f, 6f, 0.025f) }, { "island_tree_01", (0.2f, 7f, 0.02f) }, { "shrub_02", (0.05f, 1.2f, 0.015f) }, { "shrub_03", (0.03f, 0.5f, 0.01f) }, { "shrub_04", (0.03f, 0.4f, 0.01f) },
        { "fern_02", (0.04f, 0.6f, 0.012f) }, { "weed_plant_02", (0.04f, 0.5f, 0.01f) }, { "flower_empodium", (0.03f, 0.3f, 0.008f) },
    };
    static Material LeafMat(string file, Material src)
    {
        if (src == null || !LeafWind.TryGetValue(file, out var wind)) return null;
        if ((file == "jacaranda_tree" || file == "island_tree_01") && !src.name.ToLower().Contains("leaves")) return null;
        Texture tex = null;
        foreach (var n in src.GetTexturePropertyNames())
            if (n.ToLower().Contains("basecolor") || n == "_BaseMap" || n == "_MainTex") { tex = src.GetTexture(n); if (tex != null) break; }
        if (tex == null) { Debug.LogWarning($"[SceneBuilder] no leaf texture on {file}/{src.name}"); return null; }
        var m = LoadOrCreate($"{MatDir}/Leaves_{file}_{src.name.Replace(" ", "_")}.mat", Shader.Find("CozyFishing/Leaves"));
        m.SetTexture("_BaseMap", tex);
        float cutoff = src.HasProperty("alphaCutoff") ? src.GetFloat("alphaCutoff") : 0.5f;
        m.SetFloat("_Cutoff", Mathf.Clamp(cutoff, 0.3f, 0.6f));
        m.SetFloat("_Sway", wind.sway); m.SetFloat("_SwayHeight", wind.height); m.SetFloat("_Flutter", wind.flutter);
        m.SetColor("_BaseColor", file.EndsWith("_tree") || file.StartsWith("island_tree") ? Color.white : new Color(1.3f, 1.3f, 1.15f));   // พุ่มไม้เล็กลายใบมืด ยกให้สว่างขึ้น
        m.enableInstancing = true;
        return m;
    }

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
