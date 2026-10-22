// ปลาจากภาพถ่ายจริง (Wikimedia Commons, ตัดพื้นหลังแล้ว) → โมเดล 3D พองตามรูปทรงในภาพ
//   Unity.exe -batchmode -projectPath . -executeMethod FishPhotoTools.Build -quit
// อ่าน Assets/Game/Resources/Fish/<id>.png → สร้าง <id>_mesh.asset (ยาว 1 หน่วย หัวอยู่ +X หางอยู่ -X ลำตัวหนาตรงกลางบางที่ขอบ)
// + วัสดุ FishPhoto.mat (CozyFishing/FishPhoto) · ภาพตั้งค่า import ให้คมและขอบโปร่งใสไม่มีขอบดำ
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FishPhotoTools
{
    const string Dir = "Assets/Game/Resources/Fish";

    public static void Build()
    {
        AssetDatabase.Refresh();
        var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/FishPhoto.mat");
        if (mat == null) { mat = new Material(Shader.Find("CozyFishing/FishPhoto")); AssetDatabase.CreateAsset(mat, $"{Dir}/FishPhoto.mat"); }
        int n = 0;
        foreach (var png in Directory.GetFiles(Dir, "*.png"))
        {
            var path = png.Replace('\\', '/');
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Default; ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp;
            ti.mipmapEnabled = true; ti.maxTextureSize = 512; ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.npotScale = TextureImporterNPOTScale.None;
            ti.SaveAndReimport();
            var tex = new Texture2D(2, 2); tex.LoadImage(File.ReadAllBytes(png));
            var mesh = Inflate(tex);
            var mp = $"{Dir}/{Path.GetFileNameWithoutExtension(png)}_mesh.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(mp);
            if (old != null) { old.Clear(); EditorUtility.CopySerialized(mesh, old); } else AssetDatabase.CreateAsset(mesh, mp);
            Object.DestroyImmediate(tex);
            n++;
        }
        BuildPondMaterials();
        AssetDatabase.SaveAssets();
        Debug.Log($"[FishPhotoTools] built {n} fish meshes");
    }

    // วัสดุสำหรับมุมมองใต้น้ำ (UnderwaterView) — อยู่ใน Resources เพราะสร้างตอนเล่น ไม่ได้อบลงฉาก
    static void BuildPondMaterials()
    {
        const string wd = "Assets/Game/Resources/Water";
        Directory.CreateDirectory(wd);
        Material Mat(string name, Shader sh)
        {
            var p = $"{wd}/{name}.mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); } else m.shader = sh;
            return m;
        }
        Mat("WaterUnder", Shader.Find("CozyFishing/WaterUnder"));
        var bait = Mat("Bait", Shader.Find("Universal Render Pipeline/Lit")); bait.SetColor("_BaseColor", new Color(0.85f, 0.42f, 0.42f)); bait.SetFloat("_Smoothness", 0.7f);
        var line = Mat("Leader", Shader.Find("Universal Render Pipeline/Unlit")); line.SetColor("_BaseColor", new Color(0.92f, 0.92f, 0.88f, 1));
        var src = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
        var mp = $"{wd}/Mote.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(mp) == null && src != null) AssetDatabase.CreateAsset(new Material(src), mp);
        foreach (var m in new[] { bait, line }) EditorUtility.SetDirty(m);
        // คันเบ็ด (HandIK สร้างตอนเล่น): ไม้ก๊อก · โลหะ · ตัวคันกราไฟต์
        Directory.CreateDirectory("Assets/Game/Resources/Rod");
        Material RodMat(string name, Color c, float smooth, float metal)
        {
            var p = $"Assets/Game/Resources/Rod/{name}.mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal); EditorUtility.SetDirty(m);
            return m;
        }
        RodMat("Cork", new Color(0.72f, 0.56f, 0.38f), 0.2f, 0);
        RodMat("Metal", new Color(0.75f, 0.76f, 0.78f), 0.8f, 0.9f);
        RodMat("Blank", new Color(0.08f, 0.09f, 0.1f), 0.75f, 0.2f);
    }

    // ตาราง N คอลัมน์ตามความยาว · จุดในภาพหนาตามระยะห่างจากขอบ (sqrt ให้ท้องป่องโค้งมน) · หน้า-หลังแยกจุดยอดกัน
    static Mesh Inflate(Texture2D tex)
    {
        const int N = 48;
        float aspect = tex.height / (float)tex.width;
        int M = Mathf.Max(8, Mathf.RoundToInt(N * aspect));
        float A(int i, int j) => tex.GetPixelBilinear(i / (float)N, j / (float)M).a;
        var inside = new bool[N + 1, M + 1];
        for (int i = 0; i <= N; i++) for (int j = 0; j <= M; j++) inside[i, j] = A(i, j) > 0.3f;
        // ระยะถึงขอบ (หน่วยช่องตาราง) แบบไล่ตรง ๆ — ตารางเล็ก
        var dist = new float[N + 1, M + 1]; float dmax = 0.01f;
        for (int i = 0; i <= N; i++)
            for (int j = 0; j <= M; j++)
            {
                if (!inside[i, j]) continue;
                float best = 99;
                for (int a = 0; a <= N; a++) for (int b = 0; b <= M; b++) if (!inside[a, b]) best = Mathf.Min(best, (a - i) * (a - i) * aspect * aspect * 0.25f + (b - j) * (b - j));
                dist[i, j] = Mathf.Sqrt(best); dmax = Mathf.Max(dmax, dist[i, j]);
            }
        float bodyH = aspect;                                   // ลำตัวยาว 1 หน่วย สูงตามสัดส่วนภาพ
        float thick = Mathf.Min(bodyH * 0.22f, 0.09f);
        var v = new System.Collections.Generic.List<Vector3>(); var uv = new System.Collections.Generic.List<Vector2>(); var tri = new System.Collections.Generic.List<int>();
        int[,] front = new int[N + 1, M + 1], back = new int[N + 1, M + 1];
        for (int side = 0; side < 2; side++)
            for (int i = 0; i <= N; i++)
                for (int j = 0; j <= M; j++)
                {
                    float u = i / (float)N, vv = j / (float)M;
                    float z = inside[i, j] ? thick * Mathf.Sqrt(dist[i, j] / dmax) : 0;
                    v.Add(new Vector3(0.5f - u, (vv - 0.5f) * bodyH, side == 0 ? z : -z));
                    uv.Add(new Vector2(u, vv));
                    if (side == 0) front[i, j] = v.Count - 1; else back[i, j] = v.Count - 1;
                }
        for (int i = 0; i < N; i++)
            for (int j = 0; j < M; j++)
            {
                if (!(inside[i, j] || inside[i + 1, j] || inside[i, j + 1] || inside[i + 1, j + 1])) continue;
                tri.AddRange(new[] { front[i, j], front[i, j + 1], front[i + 1, j], front[i, j + 1], front[i + 1, j + 1], front[i + 1, j] });   // หน้า +Z
                tri.AddRange(new[] { back[i, j], back[i + 1, j], back[i, j + 1], back[i, j + 1], back[i + 1, j], back[i + 1, j + 1] });     // หลัง -Z
            }
        var m = new Mesh { name = "FishPhoto" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
}
