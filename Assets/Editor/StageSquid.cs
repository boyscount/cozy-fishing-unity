// ด่าน: เรือไดหมึกคลื่นมรณะ — เรือประมงไม้กลางทะเลมืดตอนกลางคืน ราวไฟเขียวล่อหมึกเรียงเหนือดาดฟ้าและคานยื่นสองข้าง ทะเลใต้แสงไฟเรืองเขียว
//      ผู้เล่นยืนหัวเรือ (เรือโคลงตามคลื่น: SquidSea) · เรือไดหมึกลำอื่นไกล ๆ เป็นแนวไฟเขียวกลางทะเล · เสากระโดงเรือแตกโผล่น้ำ (ถิ่นหมึกยักษ์)
//      ฝั่งไกลเป็นเงาภูเขามืด · กลางคืนเท่านั้น (StoryDirector ตั้งเวลา 19:00 ตอนเข้าด่าน)
using System.IO;
using CozyFishing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class SceneBuilder
{
    const float SW = -0.3f;   // ระดับน้ำทะเล

    static float SquidGround(float x, float z)
    {
        float n = Mathf.PerlinNoise(x * 0.02f + 7, z * 0.02f + 3) - 0.5f, n2 = Mathf.PerlinNoise(x * 0.07f + 1, z * 0.07f + 9) - 0.5f;
        float d = Mathf.Sqrt(x * x * 0.5f + (z - 40) * (z - 40));
        float bed = SW - 14f + n * 4f;
        float coast = SStep(150f, 185f, d + n * 30);                                   // ฝั่งไกลเป็นภูเขาเงาดำ
        return Mathf.Lerp(bed, SW + 4f + Mathf.Max(0, d - 170) * 0.9f + n2 * 18f, coast);
    }

    public static void CreateSquid()
    {
        const string path = "Assets/Game/Scenes/Squid.unity";
        Directory.CreateDirectory(MatDir); Directory.CreateDirectory(GenDir);
        AssetDatabase.Refresh();
        ConfigureTextures();
        surfShader = Shader.Find("CozyFishing/Surface");
        var waterShader = Shader.Find("CozyFishing/Water");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        ConfigureUrp();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var rnd = new System.Random(6161);
        float Rf(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        // ---------- โลก ----------
        var world = new GameObject("World").AddComponent<StageWorld>();
        world.stageId = "squid"; world.waterY = SW;
        world.origin = new Vector2(-50, -30); world.cell = 0.5f; world.nx = 201; world.nz = 201;
        world.heights = new float[world.nx * world.nz];
        for (int j = 0; j < world.nz; j++) for (int i = 0; i < world.nx; i++) world.heights[j * world.nx + i] = SquidGround(world.origin.x + i * world.cell, world.origin.y + j * world.cell);
        float deckY = SW + 0.6f;
        world.walkBounds = new Rect(-1.6f, -5.4f, 3.2f, 9.2f);
        world.spawn = new Vector3(0, deckY, 2.6f); world.spawnYaw = 0;
        world.special = new Vector3(6f, SW, 15f); world.specialRadius = 3.5f;          // รอบเสากระโดงเรือแตก
        world.camBounds = new Rect(-8, -8, 16, 20); world.camBias = new Vector2(0, 10f); world.camBiasK = 0.3f;
        world.docks.Add(new Rect(-1.35f, -5.2f, 2.7f, 8.9f)); world.dockY.Add(deckY);

        // ---------- ทะเล + ฝั่งไกล ----------
        var terrain = new GameObject("Terrain");
        var m = Grid(420, 420, 140, new Vector3(0, 0, 40));
        var v = m.vertices; var cols = new Color[v.Length];
        for (int i = 0; i < v.Length; i++) { float h = SquidGround(v[i].x, v[i].z); v[i].y = h; cols[i] = new Color(0, 0.8f, SStep(SW, SW - 2, h), 0); }
        m.vertices = v; m.colors = cols; m.RecalculateNormals(); m.RecalculateBounds();
        terrain.AddComponent<MeshFilter>().sharedMesh = SaveMesh(m, "SquidTerrain");
        var tr = terrain.AddComponent<MeshRenderer>();
        tr.sharedMaterial = TerrainMat("Squid", ("forest_ground_05", "rocky_gravel", "rock_face_04"), new Vector4(0.1f, 0.2f, 0.06f, 0.02f), "#3a4a40", "#2a3434");
        tr.shadowCastingMode = ShadowCastingMode.Off;
        var water = new GameObject("SeaWater");
        water.AddComponent<MeshFilter>().sharedMesh = SaveMesh(Grid(300, 300, 160, new Vector3(0, SW, 40)), "SquidWater");
        var wm = LoadOrCreate($"{MatDir}/Water_Squid.mat", waterShader);
        wm.SetColor("_Shallow", Hex("#1f5a4c")); wm.SetColor("_Deep", Hex("#03080c"));
        wm.SetFloat("_DepthScale", 2.5f); wm.SetFloat("_Clarity", 0.35f); wm.SetFloat("_Glint", 1.4f); wm.SetFloat("_Refraction", 0.05f);
        wm.SetFloat("_Reflect", 1f); wm.SetFloat("_Waves", 0.085f); wm.SetFloat("_FoamWidth", 0.12f); wm.SetVector("_Flow", new Vector4(0.12f, 0.08f, 0, 0));   // คลื่นทะเลแรง
        wm.SetVector("_RapidZone", Vector4.zero);
        var wr = water.AddComponent<MeshRenderer>(); wr.sharedMaterial = wm; wr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- วัสดุ ----------
        var plank = SurfMat("HBPlank", Hex("#c0a888"), 0.04f, vertexColor: true, smooth: 0.12f, tex: "weathered_brown_planks", tiles: 0.7f);
        var plankDark = SurfMat("HBPlankDark", Hex("#9a8268"), 0.05f, vertexColor: true, smooth: 0.1f, tex: "weathered_brown_planks", tiles: 0.7f);
        var hullMat = SurfMat("SquidHull", Hex("#3c6a8a"), 0.08f, vertexColor: true, smooth: 0.3f, tex: "weathered_brown_planks", tiles: 0.8f, cull: CullMode.Off);   // เรือไม้ทาสีฟ้าซีด
        var trimMat = SurfMat("SquidTrim", Hex("#b8342a"), 0.05f, smooth: 0.35f);                                                  // ขอบเรือสีแดง
        var cabinMat = SurfMat("SquidCabin", Hex("#e2dccc"), 0.12f, smooth: 0.2f, tex: "worn_mossy_plasterwall", tiles: 0.6f);
        var tin = SurfMat("HBTin", Color.white, 0f, vertexColor: true, smooth: 0.35f, cull: CullMode.Off);
        var metal = SurfMat("SquidPole", Hex("#4a4c4e"), 0f, smooth: 0.5f);
        var rope = SurfMat("HeadRope", Hex("#b89a6a"), 0f, smooth: 0.05f, tex: "reed_roof_04", tiles: 3f);
        var netMat = SurfMat("HBNetCloth", Hex("#8a8670"), 0f, smooth: 0.1f, cull: CullMode.Off);
        var bucketBlue = SurfMat("HBGallonBlue", Hex("#3a5a8a"), 0.15f, smooth: 0.5f);
        var bulbMat = UnlitMat("SquidBulb", new Color(0.3f, 1f, 0.4f) * 1.3f);   // ไม่สว่างเกิน (เกินแล้วกลายเป็นสีขาว)
        var windowMat = UnlitMat("SquidWindow", new Color(1f, 0.75f, 0.4f) * 1.6f);
        Transform group = null;
        GameObject Box(string name, Vector3 lp, Vector3 s, Material mat, Vector3? rot = null)
        {
            var g = Prim(PrimitiveType.Cube, name, Vector3.zero, s, mat);
            g.transform.SetParent(group, false); g.transform.localPosition = lp; g.transform.localRotation = Quaternion.Euler(rot ?? Vector3.zero);
            return g;
        }
        GameObject Pole(Vector3 a, Vector3 b, float r, Material mat)
        {
            var g = Prim(PrimitiveType.Cylinder, "Pole", (a + b) * 0.5f, new Vector3(r * 2, (b - a).magnitude * 0.5f, r * 2), mat);
            g.transform.up = (b - a).normalized; g.transform.SetParent(group, true); return g;
        }

        // ---------- เรือผู้เล่น (ทั้งลำอยู่ใต้ SquidBoat ให้โคลงด้วยกัน) ----------
        var boatRoot = new GameObject("SquidBoat").transform; boatRoot.position = new Vector3(0, 0, 0);
        group = new GameObject("BoatBody").transform; group.SetParent(boatRoot, false);
        var hull = new GameObject("Hull"); hull.transform.SetParent(group, false); hull.transform.localPosition = new Vector3(0, deckY + 0.55f, -0.9f);
        hull.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.Rowboat(11.5f, 3.4f, 1.7f), "SquidHullMesh"); hull.AddComponent<MeshRenderer>().sharedMaterial = hullMat;
        for (float z = -5.2f; z < 3.6f; z += 0.24f)                                     // ดาดฟ้าไม้กระดานขวาง
        {
            float t = Mathf.InverseLerp(-6.6f, 4.8f, z), hw = Mathf.Max(0.3f, 1.62f * Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.PI)), 0.55f) - 0.12f);
            Box("Deck", new Vector3(0, deckY - 0.03f, z), new Vector3(hw * 2, 0.06f, 0.21f), rnd.NextDouble() < 0.3 ? plankDark : plank);
        }
        for (int s = -1; s <= 1; s += 2) for (float z = -5f; z < 3.4f; z += 0.5f)   // ขอบกราบเรือสีแดง
        {
            float t = Mathf.InverseLerp(-6.65f, 4.85f, z), hw = 1.7f * Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.PI)), 0.55f);
            Box("Gunwale", new Vector3(s * hw, deckY + 0.58f, z), new Vector3(0.1f, 0.08f, 0.52f), trimMat);
        }
        // ห้องคนขับท้ายเรือ: ผนังปูนขาว หน้าต่างไฟส้ม หลังคาสังกะสี
        Box("Cabin", new Vector3(0, deckY + 1.0f, -4.4f), new Vector3(2.2f, 2.0f, 1.6f), cabinMat);
        Box("CabinWindow", new Vector3(0, deckY + 1.35f, -3.59f), new Vector3(1.4f, 0.5f, 0.02f), windowMat);
        var roofG = new GameObject("CabinRoof"); roofG.transform.SetParent(group, false); roofG.transform.localPosition = new Vector3(0, deckY + 2.0f, -4.4f);
        roofG.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.TinRoof(616, 2.0f, 1.35f, 0.3f), "SquidCabinRoof"); roofG.AddComponent<MeshRenderer>().sharedMaterial = tin;
        // เสากระโดง + ราวไฟเหนือดาดฟ้า + คานยื่นสองข้าง
        Pole(new Vector3(0, deckY, -2.6f), new Vector3(0, deckY + 6f, -2.6f), 0.07f, metal);
        var bulbs = new System.Collections.Generic.List<Vector3>();
        for (int s = -1; s <= 1; s += 2)
        {
            foreach (float z in new[] { -3.4f, 3.2f }) Pole(new Vector3(s * 1.2f, deckY + 0.5f, z), new Vector3(s * 1.2f, deckY + 3.1f, z), 0.04f, metal);
            Pole(new Vector3(s * 1.2f, deckY + 3.05f, -3.5f), new Vector3(s * 1.2f, deckY + 3.05f, 3.3f), 0.025f, metal);                   // ราวยาวหัว-ท้าย
            for (float z = -3.2f; z <= 3.1f; z += 0.9f) bulbs.Add(new Vector3(s * 1.2f, deckY + 2.85f, z));
            var a = new Vector3(s * 0.2f, deckY + 5.6f, -2.6f); var b = new Vector3(s * 5.2f, deckY + 2.4f, -0.4f);                        // คานยื่นออกนอกลำ
            Pole(a, b, 0.05f, metal);
            for (int k = 1; k <= 5; k++) bulbs.Add(Vector3.Lerp(a, b, k / 5.5f) + Vector3.down * 0.25f);
            Pole(new Vector3(s * 5.2f, deckY + 2.4f, -0.4f), new Vector3(s * 1.4f, deckY + 0.6f, 1.2f), 0.012f, rope);                     // เชือกยึดคาน
        }
        int bi = 0;
        var bulbGroup = new GameObject("BulbGroup").transform; bulbGroup.SetParent(boatRoot, false);   // หลอดไฟรวมเป็น mesh เดียว (กะพริบพร้อมกันด้วยสีเดียว)
        foreach (var p in bulbs)
        {
            var g = Prim(PrimitiveType.Sphere, "SquidBulb" + bi++, p, new Vector3(0.18f, 0.24f, 0.18f), bulbMat); g.transform.SetParent(bulbGroup, true);
            g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
        // ไฟจริงเหลือ 2 ดวง (เดิม 5) ระยะกว้างขึ้น · ผิวน้ำเต็มจอคำนวณแสงทุกพิกเซล ไฟยิ่งน้อยยิ่งลื่นบนมือถือ
        foreach (var lp in new[] { new Vector3(-2.6f, deckY + 2.8f, 0.4f), new Vector3(2.6f, deckY + 2.8f, -0.4f) })
        {
            var l = new GameObject("SquidLamp").AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(0.45f, 1f, 0.55f); l.range = 14f; l.intensity = 3.6f; l.shadows = LightShadows.None;
            l.transform.SetParent(boatRoot, true); l.transform.position = lp;
        }
        { var cl = new GameObject("LanternLight").AddComponent<Light>(); cl.type = LightType.Point; cl.color = Hex("#ffb060"); cl.range = 4f; cl.intensity = 1.2f; cl.shadows = LightShadows.None; cl.transform.SetParent(boatRoot, true); cl.transform.position = new Vector3(0, deckY + 1.4f, -3.2f); }
        // ของบนดาดฟ้า: ถังน้ำแข็งสีฟ้า ตะกร้าหมึก อวนกอง ขดเชือก ทุ่นส้ม
        Box("IceBox", new Vector3(-0.9f, deckY + 0.25f, -1.6f), new Vector3(0.8f, 0.5f, 0.6f), bucketBlue);
        Box("IceBox", new Vector3(0.95f, deckY + 0.22f, -2.2f), new Vector3(0.7f, 0.44f, 0.55f), bucketBlue, new Vector3(0, 12, 0));
        Box("NetPile", new Vector3(0.7f, deckY + 0.08f, -0.8f), new Vector3(0.9f, 0.16f, 0.7f), netMat, new Vector3(0, 30, 4));
        for (int k = 0; k < 4; k++) Box("RopeCoil", new Vector3(-0.8f, deckY + 0.02f + k * 0.04f, 0.9f), new Vector3(0.44f - k * 0.06f, 0.035f, 0.44f - k * 0.06f), rope, new Vector3(0, k * 30, 0));
        var basket = new GameObject("FishBasket"); basket.transform.SetParent(group, false); basket.transform.localPosition = new Vector3(0.9f, deckY - 0.02f, 0.4f); basket.transform.localScale = Vector3.one * 1.2f;
        basket.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.FishBasket(), "HBFishBasket"); basket.AddComponent<MeshRenderer>().sharedMaterial = SurfMat("HBBasketMat", Color.white, 0.1f, vertexColor: true, smooth: 0.15f);
        world.colliders.Add(new Vector4(0, -4.4f, 1.3f, 99));
        CombineGroup(group, "SquidBoatBody");   // ตัวเรือร้อยกว่าชิ้นรวมเหลือชิ้นละวัสดุ (เรือโคลงทุกเฟรม)
        CombineGroup(bulbGroup, "SquidBulbs");
        foreach (var r in bulbGroup.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- เรือไดหมึกลำอื่นไกล ๆ: แนวไฟเขียวกลางทะเล ----------
        var far = new GameObject("FarBoats").transform;
        foreach (var (x, z, yaw) in new[] { (-38f, 55f, 20f), (24f, 70f, -30f), (-12f, 95f, 75f), (55f, 40f, 50f), (-65f, 30f, -20f), (40f, 110f, 10f) })
        {
            group = new GameObject("FarBoat").transform; group.SetParent(far, false); group.SetPositionAndRotation(new Vector3(x, SW, z), Quaternion.Euler(0, yaw, 0));
            Box("Hull", new Vector3(0, 0.3f, 0), new Vector3(2.6f, 0.9f, 9f), SurfMat("SquidFarHull", Hex("#1a2228"), 0f, smooth: 0.2f));
            Box("Cabin", new Vector3(0, 1.4f, -3f), new Vector3(1.8f, 1.4f, 1.6f), SurfMat("SquidFarHull", Hex("#1a2228"), 0f, smooth: 0.2f));
            for (float zz = -3.5f; zz <= 3.5f; zz += 0.7f) for (int s = -1; s <= 1; s += 2)
            { var g = Prim(PrimitiveType.Sphere, "FarBulb", Vector3.zero, Vector3.one * 0.35f, bulbMat); g.transform.SetParent(group, false); g.transform.localPosition = new Vector3(s * 1.1f, 3.2f, zz); g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off; }
        }
        CombineGroup(far, "SquidFar");

        // ---------- เสากระโดงเรือแตกโผล่น้ำ (ถิ่นหมึกยักษ์) ----------
        group = new GameObject("Wreck").transform;
        var deadWood = SurfMat("ResDeadWood", Hex("#a49c90"), 0.1f, vertexColor: true, smooth: 0.18f, tex: "bark_brown_02", tiles: 1.1f);
        Pole(new Vector3(6f, SW - 4f, 15f), new Vector3(6.6f, SW + 3.2f, 15.4f), 0.12f, deadWood);
        Pole(new Vector3(5.2f, SW + 2.2f, 15.2f), new Vector3(7.6f, SW + 1.8f, 15.6f), 0.06f, deadWood);
        Pole(new Vector3(6.5f, SW + 3f, 15.4f), new Vector3(8.5f, SW - 0.2f, 14f), 0.012f, rope);
        Box("Bow", new Vector3(4.8f, SW + 0.15f, 16.5f), new Vector3(1.4f, 0.8f, 2.4f), SurfMat("SquidWreck", Hex("#2a3a3a"), 0.4f, smooth: 0.2f, tex: "weathered_brown_planks", tiles: 0.8f), new Vector3(-24, 30, 18));
        world.colliders.Add(new Vector4(6f, 15f, 1f, 99));

        // ---------- ประกายเขียวบนผิวน้ำใต้ไฟ + ละอองคลื่น ----------
        var glow = ParticleMat("SquidGlow", new Color(0.4f, 1.2f, 0.5f, 0.5f));
        foreach (var x in new[] { -3.5f, 3.5f })
            Particles("GreenSheen", new Vector3(x, SW + 0.05f, 0), new Vector3(4f, 9f, 0.1f), glow, 6, new Vector2(2f, 4f), new Vector2(3f, 5f), new Vector2(0.02f, 0.06f), new Color(0.4f, 1f, 0.55f, 0.08f));
        var spray = ParticleMat("Spray", new Color(1, 1, 1, 0.5f));
        Particles("BowSpray", new Vector3(0, SW + 0.2f, 4.6f), new Vector3(2.4f, 0.6f, 0.2f), spray, 10, new Vector2(0.3f, 0.8f), new Vector2(0.8f, 1.4f), new Vector2(0.3f, 0.8f), new Color(0.85f, 1f, 0.9f, 0.25f), 0.3f);

        // ---------- ตัวละคร กล้อง แสง ระบบเกม ----------
        var look = new Look { sun = "#8aa0c8", sunIntensity = 0.5f, sunRot = new Vector2(30, 200), fog = "#0c1a20", fogStart = 25, fogEnd = 160, exposure = 0.6f, ambient = 0.55f, skyId = "Overcast", skyTint = "#0a1220" };
        BuildGameplay(world, model, world.spawn, look, world);
        SaveStage(scene, path);
    }

    // ภาพปกด่านเรือไดหมึก: มุมกว้างจากเหนือท้ายเรือมองไปหัวเรือ เห็นราวไฟเขียวและทะเลมืด
    public static void SquidCover()
    {
        EditorSceneManager.OpenScene("Assets/Game/Scenes/Squid.unity");
        RenderSettings.ambientIntensity = 0.5f;
        var go = new GameObject("CoverCam"); var cam = go.AddComponent<Camera>();
        go.transform.position = new Vector3(-4.5f, 4.2f, -8.5f); go.transform.LookAt(new Vector3(1f, 0.6f, 6f));
        cam.fieldOfView = 55; cam.nearClipPlane = 0.2f; cam.farClipPlane = 300; cam.clearFlags = CameraClearFlags.Skybox;
        go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (c != cam) c.enabled = false;
        var hud = Object.FindFirstObjectByType<Hud>(); if (hud != null) hud.gameObject.SetActive(false);
        var sun = RenderSettings.sun; if (sun != null) sun.intensity = 0.35f;
        const int W = 1024, H = 576;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt; cam.Render(); cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
        const string outPath = "Assets/Game/Resources/UI/Stages/squid.jpg";
        File.WriteAllBytes(outPath, tex.EncodeToJPG(88)); AssetDatabase.ImportAsset(outPath);
        if (AssetImporter.GetAtPath(outPath) is TextureImporter ti)
        {
            ti.mipmapEnabled = false; var ps = ti.GetPlatformTextureSettings("WebGL"); ps.overridden = true; ps.maxTextureSize = 512; ps.format = TextureImporterFormat.ASTC_6x6;
            ti.SetPlatformTextureSettings(ps); ti.SaveAndReimport();
        }
        Debug.Log("[SceneBuilder] cover saved " + outPath);
    }
}
