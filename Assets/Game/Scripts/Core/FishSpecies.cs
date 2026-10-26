using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CozyFishing
{
    // ข้อมูลปลาหนึ่งชนิด (โหลดจาก Resources/Data/species.json ที่ส่งออกจากเวอร์ชันเว็บ)
    [Serializable]
    public class FishSpecies
    {
        public string id, stage, name, en, pattern, time, desc;
        public string photoId;   // ใช้รูป/โมเดลรูปของชนิดอื่น (ชนิดเดียวกันต่างด่าน เช่น ชะโดเขื่อน = chado) · ว่าง = ใช้ id
        public string top, belly, fin;
        public int stars, chance, price;
        public float kgMin, kgMax, cmMin, cmMax;
        public float[] shape;
        public float pull, dash, prog, win;
        public bool rare, glow, whiskers;

        public Color TopColor => Hex(top);
        public Color BellyColor => Hex(belly);
        public Color FinColor => Hex(fin);
        public static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.white;

        // ภาพถ่ายปลาจริง (Resources/Fish/<id>.png ตัดพื้นหลังแล้ว หัวหันซ้าย) + โมเดลพองจากภาพ (<id>_mesh) — ไม่มีก็ใช้ปลาโมเดลเดิม
        [NonSerialized] Texture2D photo; [NonSerialized] bool photoLoaded;
        public Texture2D Photo { get { if (!photoLoaded) { photo = Resources.Load<Texture2D>("Fish/" + (string.IsNullOrEmpty(photoId) ? id : photoId)); photoLoaded = true; } return photo; } }
        public Mesh PhotoMesh => Resources.Load<Mesh>("Fish/" + (string.IsNullOrEmpty(photoId) ? id : photoId) + "_mesh");
        public string Credit => FishCredits.Get(string.IsNullOrEmpty(photoId) ? id : photoId);
    }

    // เครดิตภาพปลา (Resources/Fish/credits.txt: id|ข้อความ) — ภาพจาก Wikimedia Commons สัญญาอนุญาตเสรี
    public static class FishCredits
    {
        static Dictionary<string, string> map;
        public static string Get(string id)
        {
            if (map == null)
            {
                map = new Dictionary<string, string>();
                var ta = Resources.Load<TextAsset>("Fish/credits");
                if (ta != null)
                    foreach (var line in ta.text.Split('\n'))
                    {
                        int bar = line.IndexOf('|');
                        if (bar > 0) map[line.Substring(0, bar).Trim()] = line.Substring(bar + 1).Trim();
                    }
            }
            return map.TryGetValue(id, out var s) ? s : null;
        }
    }

    [Serializable]
    class SpeciesFile { public FishSpecies[] species; }

    public static class FishDatabase
    {
        static List<FishSpecies> all;
        static Dictionary<string, FishSpecies> byId;

        public static IReadOnlyList<FishSpecies> All { get { Load(); return all; } }
        public static FishSpecies Get(string id) { Load(); return byId.TryGetValue(id, out var s) ? s : null; }
        public static IEnumerable<FishSpecies> ForStage(string stage) => All.Where(s => s.stage == stage);

        static void Load()
        {
            if (all != null) return;
            var ta = Resources.Load<TextAsset>("Data/species");
            all = ta != null ? JsonUtility.FromJson<SpeciesFile>(ta.text).species.ToList() : new List<FishSpecies>();
            byId = all.ToDictionary(s => s.id);
        }

        public static readonly string[] Rarity = { "", "ธรรมดา", "ไม่บ่อย", "หายาก", "ตำนาน" };
    }

    // ผลการตกปลาหนึ่งครั้ง (ปลา หรือ ขยะ)
    public struct Catch
    {
        public FishSpecies species;
        public float kg;
        public int cm;
        public string junk;           // ไม่ว่าง = ได้ขยะแทนปลา
        public bool IsJunk => !string.IsNullOrEmpty(junk);
    }
}
