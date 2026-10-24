// ด่าน: วังปลาโบสถ์ร้าง — วังน้ำลึกกลางป่า น้ำท่วมโบสถ์เก่าถึงขอบหน้าต่าง · หลังคาซ้อนสองตับ ช่อฟ้า ใบระกา ยังโผล่พ้นน้ำ
//      เจดีย์อิฐเอียงบนเกาะเล็ก · ใบเสมาโผล่น้ำรอบโบสถ์ · กำแพงแก้วพังเป็นช่วง ๆ · ผู้เล่นยืนบันไดท่าน้ำวัดฝั่งใกล้
using System.IO;
using CozyFishing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class SceneBuilder
{
    const float TW = -0.3f;                       // ระดับน้ำ
    const float TX = 1.5f, TZ = 16f;              // กลางโบสถ์ (แกนยาวตาม Z ประตูหันหาผู้เล่นที่ z = TZ - 6)
    static readonly Vector2 ChediPos = new Vector2(11f, 20f);

    static float TempleGround(float x, float z)
    {
        float n = Mathf.PerlinNoise(x * 0.16f + 5, z * 0.16f + 2) - 0.5f, n2 = Mathf.PerlinNoise(x * 0.55f + 9, z * 0.55f + 4) - 0.5f;
        float a = Mathf.Sqrt(Sq((x - 0.5f) / 13f) + Sq((z - 7.5f) / 8.5f));                     // วังน้ำ (วงรี)
        float b = Mathf.Max(Mathf.Abs(x - TX) / 7.2f, Mathf.Abs(z - TZ) / 8.6f);                   // ลานโบสถ์ที่จมน้ำ
        float bank = 0.22f + n * 0.5f + n2 * 0.12f + Mathf.Max(0, z - 26) * 0.32f + Mathf.Max(0, Mathf.Abs(x) - 17) * 0.3f;
        bank = Mathf.Min(bank, 6.5f + n * 2);
        float poolBed = TW - 0.4f - 2.6f * Mathf.Clamp01(1 - a * a) + n2 * 0.2f;
        float yardBed = TW - 1.45f + n2 * 0.15f;
        float h = Mathf.Lerp(bank, poolBed, SStep(1.2f, 0.95f, a));
        h = Mathf.Lerp(h, Mathf.Min(h, yardBed), SStep(1.15f, 0.95f, b));
        float isl = Mathf.Sqrt(Sq(x - ChediPos.x) + Sq(z - ChediPos.y)) / 3.4f;                     // เกาะเจดีย์
        return Mathf.Lerp(h, TW + 0.35f + n2 * 0.15f, SStep(1.1f, 0.7f, isl));
    }
    static float Sq(float v) => v * v;

    public static void CreateTemple()
    {
        const string path = "Assets/Game/Scenes/Temple.unity";
        Directory.CreateDirectory(MatDir); Directory.CreateDirectory(GenDir);
        AssetDatabase.Refresh();
        ConfigureTextures();
        surfShader = Shader.Find("CozyFishing/Surface");
        var waterShader = Shader.Find("CozyFishing/Water");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        ConfigureUrp();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var rnd = new System.Random(5151);
        float Rf(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        // ---------- โลก ----------
        var world = new GameObject("World").AddComponent<StageWorld>();
        world.stageId = "temple"; world.waterY = TW;
        world.origin = new Vector2(-40, -20); world.cell = 0.5f; world.nx = 161; world.nz = 141;
        world.heights = new float[world.nx * world.nz];
        for (int j = 0; j < world.nz; j++) for (int i = 0; i < world.nx; i++) world.heights[j * world.nx + i] = TempleGround(world.origin.x + i * world.cell, world.origin.y + j * world.cell);
        world.walkBounds = new Rect(-18, -14, 36, 44);
        world.spawn = new Vector3(0.4f, 0, -4.6f); world.spawnYaw = 0;
        world.special = new Vector3(TX, TW, TZ - 7.6f); world.specialRadius = 3f;
        world.camBounds = new Rect(-12, -9, 24, 22); world.camBias = new Vector2(TX, 9f); world.camBiasK = 0.35f;

        // ---------- พื้นดิน ----------
        var terrain = new GameObject("Terrain");
        var m = Grid(90, 70, 180, new Vector3(0, 0, 10));
        var v = m.vertices; var cols = new Color[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            float x = v[i].x, z = v[i].z, h = TempleGround(x, z);
            v[i].y = h;
            float slope = Mathf.Abs(TempleGround(x + 0.5f, z) - h) + Mathf.Abs(TempleGround(x, z + 0.5f) - h);
            float mud = SStep(TW + 0.4f, TW + 0.02f, h);
            float ground = Mathf.Max(SStep(0.25f, 0.6f, slope), 0.35f * (Mathf.PerlinNoise(x * 0.3f, z * 0.3f)));
            cols[i] = new Color(mud, ground, SStep(TW + 0.05f, TW - 1.2f, h), 0);
        }
        m.vertices = v; m.colors = cols; m.RecalculateNormals(); m.RecalculateBounds();
        terrain.AddComponent<MeshFilter>().sharedMesh = SaveMesh(m, "TempleTerrain");
        var tr = terrain.AddComponent<MeshRenderer>();
        tr.sharedMaterial = TerrainMat("Temple", ("leafy_grass", "mud_forest", "forest_ground_05"), new Vector4(0.3f, 0.4f, 0.3f, 0.03f), "#9db07e", "#748a6c");
        tr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- น้ำวังลึก: เขียวขุ่น นิ่ง ----------
        var water = new GameObject("PoolWater");
        water.AddComponent<MeshFilter>().sharedMesh = SaveMesh(Grid(90, 70, 140, new Vector3(0, TW, 10)), "TempleWater");
        var wm = LoadOrCreate($"{MatDir}/Water_Temple.mat", waterShader);
        wm.SetColor("_Shallow", Hex("#b0c0a0")); wm.SetColor("_Deep", Hex("#1d362c"));
        wm.SetFloat("_DepthScale", 1.7f); wm.SetFloat("_Clarity", 0.45f); wm.SetFloat("_Glint", 1.1f); wm.SetFloat("_Refraction", 0.03f);
        wm.SetFloat("_Reflect", 0.9f); wm.SetFloat("_Waves", 0.012f); wm.SetFloat("_FoamWidth", 0.06f); wm.SetVector("_Flow", new Vector4(0.03f, 0.02f, 0, 0));
        wm.SetVector("_RapidZone", Vector4.zero);
        var wr = water.AddComponent<MeshRenderer>(); wr.sharedMaterial = wm; wr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- วัสดุโบสถ์ ----------
        var plaster = SurfMat("TemplePlaster", Hex("#f4ecd8"), 0.15f, smooth: 0.08f, tex: "worn_mossy_plasterwall", tiles: 0.3f);
        var brick = SurfMat("TempleBrick", Hex("#f2c4a4"), 0.08f, smooth: 0.1f, tex: "mossy_brick", tiles: 0.5f);
        var tileOrange = SurfMat("TempleTileOrange", Hex("#d27a4c"), 0.25f, smooth: 0.3f, tex: "clay_roof_tiles", tiles: 0.45f, cull: CullMode.Off);
        var tileGreen = SurfMat("TempleTileGreen", Hex("#7fa078"), 0.25f, smooth: 0.3f, tex: "clay_roof_tiles", tiles: 0.45f, cull: CullMode.Off);
        var gold = SurfMat("TempleGold", Hex("#c8a050"), 0.2f, smooth: 0.55f, cull: CullMode.Off);
        var root = new GameObject("SunkenTemple").transform;
        GameObject Box(string name, Vector3 c, Vector3 s, Material mat)
        {
            var g = Prim(PrimitiveType.Cube, name, c, s, mat); g.transform.SetParent(root, true); return g;
        }
        GameObject MeshAt(string name, Mesh mesh, Material[] mats, Vector3 pos, Quaternion rot, float scale = 1)
        {
            var g = new GameObject(name);
            g.transform.SetParent(root, true); g.transform.SetPositionAndRotation(pos, rot); g.transform.localScale = Vector3.one * scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterials = mats;
            return g;
        }

        // ฐานโบสถ์ (จมใต้น้ำ เห็นลาง ๆ ผ่านผิวน้ำ) · พื้นโบสถ์อยู่ใต้น้ำ 1.1 ม. → น้ำปริ่มขอบหน้าต่าง
        float F = TW - 1.1f, T = F + 3.9f;
        Box("Platform", new Vector3(TX, F - 0.6f, TZ), new Vector3(9.4f, 1.2f, 16.6f), brick);
        // ผนังข้าง: เว้นช่องหน้าต่าง 3 บาน · มุมหน้าขวาพังลงมา
        float[] winZ = { -3.6f, 0, 3.6f };
        for (int side = -1; side <= 1; side += 2)
        {
            float wx = TX + side * 3.0f;
            var segs = new[] { (-6f, -4.15f), (-3.05f, -0.55f), (0.55f, 3.05f), (4.15f, 6f) };
            foreach (var (z0, z1) in segs)
            {
                bool broken = side > 0 && z0 < -5;
                float top = broken ? F + 2.1f : T;
                Box("Wall", new Vector3(wx, (F + top) * 0.5f, TZ + (z0 + z1) * 0.5f), new Vector3(0.4f, top - F, z1 - z0), plaster);
            }
            foreach (var wz in winZ)
            {
                Box("Sill", new Vector3(wx, F + 0.7f, TZ + wz), new Vector3(0.4f, 1.4f, 1.1f), plaster);
                if (!(side > 0 && wz < -3)) Box("Lintel", new Vector3(wx, (F + 3.0f + T) * 0.5f, TZ + wz), new Vector3(0.4f, T - F - 3.0f, 1.1f), plaster);
                // กรอบซุ้มหน้าต่างปิดทอง (ด้านนอก)
                if (!(side > 0 && wz < -3)) Box("WinFrame", new Vector3(wx + side * 0.22f, F + 3.12f, TZ + wz), new Vector3(0.06f, 0.18f, 1.4f), gold);
            }
        }
        // ผนังหน้า (ประตูกลาง) · ผนังหลังทึบ
        foreach (var (x0, x1) in new[] { (-3.2f, -0.9f), (0.9f, 3.2f) })
            Box("FrontWall", new Vector3(TX + (x0 + x1) * 0.5f, (F + T) * 0.5f, TZ - 6f), new Vector3(x1 - x0, T - F, 0.4f), plaster);
        Box("DoorLintel", new Vector3(TX, (F + 3.2f + T) * 0.5f, TZ - 6f), new Vector3(1.8f, T - F - 3.2f, 0.4f), plaster);
        Box("DoorFrame", new Vector3(TX, F + 3.35f, TZ - 6.22f), new Vector3(2.3f, 0.22f, 0.06f), gold);
        Box("BackWall", new Vector3(TX, (F + T) * 0.5f, TZ + 6f), new Vector3(6.4f, T - F, 0.4f), plaster);
        // บัวคาดบนผนัง
        Box("CorniceL", new Vector3(TX - 3.0f, T + 0.1f, TZ + 0.9f), new Vector3(0.6f, 0.2f, 10.2f), plaster);
        Box("CorniceR", new Vector3(TX + 3.0f, T + 0.1f, TZ), new Vector3(0.6f, 0.2f, 12.2f), plaster);
        Box("CorniceF", new Vector3(TX, T + 0.1f, TZ - 6f), new Vector3(6.6f, 0.2f, 0.6f), plaster);
        Box("CorniceB", new Vector3(TX, T + 0.1f, TZ + 6f), new Vector3(6.6f, 0.2f, 0.6f), plaster);

        // เสาระเบียงรอบโบสถ์ + บัวหัวเสา
        var capital = SaveMesh(ProceduralMeshes.Lathe(new[] { new Vector2(0.2f, 0), new Vector2(0.22f, 0.05f), new Vector2(0.3f, 0.18f), new Vector2(0.36f, 0.3f), new Vector2(0.3f, 0.34f), new Vector2(0.3f, 0.4f) }, 16, Color.white), "TempleCapital");
        float colTop = T - 0.3f;
        void Column(float x, float z)
        {
            var c = Prim(PrimitiveType.Cylinder, "Column", new Vector3(x, (F + colTop) * 0.5f, z), new Vector3(0.38f, (colTop - F) * 0.5f, 0.38f), plaster);
            c.transform.SetParent(root, true);
            MeshAt("Capital", capital, new[] { gold }, new Vector3(x, colTop - 0.38f, z), Quaternion.identity);
        }
        foreach (var z in new[] { -7.2f, -4f, -1f, 2f, 5f }) { Column(TX - 3.9f, TZ + z); if (!(z < -6)) Column(TX + 3.9f, TZ + z); }
        Column(TX - 1.6f, TZ - 7.2f); Column(TX + 1.6f, TZ - 7.2f);
        // เสามุมหน้าขวาหักโค่น นอนจมน้ำ
        var fallen = Prim(PrimitiveType.Cylinder, "FallenColumn", new Vector3(TX + 5.2f, TW - 0.05f, TZ - 6.6f), new Vector3(0.38f, 1.5f, 0.38f), plaster);
        fallen.transform.rotation = Quaternion.Euler(84, 35, 0); fallen.transform.SetParent(root, true);

        // หลังคาซ้อน 2 ตับ (ลาดเท่ากัน ตับล่างยาวและกว้างกว่า ลดระดับลง) สลับสีส้ม/เขียว · ช่อฟ้า + หางหงส์
        float y1 = T + 0.8f, rise1 = 3.4f, hw1 = 3.8f, len1 = 11f;
        float hw2 = 4.6f, rise2 = rise1 * hw2 / hw1, len2 = 13.4f, y2 = y1 + rise1 - 0.6f - rise2;
        var yaw90 = Quaternion.Euler(0, 90, 0);
        MeshAt("RoofUpper", SaveMesh(ProceduralMeshes.ThaiRoof(len1, hw1, rise1, true), "TempleRoofUpper"), new[] { tileOrange, gold }, new Vector3(TX, y1, TZ), yaw90);
        MeshAt("RoofLower", SaveMesh(ProceduralMeshes.ThaiRoof(len2, hw2, rise2, false), "TempleRoofLower"), new[] { tileGreen, gold }, new Vector3(TX, y2, TZ), yaw90);
        var chofa = SaveMesh(ProceduralMeshes.Chofa(1f), "Chofa");
        for (int e = -1; e <= 1; e += 2)
        {
            var outward = Quaternion.Euler(0, e < 0 ? 90 : -90, 0);   // ปลายงอนชี้ออกนอกตามแกนยาว
            MeshAt("Chofa", chofa, new[] { gold }, new Vector3(TX, y1 + rise1 - 0.05f, TZ + e * len1 * 0.5f), outward, 1.15f);
            for (int s = -1; s <= 1; s += 2)
            {
                MeshAt("HangHong", chofa, new[] { gold }, new Vector3(TX + s * hw1, y1 + rise1 * 0.06f, TZ + e * len1 * 0.5f), outward, 0.5f);
                MeshAt("HangHong", chofa, new[] { gold }, new Vector3(TX + s * hw2, y2 + rise2 * 0.06f, TZ + e * len2 * 0.5f), outward, 0.5f);
            }
        }
        Box("Ridge", new Vector3(TX, y1 + rise1 + 0.02f, TZ), new Vector3(0.22f, 0.16f, len1), gold);

        // กำแพงแก้วรอบลาน: พังเป็นช่วง ๆ โผล่พ้นน้ำบ้าง จมบ้าง
        void YardWall(Vector3 a, Vector3 b)
        {
            int n = Mathf.CeilToInt(Vector3.Distance(a, b) / 1.6f);
            for (int k = 0; k < n; k++)
            {
                if (rnd.NextDouble() < 0.28) continue;
                var p = Vector3.Lerp(a, b, (k + 0.5f) / n);
                float top = TW + Rf(-0.25f, 0.45f), bot = TW - 1.5f;
                var g = Box("YardWall", new Vector3(p.x, (top + bot) * 0.5f, p.z), new Vector3(0.35f, top - bot, 1.5f), brick);
                g.transform.rotation = Quaternion.LookRotation(b - a) * Quaternion.Euler(Rf(-3, 3), 0, Rf(-4, 4));
            }
        }
        float yx = 6.6f, yz0 = TZ - 9.4f, yz1 = TZ + 8.2f;
        YardWall(new Vector3(TX - yx, 0, yz0), new Vector3(TX - 1.8f, 0, yz0)); YardWall(new Vector3(TX + 1.8f, 0, yz0), new Vector3(TX + yx, 0, yz0));
        YardWall(new Vector3(TX - yx, 0, yz0), new Vector3(TX - yx, 0, yz1)); YardWall(new Vector3(TX + yx, 0, yz0), new Vector3(TX + yx, 0, yz1));

        // ใบเสมา 8 ทิศ บนแท่นโผล่พ้นน้ำ
        foreach (var (dx, dz) in new[] { (-5.4f, -8.4f), (5.4f, -8.4f), (-5.4f, 7.4f), (5.4f, 7.4f), (0f, -8.6f), (0f, 7.6f), (-5.6f, -0.4f), (5.6f, -0.4f) })
        {
            if (dz < -8.5f) continue;   // ตรงหน้าประตูเว้นไว้ให้เห็นทางเข้า
            var p = new Vector3(TX + dx, 0, TZ + dz);
            Box("SemaBase", new Vector3(p.x, TW - 0.55f, p.z), new Vector3(0.7f, 1.3f, 0.7f), brick);
            var sema = Prim(PrimitiveType.Sphere, "Sema", new Vector3(p.x, TW + 0.45f, p.z), new Vector3(0.48f, 0.82f, 0.14f), plaster);
            sema.transform.rotation = Quaternion.Euler(Rf(-6, 6), Mathf.Atan2(dx, dz) * Mathf.Rad2Deg, Rf(-8, 8)); sema.transform.SetParent(root, true);
        }

        // ---------- เจดีย์อิฐเอียงบนเกาะ ----------
        var chediBase = SaveMesh(ProceduralMeshes.Lathe(new[] { new Vector2(2.3f, -0.4f), new Vector2(2.3f, 0.5f), new Vector2(1.95f, 0.5f), new Vector2(1.95f, 1.0f), new Vector2(1.6f, 1.0f), new Vector2(1.6f, 1.45f) }, 4, Color.white, Mathf.PI * 0.25f), "ChediBase");
        var chediBell = SaveMesh(ProceduralMeshes.Lathe(new[] {
            new Vector2(1.08f, 1.45f), new Vector2(1.12f, 1.55f), new Vector2(0.98f, 1.62f), new Vector2(1.04f, 1.75f), new Vector2(0.92f, 1.82f), new Vector2(0.96f, 1.95f),
            new Vector2(1.0f, 2.2f), new Vector2(0.98f, 2.6f), new Vector2(0.86f, 3.0f), new Vector2(0.62f, 3.35f), new Vector2(0.34f, 3.55f), new Vector2(0.3f, 3.62f),
            new Vector2(0.42f, 3.66f), new Vector2(0.42f, 3.98f), new Vector2(0.3f, 4.0f), new Vector2(0.24f, 4.3f), new Vector2(0.27f, 4.36f), new Vector2(0.19f, 4.44f),
            new Vector2(0.22f, 4.5f), new Vector2(0.14f, 4.58f), new Vector2(0.17f, 4.64f), new Vector2(0.09f, 4.75f), new Vector2(0.05f, 5.4f), new Vector2(0.0f, 5.8f) }, 24, Color.white), "ChediBell");
        var chedi = new GameObject("Chedi").transform;
        chedi.SetParent(root, true);
        chedi.SetPositionAndRotation(new Vector3(ChediPos.x, TW + 0.3f, ChediPos.y), Quaternion.Euler(3.5f, 20, -4.5f));
        foreach (var (mesh, nm) in new[] { (chediBase, "Base"), (chediBell, "Bell") })
        {
            var g = new GameObject(nm); g.transform.SetParent(chedi, false); g.transform.localScale = Vector3.one * 1.25f;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = brick;
        }

        // ---------- บันไดท่าน้ำวัด (ยืนตกปลา) + ทางอิฐเก่า ----------
        foreach (var (z0, z1, top) in new[] { (-2.4f, -1.5f, 0.2f), (-1.5f, -0.6f, TW + 0.22f), (-0.6f, 0.7f, TW + 0.1f) })
        {
            Box("Step", new Vector3(0, (top + TW - 1.6f) * 0.5f, (z0 + z1) * 0.5f), new Vector3(3.0f, top - (TW - 1.6f), z1 - z0), brick);
            if (top < 0) { world.docks.Add(new Rect(-1.45f, z0, 2.9f, z1 - z0)); world.dockY.Add(top); }
        }
        foreach (var s in new[] { -1f, 1f })
        {
            Box("StepPost", new Vector3(s * 1.7f, (0.75f + TW - 1.6f) * 0.5f, -1.9f), new Vector3(0.5f, 0.75f - (TW - 1.6f), 0.5f), plaster);
            world.colliders.Add(new Vector4(s * 1.7f, -1.9f, 0.4f, 1.0f));
        }
        for (float z = -13.5f; z < -2.4f; z += 0.9f)
        {
            float x = Mathf.Sin(z * 0.25f) * 0.4f;
            var pv = Box("Paver", new Vector3(x + Rf(-0.08f, 0.08f), TempleGround(x, z) + 0.01f, z), new Vector3(Rf(1.5f, 1.8f), 0.07f, 0.8f), brick);
            pv.transform.rotation = Quaternion.Euler(Rf(-2, 2), Rf(-6, 6), Rf(-2, 2));
            pv.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- ธรรมชาติ ----------
        var natureRoot = new GameObject("Nature").transform;
        GameObject Nature(string file, float x, float z, float scale, float yaw, float collide = 0, bool shadows = true, float sink = 0.04f, float height = 99, float? y = null)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/{file}.glb");
            if (src == null) { Debug.LogWarning("[SceneBuilder] missing " + file); return null; }
            var go = Object.Instantiate(src, natureRoot);
            go.name = file;
            go.transform.SetPositionAndRotation(new Vector3(x, (y ?? TempleGround(x, z)) - sink, z), Quaternion.Euler(0, yaw, 0));
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
        // ต้นไม้ใหญ่โอบโบสถ์ · รากไม้ริมตลิ่ง · ขอนไม้ตายจมน้ำ
        foreach (var (x, z, s) in new[] { (-6.5f, 26f, 1.7f), (14f, 27f, 1.6f), (-14.5f, 11f, 1.5f), (15.5f, 9f, 1.45f), (-13f, 28f, 1.8f), (4f, 30f, 1.7f), (17f, 20f, 1.6f), (-17f, 1f, 1.4f), (13f, -3.5f, 1.3f) })
            Nature("island_tree_01", x, z, s, Rf(0, 360), 0.9f, true, 0.15f);
        foreach (var (x, z, yaw) in new[] { (-11.5f, 3.5f, 40f), (12.5f, 2.5f, -30f), (-4f, 23.5f, 160f), (-10.5f, 19f, 100f), (-7.6f, 17.6f, 210f) })
            Nature("root_cluster_01", x, z, Rf(0.8f, 1.1f), yaw, 0, true, 0.35f);
        Nature("dead_tree_trunk", -6.5f, 8.5f, 1.5f, 32, 0, true, 0, 99, TW - 0.08f);
        Nature("dead_tree_trunk", 8.2f, 11.5f, 1.3f, -55, 0, true, 0, 99, TW - 0.12f);
        var trunk = Nature("dead_tree_trunk", -2.6f, 13.2f, 1.2f, 75, 0, true, 0, 99, TW - 0.2f);
        if (trunk != null) trunk.transform.rotation = Quaternion.Euler(0, 75, 12);
        // หินและเศษอิฐพังตรงมุมโบสถ์
        for (int i = 0; i < 6; i++) Nature(i % 2 == 0 ? "stone_01" : "namaqualand_boulder_02", TX + Rf(3.6f, 5.6f), TZ + Rf(-7.5f, -4.5f), Rf(0.25f, 0.5f), Rf(0, 360), 0, true, 0, 99, TW - Rf(0.05f, 0.3f));
        foreach (var (x, z, s) in new[] { (-10f, -2f, 0.55f), (8.5f, -1.5f, 0.5f), (-15f, 6f, 0.6f), (14f, 5f, 0.5f) })
            Nature("rock_moss_set_01", x, z, s, Rf(0, 360), 0.9f, true, 0.15f, 0.8f);
        for (int i = 0; i < 26; i++)
        {
            float x = Rf(-17, 17), z = Rf(-12, 30);
            float h = TempleGround(x, z);
            if (h < TW + 0.08f || Mathf.Abs(x) < 2.2f && z < 0 || Mathf.Abs(x - world.spawn.x) < 1.5f && Mathf.Abs(z - world.spawn.z) < 1.5f) continue;
            Nature(new[] { "fern_02", "shrub_02", "shrub_03", "weed_plant_02" }[i % 4], x, z, Rf(0.6f, 1.0f), Rf(0, 360), i % 4 == 1 ? 0.6f : 0, i % 4 != 3, 0.03f);
        }

        // บัวในวังน้ำตื้นริมขอบ
        var pad = SurfMat("LotusPad", Color.white, vertexColor: true, smooth: 0.4f);
        var lotus = SurfMat("Lotus", Color.white, vertexColor: true, smooth: 0.25f, cull: CullMode.Off);
        var padMeshes = new[] { SaveMesh(ProceduralMeshes.LotusPad(1), "LotusPad1"), SaveMesh(ProceduralMeshes.LotusPad(2), "LotusPad2"), SaveMesh(ProceduralMeshes.LotusPad(3), "LotusPad3") };
        var flowerMesh = SaveMesh(ProceduralMeshes.LotusFlower(4, false), "LotusFlower");
        int pads = 0;
        for (int tries = 0; tries < 1500 && pads < 110; tries++)
        {
            float x = Rf(-13, 14), z = Rf(-1, 24), d = TW - TempleGround(x, z);
            if (d < 0.1f || d > 1.3f || x * x + z * z < 9 || Mathf.Abs(x - TX) < 3.6f && z > TZ - 9 && z < TZ + 7) continue;
            var g = new GameObject("LotusPad");
            g.transform.SetPositionAndRotation(new Vector3(x, TW + 0.02f, z), Quaternion.Euler(0, Rf(0, 360), 0)); g.transform.localScale = Vector3.one * Rf(0.35f, 0.6f);
            g.AddComponent<MeshFilter>().sharedMesh = padMeshes[rnd.Next(3)];
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = pad; r.shadowCastingMode = ShadowCastingMode.Off;
            if (rnd.NextDouble() < 0.18)
            {
                var f = new GameObject("Lotus");
                f.transform.SetPositionAndRotation(new Vector3(x + 0.2f, TW + Rf(0.05f, 0.25f), z), Quaternion.Euler(0, Rf(0, 360), 0)); f.transform.localScale = Vector3.one * Rf(0.22f, 0.3f);
                f.AddComponent<MeshFilter>().sharedMesh = flowerMesh; f.AddComponent<MeshRenderer>().sharedMaterial = lotus;
            }
            pads++;
        }

        // ตะเกียงทองเหลืองบนเสาบันได + หน้าประตูโบสถ์ (แสงอุ่นตอนค่ำ)
        foreach (var p in new[] { new Vector3(-1.7f, 0.75f, -1.9f), new Vector3(1.7f, 0.75f, -1.9f), new Vector3(TX - 1.6f, colTop + 0.02f, TZ - 7.2f) })
        {
            Nature("brass_diya_lantern", p.x, p.z, 1.6f, Rf(0, 360), 0, false, 0, 99, p.y);
            var lg = new GameObject("LanternLight").AddComponent<Light>();
            lg.type = LightType.Point; lg.range = 3.5f; lg.intensity = 0.9f; lg.color = Hex("#ffb060"); lg.shadows = LightShadows.None;
            lg.transform.position = p + Vector3.up * 0.4f;
        }

        // เทียนบนหัวเสาบันไดท่าน้ำสองต้น (แสงอุ่นกลางความมืด)
        var flame = UnlitMat("CandleFlame", new Color(2.4f, 1.4f, 0.5f));
        foreach (var s in new[] { -1f, 1f })
        {
            var cd = Prim(PrimitiveType.Cylinder, "Candle", new Vector3(s * 1.7f + 0.15f, 0.82f, -1.75f), new Vector3(0.05f, 0.07f, 0.05f), SurfMat("CandleWax", Hex("#f0e8d0"), 0f, smooth: 0.4f)); cd.transform.SetParent(root, true);
            var fl = Prim(PrimitiveType.Sphere, "Flame", new Vector3(s * 1.7f + 0.15f, 0.93f, -1.75f), new Vector3(0.035f, 0.06f, 0.035f), flame); fl.transform.SetParent(root, true); fl.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var cl = new GameObject("CandleLight").AddComponent<Light>(); cl.type = LightType.Point; cl.range = 3.5f; cl.intensity = 1.4f; cl.color = Hex("#ffb060"); cl.shadows = LightShadows.None; cl.transform.position = new Vector3(s * 1.7f + 0.15f, 1.1f, -1.75f);
        }
        // ขอนไม้ลอย · ซากกำแพงอิฐพังจมน้ำ
        var logMat = SurfMat("TempleDriftLog", Hex("#4a3e30"), 0.3f, smooth: 0.25f, tex: "bark_brown_02", tiles: 1f);
        for (int i = 0; i < 10; i++)
        {
            float x = Rf(-11, 12), z = Rf(2, 20);
            if (Mathf.Abs(x - TX) < 3.8f && z > TZ - 9 && z < TZ + 7 || TW - TempleGround(x, z) < 0.25f) continue;
            var lg2 = Prim(PrimitiveType.Cylinder, "DriftLog", new Vector3(x, TW + 0.03f, z), new Vector3(Rf(0.2f, 0.35f), Rf(0.8f, 1.6f), Rf(0.2f, 0.35f)), logMat);
            lg2.transform.rotation = Quaternion.Euler(90 + Rf(-6, 6), Rf(0, 360), 0); lg2.transform.SetParent(root, true);
        }
        foreach (var (x, z, yaw) in new[] { (-8.5f, 14f, 20f), (-10f, 9f, -40f), (9f, 18f, 60f) })
            for (int k = 0; k < 4; k++)
            {
                var wb = Box("RuinWall", new Vector3(x + k * 0.9f * Mathf.Cos(yaw * Mathf.Deg2Rad), TW + Rf(0.1f, 0.9f) - 0.3f, z + k * 0.9f * Mathf.Sin(yaw * Mathf.Deg2Rad)), new Vector3(0.9f, Rf(0.8f, 1.8f), 0.4f), brick);
                wb.transform.rotation = Quaternion.Euler(Rf(-8, 8), -yaw, Rf(-10, 10));
            }
        // ดวงตาแดงเรืองในความมืดรอบวัด (ออกเฉพาะกลางคืน)
        var eyeMat = ParticleMat("GhostEyes", new Color(2.2f, 0.25f, 0.15f, 1));
        foreach (var (ex, ez) in new[] { (-12f, 16f), (13f, 18f), (-9f, 25f), (8f, 27f), (15f, 8f) })
        {
            var eyes = Particles("GhostEyes", new Vector3(ex, TempleGround(ex, ez) + 1.2f, ez), new Vector3(1.5f, 0.6f, 0.3f), eyeMat, 0.4f, new Vector2(0.06f, 0.09f), new Vector2(4f, 7f), new Vector2(0f, 0f), new Color(1, 0.3f, 0.2f, 1));
            var sz = eyes.sizeOverLifetime; sz.enabled = false;
            eyes.gameObject.AddComponent<DuskOnly>().from = 19.5f; eyes.gameObject.GetComponent<DuskOnly>().to = 5f;
        }

        // หมอกบาง ๆ ลอยเหนือวังน้ำ
        var mist = ParticleMat("Spray", new Color(1, 1, 1, 0.5f));
        Particles("PoolMist", new Vector3(TX, TW + 0.4f, 10f), new Vector3(28f, 22f, 1f), mist, 28, new Vector2(4f, 7f), new Vector2(7f, 10f), new Vector2(0.04f, 0.15f), new Color(0.92f, 0.94f, 0.92f, 0.13f));

        // ---------- ตัวละคร กล้อง แสง ระบบเกม ----------
        var look = new Look { sun = "#f0dcc0", sunIntensity = 1.4f, sunRot = new Vector2(26, 125), fog = "#98a29a", fogStart = 8, fogEnd = 58, exposure = 0.95f, ambient = 0.95f, skyId = "Overcast", skyTint = "#8e9894" };   // ครึ้มหมอก วัดร้างวังเวง
        world.spawn.y = TempleGround(world.spawn.x, world.spawn.z);
        BuildGameplay(world, model, world.spawn, look, world);
        SaveStage(scene, path);
    }
}
