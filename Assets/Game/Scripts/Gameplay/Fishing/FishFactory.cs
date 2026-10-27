using System.Collections.Generic;
using UnityEngine;

namespace CozyFishing
{
    // สร้างตัวปลา: ภาพถ่ายบนเมชแบน (ถ้ามี) หรือโคลนโมเดลจาก Blender แล้วย้อมสีตามชนิด
    static class FishFactory
    {
        // วัสดุปลาจากภาพถ่าย (หนึ่งชิ้นต่อชนิด ใช้ซ้ำ) · ปลาหายากเรืองแสงทองจาง ๆ
        static readonly Dictionary<string, Material> photoMats = new Dictionary<string, Material>();
        public static Material PhotoMat(FishSpecies sp)
        {
            if (photoMats.TryGetValue(sp.id, out var m) && m != null) return m;
            var baseMat = Resources.Load<Material>("Fish/FishPhoto");
            m = new Material(baseMat) { name = "FishPhoto_" + sp.id };
            m.SetTexture("_BaseMap", sp.Photo);
            m.SetFloat("_Wiggle", 0.07f); m.SetFloat("_SwimSpeed", 13f);
            if (sp.glow) m.SetColor("_EmissionColor", new Color(0.16f, 0.11f, 0.02f));
            photoMats[sp.id] = m;
            return m;
        }

        public static GameObject Make(GameObject template, FishSpecies sp, float size)
        {
            var mesh = sp.Photo != null ? sp.PhotoMesh : null;
            if (mesh != null)
            {
                var pg = new GameObject("Fish_" + sp.id);
                pg.AddComponent<MeshFilter>().sharedMesh = mesh;
                pg.AddComponent<MeshRenderer>().sharedMaterial = PhotoMat(sp);
                pg.transform.localScale = Vector3.one * size * 1.15f;
                return pg;
            }
            var g = Object.Instantiate(template);
            g.SetActive(true);
            g.transform.localScale = new Vector3(sp.shape[0], sp.shape[1], sp.shape[2]) * size;
            var mpb = new MaterialPropertyBlock();
            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var n = mats[i] ? mats[i].name : "";
                    if (n.Contains("Eye")) continue;
                    mpb.Clear();
                    mpb.SetColor("_BaseColor", n.Contains("Fin") ? sp.FinColor : Color.Lerp(sp.TopColor, sp.BellyColor, 0.25f));
                    if (sp.glow) mpb.SetColor("_EmissionColor", new Color(0.35f, 0.23f, 0f));
                    r.SetPropertyBlock(mpb, i);
                }
            }
            return g;
        }
    }
}
