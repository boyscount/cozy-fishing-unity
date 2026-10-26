// ด่านบอส: แก่งอาถรรพ์พญานาค — แม่น้ำโขงสีน้ำตาลแดงยามเช้ามืด กว้างสุดตา แก่งหินน้ำเชี่ยวช่วงต้นน้ำ ภูเขาฝั่งลาวสูงตระหง่านฝั่งตรงข้าม
//      ผู้เล่นยืนบนลานหินแบนที่ยื่นลงแม่น้ำ · ศาลริมแก่งบนเสาผูกผ้าสามสี ธูปเทียน พวงมาลัย · รูปสลักพญานาคเลื้อยบนโขดหินใหญ่
//      ลูกไฟพญานาคลอยขึ้นจากน้ำ (MekongFx) · ปลาบึกยักษ์ตัวเดียวต่อสัปดาห์ (Boss)
using System.IO;
using CozyFishing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class SceneBuilder
{
    const float KW = -0.3f;
    static float KZc(float x) => 20f + 2.5f * Mathf.Sin(x * 0.045f + 0.4f);
    const float KHw = 17f;

    static float MekongGround(float x, float z)
    {
        float zc = KZc(x), d = z - zc, a = Mathf.Abs(d) / KHw;
        float n = Mathf.PerlinNoise(x * 0.06f + 3, z * 0.06f + 7) - 0.5f, n2 = Mathf.PerlinNoise(x * 0.3f + 11, z * 0.3f + 5) - 0.5f;
        float bankNear = 0.25f + n * 0.6f + n2 * 0.15f + Mathf.Max(0, -d - KHw - 3) * 0.12f;           // ตลิ่งทรายปนหินฝั่งใกล้ ค่อย ๆ ลาดขึ้น
        float up = Mathf.Max(0, d - KHw);
        float bankFar = 0.4f + up * 0.25f + Mathf.Max(0, up - 14) * 1.3f + n * 8f * SStep(10, 30, up) + n2 * 1.2f;   // ริมตลิ่งลาดก่อน แล้วค่อยเป็นภูเขาไกล ๆ   // ภูเขาฝั่งลาวสูงชัน
        bankFar = Mathf.Min(bankFar, 60f + n * 20);
        float bank = d < 0 ? bankNear : bankFar;
        float depth = 1.6f + 3.4f * (1 - a * a);
        float rapid = SStep(-30f, -24f, x) * SStep(-1f, -5f, x);                                      // ช่วงแก่ง: ตื้นมีหินโผล่
        depth = Mathf.Lerp(depth, 0.55f + n2 * 0.4f, rapid * 0.85f);
        return Mathf.Lerp(KW - depth + n2 * 0.2f, bank, SStep(0.9f, 1.18f, a));
    }

    public static void CreateMekong()
    {
        const string path = "Assets/Game/Scenes/Mekong.unity";
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
        world.stageId = "mekong"; world.waterY = KW;
        world.origin = new Vector2(-60, -25); world.cell = 0.5f; world.nx = 241; world.nz = 201;
        world.heights = new float[world.nx * world.nz];
        for (int j = 0; j < world.nz; j++) for (int i = 0; i < world.nx; i++) world.heights[j * world.nx + i] = MekongGround(world.origin.x + i * world.cell, world.origin.y + j * world.cell);
        world.walkBounds = new Rect(-14, -10, 28, 16);
        float nearEdge(float x) => KZc(x) - KHw * 1.0f;
        float sx = 2f, sz = nearEdge(sx) + 2.4f;
        world.spawn = new Vector3(sx, 0, sz); world.spawnYaw = 0;
        world.special = new Vector3(-4f, KW, KZc(-4f) - 4f); world.specialRadius = 4f;               // วังน้ำวนท้ายแก่ง
        world.camBounds = new Rect(-14, -8, 28, 22); world.camBias = new Vector2(0, KZc(0) - 6); world.camBiasK = 0.3f;

        // ---------- พื้นดิน: ทรายริมโขง / หินแก่ง / ภูเขาป่า ----------
        var terrain = new GameObject("Terrain");
        var m = Grid(220, 150, 220, new Vector3(0, 0, 40));
        var v = m.vertices; var cols = new Color[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            float x = v[i].x, z = v[i].z, h = MekongGround(x, z);
            v[i].y = h;
            float slope = Mathf.Abs(MekongGround(x + 0.6f, z) - h) + Mathf.Abs(MekongGround(x, z + 0.6f) - h);
            float sand = SStep(KW + 1.4f, KW + 0.05f, h) * (z < KZc(x) ? 1 : 0.4f);
            float rock = Mathf.Max(SStep(0.6f, 1.4f, slope), SStep(KW + 0.1f, KW - 0.8f, h) * 0.5f);
            cols[i] = new Color(sand, rock, SStep(KW + 0.05f, KW - 1.2f, h), 0);
        }
        m.vertices = v; m.colors = cols; m.RecalculateNormals(); m.RecalculateBounds();
        terrain.AddComponent<MeshFilter>().sharedMesh = SaveMesh(m, "MekongTerrain");
        var tr = terrain.AddComponent<MeshRenderer>();
        tr.sharedMaterial = TerrainMat("Mekong", ("leafy_grass", "brown_mud_02", "rock_face_04"), new Vector4(0.25f, 0.45f, 0.12f, 0.025f), "#8a9a6a", "#5a6c5c");
        tr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- แม่น้ำโขงสีน้ำตาลแดง ไหลซ้าย→ขวา ----------
        var water = new GameObject("MekongWater");
        water.AddComponent<MeshFilter>().sharedMesh = SaveMesh(Grid(200, 60, 170, new Vector3(0, KW, 20)), "MekongWater");
        var wm = LoadOrCreate($"{MatDir}/Water_Mekong.mat", waterShader);
        wm.SetColor("_Shallow", Hex("#b0925a")); wm.SetColor("_Deep", Hex("#4e3c1e"));   // น้ำโขงสีน้ำตาลอมเหลือง (ตะกอนดิน)
        wm.SetFloat("_DepthScale", 1.2f); wm.SetFloat("_Clarity", 0.12f); wm.SetFloat("_Glint", 1.2f); wm.SetFloat("_Refraction", 0.03f);
        wm.SetFloat("_Reflect", 0.65f); wm.SetFloat("_Waves", 0.04f); wm.SetFloat("_FoamWidth", 0.18f); wm.SetVector("_Flow", new Vector4(0.6f, 0.03f, 0, 0));
        wm.SetVector("_RapidZone", new Vector4(-30f, -1.5f, 1f, 0));   // ฟองขาวเป็นริ้วตลอดช่วงแก่ง
        var wr = water.AddComponent<MeshRenderer>(); wr.sharedMaterial = wm; wr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- ธรรมชาติ + หินแก่ง ----------
        var natureRoot = new GameObject("Nature").transform;
        GameObject Nature(string file, float x, float z, float scale, float yaw, float collide = 0, bool shadows = true, float sink = 0.04f, float? y = null)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/{file}.glb");
            if (src == null) return null;
            var go = Object.Instantiate(src, natureRoot); go.name = file;
            go.transform.SetPositionAndRotation(new Vector3(x, (y ?? MekongGround(x, z)) - sink, z), Quaternion.Euler(0, yaw, 0)); go.transform.localScale = Vector3.one * scale;
            foreach (var rr in go.GetComponentsInChildren<Renderer>())
            {
                rr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                var mats = rr.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = LeafMat(file, mats[i]) ?? mats[i]; rr.sharedMaterials = mats;
            }
            if (collide > 0) world.colliders.Add(new Vector4(x, z, collide, 99));
            return go;
        }
        var spray0 = ParticleMat("Spray", new Color(1, 1, 1, 0.5f));
        for (int i = 0; i < 34; i++)   // หินโผล่กลางแก่ง + ละอองน้ำแตกรอบหินก้อนใหญ่
        {
            float x = Rf(-30f, -3f), z = KZc(x) + Rf(-0.85f, 0.85f) * KHw, sc = Rf(0.6f, 1.6f);
            Nature(i % 3 == 0 ? "namaqualand_boulder_02" : "boulder_01", x, z, sc, Rf(0, 360), 0, true, 0.3f, KW - 0.3f);
            if (sc > 1.1f && i % 2 == 0) Particles("RockSplash", new Vector3(x - 0.8f, KW + 0.1f, z), new Vector3(1.2f, 1.6f, 0.1f), spray0, 14, new Vector2(0.3f, 0.7f), new Vector2(0.6f, 1.1f), new Vector2(0.4f, 1.1f), new Color(1, 0.97f, 0.92f, 0.4f), 0.4f);
        }
        // ลานหินแบนที่ผู้เล่นยืน (ยื่นลงน้ำสามก้อน)
        for (int k = 0; k < 3; k++)
        {
            float x = sx - 1.2f + k * 1.3f, z = nearEdge(x) + 0.9f + (k == 1 ? 1.5f : 0.6f);
            var st = Nature("namaqualand_boulder_02", x, z, 1, Rf(0, 360), 0, true, 0, KW - 0.1f);
            if (st != null) st.transform.localScale = new Vector3(1.05f, 0.45f, 1.05f);
            world.docks.Add(new Rect(x - 0.95f, z - 0.95f, 1.9f, 1.9f)); world.dockY.Add(KW + 0.35f);
        }
        world.spawn = new Vector3(sx + 0.1f, KW + 0.35f, nearEdge(sx + 0.1f) + 2.4f);
        foreach (var (x, side, s) in new[] { (-14f, -1f, 0.6f), (12f, -1f, 0.55f), (-6f, -1f, 0.45f), (18f, -1f, 0.5f) })
            Nature("rock_moss_set_01", x, nearEdge(x) - 0.6f, s, Rf(0, 360), 1.2f, true, 0.15f);
        // ฝั่งไทย: ต้นไม้ใหญ่ กอไผ่ ตาลโตนด พุ่มไม้ · ฝั่งลาว: ป่าบนภูเขา
        foreach (var (x, off) in new[] { (-16f, -6f), (-9f, -8.5f), (9f, -7f), (16f, -9f), (-2f, -10f) }) Nature("island_tree_01", x, nearEdge(x) + off, Rf(1.3f, 1.8f), Rf(0, 360), 0.9f, true, 0.15f);
        var palmTrunk = SurfMat("SugarPalmTrunk", Hex("#b8a890"), tex: "bark_brown_02", tiles: 0.9f, cull: CullMode.Off);
        var palmLeaf = SurfMat("SugarPalmLeaf", Color.white, sway: 0.05f, swayStart: 6f, vertexColor: true, smooth: 0.35f, cull: CullMode.Off);
        int pi = 0;
        foreach (var (x, off, h) in new[] { (-12f, -4.5f, 9f), (7f, -5f, 10f), (14f, -4f, 8.5f) })
        {
            var go = new GameObject("SugarPalm"); go.transform.SetParent(natureRoot, true);
            float z = nearEdge(x) + off; go.transform.SetPositionAndRotation(new Vector3(x, MekongGround(x, z) - 0.1f, z), Quaternion.Euler(0, Rf(0, 360), 0));
            go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.SugarPalm(551 + pi, h), "MKPalm" + pi++);
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { palmTrunk, palmLeaf }; world.colliders.Add(new Vector4(x, z, 0.5f, 99));
        }
        var pineTrunk = SurfMat("BroadTrunk", Color.white, vertexColor: true, instancing: true, smooth: 0.2f);
        var pineLeaf = SurfMat("BroadLeaf", Color.white, sway: 0.03f, swayStart: 3f, vertexColor: true, instancing: true, smooth: 0.3f);
        var farTrees = new Mesh[4]; for (int i = 0; i < 4; i++) farTrees[i] = SaveMesh(ProceduralMeshes.Broadleaf(560 + i, 8f + i * 1.8f), "MKFarTree" + i);
        for (int i = 0, n = 0; i < 3000 && n < 230; i++)
        {
            float x = Rf(-80, 80), z = Rf(KZc(0) + KHw + 2, 110), g = MekongGround(x, z);
            if (g < KW + 1 || Mathf.PerlinNoise(x * 0.08f, z * 0.08f) < 0.4f) continue;
            var go = new GameObject("FarTree"); go.transform.SetParent(natureRoot, true);
            go.transform.SetPositionAndRotation(new Vector3(x, g - 0.2f, z), Quaternion.Euler(0, Rf(0, 360), 0)); go.transform.localScale = Vector3.one * Rf(0.9f, 1.4f);
            go.AddComponent<MeshFilter>().sharedMesh = farTrees[rnd.Next(4)]; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { pineTrunk, pineLeaf }; mr.shadowCastingMode = ShadowCastingMode.Off;
            n++;
        }
        var bambooCulm = SurfMat("BambooCulm", Color.white, vertexColor: true, smooth: 0.45f);
        var bambooLeaf = SurfMat("BambooLeaf", Color.white, sway: 0.06f, swayStart: 3f, vertexColor: true, smooth: 0.3f, cull: CullMode.Off);
        int bmi = 0;
        foreach (var (x, off, far) in new[] { (-18f, -3.5f, false), (-11f, -5f, false), (11f, -4f, false), (19f, -3f, false), (-24f, 3f, true), (-6f, 4f, true), (9f, 3.5f, true), (24f, 5f, true) })
        {
            float z = far ? KZc(x) + KHw + off : nearEdge(x) + off;
            var go = new GameObject("Bamboo"); go.transform.SetParent(natureRoot, true);
            go.transform.SetPositionAndRotation(new Vector3(x, MekongGround(x, z) - 0.1f, z), Quaternion.Euler(0, Rf(0, 360), 0));
            go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.Bamboo(590 + bmi, Rf(7f, 10f)), "MKBamboo" + bmi++);
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { bambooCulm, bambooLeaf };
            if (!far) world.colliders.Add(new Vector4(x, z, 0.9f, 99));
        }
        for (int i = 0; i < 24; i++)
        {
            float x = Rf(-18, 18), z = nearEdge(x) - Rf(1.5f, 8f);
            if (Mathf.Abs(x - sx) < 2f && z > nearEdge(x) - 3) continue;
            Nature(new[] { "shrub_02", "fern_02", "weed_plant_02", "shrub_04" }[i % 4], x, z, Rf(0.6f, 1.1f), Rf(0, 360), i % 4 == 0 ? 0.6f : 0, i % 4 != 2, 0.03f);
        }

        // ---------- ศาลริมแก่ง + รูปสลักพญานาค ----------
        var shrineRoot = new GameObject("Shrine").transform;
        var plaster = SurfMat("TemplePlaster", Hex("#f4ecd8"), 0.15f, smooth: 0.08f, tex: "worn_mossy_plasterwall", tiles: 0.3f);
        var redLac = SurfMat("MKRedLacquer", Hex("#a8281e"), 0.05f, smooth: 0.45f);
        var gold = SurfMat("ResGold", Hex("#c9a24a"), 0f, smooth: 0.75f);
        var wood = SurfMat("HBPlankDark", Hex("#9a8268"), 0.05f, vertexColor: true, smooth: 0.1f, tex: "weathered_brown_planks", tiles: 0.7f);
        var ragMat = SurfMat("Ribbon", Color.white, sway: 0.12f, swayStart: 0f, vertexColor: true, smooth: 0.5f, cull: CullMode.Off);
        var stoneMat = SurfMat("MKNagaGold", Hex("#e8c068"), 0f, smooth: 0.35f);   // พญานาคปูนปั้นลงรักปิดทอง (เก่า ทองหม่น)
        float shx = sx - 4.2f, shz = nearEdge(shx) - 1.4f, shy = MekongGround(shx, shz);
        GameObject P(PrimitiveType t, string n, Vector3 p, Vector3 s, Material mt, Vector3? rot = null) { var g = Prim(t, n, p, s, mt); if (rot != null) g.transform.rotation = Quaternion.Euler(rot.Value); g.transform.SetParent(shrineRoot, true); return g; }
        P(PrimitiveType.Cylinder, "ShrinePost", new Vector3(shx, shy + 0.7f, shz), new Vector3(0.18f, 0.7f, 0.18f), redLac);
        P(PrimitiveType.Cube, "ShrineBase", new Vector3(shx, shy + 1.45f, shz), new Vector3(0.9f, 0.08f, 0.8f), wood);
        P(PrimitiveType.Cube, "ShrineHouse", new Vector3(shx, shy + 1.75f, shz), new Vector3(0.6f, 0.55f, 0.5f), plaster);
        var roof = new GameObject("ShrineRoof"); roof.transform.SetParent(shrineRoot, false); roof.transform.position = new Vector3(shx, shy + 2.02f, shz);
        roof.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.ThaiRoof(0.85f, 0.45f, 0.4f, true), "MKShrineRoof");
        roof.AddComponent<MeshRenderer>().sharedMaterials = new[] { redLac, gold };
        for (int k = 0; k < 5; k++)   // ธูปปักกระถาง ปลายแดงเรือง
        {
            P(PrimitiveType.Cylinder, "Incense", new Vector3(shx - 0.15f + k * 0.07f, shy + 1.62f, shz - 0.28f), new Vector3(0.012f, 0.16f, 0.012f), wood);
            P(PrimitiveType.Sphere, "IncenseTip", new Vector3(shx - 0.15f + k * 0.07f, shy + 1.79f, shz - 0.28f), Vector3.one * 0.022f, UnlitMat("IncenseTip", new Color(1f, 0.3f, 0.1f) * 2f));
        }
        var garland = new GameObject("Garland"); garland.transform.SetParent(shrineRoot, false); garland.transform.position = new Vector3(shx, shy + 1.5f, shz - 0.34f); garland.transform.rotation = Quaternion.Euler(90, 0, 0);
        garland.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.Garland(571, 0.16f, 14), "MKGarland"); garland.AddComponent<MeshRenderer>().sharedMaterial = SurfMat("Garland", Color.white, vertexColor: true, smooth: 0.3f);
        var cloth = new[] { Hex("#c02a2a"), Hex("#e8c030"), Hex("#2a8a3a") };
        for (int k = 0; k < 6; k++)
        {
            var rib = new GameObject("TriColorCloth"); rib.transform.SetParent(shrineRoot, true);
            rib.transform.SetPositionAndRotation(new Vector3(shx + Rf(-0.08f, 0.08f), shy + 1.2f - k * 0.04f, shz + Rf(-0.08f, 0.08f)), Quaternion.Euler(180, Rf(0, 360), 0)); rib.transform.localScale = Vector3.one * Rf(2.4f, 3.2f);
            rib.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.Ribbon(580 + k, cloth[k % 3], Rf(0.5f, 0.8f)), "MKCloth" + k); rib.AddComponent<MeshRenderer>().sharedMaterial = ragMat;
        }
        world.colliders.Add(new Vector4(shx, shz, 0.6f, 99));
        // พญานาคเลื้อยบนโขดหินใหญ่ริมแก่ง: ลำตัวเป็นลูกกลมเรียงโค้ง หัวชูหงอนสามแฉก
        float nx0 = sx - 8.5f, nz0 = nearEdge(nx0) + 0.4f;
        Nature("boulder_01", nx0, nz0, 2.6f, 40, 2.2f, true, 0.6f, KW + 0.1f);
        var nagaMat = SurfMat("MKNaga2", Color.white, 0.06f, vertexColor: true, smooth: 0.42f, cull: CullMode.Off);   // ทองเก่า มีคราบตะไคร่จาง ๆ   // ทองเก่าหม่น มีผิวปูนและคราบตะไคร่จาง ๆ   // ปูนปั้นลงรักปิดทอง
        GameObject NagaAt(string name, Vector3[] pts, float tailR, float neckR, int heads)
        {
            var g = new GameObject(name); g.transform.SetParent(shrineRoot, true);
            g.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.Naga(pts, tailR, neckR, heads), "MK" + name);
            g.AddComponent<MeshRenderer>().sharedMaterial = nagaMat; return g;
        }
        // พญานาคใหญ่เลื้อยพันโขดหิน หางจุ่มน้ำ ชูพังพานห้าเศียรหันหน้าลงแม่น้ำ
        {
            var bigPath = new Vector3[40];
            for (int k = 0; k < 40; k++)
            {
                float t = k / 39f, ang = t * Mathf.PI * 1.6f;
                bigPath[k] = new Vector3(nx0 + Mathf.Cos(ang + 2.4f) * 2.3f * (1 - t * 0.35f), KW + 0.2f + t * 2.6f + Mathf.Sin(t * Mathf.PI) * 0.6f, nz0 + Mathf.Sin(ang + 2.4f) * 1.9f * (1 - t * 0.35f));
            }
            bigPath[39] = bigPath[38] + new Vector3(0.05f, 0.1f, 0.35f);   // คอหันไปทางแม่น้ำ (+z)
            NagaAt("NagaBig", bigPath, 0.1f, 0.42f, 5);
        }
        // บันไดนาคลงท่าน้ำ: ขั้นปูนจากริมตลิ่งลงถึงน้ำ ราวสองข้างเป็นลำตัวนาค หัวนาคห้าเศียรชูที่ตีนบันได
        {
            float bx = sx - 6.4f, bz0 = nearEdge(bx) - 4.2f, bz1 = nearEdge(bx) + 0.6f, top = MekongGround(bx, bz0);
            int steps = 9;
            for (int k = 0; k < steps; k++)
            {
                float t = (k + 0.5f) / steps, z = Mathf.Lerp(bz0, bz1, t), y = Mathf.Lerp(top, KW - 0.1f, t);
                P(PrimitiveType.Cube, "Step", new Vector3(bx, y - 0.25f, z), new Vector3(1.6f, 0.5f, (bz1 - bz0) / steps + 0.02f), plaster);
            }
            foreach (var sd in new[] { -1f, 1f })
            {
                var rail = new Vector3[18];
                for (int k = 0; k < 18; k++) { float t = k / 17f; rail[k] = new Vector3(bx + sd * 0.95f, Mathf.Lerp(top + 0.55f, KW + 0.45f, t), Mathf.Lerp(bz0 - 0.4f, bz1 - 0.2f, t)); }
                NagaAt(sd < 0 ? "NagaStairL" : "NagaStairR", rail, 0.12f, 0.24f, 5);
                world.colliders.Add(new Vector4(bx + sd * 0.95f, bz1 - 0.3f, 0.5f, 99));
            }
        }
        // เรือหางยาวผูกริมตลิ่งข้างบันได + อีกลำลอยไกลกลางแม่น้ำ
        var boatMat = SurfMat("MKLongtail", Color.white, 0.05f, vertexColor: true, smooth: 0.4f, cull: CullMode.Off);
        foreach (var (bx2, bz2, yaw) in new[] { (sx - 8.6f, nearEdge(sx - 8.6f) + 0.8f, 12f), (14f, KZc(14f) + 4f, -70f) })
        {
            var b = new GameObject("Longtail"); b.transform.SetParent(shrineRoot, true); b.transform.SetPositionAndRotation(new Vector3(bx2, KW + 0.05f, bz2), Quaternion.Euler(0, yaw, 0));
            b.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.LongtailBoat(7f), "MKLongtailMesh"); b.AddComponent<MeshRenderer>().sharedMaterial = boatMat;
        }
        CombineGroup(shrineRoot, "MKShrine");

        // ---------- หมอกเช้ามืดเรี่ยแม่น้ำ + ละอองแก่ง ----------
        var spray = ParticleMat("Spray", new Color(1, 1, 1, 0.5f));
        Particles("RapidsSpray", new Vector3(-17f, KW + 0.05f, KZc(-17f)), new Vector3(18f, KHw * 1.6f, 0.1f), spray, 40, new Vector2(0.5f, 1.2f), new Vector2(1.2f, 2f), new Vector2(0.3f, 0.9f), new Color(1, 0.95f, 0.9f, 0.32f));
        Particles("RiverMist", new Vector3(0, KW + 0.6f, KZc(0)), new Vector3(90f, KHw * 1.8f, 0.1f), spray, 10, new Vector2(5f, 9f), new Vector2(8f, 12f), new Vector2(0.05f, 0.2f), new Color(1f, 0.92f, 0.85f, 0.07f));
        var mm = Particles("MountainMist", new Vector3(0, 14f, KZc(0) + KHw + 18f), new Vector3(120f, 10f, 6f), spray, 8, new Vector2(10f, 16f), new Vector2(10f, 16f), new Vector2(0.1f, 0.3f), new Color(1f, 0.9f, 0.85f, 0.18f));

        // ---------- ตัวละคร กล้อง แสง ระบบเกม ----------
        var look = new Look { sun = "#ffc89a", sunIntensity = 1.6f, sunRot = new Vector2(14, 110), fog = "#c8a890", fogStart = 30, fogEnd = 170, exposure = 0.95f, ambient = 1.0f };   // เช้ามืดแสงส้มต่ำ หมอกอมชมพู
        world.spawn.y = KW + 0.35f;
        BuildGameplay(world, model, world.spawn, look, world);
        SaveStage(scene, path);
    }

    public static void MekongCover()
    {
        EditorSceneManager.OpenScene("Assets/Game/Scenes/Mekong.unity");
        var go = new GameObject("CoverCam"); var cam = go.AddComponent<Camera>();
        go.transform.position = new Vector3(-9.2f, 3.0f, -0.2f); go.transform.LookAt(new Vector3(-1.5f, 1.9f, 18f));   // จากริมตลิ่งหลังพญานาค มองข้ามแก่งไปภูเขาฝั่งลาว   // จากกลางแม่น้ำมองเข้าฝั่ง: พญานาค บันไดนาค ศาล ป่าริมโขง   // เห็นพญานาค บันไดนาค ศาล และแก่งน้ำเชี่ยวในภาพเดียว
        cam.fieldOfView = 55; cam.nearClipPlane = 0.2f; cam.farClipPlane = 300; cam.clearFlags = CameraClearFlags.Skybox;
        go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (c != cam) c.enabled = false;
        var hud = Object.FindFirstObjectByType<Hud>(); if (hud != null) hud.gameObject.SetActive(false);
        const int W = 1024, H = 576;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt; cam.Render(); cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
        const string outPath = "Assets/Game/Resources/UI/Stages/mekong.jpg";
        File.WriteAllBytes(outPath, tex.EncodeToJPG(88)); AssetDatabase.ImportAsset(outPath);
        if (AssetImporter.GetAtPath(outPath) is TextureImporter ti)
        {
            ti.mipmapEnabled = false; var ps = ti.GetPlatformTextureSettings("WebGL"); ps.overridden = true; ps.maxTextureSize = 512; ps.format = TextureImporterFormat.ASTC_6x6;
            ti.SetPlatformTextureSettings(ps); ti.SaveAndReimport();
        }
        Debug.Log("[SceneBuilder] cover saved " + outPath);
    }
}
