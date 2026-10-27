using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // แปลข้อความบนจอ UI Toolkit ทั้งหมดอัตโนมัติ: ไล่ดูทุก Label/Button ทุก 0.1 วิ · ข้อความเปลี่ยน → จำต้นฉบับไทยไว้ แล้วใส่คำแปล
    // สลับภาษา → คืนต้นฉบับแล้วแปลใหม่ทันที · โค้ดเกมเขียนข้อความไทยตามเดิมได้เลย
    // ข้อความที่โค้ดเลือกภาษาเองแล้ว (ป้ายสลับภาษา) ใส่คลาส NoLoc → ไม่แปลทับ (ไม่งั้นโค้ดกับตัวแปลแย่งกันตั้งจนกะพริบ)
    public class TextLocalizer : MonoBehaviour
    {
        VisualElement root;
        readonly Dictionary<TextElement, (string src, string shown)> seen = new Dictionary<TextElement, (string, string)>();
        readonly List<TextElement> scratch = new List<TextElement>();
        // ข้อความที่โค้ดตั้งซ้ำทุกเฟรม (ชื่อเหยื่อ ป้ายระยะ ฯลฯ): เจอว่าถูกตั้งกลับเป็นไทยซ้ำ → แปลทุกเฟรมก่อนวาด ไม่งั้นไทย/อังกฤษสลับกันรัว ๆ
        readonly HashSet<TextElement> hot = new HashSet<TextElement>();
        readonly List<TextElement> dead = new List<TextElement>();
        float t;
        public const string NoLoc = "no-loc";

        static TextLocalizer inst;
        public void Init(VisualElement r) { root = r; inst = this; Loc.Changed += Refresh; }
        /// แปลทันที (หลังสร้าง/เปลี่ยนข้อความไทยในหน้าที่เปิดอยู่) · ไม่ต้องรอรอบ 0.1 วิ ภาษาไทยจะไม่โผล่แวบ
        public static void Now() { if (inst != null && inst.root != null) inst.Scan(); }
        void OnDestroy() => Loc.Changed -= Refresh;

        void LateUpdate()
        {
            if (root == null) return;
            if (hot.Count > 0) Hot();
            if ((t -= Time.unscaledDeltaTime) > 0) return;
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
                if (string.IsNullOrEmpty(cur) || te.ClassListContains(NoLoc)) continue;
                if (seen.TryGetValue(te, out var e) && e.shown == cur) continue;   // ยังเป็นคำแปลที่เราใส่ไว้
                var tr = Loc.T(cur);
                if (tr != cur) { te.text = tr; if (e.shown != null && e.shown != e.src) hot.Add(te); }   // เคยแปลไว้แล้วโดนตั้งไทยทับอีก = ข้อความที่โค้ดตั้งซ้ำเรื่อย ๆ
                seen[te] = (cur, tr);
            }
            if (seen.Count > 3000) { seen.Clear(); hot.Clear(); }
        }

        void Hot()
        {
            dead.Clear();
            foreach (var te in hot)
            {
                if (te.panel == null) { dead.Add(te); continue; }
                var cur = te.text;
                if (string.IsNullOrEmpty(cur) || (seen.TryGetValue(te, out var e) && e.shown == cur)) continue;
                var tr = Loc.T(cur);
                if (tr != cur) te.text = tr;
                seen[te] = (cur, tr);
            }
            foreach (var te in dead) hot.Remove(te);
            if (hot.Count > 200) hot.Clear();
        }

        void Refresh()
        {
            foreach (var kv in seen)
            {
                if (kv.Key == null || kv.Key.panel == null || kv.Key.ClassListContains(NoLoc)) continue;
                var tr = Loc.T(kv.Value.src);
                kv.Key.text = tr;
            }
            var keys = new List<TextElement>(seen.Keys);
            foreach (var k in keys) { var v = seen[k]; seen[k] = (v.src, Loc.T(v.src)); }
        }
    }
}
