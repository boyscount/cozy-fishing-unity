using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CozyFishing
{
    // ภาษา (ไทย/อังกฤษ): เกมเขียนข้อความเป็นภาษาไทยตามเดิม แล้วแปลตอนแสดงผลจากตาราง Resources/Data/lang_en.json
    //  · ข้อความตรงตัว → ค้นตาราง · ข้อความมีตัวแปร ("ได้ปลา{0}!") → จับคู่แม่แบบ แล้วแปลค่าที่อยู่ในช่องซ้ำ (ชื่อปลา ชื่อด่าน)
    //  · ไม่เจอทั้งก้อน → แยกแปลทีละบรรทัด / ทีละส่วนคั่น " · " / ข้ามแท็ก <size> <color>
    public static class Loc
    {
        const string PrefKey = "abot.lang";
        public static event Action Changed;
        static string lang;
        public static string Lang
        {
            get
            {
                if (lang == null)
                {
                    try { lang = PlayerPrefs.GetString(PrefKey, ""); } catch (Exception) { lang = ""; }
                    if (lang == "") lang = Application.systemLanguage == SystemLanguage.Thai ? "th" : "en";
                    try { var u = Application.absoluteURL ?? ""; if (u.Contains("lang=en")) lang = "en"; else if (u.Contains("lang=th")) lang = "th"; } catch (Exception) { }   // ทดสอบ: ?lang=en
                }
                return lang;
            }
            set
            {
                if (value == Lang) return;
                lang = value;
                try { PlayerPrefs.SetString(PrefKey, value); PlayerPrefs.Save(); } catch (Exception) { }
                cache.Clear(); Changed?.Invoke();
            }
        }
        public static bool English => Lang == "en";

        [Serializable] class Item { public string th, en; }
        [Serializable] class Table { public Item[] items; }
        static Dictionary<string, string> exact;
        static List<(string prefix, Regex rx, string en)> templates;
        static readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        static readonly Regex Thai = new Regex("[฀-๿]");
        static readonly Regex Tag = new Regex("(<[^>]+>)");
        static readonly Regex Slot = new Regex(@"\{(\d+)\}");

        static void Load()
        {
            exact = new Dictionary<string, string>(); templates = new List<(string, Regex, string)>();
            var ta = Resources.Load<TextAsset>("Data/lang_en");
            if (ta == null) return;
            var t = JsonUtility.FromJson<Table>(ta.text);
            if (t?.items == null) return;
            foreach (var it in t.items)
            {
                if (string.IsNullOrEmpty(it.th) || string.IsNullOrEmpty(it.en)) continue;
                if (!Slot.IsMatch(it.th)) { exact[it.th] = it.en; continue; }
                // แม่แบบ: ข้อความคงที่ตรงตัว · {n} = อะไรก็ได้ (สั้นที่สุด)
                var sb = new StringBuilder("^");
                int last = 0;
                foreach (Match m in Slot.Matches(it.th))
                {
                    sb.Append(Regex.Escape(it.th.Substring(last, m.Index - last)));
                    sb.Append("(?<g").Append(m.Groups[1].Value).Append(">.*?)");
                    last = m.Index + m.Length;
                }
                sb.Append(Regex.Escape(it.th.Substring(last))).Append("$");
                int first = it.th.IndexOf('{');
                templates.Add((it.th.Substring(0, first), new Regex(sb.ToString(), RegexOptions.Singleline), it.en));
            }
            // แม่แบบที่มีข้อความคงที่ยาวกว่าลองก่อน (เจาะจงกว่า)
            templates.Sort((a, b) => Slot.Replace(b.rx.ToString(), "").Length.CompareTo(Slot.Replace(a.rx.ToString(), "").Length));
        }

        /// แปลข้อความที่จะแสดง (ภาษาไทย = คืนค่าเดิม)
        public static string T(string s)
        {
            if (!English || string.IsNullOrEmpty(s) || !Thai.IsMatch(s)) return s;
            if (exact == null) Load();
            if (cache.TryGetValue(s, out var hit)) return hit;
            var r = Translate(s, 0);
            if (cache.Count > 4000) cache.Clear();
            cache[s] = r;
            return r;
        }

        static string Translate(string s, int depth)
        {
            if (!Thai.IsMatch(s) || depth > 4) return s;
            if (exact.TryGetValue(s, out var e)) return e;
            var trimmed = s.Trim();
            if (trimmed != s && exact.TryGetValue(trimmed, out e)) return s.Replace(trimmed, e);
            foreach (var (prefix, rx, en) in templates)
            {
                if (prefix.Length > 0 && !s.StartsWith(prefix, StringComparison.Ordinal)) continue;
                var m = rx.Match(s);
                if (!m.Success) continue;
                return Slot.Replace(en, sm => { var g = m.Groups["g" + sm.Groups[1].Value]; return g.Success ? Translate(g.Value, depth + 1) : sm.Value; });
            }
            // ไม่เจอทั้งก้อน: แยกแปลเป็นส่วน ๆ
            if (s.Contains("\n")) return Join(s.Split('\n'), "\n", depth);
            if (Tag.IsMatch(s))
            {
                var parts = Tag.Split(s);
                for (int i = 0; i < parts.Length; i++) if (!parts[i].StartsWith("<")) parts[i] = Translate(parts[i], depth + 1);
                return string.Concat(parts);
            }
            if (s.Contains(" · ")) return Join(s.Split(new[] { " · " }, StringSplitOptions.None), " · ", depth);
            return s;
        }
        static string Join(string[] parts, string sep, int depth)
        {
            for (int i = 0; i < parts.Length; i++) parts[i] = Translate(parts[i], depth + 1);
            return string.Join(sep, parts);
        }
    }
}
