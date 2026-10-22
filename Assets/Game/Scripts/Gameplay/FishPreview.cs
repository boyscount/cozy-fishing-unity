using System.Collections.Generic;
using UnityEngine;

namespace CozyFishing
{
    // ถ่ายรูปปลา 3D แยกจากฉาก (กล้องลับใต้แผนที่ เห็นเฉพาะเลเยอร์ 31):
    //  · Live = ภาพปลาหมุนช้า ๆ สำหรับการ์ด "ได้ปลาแล้ว!"
    //  · Icon = รูปนิ่งของแต่ละชนิดสำหรับสมุดปลา (ยังไม่เคยตก = เงาดำ) เก็บไว้ใช้ซ้ำ
    public class FishPreview : MonoBehaviour
    {
        public const int Layer = 31;
        public Camera cam;
        public FishingController fishing;
        RenderTexture liveRt;
        GameObject live;
        readonly Dictionary<string, RenderTexture> icons = new Dictionary<string, RenderTexture>();
        static readonly Vector3 Stage = new Vector3(0, -200, 0);

        void Awake()
        {
            cam.enabled = false;
            cam.cullingMask = 1 << Layer;
            liveRt = new RenderTexture(640, 360, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
        }

        GameObject Spawn(FishSpecies sp, bool silhouette)
        {
            var g = fishing.MakeFish(sp, 1f);
            foreach (var t in g.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
            // ย่อ/ขยายให้พอดีกรอบภาพ (ปลาแต่ละชนิดสัดส่วนต่างกัน)
            var b = new Bounds(g.transform.position, Vector3.zero);
            foreach (var r in g.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            float size = Mathf.Max(b.size.x, b.size.z, b.size.y * 1.6f);
            g.transform.localScale *= 1.5f / Mathf.Max(size, 0.01f);
            g.transform.SetPositionAndRotation(Stage, Quaternion.Euler(0, 180, 0));   // โมเดลปลายาวตามแกน X → มองด้านข้าง หัวหันซ้าย
            b = new Bounds(g.transform.position, Vector3.zero);
            foreach (var r in g.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            g.transform.position += Stage - b.center;
            if (silhouette)
            {
                var mpb = new MaterialPropertyBlock();
                foreach (var r in g.GetComponentsInChildren<Renderer>())
                    for (int i = 0; i < r.sharedMaterials.Length; i++) { mpb.Clear(); mpb.SetColor("_BaseColor", Color.black); mpb.SetFloat("_Smoothness", 0); r.SetPropertyBlock(mpb, i); }
            }
            return g;
        }

        void Shoot(RenderTexture rt)
        {
            cam.transform.SetPositionAndRotation(Stage + new Vector3(0, 0.35f, -2.4f), Quaternion.Euler(8, 0, 0));
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
        }

        public RenderTexture Live(FishSpecies sp)
        {
            if (live) Destroy(live);
            live = Spawn(sp, false);
            Shoot(liveRt);
            return liveRt;
        }

        public void StopLive() { if (live) Destroy(live); live = null; }

        public RenderTexture Icon(FishSpecies sp, bool silhouette)
        {
            string key = sp.id + (silhouette ? "#s" : "");
            if (icons.TryGetValue(key, out var rt) && rt.IsCreated()) return rt;
            rt = new RenderTexture(320, 200, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            var g = Spawn(sp, silhouette);
            Shoot(rt);
            DestroyImmediate(g);
            icons[key] = rt;
            return rt;
        }

        void LateUpdate()
        {
            if (!live) return;
            live.transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.3f) * 8, 180 + Mathf.Sin(Time.time * 0.8f) * 35, Mathf.Sin(Time.time * 2.1f) * 6);
            Shoot(liveRt);
        }
    }
}
