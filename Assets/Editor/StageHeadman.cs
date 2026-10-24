// ด่าน: ท่าน้ำบ้านผู้ใหญ่ — แม่น้ำกว้างยามพลบ ฝั่งตรงข้ามเป็นเรือนไทยใต้ถุนสูงหลังใหญ่ของผู้ใหญ่บ้าน (เรือนนอน + เรือนครัว เชื่อมด้วยชาน)
//      หน้าต่างแสงตะเกียงอุ่น ๆ ระเบียงแขวนตะเกียง บันไดลงท่าน้ำ ศาลพระภูมิริมตลิ่ง ตาลโตนด กอไผ่
//      ฝั่งผู้เล่น: ท่าน้ำไม้ยื่นลงแม่น้ำ ปลายท่ามีศาลาหลังคาทรงไทย โอ่งดิน ขดเชือก เรือพายผูกหลัก
//      บรรยากาศวังเวง: หน้าต่างบางบานมืด บางบานแสงตะเกียง/นีออนสลัวลอดมู่ลี่ไม้ไผ่ เงาคนยืนนิ่งในห้อง · ใต้ถุนกองไม้ กระสอบข้าว กรงไก่
//      เสาไฟไม้ริมตลิ่ง · ท่าน้ำไม้เก่าเสาจับตะไคร่ พวงมาลัยแห้งบูชาแม่ย่านาง โอ่งมังกรมันวาว แกลลอนน้ำมัน ข้องเก่า โซ่สนิมผูกเรือ
//      น้ำขุ่นมืด ใบไม้เน่า ใบบัว · ตลิ่งกกรก กอไผ่ ตาลเป็นเงาตัดฟ้า · ท่าน้ำพังของเพื่อนบ้านไกล ๆ
using System.IO;
using CozyFishing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class SceneBuilder
{
    const float HMW = -0.3f;
    static float HMZc(float x) => 4.4f + 0.6f * Mathf.Sin(x * 0.05f + 0.5f);                    // แนวกลางแม่น้ำ (ไหลตามแกน x)
    static float HMHw(float x) => 7.2f + 0.5f * Mathf.Sin(x * 0.11f + 1.7f);

    static float HeadmanGround(float x, float z)
    {
        float n = Mathf.PerlinNoise(x * 0.12f + 71, z * 0.12f + 13) - 0.5f, n2 = Mathf.PerlinNoise(x * 0.45f + 7, z * 0.45f + 59) - 0.5f;
        float d = Mathf.Abs(z - HMZc(x)) / HMHw(x);
        float bed = HMW - 0.8f - 2.2f * Mathf.Clamp01(1 - d * d) + n2 * 0.25f;
        float up = Mathf.Max(0, Mathf.Abs(z - HMZc(x)) - HMHw(x));
        float bank = HMW + 0.45f + n * 0.4f + n2 * 0.15f + Mathf.Min(up, 3f) * 0.12f;
        return Mathf.Lerp(bed, bank, SStep(0.9f, 1.1f, d));
    }

    public static void CreateHeadman()
    {
        const string path = "Assets/Game/Scenes/Headman.unity";
        Directory.CreateDirectory(MatDir); Directory.CreateDirectory(GenDir);
        AssetDatabase.Refresh();
        ConfigureTextures();
        surfShader = Shader.Find("CozyFishing/Surface");
        var waterShader = Shader.Find("CozyFishing/Water");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        ConfigureUrp();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var rnd = new System.Random(7373);
        float Rf(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        // ---------- โลก ----------
        const float px = 3f;                                         // แนวท่าน้ำของผู้เล่น
        float deckY = HMW + 0.6f;
        var world = new GameObject("World").AddComponent<StageWorld>();
        world.stageId = "headman"; world.waterY = HMW;
        world.origin = new Vector2(-40, -20); world.cell = 0.5f; world.nx = 161; world.nz = 101;
        world.heights = new float[world.nx * world.nz];
        for (int j = 0; j < world.nz; j++) for (int i = 0; i < world.nx; i++) world.heights[j * world.nx + i] = HeadmanGround(world.origin.x + i * world.cell, world.origin.y + j * world.cell);
        world.walkBounds = new Rect(-16, -14, 32, 14);
        world.spawn = new Vector3(px, deckY, -2.2f); world.spawnYaw = 0;
        world.docks.Add(new Rect(px - 0.9f, -9f, 1.8f, 6f)); world.dockY.Add(deckY);            // ทางเดินท่าน้ำ
        world.docks.Add(new Rect(px - 1.6f, -3.9f, 5.2f, 2.7f)); world.dockY.Add(deckY);         // ลานปลายท่า + ศาลา
        world.camBounds = new Rect(-10, -9, 20, 22); world.camBias = new Vector2(px - 3f, 13f); world.camBiasK = 0.45f;
        world.special = new Vector3(-3f, HMW, HMZc(-3f) + HMHw(-3f) - 2.5f); world.specialRadius = 3f;   // ใต้ท่าน้ำเรือนผู้ใหญ่ฝั่งตรงข้าม

        // ---------- ตลิ่ง + แม่น้ำ ----------
        var terrain = new GameObject("Terrain");
        var m = Grid(80, 50, 170, new Vector3(0, 0, 5));
        var v = m.vertices; var cols = new Color[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            float x = v[i].x, z = v[i].z, h = HeadmanGround(x, z);
            v[i].y = h;
            cols[i] = new Color(SStep(HMW + 0.6f, HMW + 0.05f, h), 0.5f + 0.4f * Mathf.PerlinNoise(x * 0.3f, z * 0.3f), SStep(HMW + 0.05f, HMW - 1.2f, h), 0);
        }
        m.vertices = v; m.colors = cols; m.RecalculateNormals(); m.RecalculateBounds();
        terrain.AddComponent<MeshFilter>().sharedMesh = SaveMesh(m, "HeadmanTerrain");
        var tr = terrain.AddComponent<MeshRenderer>();
        tr.sharedMaterial = TerrainMat("Headman", ("leafy_grass", "brown_mud_02", "grass_path_3"), new Vector4(0.35f, 0.4f, 0.3f, 0.03f), "#7a8a62", "#5a6a4c");
        tr.shadowCastingMode = ShadowCastingMode.Off;
        var water = new GameObject("RiverWater");
        water.AddComponent<MeshFilter>().sharedMesh = SaveMesh(Grid(80, 50, 140, new Vector3(0, HMW, 5)), "HeadmanWater");
        var wm = LoadOrCreate($"{MatDir}/Water_Headman.mat", waterShader);
        wm.SetColor("_Shallow", Hex("#66705e")); wm.SetColor("_Deep", Hex("#1e2a30"));
        wm.SetFloat("_DepthScale", 1.2f); wm.SetFloat("_Clarity", 0.12f); wm.SetFloat("_Glint", 0.05f); wm.SetFloat("_Refraction", 0.02f);
        wm.SetFloat("_Reflect", 1f); wm.SetFloat("_Waves", 0.014f); wm.SetFloat("_FoamWidth", 0.05f); wm.SetVector("_Flow", new Vector4(0.05f, 0f, 0, 0));
        wm.SetVector("_RapidZone", Vector4.zero);
        var wr = water.AddComponent<MeshRenderer>(); wr.sharedMaterial = wm; wr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- วัสดุ ----------
        var plank = SurfMat("HBPlank", Hex("#c0a888"), 0.04f, smooth: 0.12f, tex: "weathered_brown_planks", tiles: 0.7f);
        var plankDark = SurfMat("HBPlankDark", Hex("#9a8268"), 0.05f, smooth: 0.1f, tex: "weathered_brown_planks", tiles: 0.7f);
        var teak = SurfMat("HeadTeak", Hex("#8a5e3c"), 0f, smooth: 0.25f, tex: "weathered_brown_planks", tiles: 0.8f);
        var teakDark = SurfMat("HeadTeakDark", Hex("#5a3c26"), 0f, smooth: 0.2f, tex: "weathered_brown_planks", tiles: 0.8f);
        var tile = SurfMat("HeadRoofTile", Hex("#7a5a4a"), 0.12f, smooth: 0.25f, tex: "clay_roof_tiles", tiles: 0.45f, cull: CullMode.Off);
        var gold = SurfMat("TempleGold", Hex("#c8a050"), 0.2f, smooth: 0.55f, cull: CullMode.Off);
        var boatMat = SurfMat("HBBoat", Hex("#8a6e52"), 0.05f, smooth: 0.25f, tex: "weathered_brown_planks", tiles: 0.9f, cull: CullMode.Off);
        var clay = SurfMat("HBClay", Hex("#8a5a3a"), 0.08f, smooth: 0.35f);
        var rope = SurfMat("HeadRope", Hex("#b89a6a"), 0f, smooth: 0.05f, tex: "reed_roof_04", tiles: 3f);
        var glow = UnlitMat("HeadWindowGlow", new Color(2.2f, 1.25f, 0.5f));
        var white = SurfMat("TemplePlaster", Hex("#f4ecd8"), 0.15f, smooth: 0.08f, tex: "worn_mossy_plasterwall", tiles: 0.3f);
        var moss = SurfMat("HeadMoss", Hex("#2e4a26"), 0.6f, smooth: 0.35f);
        var dragonJar = SurfMat("HeadDragonJar", Hex("#4a2c1c"), 0.05f, smooth: 0.7f);
        var dimGlow = UnlitMat("HeadWindowDim", new Color(0.9f, 0.45f, 0.16f));
        var neonGlow = UnlitMat("HeadWindowNeon", new Color(0.75f, 1.05f, 0.95f));
        var darkPane = SurfMat("HeadWindowDark", Hex("#0c0e10"), 0f, smooth: 0.6f);
        var sack = SurfMat("HeadSack", Hex("#b8a888"), 0.05f, smooth: 0.05f, tex: "reed_roof_04", tiles: 2f);
        var bambooMat = SurfMat("HeadBambooSlat", Hex("#a08a58"), 0.05f, smooth: 0.2f);
        var rust = SurfMat("HeadRust", Hex("#6a3a1c"), 0f, smooth: 0.3f);
        var gallon = SurfMat("HeadGallon", Hex("#c8a830"), 0.1f, smooth: 0.45f);
        var garland = SurfMat("HeadGarland", Hex("#a87a30"), 0f, smooth: 0.2f);
        var ribbon = SurfMat("HeadRibbon", Hex("#b02a20"), 0f, smooth: 0.3f);
        var root = new GameObject("Waterfront").transform;
        GameObject Box(string name, Vector3 c, Vector3 s, Material mat, float yaw = 0)
        {
            var g = Prim(PrimitiveType.Cube, name, c, s, mat); g.transform.SetParent(root, true); g.transform.rotation = Quaternion.Euler(0, yaw, 0); return g;
        }
        GameObject Post(Vector3 bottom, float h, float r, Material mat)
        {
            var g = Prim(PrimitiveType.Cylinder, "Post", bottom + Vector3.up * h * 0.5f, new Vector3(r * 2, h * 0.5f, r * 2), mat); g.transform.SetParent(root, true);
            if (bottom.y < HMW && bottom.y + h > HMW)   // คราบตะไคร่เขียวเข้มรอบเสาช่วงปริ่มน้ำ
            {
                var mo = Prim(PrimitiveType.Cylinder, "Moss", new Vector3(bottom.x, HMW + 0.12f, bottom.z), new Vector3(r * 2.3f, 0.28f, r * 2.3f), moss); mo.transform.SetParent(root, true);
            }
            return g;
        }
        GameObject MeshAt(string name, Mesh mesh, Material[] mats, Vector3 pos, float yaw, float scale = 1)
        {
            var g = new GameObject(name); g.transform.SetParent(root, true);
            g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0)); g.transform.localScale = Vector3.one * scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterials = mats; return g;
        }
        Light Lamp(Vector3 p, float range, float intensity, string col = "#ffb060")
        {
            var l = new GameObject("Lamp").AddComponent<Light>(); l.transform.SetParent(root, true);
            l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = Hex(col); l.shadows = LightShadows.None; l.transform.position = p; return l;
        }
        var lanternSrc = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/brass_diya_lantern.glb");
        void Lantern(Vector3 p, float scale = 1.5f)
        {
            if (lanternSrc == null) return;
            var l = Object.Instantiate(lanternSrc, root); l.transform.position = p; l.transform.localScale = Vector3.one * scale;
        }

        // ---------- ท่าน้ำของผู้เล่น: ทางเดินไม้ + ลานปลายท่า + ศาลาหลังคาทรงไทย ----------
        for (float z = -9f; z < -1.2f; z += 0.32f)
            Box("Plank", new Vector3(px + Rf(-0.03f, 0.03f), deckY - 0.04f, z), new Vector3(1.8f, 0.06f, 0.28f), (int)(z * 3) % 4 == 0 ? plankDark : plank, Rf(-1.5f, 1.5f));
        for (float x = px - 1.6f; x < px + 3.6f; x += 0.32f)
            if (x > px + 0.9f || x < px - 0.9f) Box("Plank", new Vector3(x, deckY - 0.04f, -2.55f), new Vector3(0.28f, 0.06f, 2.7f), plank, Rf(-1, 1));
        foreach (var (x, z) in new[] { (px - 0.85f, -8.5f), (px + 0.85f, -8.5f), (px - 0.85f, -6.5f), (px + 0.85f, -6.5f), (px - 0.85f, -4.5f), (px + 0.85f, -4.5f), (px - 1.55f, -1.3f), (px + 3.55f, -1.3f), (px - 1.55f, -3.85f), (px + 3.55f, -3.85f), (px + 0.85f, -1.3f) })
            Post(new Vector3(x, HMW - 1.5f, z), deckY - HMW + 1.45f, 0.08f, plankDark);
        // ศาลา 4 เสาด้านขวาของลาน
        foreach (var (x, z) in new[] { (px + 1.3f, -1.45f), (px + 3.4f, -1.45f), (px + 1.3f, -3.7f), (px + 3.4f, -3.7f) })
            Post(new Vector3(x, deckY, z), 2.3f, 0.07f, teak);
        MeshAt("SalaRoof", SaveMesh(ProceduralMeshes.ThaiRoof(3f, 1.7f, 1.3f, true), "HeadSalaRoof"), new[] { tile, teakDark }, new Vector3(px + 2.35f, deckY + 2.3f, -2.57f), 90);
        Box("SalaBench", new Vector3(px + 3.25f, deckY + 0.4f, -2.57f), new Vector3(0.4f, 0.06f, 1.9f), teak);
        Box("SalaBenchLeg", new Vector3(px + 3.25f, deckY + 0.2f, -2.57f), new Vector3(0.3f, 0.4f, 0.08f), teakDark);
        // ราวไม้ไผ่เตี้ย ๆ ริมทางเดิน
        foreach (float side in new[] { -1f, 1f })
            Box("Rail", new Vector3(px + side * 0.88f, deckY + 0.55f, -6.6f), new Vector3(0.05f, 0.05f, 3.8f), plank);
        // ตะเกียงบนเสาศาลา + โอ่ง + ขดเชือก
        Lantern(new Vector3(px + 1.3f, deckY + 1.55f, -1.4f), 1.3f);
        Lamp(new Vector3(px + 1.6f, deckY + 1.9f, -1.7f), 8f, 5f);
        var jar = SaveMesh(ProceduralMeshes.Lathe(new[] { new Vector2(0.12f, 0), new Vector2(0.2f, 0.12f), new Vector2(0.23f, 0.28f), new Vector2(0.2f, 0.45f), new Vector2(0.13f, 0.55f), new Vector2(0.14f, 0.6f), new Vector2(0.11f, 0.6f) }, 16, Color.white), "HBJar");
        MeshAt("DragonJar", jar, new[] { dragonJar }, new Vector3(px + 3.1f, deckY, -3.5f), 0, 1.7f);
        MeshAt("DragonJar", jar, new[] { dragonJar }, new Vector3(px + 2.5f, deckY, -3.6f), 40, 1.35f);
        MeshAt("Jar", jar, new[] { clay }, new Vector3(px - 1.2f, deckY, -3.6f), 80, 1.5f);
        // แกลลอนน้ำมันสีเหลืองซีด + หูจับ
        Box("OilGallon", new Vector3(px + 1.9f, deckY + 0.18f, -3.45f), new Vector3(0.22f, 0.36f, 0.15f), gallon, 12);
        Box("GallonCap", new Vector3(px + 1.95f, deckY + 0.39f, -3.45f), new Vector3(0.05f, 0.05f, 0.05f), rust, 12);
        Box("GallonHandle", new Vector3(px + 1.85f, deckY + 0.4f, -3.45f), new Vector3(0.12f, 0.03f, 0.04f), gallon, 12);
        // ข้องเก่าวางข้างโอ่ง
        MeshAt("OldFishBasket", SaveMesh(ProceduralMeshes.FishBasket(), "HeadFishBasket"), new[] { SurfMat("HeadBasket", Color.white, 0.2f, vertexColor: true, smooth: 0.15f) }, new Vector3(px - 0.9f, deckY, -3.3f), 20, 1.2f);
        // พวงมาลัยแห้งบูชาแม่ย่านาง ห้อยเสาศาลา (ดอกเหี่ยวสีน้ำตาลเหลือง + ริบบิ้นแดง)
        {
            var gp = new Vector3(px + 1.3f, deckY + 1.25f, -1.38f);
            for (int k = 0; k < 14; k++)
            {
                float a = k / 14f * Mathf.PI * 2;
                var b = Prim(PrimitiveType.Sphere, "GarlandBead", gp + new Vector3(Mathf.Cos(a) * 0.09f, Mathf.Sin(a) * 0.14f - 0.14f, -0.07f), Vector3.one * 0.045f, garland); b.transform.SetParent(root, true);
            }
            for (int k = 0; k < 3; k++) Box("GarlandRibbon", gp + new Vector3(-0.03f + k * 0.03f, -0.4f, -0.07f), new Vector3(0.015f, 0.2f, 0.005f), ribbon, k * 8 - 8);
        }
        // โซ่เหล็กขึ้นสนิมจากเสาศาลาไปผูกหัวเรือ
        {
            Vector3 c0 = new Vector3(px - 1.55f, deckY + 0.3f, -3.85f), c1 = new Vector3(px - 2.5f, HMW + 0.35f, -4.4f);
            for (int k = 0; k <= 16; k++)
            {
                float t = k / 16f; var pt = Vector3.Lerp(c0, c1, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * 0.18f;
                var l = Prim(PrimitiveType.Cube, "ChainLink", pt, new Vector3(0.025f, 0.04f, 0.075f), rust); l.transform.SetParent(root, true);
                l.transform.rotation = Quaternion.LookRotation(c1 - c0) * Quaternion.Euler(0, 0, k % 2 * 90);
            }
        }
        // ตะเกียงรั้ววางบนพื้นท่าข้างตัวพรานปลา: วงแสงอุ่นท่ามกลางความมืด
        Lantern(new Vector3(px - 0.6f, deckY + 0.02f, -1.75f), 1.1f);
        Lamp(new Vector3(px - 0.6f, deckY + 0.5f, -1.8f), 5f, 3.5f, "#ffa850");
        for (int k = 0; k < 4; k++)
        {
            var c = Prim(PrimitiveType.Cylinder, "RopeCoil", new Vector3(px - 1.1f, deckY + 0.02f + k * 0.035f, -1.75f), new Vector3(0.55f - k * 0.08f, 0.018f, 0.55f - k * 0.08f), rope);
            c.transform.SetParent(root, true);
        }
        // เรือพายผูกข้างท่า + หลักผูกเรือ
        var boat = MeshAt("MooredBoat", SaveMesh(ProceduralMeshes.Rowboat(3.6f, 1.05f, 0.38f), "Rowboat"), new[] { boatMat }, new Vector3(px - 2.6f, HMW + 0.1f, -3.2f), 4);
        for (int k = -1; k <= 1; k++)
        {
            var th = Prim(PrimitiveType.Cube, "Thwart", Vector3.zero, new Vector3(0.88f, 0.04f, 0.2f), plank);
            th.transform.SetParent(boat.transform, false); th.transform.localPosition = new Vector3(0, 0.03f, k * 1f);
        }
        Post(new Vector3(px - 2.5f, HMW - 1f, -1f), deckY - HMW + 1.6f, 0.07f, plankDark);
        Post(new Vector3(px - 2.7f, HMW - 1f, -5.4f), deckY - HMW + 1.4f, 0.07f, plankDark);
        world.colliders.Add(new Vector4(px - 2.6f, -3.2f, 0.9f, 99));

        // ---------- เรือนไทยผู้ใหญ่บ้าน (ฝั่งตรงข้าม) ----------
        float hx = -3f, bankZ = HMZc(hx) + HMHw(hx);
        float hz = bankZ + 3.7f, gy = HeadmanGround(hx, hz), fy = gy + 2.1f;    // พื้นเรือนสูงจากดิน 2.1 ม. (ใต้ถุนสูง)
        int winSeq = 0;
        void Wall(Vector3 c, float w, float h, bool alongX, int windows)
        {
            Box("Wall", c, alongX ? new Vector3(w, h, 0.12f) : new Vector3(0.12f, h, w), teak);
            // ฝาปะกนเป็นแผง ๆ (เส้นคิ้วไม้)
            for (int k = 0; k <= Mathf.RoundToInt(w / 0.9f); k++)
            {
                float t = -w * 0.5f + k * (w / Mathf.Max(1, Mathf.RoundToInt(w / 0.9f)));
                var o = alongX ? new Vector3(t, 0, -0.07f) : new Vector3(-0.07f, 0, t);
                Box("Frame", c + o, alongX ? new Vector3(0.06f, h, 0.04f) : new Vector3(0.04f, h, 0.06f), teakDark);
            }
            for (int k = 0; k < windows; k++)
            {
                float t = (k + 0.5f) / windows * w - w * 0.5f;
                var o = alongX ? new Vector3(t, 0.1f, -0.075f) : new Vector3(-0.075f, 0.1f, t);
                int kind = (winSeq++ * 7 + 3) % 5;   // 0 ตะเกียงอุ่น · 1 มืด · 2 สลัว · 3 นีออนซีด · 4 มืด
                var paneMat = kind == 0 ? glow : kind == 2 ? dimGlow : kind == 3 ? neonGlow : darkPane;
                var g = Box("Window", c + o, alongX ? new Vector3(0.6f, 0.95f, 0.02f) : new Vector3(0.02f, 0.95f, 0.6f), paneMat);
                g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                if (kind == 0 || kind == 2 || kind == 3)
                {
                    for (int sl = 0; sl < 7; sl++)   // มู่ลี่ไม้ไผ่ลดลงครึ่งบาน
                    {
                        var so = o + new Vector3(0, 0.42f - sl * 0.07f, 0) + (alongX ? new Vector3(0, 0, -0.02f) : new Vector3(-0.02f, 0, 0));
                        Box("BlindSlat", c + so, alongX ? new Vector3(0.62f, 0.025f, 0.015f) : new Vector3(0.015f, 0.025f, 0.62f), bambooMat);
                    }
                    if (winSeq == 2)   // เงาคนยืนนิ่งหลังหน้าต่าง
                    {
                        var so = o + new Vector3(0.05f, -0.12f, 0) + (alongX ? new Vector3(0, 0, 0.01f) : new Vector3(0.01f, 0, 0));
                        Box("Silhouette", c + so, alongX ? new Vector3(0.2f, 0.5f, 0.005f) : new Vector3(0.005f, 0.5f, 0.2f), darkPane);
                        Box("SilhouetteHead", c + so + Vector3.up * 0.33f, alongX ? new Vector3(0.12f, 0.14f, 0.005f) : new Vector3(0.005f, 0.14f, 0.12f), darkPane);
                    }
                }
                Box("WindowFrame", c + o + (alongX ? new Vector3(0, 0, -0.02f) : new Vector3(-0.02f, 0, 0)), alongX ? new Vector3(0.72f, 0.07f, 0.03f) : new Vector3(0.03f, 0.07f, 0.72f), teakDark);
            }
        }
        // เรือนนอน (ใหญ่) + เรือนครัว (เล็ก) บนชานเดียวกัน
        foreach (var (cx, w, d, h, roofRise, win) in new[] { (hx, 8f, 5f, 2.5f, 3.3f, 4), (hx + 7.2f, 4.2f, 3.8f, 2.2f, 2.4f, 2) })
        {
            float cz = hz + (w < 5 ? 0.6f : 0);
            for (int ix = 0; ix <= 3; ix++) for (int iz = 0; iz <= 2; iz++)
                    Post(new Vector3(cx - w * 0.5f + ix * w / 3f, gy - 0.3f, cz - d * 0.5f + iz * d / 2f), fy - gy + 0.3f + h, 0.13f, teakDark);
            Wall(new Vector3(cx, fy + h * 0.5f, cz - d * 0.5f), w, h, true, win);
            Wall(new Vector3(cx, fy + h * 0.5f, cz + d * 0.5f), w, h, true, 0);
            Wall(new Vector3(cx - w * 0.5f, fy + h * 0.5f, cz), d, h, false, 1);
            Wall(new Vector3(cx + w * 0.5f, fy + h * 0.5f, cz), d, h, false, 1);
            Box("Floor", new Vector3(cx, fy - 0.08f, cz), new Vector3(w + 0.4f, 0.16f, d + 0.4f), teakDark);
            MeshAt("HouseRoof", SaveMesh(ProceduralMeshes.ThaiRoof(w + 1.4f, d * 0.5f + 1.1f, roofRise, true), "HeadRoof" + (int)w), new[] { tile, teakDark }, new Vector3(cx, fy + h, cz), 0);
            world.colliders.Add(new Vector4(cx, cz, Mathf.Max(w, d) * 0.5f, 99));
        }
        // ชานหน้าเรือน + ราวระเบียง + ตะเกียงแขวน
        float vz = hz - 2.5f - 1.2f;
        Box("Veranda", new Vector3(hx + 3.4f, fy - 0.08f, vz), new Vector3(15f, 0.16f, 2.4f), teak);
        for (float x = hx - 4f; x <= hx + 10.8f; x += 0.42f) Box("Baluster", new Vector3(x, fy + 0.42f, vz - 1.15f), new Vector3(0.05f, 0.7f, 0.05f), teakDark);
        Box("RailTop", new Vector3(hx + 3.4f, fy + 0.8f, vz - 1.15f), new Vector3(15f, 0.07f, 0.1f), teak);
        foreach (float x in new[] { hx - 3.9f, hx - 0.5f, hx + 3f, hx + 6.6f, hx + 10.6f })
        {
            Post(new Vector3(x, gy - 0.3f, vz - 1.1f), fy - gy + 0.3f, 0.12f, teakDark);
            Lantern(new Vector3(x, fy + 0.85f, vz - 1.15f), 1.2f);
        }
        foreach (float x in new[] { hx - 2f, hx + 8.5f }) Lamp(new Vector3(x, fy + 1.4f, vz - 0.6f), 8f, 3.2f);
        Lamp(new Vector3(hx - 2f, fy + 1.3f, hz - 1.5f), 5f, 1.4f);   // แสงในเรือนลอดหน้าต่าง (สลัวกว่าเดิม)
        // บันไดลงท่าน้ำ + ท่าน้ำเล็กหน้าเรือน
        for (int k = 0; k < 9; k++)
        {
            float t = k / 8f;
            Box("Step", new Vector3(hx + 1.2f, Mathf.Lerp(fy - 0.15f, HMW + 0.6f, t), Mathf.Lerp(vz - 1.4f, bankZ - 0.2f, t)), new Vector3(1.3f, 0.07f, 0.32f), teak);
        }
        for (float z = bankZ - 0.2f; z > bankZ - 3.6f; z -= 0.32f) Box("Plank", new Vector3(hx + 1.2f, HMW + 0.55f, z), new Vector3(2.2f, 0.06f, 0.28f), plank, Rf(-1.5f, 1.5f));
        foreach (var (x, z) in new[] { (hx + 0.2f, bankZ - 3.4f), (hx + 2.2f, bankZ - 3.4f), (hx + 0.2f, bankZ - 1.5f), (hx + 2.2f, bankZ - 1.5f) })
            Post(new Vector3(x, HMW - 1.5f, z), 2.4f, 0.08f, plankDark);
        Lantern(new Vector3(hx + 2.2f, HMW + 0.9f, bankZ - 3.4f), 1.1f);
        Lamp(new Vector3(hx + 2f, HMW + 1.3f, bankZ - 3.2f), 6f, 2.4f);
        // กาแล: ไม้แผ่นไขว้รูปตัว V บนยอดจั่วทั้งสองหลัง
        foreach (var (cx, w, rise) in new[] { (hx, 8f, 3.3f), (hx + 7.2f, 4.2f, 2.4f) })
            foreach (float side in new[] { -1f, 1f })
            {
                var top = new Vector3(cx + side * (w * 0.5f + 0.7f), fy + (w < 5 ? 2.2f : 2.5f) + rise, hz + (w < 5 ? 0.6f : 0));
                for (int k = -1; k <= 1; k += 2)
                {
                    var g = Box("Galae", top + new Vector3(0, 0.35f, k * 0.18f), new Vector3(0.05f, 0.9f, 0.16f), teakDark);
                    g.transform.rotation = Quaternion.Euler(k * 28, 0, 0);
                }
            }
        // ใต้ถุนเรือน: กองฟืน กระสอบข้าวสาร กรงไก่ไม้ไผ่ แคร่
        {
            float ux = hx - 1.5f, uz = hz - 1f;
            for (int k = 0; k < 12; k++)
            {
                var lg = Prim(PrimitiveType.Cylinder, "Firewood", new Vector3(ux - 2.2f + (k % 4) * 0.2f, gy + 0.09f + (k / 4) * 0.16f, uz + Rf(-0.05f, 0.05f)), new Vector3(0.16f, 0.6f, 0.16f), teakDark);
                lg.transform.rotation = Quaternion.Euler(90, 0, 0); lg.transform.SetParent(root, true);
            }
            for (int k = 0; k < 5; k++) Box("RiceSack", new Vector3(ux + 0.4f + (k % 3) * 0.55f, gy + 0.18f + (k / 3) * 0.3f, uz + 0.4f), new Vector3(0.5f, 0.32f, 0.75f), sack, Rf(-10, 10));
            float cx2 = ux + 3.6f, cz2 = uz - 0.3f;
            Box("CoopBase", new Vector3(cx2, gy + 0.05f, cz2), new Vector3(1.1f, 0.06f, 0.8f), plankDark);
            Box("CoopTop", new Vector3(cx2, gy + 0.75f, cz2), new Vector3(1.1f, 0.05f, 0.8f), plankDark);
            for (int k = 0; k <= 10; k++) { float t = k / 10f - 0.5f; Box("CoopSlat", new Vector3(cx2 + t * 1.05f, gy + 0.4f, cz2 - 0.39f), new Vector3(0.025f, 0.7f, 0.025f), bambooMat); Box("CoopSlat", new Vector3(cx2 + t * 1.05f, gy + 0.4f, cz2 + 0.39f), new Vector3(0.025f, 0.7f, 0.025f), bambooMat); }
            Box("Krae", new Vector3(ux + 6.2f, gy + 0.45f, uz), new Vector3(1.8f, 0.06f, 1f), bambooMat);
            foreach (var (ox, oz) in new[] { (-0.8f, -0.4f), (0.8f, -0.4f), (-0.8f, 0.4f), (0.8f, 0.4f) }) Box("KraeLeg", new Vector3(ux + 6.2f + ox, gy + 0.22f, uz + oz), new Vector3(0.06f, 0.45f, 0.06f), bambooMat);
            Lamp(new Vector3(ux + 2f, gy + 1.6f, uz), 4f, 0.7f, "#ff9a40");   // แสงตะเกียงริบหรี่ใต้ถุน
        }
        // เรือบดผูกริมตลิ่งหน้าเรือน
        MeshAt("Ruea Bot", SaveMesh(ProceduralMeshes.Rowboat(3.2f, 0.85f, 0.32f), "HeadDugout"), new[] { boatMat }, new Vector3(hx + 4f, HMW + 0.08f, bankZ - 1.2f), 84);
        // เสาไฟไม้โบราณริมตลิ่งสองฝั่ง ไฟสลัว
        foreach (var (lx, far) in new[] { (hx - 5f, true), (hx + 9f, true), (hx + 14f, true), (px + 8f, false), (px - 9f, false) })
        {
            float lz = far ? HMZc(lx) + HMHw(lx) + 0.8f : -5.5f, lg = HeadmanGround(lx, lz);
            Post(new Vector3(lx, lg - 0.2f, lz), 3.4f, 0.07f, plankDark);
            Box("LampArm", new Vector3(lx, lg + 3.05f, lz + (far ? -0.3f : 0.3f)), new Vector3(0.05f, 0.05f, 0.6f), plankDark);
            var bulb = Box("LampBulb", new Vector3(lx, lg + 2.9f, lz + (far ? -0.55f : 0.55f)), new Vector3(0.07f, 0.1f, 0.07f), dimGlow);
            bulb.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Lamp(new Vector3(lx, lg + 2.7f, lz + (far ? -0.6f : 0.6f)), 6f, 1.6f, "#ffb070");
        }
        // ท่าน้ำพังของบ้านเพื่อนบ้านไกล ๆ (เสาเอียง ไม้กระดานหลุด)
        foreach (float bx in new[] { 22f, -24f })
        {
            float bz = HMZc(bx) + HMHw(bx) - 1.5f;
            for (int k = 0; k < 5; k++)
            {
                var pp = Post(new Vector3(bx + (k % 2) * 1.4f, HMW - 1.2f, bz - k * 0.9f), 2.2f - k * 0.15f, 0.07f, plankDark);
                pp.transform.rotation = Quaternion.Euler(Rf(-12, 12), 0, Rf(-12, 12));
            }
            for (int k = 0; k < 4; k++) Box("BrokenPlank", new Vector3(bx + 0.7f, HMW + 0.5f - k * 0.1f, bz - k * 0.8f), new Vector3(1.6f, 0.05f, 0.25f), plankDark, Rf(-25, 25)).transform.rotation *= Quaternion.Euler(Rf(-15, 15), 0, Rf(-8, 8));
        }

        // ศาลพระภูมิริมตลิ่ง
        float sx = hx - 7f, sz = bankZ + 1.5f, sg = HeadmanGround(sx, sz);
        Post(new Vector3(sx, sg - 0.1f, sz), 1.5f, 0.09f, white);
        Box("ShrineBase", new Vector3(sx, sg + 1.45f, sz), new Vector3(0.8f, 0.08f, 0.8f), white);
        Box("ShrineBody", new Vector3(sx, sg + 1.75f, sz), new Vector3(0.5f, 0.55f, 0.45f), white);
        MeshAt("ShrineRoof", SaveMesh(ProceduralMeshes.ThaiRoof(0.75f, 0.42f, 0.45f, true), "HeadShrineRoof"), new[] { SurfMat("TempleTileOrange", Hex("#d27a4c"), 0.25f, smooth: 0.3f, tex: "clay_roof_tiles", tiles: 0.45f, cull: CullMode.Off), gold }, new Vector3(sx, sg + 2.02f, sz), 0);
        Box("ShrineCandle", new Vector3(sx, sg + 1.53f, sz - 0.3f), new Vector3(0.04f, 0.1f, 0.04f), glow);
        Lamp(new Vector3(sx, sg + 1.8f, sz - 0.5f), 3f, 1.2f, "#ffc070");

        // ---------- ตาล ไผ่ ต้นไม้ พุ่มไม้ สองฝั่ง ----------
        var natureRoot = new GameObject("Nature").transform;
        GameObject Nature(string file, float x, float z, float scale, float yaw, float collide = 0, float sink = 0.05f)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/{file}.glb");
            if (src == null) return null;
            var go = Object.Instantiate(src, natureRoot); go.name = file;
            go.transform.SetPositionAndRotation(new Vector3(x, HeadmanGround(x, z) - sink, z), Quaternion.Euler(0, yaw, 0)); go.transform.localScale = Vector3.one * scale;
            foreach (var rr in go.GetComponentsInChildren<Renderer>())
            {
                var mats = rr.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = LeafMat(file, mats[i]) ?? mats[i]; rr.sharedMaterials = mats;
            }
            if (collide > 0) world.colliders.Add(new Vector4(x, z, collide, 99));
            return go;
        }
        var palmTrunk = SurfMat("SugarPalmTrunk", Hex("#b8a890"), tex: "bark_brown_02", tiles: 0.9f, cull: CullMode.Off);
        var palmLeaf = SurfMat("SugarPalmLeaf", Color.white, sway: 0.05f, swayStart: 6f, vertexColor: true, smooth: 0.35f, cull: CullMode.Off);
        int pi = 0;
        foreach (var (x, dz, h) in new[] { (hx + 12.5f, 3f, 10f), (hx + 15f, 6f, 11f), (hx - 9.5f, 4f, 9.5f), (hx + 19f, 2f, 9f), (hx - 15f, 2.5f, 10.5f), (13f, -6f, 8.5f), (-11f, -7f, 9.5f), (hx + 25f, 7f, 11.5f), (hx - 20f, 8f, 12f), (hx + 4f, 12f, 12.5f), (hx - 2f, 14f, 11f), (24f, -9f, 10f) })
        {
            float z = dz > 0 ? HMZc(x) + HMHw(x) + dz : dz;
            var go = new GameObject("SugarPalm"); go.transform.SetParent(natureRoot, true);
            go.transform.SetPositionAndRotation(new Vector3(x, HeadmanGround(x, z) - 0.1f, z), Quaternion.Euler(0, Rf(0, 360), 0));
            go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.SugarPalm(431 + pi, h), "HeadPalm" + pi++);
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { palmTrunk, palmLeaf };
        }
        var bambooCulm = SurfMat("BambooCulm", Color.white, vertexColor: true, smooth: 0.45f);
        var bambooLeaf = SurfMat("BambooLeaf", Color.white, sway: 0.06f, swayStart: 3f, vertexColor: true, smooth: 0.3f, cull: CullMode.Off);
        int bi = 0;
        foreach (var (x, dz) in new[] { (hx - 12f, 2f), (hx - 5.5f, 9f), (hx + 22f, 3f), (-6f, -6f), (-15f, -5f), (10f, -7f), (18f, -5.5f), (hx + 16f, 1f), (hx - 17f, 1.2f), (-20f, -4.5f), (22f, -4.8f) })
        {
            float z = dz > 0 ? HMZc(x) + HMHw(x) + dz : dz;
            var go = new GameObject("Bamboo"); go.transform.SetParent(natureRoot, true);
            go.transform.SetPositionAndRotation(new Vector3(x, HeadmanGround(x, z) - 0.1f, z), Quaternion.Euler(0, Rf(0, 360), 0));
            go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.Bamboo(470 + bi, Rf(7f, 9.5f)), "HeadBamboo" + bi++);
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { bambooCulm, bambooLeaf };
            world.colliders.Add(new Vector4(x, z, 0.9f, 99));
        }
        for (int i = 0; i < 14; i++)
        {
            float x = Rf(-35, 35), z = HMZc(x) + HMHw(x) + Rf(10f, 18f);
            Nature("island_tree_01", x, z, Rf(1.6f, 2.2f), Rf(0, 360), 0, 0.2f);
        }
        foreach (var (x, z, s) in new[] { (-16f, -10f, 1.7f), (17f, -11f, 1.8f), (-4f, -12f, 1.5f), (9f, -12f, 1.6f) }) Nature("island_tree_01", x, z, s, Rf(0, 360), 0.9f, 0.2f);
        for (int t = 0, n = 0; t < 120 && n < 34; t++)
        {
            float x = Rf(-30, 30), far = rnd.NextDouble() < 0.55 ? 1 : -1, z = far > 0 ? HMZc(x) + HMHw(x) + Rf(0.5f, 9f) : Rf(-12f, -3.5f);
            if (Mathf.Abs(x - px) < 2.5f && far < 0) continue;
            if (far > 0 && x > hx - 5f && x < hx + 11f && z < hz + 3f) continue;   // เว้นหน้าเรือน
            if (HeadmanGround(x, z) < HMW + 0.1f) continue;
            Nature(new[] { "shrub_02", "fern_02", "shrub_03", "weed_plant_02", "shrub_04" }[n % 5], x, z, Rf(0.7f, 1.3f), Rf(0, 360), 0, 0.03f); n++;
        }
        var reedMat = SurfMat("Reeds", Color.white, sway: 0.08f, swayStart: 0.3f, vertexColor: true, smooth: 0.25f, cull: CullMode.Off);
        var reeds = new Mesh[2]; for (int i = 0; i < 2; i++) reeds[i] = SaveMesh(ProceduralMeshes.Reeds(950 + i, Rf(1.1f, 1.6f)), "HeadReeds" + i);
        for (int t = 0, n = 0; t < 3000 && n < 110; t++)
        {
            float x = Rf(-30, 30), z = Rf(-6, 18), h = HeadmanGround(x, z);
            if (h > HMW + 0.35f || h < HMW - 0.35f || Mathf.Abs(x - px) < 3.5f && z < 0 || Mathf.Abs(x - hx - 1.2f) < 2f && z > 10) continue;
            var g = new GameObject("Reeds"); g.transform.SetParent(natureRoot, true);
            g.transform.SetPositionAndRotation(new Vector3(x, Mathf.Max(h, HMW - 0.3f) - 0.05f, z), Quaternion.Euler(0, Rf(0, 360), 0));
            g.AddComponent<MeshFilter>().sharedMesh = reeds[rnd.Next(2)]; g.AddComponent<MeshRenderer>().sharedMaterial = reedMat; n++;
        }

        // ผิวน้ำ: ใบบัวเป็นกอริมตลิ่ง ใบไม้เน่าลอย ฟองรอบเสาท่าน้ำ
        {
            var ci = new System.Collections.Generic.List<CombineInstance>();
            var pad = ProceduralMeshes.Blob(new Color(0.2f, 0.32f, 0.16f), 10, 2); var padY = ProceduralMeshes.Blob(new Color(0.42f, 0.4f, 0.18f), 10, 2);
            var leafB = ProceduralMeshes.Blob(new Color(0.36f, 0.26f, 0.12f), 6, 2); var foamB = ProceduralMeshes.Blob(new Color(0.62f, 0.64f, 0.6f), 6, 2);
            void Put(Mesh mesh, Vector3 pos, float yaw, Vector3 sc) => ci.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(pos, Quaternion.Euler(0, yaw, 0), sc) });
            foreach (var (lx, lzSide) in new[] { (-14f, 1f), (-9f, -1f), (9f, 1f), (14f, -1f), (19f, 1f), (-19f, -1f) })   // กอบัวริมตลิ่ง
                for (int k = 0; k < 18; k++)
                {
                    float x = lx + Rf(-2.2f, 2.2f), z = HMZc(x) + lzSide * (HMHw(x) - Rf(0.6f, 2.4f));
                    if (HeadmanGround(x, z) > HMW - 0.05f) continue;
                    float dsz = Rf(0.3f, 0.55f); Put(rnd.NextDouble() < 0.8 ? pad : padY, new Vector3(x, HMW + 0.01f, z), Rf(0, 360), new Vector3(dsz, 0.01f, dsz));
                }
            for (int k = 0; k < 260; k++)
            {
                float x = Rf(-26, 26), z = HMZc(x) + Rf(-1, 1) * HMHw(x);
                if (HeadmanGround(x, z) > HMW - 0.05f) continue;
                float dsz = Rf(0.06f, 0.12f); Put(leafB, new Vector3(x, HMW + 0.008f, z), Rf(0, 360), new Vector3(dsz, 0.006f, dsz * 0.5f));
            }
            foreach (var (fx, fz) in new[] { (px - 0.85f, -1.3f), (px + 0.85f, -1.3f), (px + 3.55f, -1.3f), (hx + 0.2f, bankZ - 3.4f), (hx + 2.2f, bankZ - 3.4f) })
                for (int k = 0; k < 6; k++) { float dsz = Rf(0.08f, 0.2f); Put(foamB, new Vector3(fx + Rf(-0.3f, 0.3f), HMW + 0.012f, fz + Rf(-0.3f, 0.3f)), Rf(0, 360), new Vector3(dsz, 0.006f, dsz * 0.7f)); }
            var fm = new Mesh { indexFormat = IndexFormat.UInt32 }; fm.CombineMeshes(ci.ToArray(), true, true);
            var fg = new GameObject("RiverDebris"); fg.AddComponent<MeshFilter>().sharedMesh = SaveMesh(fm, "HeadRiverDebris");
            var fr = fg.AddComponent<MeshRenderer>(); fr.sharedMaterial = SurfMat("HeadDebris", Color.white, 0f, vertexColor: true, smooth: 0.5f); fr.shadowCastingMode = ShadowCastingMode.Off;
        }

        // หมอกจางบนผิวน้ำ + หิ่งห้อยริมตลิ่ง
        Particles("RiverMist", new Vector3(0, HMW + 0.35f, 6), new Vector3(50f, 16f, 0.1f), ParticleMat("Spray", new Color(1, 1, 1, 0.5f)), 46, new Vector2(4f, 9f), new Vector2(8f, 12f), new Vector2(0.04f, 0.14f), new Color(0.72f, 0.8f, 0.95f, 0.15f));
        var ff = Particles("Fireflies", new Vector3(hx + 3, HMW + 1.6f, bankZ + 2), new Vector3(36f, 6f, 2.5f), ParticleMat("Firefly", new Color(1.6f, 1.5f, 0.5f, 1)), 26, new Vector2(0.04f, 0.08f), new Vector2(3f, 6f), new Vector2(0.05f, 0.25f), new Color(0.9f, 1f, 0.45f, 1));
        var noise = ff.noise; noise.enabled = true; noise.strength = 0.4f; noise.frequency = 0.35f;
        var ff2 = Particles("Fireflies", new Vector3(hx + 2, gy + 0.8f, hz - 0.5f), new Vector3(12f, 4f, 1.4f), ParticleMat("Firefly", new Color(1.6f, 1.5f, 0.5f, 1)), 8, new Vector2(0.04f, 0.08f), new Vector2(3f, 6f), new Vector2(0.05f, 0.2f), new Color(0.9f, 1f, 0.45f, 1));
        var noise2 = ff2.noise; noise2.enabled = true; noise2.strength = 0.4f; noise2.frequency = 0.35f;

        // ---------- ตัวละคร กล้อง แสง ระบบเกม ----------
        var look = new Look { sun = "#b4c2e0", sunIntensity = 1.3f, sunRot = new Vector2(22, 100), fog = "#54607c", fogStart = 8, fogEnd = 70, exposure = 1.05f, ambient = 1.2f, skyId = "Overcast", skyTint = "#222c44" };   // ฟ้าน้ำเงินเข้มยามพลบค่ำ
        BuildGameplay(world, model, world.spawn, look, world);
        SaveStage(scene, path);
    }
}
