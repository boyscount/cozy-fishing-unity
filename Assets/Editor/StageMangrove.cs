// ด่าน: ป่าชายเลนรากมรณะ — ร่องน้ำกร่อยคดเคี้ยวลอดป่าโกงกางรกทึบ รากค้ำยันโค้งพันกันเหมือนกรงเล็บ ต้นโกงกางตายยืนต้นซีด
//      ซากเรือประมงติดค้างในราก แหเก่าขาดห้อยตามรากไม้ รากหายใจโผล่เต็มลานเลน ฟ้าครึ้มหมอกจาง
//      ผู้เล่นยืนบนลานเลนริมร่องน้ำ หันเฉียงลงน้ำ กล้องมองตามร่องน้ำลึกเข้าไปในป่า
using System.Collections.Generic;
using System.IO;
using CozyFishing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class SceneBuilder
{
    const float GW = -0.3f;
    static float GXc(float z) => 2f * Mathf.Sin(z * 0.09f + 0.3f);                              // แนวกลางร่องน้ำ (ทอดไปตามแกน z)
    static float GHw(float z) => 3.6f + 0.9f * Mathf.Sin(z * 0.15f + 0.7f);

    static float MangroveGround(float x, float z)
    {
        float n = Mathf.PerlinNoise(x * 0.18f + 31, z * 0.18f + 9) - 0.5f, n2 = Mathf.PerlinNoise(x * 0.6f + 5, z * 0.6f + 77) - 0.5f;
        float s = Mathf.Abs(x - GXc(z)), d = s / GHw(z);
        float bed = GW - 0.5f - 1.3f * Mathf.Clamp01(1 - d * d) + n2 * 0.2f;
        float bank = GW + 0.12f + n * 0.4f + n2 * 0.12f + Mathf.Max(0, s - GHw(z) - 4f) * 0.06f;   // ลานเลนแบน ๆ มีแอ่งน้ำขังเป็นหย่อม
        return Mathf.Lerp(bed, bank, SStep(0.8f, 1.2f, d));
    }

    // แหแขวน: ตาข่ายหย่อนโค้งระหว่างจุดผูกสองจุด ปลายล่างย้วยไม่เท่ากัน
    static Mesh NetMesh(int seed, float w, float h)
    {
        var rnd = new System.Random(seed);
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        const int nu = 12, nv = 9;
        for (int j = 0; j <= nv; j++)
            for (int i = 0; i <= nu; i++)
            {
                float u = i / (float)nu, t = j / (float)nv;
                float sag = Mathf.Sin(u * Mathf.PI) * 0.35f * (1 - t * 0.5f), drip = t * h * (1 + 0.25f * Mathf.Sin(u * 7 + seed) * t);
                v.Add(new Vector3((u - 0.5f) * w * (1 - t * 0.25f), -sag - drip, Mathf.Sin(u * Mathf.PI) * 0.12f + t * t * 0.15f + (float)rnd.NextDouble() * 0.03f));
                uv.Add(new Vector2(u * w * 0.6f, t * h * 0.6f));
            }
        for (int j = 0; j < nv; j++)
            for (int i = 0; i < nu; i++) { int a = j * (nu + 1) + i, b = a + nu + 1; tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
        var m = new Mesh(); m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    public static void CreateMangrove()
    {
        const string path = "Assets/Game/Scenes/Mangrove.unity";
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

        // ---------- โลก ----------
        var world = new GameObject("World").AddComponent<StageWorld>();
        world.stageId = "mangrove"; world.waterY = GW;
        world.origin = new Vector2(-30, -20); world.cell = 0.5f; world.nx = 121; world.nz = 161;
        world.heights = new float[world.nx * world.nz];
        for (int j = 0; j < world.nz; j++) for (int i = 0; i < world.nx; i++) world.heights[j * world.nx + i] = MangroveGround(world.origin.x + i * world.cell, world.origin.y + j * world.cell);
        world.walkBounds = new Rect(-14, -10, 28, 30);
        float spX = GXc(0) + GHw(0) + 0.9f;
        world.spawn = new Vector3(spX, MangroveGround(spX, 0), 0); world.spawnYaw = -35;
        world.camBounds = new Rect(-8, -8, 16, 22); world.camBias = new Vector2(GXc(6), 6); world.camBiasK = 0.35f;
        world.special = new Vector3(GXc(12) - 1f, GW, 12); world.specialRadius = 3f;   // หน้าซากเรือที่ติดรากฝั่งซ้าย

        // ---------- ลานเลน + น้ำกร่อย ----------
        var terrain = new GameObject("Terrain");
        var m = Grid(60, 80, 170, new Vector3(0, 0, 20));
        var v = m.vertices; var cols = new Color[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            float x = v[i].x, z = v[i].z, h = MangroveGround(x, z);
            v[i].y = h;
            cols[i] = new Color(SStep(GW + 0.55f, GW + 0.02f, h), 0.5f + 0.4f * Mathf.PerlinNoise(x * 0.3f, z * 0.3f), SStep(GW + 0.05f, GW - 1f, h), 0);
        }
        m.vertices = v; m.colors = cols; m.RecalculateNormals(); m.RecalculateBounds();
        terrain.AddComponent<MeshFilter>().sharedMesh = SaveMesh(m, "MangroveTerrain");
        var tr = terrain.AddComponent<MeshRenderer>();
        tr.sharedMaterial = TerrainMat("Mangrove", ("mud_forest", "brown_mud_02", "mud_forest"), new Vector4(0.35f, 0.4f, 0.3f, 0.03f), "#7a6e58", "#5a5244");
        tr.shadowCastingMode = ShadowCastingMode.Off;
        var water = new GameObject("CreekWater");
        water.AddComponent<MeshFilter>().sharedMesh = SaveMesh(Grid(60, 80, 130, new Vector3(0, GW, 20)), "MangroveWater");
        var wm = LoadOrCreate($"{MatDir}/Water_Mangrove.mat", waterShader);
        wm.SetColor("_Shallow", Hex("#9a9070")); wm.SetColor("_Deep", Hex("#3a3e2c"));
        wm.SetFloat("_DepthScale", 1.1f); wm.SetFloat("_Clarity", 0.25f); wm.SetFloat("_Glint", 0.05f); wm.SetFloat("_Refraction", 0.02f);
        wm.SetFloat("_Reflect", 1f); wm.SetFloat("_Waves", 0.012f); wm.SetFloat("_FoamWidth", 0.04f); wm.SetVector("_Flow", new Vector4(0f, 0.04f, 0, 0));
        wm.SetVector("_RapidZone", Vector4.zero);
        var wr = water.AddComponent<MeshRenderer>(); wr.sharedMaterial = wm; wr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- ต้นโกงกาง ----------
        var barkMat = SurfMat("MangroveBark", Color.white, 0.12f, vertexColor: true, smooth: 0.18f, tex: "bark_brown_02", tiles: 1.4f, cull: CullMode.Off);
        var leafMat = SurfMat("MangroveLeaf", Color.white, 0f, sway: 0.05f, swayStart: 3f, vertexColor: true, smooth: 0.45f, cull: CullMode.Off);
        var treeMeshes = new Mesh[6];
        for (int i = 0; i < 6; i++) treeMeshes[i] = SaveMesh(ProceduralMeshes.Mangrove(300 + i, i == 5 ? 10f : Rf(5.5f, 8f), i < 4 || i == 5), "Mangrove" + i);
        var forest = new GameObject("Mangroves").transform;
        void Tree(int kind, float x, float z, float scale, float yaw)
        {
            var g = new GameObject(kind == 4 ? "DeadMangrove" : "Mangrove"); g.transform.SetParent(forest, true);
            g.transform.SetPositionAndRotation(new Vector3(x, Mathf.Max(MangroveGround(x, z), GW - 0.4f), z), Quaternion.Euler(0, yaw, 0)); g.transform.localScale = Vector3.one * scale;
            g.AddComponent<MeshFilter>().sharedMesh = treeMeshes[kind];
            g.AddComponent<MeshRenderer>().sharedMaterials = new[] { barkMat, leafMat };
            world.colliders.Add(new Vector4(x, z, 0.9f * scale, 99));
        }
        // ต้นใหญ่โน้มข้ามร่องน้ำเป็นซุ้ม (ใช้ต้นสูงสุด)
        foreach (var (z, side) in new[] { (7f, -1f), (15f, 1f), (26f, -1f), (35f, 1f) }) Tree(5, GXc(z) + side * (GHw(z) + 0.4f), z, Rf(1f, 1.2f), Rf(0, 360));
        for (int t = 0, n = 0; t < 600 && n < 46; t++)
        {
            float z = Rf(-16, 56), side = rnd.NextDouble() < 0.5 ? -1 : 1, x = GXc(z) + side * (GHw(z) + Rf(-0.4f, 9f));
            if (Mathf.Abs(x - spX) < 4f && Mathf.Abs(z) < 5f) continue;    // เว้นลานเลนที่ผู้เล่นยืน
            bool tooClose = false;
            foreach (var c in world.colliders) if ((new Vector2(c.x - x, c.y - z)).sqrMagnitude < 9f) { tooClose = true; break; }
            if (tooClose) continue;
            Tree(rnd.NextDouble() < 0.25 ? 4 : rnd.Next(4), x, z, Rf(0.8f, 1.25f), Rf(0, 360)); n++;
        }

        // ---------- ซากเรือติดราก + แผ่นไม้แตก ----------
        var props = new GameObject("Wrecks").transform;
        var boatMat = SurfMat("HBBoat", Hex("#8a6e52"), 0.05f, smooth: 0.25f, tex: "weathered_brown_planks", tiles: 0.9f, cull: CullMode.Off);
        var plank = SurfMat("HBPlank", Hex("#c0a888"), 0.04f, smooth: 0.12f, tex: "weathered_brown_planks", tiles: 0.7f);
        var wreckMesh = SaveMesh(ProceduralMeshes.Rowboat(4.4f, 1.3f, 0.5f), "MangroveWreck");
        foreach (var (x, z, rot) in new[] { (GXc(12) - GHw(12) - 0.6f, 12f, new Vector3(-8, 20, 28)), (GXc(21) + GHw(21) + 0.5f, 21f, new Vector3(10, -150, -24)), (GXc(-6) - GHw(-6) - 1.5f, -6f, new Vector3(4, 70, 16)) })
        {
            var g = new GameObject("Wreck"); g.transform.SetParent(props, true);
            g.transform.SetPositionAndRotation(new Vector3(x, MangroveGround(x, z) - 0.15f, z), Quaternion.Euler(rot));
            g.AddComponent<MeshFilter>().sharedMesh = wreckMesh; g.AddComponent<MeshRenderer>().sharedMaterial = boatMat;
            for (int k = 0; k < 3; k++)
            {
                var p = Prim(PrimitiveType.Cube, "BrokenPlank", new Vector3(x + Rf(-1.5f, 1.5f), MangroveGround(x, z) + 0.02f, z + Rf(-1.5f, 1.5f)), new Vector3(Rf(0.8f, 1.6f), 0.04f, 0.18f), plank);
                p.transform.rotation = Quaternion.Euler(Rf(-8, 8), Rf(0, 360), Rf(-8, 8)); p.transform.SetParent(props, true);
            }
            world.colliders.Add(new Vector4(x, z, 1.6f, 99));
        }

        // ---------- แหเก่าห้อยตามรากโกงกาง ----------
        var netMat = LoadOrCreate($"{MatDir}/MangroveNet.mat", Shader.Find("Universal Render Pipeline/Lit"));
        netMat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Textures/Mangrove/net.png"));
        netMat.SetColor("_BaseColor", Hex("#c8bca0")); netMat.SetFloat("_Smoothness", 0.05f); netMat.SetFloat("_Metallic", 0);
        netMat.SetFloat("_AlphaClip", 1); netMat.SetFloat("_Cutoff", 0.4f); netMat.EnableKeyword("_ALPHATEST_ON");
        netMat.SetFloat("_Cull", 0); netMat.SetFloat("_DoubleSidedGI", 1);
        netMat.renderQueue = (int)RenderQueue.AlphaTest; netMat.SetOverrideTag("RenderType", "TransparentCutout");
        EditorUtility.SetDirty(netMat);
        int ni = 0;
        foreach (var (x, z, y, yaw, w, h) in new[] { (GXc(10) - GHw(10) - 1.5f, 10f, 1.9f, 80f, 2.4f, 1.6f), (GXc(4) - GHw(4) - 1.2f, 4f, 2.1f, 100f, 2f, 1.4f), (GXc(19) + GHw(19) + 1.3f, 19f, 2f, -70f, 2.6f, 1.7f), (GXc(30) + GHw(30) + 1f, 30f, 2.2f, -95f, 2.2f, 1.5f) })
        {
            var g = new GameObject("OldNet"); g.transform.SetParent(props, true);
            g.transform.SetPositionAndRotation(new Vector3(x, MangroveGround(x, z) + y, z), Quaternion.Euler(0, yaw, 0));
            g.AddComponent<MeshFilter>().sharedMesh = SaveMesh(NetMesh(40 + ni, w, h), "MangroveNet" + ni++);
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = netMat; r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- รากหายใจ (แท่งเล็ก ๆ โผล่เต็มลานเลน) ----------
        {
            var ci = new List<CombineInstance>();
            var spike = ProceduralMeshes.Lathe(new[] { new Vector2(0.025f, -0.1f), new Vector2(0.02f, 0.1f), new Vector2(0.012f, 0.22f), new Vector2(0.004f, 0.3f) }, 5, new Color(0.42f, 0.37f, 0.3f));
            for (int t = 0, n = 0; t < 8000 && n < 1100; t++)
            {
                float x = Rf(-22, 22), z = Rf(-14, 50), h = MangroveGround(x, z);
                if (h < GW - 0.08f || h > GW + 0.5f || Mathf.PerlinNoise(x * 0.25f + 3, z * 0.25f) < 0.45f) continue;
                if ((new Vector2(x - spX, z)).sqrMagnitude < 4f) continue;
                float s = Rf(0.6f, 1.5f);
                ci.Add(new CombineInstance { mesh = spike, transform = Matrix4x4.TRS(new Vector3(x, h, z), Quaternion.Euler(Rf(-12, 12), Rf(0, 360), Rf(-12, 12)), new Vector3(s, s * Rf(0.8f, 1.4f), s)) });
                n++;
            }
            var pm = new Mesh { indexFormat = IndexFormat.UInt32 }; pm.CombineMeshes(ci.ToArray(), true, true);
            var g = new GameObject("Pneumatophores");
            g.AddComponent<MeshFilter>().sharedMesh = SaveMesh(pm, "MangrovePneumatophores");
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = SurfMat("MangroveKnee", Color.white, 0.1f, vertexColor: true, smooth: 0.3f, tex: "bark_brown_02", tiles: 3f); r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- ป่าไกล + กิ่งไม้ล้มในเลน ----------
        var natureRoot = new GameObject("Nature").transform;
        GameObject Nature(string file, float x, float z, float scale, float yaw, float sink = 0.1f, float? y = null)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/{file}.glb");
            if (src == null) return null;
            var go = Object.Instantiate(src, natureRoot); go.name = file;
            go.transform.SetPositionAndRotation(new Vector3(x, (y ?? MangroveGround(x, z)) - sink, z), Quaternion.Euler(0, yaw, 0)); go.transform.localScale = Vector3.one * scale;
            foreach (var rr in go.GetComponentsInChildren<Renderer>())
            {
                var mats = rr.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = LeafMat(file, mats[i]) ?? mats[i]; rr.sharedMaterials = mats;
            }
            return go;
        }
        for (int i = 0; i < 18; i++)
        {
            float z = Rf(-10, 58), side = rnd.NextDouble() < 0.5 ? -1 : 1;
            Nature("island_tree_01", GXc(z) + side * (GHw(z) + Rf(10f, 17f)), z, Rf(1.6f, 2.2f), Rf(0, 360), 0.2f);
        }
        foreach (var (x, z, yaw) in new[] { (GXc(2) - GHw(2) - 2f, 2f, 30f), (GXc(9) + GHw(9) + 2.5f, 9f, -60f), (GXc(-3) + GHw(-3) + 3f, -3f, 110f) })
        {
            Nature("dead_tree_trunk", x, z, 1.4f, yaw, 0.05f);   // ขอนล้มนอนในเลน
        }

        // หมอกจางลอยต่ำเหนือร่องน้ำ
        Particles("MangroveMist", new Vector3(GXc(15), GW + 0.4f, 15), new Vector3(14f, 44f, 0.1f), ParticleMat("Spray", new Color(1, 1, 1, 0.5f)), 16, new Vector2(3f, 6f), new Vector2(7f, 10f), new Vector2(0.03f, 0.1f), new Color(0.85f, 0.9f, 0.88f, 0.1f));

        // ---------- ตัวละคร กล้อง แสง ระบบเกม ----------
        var look = new Look { sun = "#d4d8cc", sunIntensity = 1.35f, sunRot = new Vector2(52, 150), fog = "#8e9a90", fogStart = 12, fogEnd = 75, exposure = 0.95f, ambient = 1.15f, skyId = "Overcast", skyTint = "#76808a" };
        BuildGameplay(world, model, world.spawn, look, world);
        var foliage = Object.FindFirstObjectByType<FoliageRenderer>(); if (foliage != null) foliage.maxGrass = 0;   // ลานเลนไม่มีหญ้า
        SaveStage(scene, path);
    }
}
