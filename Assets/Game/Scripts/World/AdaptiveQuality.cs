using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CozyFishing
{
    // ปรับคุณภาพอัตโนมัติตามแรงเครื่อง: เริ่มที่ความละเอียดเรนเดอร์เต็ม 100% (ภาพคมสุด)
    //   เฟรมตกต่ำกว่า ~30 fps ต่อเนื่อง → ลดความละเอียดเรนเดอร์ทีละขั้น (ต่ำสุด 80%) แล้วขยายกลับเต็มจอด้วย FSR ที่ช่วยให้ขอบยังคม
    //   เครื่องไหลลื่นเกิน ~56 fps นาน ๆ → ค่อย ๆ คืนความละเอียดขึ้นไป · UI ไม่ได้รับผล (วาดที่ความละเอียดจอเสมอ)
    public class AdaptiveQuality : MonoBehaviour
    {
        const float Min = 0.8f, Max = 1f;   // ไม่ลดต่ำกว่า 80% (ต่ำกว่านี้ภาพเริ่มเบลอ)
        float acc, slowFor, fastFor; int frames;
        UniversalRenderPipelineAsset urp;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("AdaptiveQuality"); DontDestroyOnLoad(go);
            go.AddComponent<AdaptiveQuality>();
        }

        void Start()
        {
            urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null) { enabled = false; return; }
            urp.renderScale = Max;
            urp.upscalingFilter = UpscalingFilterSelection.FSR;   // ใช้เมื่อเรนเดอร์ต่ำกว่า 100% เท่านั้น (เครื่องไม่รองรับจะถอยไปใช้แบบเส้นตรงเอง)
            urp.fsrOverrideSharpness = true; urp.fsrSharpness = 0.85f;
        }

        void Update()
        {
            if (urp == null || Time.timeScale < 0.5f) return;   // ช่วงฉากผีสโลว์โมชันไม่นับ
            acc += Time.unscaledDeltaTime; frames++;
            if (acc < 1f) return;
            float fps = frames / acc; acc = 0; frames = 0;
            if (fps < 30f) { slowFor += 1; fastFor = 0; } else if (fps > 56f) { fastFor += 1; slowFor = 0; } else { slowFor = 0; fastFor = 0; }
            if (slowFor >= 3 && urp.renderScale > Min + 0.01f) { urp.renderScale = Mathf.Max(Min, urp.renderScale - 0.1f); slowFor = 0; }
            else if (fastFor >= 8 && urp.renderScale < Max - 0.01f) { urp.renderScale = Mathf.Min(Max, urp.renderScale + 0.05f); fastFor = 0; }
        }

        void OnDestroy() { if (urp != null) urp.renderScale = Max; }
    }
}
