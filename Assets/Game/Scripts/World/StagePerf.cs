using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CozyFishing
{
    // ลดภาระวาดเงาตอนโหลดด่าน (ภาพแทบไม่ต่าง แต่เครื่องเบาลงมาก):
    //   เงาต้องวาดฉากซ้ำอีกรอบต่อชั้นเงา (2 ชั้น) — ต้นไม้ไกลที่หมอกกลืนแล้ว และกอรากเล็ก ๆ ที่อยู่ห่างตัวละคร ไม่จำเป็นต้องทอดเงา
    //   · วัตถุไกลจากพื้นที่เล่น (กรอบกล้อง) เกิน FarShadow เมตร → ไม่ทอดเงา (ยังรับเงาได้)
    //   · ป่าชายเลน (ด่านหนักสุด ~2.2 ล้านสามเหลี่ยม): กอรากโกงกางทอดเงาเฉพาะใกล้ตัว · ระยะเงาสั้นลง (หมอกหนามองไม่ถึงอยู่แล้ว)
    public static class StagePerf
    {
        const float FarShadow = 26f;

        public static void Apply(IWorld w)
        {
            bool mangrove = w.StageId == "mangrove";
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp) urp.shadowDistance = mangrove ? 32f : 50f;
            var b = w.CamBounds; var sp = w.Spawn;
            int noShadow = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                if (!r.enabled || r.shadowCastingMode == ShadowCastingMode.Off) continue;
                var c = r.bounds.center; float ext = r.bounds.extents.magnitude;
                // ระยะจากกรอบพื้นที่เล่น (ศูนย์ถ้าอยู่ข้างใน)
                float dx = Mathf.Max(b.xMin - c.x, 0, c.x - b.xMax), dz = Mathf.Max(b.yMin - c.z, 0, c.z - b.yMax);
                float d = Mathf.Sqrt(dx * dx + dz * dz) - ext;
                var mf = r.GetComponent<MeshFilter>(); string mn = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "";
                bool far = d > FarShadow;
                if (mangrove && mn.StartsWith("MangroveRoots") && (new Vector2(c.x - sp.x, c.z - sp.z)).magnitude > 9f) far = true;
                if (mangrove && mn.StartsWith("island_trees") && d > 10f) far = true;
                if (far) { r.shadowCastingMode = ShadowCastingMode.Off; noShadow++; }
            }
            // ป่าชายเลน: สีหมอกแทบเท่าสีฟ้า → ของที่ไกลเกินหมอกไม่ต้องวาด (ฟ้ายังวาดเต็มจอ) ตัดป่าไกลหนัก ๆ ทิ้งได้โดยภาพไม่เปลี่ยน
            if (mangrove && Camera.main != null && RenderSettings.fog && RenderSettings.fogMode == FogMode.Linear)
                Camera.main.farClipPlane = Mathf.Max(60f, RenderSettings.fogEndDistance + 12f);
            Debug.Log($"[StagePerf] {w.StageId}: shadows off on {noShadow} far objects");
        }
    }
}
