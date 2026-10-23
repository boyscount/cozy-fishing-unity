// ด่าน: หลืบแพสุสาน — คุ้งแม่น้ำกว้างหมอกลงจัด เรือนแพร้างลอยเกยกันเป็นกลุ่ม บางหลังเอียงจมครึ่งหลัง หลังคาจากพัง
//      เรือพายคว่ำ/จมอยู่ระหว่างแพ หลักผูกเรือโผล่น้ำ เศษไม้ลอย ตะเกียงริบหรี่บนแพหลังหนึ่ง · ผู้เล่นยืนบนแพไม้ไผ่ที่ยื่นจากฝั่งออกไปกลางน้ำ
using System.IO;
using CozyFishing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class SceneBuilder
{
    const float HW = -0.3f;                                  // ระดับน้ำ

    static float HouseboatGround(float x, float z)
    {
        float n = Mathf.PerlinNoise(x * 0.12f + 31, z * 0.12f + 17) - 0.5f, n2 = Mathf.PerlinNoise(x * 0.45f + 7, z * 0.45f + 23) - 0.5f;
        float near = -3.5f + n * 2.5f, far = 40f + n * 5f;
        float land = Mathf.Max(SStep(near + 1.5f, near - 1.5f, z), SStep(far - 3f, far + 3f, z));
        float bank = 0.25f + n2 * 0.2f + Mathf.Max(0, z - far) * 0.3f + Mathf.Max(0, near - 2 - z) * 0.06f;
        float mid = Mathf.Min(SStep(near, near + 8, z), SStep(far, far - 10, z));
        float bed = HW - 1.2f - 1.8f * mid + n * 0.6f + n2 * 0.2f;           // แม่น้ำลึก 1.2–3 ม.
        return Mathf.Lerp(bed, bank, land);
    }

    // รวมลูกทั้งหมดใต้ root เป็น mesh เดียวต่อวัสดุ (ลดจำนวนวัตถุ/draw call) แล้วลบลูกเดิม
    static void CombineGroup(Transform root, string name)
    {
        var byMat = new System.Collections.Generic.Dictionary<Material, System.Collections.Generic.List<CombineInstance>>();
        var dead = new System.Collections.Generic.List<GameObject>();
        var w2l = root.worldToLocalMatrix;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
        {
            var mr = mf.GetComponent<MeshRenderer>();
            if (mr == null || mf.sharedMesh == null || mf.transform == root) continue;
            var mats = mr.sharedMaterials;
            for (int sm = 0; sm < mf.sharedMesh.subMeshCount; sm++)
            {
                var mat = mats[Mathf.Min(sm, mats.Length - 1)];
                if (!byMat.TryGetValue(mat, out var l)) byMat[mat] = l = new System.Collections.Generic.List<CombineInstance>();
                l.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = sm, transform = w2l * mf.transform.localToWorldMatrix });
            }
            dead.Add(mf.gameObject);
        }
        foreach (var g in dead) if (g != null) Object.DestroyImmediate(g);
        int i = 0;
        foreach (var kv in byMat)
        {
            var m = new Mesh { indexFormat = IndexFormat.UInt32 };
            m.CombineMeshes(kv.Value.ToArray(), true, true);
            var go = new GameObject(name + "_" + i); go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(m, name + "_" + i);
            go.AddComponent<MeshRenderer>().sharedMaterial = kv.Key;
            i++;
        }
    }

    public static void CreateHouseboat()
    {
        const string path = "Assets/Game/Scenes/Houseboat.unity";
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
        var world = new GameObject("World").AddComponent<StageWorld>();
        world.stageId = "houseboat"; world.waterY = HW;
        world.origin = new Vector2(-45, -20); world.cell = 0.5f; world.nx = 181; world.nz = 141;
        world.heights = new float[world.nx * world.nz];
        for (int j = 0; j < world.nz; j++) for (int i = 0; i < world.nx; i++) world.heights[j * world.nx + i] = HouseboatGround(world.origin.x + i * world.cell, world.origin.y + j * world.cell);
        world.walkBounds = new Rect(-22, -14, 44, 24);
        float deckY = HW + 0.28f;
        world.spawn = new Vector3(0, deckY, 6.2f); world.spawnYaw = 0;
        world.special = new Vector3(3f, HW, 13f); world.specialRadius = 3f;
        world.camBounds = new Rect(-14, -9, 28, 26); world.camBias = new Vector2(0, 12f); world.camBiasK = 0.35f;

        // ---------- พื้นดิน + น้ำ ----------
        var terrain = new GameObject("Terrain");
        var m = Grid(100, 70, 190, new Vector3(0, 0, 12));
        var v = m.vertices; var cols = new Color[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            float x = v[i].x, z = v[i].z, h = HouseboatGround(x, z);
            v[i].y = h;
            cols[i] = new Color(SStep(HW + 0.4f, HW + 0.02f, h), 0.3f * Mathf.PerlinNoise(x * 0.3f, z * 0.3f), SStep(HW + 0.05f, HW - 1.2f, h), 0);
        }
        m.vertices = v; m.colors = cols; m.RecalculateNormals(); m.RecalculateBounds();
        terrain.AddComponent<MeshFilter>().sharedMesh = SaveMesh(m, "HouseboatTerrain");
        var tr = terrain.AddComponent<MeshRenderer>();
        tr.sharedMaterial = TerrainMat("Houseboat", ("leafy_grass", "brown_mud_02", "forest_ground_05"), new Vector4(0.3f, 0.4f, 0.3f, 0.03f), "#98a87e", "#6f8068");
        tr.shadowCastingMode = ShadowCastingMode.Off;
        var water = new GameObject("RiverWater");
        water.AddComponent<MeshFilter>().sharedMesh = SaveMesh(Grid(100, 70, 150, new Vector3(0, HW, 12)), "HouseboatWater");
        var wm = LoadOrCreate($"{MatDir}/Water_Houseboat.mat", waterShader);
        wm.SetColor("_Shallow", Hex("#a8b0a4")); wm.SetColor("_Deep", Hex("#2a3632"));
        wm.SetFloat("_DepthScale", 1.5f); wm.SetFloat("_Clarity", 0.4f); wm.SetFloat("_Glint", 0.8f); wm.SetFloat("_Refraction", 0.03f);
        wm.SetFloat("_Reflect", 0.95f); wm.SetFloat("_Waves", 0.018f); wm.SetFloat("_FoamWidth", 0.05f); wm.SetVector("_Flow", new Vector4(0.12f, 0.01f, 0, 0));
        wm.SetVector("_RapidZone", Vector4.zero);
        var wr = water.AddComponent<MeshRenderer>(); wr.sharedMaterial = wm; wr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- วัสดุ ----------
        var plank = SurfMat("HBPlank", Hex("#c0a888"), 0.04f, smooth: 0.12f, tex: "weathered_brown_planks", tiles: 0.7f);
        var plankDark = SurfMat("HBPlankDark", Hex("#9a8268"), 0.05f, smooth: 0.1f, tex: "weathered_brown_planks", tiles: 0.7f);
        var bamboo = SurfMat("HBBamboo", Hex("#c8b07a"), 0.04f, smooth: 0.35f, tex: "bark_brown_02", tiles: 1.6f);
        var roofMat = SurfMat("HBRoof", Hex("#9a8060"), 0.03f, smooth: 0.08f, tex: "reed_roof_04", tiles: 0.6f, cull: CullMode.Off);
        var boatMat = SurfMat("HBBoat", Hex("#8a6e52"), 0.05f, smooth: 0.25f, tex: "weathered_brown_planks", tiles: 0.9f, cull: CullMode.Off);
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

        // ---------- แพลอยของผู้เล่น: ทางเดินไม้ไผ่จากฝั่ง + ลานแพปลายทาง ----------
        group = new GameObject("PlayerRaft").transform;
        foreach (var (a, b, w) in new[] { (new Vector2(0, -4f), new Vector2(0, 4.2f), 2.2f), (new Vector2(0, 4.2f), new Vector2(0, 7.6f), 6.5f) })
        {
            float len = b.y - a.y, cz = (a.y + b.y) * 0.5f;
            for (float x = -w * 0.5f + 0.13f; x < w * 0.5f; x += 0.26f) Log(new Vector3(x, HW + 0.08f, cz), len, 0.12f, bamboo, new Vector3(90, 0, 0));
            for (float z = a.y + 0.3f; z < b.y; z += 1.4f) Box("Crossbar", new Vector3(0, HW + 0.22f, z), new Vector3(w + 0.2f, 0.07f, 0.12f), plankDark);
            world.docks.Add(new Rect(-w * 0.5f - 0.1f, a.y, w + 0.2f, len)); world.dockY.Add(deckY);
        }
        foreach (var (x, z) in new[] { (-3.1f, 7.4f), (3.1f, 7.4f), (-1.2f, 0f), (1.2f, 0f) })   // หลักปักยึดแพ
            Log(new Vector3(x, HW + 0.2f, z), 3f, 0.08f, plankDark, Vector3.zero);
        Box("RaftShade", new Vector3(-2.2f, deckY + 1.15f, 6.4f), new Vector3(1.8f, 0.06f, 1.6f), roofMat, new Vector3(0, 0, 12));
        foreach (var (x, z) in new[] { (-3f, 5.7f), (-1.4f, 5.7f), (-3f, 7.1f), (-1.4f, 7.1f) }) Log(new Vector3(x, deckY + 0.55f, z), 1.15f, 0.04f, bamboo, Vector3.zero);

        // ---------- เรือนแพร้าง: แพลูกบวบไม้ไผ่ + พื้นไม้ + ฝาไม้กระดานทีละแผ่น (มีร่อง แผ่นหลุด) + หลังคาจากมุงซ้อนชั้น
        //            ภายในมืด (มองผ่านประตูเห็นความมืด) · บานหน้าต่างห้อยเอียง · โอ่ง ไซดักปลา ราวตากผ้าขาด · บางหลังเอียงจม หลังคายุบ
        var thatch = SurfMat("HBThatch", Color.white, 0f, smooth: 0.06f, vertexColor: true, tex: "reed_roof_04", tiles: 0.55f, cull: CullMode.Off);
        var interior = SurfMat("HBInterior", Hex("#1c1a16"), 0f, smooth: 0.02f);
        var clay = SurfMat("HBClay", Hex("#8a5a3a"), 0.08f, smooth: 0.35f);
        var wicker = SurfMat("HBWicker", Hex("#b8a070"), 0f, smooth: 0.1f, tex: "reed_roof_04", tiles: 2.5f, cull: CullMode.Off);
        var ragMat = SurfMat("Ribbon", Color.white, sway: 0.12f, swayStart: 0f, vertexColor: true, smooth: 0.5f, cull: CullMode.Off);
        var jarMesh = SaveMesh(ProceduralMeshes.Lathe(new[] { new Vector2(0.12f, 0), new Vector2(0.2f, 0.12f), new Vector2(0.23f, 0.28f), new Vector2(0.2f, 0.45f), new Vector2(0.13f, 0.55f), new Vector2(0.14f, 0.6f), new Vector2(0.11f, 0.6f) }, 16, Color.white), "HBJar");
        var trapMesh = SaveMesh(ProceduralMeshes.Lathe(new[] { new Vector2(0.03f, 0), new Vector2(0.14f, 0.15f), new Vector2(0.17f, 0.5f), new Vector2(0.12f, 0.85f), new Vector2(0.05f, 1f) }, 12, Color.white), "HBTrap");
        var ragColors = new[] { Hex("#7a3a2a"), Hex("#4a5a6a"), Hex("#8a7a5a"), Hex("#5a3a4a") };
        GameObject MeshAt(string name, Mesh mesh, Material mat, Vector3 lp, Vector3 rot, float scale = 1)
        {
            var g = new GameObject(name); g.transform.SetParent(group, false); g.transform.localPosition = lp; g.transform.localRotation = Quaternion.Euler(rot); g.transform.localScale = Vector3.one * scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = mat; return g;
        }
        // ฝาไม้กระดานเรียงแนวตั้ง: axisX = ผนังวางตามแกน X (หน้า/หลัง) ไม่งั้นตามแกน Z (ข้าง) · ช่วง [gap0,gap1] เว้นช่องประตู/หน้าต่าง (เหลือไม้ใต้ gapBottom และเหนือ gapTop)
        void Boards(Vector3 c, float width, float h, bool axisX, float gap0 = 99, float gap1 = 99, float gapBottom = 0, float gapTop = 0, bool ruined = false)
        {
            for (float u = -width * 0.5f + 0.09f; u < width * 0.5f; u += 0.19f)
            {
                float bh = h * Rf(0.94f, 1.02f);
                if (ruined && rnd.NextDouble() < 0.4) bh *= Rf(0.25f, 0.6f);          // แผ่นหักเหลือครึ่ง
                if (rnd.NextDouble() < 0.05) continue;                                  // แผ่นหลุดหาย
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
                Box("Board", c + off + Vector3.up * bh * 0.5f, size, col, axisX ? new Vector3(0, 0, Rf(-1.5f, 1.5f)) : new Vector3(Rf(-1.5f, 1.5f), 0, 0));
            }
        }
        var lanterns = new System.Collections.Generic.List<Vector3>();
        var houseRoots = new System.Collections.Generic.List<Transform>();
        int hb = 0;
        foreach (var (x, z, yaw, sink, tiltX, tiltZ, broken) in new[] {
            (-9f, 11f, 12f, 0f, 0f, 2f, false), (-3.5f, 16f, -8f, 0.25f, -4f, 3f, false), (3f, 13.5f, 20f, 0.75f, 7f, -9f, true), (9.5f, 11.5f, -15f, 0.1f, 0f, -3f, false),
            (14f, 18f, 30f, 0.4f, 5f, 4f, true), (-13.5f, 19.5f, -25f, 0f, 0f, 0f, false), (6.5f, 22.5f, 5f, 0.2f, -3f, 2f, false), (-1.5f, 26f, -12f, 0.9f, 10f, 6f, true) })
        {
            group = new GameObject("Houseboat").transform; houseRoots.Add(group);
            group.SetPositionAndRotation(new Vector3(x, HW - sink, z), Quaternion.Euler(tiltX, yaw, tiltZ));
            float L = Rf(4.2f, 5.6f), W = Rf(3.2f, 3.8f), H = Rf(1.9f, 2.3f);
            for (float lx = -W * 0.5f + 0.16f; lx < W * 0.5f; lx += 0.32f) Log(new Vector3(lx, 0.05f, Rf(-0.1f, 0.1f)), L + Rf(0.4f, 0.8f), Rf(0.13f, 0.16f), bamboo, new Vector3(90, 0, 0));   // ลูกบวบ
            for (float dz = -L * 0.5f - 0.15f; dz < L * 0.5f + 0.2f; dz += 0.22f)                                                                                      // พื้นกระดานขวาง
                if (rnd.NextDouble() > (broken ? 0.12 : 0.03)) Box("DeckBoard", new Vector3(Rf(-0.04f, 0.04f), 0.25f, dz), new Vector3(W * Rf(0.97f, 1.03f), 0.05f, 0.2f), rnd.NextDouble() < 0.3 ? plankDark : plank, new Vector3(0, Rf(-1, 1), 0));
            float hw = W * 0.4f, hl = L * 0.32f, y0 = 0.28f;
            Box("Interior", new Vector3(0, y0 + H * 0.45f, 0), new Vector3(hw * 2 - 0.12f, H * 0.9f, hl * 2 - 0.12f), interior);
            Boards(new Vector3(0, y0, hl), hw * 2, H, true, ruined: broken);
            Boards(new Vector3(0, y0, -hl), hw * 2, H, true, -0.38f, 0.38f, 0, 1.75f);                                      // ประตูหน้า
            for (int s = -1; s <= 1; s += 2)
            {
                Boards(new Vector3(s * hw, y0, 0), hl * 2, H, false, -0.35f, 0.35f, 0.85f, 1.55f, broken && s > 0);           // หน้าต่างข้าง
                Box("Shutter", new Vector3(s * (hw + 0.05f), y0 + 1.25f, 0.42f), new Vector3(0.03f, 0.62f, 0.36f), plankDark, new Vector3(0, s * Rf(30, 70), Rf(-20, 20)));   // บานเปิดห้อยเอียง
            }
            // หลังคาจาก (สันตามแกน Z) · หลังที่พังเอียงยุบลงข้างหนึ่ง
            var roofMesh = SaveMesh(ProceduralMeshes.ThatchRoof(400 + hb, hl * 2 + 1.0f, hw + 0.65f, 1.15f), "HBThatch" + hb);
            MeshAt("Thatch", roofMesh, thatch, new Vector3(0, y0 + H - 0.05f - (broken ? 0.35f : 0), 0), new Vector3(0, 0, broken ? Rf(10, 18) : Rf(-2, 2)));
            Box("Ridge", new Vector3(0, y0 + H + 1.25f - (broken ? 0.35f : 0), 0), new Vector3(0.14f, 0.12f, hl * 2 + 1.0f), plankDark, new Vector3(0, 0, broken ? 14 : 0));
            // ระเบียงหน้า: เสา ราว · โอ่งน้ำ ไซดักปลา
            for (int s = -1; s <= 1; s += 2) Log(new Vector3(s * (W * 0.5f - 0.15f), y0 + 0.5f, -L * 0.5f + 0.1f), 1f, 0.05f, plankDark, Vector3.zero);
            Box("Rail", new Vector3(0, y0 + 0.95f, -L * 0.5f + 0.1f), new Vector3(W - 0.2f, 0.06f, 0.06f), plankDark, new Vector3(0, 0, broken ? 14 : 0));
            MeshAt("Jar", jarMesh, clay, new Vector3(-W * 0.32f, y0, -hl - 0.45f), new Vector3(0, Rf(0, 360), 0), Rf(0.9f, 1.2f));
            if (rnd.NextDouble() < 0.6) MeshAt("Jar", jarMesh, clay, new Vector3(-W * 0.32f + 0.45f, y0, -hl - 0.5f), new Vector3(broken ? 80 : 0, Rf(0, 360), 0), Rf(0.7f, 0.9f));
            MeshAt("FishTrap", trapMesh, wicker, new Vector3(W * 0.33f, y0 + 0.17f, hl * 0.4f), new Vector3(0, Rf(0, 360), 90), Rf(0.8f, 1.1f));
            MeshAt("FishTrap", trapMesh, wicker, new Vector3(W * 0.36f, y0 + 0.17f, hl * 0.4f + 0.38f), new Vector3(0, Rf(0, 360), 90), Rf(0.7f, 0.9f));
            if (hb % 2 == 0)   // ราวตากผ้าเก่าขาด ๆ ด้านหลังเรือน
            {
                float rz = L * 0.5f - 0.15f;
                for (int s = -1; s <= 1; s += 2) Log(new Vector3(s * (W * 0.5f - 0.2f), y0 + 0.75f, rz), 1.5f, 0.03f, bamboo, Vector3.zero);
                Box("Line", new Vector3(0, y0 + 1.45f, rz), new Vector3(W - 0.4f, 0.012f, 0.012f), plankDark);
                for (int k = 0; k < 3; k++)
                    MeshAt("Rag", SaveMesh(ProceduralMeshes.Ribbon(500 + hb * 3 + k, ragColors[(hb + k) % ragColors.Length], Rf(0.45f, 0.75f)), "HBRag" + hb + "_" + k), ragMat,
                        new Vector3(-W * 0.3f + k * W * 0.28f, y0 + 1.45f, rz), new Vector3(180, Rf(-20, 20), 0), Rf(2.2f, 3.2f));
            }
            if (hb == 0 || hb == 3 || hb == 5 || hb == 6) lanterns.Add(group.TransformPoint(new Vector3(W * 0.5f - 0.3f, y0 + 1.05f, -L * 0.5f + 0.15f)));
            hb++;
        }
        // สะพานไม้พาดระหว่างแพ (บางแผ่นหาย)
        foreach (var (ia, ib) in new[] { (0, 1), (3, 6), (1, 7) })
        {
            group = new GameObject("PlankBridge").transform;
            var a = houseRoots[ia].position; var b = houseRoots[ib].position;
            a.y = b.y = HW + 0.22f;
            var dir = (b - a); float len = dir.magnitude; dir /= len;
            group.SetPositionAndRotation(a, Quaternion.LookRotation(dir));
            for (float t = 2.6f; t < len - 2.6f; t += 0.3f) if (rnd.NextDouble() > 0.18) Box("BridgeBoard", new Vector3(Rf(-0.05f, 0.05f), Mathf.Sin(t * 0.5f) * 0.05f, t), new Vector3(0.75f, 0.05f, 0.22f), plank, new Vector3(Rf(-3, 3), Rf(-6, 6), Rf(-4, 4)));
            for (int s = -1; s <= 1; s += 2) Box("BridgeBeam", new Vector3(s * 0.32f, -0.06f, len * 0.5f), new Vector3(0.08f, 0.08f, len - 5f), plankDark);
            houseRoots.Add(group);
        }
        foreach (var p in lanterns)
        {
            Nature2("brass_diya_lantern", p, 1.6f);
            var lg = new GameObject("LanternLight").AddComponent<Light>();
            lg.type = LightType.Point; lg.range = 4f; lg.intensity = 1.1f; lg.color = Hex("#ffa850"); lg.shadows = LightShadows.None;
            lg.transform.position = p + Vector3.up * 0.4f;
        }
        // บ้านเสาสูงริมตลิ่งฝั่งไกล: เงาตะคุ่มในหมอก ให้ฉากลึก
        int si = 0;
        foreach (var (x, yaw) in new[] { (-17f, 8f), (-6f, -6f), (5f, 12f), (16f, -10f) })
        {
            float z = 39f + Rf(-1, 1.5f), gy = HouseboatGround(x, z);
            group = new GameObject("StiltHouse").transform; houseRoots.Add(group);
            group.SetPositionAndRotation(new Vector3(x, gy, z), Quaternion.Euler(0, yaw, 0));
            float W = Rf(4f, 5.5f), L = Rf(3.5f, 4.5f), raise = Rf(1.6f, 2.2f), H = 2.2f;
            for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2) Log(new Vector3(sx * W * 0.45f, raise * 0.5f - 0.3f, sz * L * 0.45f), raise + 0.6f, 0.09f, plankDark, Vector3.zero);
            Box("Floor", new Vector3(0, raise, 0), new Vector3(W, 0.12f, L), plankDark);
            Box("Walls", new Vector3(0, raise + H * 0.5f, 0), new Vector3(W * 0.9f, H, L * 0.9f), plankDark);
            MeshAt("Thatch", SaveMesh(ProceduralMeshes.ThatchRoof(450 + si, L + 0.8f, W * 0.5f + 0.5f, 1.4f), "HBStiltThatch" + si), thatch, new Vector3(0, raise + H - 0.05f, 0), new Vector3(0, 90, 0));
            si++;
        }
        // ผักตบชวาลอยเป็นแพรอบเรือนแพและริมตลิ่ง
        var hyaMat = SurfMat("Hyacinth", Color.white, 0f, sway: 0.03f, swayStart: 0.1f, vertexColor: true, smooth: 0.55f, cull: CullMode.Off);
        var hyaMeshes = new Mesh[4]; for (int i = 0; i < 4; i++) hyaMeshes[i] = SaveMesh(ProceduralMeshes.Hyacinth(600 + i), "Hyacinth" + i);
        group = new GameObject("Hyacinths").transform; houseRoots.Add(group);
        for (int c = 0, n = 0; c < 400 && n < 140; c++)
        {
            float cx = Rf(-18, 18), cz = Rf(-1, 34);
            if (Mathf.Abs(cx) < 4 && cz < 9 || HouseboatGround(cx, cz) > HW - 0.1f) continue;
            if (Mathf.PerlinNoise(cx * 0.25f + 3, cz * 0.25f + 9) < 0.55f) continue;   // เกาะกันเป็นแพ
            MeshAt("Hyacinth", hyaMeshes[rnd.Next(4)], hyaMat, new Vector3(cx, HW - 0.02f, cz), new Vector3(0, Rf(0, 360), 0), Rf(1.2f, 1.9f));
            n++;
        }
        // รวม mesh ต่อเรือน (วัตถุน้อยลง เบาเครื่องบนเว็บ)
        for (int i = 0; i < houseRoots.Count; i++) CombineGroup(houseRoots[i], "HBGroup" + i);
        void Nature2(string file, Vector3 p, float scale)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/{file}.glb");
            if (src == null) return;
            var go = Object.Instantiate(src); go.name = file; go.transform.position = p; go.transform.localScale = Vector3.one * scale;
            foreach (var rr in go.GetComponentsInChildren<Renderer>()) rr.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- เรือพาย: ลอยผูกหลัก · จมครึ่งลำ · คว่ำ ----------
        var boatMesh = SaveMesh(ProceduralMeshes.Rowboat(3.6f, 0.95f, 0.38f), "Rowboat");
        foreach (var (x, z, yaw, y, roll, pitch) in new[] {
            (-5.5f, 8.5f, 70f, HW + 0.12f, 0f, 0f), (6f, 9f, -60f, HW - 0.12f, 8f, 4f), (0.5f, 19.5f, 15f, HW - 0.3f, 25f, -8f),
            (-8f, 23f, 110f, HW + 0.05f, 180f, 0f), (11f, 20f, 40f, HW + 0.1f, -4f, 0f) })
        {
            var g = new GameObject("Rowboat"); g.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(pitch, yaw, roll));
            g.AddComponent<MeshFilter>().sharedMesh = boatMesh; g.AddComponent<MeshRenderer>().sharedMaterial = boatMat;
            group = g.transform;
            if (roll < 90) for (int k = -1; k <= 1; k++) Box("Thwart", new Vector3(0, 0.02f, k * 0.9f), new Vector3(0.8f, 0.04f, 0.18f), plank);   // ไม้นั่งพาย
        }
        // หลักผูกเรือ + เศษไม้ลอย
        var stakes = new GameObject("Stakes").transform;
        for (int i = 0; i < 22; i++)
        {
            float x = Rf(-16, 16), z = Rf(8, 30);
            if (Mathf.Abs(x) < 4 && z < 10) continue;
            var c = Prim(PrimitiveType.Cylinder, "Stake", new Vector3(x, HW + Rf(0.1f, 0.9f) - 1.2f, z), new Vector3(0.14f, 1.4f + Rf(0, 0.5f), 0.14f), plankDark);
            c.transform.rotation = Quaternion.Euler(Rf(-8, 8), 0, Rf(-8, 8)); c.transform.SetParent(stakes, true);
        }
        for (int i = 0; i < 14; i++)
        {
            var g = Prim(PrimitiveType.Cube, "Driftwood", new Vector3(Rf(-15, 15), HW + 0.01f, Rf(9, 30)), new Vector3(Rf(0.2f, 0.35f), 0.05f, Rf(1f, 2.4f)), plank);
            g.transform.rotation = Quaternion.Euler(0, Rf(0, 360), Rf(-4, 4)); g.transform.SetParent(stakes, true);
        }

        // ---------- ตลิ่งสองฝั่ง: กอกก ต้นไม้ ตาล ----------
        var natureRoot = new GameObject("Nature").transform;
        GameObject Nature(string file, float x, float z, float scale, float yaw, float collide = 0, bool shadows = true, float sink = 0.04f)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/{file}.glb");
            if (src == null) return null;
            var go = Object.Instantiate(src, natureRoot); go.name = file;
            go.transform.SetPositionAndRotation(new Vector3(x, HouseboatGround(x, z) - sink, z), Quaternion.Euler(0, yaw, 0)); go.transform.localScale = Vector3.one * scale;
            foreach (var rr in go.GetComponentsInChildren<Renderer>())
            {
                rr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                var mats = rr.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = LeafMat(file, mats[i]) ?? mats[i]; rr.sharedMaterials = mats;
            }
            if (collide > 0) world.colliders.Add(new Vector4(x, z, collide, 99));
            return go;
        }
        var reedMat = SurfMat("Reeds", Color.white, sway: 0.08f, swayStart: 0.3f, vertexColor: true, smooth: 0.25f, cull: CullMode.Off);
        var reedMeshes = new Mesh[4]; for (int i = 0; i < 4; i++) reedMeshes[i] = SaveMesh(ProceduralMeshes.Reeds(300 + i, Rf(1.4f, 2.2f)), "HBReeds" + i);
        for (int tries = 0, n = 0; tries < 2000 && n < 110; tries++)
        {
            float x = Rf(-22, 22), z = Rf(-8, 46), h = HouseboatGround(x, z);
            if (h > HW + 0.5f || h < HW - 0.35f || Mathf.Abs(x) < 2.5f && z < 1) continue;
            var g = new GameObject("Reeds"); g.transform.SetParent(natureRoot, true);
            g.transform.SetPositionAndRotation(new Vector3(x, Mathf.Max(h, HW - 0.35f) - 0.05f, z), Quaternion.Euler(0, Rf(0, 360), 0)); g.transform.localScale = Vector3.one * Rf(0.8f, 1.3f);
            g.AddComponent<MeshFilter>().sharedMesh = reedMeshes[rnd.Next(4)]; g.AddComponent<MeshRenderer>().sharedMaterial = reedMat;
            n++;
        }
        foreach (var (x, z, s) in new[] { (-18f, -6f, 1.6f), (16f, -7f, 1.5f), (-9f, -9f, 1.4f), (9f, -10f, 1.5f), (-20f, 45f, 1.8f), (-6f, 47f, 1.7f), (8f, 46f, 1.9f), (22f, 44f, 1.6f) })
            Nature("island_tree_01", x, z, s, Rf(0, 360), 0.9f, true, 0.15f);
        var palmTrunk = SurfMat("SugarPalmTrunk", Hex("#b8a890"), tex: "bark_brown_02", tiles: 0.9f, cull: CullMode.Off);
        var palmLeaf = SurfMat("SugarPalmLeaf", Color.white, sway: 0.05f, swayStart: 6f, vertexColor: true, smooth: 0.35f, cull: CullMode.Off);
        int pi = 0;
        foreach (var (x, z, h) in new[] { (-14f, 47f, 9f), (1f, 48f, 10f), (15f, 47f, 8.5f), (-4f, -11f, 8f) })
        {
            var go = new GameObject("SugarPalm"); go.transform.SetParent(natureRoot, true);
            go.transform.SetPositionAndRotation(new Vector3(x, HouseboatGround(x, z) - 0.1f, z), Quaternion.Euler(0, Rf(0, 360), 0));
            go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ProceduralMeshes.SugarPalm(171 + pi, h), "HBPalm" + pi++);
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { palmTrunk, palmLeaf };
        }
        Nature("dead_tree_trunk", -11f, 13f, 1.5f, 40, 0, true, 0.4f);
        Nature("root_cluster_01", 12f, -4f, 1f, 200, 0, true, 0.3f);
        for (int i = 0; i < 12; i++)
        {
            float x = Rf(-20, 20), z = Rf(-12, -5);
            if (HouseboatGround(x, z) < HW + 0.1f || Mathf.Abs(x) < 2f) continue;
            Nature(new[] { "shrub_02", "fern_02", "weed_plant_02" }[i % 3], x, z, Rf(0.6f, 1f), Rf(0, 360), i % 3 == 0 ? 0.6f : 0, i % 3 != 2, 0.03f);
        }

        // หมอกหนาลอยบนน้ำ
        var mist = ParticleMat("Spray", new Color(1, 1, 1, 0.5f));
        Particles("RiverMist", new Vector3(0, HW + 0.4f, 16), new Vector3(40f, 34f, 0.1f), mist, 34, new Vector2(4f, 8f), new Vector2(7f, 11f), new Vector2(0.05f, 0.18f), new Color(1, 1, 1, 0.16f));

        // ---------- ตัวละคร กล้อง แสง ระบบเกม ----------
        var look = new Look { sun = "#dde2e4", sunIntensity = 1.25f, sunRot = new Vector2(38, 140), fog = "#9ea6a8", fogStart = 7, fogEnd = 50, exposure = 0.75f, ambient = 0.95f, skyId = "Overcast", skyTint = "#76808a" };   // ฟ้าครึ้มเทา หมอกจัด
        BuildGameplay(world, model, world.spawn, look, world);
        SaveStage(scene, path);
    }
}
