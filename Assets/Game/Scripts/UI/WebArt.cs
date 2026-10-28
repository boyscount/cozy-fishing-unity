using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace CozyFishing
{
    // ภาพพื้นหลังใหญ่ที่ไม่ต้องใช้ตอนเปิดเกม (หน้าเช้า ห้องนอน): เก็บใน TemplateData/art ของหน้าเว็บ โหลดตอนจะแสดงครั้งแรก
    //   ไม่นับรวมไฟล์เกมที่ต้องโหลดก่อนเล่น (เพดานหน้าแรก 50 MB) · ใน Editor อ่านไฟล์จากโฟลเดอร์เทมเพลตตรง ๆ
    public static class WebArt
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, List<Action<Texture2D>>> waiting = new Dictionary<string, List<Action<Texture2D>>>();

        public static void Load(MonoBehaviour host, string file, Action<Texture2D> done)
        {
            if (cache.TryGetValue(file, out var t) && t != null) { done(t); return; }
            if (waiting.TryGetValue(file, out var list)) { list.Add(done); return; }
            waiting[file] = new List<Action<Texture2D>> { done };
            host.StartCoroutine(Fetch(file));
        }

        static IEnumerator Fetch(string file)
        {
            Texture2D tex = null;
#if UNITY_EDITOR
            var path = System.IO.Path.Combine(Application.dataPath, "WebGLTemplates/Cozy/TemplateData/art", file);
            if (System.IO.File.Exists(path)) { tex = new Texture2D(2, 2, TextureFormat.RGB24, false); tex.LoadImage(System.IO.File.ReadAllBytes(path)); }
            yield return null;
#else
            var url = Application.absoluteURL; int q = url.IndexOfAny(new[] { '?', '#' }); if (q >= 0) url = url.Substring(0, q);
            url = url.Substring(0, url.LastIndexOf('/') + 1) + "TemplateData/art/" + file;
            using (var req = UnityWebRequestTexture.GetTexture(url, true))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) tex = DownloadHandlerTexture.GetContent(req);
            }
#endif
            if (tex != null) { tex.wrapMode = TextureWrapMode.Clamp; cache[file] = tex; }
            var list = waiting[file]; waiting.Remove(file);
            foreach (var cb in list) if (tex != null) cb(tex);
        }
    }
}
