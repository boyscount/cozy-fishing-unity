using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyFishing
{
    // สมุดสะสมปลา: จำนวนที่ตกได้และตัวหนักที่สุดของแต่ละชนิด — บันทึกถาวรใน PlayerPrefs (บนเว็บเก็บใน IndexedDB ของเบราว์เซอร์)
    public static class FishDex
    {
        [Serializable] class Entry { public string id; public int count; public float bestKg; }
        [Serializable] class Save { public List<Entry> fish = new List<Entry>(); }

        const string Key = "cozy.dex.v1";
        static Dictionary<string, Entry> map;

        static void Load()
        {
            if (map != null) return;
            map = new Dictionary<string, Entry>();
            var json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return;
            try { foreach (var e in JsonUtility.FromJson<Save>(json).fish) map[e.id] = e; }
            catch (Exception) { map.Clear(); }
        }

        static void Store()
        {
            var s = new Save(); s.fish.AddRange(map.Values);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(s));
            PlayerPrefs.Save();
        }

        /// บันทึกปลาที่ตกได้ · คืน true ถ้าเป็นชนิดใหม่ในสมุด
        public static bool Record(string id, float kg)
        {
            Load();
            bool isNew = !map.TryGetValue(id, out var e);
            if (isNew) map[id] = e = new Entry { id = id };
            e.count++;
            if (kg > e.bestKg) e.bestKg = kg;
            Store();
            return isNew;
        }

        public static bool Has(string id) { Load(); return map.ContainsKey(id); }
        public static int Count(string id) { Load(); return map.TryGetValue(id, out var e) ? e.count : 0; }
        public static float BestKg(string id) { Load(); return map.TryGetValue(id, out var e) ? e.bestKg : 0; }
        public static int Discovered { get { Load(); return map.Count; } }

        // ใช้ในเทสต์
        public static void ResetForTests() { map = new Dictionary<string, Entry>(); PlayerPrefs.DeleteKey(Key); }
    }
}
