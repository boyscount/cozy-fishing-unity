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
    const float KHw = 24f;   // แม่น้ำกว้าง ฝั่งลาวอยู่ไกลในหมอก

    /// แหลมหินยื่นลงแม่น้ำทางซ้ายมือผู้เล่น (ที่ตั้งพญานาค ศาล กระท่อม — อยู่ในมุมกล้องตกปลา)
    static float PointOut(float x) => SStep(-20f, -15f, x) * SStep(-6f, -10f, x) * 7f;
    static float MekongGround(float x, float z)
    {
        float zc = KZc(x), d = z - zc; if (d < 0) d -= PointOut(x); float a = Mathf.Abs(d) / KHw;
        float n = Mathf.PerlinNoise(x * 0.06f + 3, z * 0.06f + 7) - 0.5f, n2 = Mathf.PerlinNoise(x * 0.3f + 11, z * 0.3f + 5) - 0.5f;
        float bankNear = 0.25f + n * 0.6f + n2 * 0.15f + Mathf.Max(0, -d - KHw - 3) * 0.12f;           // ตลิ่งทรายปนหินฝั่งใกล้ ค่อย ๆ ลาดขึ้น
        float up = Mathf.Max(0, d - KHw);
        float karst = Mathf.Pow(Mathf.Clamp01(1 - Mathf.Abs(Mathf.PerlinNoise(x * 0.035f + 40, z * 0.035f + 9) - 0.5f) * 3.2f), 3f);   // สันหินปูนเป็นแท่งชัน
        float bankFar = 0.4f + up * 0.3f + SStep(18, 45, up) * (10f + karst * 75f + n * 12f) + n2 * 1.2f;   // ป่าริมตลิ่งเตี้ย ๆ แล้วค่อยเป็นแท่งหินปูนไกล ๆ   // ภูเขาฝั่งลาวสูงชัน
        bankFar = Mathf.Min(bankFar, 85f + n * 20);
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
            float rock = Mathf.Max(SStep(0.5f, 1.1f, slope), SStep(KW + 0.1f, KW - 0.8f, h) * 0.5f);
            cols[i] = new Color(sand, rock, SStep(KW + 0.05f, KW - 1.2f, h), 0);
        }
        m.vertices = v; m.colors = cols; m.RecalculateNormals(); m.RecalculateBounds();
        terrain.AddComponent<MeshFilter>().sharedMesh = SaveMesh(m, "MekongTerrain");
        var tr = terrain.AddComponent<MeshRenderer>();
        tr.sharedMaterial = TerrainMat("Mekong", ("leafy_grass", "brown_mud_02", "rock_face_04"), new Vector4(0.25f, 0.45f, 0.1f, 0.025f), "#7a8e5a", "#8a8c84");   // หน้าผาหินปูนเทาอมเขียว
        tr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- แม่น้ำโขงสีน้ำตาลแดง ไหลซ้าย→ขวา ----------
        var water = new GameObject("MekongWater");
        water.AddComponent<MeshFilter>().sharedMesh = SaveMesh(Grid(200, 60, 170, new Vector3(0, KW, 20)), "MekongWater");
        var wm = LoadOrCreate($"{MatDir}/Water_Mekong.mat", waterShader);
        wm.SetColor("_Shallow", Hex("#b8784e")); wm.SetColor("_Deep", Hex("#5a2c18"));   // แม่น้ำโขงหน้าน้ำ สีน้ำตาลแดง   // น้ำโขงสีน้ำตาลอมเหลือง (ตะกอนดิน)
        wm.SetFloat("_DepthScale", 1.2f); wm.SetFloat("_Clarity", 0.12f); wm.SetFloat("_Glint", 1.2f); wm.SetFloat("_Refraction", 0.03f);
        wm.SetFloat("_Reflect", 0.55f); wm.SetFloat("_Waves", 0.075f); wm.SetFloat("_FoamWidth", 0.28f); wm.SetVector("_Flow", new Vector4(0.95f, 0.05f, 0, 0));
        wm.SetVector("_RapidZone", new Vector4(-34f, 14f, 1f, 0));   // ฟองขาวกว้างทั้งหน้าผู้เล่น   // ฟองขาวเป็นริ้วตลอดช่วงแก่ง
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
        // หินก้าวจากตลิ่งออกไปถึงโขดหินแบนกลางกระแสน้ำ (ผู้เล่นยืนตรงนั้น น้ำเชี่ยวแตกฟองรอบตัว)
        Vector3 standPt = Vector3.zero;
        for (int k = 0; k < 4; k++)
        {
            float x = sx - 0.6f + k * 0.35f, z = nearEdge(x) + 0.7f + k * 1.25f; bool last = k == 3;
            var st = Nature("namaqualand_boulder_02", x, z, 1, Rf(0, 360), 0, true, 0, KW - 0.1f);
            if (st != null) st.transform.localScale = last ? new Vector3(1.25f, 0.5f, 1.25f) : new Vector3(0.75f, 0.42f, 0.75f);
            float hr = last ? 1.1f : 0.6f;
            world.docks.Add(new Rect(x - hr, z - hr, hr * 2, hr * 2)); world.dockY.Add(KW + 0.35f);
            if (last) standPt = new Vector3(x, KW + 0.35f, z);
            Particles("StandFoam", new Vector3(x - 0.7f, KW + 0.06f, z), new Vector3(last ? 2.8f : 1.6f, last ? 2.6f : 1.4f, 0.1f), spray0, last ? 45 : 18, new Vector2(0.4f, 1.0f), new Vector2(0.6f, 1.2f), new Vector2(0.2f, 0.7f), new Color(1, 0.98f, 0.95f, 0.75f), 0.3f);   // น้ำแตกฟองขาวรอบหินที่ยืน
        }
        world.spawn = standPt;
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
            float x = Rf(-90, 90), z = Rf(KZc(0) + KHw + 12, 120), g = MekongGround(x, z);   // ป่าฝั่งลาวถอยห่างจากริมน้ำ (ไม่เป็นกำแพงต้นไม้ใกล้ ๆ)
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
        float nx0 = -11f, nz0 = nearEdge(nx0) + PointOut(nx0) - 0.4f;   // ปลายแหลมหิน
        Nature("boulder_01", nx0, nz0, 2.6f, 40, 2.2f, true, 0.6f, KW + 0.1f);
        var nagaMat = SurfMat("MKNagaStone", Color.white, 0.5f, vertexColor: true, smooth: 0.18f, tex: "rock_face_04", tiles: 1.6f, cull: CullMode.Off);   // หินแกะสลักเก่า ตะไคร่เขียว   // ทองเก่าหม่น มีผิวปูนและคราบตะไคร่จาง ๆ   // ปูนปั้นลงรักปิดทอง
        ProceduralMeshes.NagaPalette(true);   // หินเก่าตะไคร่จับ (ไม่ใช่ทองใหม่)
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
            for (int k = 0; k < 4; k++)   // ผ้าสามสีพันคอพญานาค
            {
                var rib = new GameObject("NagaCloth"); rib.transform.SetParent(shrineRoot, true);
                rib.transform.SetPositionAndRotation(bigPath[37] + new Vector3(Rf(-0.2f, 0.2f), 0.1f, Rf(-0.2f, 0.2f)), Quaternion.Euler(180, Rf(0, 360), 0)); rib.transform.localScale = Vector3.one * Rf(2.6f, 3.4f);
                rib.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.Ribbon(600 + k, new[] { Hex("#c02a2a"), Hex("#e8c030"), Hex("#2a8a3a"), Hex("#c02a2a") }[k], Rf(0.6f, 0.9f)), "MKNagaCloth" + k);
                rib.AddComponent<MeshRenderer>().sharedMaterial = ragMat;
            }
        }
        // กระท่อมไม้หลังคาสังกะสีริมตลิ่งซ้าย (แบบชาวประมงริมโขง) + ตะเกียงหน้ากระท่อม + ศาลพระภูมิเล็ก
        {
            float hx = -15.5f, hz = nearEdge(hx) + PointOut(hx) - 3.4f, hy = MekongGround(hx, hz);   // บนแหลมหิน หลังพญานาค   // ใกล้จุดยืน เห็นในมุมกล้องตกปลา
            var plank = SurfMat("HBPlank", Hex("#c0a888"), 0.04f, vertexColor: true, smooth: 0.12f, tex: "weathered_brown_planks", tiles: 0.7f);
            var tin = SurfMat("HBTin", Color.white, 0f, vertexColor: true, smooth: 0.35f, cull: CullMode.Off);
            foreach (var (px, pz) in new[] { (-1.5f, -1.2f), (1.5f, -1.2f), (-1.5f, 1.2f), (1.5f, 1.2f) }) P(PrimitiveType.Cylinder, "HutPost", new Vector3(hx + px, hy + 0.6f, hz + pz), new Vector3(0.14f, 0.6f, 0.14f), wood);
            P(PrimitiveType.Cube, "HutFloor", new Vector3(hx, hy + 1.2f, hz), new Vector3(3.4f, 0.1f, 2.8f), plank);
            P(PrimitiveType.Cube, "HutWallBack", new Vector3(hx, hy + 2.1f, hz - 1.3f), new Vector3(3.2f, 1.8f, 0.08f), plank);
            P(PrimitiveType.Cube, "HutWallL", new Vector3(hx - 1.6f, hy + 2.1f, hz), new Vector3(0.08f, 1.8f, 2.6f), plank);
            var roofT = new GameObject("HutRoof"); roofT.transform.SetParent(shrineRoot, false); roofT.transform.position = new Vector3(hx, hy + 2.95f, hz); roofT.transform.rotation = Quaternion.Euler(0, 90, 0);
            roofT.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.TinRoof(611, 3.8f, 2.0f, 0.75f), "MKHutRoof"); roofT.AddComponent<MeshRenderer>().sharedMaterial = tin;
            var lampP = new Vector3(hx + 1.5f, hy + 1.6f, hz + 1.3f);
            P(PrimitiveType.Cube, "LampCrate", lampP - Vector3.up * 0.22f, new Vector3(0.45f, 0.35f, 0.4f), wood);
            var lsrc = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/brass_diya_lantern.glb");
            if (lsrc != null) { var lg = Object.Instantiate(lsrc); lg.transform.position = lampP; lg.transform.localScale = Vector3.one * 1.6f; }
            var ll = new GameObject("LanternLight").AddComponent<Light>(); ll.type = LightType.Point; ll.color = Hex("#ffa850"); ll.range = 6f; ll.intensity = 2.4f; ll.shadows = LightShadows.None; ll.transform.position = lampP + Vector3.up * 0.4f;
            world.colliders.Add(new Vector4(hx, hz, 2.2f, 99));
        }
        {
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

        // ---------- ต้นไทรใหญ่ รากห้อย เถาวัลย์ บนตลิ่งซ้ายหลังกระท่อม (กรอบภาพแบบป่าริมโขง) ----------
        var barkMat = SurfMat("BendBark", Hex("#a09480"), 0.35f, smooth: 0.12f, vertexColor: true, tex: "bark_brown_02", tiles: 1.2f, cull: CullMode.Off);
        var aerialMat = SurfMat("BendAerial", Hex("#8a8070"), 0.1f, sway: 0.05f, swayStart: 2f, vertexColor: true, smooth: 0.1f, cull: CullMode.Off);
        var vineMat = SurfMat("BendVine", Color.white, 0f, sway: 0.08f, swayStart: 0.5f, vertexColor: true, smooth: 0.25f, cull: CullMode.Off);
        int bni = 0;
        foreach (var (x, off, h) in new[] { (-17f, -4.5f, 13f), (-23f, -2.5f, 15f), (-9f, -8.5f, 12f), (16f, -6f, 13f) })
        {
            float z = nearEdge(x) + off;
            var g = new GameObject("Banyan"); g.transform.SetParent(natureRoot, true); g.transform.SetPositionAndRotation(new Vector3(x, MekongGround(x, z) - 0.2f, z), Quaternion.Euler(0, Rf(0, 360), 0));
            g.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.Banyan(620 + bni, h), "MKBanyan" + bni); g.AddComponent<MeshRenderer>().sharedMaterials = new[] { barkMat, aerialMat };
            world.colliders.Add(new Vector4(x, z, 1.6f, 99));
            var vn = new GameObject("Vines"); vn.transform.SetParent(natureRoot, true); vn.transform.SetPositionAndRotation(new Vector3(x + 1.5f, MekongGround(x, z) + h * 0.15f, z + 1.2f), Quaternion.Euler(0, Rf(0, 360), 0));
            vn.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.Vines(630 + bni, 3.5f, h * 0.55f), "MKVines" + bni++); vn.AddComponent<MeshRenderer>().sharedMaterial = vineMat;
        }

        // ---------- หมอกเช้ามืดเรี่ยแม่น้ำ + ละอองแก่ง ----------
        var spray = ParticleMat("Spray", new Color(1, 1, 1, 0.5f));
        Particles("RapidsSpray", new Vector3(-17f, KW + 0.05f, KZc(-17f)), new Vector3(18f, KHw * 1.6f, 0.1f), spray, 40, new Vector2(0.5f, 1.2f), new Vector2(1.2f, 2f), new Vector2(0.3f, 0.9f), new Color(1, 0.95f, 0.9f, 0.32f));
        Particles("RiverMist", new Vector3(0, KW + 0.6f, KZc(0)), new Vector3(90f, KHw * 1.8f, 0.1f), spray, 10, new Vector2(5f, 9f), new Vector2(8f, 12f), new Vector2(0.05f, 0.2f), new Color(1f, 0.85f, 0.72f, 0.08f));
        foreach (var (fx, fz, fs) in new[] { (-30f, 34f, 22f), (10f, 40f, 26f), (40f, 30f, 20f), (-5f, 52f, 30f) })   // หมอกเป็นก้อนพาดตีนภูเขาหินปูน
            Particles("KarstMist", new Vector3(fx, KW + 3f, fz), new Vector3(fs, fs * 0.25f, 3f), spray, 5, new Vector2(10f, 16f), new Vector2(12f, 18f), new Vector2(0.05f, 0.15f), new Color(1f, 0.82f, 0.68f, 0.2f));
        var mm = Particles("MountainMist", new Vector3(0, 14f, KZc(0) + KHw + 18f), new Vector3(120f, 10f, 6f), spray, 8, new Vector2(10f, 16f), new Vector2(10f, 16f), new Vector2(0.1f, 0.3f), new Color(1f, 0.9f, 0.85f, 0.18f));

        // ---------- ตัวละคร กล้อง แสง ระบบเกม ----------
        var look = new Look { sun = "#ffb070", sunIntensity = 1.7f, sunRot = new Vector2(10, 160), fog = "#d8a07a", fogStart = 18, fogEnd = 150, exposure = 0.95f, ambient = 1.0f, skyId = "Overcast", skyTint = "#7a8aa8" };   // อาทิตย์ขึ้นหลังภูเขา หมอกส้มอมชมพู   // เช้ามืดแสงส้มต่ำ หมอกอมชมพู
        world.spawn.y = KW + 0.35f;   // บนโขดหินกลางน้ำ
        BuildGameplay(world, model, world.spawn, look, world);
        SaveStage(scene, path);
    }

    public static void MekongCover()
    {
        EditorSceneManager.OpenScene("Assets/Game/Scenes/Mekong.unity");
        var go = new GameObject("CoverCam"); var cam = go.AddComponent<Camera>();
        var sw = Object.FindFirstObjectByType<StageWorld>(); var sp0 = sw != null ? sw.spawn : Vector3.zero;
        go.transform.position = sp0 + new Vector3(4.5f, 3.4f, -7f); go.transform.LookAt(sp0 + new Vector3(-13f, 1.6f, 16f));   // มุมแบบภาพตัวอย่าง: หลังผู้เล่นเยื้องซ้าย เห็นพญานาค กระท่อม แก่ง ภูเขาหินปูนในหมอก
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
