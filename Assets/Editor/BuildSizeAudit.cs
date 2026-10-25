using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// วัดว่าอะไรกินที่ในไฟล์โหลดจริง (ประมาณจากขนาดภาพ/รูปแบบบีบอัดของแพลตฟอร์มที่เลือก) — ใช้ก่อน/หลังลดขนาด
//   Unity.exe -batchmode -buildTarget WebGL -projectPath . -executeMethod BuildSizeAudit.Run -quit  → Logs/size_audit.txt
public static class BuildSizeAudit
{
    public static void Run()
    {
        var scenes = Directory.GetFiles("Assets/Game/Scenes", "*.unity").Select(p => p.Replace('\\', '/')).ToArray();
        var deps = AssetDatabase.GetDependencies(scenes, true).Concat(AssetDatabase.FindAssets("", new[] { "Assets/Game/Resources" }).Select(AssetDatabase.GUIDToAssetPath)).Distinct().ToArray();
        // ใช้กี่ฉาก (2 ขึ้นไป = อยู่ใน bundle common-*)
        var users = deps.ToDictionary(d => d, d => 0);
        foreach (var s in scenes) foreach (var d in AssetDatabase.GetDependencies(s, true)) if (users.ContainsKey(d)) users[d]++;
        var rows = new System.Collections.Generic.List<(long size, string line, string group)>();
        var kinds = new System.Collections.Generic.List<(string kind, long size)>();
        foreach (var path in deps)
        {
            if (AssetDatabase.IsValidFolder(path) || path.EndsWith(".unity")) continue;
            string group = path.Contains("/Resources/") ? "start(Resources)" : users[path] >= 2 ? "common" : "stage";
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (o is Texture2D t)
                {
                    long s = Estimate(t);
                    rows.Add((s, $"{s / 1024,8} KB  {group,-16} {t.width}x{t.height} {t.format,-10} mips={t.mipmapCount,-2} {path} :: {t.name}", group)); kinds.Add((group + " tex " + t.format + (path.EndsWith(".glb") ? " (glb)" : ""), s));
                }
                else if (o is Cubemap c)
                {
                    long s = (long)c.width * c.width * 6 * (c.format.ToString().Contains("Half") ? 8 : 4);
                    rows.Add((s, $"{s / 1024,8} KB  {group,-16} cube {c.width} {c.format,-10} {path}", group));
                }
                else if (o is Mesh m)
                {
                    long s = (long)m.vertexCount * 48 + (long)m.triangles.Length * 2;
                    rows.Add((s, $"{s / 1024,8} KB  {group,-16} mesh {m.vertexCount}v {path} :: {m.name}", group)); kinds.Add((group + " mesh" + (path.EndsWith(".glb") ? " (glb)" : ""), s));
                }
            }
        }
        var lines = new System.Collections.Generic.List<string>();
        foreach (var g in rows.GroupBy(r => r.group)) lines.Add($"TOTAL {g.Key}: {g.Sum(r => r.size) / 1024 / 1024} MB ({g.Count()} items)");
        foreach (var k in kinds.GroupBy(k => k.kind).OrderByDescending(g => g.Sum(x => x.size))) lines.Add($"  {k.Key}: {k.Sum(x => x.size) / 1024 / 1024} MB ({k.Count()})");
        lines.Add("");
        lines.AddRange(rows.OrderByDescending(r => r.size).Take(120).Select(r => r.line));
        File.WriteAllLines("Logs/size_audit.txt", lines);
        Debug.Log("[SizeAudit] done");
    }

    // ขนาดโดยประมาณบนดิสก์ (ก่อน brotli) ตามรูปแบบที่ build จริงจะใช้
    static long Estimate(Texture2D t)
    {
        string f = t.format.ToString();
        double bpp = f.StartsWith("ASTC_") ? 128.0 / (int.Parse(f.Substring(5).Split('x')[0]) * int.Parse(f.Substring(5).Split('x')[1].Split('_')[0]))
            : f.Contains("DXT1") || f.Contains("ETC_") || f.Contains("ETC2_RGB") ? 4 : f.Contains("DXT5") || f.Contains("BC7") || f.Contains("ETC2_RGBA8") ? 8
            : f.Contains("RGBA32") || f.Contains("ARGB32") || f.Contains("BGRA32") ? 32 : f.Contains("RGB24") ? 24 : f.Contains("RGBAHalf") ? 64 : f.Contains("R8") || f.Contains("Alpha8") ? 8 : 32;
        return (long)(t.width * (long)t.height * bpp / 8 * (t.mipmapCount > 1 ? 1.333 : 1));
    }
}
