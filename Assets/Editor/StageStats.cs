using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// เทียบภาระวาดภาพของแต่ละด่าน (หาว่าด่านไหนหนักเพราะอะไร): จำนวนชิ้นที่วาด · สามเหลี่ยมทั้งหมด · สามเหลี่ยมที่ทอดเงา · ชิ้นตัดขอบใบไม้ (alpha test) · ไฟ
//   batch: Unity -batchmode -quit -executeMethod StageStats.Run
public static class StageStats
{
    [MenuItem("Tools/Cozy/Stage Stats")]
    public static void Run()
    {
        var scenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Game/Scenes" }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        foreach (var path in scenes)
        {
            var sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            long tris = 0, shadowTris = 0, cutTris = 0, bigTris = 0; int rends = 0, cut = 0, lights = 0; string big = "";
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null || !r.enabled) continue;
                long t = 0; var m = mf.sharedMesh; for (int s = 0; s < m.subMeshCount; s++) t += m.GetIndexCount(s) / 3;
                rends += r.sharedMaterials.Length; tris += t;
                if (r.shadowCastingMode != ShadowCastingMode.Off) shadowTris += t;
                if (r.sharedMaterials.Any(mt => mt != null && (mt.renderQueue >= 2450 && mt.renderQueue < 3000))) { cut++; cutTris += t; }
                if (t > bigTris) { bigTris = t; big = r.name; }
            }
            foreach (var r in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Exclude)) if (r.sharedMesh) { rends++; tris += r.sharedMesh.triangles.Length / 3; }
            lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude).Length;
            if (path.Contains(System.Environment.GetEnvironmentVariable("STATS_DETAIL") ?? "~none~"))
                foreach (var g in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude).Where(r => r.enabled && r.GetComponent<MeshFilter>()?.sharedMesh != null)
                    .GroupBy(r => r.GetComponent<MeshFilter>().sharedMesh.name).Select(g => (g.Key, n: g.Count(), t: g.Sum(r => (long)r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3), sh: g.First().shadowCastingMode))
                    .OrderByDescending(x => x.t).Take(14))
                    Debug.Log($"[StageStats]   {g.Key,-28} x{g.n,4} {g.t / 1000,6}k shadow={g.sh}");
            Debug.Log($"[StageStats] {System.IO.Path.GetFileNameWithoutExtension(path),-12} draws={rends,5} tris={tris / 1000,6}k shadowTris={shadowTris / 1000,6}k alphaTest={cut,4} ({cutTris / 1000}k tris) lights={lights} biggest={big} {bigTris / 1000}k");
        }
    }
}
