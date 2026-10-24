// ด่าน: ถ้ำลอดรอยจารึก — ลำธารใต้ดินไหลลอดภูเขาหินปูน อุโมงค์หินโค้งสูง หินย้อยห้อยจากเพดาน เสาหินกลางน้ำ
//      ผนังสองข้างสลักอักษรโบราณกับภาพปลา/ดวงตา/คน · ราเรืองแสงสีเขียวเกาะผนัง · แสงสีฟ้าวิบวับบนผิวน้ำ · หยดน้ำจากเพดาน
//      ผู้เล่นยืนบนเรือไม้ลำเล็กหันหัวเข้าไปในถ้ำ มีตะเกียงดวงเดียวส่องรอยจารึก
//      หินงอกหินย้อยรูปทรงบิดเบี้ยวเป็นกลุ่ม ม่านหินปูนย้อยถึงน้ำ ผิวเปียกมันสะท้อนไฟ · รอยจารึกเป็นภาพเขียนสีดินแดง/รอยขูดที่หลุดร่อน มีเขม่าคบเพลิง
//      น้ำใสลึก ระลอกจากหยดน้ำ · หนอนเรืองแสงสีฟ้าตามซอกและเพดาน · แสงจากปากถ้ำไกล ๆ ลำแสงลอดรอยแยกเพดาน · ค้างคาว · ซากหินสลักจมน้ำ
using System.Collections.Generic;
using System.IO;
using CozyFishing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class SceneBuilder
{
    const float CW = -0.3f, CBase = CW - 0.8f;
    static float CXc(float z) => 1.2f * Mathf.Sin(z * 0.09f);                                  // แนวกลางลำธาร (อุโมงค์ทอดไปตามแกน z)
    static float CHw(float z) => CRs(z) - 2.6f + 0.5f * Mathf.Sin(z * 0.17f + 1f);              // ครึ่งความกว้างลำน้ำ (น้ำเกือบเต็มอุโมงค์ เหลือหิ้งหินแคบ ๆ ริมผนัง)
    // โถงถ้ำกว้างสูงตรงที่จอดเรือ (กล้องมุมสูงต้องอยู่ใต้เพดาน) แล้วแคบลงเป็นอุโมงค์ลอดลึกเข้าไปในความมืด
    static float CRs(float z) => Mathf.Lerp(10f, 7.5f, SStep(4, 22, z)) + 1.2f * Mathf.PerlinNoise(z * 0.06f, 5.3f);   // ครึ่งความกว้างอุโมงค์
    static float CRy(float z) => Mathf.Lerp(15.5f, 8.5f, SStep(6, 24, z)) + 1.6f * Mathf.PerlinNoise(z * 0.05f, 9.1f);  // ความสูงเพดาน

    static float CaveGround(float x, float z)
    {
        float n = Mathf.PerlinNoise(x * 0.3f + 13, z * 0.3f + 7) - 0.5f, n2 = Mathf.PerlinNoise(x * 0.9f + 3, z * 0.9f + 29) - 0.5f;
        float s = Mathf.Abs(x - CXc(z)), d = s / CHw(z);
        float bed = CW - 0.7f - 1.7f * Mathf.Clamp01(1 - d * d) + n2 * 0.3f;
        float ledge = CW + 0.2f + n * 0.25f + n2 * 0.1f + Mathf.Max(0, s - (CRs(z) - 2.2f)) * 1.6f;
        return Mathf.Lerp(bed, ledge, SStep(0.85f, 1.12f, d));
    }

    // ผิวหินของอุโมงค์: φ = 0 ผนังขวา (+x) → π/2 เพดาน → π ผนังซ้าย · ขรุขระด้วย noise หลายชั้น + ร่องหินปูนแนวตั้ง
    static float CaveRough(float z, float p) =>
        1.0f * Mathf.PerlinNoise(z * 0.22f + 3, p * 2.2f) + 0.45f * Mathf.PerlinNoise(z * 0.7f + 11, p * 6.5f)
        + 0.22f * Mathf.PerlinNoise(z * 1.9f, p * 15f) + 0.35f * Mathf.PerlinNoise(z * 1.3f, p * 0.8f + 40);
    static Vector3 ShellP(float z, float p)
    {
        float d = CaveRough(z, p);
        return new Vector3(CXc(z) + (CRs(z) - d) * Mathf.Cos(p), CBase + (CRy(z) - d * 0.8f) * Mathf.Sin(p), z);
    }
    static Vector3 ShellN(float z, float p)   // ชี้เข้าในอุโมงค์
    {
        var a = ShellP(z, p);
        var n = Vector3.Cross(ShellP(z + 0.05f, p) - a, ShellP(z, p + 0.01f) - a).normalized;
        var toAxis = new Vector3(CXc(z), CBase + 3f, z) - a;
        return Vector3.Dot(n, toAxis) < 0 ? -n : n;
    }

    // แผ่นผิวหินตามรูปอุโมงค์ (ใช้ทั้งตัวอุโมงค์และแผ่นรอยจารึก) · หน้าหันเข้าในอุโมงค์เสมอ
    static Mesh ShellPatch(float z0, float z1, float p0, float p1, int nzs, int nps, float lift, bool glyphUV, bool rightWall, System.Func<Vector3, Color> tint)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var col = new List<Color>(); var tri = new List<int>();
        for (int j = 0; j <= nzs; j++)
            for (int i = 0; i <= nps; i++)
            {
                float tz = j / (float)nzs, tp = i / (float)nps, z = Mathf.Lerp(z0, z1, tz), p = Mathf.Lerp(p0, p1, tp);
                var pos = ShellP(z, p) + (lift != 0 ? ShellN(z, p) * lift : Vector3.zero);
                v.Add(pos); col.Add(tint != null ? tint(pos) : Color.white);
                uv.Add(glyphUV ? (rightWall ? new Vector2(1 - tz, tp) : new Vector2(tz, 1 - tp)) : new Vector2(tz, tp));
            }
        for (int j = 0; j < nzs; j++)
            for (int i = 0; i < nps; i++)
            {
                int a = j * (nps + 1) + i, b = a + nps + 1;
                tri.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
            }
        var m = new Mesh { indexFormat = IndexFormat.UInt32 };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetColors(col); m.SetTriangles(tri, 0); m.RecalculateNormals();
        // กลับด้านสามเหลี่ยมถ้า normal ชี้ออกนอกอุโมงค์
        int mid = (nzs / 2) * (nps + 1) + nps / 2;
        if (Vector3.Dot(m.normals[mid], ShellN(Mathf.Lerp(z0, z1, 0.5f), Mathf.Lerp(p0, p1, 0.5f))) < 0)
        {
            for (int k = 0; k < tri.Count; k += 3) (tri[k + 1], tri[k + 2]) = (tri[k + 2], tri[k + 1]);
            m.SetTriangles(tri, 0); m.RecalculateNormals();
        }
        m.RecalculateBounds();
        return m;
    }

    public static void CreateCave()
    {
        const string path = "Assets/Game/Scenes/Cave.unity";
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
        world.stageId = "cave"; world.waterY = CW;
        world.origin = new Vector2(-20, -20); world.cell = 0.5f; world.nx = 81; world.nz = 181;
        world.heights = new float[world.nx * world.nz];
        for (int j = 0; j < world.nz; j++) for (int i = 0; i < world.nx; i++) world.heights[j * world.nx + i] = CaveGround(world.origin.x + i * world.cell, world.origin.y + j * world.cell);
        world.walkBounds = new Rect(-8, -12, 16, 30);
        float boatX = CXc(0) + 2.4f, deckY = CW + 0.18f;
        world.spawn = new Vector3(boatX, deckY, 0); world.spawnYaw = 0;
        world.docks.Add(new Rect(boatX - 0.45f, -1.6f, 0.9f, 3.2f)); world.dockY.Add(deckY);
        world.camBounds = new Rect(-6, -9, 12, 24); world.camBias = new Vector2(CXc(8), 8); world.camBiasK = 0.3f;
        world.special = new Vector3(CXc(11) - 2.8f, CW, 11); world.specialRadius = 3f;   // ใต้ภาพสลักดวงตาฝั่งซ้าย

        // ---------- พื้นท้องลำธาร + น้ำ ----------
        var terrain = new GameObject("Terrain");
        var m = Grid(40, 90, 170, new Vector3(0, 0, 25));
        var v = m.vertices; var cols = new Color[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            float x = v[i].x, z = v[i].z, h = CaveGround(x, z);
            v[i].y = h;
            cols[i] = new Color(SStep(CW + 0.5f, CW + 0.02f, h), 0.5f + 0.4f * Mathf.PerlinNoise(x * 0.3f, z * 0.3f), SStep(CW + 0.05f, CW - 1.2f, h), 0);
        }
        m.vertices = v; m.colors = cols; m.RecalculateNormals(); m.RecalculateBounds();
        terrain.AddComponent<MeshFilter>().sharedMesh = SaveMesh(m, "CaveTerrain");
        var tr = terrain.AddComponent<MeshRenderer>();
        tr.sharedMaterial = TerrainMat("Cave", ("rocky_gravel", "brown_mud_02", "rock_face_04"), new Vector4(0.4f, 0.4f, 0.3f, 0.03f), "#8a8678", "#6a6a62");
        tr.shadowCastingMode = ShadowCastingMode.Off;
        var water = new GameObject("CaveWater");
        water.AddComponent<MeshFilter>().sharedMesh = SaveMesh(Grid(40, 90, 120, new Vector3(0, CW, 25)), "CaveWater");
        var wm = LoadOrCreate($"{MatDir}/Water_Cave.mat", waterShader);
        wm.SetColor("_Shallow", Hex("#3c8c8a")); wm.SetColor("_Deep", Hex("#071e26"));
        wm.SetFloat("_DepthScale", 2.6f); wm.SetFloat("_Clarity", 0.92f); wm.SetFloat("_Glint", 0.12f); wm.SetFloat("_Refraction", 0.03f);
        wm.SetFloat("_Reflect", 1f); wm.SetFloat("_Waves", 0.012f); wm.SetFloat("_FoamWidth", 0.04f); wm.SetVector("_Flow", new Vector4(0f, 0.08f, 0, 0));
        wm.SetVector("_RapidZone", Vector4.zero);
        var wr = water.AddComponent<MeshRenderer>(); wr.sharedMaterial = wm; wr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- อุโมงค์หินปูน ----------
        var rockMat = SurfMat("CaveRock", Color.white, 0.05f, vertexColor: true, smooth: 0.12f, tex: "rock_face_04", tiles: 0.3f);
        Color RockTint(Vector3 p)
        {
            float wet = SStep(CW + 1.6f, CW - 0.1f, p.y), n = Mathf.PerlinNoise(p.z * 0.4f + 2, p.y * 0.5f + 8);
            var c = Color.Lerp(Hex("#d0c6ae"), Hex("#5c6662"), wet) * (0.85f + 0.35f * n);
            c.a = 1; return c;
        }
        var shell = new GameObject("CaveShell");
        shell.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ShellPatch(-18, 70, -0.14f, Mathf.PI + 0.14f, 176, 84, 0, false, false, RockTint), "CaveShell");
        var shr = shell.AddComponent<MeshRenderer>(); shr.sharedMaterial = rockMat; shr.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- รอยจารึกบนผนัง (แผ่นบางแนบผิวหิน) ----------
        Material GlyphMat(string id)
        {
            var gm = LoadOrCreate($"{MatDir}/CaveGlyphs_{id}.mat", Shader.Find("Universal Render Pipeline/Lit"));
            gm.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Game/Textures/Cave/glyphs_{id}.png"));
            gm.SetColor("_BaseColor", Color.white); gm.SetFloat("_Smoothness", 0.15f); gm.SetFloat("_Metallic", 0);
            gm.SetFloat("_AlphaClip", 1); gm.SetFloat("_Cutoff", 0.5f); gm.EnableKeyword("_ALPHATEST_ON");
            gm.EnableKeyword("_EMISSION"); gm.SetTexture("_EmissionMap", gm.GetTexture("_BaseMap")); gm.SetColor("_EmissionColor", new Color(0.16f, 0.09f, 0.05f));   // ภาพเขียนสีดิน: เรืองแค่นิดเดียวพอมองเห็นในความมืด
            gm.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            gm.renderQueue = (int)RenderQueue.AlphaTest; gm.SetOverrideTag("RenderType", "TransparentCutout");
            EditorUtility.SetDirty(gm); return gm;
        }
        var glyphA = GlyphMat("a"); var glyphB = GlyphMat("b");
        var glyphRoot = new GameObject("Inscriptions").transform;
        int gi = 0;
        foreach (var (z0, right, mat) in new[] { (0.5f, true, glyphA), (7.5f, true, glyphB), (15f, true, glyphA), (-1.5f, false, glyphB), (9f, false, glyphA), (17f, false, glyphB), (25f, true, glyphB), (27f, false, glyphA) })
        {
            float p0 = right ? 0.12f : Mathf.PI - 0.47f, p1 = right ? 0.47f : Mathf.PI - 0.12f;
            var g = new GameObject("Inscription"); g.transform.SetParent(glyphRoot, false);
            g.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ShellPatch(z0, z0 + 4.4f, p0, p1, 22, 12, 0.05f, true, right, null), "CaveGlyph" + gi++);
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- หินย้อย หินงอก เสาหิน ----------
        var spikeRoot = new GameObject("Speleothems").transform;
        var spikeMat = SurfMat("CaveSpike", Color.white, 0f, vertexColor: true, smooth: 0.55f, tex: "rock_face_04", tiles: 0.6f, cull: CullMode.Off);   // ผิวเปียกมันสะท้อนไฟ
        int spikeSeed = 0;
        void Spike(Vector3 at, float len, bool down, float r0, Color c)
        {
            // กลุ่มหิน: แท่งหลัก + แท่งเล็ก 1–3 แท่งเกาะข้าง ๆ (ไม่เรียงเป็นระเบียบ)
            int extra = rnd.Next(0, 4);
            for (int e = 0; e <= extra; e++)
            {
                float k = e == 0 ? 1 : Rf(0.3f, 0.65f);
                var off = e == 0 ? Vector3.zero : new Vector3(Rf(-1, 1), 0, Rf(-1, 1)).normalized * r0 * Rf(1.1f, 2.2f);
                var g = new GameObject("Spike"); g.transform.SetParent(spikeRoot, false);
                g.transform.SetPositionAndRotation(at + off, Quaternion.Euler(Rf(-5, 5), Rf(0, 360), Rf(-5, 5)));
                g.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Speleothem(1000 + spikeSeed++, len * k, r0 * Mathf.Sqrt(k), down, c, Color.Lerp(c, Hex("#f4ecd8"), 0.4f));
                g.AddComponent<MeshRenderer>().sharedMaterial = spikeMat;
            }
        }
        for (int t = 0, n = 0; t < 600 && n < 170; t++)
        {
            float z = Rf(-16, 62), p = Rf(0.55f, Mathf.PI - 0.55f);
            if (z < 6 && p > 1f && p < Mathf.PI - 1f) continue;   // เพดานโถงเหนือกล้อง: ไม่ให้หินย้อยบังตัวละคร
            float up = Mathf.Sin(p), len = Rf(0.3f, 1f) * (0.6f + 2.2f * up * up) * (rnd.NextDouble() < 0.12 ? 1.8f : 1f) * (z < 6 ? 0.6f : 1f);
            n++;
            Spike(ShellP(z, p) + Vector3.up * 0.25f, len, true, Mathf.Max(0.06f, len * Rf(0.12f, 0.2f)), Color.Lerp(Hex("#f0e4c8"), Hex("#c0b49c"), Rf(0, 1)));
        }
        // หินงอกโผล่พ้นน้ำริมลำธาร (เว้นทางเหวี่ยงเบ็ดหน้าเรือ)
        for (int t = 0, n = 0; t < 300 && n < 26; t++)
        {
            float z = Rf(-12, 50), side = rnd.NextDouble() < 0.5 ? -1 : 1, x = CXc(z) + side * CHw(z) * Rf(0.65f, 1.05f);
            if (z > -3 && z < 14 && Mathf.Abs(x - boatX) < 3.5f) continue;
            float len = Rf(0.6f, 2.4f);
            Spike(new Vector3(x, CaveGround(x, z), z), len + (CW - CaveGround(x, z)), false, Rf(0.18f, 0.4f), Color.Lerp(Hex("#e8dcc0"), Hex("#b0a890"), Rf(0, 1)));
            n++;
        }
        // เสาหินจากพื้นจรดเพดาน (หินย้อยกับหินงอกมาบรรจบกัน)
        foreach (var (x, z) in new[] { (CXc(19f) - 3.2f, 19f), (CXc(31f) + 3.4f, 31f), (CXc(-8f) - 3.6f, -8f) })
        {
            float top = CBase + CRy(z) - 1f, bot = CaveGround(x, z) - 0.3f, h = top - bot;
            var prof = new[] { new Vector2(1.1f, 0), new Vector2(0.75f, h * 0.15f), new Vector2(0.42f, h * 0.45f), new Vector2(0.32f, h * 0.55f), new Vector2(0.6f, h * 0.85f), new Vector2(1.3f, h) };
            var g = new GameObject("Column"); g.transform.SetParent(spikeRoot, false); g.transform.position = new Vector3(x, bot, z);
            g.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Lathe(prof, 14, Hex("#bcb096"), 0.4f);
            g.AddComponent<MeshRenderer>().sharedMaterial = spikeMat;
            world.colliders.Add(new Vector4(x, z, 1f, 99));
        }
        foreach (var (z, side) in new[] { (12f, -1f), (20f, 1f), (27f, -1f), (-5f, 1f), (35f, 1f) })
        {
            float p = side > 0 ? 0.42f : Mathf.PI - 0.42f; var top = ShellP(z, p) + Vector3.up * 0.3f;
            float len = top.y - CW + 0.2f;
            for (int k = 0; k < 4; k++)
            {
                var g = new GameObject("Flowstone"); g.transform.SetParent(spikeRoot, false);
                g.transform.SetPositionAndRotation(top + new Vector3(0, 0, (k - 1.5f) * 0.45f), Quaternion.Euler(0, Rf(-15, 15), 0));
                g.transform.localScale = new Vector3(0.45f, 1, 1.6f);
                g.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Speleothem(2000 + spikeSeed++, len * Rf(0.75f, 1f), Rf(0.35f, 0.55f), true, Hex("#d8ccb0"), Hex("#a89c80"));
                g.AddComponent<MeshRenderer>().sharedMaterial = spikeMat;
            }
        }
        CombineGroup(spikeRoot, "CaveSpeleothems");
        foreach (var r in spikeRoot.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = ShadowCastingMode.Off;

        // ---------- ราเรืองแสงเกาะผนังช่วงล่าง (จุดเล็ก ๆ แนบผิวหิน เป็นกลุ่ม ๆ) ----------
        {
            var gv = new List<Vector3>(); var gt = new List<int>(); var gc = new List<Color>();
            for (int t = 0, n = 0; t < 60000 && n < 4500; t++)
            {
                float z = Rf(-14, 45), u = (float)rnd.NextDouble(), p = u < 0.35 ? Rf(-0.02f, 0.85f) : u < 0.7 ? Rf(Mathf.PI - 0.85f, Mathf.PI + 0.02f) : Rf(1.0f, Mathf.PI - 1.0f);   // ผนังล่าง + เพดาน (หนอนเรืองแสงห้อย)
                if (Mathf.PerlinNoise(z * 0.35f + 4, p * 3.2f + 1) < 0.6f) continue;
                var pos = ShellP(z, p); var nrm = ShellN(z, p);
                if (pos.y < CW + 0.05f) continue;
                pos += nrm * 0.04f;
                var t1 = Vector3.Cross(nrm, Vector3.up).normalized; if (t1.sqrMagnitude < 0.1f) t1 = Vector3.right;
                var t2 = Vector3.Cross(nrm, t1);
                float s = Rf(0.008f, 0.028f);
                int b = gv.Count;
                gv.Add(pos - t1 * s - t2 * s); gv.Add(pos + t1 * s - t2 * s); gv.Add(pos + t1 * s + t2 * s); gv.Add(pos - t1 * s + t2 * s);
                var c = Color.Lerp(new Color(0.35f, 0.8f, 1f), new Color(0.55f, 1f, 0.8f), Rf(0, 1)); for (int k = 0; k < 4; k++) gc.Add(c);
                gt.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3, b, b + 2, b + 1, b, b + 3, b + 2 });   // สองหน้า
                n++;
            }
            var gm = new Mesh { indexFormat = IndexFormat.UInt32 };
            gm.SetVertices(gv); gm.SetColors(gc); gm.SetTriangles(gt, 0); gm.RecalculateNormals(); gm.RecalculateBounds();
            var g = new GameObject("GlowMoss");
            g.AddComponent<MeshFilter>().sharedMesh = SaveMesh(gm, "CaveGlowMoss");
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = UnlitMat("CaveGlow", new Color(0.45f, 1.1f, 1.5f)); r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- เรือไม้ลำเล็กของผู้เล่น + ตะเกียง ----------
        var root = new GameObject("Props").transform;
        GameObject Obj(string name, Mesh mesh, Material[] mats, Vector3 p, float yaw)
        {
            var g = new GameObject(name); g.transform.SetParent(root, true);
            g.transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterials = mats; return g;
        }
        var boatMat = SurfMat("HBBoat", Hex("#8a6e52"), 0.05f, smooth: 0.25f, tex: "weathered_brown_planks", tiles: 0.9f, cull: CullMode.Off);
        var plank = SurfMat("HBPlank", Hex("#c0a888"), 0.04f, smooth: 0.12f, tex: "weathered_brown_planks", tiles: 0.7f);
        var boat = Obj("PlayerBoat", SaveMesh(ProceduralMeshes.Rowboat(3.4f, 1.0f, 0.35f), "Rowboat"), new[] { boatMat }, new Vector3(boatX, CW + 0.12f, 0), 0);
        for (int k = -1; k <= 1; k++)
        {
            var th = Prim(PrimitiveType.Cube, "Thwart", Vector3.zero, new Vector3(0.85f, 0.04f, 0.2f), plank);
            th.transform.SetParent(boat.transform, false); th.transform.localPosition = new Vector3(0, 0.03f, k * 0.95f);
        }
        var box = Prim(PrimitiveType.Cube, "SupplyBox", new Vector3(boatX + 0.05f, deckY + 0.14f, -1.15f), new Vector3(0.5f, 0.26f, 0.36f), plank); box.transform.SetParent(root, true);
        box.transform.rotation = Quaternion.Euler(0, 8, 0);
        Obj("FishBasket", SaveMesh(ProceduralMeshes.FishBasket(), "CaveFishBasket"), new[] { SurfMat("CaveBasket", Color.white, 0.1f, vertexColor: true, smooth: 0.2f) }, new Vector3(boatX + 0.25f, deckY, 0.6f), 30);
        Obj("Bucket", SaveMesh(ProceduralMeshes.Bucket(), "CaveBucket"), new[] { SurfMat("CaveBucketMat", Color.white, 0f, vertexColor: true, smooth: 0.55f) }, new Vector3(boatX - 0.22f, deckY, -0.55f), 0);
        var lanternPos = new Vector3(boatX - 0.25f, deckY + 0.02f, 1.25f);
        var lsrc = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/brass_diya_lantern.glb");
        if (lsrc != null) { var l = Object.Instantiate(lsrc); l.transform.position = lanternPos; l.transform.localScale = Vector3.one * 1.7f; }
        var lg = new GameObject("LanternLight").AddComponent<Light>();
        lg.type = LightType.Point; lg.range = 14f; lg.intensity = 12f; lg.color = Hex("#ffb060"); lg.shadows = LightShadows.None;
        lg.transform.position = lanternPos + Vector3.up * 1.2f;
        // แสงเรืองจากกลุ่มราบนผนัง + แสงฟ้าจากน้ำลึกปลายอุโมงค์
        foreach (var (p, c, range, inten) in new[] { (new Vector3(CXc(10) - 5.5f, CW + 1.8f, 10), "#58c8ff", 8f, 1.3f), (new Vector3(CXc(4) + 6f, CW + 2.2f, 4), "#70e0d0", 7f, 1.0f), (new Vector3(CXc(22), CW + 0.4f, 22), "#40c8ff", 12f, 1.8f) })
        {
            var gl = new GameObject("GlowLight").AddComponent<Light>();
            gl.type = LightType.Point; gl.range = range; gl.intensity = inten; gl.color = Hex(c); gl.shadows = LightShadows.None; gl.transform.position = p;
        }

        // ---------- แสงจากปากถ้ำปลายอุโมงค์ (ไกล ๆ ลอดไอหมอก) ----------
        {
            float ez = 64f; var ec = new Vector3(CXc(ez), CW + 3f, ez);
            var mouth = Prim(PrimitiveType.Quad, "CaveMouthLight", ec, new Vector3(9f, 7f, 1f), UnlitMat("CaveMouth", new Color(1.6f, 1.75f, 1.8f)));
            mouth.transform.rotation = Quaternion.LookRotation(Vector3.forward); mouth.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var el = new GameObject("MouthLight").AddComponent<Light>(); el.type = LightType.Spot; el.transform.position = ec + Vector3.back * 2; el.transform.rotation = Quaternion.LookRotation(Vector3.back + Vector3.down * 0.15f);
            el.range = 40; el.spotAngle = 50; el.intensity = 6; el.color = Hex("#cfe6ff"); el.shadows = LightShadows.None;
        }
        // ลำแสงลอดรอยแยกเพดาน: กรวยโปร่งแสงจางลงล่าง ตกกระทบผิวน้ำเป็นจุด
        Material shaftMat = ParticleMat("CaveShaft", new Color(0.85f, 0.95f, 1f, 0.16f));
        foreach (var (sx, sz) in new[] { (CXc(13f) - 1.5f, 13f), (CXc(24f) + 1.2f, 24f) })
        {
            float topY = CBase + CRy(sz) - 1.2f, h = topY - CW;
            var cv = new List<Vector3>(); var cc = new List<Color>(); var ct = new List<int>();
            const int seg = 16;
            for (int k = 0; k <= seg; k++)
            {
                float a = k / (float)seg * Mathf.PI * 2;
                cv.Add(new Vector3(Mathf.Cos(a) * 0.5f, h, Mathf.Sin(a) * 0.5f)); cc.Add(new Color(1, 1, 1, 0.9f));
                cv.Add(new Vector3(Mathf.Cos(a) * 1.9f + 0.6f, 0, Mathf.Sin(a) * 1.9f)); cc.Add(new Color(1, 1, 1, 0f));
                if (k < seg) { int i0 = k * 2; ct.AddRange(new[] { i0, i0 + 2, i0 + 1, i0 + 1, i0 + 2, i0 + 3, i0, i0 + 1, i0 + 2, i0 + 1, i0 + 3, i0 + 2 }); }
            }
            var sm = new Mesh(); sm.SetVertices(cv); sm.SetColors(cc); sm.SetTriangles(ct, 0); sm.RecalculateBounds();
            var sg = new GameObject("LightShaft"); sg.transform.position = new Vector3(sx, CW, sz);
            sg.AddComponent<MeshFilter>().sharedMesh = SaveMesh(sm, "CaveShaft" + (int)sz);
            var srr = sg.AddComponent<MeshRenderer>(); srr.sharedMaterial = shaftMat; srr.shadowCastingMode = ShadowCastingMode.Off;
            var spot = new GameObject("ShaftLight").AddComponent<Light>(); spot.type = LightType.Spot; spot.transform.position = new Vector3(sx, topY, sz); spot.transform.rotation = Quaternion.LookRotation(Vector3.down);
            spot.range = h + 3; spot.spotAngle = 40; spot.intensity = 5; spot.color = Hex("#d8ecff"); spot.shadows = LightShadows.None;
        }
        // ซากหินสลักโบราณจมน้ำ: แท่นหิน ก้อนกลองเสาหักล้ม แผ่นหินสลัก
        {
            var ruin = SurfMat("CaveRuin", Hex("#8a8270"), 0.35f, smooth: 0.35f, tex: "rock_face_04", tiles: 0.8f);
            var ruinRoot = new GameObject("SunkenRuins").transform;
            foreach (var (z, side) in new[] { (17f, -1f), (22f, 1f), (30f, -1f), (38f, 1f), (6f, -1f) })
            {
                float x = CXc(z) + side * CHw(z) * Rf(0.55f, 0.8f), gy = CaveGround(x, z);
                for (int k = 0; k < 3; k++)
                {
                    var d = Prim(PrimitiveType.Cylinder, "ColumnDrum", new Vector3(x + k * 0.7f * side, gy + 0.25f, z + Rf(-0.4f, 0.4f)), new Vector3(0.7f, 0.3f, 0.7f), ruin);
                    d.transform.rotation = Quaternion.Euler(Rf(60, 95), Rf(0, 360), 0); d.transform.SetParent(ruinRoot, true);
                }
                var slab = Prim(PrimitiveType.Cube, "CarvedSlab", new Vector3(x - side * 0.6f, gy + 0.35f, z + 0.9f), new Vector3(1.2f, 0.9f, 0.18f), ruin);
                slab.transform.rotation = Quaternion.Euler(Rf(-25, -8), Rf(0, 360), Rf(-12, 12)); slab.transform.SetParent(ruinRoot, true);
            }
            foreach (var r in ruinRoot.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- ก้อนหินริมลำธาร ----------
        var natureRoot = new GameObject("Nature").transform;
        GameObject Nature(string file, float x, float z, float scale, float yaw, float collide = 0, float sink = 0.1f)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureDir}/{file}.glb");
            if (src == null) return null;
            var go = Object.Instantiate(src, natureRoot); go.name = file;
            go.transform.SetPositionAndRotation(new Vector3(x, CaveGround(x, z) - sink, z), Quaternion.Euler(0, yaw, 0)); go.transform.localScale = Vector3.one * scale;
            foreach (var rr in go.GetComponentsInChildren<Renderer>())
            {
                rr.shadowCastingMode = ShadowCastingMode.Off;
                var mats = rr.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = LeafMat(file, mats[i]) ?? mats[i]; rr.sharedMaterials = mats;
            }
            if (collide > 0) world.colliders.Add(new Vector4(x, z, collide, 99));
            return go;
        }
        for (int t = 0, n = 0; t < 200 && n < 14; t++)
        {
            float z = Rf(-12, 45), side = rnd.NextDouble() < 0.5 ? -1 : 1, x = CXc(z) + side * (CHw(z) + Rf(0.4f, 1.6f));
            if (Mathf.Abs(z) < 3f && side > 0) continue;
            Nature(new[] { "stone_01", "rock_moss_set_01", "boulder_01", "stone_01" }[n % 4], x, z, Rf(0.4f, 0.8f), Rf(0, 360), 0.4f, 0.15f); n++;
        }

        // ---------- หยดน้ำจากเพดาน · แสงวิบวับบนผิวน้ำ · ไอน้ำเย็น ----------
        Particles("Drips", new Vector3(CXc(16), CBase + 8.5f, 16), new Vector3(9f, 40f, 0.1f), ParticleMat("CaveDrip", new Color(0.85f, 0.95f, 1f, 0.8f)), 7, new Vector2(0.02f, 0.035f), new Vector2(1.1f, 1.5f), new Vector2(0f, 0f), new Color(0.8f, 0.95f, 1f, 0.9f), 1f);
        var wg = Particles("WaterGlow", new Vector3(CXc(14), CW + 0.04f, 14), new Vector3(8f, 40f, 0.02f), ParticleMat("CaveWaterGlow", new Color(0.4f, 1.4f, 1.9f, 1)), 22, new Vector2(0.04f, 0.1f), new Vector2(3f, 6f), new Vector2(0f, 0.02f), new Color(0.5f, 1f, 1f, 1));
        var noise = wg.noise; noise.enabled = true; noise.strength = 0.15f; noise.frequency = 0.3f;
        Particles("CaveMist", new Vector3(CXc(12), CW + 0.5f, 12), new Vector3(12f, 44f, 1.2f), ParticleMat("Spray", new Color(1, 1, 1, 0.5f)), 30, new Vector2(3f, 7f), new Vector2(7f, 11f), new Vector2(0.02f, 0.08f), new Color(0.62f, 0.78f, 0.86f, 0.1f));
        var rippleMat = ParticleMat("CaveRipple", new Color(0.8f, 0.92f, 1f, 0.55f)); rippleMat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Textures/Cave/ripple.png"));
        var rip = Particles("DripRipples", new Vector3(CXc(12), CW + 0.015f, 12), new Vector3(8f, 34f, 0.01f), rippleMat, 9, new Vector2(0.25f, 0.5f), new Vector2(1f, 1.6f), new Vector2(0f, 0f), new Color(0.85f, 0.95f, 1f, 0.6f));
        rip.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        var rsz = rip.sizeOverLifetime; rsz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0.2f, 1, 1.6f));
        var batMat = ParticleMat("CaveBat", new Color(0.12f, 0.1f, 0.1f, 1f)); batMat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Textures/Cave/bat.png"));
        var bats = Particles("Bats", new Vector3(CXc(18), CBase + 7f, 18), new Vector3(8f, 30f, 2f), batMat, 2.2f, new Vector2(0.22f, 0.4f), new Vector2(4f, 7f), new Vector2(1.5f, 3.2f), Color.white);
        var bn = bats.noise; bn.enabled = true; bn.strength = 1.8f; bn.frequency = 0.6f; var bsz = bats.sizeOverLifetime; bsz.enabled = false;
        var bcol = bats.colorOverLifetime; var bg = new Gradient(); bg.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.1f), new GradientAlphaKey(1, 0.9f), new GradientAlphaKey(0, 1) }); bcol.color = bg;

        // ---------- ตัวละคร กล้อง แสง ระบบเกม ----------
        var look = new Look { sun = "#a8c8d4", sunIntensity = 1.2f, sunRot = new Vector2(70, 170), fog = "#122028", fogStart = 14, fogEnd = 56, exposure = 1.0f, ambient = 1.15f, skyId = "Overcast", skyTint = "#2a3c44" };
        BuildGameplay(world, model, world.spawn, look, world);
        var foliage = Object.FindFirstObjectByType<FoliageRenderer>(); if (foliage != null) foliage.maxGrass = 0;   // ในถ้ำไม่มีหญ้า
        RenderSettings.reflectionIntensity = 0.15f;   // ผนังถ้ำไม่สะท้อนฟ้า (บนเว็บหินออกขาวเหมือนน้ำแข็ง)
        SaveStage(scene, path);
    }
}
