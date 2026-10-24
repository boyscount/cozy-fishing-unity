using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // แปลข้อความบนจอ UI Toolkit ทั้งหมดอัตโนมัติ: ไล่ดูทุก Label/Button ทุก 0.1 วิ · ข้อความเปลี่ยน → จำต้นฉบับไทยไว้ แล้วใส่คำแปล
    // สลับภาษา → คืนต้นฉบับแล้วแปลใหม่ทันที · โค้ดเกมเขียนข้อความไทยตามเดิมได้เลย
    public class TextLocalizer : MonoBehaviour
    {
        VisualElement root;
        readonly Dictionary<TextElement, (string src, string shown)> seen = new Dictionary<TextElement, (string, string)>();
        readonly List<TextElement> scratch = new List<TextElement>();
        float t;

        public void Init(VisualElement r) { root = r; Loc.Changed += Refresh; }
        void OnDestroy() => Loc.Changed -= Refresh;

        void LateUpdate()
        {
            if (root == null || (t -= Time.unscaledDeltaTime) > 0) return;
            t = 0.1f;
            Scan();
        }

        void Scan()
        {
            scratch.Clear();
            root.Query<TextElement>().ForEach(te => scratch.Add(te));
            foreach (var te in scratch)
            {
                var cur = te.text;
                if (string.IsNullOrEmpty(cur)) continue;
                if (seen.TryGetValue(te, out var e) && e.shown == cur) continue;   // ยังเป็นคำแปลที่เราใส่ไว้
                var tr = Loc.T(cur);
                if (tr != cur) te.text = tr;
                seen[te] = (cur, tr);
            }
            if (seen.Count > 3000) seen.Clear();
        }

        void Refresh()
        {
            foreach (var kv in seen)
            {
                if (kv.Key == null || kv.Key.panel == null) continue;
                var tr = Loc.T(kv.Value.src);
                kv.Key.text = tr;
            }
            var keys = new List<TextElement>(seen.Keys);
            foreach (var k in keys) { var v = seen[k]; seen[k] = (v.src, Loc.T(v.src)); }
        }
    }
}
