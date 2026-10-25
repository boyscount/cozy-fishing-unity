using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// ภาพที่ฝังอยู่ในโมเดล .glb (Poly Haven) ตัวนำเข้า glTFast เก็บแบบไม่บีบอัด (RGB24/ARGB32) และตั้งค่าไม่ได้ → ไฟล์โหลดบวมหลายสิบ MB
// แก้: ดึงภาพออกเป็นไฟล์ PNG ในโปรเจกต์ (บีบอัด ASTC ตามปกติ) · วัสดุของ glb ทำสำเนาเป็นไฟล์ .mat ที่ชี้ภาพใหม่
//      แล้วสลับวัสดุในทุกฉาก → ภาพเดิมใน glb ไม่มีใครอ้างถึง ไม่ถูกรวมใน AssetBundle อีก
//   Unity.exe -batchmode -buildTarget WebGL -projectPath . -executeMethod GlbTextureBaker.BakeAllScenes -quit
public static class GlbTextureBaker
{
    const string TexOut = "Assets/Game/Textures/GLB", MatOut = "Assets/Game/Materials/GLB", MeshOut = "Assets/Game/Generated/GLB";

    public static void BakeAllScenes()
    {
        foreach (var path in Directory.GetFiles("Assets/Game/Scenes", "*.unity").Select(p => p.Replace('\\', '/')))
        {
            if (path.EndsWith("/Boot.unity")) continue;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int n = BakeScene();
            if (n > 0) EditorSceneManager.SaveScene(scene);
            Debug.Log($"[GlbBaker] {path}: {n} material slots");
        }
        AssetDatabase.SaveAssets();
    }

    /// สลับวัสดุ/ภาพจาก glb ในฉากที่เปิดอยู่ · คืนจำนวนช่องวัสดุที่เปลี่ยน
    public static int BakeScene()
    {
        Directory.CreateDirectory(TexOut); Directory.CreateDirectory(MatOut);
        int n = 0;
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var mats = r.sharedMaterials; bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m2 = Fix(mats[i]);
                if (m2 != mats[i]) { mats[i] = m2; changed = true; n++; }
            }
            if (changed) r.sharedMaterials = mats;
        }
        // mesh จาก glb → ไฟล์ mesh ของโปรเจกต์ (บีบอัดได้) · พอไม่มีฉากไหนอ้างถึงไฟล์ .glb ทั้งก้อน (รวมภาพไม่บีบอัดข้างใน) ก็ไม่ถูกใส่ใน bundle
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!FromGlb(mf.sharedMesh)) continue;
            mf.sharedMesh = ExtractMesh(mf.sharedMesh); n++;
        }
        return n;
    }

    static bool FromGlb(Object o) => o != null && AssetDatabase.GetAssetPath(o).ToLowerInvariant().EndsWith(".glb");

    static Material Fix(Material m)
    {
        if (m == null) return m;
        var target = m;
        if (FromGlb(m))
        {
            var glb = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(m));
            var path = $"{MatOut}/{glb}_{Safe(m.name)}.mat";
            target = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (target == null) { target = new Material(m); AssetDatabase.CreateAsset(target, path); }
            else { target.shader = m.shader; target.CopyPropertiesFromMaterial(m); }
        }
        bool dirty = target != m;
        foreach (var prop in target.GetTexturePropertyNames())
        {
            var tex = target.GetTexture(prop);
            if (!FromGlb(tex) || !(tex is Texture2D t2)) continue;
            target.SetTexture(prop, Extract(t2));
            dirty = true;
        }
        if (dirty) EditorUtility.SetDirty(target);
        return target;
    }

    static Mesh ExtractMesh(Mesh src)
    {
        var glb = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(src));
        Directory.CreateDirectory(MeshOut);
        var path = $"{MeshOut}/{glb}_{Safe(src.name)}_{src.vertexCount}.asset";
        var have = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (have != null) return have;
        var m = Object.Instantiate(src); m.name = src.name;
        MeshUtility.SetMeshCompression(m, ModelImporterMeshCompression.Medium);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static string Safe(string s) => new string(s.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_').ToArray());

    // ภาพจาก glb → PNG ในโปรเจกต์ (อ่านผ่าน RenderTexture เพราะภาพที่นำเข้าอาจอ่านพิกเซลตรง ๆ ไม่ได้)
    static Texture2D Extract(Texture2D src)
    {
        var glb = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(src));
        var path = $"{TexOut}/{glb}_{Safe(src.name)}.png";
        var have = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (have != null) return have;
        bool srgb = GraphicsFormatUtility.IsSRGBFormat(src.graphicsFormat);
        var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, srgb ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
        Graphics.Blit(src, rt);
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tmp = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false, !srgb);
        tmp.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0); tmp.Apply();
        RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
        bool alpha = tmp.GetPixels32().Any(c => c.a < 250);
        File.WriteAllBytes(path, tmp.EncodeToPNG());
        Object.DestroyImmediate(tmp);
        AssetDatabase.ImportAsset(path);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.sRGBTexture = srgb;
        ti.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        ti.alphaIsTransparency = alpha;
        ti.mipmapEnabled = true; ti.filterMode = FilterMode.Trilinear;
        ti.maxTextureSize = 512;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        var n = src.name.ToLowerInvariant();
        var ps = ti.GetPlatformTextureSettings("WebGL");
        ps.overridden = true; ps.maxTextureSize = 512;
        // สีหลัก 6x6 (ใบไม้มีขอบโปร่งใช้ 5x5 ให้ขอบใบไม่แตก) · แผนที่นูน 6x6 · ความมัน/AO (arm/rough) 8x8
        ps.format = n.Contains("rough") || n.Contains("_arm") || n.Contains("_ao") ? TextureImporterFormat.ASTC_8x8 : alpha ? TextureImporterFormat.ASTC_5x5 : TextureImporterFormat.ASTC_6x6;
        ti.SetPlatformTextureSettings(ps);
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}

// เฟส 1 (ลดไฟล์โหลด): บีบภาพ UI ที่ยังไม่บีบ · บีบ mesh ที่สร้างเอง · แยกภาพออกจาก glb — รันทีเดียวจบ
//   Unity.exe -batchmode -buildTarget WebGL -projectPath . -executeMethod ShrinkBuild.Run -quit
public static class ShrinkBuild
{
    public static void Run()
    {
        CompressTools.CompressUi();
        CompressGeneratedMeshes();
        GlbTextureBaker.BakeAllScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[ShrinkBuild] done");
    }

    // mesh ที่ตัวสร้างด่านบันทึกไว้ (Assets/Game/Generated): บีบระดับกลาง (ตำแหน่ง/normal/uv ใช้บิตน้อยลง) · ตาแทบไม่เห็นต่าง
    public static void CompressGeneratedMeshes()
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Mesh", new[] { "Assets/Game/Generated" }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null || MeshUtility.GetMeshCompression(m) == ModelImporterMeshCompression.Medium) continue;
            MeshUtility.SetMeshCompression(m, ModelImporterMeshCompression.Medium);
            EditorUtility.SetDirty(m); n++;
        }
        Debug.Log($"[ShrinkBuild] compressed {n} generated meshes");
    }
}
