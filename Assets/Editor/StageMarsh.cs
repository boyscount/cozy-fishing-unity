// ด่าน: หนองบัวซ่อนศาล — หนองน้ำกว้างเต็มไปด้วยใบบัว ดอกบัวชมพู กอกก ธูปฤาษี · ยามโพล้เพล้มีหมอกลอยต่ำ หิ่งห้อย
//      ศาลเจ้าที่ไม้หลังเล็กบนเสาต้นเดียวกลางหนอง ผูกผ้าแพรเจ็ดสี พวงมาลัยดาวเรือง · ผู้เล่นเดินสะพานไม้ผุ ๆ ออกไปกลางหนอง
using System.IO;
using CozyFishing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class SceneBuilder
{
    const float MW = -0.3f;                                  // ระดับน้ำ
    static readonly Vector2 ShrinePos = new Vector2(6.5f, 14f);
    // สะพานไม้: ช่วงตรงตามแกน (ใช้เป็นพื้นที่ยืนได้ตรง ๆ) — จากฝั่ง → กลางหนอง → ชานพักใกล้ศาล
    static readonly (Vector2 a, Vector2 b)[] Walkway = { (new Vector2(0, -3.2f), new Vector2(0, 5f)), (new Vector2(0, 5f), new Vector2(4f, 5f)), (new Vector2(4f, 5f), new Vector2(4f, 9.6f)) };

    static float MarshGround(float x, float z)
    {
        float n = Mathf.PerlinNoise(x * 0.13f + 21, z * 0.13f + 8) - 0.5f, n2 = Mathf.PerlinNoise(x * 0.5f + 3, z * 0.5f + 13) - 0.5f;
        float shore = -2.2f + n * 3f;                                                    // ขอบหนองฝั่งใกล้ (คดเคี้ยว)
        float far = 30f + n * 6f;
        float land = Mathf.Max(SStep(shore + 1.2f, shore - 1.2f, z), SStep(far - 2f, far + 2f, z), SStep(20f, 25f, Mathf.Abs(x - 2)));
        float bank = 0.18f + n2 * 0.15f + Mathf.Max(0, z - far) * 0.25f + Mathf.Max(0, Mathf.Abs(x - 2) - 24) * 0.25f + Mathf.Max(0, -6 - z) * 0.05f;
        float bed = MW - 0.55f - n * 0.6f - n2 * 0.25f;                                 // หนองตื้น 0.3–1 ม.
        float pool = Mathf.Exp(-(Sq(x - ShrinePos.x) + Sq(z - ShrinePos.y - 1)) / 30f);  // วังลึกรอบเสาศาล
        float channel = Mathf.Exp(-Sq(x - (6f + Mathf.Sin(z * 0.2f) * 4f)) / 6f) * SStep(0, 6, z);   // ร่องน้ำคดเคี้ยวผ่านกลางหนอง
        bed -= pool * 1.5f + channel * 0.6f;
        float hummock = SStep(0.24f, 0.34f, Mathf.PerlinNoise(x * 0.22f + 40, z * 0.22f + 9) - 0.5f) * (1 - pool) * (1 - channel);   // เนินกกกลางหนอง
        bed = Mathf.Lerp(bed, MW + 0.12f, hummock);
        return Mathf.Lerp(bed, bank, land);
    }

    public static void CreateMarsh()
    {
        const string path = "Assets/Game/Scenes/Marsh.unity";
        Directory.CreateDirectory(MatDir); Directory.CreateDirectory(GenDir);
        AssetDatabase.Refresh();
        ConfigureTextures();
        surfShader = Shader.Find("CozyFishing/Surface");
        var waterShader = Shader.Find("CozyFishing/Water");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        ConfigureUrp();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var rnd = new System.Random(6262);
        float Rf(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        bool NearWalk(float x, float z, float pad)
        {
            foreach (var (a, b) in Walkway)
                if (x > Mathf.Min(a.x, b.x) - pad && x < Mathf.Max(a.x, b.x) + pad && z > Mathf.Min(a.y, b.y) - pad && z < Mathf.Max(a.y, b.y) + pad) return true;
            return false;
        }

        // ---------- โลก ----------
        var world = new GameObject("World").AddComponent<StageWorld>();
        world.stageId = "marsh"; world.waterY = MW;
        world.origin = new Vector2(-40, -20); world.cell = 0.5f; world.nx = 161; world.nz = 141;
        world.heights = new float[world.nx * world.nz];
        for (int j = 0; j < world.nz; j++) for (int i = 0; i < world.nx; i++) world.heights[j * world.nx + i] = MarshGround(world.origin.x + i * world.cell, world.origin.y + j * world.cell);
        world.walkBounds = new Rect(-20, -14, 42, 46);
        world.spawn = new Vector3(4f, 0, 10.1f); world.spawnYaw = 31;   // ปลายสะพาน หันหาศาล (ยืนฝั่งแล้วเหวี่ยงตรงไปจะโดนสะพาน)
        world.special = new Vector3(ShrinePos.x, MW, ShrinePos.y - 1.5f); world.specialRadius = 3f;
        world.camBounds = new Rect(-12, -9, 26, 24); world.camBias = new Vector2(4f, 9f); world.camBiasK = 0.35f;

        // ---------- พื้นดิน ----------
        var terrain = new GameObject("Terrain");
        var m = Grid(90, 70, 180, new Vector3(2, 0, 10));
        var v = m.vertices; var cols = new Color[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            float x = v[i].x, z = v[i].z, h = MarshGround(x, z);
            v[i].y = h;
            float mud = SStep(MW + 0.35f, MW + 0.02f, h);
            float trail = SStep(1.2f, 0.4f, Mathf.Abs(x - 0.3f)) * SStep(-3f, -4f, z) * SStep(-16f, -13f, z);   // ทางดินเดินมาสะพาน
            cols[i] = new Color(mud, trail, SStep(MW + 0.05f, MW - 1f, h), 0);
        }
        m.vertices = v; m.colors = cols; m.RecalculateNormals(); m.RecalculateBounds();
        terrain.AddComponent<MeshFilter>().sharedMesh = SaveMesh(m, "MarshTerrain");
        var tr = terrain.AddComponent<MeshRenderer>();
        tr.sharedMaterial = TerrainMat("Marsh", ("leafy_grass", "brown_mud_02", "grass_path_3"), new Vector4(0.3f, 0.4f, 0.35f, 0.03f), "#a2ac78", "#76866a");
        tr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- น้ำหนอง: ชาดำเข้ม สะท้อนฟ้ายามเย็น ----------
        var water = new GameObject("MarshWater");
        water.AddComponent<MeshFilter>().sharedMesh = SaveMesh(Grid(90, 70, 140, new Vector3(2, MW, 10)), "MarshWater");
        var wm = LoadOrCreate($"{MatDir}/Water_Marsh.mat", waterShader);
        wm.SetColor("_Shallow", Hex("#b8b898")); wm.SetColor("_Deep", Hex("#384430"));
        wm.SetFloat("_DepthScale", 1.2f); wm.SetFloat("_Clarity", 0.35f); wm.SetFloat("_Glint", 1.4f); wm.SetFloat("_Refraction", 0.025f);
        wm.SetFloat("_Reflect", 1f); wm.SetFloat("_Waves", 0.008f); wm.SetFloat("_FoamWidth", 0f); wm.SetVector("_Flow", new Vector4(0.02f, 0.01f, 0, 0));
        wm.SetVector("_RapidZone", Vector4.zero);
        var wr = water.AddComponent<MeshRenderer>(); wr.sharedMaterial = wm; wr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- สะพานไม้ผุ: แผ่นกระดานเว้นร่อง เอียงนิด ๆ บางแผ่นหาย · เสาไม้คู่ทุกช่วง ----------
        var plank = SurfMat("MarshPlank", Hex("#c8b8a0"), 0.15f, smooth: 0.12f, tex: "weathered_brown_planks", tiles: 0.6f);
        var post = SurfMat("MarshPost", Hex("#9a8a76"), 0.25f, smooth: 0.1f, tex: "bark_brown_02", tiles: 0.8f);
        var walkRoot = new GameObject("Walkway").transform;
        float deckY = MW + 0.38f;
        foreach (var (a, b) in Walkway)
        {
            var dir = (b - a); float len = dir.magnitude; dir /= len;
            var side = new Vector2(-dir.y, dir.x);
            for (float t = 0.12f; t < len; t += 0.28f)
            {
                if (rnd.NextDouble() < 0.06 && t > 2) continue;   // แผ่นหลุดหาย (ยังเดินได้ — เหยียบคาน)
                var p = a + dir * t;
                var g = Prim(PrimitiveType.Cube, "Plank", new Vector3(p.x, deckY + Rf(-0.02f, 0.02f), p.y), new Vector3(dir.x != 0 ? 0.24f : Rf(1.05f, 1.2f), 0.05f, dir.x != 0 ? Rf(1.05f, 1.2f) : 0.24f), plank);
                g.transform.rotation = Quaternion.Euler(Rf(-2, 2), Rf(-4, 4), Rf(-3, 3)); g.transform.SetParent(walkRoot, true);
            }
            for (int s = -1; s <= 1; s += 2)   // คานรองสองข้าง
            {
                var mid = (a + b) * 0.5f + side * s * 0.5f;
                var beam = Prim(PrimitiveType.Cube, "Beam", new Vector3(mid.x, deckY - 0.07f, mid.y), new Vector3(dir.x != 0 ? len : 0.1f, 0.1f, dir.x != 0 ? 0.1f : len), post);
                beam.transform.SetParent(walkRoot, true);
            }
            for (float t = 0; t <= len + 0.01f; t += 1.6f)
                for (int s = -1; s <= 1; s += 2)
                {
                    var p = a + dir * t + side * s * 0.55f;
                    float bot = MarshGround(p.x, p.y) - 0.3f, ptop = deckY + Rf(0.05f, 0.5f);
                    var c = Prim(PrimitiveType.Cylinder, "Post", new Vector3(p.x, (ptop + bot) * 0.5f, p.y), new Vector3(0.12f, (ptop - bot) * 0.5f, 0.12f), post);
                    c.transform.rotation = Quaternion.Euler(Rf(-4, 4), 0, Rf(-4, 4)); c.transform.SetParent(walkRoot, true);
                }
            float margin = 0.6f;
            var r = new Rect(Mathf.Min(a.x, b.x) - margin, Mathf.Min(a.y, b.y) - margin, Mathf.Abs(b.x - a.x) + margin * 2, Mathf.Abs(b.y - a.y) + margin * 2);
            world.docks.Add(r); world.dockY.Add(deckY + 0.03f);
        }
        // ชานพักปลายสะพาน
        var end = Walkway[Walkway.Length - 1].b;
        for (float dz = -0.6f; dz <= 0.61f; dz += 0.28f)
        {
            var g = Prim(PrimitiveType.Cube, "Plank", new Vector3(end.x, deckY, end.y + 0.6f + dz), new Vector3(2.2f, 0.05f, 0.24f), plank);
            g.transform.rotation = Quaternion.Euler(Rf(-2, 2), Rf(-3, 3), Rf(-2, 2)); g.transform.SetParent(walkRoot, true);
        }
        world.docks.Add(new Rect(end.x - 1.1f, end.y - 0.1f, 2.2f, 1.45f)); world.dockY.Add(deckY + 0.03f);

        // ---------- ศาลเจ้าที่บนเสาต้นเดียวกลางหนอง ----------
        var shrine = new GameObject("Shrine").transform;
        shrine.position = new Vector3(ShrinePos.x, MW, ShrinePos.y);   // สร้างขนาดจริงแล้วขยายทั้งหลังจากโคนเสาที่ผิวน้ำ
        GameObject SBox(string name, Vector3 c, Vector3 s, Material mat) { var g = Prim(PrimitiveType.Cube, name, c, s, mat); g.transform.SetParent(shrine, true); return g; }
        GameObject SMesh(string name, Mesh mesh, Material[] mats, Vector3 pos, Quaternion rot, float scale = 1)
        {
            var g = new GameObject(name); g.transform.SetParent(shrine, true); g.transform.SetPositionAndRotation(pos, rot); g.transform.localScale = Vector3.one * scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterials = mats; return g;
        }
        var wood = SurfMat("ShrineWood", Hex("#8a6a4c"), 0.2f, smooth: 0.15f, tex: "weathered_brown_planks", tiles: 1.2f);
        var redLacquer = SurfMat("ShrineOldWood", Hex("#7a6248"), 0.35f, smooth: 0.15f, tex: "weathered_brown_planks", tiles: 0.6f);   // ไม้เก่าผุคราบตะไคร่
        var plasterPost = SurfMat("ShrineStump", Hex("#6a5a46"), 0.55f, smooth: 0.18f, tex: "bark_brown_02", tiles: 0.8f);           // ตอไม้ใหญ่มอสเกาะ
        var gold = SurfMat("TempleGold", Hex("#c8a050"), 0.2f, smooth: 0.55f, cull: CullMode.Off);
        var tileRed = SurfMat("ShrineTileOld", Hex("#5e4c3c"), 0.45f, smooth: 0.2f, tex: "clay_roof_tiles", tiles: 1.6f, cull: CullMode.Off);   // กระเบื้องไม้ดำหม่นมอสขึ้น
        float sx = ShrinePos.x, sz = ShrinePos.y, top = MW + 1.6f;
        var pole = Prim(PrimitiveType.Cylinder, "ShrineStump", new Vector3(sx, (top + MW - 1.8f) * 0.5f, sz), new Vector3(0.95f, (top - MW + 1.8f) * 0.5f, 0.95f), plasterPost);
        {   // รากตอไม้แผ่ลงน้ำ บิดเป็นปุ่มปม
            var rootsG = new GameObject("StumpRoots"); rootsG.transform.SetParent(shrine, true); rootsG.transform.SetPositionAndRotation(new Vector3(sx, MW - 0.2f, sz), Quaternion.identity); rootsG.transform.localScale = new Vector3(0.9f, 0.4f, 0.9f);   // รากเตี้ยแผ่กว้างรอบโคนตอ
            rootsG.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.MangroveRoots(733, 1.6f), "MarshStumpRoots");
            rootsG.AddComponent<MeshRenderer>().sharedMaterial = SurfMat("MarshRootBark", Color.white, 0.4f, vertexColor: true, smooth: 0.2f, tex: "bark_brown_02", tiles: 1.2f, cull: CullMode.Off);
        }
        // หัวเสารูปบัวหงายรองแท่น
        var cap = Prim(PrimitiveType.Cylinder, "LotusCap", new Vector3(sx, top - 0.08f, sz), new Vector3(0.42f, 0.06f, 0.42f), plasterPost); cap.transform.SetParent(shrine, true);
        var cap2 = Prim(PrimitiveType.Cylinder, "LotusCapGold", new Vector3(sx, top - 0.16f, sz), new Vector3(0.3f, 0.03f, 0.3f), gold); cap2.transform.SetParent(shrine, true);
        pole.transform.rotation = Quaternion.Euler(2.5f, 0, -3f); pole.transform.SetParent(shrine, true);
        SBox("Platform", new Vector3(sx, top + 0.03f, sz), new Vector3(1.0f, 0.07f, 1.0f), wood);
        SBox("Body", new Vector3(sx, top + 0.33f, sz), new Vector3(0.56f, 0.52f, 0.62f), redLacquer);
        SBox("Door", new Vector3(sx, top + 0.3f, sz - 0.315f), new Vector3(0.2f, 0.32f, 0.02f), gold);
        SBox("DoorDark", new Vector3(sx, top + 0.28f, sz - 0.328f), new Vector3(0.13f, 0.24f, 0.01f), SurfMat("ShrineInner", Hex("#2a1a10"), smooth: 0.3f));
        SBox("TrimTop", new Vector3(sx, top + 0.6f, sz), new Vector3(0.6f, 0.035f, 0.66f), gold);
        SBox("TrimBase", new Vector3(sx, top + 0.08f, sz), new Vector3(0.62f, 0.04f, 0.68f), gold);
        // เครื่องเซ่น: กระถางธูป ธูปปลายแดง · น้ำแดงสองขวด · ตุ๊กตาช้างม้าเล็ก ๆ บนแท่น
        SBox("IncensePot", new Vector3(sx, top + 0.1f, sz - 0.42f), new Vector3(0.14f, 0.1f, 0.14f), SurfMat("Brass", Hex("#8a6a30"), 0, smooth: 0.5f));
        var emb = SurfMat("IncenseTip", Hex("#ff5a20"), smooth: 0.2f); emb.EnableKeyword("_EMISSION"); emb.SetColor("_EmissionColor", new Color(2.2f, 0.5f, 0.1f));
        for (int k = -1; k <= 1; k++)
        {
            SBox("Incense", new Vector3(sx + k * 0.03f, top + 0.24f, sz - 0.42f + k * 0.01f), new Vector3(0.008f, 0.2f, 0.008f), SurfMat("IncenseStick", Hex("#b0402a")));
            SBox("IncenseTip", new Vector3(sx + k * 0.03f, top + 0.345f, sz - 0.42f + k * 0.01f), new Vector3(0.012f, 0.015f, 0.012f), emb);
        }
        var soda = SurfMat("RedSoda", Hex("#c0141e"), smooth: 0.85f);
        foreach (var dx in new[] { -0.3f, 0.3f }) { var b = Prim(PrimitiveType.Cylinder, "RedSoda", new Vector3(sx + dx, top + 0.13f, sz - 0.38f), new Vector3(0.05f, 0.08f, 0.05f), soda); b.transform.SetParent(shrine, true); }
        var figure = SurfMat("Figurine", Hex("#e8e2d4"), smooth: 0.6f);
        foreach (var (dx, dz) in new[] { (-0.36f, 0.1f), (0.36f, 0.1f), (-0.36f, 0.3f) }) SBox("Figurine", new Vector3(sx + dx, top + 0.1f, sz + dz), new Vector3(0.07f, 0.08f, 0.1f), figure);
        foreach (var (dx, dz) in new[] { (-0.38f, -0.4f), (0.38f, -0.4f), (-0.38f, 0.4f), (0.38f, 0.4f) })
        {
            var c = Prim(PrimitiveType.Cylinder, "ShrineColumn", new Vector3(sx + dx, top + 0.33f, sz + dz), new Vector3(0.045f, 0.3f, 0.045f), gold); c.transform.SetParent(shrine, true);
        }
        var yaw90 = Quaternion.Euler(0, 90, 0);
        SMesh("ShrineRoof", SaveMesh(ProceduralMeshes.ThaiRoof(0.8f, 0.5f, 0.5f, true), "ShrineRoofUpper"), new[] { tileRed, gold }, new Vector3(sx, top + 0.72f, sz), yaw90);
        SMesh("ShrineRoofLow", SaveMesh(ProceduralMeshes.ThaiRoof(1.05f, 0.62f, 0.62f, false), "ShrineRoofLower"), new[] { tileRed, gold }, new Vector3(sx, top + 0.52f, sz), yaw90);
        var chofa = AssetDatabase.LoadAssetAtPath<Mesh>($"{GenDir}/Chofa.asset") ?? SaveMesh(ProceduralMeshes.Chofa(1f), "Chofa");
        for (int e = -1; e <= 1; e += 2) SMesh("Chofa", chofa, new[] { gold }, new Vector3(sx, top + 1.21f, sz + e * 0.4f), Quaternion.Euler(0, e < 0 ? 90 : -90, 0), 0.22f);
        // ผ้าแพรผูกรอบเสา + ชายผ้าห้อยปลิว · พวงมาลัยดาวเรืองคล้องเสา
        var ribbonMat = SurfMat("Ribbon", Color.white, sway: 0.12f, swayStart: 0f, vertexColor: true, smooth: 0.5f, cull: CullMode.Off);
        var cloth = new[] { Hex("#c0262a"), Hex("#e0b020"), Hex("#2e7a3a"), Hex("#c0262a"), Hex("#e0b020"), Hex("#2e7a3a"), Hex("#8a6a4a") };   // ผ้าสามสี + ผ้าสไบเก่า
        for (int k = 0; k < cloth.Length; k++) cloth[k] = Color.Lerp(cloth[k], Hex("#9a9284"), 0.38f);   // ผ้าเก่าซีดแดด
        for (int k = 0; k < cloth.Length; k++)
        {
            float y = top - 0.25f - k * 0.075f;
            var band = Prim(PrimitiveType.Cylinder, "ClothBand", new Vector3(sx + 0.005f * k, y, sz), new Vector3(0.97f, 0.036f, 0.97f), SurfMat("Cloth" + k, cloth[k], smooth: 0.45f));
            band.transform.rotation = Quaternion.Euler(Rf(-4, 4), 0, Rf(-4, 4)); band.transform.SetParent(shrine, true);
            float a = Rf(0, Mathf.PI * 2);
            var rb = SMesh("RibbonTail", SaveMesh(ProceduralMeshes.Ribbon(90 + k, cloth[k], Rf(0.5f, 0.85f)), "Ribbon" + k), new[] { ribbonMat },
                new Vector3(sx + Mathf.Cos(a) * 0.48f, y, sz + Mathf.Sin(a) * 0.48f), Quaternion.Euler(180, a * Mathf.Rad2Deg, Rf(-10, 10)));
            rb.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
        var garlandMat = SurfMat("Garland", Color.white, vertexColor: true, smooth: 0.3f, cull: CullMode.Off);
        SMesh("Garland", SaveMesh(ProceduralMeshes.Garland(7, 0.16f, 22), "Garland"), new[] { garlandMat }, new Vector3(sx, top - 0.05f, sz), Quaternion.Euler(12, 0, 6));
        SMesh("GarlandRail", SaveMesh(ProceduralMeshes.Garland(8, 0.07f, 12), "GarlandSmall"), new[] { garlandMat }, new Vector3(sx + 0.2f, top + 0.1f, sz - 0.42f), Quaternion.Euler(80, 0, 0));

        var clayOld = SurfMat("ShrineClay", Hex("#7a4a2e"), 0.25f, smooth: 0.3f);
        var potMesh = SaveMesh(ProceduralMeshes.Lathe(new[] { new Vector2(0.03f, 0), new Vector2(0.05f, 0.03f), new Vector2(0.055f, 0.07f), new Vector2(0.03f, 0.1f), new Vector2(0.035f, 0.12f) }, 10, Color.white), "ShrineOfferingPot");
        foreach (var (dx, dz, tip) in new[] { (-0.42f, -0.2f, 0f), (0.4f, -0.15f, 70f), (-0.25f, 0.38f, 0f), (0.3f, 0.36f, 0f), (0.1f, -0.45f, 85f) })
            SMesh("OfferingPot", potMesh, new[] { clayOld }, new Vector3(sx + dx, top + 0.07f, sz + dz), Quaternion.Euler(tip, Rf(0, 360), 0), Rf(0.9f, 1.4f));
        shrine.rotation = Quaternion.Euler(4f, 0, -6f);   // ศาลเก่าเอียงทรุดไปตามตอที่ผุ
        shrine.localScale = Vector3.one * 1.5f;
        float shrineTop = MW + (top - MW) * 1.5f;

        // ---------- ธรรมชาติ ----------
        var natureRoot = new GameObject("Nature").transform;
        GameObject Nature(string file, float x, float z, float scale, float yaw, float collide = 0, bool shadows = true, float sink = 0.04f, float? y = null)
        {
            Vary(ref file, ref scale, x, z);
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/{file}.glb");
            if (src == null) { Debug.LogWarning("[SceneBuilder] missing " + file); return null; }
            var go = Object.Instantiate(src, natureRoot);
            go.name = file;
            go.transform.SetPositionAndRotation(new Vector3(x, (y ?? MarshGround(x, z)) - sink, z), Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.one * scale;
            foreach (var rr in go.GetComponentsInChildren<Renderer>())
            {
                rr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                var mats = rr.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = LeafMat(file, mats[i]) ?? mats[i];
                rr.sharedMaterials = mats;
            }
            if (collide > 0) world.colliders.Add(new Vector4(x, z, collide, 99));
            return go;
        }
        // ใบบัว ดอกบัว ตูม เต็มหนอง (เว้นทางสะพาน วังลึกรอบศาล และร่องน้ำบางช่วงให้เหวี่ยงเบ็ดลงได้)
        var pad = SurfMat("LotusPad", Color.white, vertexColor: true, smooth: 0.4f);
        var lotus = SurfMat("Lotus", Color.white, vertexColor: true, smooth: 0.25f, cull: CullMode.Off);
        var padMeshes = new[] { SaveMesh(ProceduralMeshes.LotusPad(1), "LotusPad1"), SaveMesh(ProceduralMeshes.LotusPad(2), "LotusPad2"), SaveMesh(ProceduralMeshes.LotusPad(3), "LotusPad3") };
        var flowerMesh = SaveMesh(ProceduralMeshes.LotusFlower(4, false), "LotusFlower"); var budMesh = SaveMesh(ProceduralMeshes.LotusFlower(5, true), "LotusBud");
        var lotusRoot = new GameObject("Lotus").transform;
        void Obj(string name, Mesh mesh, Material mat, Vector3 p, float yaw, float scale, bool shadows)
        {
            var g = new GameObject(name); g.transform.SetParent(lotusRoot, true);
            g.transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0)); g.transform.localScale = Vector3.one * scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }
        int pads = 0;
        var stemMat = SurfMat("LotusStem", Hex("#4a6a30"), 0f, smooth: 0.3f);
        var podMesh = SaveMesh(ProceduralMeshes.Lathe(new[] { new Vector2(0.01f, -0.06f), new Vector2(0.03f, -0.02f), new Vector2(0.07f, 0.04f), new Vector2(0.075f, 0.06f), new Vector2(0.001f, 0.065f) }, 12, new Color(0.42f, 0.32f, 0.2f)), "LotusPod");
        var podMat = SurfMat("LotusPodDry", Color.white, 0.05f, vertexColor: true, smooth: 0.15f);
        for (int tries = 0; tries < 6000 && pads < 560; tries++)
        {
            float x = Rf(-16, 20), z = Rf(-1, 28), d = MW - MarshGround(x, z);
            if (d < 0.08f || d > 1.4f || NearWalk(x, z, 0.9f) || Sq(x - ShrinePos.x) + Sq(z - ShrinePos.y) < 6.5f) continue;
            float clump = Mathf.PerlinNoise(x * 0.35f + 5, z * 0.35f + 1);   // บัวขึ้นเป็นดง ๆ มีช่องน้ำโล่งระหว่างดง
            if (clump < 0.42f) continue;
            Obj("LotusPad", padMeshes[rnd.Next(3)], pad, new Vector3(x, MW + 0.02f, z), Rf(0, 360), Rf(0.35f, 0.65f), false);
            double fr = rnd.NextDouble();
            if (fr < 0.2) Obj("Lotus", flowerMesh, lotus, new Vector3(x + 0.2f, MW + Rf(0.1f, 0.4f), z), Rf(0, 360), Rf(0.24f, 0.32f), true);
            else if (fr < 0.34) Obj("LotusBud", budMesh, lotus, new Vector3(x + 0.2f, MW + Rf(0.15f, 0.5f), z), Rf(0, 360), Rf(0.17f, 0.24f), true);
            else if (fr < 0.44)   // ใบบัวหลวงชูก้านพ้นน้ำ
            {
                float hgt = Rf(0.4f, 1.1f);
                var st = Prim(PrimitiveType.Cylinder, "LotusStem", new Vector3(x - 0.15f, MW + hgt * 0.5f, z), new Vector3(0.025f, hgt * 0.5f, 0.025f), stemMat); st.transform.SetParent(lotusRoot, true);
                var lp = new GameObject("RaisedPad"); lp.transform.SetParent(lotusRoot, true); lp.transform.SetPositionAndRotation(new Vector3(x - 0.15f, MW + hgt, z), Quaternion.Euler(Rf(-25, 25), Rf(0, 360), Rf(-25, 25))); lp.transform.localScale = Vector3.one * Rf(0.5f, 0.8f);
                lp.AddComponent<MeshFilter>().sharedMesh = padMeshes[rnd.Next(3)]; lp.AddComponent<MeshRenderer>().sharedMaterial = pad;
            }
            else if (fr < 0.52)   // ฝักบัวแก่แห้งก้มหัว
            {
                float hgt = Rf(0.35f, 0.8f);
                var st = Prim(PrimitiveType.Cylinder, "PodStem", new Vector3(x + 0.1f, MW + hgt * 0.5f, z), new Vector3(0.018f, hgt * 0.5f, 0.018f), podMat); st.transform.SetParent(lotusRoot, true);
                var pg = new GameObject("LotusPod"); pg.transform.SetParent(lotusRoot, true); pg.transform.SetPositionAndRotation(new Vector3(x + 0.1f, MW + hgt, z), Quaternion.Euler(Rf(20, 60), Rf(0, 360), 0)); pg.transform.localScale = Vector3.one * Rf(1.2f, 1.7f);
                pg.AddComponent<MeshFilter>().sharedMesh = podMesh; pg.AddComponent<MeshRenderer>().sharedMaterial = podMat;
            }
            pads++;
        }
        // กอกก ธูปฤาษี ริมหนองและบนเนินกลางน้ำ
        var reedMat = SurfMat("Reeds", Color.white, sway: 0.08f, swayStart: 0.3f, vertexColor: true, smooth: 0.25f, cull: CullMode.Off);
        var reedMeshes = new Mesh[6];
        for (int i = 0; i < reedMeshes.Length; i++) reedMeshes[i] = SaveMesh(ProceduralMeshes.Reeds(200 + i, Rf(1.3f, 2.1f)), "Reeds" + i);
        int reeds = 0;
        for (int tries = 0; tries < 3000 && reeds < 140; tries++)
        {
            float x = Rf(-19, 22), z = Rf(-4, 30), h = MarshGround(x, z), d = MW - h;
            if (d > 0.45f || h > MW + 0.6f || NearWalk(x, z, 1.1f) || Sq(x - world.spawn.x) + Sq(z - world.spawn.z) < 9 || Mathf.Abs(x - 0.3f) < 1.4f && z < -2) continue;
            var g = new GameObject("Reeds"); g.transform.SetParent(natureRoot, true);
            g.transform.SetPositionAndRotation(new Vector3(x, Mathf.Max(h, MW - 0.4f) - 0.05f, z), Quaternion.Euler(0, Rf(0, 360), 0)); g.transform.localScale = Vector3.one * Rf(0.8f, 1.25f);
            g.AddComponent<MeshFilter>().sharedMesh = reedMeshes[rnd.Next(reedMeshes.Length)];
            g.AddComponent<MeshRenderer>().sharedMaterial = reedMat;
            if (h > MW) world.colliders.Add(new Vector4(x, z, 0.4f, 1.2f));
            reeds++;
        }
        // ตาลโตนดเรียงฝั่งไกล · ต้นไม้ใหญ่ · ขอนไม้จมน้ำ · พุ่มริมฝั่งใกล้
        var palmTrunk = SurfMat("SugarPalmTrunk", Hex("#b8a890"), tex: "bark_brown_02", tiles: 0.9f, cull: CullMode.Off);
        var palmLeaf = SurfMat("SugarPalmLeaf", Color.white, sway: 0.05f, swayStart: 6f, vertexColor: true, smooth: 0.35f, cull: CullMode.Off);
        int pi = 0;
        foreach (var (x, z, h) in new[] { (-14f, 33f, 9f), (-8f, 35f, 10f), (-1f, 34f, 8.5f), (9f, 36f, 9.5f), (15f, 33f, 8.8f), (21f, 30f, 9.2f), (-19f, 27f, 8f), (-12f, -9f, 8.2f), (12f, -8f, 7.6f) })
        {
            var go = new GameObject("SugarPalm"); go.transform.SetParent(natureRoot, true);
            go.transform.SetPositionAndRotation(new Vector3(x, MarshGround(x, z) - 0.1f, z), Quaternion.Euler(0, Rf(0, 360), 0));
            go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.SugarPalm(131 + pi, h), "MarshPalm" + pi++);
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { palmTrunk, palmLeaf };
            world.colliders.Add(new Vector4(x, z, 0.5f, 99));
        }
        foreach (var (x, z, s) in new[] { (-17f, -2f, 1.4f), (19f, -1f, 1.3f), (-22f, 14f, 1.6f), (24f, 16f, 1.5f) })
            Nature("island_tree_01", x, z, s, Rf(0, 360), 0.9f, true, 0.15f);
        Nature("dead_tree_trunk", -4.5f, 7f, 1.4f, 25, 0, true, 0, MW - 0.1f);
        Nature("dead_tree_trunk", 11f, 10f, 1.2f, -40, 0, true, 0, MW - 0.14f);
        Nature("root_cluster_01", -9f, -1.5f, 0.9f, 60, 0, true, 0.3f);
        for (int i = 0; i < 16; i++)
        {
            float x = Rf(-16, 16), z = Rf(-12, -2.5f);
            if (MarshGround(x, z) < MW + 0.1f || Mathf.Abs(x - 0.3f) < 2f) continue;
            Nature(new[] { "shrub_02", "fern_02", "weed_plant_02", "shrub_03" }[i % 4], x, z, Rf(0.6f, 1f), Rf(0, 360), i % 4 == 0 ? 0.6f : 0, i % 4 != 2, 0.03f);
        }

        // ต้นไทรย้อยรากอากาศลู่ลงน้ำ โอบรอบหนอง
        {
            var bBark = SurfMat("BendBark", Hex("#a09480"), 0.35f, smooth: 0.12f, vertexColor: true, tex: "bark_brown_02", tiles: 1.2f, cull: CullMode.Off);
            var bAerial = SurfMat("BendAerial", Hex("#8a8070"), 0.1f, sway: 0.05f, swayStart: 2f, vertexColor: true, smooth: 0.1f, cull: CullMode.Off);
            int bi = 0;
            foreach (var (x, z, h) in new[] { (-6f, 27f, 10f), (12f, 28f, 11f), (-15f, 18f, 9f), (21f, 20f, 9.5f), (2f, 31f, 10.5f) })
            {
                var go = new GameObject("Banyan"); go.transform.SetParent(natureRoot, true);
                go.transform.SetPositionAndRotation(new Vector3(x, MarshGround(x, z) - 0.2f, z), Quaternion.Euler(0, Rf(0, 360), 0)); go.transform.localScale = Vector3.one * Rf(0.9f, 1.15f);
                go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.Banyan(760 + bi, h), "MarshBanyan" + bi++);
                go.AddComponent<MeshRenderer>().sharedMaterials = new[] { bBark, bAerial };
                world.colliders.Add(new Vector4(x, z, 1.4f, 99));
            }
            // เจดีย์อิฐร้างจมหมอกฝั่งไกล
            var brickOld = SurfMat("TempleBrick", Hex("#f2c4a4"), 0.08f, smooth: 0.1f, tex: "mossy_brick", tiles: 0.5f);
            var ch = new GameObject("RuinedChedi"); ch.transform.SetParent(natureRoot, true);
            ch.transform.SetPositionAndRotation(new Vector3(15f, MarshGround(15f, 24f) - 0.3f, 24f), Quaternion.Euler(3, 10, -5)); ch.transform.localScale = Vector3.one * 1.1f;
            ch.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.Lathe(new[] { new Vector2(1.8f, 0), new Vector2(1.8f, 0.6f), new Vector2(1.4f, 0.6f), new Vector2(1.45f, 1.6f), new Vector2(1.3f, 2.4f), new Vector2(0.9f, 3.2f), new Vector2(0.35f, 3.8f), new Vector2(0.2f, 4.6f), new Vector2(0.05f, 5.2f) }, 16, Color.white), "MarshChedi");
            ch.AddComponent<MeshRenderer>().sharedMaterial = brickOld;
            world.colliders.Add(new Vector4(15f, 24f, 2f, 99));
            // ตะเกียงรั้วบนชานปลายสะพาน + เทียนบนหัวเสาสองต้น
            Nature("brass_diya_lantern", end.x + 0.7f, end.y + 0.9f, 1.5f, 0, 0, false, 0, deckY + 0.03f);
            Nature("fish_trap", end.x - 0.6f, end.y + 0.8f, 1f, 35, 0, true, 0, deckY + 0.03f);   // ไซดักปลาวางบนชานสะพาน (Blender)
            var ll = new GameObject("BridgeLantern").AddComponent<Light>(); ll.type = LightType.Point; ll.range = 6f; ll.intensity = 2.4f; ll.color = Hex("#ffa850"); ll.shadows = LightShadows.None; ll.transform.position = new Vector3(end.x + 0.7f, deckY + 0.5f, end.y + 0.9f);
            var candle = UnlitMat("CandleFlame", new Color(2.4f, 1.4f, 0.5f));
            foreach (var cx in new[] { end.x - 1.05f, end.x + 1.05f })
            {
                var cp = Prim(PrimitiveType.Cube, "CandlePost", new Vector3(cx, deckY + 0.2f, end.y + 1.3f), new Vector3(0.22f, 0.4f, 0.22f), post); cp.transform.SetParent(walkRoot, true);
                var cd = Prim(PrimitiveType.Cylinder, "Candle", new Vector3(cx, deckY + 0.47f, end.y + 1.3f), new Vector3(0.04f, 0.06f, 0.04f), SurfMat("CandleWax", Hex("#f0e8d0"), 0f, smooth: 0.4f)); cd.transform.SetParent(walkRoot, true);
                var fl = Prim(PrimitiveType.Sphere, "Flame", new Vector3(cx, deckY + 0.56f, end.y + 1.3f), new Vector3(0.03f, 0.05f, 0.03f), candle); fl.transform.SetParent(walkRoot, true); fl.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        // หมอกต่ำเหนือหนอง + หิ่งห้อยลอยรอบศาล
        var mist = ParticleMat("Spray", new Color(1, 1, 1, 0.5f));
        Particles("MarshMist", new Vector3(3, MW + 0.4f, 14), new Vector3(40f, 32f, 1f), mist, 36, new Vector2(4f, 8f), new Vector2(8f, 12f), new Vector2(0.03f, 0.12f), new Color(0.96f, 0.94f, 0.86f, 0.15f));
        var fly = ParticleMat("Firefly", new Color(1.6f, 1.5f, 0.5f, 1));
        var ff = Particles("Fireflies", new Vector3(ShrinePos.x - 2, MW + 0.9f, ShrinePos.y - 3), new Vector3(14f, 12f, 1.2f), fly, 5, new Vector2(0.04f, 0.07f), new Vector2(3f, 5f), new Vector2(0.05f, 0.2f), new Color(1, 0.95f, 0.5f, 1));
        var noise = ff.noise; noise.enabled = true; noise.strength = 0.35f; noise.frequency = 0.4f;
        var lamp = new GameObject("ShrineGlow").AddComponent<Light>();
        lamp.type = LightType.Point; lamp.range = 3f; lamp.intensity = 0.8f; lamp.color = Hex("#ffb060"); lamp.shadows = LightShadows.None;
        lamp.transform.position = new Vector3(sx, shrineTop + 0.5f, sz - 0.8f);

        // ---------- ตัวละคร กล้อง แสง ระบบเกม ----------
        var look = new Look { sun = "#ffd8a0", sunIntensity = 1.7f, sunRot = new Vector2(20, 135), fog = "#9ca492", fogStart = 6, fogEnd = 55, exposure = 0.95f, ambient = 0.95f, skyId = "Overcast", skyTint = "#b0ac96" };   // แดดเช้าสีทองอ่อนลอดม่านหมอกหนา
        world.spawn.y = deckY + 0.03f;
        BuildGameplay(world, model, world.spawn, look, world);
        SaveStage(scene, path);
    }
}
