using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CozyFishing
{
    // ของแต่งตัว/แต่งเบ็ด (สวยอย่างเดียว ไม่เพิ่มพลัง → กระดานคะแนนยังยุติธรรม) ซื้อด้วย "เกล็ดทอง"
    //   เกล็ดทองได้จากการเล่น: ภารกิจรายวันครบ · ปลาเด่น · เทศกาลรายสัปดาห์ · บอส · ดวล — ภายหลังเปิดซื้อด้วยเงินจริงได้ (ระบบเดียวกัน)
    public static class Cosmetics
    {
        public const string Hat = "hat", Shirt = "shirt", Bobber = "bobber", Line = "line", Card = "card";
        public static readonly string[] Slots = { Hat, Shirt, Bobber, Line, Card };
        public static string SlotName(string s) => s switch { Hat => "หมวก", Shirt => "เสื้อ", Bobber => "ทุ่น", Line => "สายเบ็ด", Card => "กรอบการ์ดแชร์", _ => s };

        public class Item { public string id, slot, name, desc; public int price; public Color color, color2; }
        static Color C(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.white;
        public static readonly Item[] Items =
        {
            new Item { id = "hat_straw", slot = Hat, name = "หมวกสานของพ่อ", price = 0, color = C("#f0e2c0"), color2 = C("#a54a36"), desc = "หมวกใบเก่าที่ใส่มาทุกวัน" },
            new Item { id = "hat_red", slot = Hat, name = "หมวกแดงงานวัด", price = 12, color = C("#c8362c"), color2 = C("#f0d070"), desc = "ซื้อมาจากงานวัดเมื่อปีก่อน" },
            new Item { id = "hat_black", slot = Hat, name = "หมวกดำพรานคืนเดือนมืด", price = 20, color = C("#2a2622"), color2 = C("#b8342a"), desc = "เหมาะกับตกปลากลางคืนกับผีป่า" },
            new Item { id = "hat_gold", slot = Hat, name = "หมวกทองแชมป์ปลาใหญ่", price = 40, color = C("#e0b040"), color2 = C("#7a2a18"), desc = "ให้คนทั้งท่าน้ำรู้ว่าใครเจ้าแห่งปลาใหญ่" },
            new Item { id = "shirt_blue", slot = Shirt, name = "เสื้อยืดตัวเก่ง", price = 0, color = Color.white, desc = "เสื้อตัวเดิม" },
            new Item { id = "shirt_mohom", slot = Shirt, name = "เสื้อม่อฮ่อม", price = 15, color = C("#3a4a7a"), desc = "ผ้าย้อมครามแบบชาวเหนือ" },
            new Item { id = "shirt_red", slot = Shirt, name = "เสื้อแดงเทศกาล", price = 15, color = C("#c04a3a"), desc = "ใส่ไปงานเทศกาลปลาใหญ่" },
            new Item { id = "shirt_green", slot = Shirt, name = "เสื้อเขียวป่า", price = 15, color = C("#5a7a48"), desc = "กลมกลืนกับต้นไม้ริมน้ำ" },
            new Item { id = "bob_red", slot = Bobber, name = "ทุ่นไม้ไผ่ยอดแดง", price = 0, color = C("#d23a2c"), color2 = C("#c8ff5a"), desc = "ทุ่นที่พ่อเหลาเอง" },
            new Item { id = "bob_green", slot = Bobber, name = "ทุ่นเรืองแสงเขียว", price = 10, color = C("#40d060"), color2 = C("#a0ff80"), desc = "มองเห็นง่ายตอนกลางคืน" },
            new Item { id = "bob_gold", slot = Bobber, name = "ทุ่นทองคำเปลว", price = 25, color = C("#e8c050"), color2 = C("#fff0a0"), desc = "ปิดทองแบบพระพุทธรูป" },
            new Item { id = "bob_naga", slot = Bobber, name = "ทุ่นเกล็ดพญานาค", price = 35, color = C("#2a8a5a"), color2 = C("#ff5030"), desc = "ลวดลายเกล็ดเขียวทอง ปลายแดงเหมือนลูกไฟพญานาค" },
            new Item { id = "line_white", slot = Line, name = "สายเอ็นใส", price = 0, color = C("#f8f8f0"), desc = "สายเอ็นธรรมดา" },
            new Item { id = "line_gold", slot = Line, name = "สายเอ็นสีทอง", price = 12, color = C("#ffd060"), desc = "แวววาวตอนต้องแดด" },
            new Item { id = "line_red", slot = Line, name = "สายเอ็นแดง", price = 10, color = C("#ff5a4a"), desc = "เห็นชัดตอนน้ำขุ่น" },
            new Item { id = "line_green", slot = Line, name = "สายเอ็นเขียวมรกต", price = 10, color = C("#5aff9a"), desc = "เข้ากับทุ่นเรืองแสง" },
            new Item { id = "card_plain", slot = Card, name = "กรอบไม้ไผ่", price = 0, color = C("#9e7333"), desc = "กรอบการ์ดแชร์แบบเดิม" },
            new Item { id = "card_gold", slot = Card, name = "กรอบทองลายไทย", price = 20, color = C("#e0b040"), desc = "การ์ดแชร์ปลาใหญ่กรอบทอง ดูหรูขึ้น" },
            new Item { id = "card_naga", slot = Card, name = "กรอบพญานาคแดงทอง", price = 30, color = C("#b8342a"), desc = "สำหรับคนที่จับปลาบึกยักษ์ได้แล้ว" },
        };
        public static Item Get(string id) => Items.FirstOrDefault(i => i.id == id);

        const string Key = "cozy.cosmetic.v1";
        [Serializable] class State { public int scales = 10; public List<string> owned = new List<string>(); public List<string> equipped = new List<string>(); }
        static State s;
        static State S
        {
            get
            {
                if (s == null) { try { var j = PlayerPrefs.GetString(Key, ""); if (j.Length > 0) s = JsonUtility.FromJson<State>(j); } catch (Exception) { s = null; } }
                return s ??= new State();
            }
        }
        static void Save() { try { PlayerPrefs.SetString(Key, JsonUtility.ToJson(s)); PlayerPrefs.Save(); } catch (Exception) { } Cloud.MarkDirty(); Changed?.Invoke(); }
        public static void Reload() { s = null; Changed?.Invoke(); }
        public static event Action Changed;

        public static int Scales => S.scales;
        public static bool Owns(Item it) => it.price == 0 || S.owned.Contains(it.id);
        public static Item Equipped(string slot)
        {
            foreach (var id in S.equipped) { var it = Get(id); if (it != null && it.slot == slot) return it; }
            return Items.First(i => i.slot == slot && i.price == 0);
        }
        public static bool Buy(Item it)
        {
            if (Owns(it) || S.scales < it.price) return false;
            S.scales -= it.price; S.owned.Add(it.id); Equip(it); return true;
        }
        public static void Equip(Item it)
        {
            if (!Owns(it)) return;
            S.equipped.RemoveAll(id => Get(id)?.slot == it.slot); S.equipped.Add(it.id); Save();
        }
        /// ได้เกล็ดทองจากการเล่น · คืนข้อความแจ้ง
        public static string Earn(int n, string why)
        {
            if (n <= 0) return "";
            S.scales += n; Save(); Debug.Log($"[Cosmetics] +{n} scales ({why})");
            return $"ได้เกล็ดทอง +{n} ({why})";
        }
    }
}
