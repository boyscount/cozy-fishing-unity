// ด่าน: เขื่อนม่านหมอกยอดดอย — อ่างเก็บน้ำกว้างกลางหุบเขาบนดอย น้ำใสเย็น สนสามใบคลุมเขารอบอ่าง หมอกไหลลงจากยอดดอย
//      ผู้เล่นยืนบนเรือนแพไม้ไผ่หลังคาสังกะสีที่ต่อทางลูกบวบออกจากฝั่ง · ตอไม้ยืนต้นตายโผล่น้ำ (ป่าที่ถูกน้ำท่วมตอนสร้างเขื่อน)
//      ยอดเจดีย์ของหมู่บ้านเก่าที่จมโผล่พ้นน้ำ · น้ำใสมองลงไปเห็นหลังคาบ้านและโบสถ์ใต้น้ำ · แพเล็ก ๆ ไกล ๆ ในหมอก
//      กลางคืน: หิ่งห้อยริมฝั่ง ตะเกียงบนแพ · ผีวิญญาณใต้บาดาล (GhostSystem)
using System.IO;
using CozyFishing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class SceneBuilder
{
    const float VW = -0.3f;                                   // ระดับน้ำ
    const float VCz = 28f, VRx = 42f, VRz = 32f;              // อ่าง: วงรีรอบ (0, 28) ฝั่งใกล้อยู่ราว z = -4

    static float ReservoirGround(float x, float z)
    {
        float n = Mathf.PerlinNoise(x * 0.05f + 13, z * 0.05f + 41) - 0.5f, n2 = Mathf.PerlinNoise(x * 0.21f + 3, z * 0.21f + 9) - 0.5f, n3 = Mathf.PerlinNoise(x * 0.6f + 77, z * 0.6f + 5) - 0.5f;
        float dx = x / VRx, dz = (z - VCz) / VRz;
        float d = Mathf.Sqrt(dx * dx + dz * dz) + n * 0.22f + n2 * 0.04f;
        // ก้นอ่าง: ลึกขึ้นเข้ากลาง (ถึง ~7 ม.) · ที่ราบหมู่บ้านเก่าใต้น้ำลึก ~2.6 ม. (มองเห็นหลังคาจากบนแพ)
        float bed = VW - 1.2f - 5.8f * Mathf.Clamp01(1 - d * d) + n3 * 0.25f;
        float village = SStep(9f, 5f, Mathf.Abs(x + 6f)) * SStep(8f, 4f, Mathf.Abs(z - 16f));
        bed = Mathf.Lerp(bed, VW - 4.1f + n3 * 0.15f, village);   // ลึกพอให้หลังคาบ้านจมมิดใต้น้ำ ~1.3 ม.
        // ฝั่ง: ไหล่เขาชันขึ้นเป็นดอย · ฝั่งหลังผู้เล่น (z ต่ำ) ลาดเนินเตี้ยกว่า · ฝั่งไกลเป็นภูเขาสูงซ้อนกัน
        float far = SStep(10f, 40f, z);
        float rise = Mathf.Max(0, d - 1f);
        float bank = VW + 0.35f + rise * Mathf.Lerp(14f, 55f, far) * (0.75f + n * 0.9f) + n2 * 1.2f * SStep(0f, 0.3f, rise);
        bank += Mathf.Max(0, rise - 0.25f) * Mathf.Lerp(10f, 60f, far) * (0.6f + n2);       // ยอดดอยไกล ๆ
        return Mathf.Lerp(bed, bank, SStep(0.96f, 1.04f, d));
    }

    public static void CreateReservoir()
    {
        const string path = "Assets/Game/Scenes/Reservoir.unity";
        Directory.CreateDirectory(MatDir); Directory.CreateDirectory(GenDir);
        AssetDatabase.Refresh();
        ConfigureTextures();
        surfShader = Shader.Find("CozyFishing/Surface");
        var waterShader = Shader.Find("CozyFishing/Water");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        ConfigureUrp();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var rnd = new System.Random(9191);
        float Rf(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        // ---------- โลก ----------
        var world = new GameObject("World").AddComponent<StageWorld>();
        world.stageId = "reservoir"; world.waterY = VW;
        world.origin = new Vector2(-60, -30); world.cell = 0.5f; world.nx = 241; world.nz = 241;
        world.heights = new float[world.nx * world.nz];
        for (int j = 0; j < world.nz; j++) for (int i = 0; i < world.nx; i++) world.heights[j * world.nx + i] = ReservoirGround(world.origin.x + i * world.cell, world.origin.y + j * world.cell);
        world.walkBounds = new Rect(-12, -14, 24, 22);
        float deckY = VW + 0.3f;
        world.spawn = new Vector3(0.9f, deckY, 4.6f); world.spawnYaw = 0;
        world.special = new Vector3(7.5f, VW, 15.5f); world.specialRadius = 3.2f;   // วังน้ำรอบยอดเจดีย์จม
        world.camBounds = new Rect(-14, -9, 28, 26); world.camBias = new Vector2(1f, 13f); world.camBiasK = 0.35f;

        // ---------- พื้นดิน: หญ้าป่าสน / กรวดริมน้ำ / หน้าผาหิน ----------
        var terrain = new GameObject("Terrain");
        var m = Grid(170, 150, 230, new Vector3(0, 0, 30));
        var v = m.vertices; var cols = new Color[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            float x = v[i].x, z = v[i].z, h = ReservoirGround(x, z);
            v[i].y = h;
            float slope = Mathf.Abs(ReservoirGround(x + 0.6f, z) - h) + Mathf.Abs(ReservoirGround(x, z + 0.6f) - h);
            float gravel = SStep(VW + 0.7f, VW + 0.05f, h);                                     // แนวดินแดงขอบอ่าง (น้ำลดเห็นตลิ่ง)
            float rock = Mathf.Max(SStep(0.9f, 1.8f, slope), SStep(18f, 30f, h) * 0.5f);
            float wet = SStep(VW + 0.05f, VW - 1.5f, h);
            cols[i] = new Color(gravel, rock, wet, 0);
        }
        m.vertices = v; m.colors = cols; m.RecalculateNormals(); m.RecalculateBounds();
        terrain.AddComponent<MeshFilter>().sharedMesh = SaveMesh(m, "ReservoirTerrain");
        var tr = terrain.AddComponent<MeshRenderer>();
        tr.sharedMaterial = TerrainMat("Reservoir", ("forest_ground_05", "brown_mud_02", "rock_face_04"), new Vector4(0.28f, 0.45f, 0.14f, 0.025f), "#8c9c74", "#5a6e60");
        tr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- น้ำใสเย็น: มองทะลุเห็นซากหมู่บ้านใต้น้ำ ----------
        var water = new GameObject("ReservoirWater");
        water.AddComponent<MeshFilter>().sharedMesh = SaveMesh(Grid(100, 80, 150, new Vector3(0, VW, 28)), "ReservoirWater");
        var wm = LoadOrCreate($"{MatDir}/Water_Reservoir.mat", waterShader);
        wm.SetColor("_Shallow", Hex("#8cc4bc")); wm.SetColor("_Deep", Hex("#0f3a46"));
        wm.SetFloat("_DepthScale", 3.2f); wm.SetFloat("_Clarity", 0.95f); wm.SetFloat("_Glint", 1.1f); wm.SetFloat("_Refraction", 0.04f);
        wm.SetFloat("_Reflect", 0.75f); wm.SetFloat("_Waves", 0.02f); wm.SetFloat("_FoamWidth", 0.08f); wm.SetVector("_Flow", new Vector4(0.04f, 0.01f, 0, 0));
        wm.SetVector("_RapidZone", Vector4.zero);
        var wr = water.AddComponent<MeshRenderer>(); wr.sharedMaterial = wm; wr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- วัสดุ ----------
        var plank = SurfMat("HBPlank", Hex("#c0a888"), 0.04f, vertexColor: true, smooth: 0.12f, tex: "weathered_brown_planks", tiles: 0.7f);
        var plankDark = SurfMat("HBPlankDark", Hex("#9a8268"), 0.05f, vertexColor: true, smooth: 0.1f, tex: "weathered_brown_planks", tiles: 0.7f);
        var bamboo = SurfMat("HBBamboo", Hex("#c8b07a"), 0.04f, vertexColor: true, smooth: 0.35f, tex: "bark_brown_02", tiles: 1.6f);
        var tin = SurfMat("HBTin", Color.white, 0f, vertexColor: true, smooth: 0.35f, cull: CullMode.Off);
        var rope = SurfMat("HeadRope", Hex("#b89a6a"), 0f, smooth: 0.05f, tex: "reed_roof_04", tiles: 3f);
        var wicker = SurfMat("HBWicker", Hex("#b8a070"), 0f, vertexColor: true, smooth: 0.1f, tex: "reed_roof_04", tiles: 2.5f, cull: CullMode.Off);
        var interior = SurfMat("HBInterior", Hex("#1c1a16"), 0f, smooth: 0.02f);
        var gallon = SurfMat("HBGallonBlue", Hex("#3a5a8a"), 0.15f, smooth: 0.5f);
        var boatMat = SurfMat("HBBoat", Hex("#8a6e52"), 0.05f, vertexColor: true, smooth: 0.25f, tex: "weathered_brown_planks", tiles: 0.9f, cull: CullMode.Off);
        var deadWood = SurfMat("ResDeadWood", Hex("#a49c90"), 0.1f, vertexColor: true, smooth: 0.18f, tex: "bark_brown_02", tiles: 1.1f);   // ไม้ตายซีดขาวเทาจากแดดและน้ำ
        var plaster = SurfMat("TemplePlaster", Hex("#f4ecd8"), 0.15f, smooth: 0.08f, tex: "worn_mossy_plasterwall", tiles: 0.3f);
        var plasterSunk = SurfMat("ResPlasterSunk", Hex("#a8b4a0"), 0.55f, smooth: 0.1f, tex: "worn_mossy_plasterwall", tiles: 0.35f);   // ปูนใต้น้ำมีตะไคร่
        var goldTip = SurfMat("ResGold", Hex("#c9a24a"), 0f, smooth: 0.75f);
        var thatchSunk = SurfMat("ResThatchSunk", Hex("#5c5242"), 0.12f, smooth: 0.05f, vertexColor: true, tex: "reed_roof_04", tiles: 0.55f, cull: CullMode.Off);
        var ragMat = SurfMat("Ribbon", Color.white, sway: 0.12f, swayStart: 0f, vertexColor: true, smooth: 0.5f, cull: CullMode.Off);
        var potMat = SurfMat("HBPot", Hex("#2a2622"), 0.05f, smooth: 0.45f);
        var clay = SurfMat("HBClay", Hex("#8a5a3a"), 0.08f, smooth: 0.35f);
        Transform group = null;
        GameObject Box(string name, Vector3 lp, Vector3 s, Material mat, Vector3? rot = null)
        {
            var g = Prim(PrimitiveType.Cube, name, Vector3.zero, s, mat);
            g.transform.SetParent(group, false); g.transform.localPosition = lp; g.transform.localRotation = Quaternion.Euler(rot ?? Vector3.zero);
            return g;
        }
        GameObject Log(Vector3 lp, float len, float r, Material mat, Vector3 axisEuler)
        {
            var g = Prim(PrimitiveType.Cylinder, "Log", Vector3.zero, new Vector3(r * 2, len * 0.5f, r * 2), mat);
            g.transform.SetParent(group, false); g.transform.localPosition = lp; g.transform.localRotation = Quaternion.Euler(axisEuler);
            return g;
        }
        GameObject MeshAt(string name, Mesh mesh, Material mat, Vector3 lp, Vector3 rot, float scale = 1)
        {
            var g = new GameObject(name); g.transform.SetParent(group, false); g.transform.localPosition = lp; g.transform.localRotation = Quaternion.Euler(rot); g.transform.localScale = Vector3.one * scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = mat; return g;
        }
        void Boards(Vector3 c, float width, float h, bool axisX, float gap0 = 99, float gap1 = 99, float gapBottom = 0, float gapTop = 0)
        {
            for (float u = -width * 0.5f + 0.09f; u < width * 0.5f; u += 0.19f)
            {
                float bh = h * Rf(0.96f, 1.02f);
                var col = rnd.NextDouble() < 0.5 ? plankDark : plank;
                var off = axisX ? new Vector3(u, 0, Rf(-0.01f, 0.01f)) : new Vector3(Rf(-0.01f, 0.01f), 0, u);
                var size = axisX ? new Vector3(0.17f, 0, 0.035f) : new Vector3(0.035f, 0, 0.17f);
                if (u > gap0 && u < gap1)
                {
                    if (gapBottom > 0.05f) { size.y = gapBottom; Box("Board", c + off + Vector3.up * gapBottom * 0.5f, size, col); }
                    if (gapTop < bh - 0.05f) { size.y = bh - gapTop; Box("Board", c + off + Vector3.up * (gapTop + (bh - gapTop) * 0.5f), size, col); }
                    continue;
                }
                size.y = bh;
                Box("Board", c + off + Vector3.up * bh * 0.5f, size, col);
            }
        }
        var roots = new System.Collections.Generic.List<Transform>();

        // ---------- เรือนแพผู้เล่น: ทางลูกบวบจากฝั่ง + ลานแพ + เรือนไม้หลังคาสังกะสี ----------
        group = new GameObject("PlayerRaft").transform; roots.Add(group);
        foreach (var (a, b, w) in new[] { (new Vector2(0, -7f), new Vector2(0, 1.4f), 2.0f), (new Vector2(0, 1.4f), new Vector2(0, 7.8f), 7.4f) })
        {
            float len = b.y - a.y, cz = (a.y + b.y) * 0.5f;
            for (float x = -w * 0.5f + 0.13f; x < w * 0.5f; x += 0.26f) Log(new Vector3(x, VW + 0.08f, cz), len, 0.12f, bamboo, new Vector3(90, 0, 0));
            for (float z = a.y + 0.2f; z < b.y; z += 0.24f) if (w > 3) Box("DeckBoard", new Vector3(Rf(-0.03f, 0.03f), VW + 0.25f, z), new Vector3(w * Rf(0.98f, 1.01f), 0.05f, 0.2f), rnd.NextDouble() < 0.3 ? plankDark : plank);
            if (w < 3) for (float z = a.y + 0.3f; z < b.y; z += 1.4f) Box("Crossbar", new Vector3(0, VW + 0.22f, z), new Vector3(w + 0.2f, 0.07f, 0.12f), plankDark);
            world.docks.Add(new Rect(-w * 0.5f - 0.1f, a.y, w + 0.2f, len)); world.dockY.Add(w > 3 ? deckY : VW + 0.28f);
        }
        for (int k = 0; k < 6; k++) Box("FloatGallon", new Vector3((k % 2 == 0 ? -1 : 1) * 2.4f, VW - 0.04f, 2.2f + (k / 2) * 2.3f), new Vector3(0.36f, 0.32f, 0.5f), gallon, new Vector3(Rf(-6, 6), Rf(0, 30), Rf(-6, 6)));
        foreach (var (x, z) in new[] { (-3.6f, 1.6f), (3.6f, 1.6f), (-3.6f, 7.6f), (3.6f, 7.6f), (-1f, -3f), (1f, -3f) }) Log(new Vector3(x, VW + 0.25f, z), 3.2f, 0.08f, plankDark, Vector3.zero);   // หลักยึดแพ
        // เรือน: ด้านซ้ายของลาน ประตูหันมาทางทางเดิน · ฝาไม้กระดาน · หลังคาสังกะสีลอนขึ้นสนิม
        {
            float hx = -2.15f, hz = 3.2f, hw = 1.35f, hl = 1.7f, H = 2.1f, y0 = deckY;   // หลังซ้ายของจุดเกิด: มุมมองตกปลาข้างหน้าโล่ง
            Box("Interior", new Vector3(hx, y0 + H * 0.45f, hz), new Vector3(hw * 2 - 0.12f, H * 0.9f, hl * 2 - 0.12f), interior);
            Boards(new Vector3(hx, y0, hz + hl), hw * 2, H, true);
            Boards(new Vector3(hx, y0, hz - hl), hw * 2, H, true, -0.4f, 0.4f, 0, 1.8f);
            Boards(new Vector3(hx - hw, y0, hz), hl * 2, H, false);
            Boards(new Vector3(hx + hw, y0, hz), hl * 2, H, false, -0.45f, 0.45f, 0.9f, 1.55f);
            MeshAt("TinRoof", SaveMesh(ProceduralMeshes.TinRoof(911, hl * 2 + 1.2f, hw + 0.7f, 0.9f), "ResTin0"), tin, new Vector3(hx, y0 + H - 0.05f, hz), new Vector3(0, 0, 3));
            Box("Ridge", new Vector3(hx, y0 + H + 0.92f, hz), new Vector3(0.12f, 0.1f, hl * 2 + 1.2f), plankDark);
            world.colliders.Add(new Vector4(hx, hz, 1.55f, 99));
        }
        // ศาลพระภูมิบนเสาที่มุมแพ ผูกผ้าสามสี
        Log(new Vector3(-3.2f, deckY + 0.6f, 7.3f), 1.2f, 0.05f, plankDark, Vector3.zero);
        Box("Shrine", new Vector3(-3.2f, deckY + 1.38f, 7.3f), new Vector3(0.48f, 0.42f, 0.42f), plaster, new Vector3(0, 25, 0));
        Box("ShrineRoof", new Vector3(-3.2f, deckY + 1.66f, 7.3f), new Vector3(0.6f, 0.08f, 0.55f), plankDark, new Vector3(0, 25, 8));
        var cloth = new[] { Hex("#c02a2a"), Hex("#e8c030"), Hex("#2a8a3a") };
        for (int k = 0; k < 3; k++) MeshAt("TriColorCloth", SaveMesh(ProceduralMeshes.Ribbon(930 + k, cloth[k], Rf(0.45f, 0.65f)), "ResCloth" + k), ragMat, new Vector3(-3.2f + Rf(-0.05f, 0.05f), deckY + 1.1f, 7.3f), new Vector3(180, Rf(0, 360), 0), Rf(2.2f, 2.8f));
        // ราวตากผ้า + ปลาแห้งห้อย · ไซดักปลา โอ่ง ถัง ขดเชือก ลังวางตะเกียง
        for (int s = -1; s <= 1; s += 2) Log(new Vector3(3.45f, deckY + 0.8f, 3.4f + s * 1.1f), 1.6f, 0.03f, bamboo, Vector3.zero);   // ราวตากผ้าขอบแพด้านขวา (ไม่บังทางเหวี่ยงเบ็ด)
        Box("Line", new Vector3(3.45f, deckY + 1.55f, 3.4f), new Vector3(0.012f, 0.012f, 2.2f), plankDark);
        var ragCols = new[] { Hex("#3a4a6a"), Hex("#7a3a2a"), Hex("#8a7a5a"), Hex("#6a5a3a"), Hex("#6a5a3a") };
        for (int k = 0; k < 5; k++) MeshAt("Rag", SaveMesh(ProceduralMeshes.Ribbon(940 + k, ragCols[k], k < 3 ? Rf(0.5f, 0.7f) : 0.25f), "ResRag" + k), ragMat, new Vector3(3.45f, deckY + 1.55f, 2.6f + k * 0.4f), new Vector3(180, Rf(-20, 20), 0), k < 3 ? Rf(2.2f, 2.8f) : 1.6f);
        var trapMesh = SaveMesh(ProceduralMeshes.Lathe(new[] { new Vector2(0.03f, 0), new Vector2(0.14f, 0.15f), new Vector2(0.17f, 0.5f), new Vector2(0.12f, 0.85f), new Vector2(0.05f, 1f) }, 12, Color.white), "HBTrap");
        var jarMesh = SaveMesh(ProceduralMeshes.Lathe(new[] { new Vector2(0.12f, 0), new Vector2(0.2f, 0.12f), new Vector2(0.23f, 0.28f), new Vector2(0.2f, 0.45f), new Vector2(0.13f, 0.55f), new Vector2(0.14f, 0.6f), new Vector2(0.11f, 0.6f) }, 16, Color.white), "HBJar");
        MeshAt("FishTrap", trapMesh, wicker, new Vector3(2.6f, deckY + 0.17f, 6.6f), new Vector3(0, 20, 90), 1.1f);
        MeshAt("FishTrap", trapMesh, wicker, new Vector3(2.9f, deckY, 6.1f), new Vector3(0, 0, 0), 0.9f);
        MeshAt("FishTrap", trapMesh, wicker, new Vector3(3.15f, deckY, 6.5f), new Vector3(0, 0, 0), 0.75f);
        MeshAt("Jar", jarMesh, clay, new Vector3(-2.6f, deckY, 6.6f), new Vector3(0, 40, 0), 1.1f);
        MeshAt("Pot", jarMesh, potMat, new Vector3(-0.5f, deckY, 1.9f), new Vector3(90, 30, 0), 0.5f);
        MeshAt("Bucket", SaveMesh(ProceduralMeshes.Bucket(), "HBBucket"), SurfMat("HBBucketMat", Color.white, 0f, vertexColor: true, smooth: 0.5f), new Vector3(2.4f, deckY - 0.02f, 3.0f), Vector3.zero);
        MeshAt("FishBasket", SaveMesh(ProceduralMeshes.FishBasket(), "HBFishBasket"), SurfMat("HBBasketMat", Color.white, 0.1f, vertexColor: true, smooth: 0.15f), new Vector3(1.4f, deckY - 0.02f, 6.6f), new Vector3(0, 30, 0), 1.2f);
        for (int k = 0; k < 4; k++) Box("RopeCoil", new Vector3(3.0f, deckY + 0.02f + k * 0.04f, 2.4f), new Vector3(0.42f - k * 0.06f, 0.035f, 0.42f - k * 0.06f), rope, new Vector3(0, k * 30, 0));
        Box("Crate", new Vector3(1.9f, deckY + 0.22f, 4.4f), new Vector3(0.6f, 0.44f, 0.5f), plankDark, new Vector3(0, 12, 0));
        var lanterns = new System.Collections.Generic.List<(Vector3 p, bool mine)> { (new Vector3(1.9f, deckY + 0.45f, 4.4f), true) };

        // เรือพายผูกข้างแพ
        var boatMesh = SaveMesh(ProceduralMeshes.Rowboat(3.6f, 0.95f, 0.38f), "Rowboat");
        group = new GameObject("TiedBoat").transform; roots.Add(group);
        { var g = MeshAt("Rowboat", boatMesh, boatMat, new Vector3(4.5f, VW + 0.1f, 4.6f), new Vector3(0, 8, 0)); for (int k = -1; k <= 1; k++) Box("Thwart", new Vector3(4.5f + k * 0.12f, VW + 0.12f, 4.6f + k * 0.9f), new Vector3(0.8f, 0.04f, 0.18f), plank, new Vector3(0, 8, 0)); }
        for (int k = 0; k < 8; k++) { float t0 = k / 8f; var p = Vector3.Lerp(new Vector3(3.6f, deckY + 0.1f, 6.2f), new Vector3(4.6f, VW + 0.25f, 6.0f), t0) + Vector3.down * Mathf.Sin(t0 * Mathf.PI) * 0.12f; Box("Rope", p, new Vector3(0.14f, 0.022f, 0.022f), rope, new Vector3(0, -10, 0)); }

        // ---------- แพเล็กไกล ๆ ในหมอก ----------
        foreach (var (x, z, yaw) in new[] { (-16f, 30f, 20f), (13f, 36f, -15f), (-4f, 44f, 5f) })
        {
            group = new GameObject("FarRaft").transform; roots.Add(group);
            group.SetPositionAndRotation(new Vector3(x, VW, z), Quaternion.Euler(0, yaw, 0));
            for (float lx = -1.6f; lx <= 1.6f; lx += 0.3f) Log(new Vector3(lx, 0.06f, 0), 4.4f, 0.13f, bamboo, new Vector3(90, 0, 0));
            Box("Hut", new Vector3(0, 0.25f + 0.9f, 0.3f), new Vector3(2.2f, 1.8f, 2.4f), plankDark);
            MeshAt("TinRoof", SaveMesh(ProceduralMeshes.TinRoof(950 + (int)x, 3.2f, 1.6f, 0.7f), "ResFarTin" + (int)(x + 50)), tin, new Vector3(0, 2.05f, 0.3f), Vector3.zero);
            lanterns.Add((group.TransformPoint(new Vector3(1.2f, 1.1f, -1.2f)), false));
        }

        // ---------- ยอดเจดีย์หมู่บ้านเก่าโผล่พ้นน้ำ + โบสถ์/บ้านจมใต้น้ำ ----------
        group = new GameObject("SunkenChedi").transform; roots.Add(group);
        {
            float cx = 7.5f, cz = 18f, by = ReservoirGround(cx, cz) - 0.2f;
            var prof = new[] { new Vector2(2.4f, 0), new Vector2(2.4f, 0.8f), new Vector2(2.0f, 0.9f), new Vector2(2.0f, 1.6f), new Vector2(1.7f, 1.7f), new Vector2(1.75f, 2.4f),   // ฐานเขียงซ้อนชั้น
                new Vector2(1.55f, 3.1f), new Vector2(1.2f, 3.9f), new Vector2(0.75f, 4.5f), new Vector2(0.55f, 4.8f), new Vector2(0.6f, 5.0f), new Vector2(0.45f, 5.1f),       // องค์ระฆัง + บัลลังก์
                new Vector2(0.32f, 5.9f), new Vector2(0.2f, 6.8f), new Vector2(0.1f, 7.6f), new Vector2(0.02f, 8.1f) };                                                     // ปล้องไฉน ปลียอด
            MeshAt("Chedi", SaveMesh(ProceduralMeshes.Lathe(prof, 20, Color.white), "ResChedi"), plaster, new Vector3(cx, by, cz), new Vector3(0, 0, 4), 1.35f);   // องค์ระฆังโผล่พ้นน้ำ
            MeshAt("ChediTip", SaveMesh(ProceduralMeshes.Lathe(new[] { new Vector2(0.11f, 0), new Vector2(0.06f, 0.5f), new Vector2(0.015f, 0.9f) }, 10, Color.white), "ResChediTip"), goldTip, new Vector3(cx - 0.74f, by + 10.2f, cz), new Vector3(0, 0, 4), 1.35f);
            world.colliders.Add(new Vector4(cx, cz, 2.2f, 99));
        }
        // โบสถ์จม: ผนังปูนมีตะไคร่ หลังคาทรงไทยอยู่ใต้ผิวน้ำ ~0.9 ม. (เห็นลาง ๆ ผ่านน้ำใส)
        group = new GameObject("SunkenUbosot").transform; roots.Add(group);
        {
            float ux = 13f, uz = 26f, gy = ReservoirGround(ux, uz);
            group.SetPositionAndRotation(new Vector3(ux, gy, uz), Quaternion.Euler(0, -18, 0));
            float top = VW - 0.9f - gy;
            Box("Walls", new Vector3(0, top * 0.35f, 0), new Vector3(4.2f, top * 0.7f, 8f), plasterSunk);
            MeshAt("Roof", SaveMesh(ProceduralMeshes.ThaiRoof(8.6f, 2.8f, top * 0.3f, true), "ResUbosotRoof"), plasterSunk, new Vector3(0, top * 0.7f, 0), new Vector3(0, 90, 0));
        }
        // บ้านไม้หมู่บ้านเก่าจมบนที่ราบใต้น้ำ: เสา ผนังผุ หลังคาจากเหลือโครง
        int sh = 0;
        foreach (var (x, z, yaw) in new[] { (-9f, 13f, 15f), (-4.5f, 17.5f, -20f), (-10.5f, 19f, 40f), (-2.5f, 13.5f, 70f) })
        {
            group = new GameObject("SunkenHouse").transform; roots.Add(group);
            float gy = ReservoirGround(x, z);
            group.SetPositionAndRotation(new Vector3(x, gy, z), Quaternion.Euler(Rf(-3, 3), yaw, Rf(-4, 4)));
            for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2) Log(new Vector3(sx * 1.4f, 0.6f, sz * 1.1f), 1.3f, 0.08f, deadWood, Vector3.zero);
            Box("Floor", new Vector3(0, 1.2f, 0), new Vector3(3.2f, 0.1f, 2.6f), plankDark, new Vector3(0, 0, Rf(-6, 6)));
            Box("WallBack", new Vector3(0, 1.75f, 1.2f), new Vector3(3f, 1.0f, 0.06f), plankDark, new Vector3(Rf(-10, 4), 0, 0));
            if (sh % 2 == 0) Box("WallSide", new Vector3(-1.45f, 1.7f, 0), new Vector3(0.06f, 0.9f, 2.4f), plankDark, new Vector3(0, 0, Rf(-12, 12)));
            MeshAt("Thatch", SaveMesh(ProceduralMeshes.ThatchRoof(960 + sh, 3.4f, 1.8f, 0.9f), "ResSunkThatch" + sh), thatchSunk, new Vector3(0, 2.15f, 0), new Vector3(Rf(-8, 8), 90, Rf(-10, 10)));
            sh++;
        }

        // ---------- ตอไม้ยืนต้นตายโผล่น้ำ (ป่าที่ถูกน้ำท่วม) ----------
        group = new GameObject("DeadForest").transform; roots.Add(group);
        for (int i = 0, n = 0; i < 300 && n < 34; i++)
        {
            float x = Rf(-30, 30), z = Rf(6, 52), g = ReservoirGround(x, z);
            if (g > VW - 0.4f || Mathf.Abs(x) < 5 && z < 11 || (new Vector2(x - 7.5f, z - 18f)).sqrMagnitude < 12) continue;
            if (Mathf.PerlinNoise(x * 0.12f + 5, z * 0.12f + 3) < 0.45f) continue;   // เป็นดง ๆ
            float above = Rf(0.4f, n % 4 == 0 ? 4.5f : 2.2f), len = VW + above - g;
            var trunk = Log(new Vector3(x, g + len * 0.5f, z), len, Rf(0.1f, 0.2f), deadWood, new Vector3(Rf(-6, 6), Rf(0, 360), Rf(-6, 6)));
            if (above > 1.5f) for (int b = 0; b < 2; b++)   // กิ่งแห้งเหลือไม่กี่กิ่ง
            {
                float by = VW + above * Rf(0.55f, 0.9f), az = Rf(0, 360), bl = Rf(0.6f, 1.3f);
                var br = Log(new Vector3(x, by, z), bl, 0.045f, deadWood, new Vector3(Rf(35, 60), az, 0));
                br.transform.localPosition += br.transform.up * bl * 0.5f;
            }
            n++;
        }

        // รวม mesh ต่อกลุ่ม (วัตถุน้อยลง เบาเครื่องบนเว็บ) · แพผู้เล่น/เรือนไกลผุเก่าเล็กน้อย ซากใต้น้ำไม่ต้อง
        for (int i = 0; i < roots.Count; i++) { float sag = roots[i].name == "PlayerRaft" ? 0.05f : 0.03f; CombineGroup(roots[i], "ResGroup" + i, (mesh, root) => Weather(mesh, root, sag)); }

        // ตะเกียงบนแพ: แสงส้มอุ่น + ไอหมอกเรืองรอบดวงไฟ
        void Nature2(string file, Vector3 p, float scale)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/{file}.glb");
            if (src == null) return;
            var go = Object.Instantiate(src); go.name = file; go.transform.position = p; go.transform.localScale = Vector3.one * scale;
            foreach (var rr in go.GetComponentsInChildren<Renderer>()) rr.shadowCastingMode = ShadowCastingMode.Off;
        }
        var glow = ParticleMat("LanternGlow", new Color(1.5f, 0.85f, 0.4f, 0.55f));
        foreach (var (p, mine) in lanterns)
        {
            Nature2("brass_diya_lantern", p, 1.6f);
            var lg = new GameObject(mine ? "PlayerLanternLight" : "LanternLight").AddComponent<Light>();
            lg.type = LightType.Point; lg.range = mine ? 7f : 5f; lg.intensity = mine ? 3f : 1.6f; lg.color = Hex("#ffa850"); lg.shadows = LightShadows.None;
            lg.transform.position = p + Vector3.up * 0.4f;
            Particles("LanternHalo", p + Vector3.up * 0.4f, new Vector3(0.3f, 0.3f, 0.3f), glow, 1.2f, new Vector2(1.6f, 2.4f), new Vector2(3f, 4f), new Vector2(0f, 0.02f), new Color(1f, 0.62f, 0.3f, 0.3f));
        }

        // ---------- ป่าสนสามใบคลุมเขารอบอ่าง (instancing · เงาเฉพาะต้นใกล้) ----------
        var natureRoot = new GameObject("Nature").transform;
        var pineTrunk = SurfMat("PineTrunk", Color.white, vertexColor: true, instancing: true, smooth: 0.2f);
        var pineLeaf = SurfMat("PineLeaf", Color.white, sway: 0.035f, swayStart: 4f, vertexColor: true, instancing: true, smooth: 0.25f, cull: CullMode.Off);
        var pines = new Mesh[6]; for (int i = 0; i < 6; i++) pines[i] = SaveMesh(ProceduralMeshes.Pine(970 + i, 9f + i * 1.6f), "ResPine" + i);
        for (int i = 0, n = 0; i < 4000 && n < 260; i++)
        {
            float x = Rf(-75, 75), z = Rf(-28, 100), g = ReservoirGround(x, z);
            if (g < VW + 0.6f || g > 55f) continue;
            if (Mathf.Abs(x) < 3.5f && z > -9 && z < 9) continue;                                        // ไม่บังทางเดินแพ
            if (Mathf.PerlinNoise(x * 0.07f + 9, z * 0.07f + 2) < 0.38f) continue;                        // ป่าเป็นหย่อม เห็นทุ่งหญ้าบนไหล่เขาบ้าง
            float slope = Mathf.Abs(ReservoirGround(x + 1, z) - g) + Mathf.Abs(ReservoirGround(x, z + 1) - g);
            if (slope > 2.6f) continue;                                                                   // หน้าผาชันเกิน
            var go = new GameObject("Pine"); go.transform.SetParent(natureRoot, true);
            go.transform.SetPositionAndRotation(new Vector3(x, g - 0.15f, z), Quaternion.Euler(Rf(-3, 3), Rf(0, 360), Rf(-3, 3)));
            go.transform.localScale = Vector3.one * Rf(0.8f, 1.25f);
            go.AddComponent<MeshFilter>().sharedMesh = pines[rnd.Next(6)];
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { pineTrunk, pineLeaf };
            float dist = new Vector2(x - world.spawn.x, z - world.spawn.z).magnitude;
            mr.shadowCastingMode = dist < 22 ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (dist < 14) world.colliders.Add(new Vector4(x, z, 0.5f, 99));
            n++;
        }
        GameObject Nature(string file, float x, float z, float scale, float yaw, float collide = 0, bool shadows = true, float sink = 0.04f)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/{file}.glb");
            if (src == null) return null;
            var go = Object.Instantiate(src, natureRoot); go.name = file;
            go.transform.SetPositionAndRotation(new Vector3(x, ReservoirGround(x, z) - sink, z), Quaternion.Euler(0, yaw, 0)); go.transform.localScale = Vector3.one * scale;
            foreach (var rr in go.GetComponentsInChildren<Renderer>())
            {
                rr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                var mats = rr.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = LeafMat(file, mats[i]) ?? mats[i]; rr.sharedMaterials = mats;
            }
            if (collide > 0) world.colliders.Add(new Vector4(x, z, collide, 99));
            return go;
        }
        // ฝั่งใกล้หลังผู้เล่น: หินมอส พุ่มไม้ เฟิร์น ต้นไม้ตายเกยฝั่ง
        foreach (var (x, z, s) in new[] { (-6f, -7.5f, 0.5f), (5.5f, -8f, 0.55f), (-11f, -5f, 0.45f), (10f, -5.5f, 0.5f) }) Nature("rock_moss_set_01", x, z, s, Rf(0, 360), 1.0f, true, 0.15f);
        for (int i = 0; i < 26; i++)
        {
            float x = Rf(-16, 16), z = Rf(-14, -5);
            if (ReservoirGround(x, z) < VW + 0.1f || Mathf.Abs(x) < 1.8f) continue;
            Nature(new[] { "shrub_02", "fern_02", "weed_plant_02", "shrub_04" }[i % 4], x, z, Rf(0.6f, 1.0f), Rf(0, 360), i % 4 == 0 ? 0.6f : 0, i % 4 != 2, 0.03f);
        }
        Nature("dead_tree_trunk", -8f, -3.8f, 1.4f, 60, 0, true, 0.35f);
        Nature("boulder_01", 9f, -3.5f, 0.9f, 30, 1.2f, true, 0.3f);

        // ---------- หมอกไหลลงจากยอดดอย + หมอกเรี่ยผิวน้ำ · หิ่งห้อยริมฝั่ง ----------
        var mist = ParticleMat("Spray", new Color(1, 1, 1, 0.5f));
        foreach (var (fx, fz, fy, fs) in new[] { (-30f, 62f, 16f, 22f), (6f, 70f, 22f, 26f), (34f, 58f, 14f, 20f), (-42f, 30f, 10f, 14f), (44f, 26f, 9f, 14f) })   // หมอกพาดไหล่เขาไกล
            Particles("MountainMist", new Vector3(fx, fy, fz), new Vector3(fs, fs * 0.35f, 4f), mist, 7, new Vector2(8f, 14f), new Vector2(10f, 16f), new Vector2(0.1f, 0.3f), new Color(0.9f, 0.93f, 0.96f, 0.22f), 0.004f);
        var fly = ParticleMat("Firefly", new Color(1.6f, 1.5f, 0.5f, 1));
        var ff = Particles("Fireflies", new Vector3(0, VW + 1.2f, -2), new Vector3(30f, 8f, 2f), fly, 9, new Vector2(0.04f, 0.07f), new Vector2(3f, 6f), new Vector2(0.05f, 0.2f), new Color(0.9f, 1f, 0.45f, 1));
        var nz = ff.noise; nz.enabled = true; nz.strength = 0.4f; nz.frequency = 0.35f;
        var ffDusk = ff.gameObject.AddComponent<DuskOnly>(); ffDusk.from = 17.5f; ffDusk.to = 30f;

        // ---------- ตัวละคร กล้อง แสง ระบบเกม ----------
        var look = new Look { sun = "#ffe8d0", sunIntensity = 1.65f, sunRot = new Vector2(32, 205), fog = "#a6b8c2", fogStart = 30, fogEnd = 150, exposure = 0.95f, ambient = 1.0f };   // บ่ายบนดอย อากาศเย็น หมอกฟ้าอมเทา
        BuildGameplay(world, model, world.spawn, look, world);
        SaveStage(scene, path);
    }
    // ภาพปกด่าน (การ์ดเลือกด่าน/จอเดินทาง): เรนเดอร์จากกล้องมุมกว้างเหนือแพ มองอ่าง ยอดเจดีย์ ภูเขา → Resources/UI/Stages/reservoir.jpg
    //   Unity.exe -batchmode -quit -buildTarget WebGL -projectPath . -executeMethod SceneBuilder.ReservoirCover
    public static void ReservoirCover()
    {
        EditorSceneManager.OpenScene("Assets/Game/Scenes/Reservoir.unity");
        var go = new GameObject("CoverCam"); var cam = go.AddComponent<Camera>();
        go.transform.position = new Vector3(-3.5f, 3.6f, -4.5f); go.transform.LookAt(new Vector3(5f, 0.5f, 22f));
        cam.fieldOfView = 52; cam.nearClipPlane = 0.2f; cam.farClipPlane = 300; cam.clearFlags = CameraClearFlags.Skybox;
        var data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); data.renderPostProcessing = true;
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (c != cam) c.enabled = false;
        var hud = Object.FindFirstObjectByType<Hud>(); if (hud != null) hud.gameObject.SetActive(false);
        const int W = 1024, H = 576;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
        cam.Render(); cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
        const string outPath = "Assets/Game/Resources/UI/Stages/reservoir.jpg";
        File.WriteAllBytes(outPath, tex.EncodeToJPG(88));
        AssetDatabase.ImportAsset(outPath);
        if (AssetImporter.GetAtPath(outPath) is TextureImporter ti)
        {
            ti.mipmapEnabled = false;
            var ps = ti.GetPlatformTextureSettings("WebGL"); ps.overridden = true; ps.maxTextureSize = 512; ps.format = TextureImporterFormat.ASTC_6x6;
            ti.SetPlatformTextureSettings(ps); ti.SaveAndReimport();
        }
        Debug.Log("[SceneBuilder] cover saved " + outPath);
    }
}
