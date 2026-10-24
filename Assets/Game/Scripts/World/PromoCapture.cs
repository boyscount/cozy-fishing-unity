using System.IO;
using UnityEngine;

namespace CozyFishing
{
    // อัดคลิปโปรโมต 4K แบบทีละเฟรม (ใช้ในเอดิเตอร์ผ่าน Tools/PromoCaptureEditor — ไม่ทำงานบนเว็บ)
    //   ตั้งตัวแปรแวดล้อม COZY_PROMO=<โฟลเดอร์ผลลัพธ์>|<วินาทีสูงสุด>|<จำนวนปลาที่อัด> และ COZY_FLAGS=bot&stage=…
    //   · เวลาในเกมเดินคงที่ 30 เฟรม/วินาที ไม่ว่าเครื่องเรนเดอร์ช้าแค่ไหน → ภาพลื่นทุกเฟรม
    //   · กล้องวาดลงภาพ 3840×2160 (ไม่มี HUD/ข้อความ เพราะ HUD วาดบนจอ ไม่ได้วาดลงภาพนี้)
    //   · อัดเฉพาะช่วงที่น่าดู: ฉากเปิดด่าน · ปลากินเบ็ด → สู้ → ยกปลาโชว์ · ผีปรากฏ · ภาพบรรยากาศสั้น ๆ เป็นระยะ
    //   แต่ละช่วงเก็บเป็นโฟลเดอร์ <ลำดับ>_<ชนิด>/f00001.jpg แล้วค่อยต่อเป็นวิดีโอด้วย ffmpeg
    [DefaultExecutionOrder(32000)]   // หลังทุกระบบวางกล้องเสร็จ (รวมคัตซีน)
    public class PromoCapture : MonoBehaviour
    {
        const int W = 3840, H = 2160, FPS = 30;
        string outDir; float maxSec = 150; int maxFish = 2;
        RenderTexture rt; Texture2D tex; Camera bound;
        string clipDir; int clipIdx, frame, fishDone; float tail = -1, nextAmbient = 20, ambientEnd = -1; string kind;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var env = System.Environment.GetEnvironmentVariable("COZY_PROMO");
            if (string.IsNullOrEmpty(env)) return;
            PlayerPrefs.DeleteAll();   // เริ่มเซฟใหม่ทุกครั้ง (ผลซ้ำได้)
            Random.InitState(1234);
            var go = new GameObject("PromoCapture"); DontDestroyOnLoad(go);
            var pc = go.AddComponent<PromoCapture>();
            var p = env.Split('|');
            pc.outDir = p[0];
            if (p.Length > 1) float.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pc.maxSec);
            if (p.Length > 2) int.TryParse(p[2], out pc.maxFish);
        }

        void Start()
        {
            Directory.CreateDirectory(outDir);
            Time.captureFramerate = FPS;
            rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 }; rt.Create();
            tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            Debug.Log("[Promo] capture → " + outDir);
        }

        // โหมด batch ไม่มีหน้าจอ: WaitForEndOfFrame ไม่ถูกเรียก และกล้องอาจไม่วาดเอง → สั่งวาดลงภาพเองท้ายเฟรม
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null && cam != bound) { if (bound != null) bound.targetTexture = null; cam.targetTexture = rt; bound = cam; }
            Capture();
        }

        void Capture()
        {
            float t = Time.time;
            if (t > maxSec) { Quit(); return; }
            var fish = FindAnyObjectByType<FishingController>();
            var st = fish != null ? fish.State : FishState.Free;
            string want = null;
            if (StageIntro.Playing) want = "intro";
            else if (GhostSystem.Revealing) want = "ghost";
            else if (fishDone < maxFish && (st == FishState.Bite || st == FishState.Strike || st == FishState.Fight || st == FishState.Caught)) { want = "fish"; tail = 3.5f; }
            else if (kind == "fish" && tail > 0) { tail -= Time.deltaTime; want = "fish"; if (tail <= 0) fishDone++; }
            else if (t >= nextAmbient && bound != null) { if (ambientEnd < 0) ambientEnd = t + 5; want = "ambient"; if (t >= ambientEnd) { ambientEnd = -1; nextAmbient = t + 25; want = null; } }
            if (want == null) { kind = null; return; }
            if (want != kind) { kind = want; clipIdx++; frame = 0; clipDir = Path.Combine(outDir, $"{clipIdx:00}_{kind}"); Directory.CreateDirectory(clipDir); Debug.Log($"[Promo] clip {clipIdx} {kind} at {t:F1}s"); }
            if (bound == null) return;
            bound.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0, false);
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(clipDir, $"f{++frame:00000}.jpg"), tex.EncodeToJPG(93));
        }

        void Quit()
        {
            Debug.Log("[Promo] done");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit();
#endif
        }
    }
}
