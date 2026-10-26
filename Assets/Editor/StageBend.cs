// ด่าน: คุ้งน้ำมืดป่าสนเถา — ลำน้ำแคบคดเคี้ยวลอดป่าทึบ ต้นไทรยักษ์รากบิดเกลียวแผ่ลงน้ำ รากอากาศห้อยระย้า
//      เถาวัลย์ขึงโยงข้ามลำน้ำ ขอนไม้ผุ หมอกเขียวหม่น หิ่งห้อยกะพริบ · ผู้เล่นยืนบนเรือไม้ลำเล็กผูกไว้ริมตลิ่ง มีตะเกียงดวงเดียว
//      คืนเดือนมืดฟ้าน้ำเงินดำ · ทิวสนเขาสูงตระหง่านเป็นเงาตัดฟ้าเหมือนนิ้วมือ · หมอกหนาลอยผ่านทิวสน · ตะเกียงรั้วริมฝั่งสาดแสงส้มบนผิวน้ำ
using System.IO;
using CozyFishing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class SceneBuilder
{
    const float BW = -0.3f;
    static float BZc(float x) => 6f + 4f * Mathf.Sin(x * 0.12f + 0.4f);           // แนวกลางลำน้ำคดเคี้ยว
    static float BHw(float x) => 5f + 0.8f * Mathf.Sin(x * 0.21f + 1.3f);

    static float BendGround(float x, float z)
    {
        float n = Mathf.PerlinNoise(x * 0.14f + 51, z * 0.14f + 3) - 0.5f, n2 = Mathf.PerlinNoise(x * 0.5f + 17, z * 0.5f + 41) - 0.5f;
        float d = Mathf.Abs(z - BZc(x)) / BHw(x);
        float bed = BW - 0.6f - 1.8f * Mathf.Clamp01(1 - d * d) + n2 * 0.25f;
        float up = Mathf.Max(0, Mathf.Abs(z - BZc(x)) - BHw(x));
        float bank = 0.2f + n * 0.6f + n2 * 0.2f + up * 0.12f + Mathf.Max(0, up - 6) * 0.15f;
        return Mathf.Lerp(bed, bank, SStep(0.85f, 1.15f, d));
    }

    public static void CreateBend()
    {
        const string path = "Assets/Game/Scenes/Bend.unity";
        Directory.CreateDirectory(MatDir); Directory.CreateDirectory(GenDir);
        AssetDatabase.Refresh();
        ConfigureTextures();
        surfShader = Shader.Find("CozyFishing/Surface");
        var waterShader = Shader.Find("CozyFishing/Water");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        ConfigureUrp();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var rnd = new System.Random(8484);
        float Rf(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        // ---------- โลก ----------
        var world = new GameObject("World").AddComponent<StageWorld>();
        world.stageId = "bend"; world.waterY = BW;
        world.origin = new Vector2(-40, -20); world.cell = 0.5f; world.nx = 161; world.nz = 121;
        world.heights = new float[world.nx * world.nz];
        for (int j = 0; j < world.nz; j++) for (int i = 0; i < world.nx; i++) world.heights[j * world.nx + i] = BendGround(world.origin.x + i * world.cell, world.origin.y + j * world.cell);
        world.walkBounds = new Rect(-20, -14, 40, 40);
        float boatZ = BZc(0) - BHw(0) + 2.1f, deckY = BW + 0.18f;   // เรือยาวขึ้น → ขยับออกจากตลิ่ง ท้ายเรือไม่จมดิน
        world.spawn = new Vector3(0, deckY, boatZ); world.spawnYaw = 0;
        world.docks.Add(new Rect(-0.6f, boatZ - 2.4f, 1.2f, 4.8f)); world.dockY.Add(deckY);
        world.camBounds = new Rect(-14, -9, 28, 24); world.camBias = new Vector2(0, BZc(0)); world.camBiasK = 0.35f;

        // ---------- พื้นดิน + น้ำ ----------
        var terrain = new GameObject("Terrain");
        var m = Grid(80, 60, 170, new Vector3(0, 0, 8));
        var v = m.vertices; var cols = new Color[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            float x = v[i].x, z = v[i].z, h = BendGround(x, z);
            v[i].y = h;
            cols[i] = new Color(SStep(BW + 0.5f, BW + 0.02f, h), 0.5f + 0.4f * Mathf.PerlinNoise(x * 0.3f, z * 0.3f), SStep(BW + 0.05f, BW - 1.2f, h), 0);
        }
        m.vertices = v; m.colors = cols; m.RecalculateNormals(); m.RecalculateBounds();
        terrain.AddComponent<MeshFilter>().sharedMesh = SaveMesh(m, "BendTerrain");
        var tr = terrain.AddComponent<MeshRenderer>();
        tr.sharedMaterial = TerrainMat("Bend", ("forest_ground_05", "brown_mud_02", "forest_ground_05"), new Vector4(0.35f, 0.4f, 0.3f, 0.03f), "#6a7a58", "#4a5a44");
        tr.shadowCastingMode = ShadowCastingMode.Off;
        var water = new GameObject("RiverWater");
        water.AddComponent<MeshFilter>().sharedMesh = SaveMesh(Grid(80, 60, 140, new Vector3(0, BW, 8)), "BendWater");
        var wm = LoadOrCreate($"{MatDir}/Water_Bend.mat", waterShader);
        wm.SetColor("_Shallow", Hex("#7a8a70")); wm.SetColor("_Deep", Hex("#16221a"));
        wm.SetFloat("_DepthScale", 1.2f); wm.SetFloat("_Clarity", 0.35f); wm.SetFloat("_Glint", 0.02f); wm.SetFloat("_Refraction", 0.025f);
        wm.SetFloat("_Reflect", 1f); wm.SetFloat("_Waves", 0.01f); wm.SetFloat("_FoamWidth", 0.03f); wm.SetVector("_Flow", new Vector4(0.06f, 0.01f, 0, 0));
        wm.SetVector("_RapidZone", Vector4.zero);
        var wr = water.AddComponent<MeshRenderer>(); wr.sharedMaterial = wm; wr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- วัสดุ ----------
        var barkMat = SurfMat("BendBark", Hex("#a09480"), 0.35f, smooth: 0.12f, vertexColor: true, tex: "bark_brown_02", tiles: 1.2f, cull: CullMode.Off);
        var aerialMat = SurfMat("BendAerial", Hex("#8a8070"), 0.1f, sway: 0.05f, swayStart: 2f, vertexColor: true, smooth: 0.1f, cull: CullMode.Off);
        var vineMat = SurfMat("BendVine", Color.white, 0.2f, sway: 0.04f, swayStart: 0f, vertexColor: true, smooth: 0.15f, cull: CullMode.Off);
        var boatMat = SurfMat("HBBoat", Hex("#8a6e52"), 0.05f, smooth: 0.25f, tex: "weathered_brown_planks", tiles: 0.9f, cull: CullMode.Off);
        var plank = SurfMat("HBPlank", Hex("#c0a888"), 0.04f, smooth: 0.12f, tex: "weathered_brown_planks", tiles: 0.7f);
        var root = new GameObject("Forest").transform;
        GameObject Obj(string name, Mesh mesh, Material[] mats, Vector3 p, float yaw, float scale = 1)
        {
            var g = new GameObject(name); g.transform.SetParent(root, true);
            g.transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0)); g.transform.localScale = Vector3.one * scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterials = mats; return g;
        }

        // ---------- ไทรยักษ์ริมตลิ่ง: รากแผ่ลงน้ำ ----------
        int bi = 0;
        foreach (var (x, side, off, h) in new[] { (-9f, 1f, 0.6f, 9f), (7f, 1f, 1.2f, 10f), (-15f, -1f, 1.5f, 8.5f), (13f, -1f, 0.8f, 9.5f), (1f, 1f, 4.5f, 11f) })
        {
            float z = BZc(x) + side * (BHw(x) + off);
            Obj("Banyan", SaveMesh(ProceduralMeshes.Banyan(700 + bi, h), "Banyan" + bi), new[] { barkMat, aerialMat }, new Vector3(x, BendGround(x, z) - 0.2f, z), Rf(0, 360), Rf(0.9f, 1.15f));
            world.colliders.Add(new Vector4(x, z, 1.4f, 99));
            bi++;
        }
        world.special = new Vector3(7f, BW, BZc(7f) + BHw(7f) - 1.5f); world.specialRadius = 3f;   // ใต้รากไทรใหญ่ฝั่งไกล

        // ---------- เถาวัลย์ขึงข้ามลำน้ำ ----------
        int vi = 0;
        foreach (var (x, zoff, h, w) in new[] { (-6f, 0f, 7.5f, 9f), (4f, -1f, 8f, 11f), (-12f, 1.5f, 7f, 8f), (10f, 0.5f, 7.5f, 9f), (-1f, 2.5f, 8.5f, 8f) })
            Obj("Vines", SaveMesh(ProceduralMeshes.Vines(800 + vi, w, h), "BendVines" + vi++), new[] { vineMat }, new Vector3(x, BW, BZc(x) + zoff), 90 + Rf(-15, 15));

        // ---------- ทิวสนเขาสูงเป็นเงาตัดฟ้า (ไกลออกไปทั้งสองฝั่ง) ----------
        {
            var pineBark = SurfMat("PineBark", Color.white, 0.05f, vertexColor: true, smooth: 0.15f, tex: "bark_brown_02", tiles: 1.2f);
            var pineLeaf = SurfMat("PineNeedles", Color.white, 0f, sway: 0.03f, swayStart: 8f, vertexColor: true, smooth: 0.25f, cull: CullMode.Off);
            var pines = new Mesh[5]; for (int i = 0; i < pines.Length; i++) pines[i] = SaveMesh(ProceduralMeshes.MountainPine(1200 + i, Rf(14f, 20f)), "BendPine" + i);
            for (int t = 0, n = 0; t < 400 && n < 34; t++)
            {
                float x = Rf(-38, 38), side = rnd.NextDouble() < 0.7 ? 1 : -1, z = BZc(x) + side * (BHw(x) + Rf(7f, 22f));
                if (side < 0 && Mathf.Abs(x) < 10) continue;   // ไม่บังกล้องหลังผู้เล่น
                Obj("MountainPine", pines[rnd.Next(pines.Length)], new[] { pineBark, pineLeaf }, new Vector3(x, BendGround(x, z) - 0.3f, z), Rf(0, 360), Rf(0.85f, 1.2f)); n++;
            }
        }

        // ---------- เรือพายของผู้เล่น (ขนาดจริง ~5.4 ม.) + ตะเกียง ----------
        var boat = Obj("PlayerBoat", SaveMesh(ProceduralMeshes.Rowboat(5.4f, 1.45f, 0.45f), "PlayerRowboat"), new[] { boatMat }, new Vector3(0, BW + 0.16f, boatZ), 0);
        for (int k = -1; k <= 1; k++)
        {
            var th = Prim(PrimitiveType.Cube, "Thwart", Vector3.zero, new Vector3(1.25f, 0.05f, 0.24f), plank);
            th.transform.SetParent(boat.transform, false); th.transform.localPosition = new Vector3(0, 0.05f, k * 1.55f);
        }
        var stake = Prim(PrimitiveType.Cylinder, "MooringStake", new Vector3(-1.1f, BW + 0.3f, boatZ - 2.6f), new Vector3(0.1f, 1f, 0.1f), plank); stake.transform.SetParent(root, true);
        var lanternPos = new Vector3(0.3f, deckY + 0.02f, boatZ + 2.0f);
        var lsrc = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/brass_diya_lantern.glb");
        if (lsrc != null) { var l = Object.Instantiate(lsrc); l.transform.position = lanternPos; l.transform.localScale = Vector3.one * 1.7f; }
        var lg = new GameObject("LanternLight").AddComponent<Light>();
        lg.type = LightType.Point; lg.range = 7f; lg.intensity = 2.6f; lg.color = Hex("#ffa850"); lg.shadows = LightShadows.None;
        lg.transform.position = lanternPos + Vector3.up * 0.45f;
        // ตะเกียงรั้วตั้งริมฝั่งหน้าเรือ: แสงส้มสาดลงผิวน้ำ
        var shorePos = new Vector3(-1.4f, BendGround(-1.4f, boatZ - 2.9f) + 0.02f, boatZ - 2.9f);
        if (lsrc != null) { var l2 = Object.Instantiate(lsrc); l2.transform.position = shorePos; l2.transform.localScale = Vector3.one * 1.6f; }
        var lg2 = new GameObject("ShoreLanternLight").AddComponent<Light>();
        lg2.type = LightType.Point; lg2.range = 8f; lg2.intensity = 3f; lg2.color = Hex("#ff9a40"); lg2.shadows = LightShadows.None; lg2.transform.position = shorePos + Vector3.up * 0.5f;

        // ---------- ป่าทึบสองฝั่ง ----------
        var natureRoot = new GameObject("Nature").transform;
        GameObject Nature(string file, float x, float z, float scale, float yaw, float collide = 0, bool shadows = true, float sink = 0.04f, float? y = null)
        {
            Vary(ref file, ref scale, x, z);
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/{file}.glb");
            if (src == null) return null;
            var go = Object.Instantiate(src, natureRoot); go.name = file;
            go.transform.SetPositionAndRotation(new Vector3(x, (y ?? BendGround(x, z)) - sink, z), Quaternion.Euler(0, yaw, 0)); go.transform.localScale = Vector3.one * scale;
            foreach (var rr in go.GetComponentsInChildren<Renderer>())
            {
                rr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                var mats = rr.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = LeafMat(file, mats[i]) ?? mats[i]; rr.sharedMaterials = mats;
            }
            if (collide > 0) world.colliders.Add(new Vector4(x, z, collide, 99));
            return go;
        }
        for (int i = 0, n = 0; i < 80 && n < 16; i++)
        {
            float x = Rf(-20, 20), side = rnd.NextDouble() < 0.5 ? -1 : 1, z = BZc(x) + side * (BHw(x) + Rf(2.5f, 9f));
            if (Mathf.Abs(x) < 3 && side < 0) continue;
            Nature("island_tree_01", x, z, Rf(1.5f, 2.1f), Rf(0, 360), 0.9f, true, 0.15f); n++;
        }
        foreach (var (x, side) in new[] { (-4f, 1f), (11f, -1f), (-18f, 1f), (16f, 1f), (-11f, -1f), (5f, -1f) })
        {
            float z = BZc(x) + side * (BHw(x) + 0.3f);
            Nature("root_cluster_01", x, z, Rf(1f, 1.4f), Rf(0, 360), 0, true, 0.35f);
        }
        Nature("dead_tree_trunk", -7f, BZc(-7f) - 2f, 1.6f, 25, 0, true, 0, BW - 0.15f);
        Nature("dead_tree_trunk", 9f, BZc(9f) + 1.5f, 1.4f, -40, 0, true, 0, BW - 0.1f);
        var tilted = Nature("dead_tree_trunk", 3.5f, BZc(3.5f) + 3.5f, 1.8f, 75, 0, true, 0, BW + 0.4f);
        if (tilted != null) tilted.transform.rotation = Quaternion.Euler(0, 75, -18);   // ขอนล้มพาดลงน้ำ
        for (int i = 0; i < 40; i++)
        {
            float x = Rf(-20, 20), side = rnd.NextDouble() < 0.5 ? -1 : 1, z = BZc(x) + side * (BHw(x) + Rf(0.2f, 6f));
            if (Mathf.Abs(x) < 2 && side < 0 && z > boatZ - 3) continue;
            Nature(new[] { "fern_02", "shrub_02", "fern_02", "weed_plant_02", "shrub_03" }[i % 5], x, z, Rf(0.7f, 1.3f), Rf(0, 360), i % 5 == 1 ? 0.6f : 0, i % 5 != 3, 0.03f);
        }
        var reedMat = SurfMat("Reeds", Color.white, sway: 0.08f, swayStart: 0.3f, vertexColor: true, smooth: 0.25f, cull: CullMode.Off);
        var reedMeshes = new Mesh[3]; for (int i = 0; i < 3; i++) reedMeshes[i] = SaveMesh(ProceduralMeshes.Reeds(900 + i, Rf(1.2f, 1.9f)), "BendReeds" + i);
        for (int t = 0, n = 0; t < 1500 && n < 70; t++)
        {
            float x = Rf(-20, 20), z = Rf(-10, 26), h = BendGround(x, z);
            if (h > BW + 0.4f || h < BW - 0.3f || Mathf.Abs(x) < 2 && Mathf.Abs(z - boatZ) < 3) continue;
            var g = new GameObject("Reeds"); g.transform.SetParent(natureRoot, true);
            g.transform.SetPositionAndRotation(new Vector3(x, Mathf.Max(h, BW - 0.3f) - 0.05f, z), Quaternion.Euler(0, Rf(0, 360), 0));
            g.AddComponent<MeshFilter>().sharedMesh = reedMeshes[rnd.Next(3)]; g.AddComponent<MeshRenderer>().sharedMaterial = reedMat; n++;
        }

        // หมอกเขียวต่ำ + หิ่งห้อยทั่วลำน้ำ
        var mist = ParticleMat("Spray", new Color(1, 1, 1, 0.5f));
        Particles("BendMist", new Vector3(0, BW + 0.5f, 8), new Vector3(40f, 28f, 1.2f), mist, 46, new Vector2(4f, 8f), new Vector2(8f, 12f), new Vector2(0.03f, 0.12f), new Color(0.66f, 0.74f, 0.84f, 0.15f));
        Particles("PineFog", new Vector3(0, BW + 4f, 14), new Vector3(60f, 30f, 6f), mist, 22, new Vector2(8f, 14f), new Vector2(10f, 16f), new Vector2(0.05f, 0.2f), new Color(0.5f, 0.58f, 0.68f, 0.1f));   // หมอกลอยผ่านทิวสน
        var fly = ParticleMat("Firefly", new Color(1.6f, 1.5f, 0.5f, 1));
        var ff = Particles("Fireflies", new Vector3(0, BW + 1.6f, 8), new Vector3(34f, 24f, 2.5f), fly, 28, new Vector2(0.04f, 0.08f), new Vector2(3f, 6f), new Vector2(0.05f, 0.25f), new Color(0.9f, 1f, 0.45f, 1));
        var noise = ff.noise; noise.enabled = true; noise.strength = 0.4f; noise.frequency = 0.35f;

        // ---------- ตัวละคร กล้อง แสง ระบบเกม ----------
        var look = new Look { sun = "#b4c4d0", sunIntensity = 1.25f, sunRot = new Vector2(50, 160), fog = "#323e4c", fogStart = 5, fogEnd = 42, exposure = 0.95f, ambient = 0.95f, skyId = "Overcast", skyTint = "#28323e" };   // ฟ้าน้ำเงินดำอมเทา เมฆหนา   // ฟ้าเทาอมน้ำเงินยามพลบ (เดิมเขียวทั้งผืน ดูแปลก)
        BuildGameplay(world, model, world.spawn, look, world);
        SaveStage(scene, path);
    }
}
