using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace CozyFishing
{
    // โหลดฉากด่าน: ฉากบ้าน (บ่อทุ่งนา) อยู่ในไฟล์เกมหลัก · ด่านอื่นแยกเป็น AssetBundle ทีละด่าน (Bundles/<scene>)
    // โหลดเฉพาะตอนเดินทางไป แล้วเก็บแคชในเบราว์เซอร์ตามแฮชใน Bundles/index.json — ไฟล์เกมหลักเล็ก เปิดเร็ว และเพิ่มด่านได้ไม่ชนเพดานไฟล์ละ 100MB ของ GitHub
    // ในเอดิเตอร์ทุกฉากอยู่ใน Build Settings → โหลดตรงได้เลย
    public class StageLoader : MonoBehaviour
    {
        static StageLoader inst;
        static AssetBundle loaded;
        static bool commonLoaded;
        static readonly HashSet<string> commonDone = new HashSet<string>();
        static bool booted;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CozyBootProgress(float p, string msg);
        [DllImport("__Internal")] static extern void CozyBootDone();
#else
        static void CozyBootProgress(float p, string msg) { }
        static void CozyBootDone() { }
#endif
        /// เปิดเกมครั้งแรกไม่สำเร็จ: แจ้งบนจอโหลดหน้าเว็บ
        public static void BootFailed(string err) => CozyBootProgress(-1, "โหลดเกมไม่สำเร็จ ลองรีเฟรชหน้าอีกครั้ง (" + err + ")");
        static Dictionary<string, string> index;
        public static bool Loading { get; private set; }

        static StageLoader Inst
        {
            get
            {
                if (inst == null) { inst = new GameObject("StageLoader").AddComponent<StageLoader>(); DontDestroyOnLoad(inst.gameObject); }
                return inst;
            }
        }

        /// เปลี่ยนไปฉาก scene · progress(0..1) ระหว่างดาวน์โหลด · fail เมื่อโหลดไม่ได้ (กลับบ้านแทน)
        public static void Go(string scene, Action<float> progress = null, Action<string> fail = null)
        {
            // ฉากใหม่เริ่มทำงาน (Start) ก่อนการโหลดรอบก่อนจะจบ → เข้าคิวไว้ ไม่งั้นคำขอหายและจอดำค้าง
            if (Loading) { queued = (scene, progress, fail); return; }
            Inst.StartCoroutine(Inst.Run(scene, progress, fail));
        }
        static (string scene, Action<float> progress, Action<string> fail)? queued;

        static string BaseUrl()
        {
            var u = Application.absoluteURL;
            int q = u.IndexOfAny(new[] { '?', '#' }); if (q >= 0) u = u.Substring(0, q);
            return u.Substring(0, u.LastIndexOf('/') + 1) + "Bundles/";
        }

        IEnumerator Run(string scene, Action<float> progress, Action<string> fail)
        {
            Loading = true;
            var old = loaded;
            AssetBundle bundle = null;
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                string url = BaseUrl(), key = scene.ToLowerInvariant(), err = null;
                if (index == null)
                {
                    using (var req = UnityWebRequest.Get(url + "index.json?t=" + DateTime.UtcNow.Ticks))
                    {
                        yield return req.SendWebRequest();
                        if (req.result == UnityWebRequest.Result.Success) index = ParseIndex(req.downloadHandler.text);
                        else err = req.error;
                    }
                }
                // ของใช้ร่วม (common*: ตัวละคร ฟ้า พื้นผิว ต้นไม้) โหลดครั้งเดียวเก็บไว้ตลอด
                //   index.json มี deps.<ด่าน> → โหลดเฉพาะชุดที่ด่านนี้ใช้และยังไม่มี · ไม่มี (ไฟล์รุ่นเก่า) → โหลด common* ทั้งหมด
                bool didCommon = false;
                if (index != null)
                {
                    var keys = new List<string>();
                    if (index.TryGetValue("deps." + key, out var deps)) { foreach (var k in deps.Split('|')) if (k.Length > 0 && !commonDone.Contains(k)) keys.Add(k); }
                    else if (!commonLoaded) foreach (var k in index.Keys) if (k.StartsWith("common") && !commonDone.Contains(k)) keys.Add(k);
                    keys.Sort();
                    for (int ci = 0; ci < keys.Count && err == null; ci++)
                    {
                        using (var req = UnityWebRequestAssetBundle.GetAssetBundle(url + keys[ci] + "?v=" + index[keys[ci]], Hash128.Parse(index[keys[ci]]), 0))
                        {
                            var op = req.SendWebRequest();
                            while (!op.isDone) { Report(progress, (ci + req.downloadProgress) / keys.Count * 0.85f, "กำลังเตรียมเรือและคันเบ็ด…"); yield return null; }
                            if (req.result == UnityWebRequest.Result.Success) { DownloadHandlerAssetBundle.GetContent(req); commonDone.Add(keys[ci]); }   // เก็บค้างไว้ในหน่วยความจำ
                            else err = req.error;
                        }
                    }
                    if (err == null) { if (!index.ContainsKey("deps." + key)) commonLoaded = true; didCommon = keys.Count > 0; } else index = null;
                }
                bool hasCommon = didCommon;   // รอบนี้โหลดของใช้ร่วมด้วย → แถบคืบหน้าแบ่งช่วง
                if (index != null && index.TryGetValue(key, out var hash))
                {
                    if (old != null && old.name == key) bundle = old;   // อยู่ด่านนี้อยู่แล้ว (เช่นโหลดเซฟกลับมา)
                    else
                    {
                        using (var req = UnityWebRequestAssetBundle.GetAssetBundle(url + key + "?v=" + hash, Hash128.Parse(hash), 0)   /* ?v= กันเบราว์เซอร์ส่งไฟล์เวอร์ชันเก่าจากแคช HTTP */)
                        {
                            var op = req.SendWebRequest();
                            while (!op.isDone) { Report(progress, (hasCommon ? 0.85f : 0) + req.downloadProgress * (hasCommon ? 0.15f : 1), "กำลังเตรียมบ่อปลา…"); yield return null; }
                            if (req.result == UnityWebRequest.Result.Success) bundle = DownloadHandlerAssetBundle.GetContent(req);
                            else err = req.error;
                        }
                    }
                }
                else err ??= "ไม่พบด่าน " + key;
                if (bundle == null)
                {
                    Debug.LogWarning($"[StageLoader] {scene}: {err}");
                    Loading = false; queued = null;
                    fail?.Invoke(err);
                    yield break;
                }
                loaded = bundle;
            }
            else loaded = null;
            progress?.Invoke(1);
            var load = SceneManager.LoadSceneAsync(scene);
            while (!load.isDone) yield return null;
            if (!booted) { booted = true; CozyBootDone(); }
            if (old != null && old != loaded) old.Unload(true);   // ออกจากด่านเดิมแล้ว คืนหน่วยความจำ
            Loading = false;
            if (queued is { } q) { queued = null; Go(q.scene, q.progress, q.fail); }
        }

        // ครั้งแรก (ก่อนเข้าเกม) แสดงบนจอโหลดหน้าเว็บ · หลังจากนั้นแสดงบนจอเดินทางในเกม
        static void Report(Action<float> progress, float p, string msg)
        {
            if (!booted) CozyBootProgress(p, msg);
            progress?.Invoke(p);
        }

        // index.json: {"temple":"<hash>", ...}
        static Dictionary<string, string> ParseIndex(string json)
        {
            var d = new Dictionary<string, string>();
            foreach (var part in json.Trim().Trim('{', '}').Split(','))
            {
                var kv = part.Split(':');
                if (kv.Length == 2) d[kv[0].Trim().Trim('"')] = kv[1].Trim().Trim('"');
            }
            return d;
        }
    }
}
