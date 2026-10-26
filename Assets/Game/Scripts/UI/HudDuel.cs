using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // ท้าดวลผ่านลิงก์ + หน้าอุปกรณ์ของฉัน
    public partial class Hud
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CozyShareLink(string title, string text, string url);
#else
        static void CozyShareLink(string title, string text, string url) { GUIUtility.systemCopyBuffer = url; Debug.Log("[Share] link " + url); }
#endif

        /// เริ่มดวลใหม่จากหน้าเช้า: สุ่มเลขดวล ด่านที่ไปล่าสุด → เข้าโหมดตกปลาอิสระแล้วออกเดินทาง
        void StartDuelFromMorning()
        {
            var stage = Stages.Get(Story.S.stage).id;
            Duel.Start(Random.Range(100000, 999999), stage);
            if (!Story.FreePlay) Story.EnterFreePlay();
            Story.S.stage = Duel.Stage;
            Hide(morning);
            Toast($"ดวล 5 ตัวที่{Stages.Get(Duel.Stage).name} · น้ำหนักรวมยิ่งมากยิ่งดี แล้วส่งลิงก์ให้เพื่อนเล่นแข่ง");
            onMorningGo?.Invoke(Duel.Stage);
        }

        /// ครบห้าตัว: สรุปผล (เทียบกับคะแนนเพื่อนถ้ามาจากลิงก์) + แชร์ลิงก์ท้า
        public void ShowDuelResult()
        {
            Debug.Log($"[Duel] result active={Duel.Active} total={Duel.TotalKg:0.00} vs={Duel.VsKg:0.00}");
            if (!Duel.Active) return;
            string me = Cloud.PlayerName.Split('#')[0];
            string log = string.Join(" · ", Duel.Log);
            string head;
            if (Duel.VsKg >= 0)
            {
                string who = string.IsNullOrEmpty(Duel.VsName) ? "เพื่อน" : Duel.VsName;
                head = Duel.TotalKg > Duel.VsKg + 0.004f ? $"ชนะ! คุณ {Duel.TotalKg:0.00} กก. · {who} {Duel.VsKg:0.00} กก."
                     : Duel.TotalKg < Duel.VsKg - 0.004f ? $"แพ้ไปนิด… คุณ {Duel.TotalKg:0.00} กก. · {who} {Duel.VsKg:0.00} กก."
                     : $"เสมอ! ทั้งคู่ {Duel.TotalKg:0.00} กก.";
            }
            else head = $"ดวลจบแล้ว! น้ำหนักรวม {Duel.TotalKg:0.00} กก.";
            Sfx.Coin();
            bool won = Duel.VsKg >= 0 && Duel.TotalKg > Duel.VsKg + 0.004f;
            head += " · " + Cosmetics.Earn(won ? 3 : 1, won ? "ชนะดวล" : "ดวลจบ");
            var link = Duel.Link(me);
            Ask($"{head}\n{log}\nส่งลิงก์ให้เพื่อน แล้วดูว่าใครตกปลาชุดเดียวกันได้หนักกว่า",
                ("ส่งลิงก์ท้าเพื่อน", () => { CozyShareLink("ท้าดวลตกปลา!", $"ฉันตกได้ {Duel.TotalKg:0.00} กก. ใน 5 ตัว ที่{Stages.Get(Duel.Stage).name} มาดวลกัน!", link); Duel.End(); }),
                ("ตกปลาต่อ", () => Duel.End()));
        }

        /// หน้าอุปกรณ์ของฉัน: ค่าพลังของคัน รอก เหยื่อ ไฟล่อ (เห็นชัดว่าของที่ซื้อช่วยอะไร)
        void ShowGear()
        {
            var s = Story.S;
            string Rod() => s.rod switch { 1 => "คันเบ็ดไฟเบอร์", 2 => "คันคาร์บอนเบา", 3 => "คันเบ็ดครูช่างฝีมือ", _ => "คันไม้ไผ่ของพ่อ" };
            string Reel() => s.reel switch { 1 => "รอกสปินนิ่ง", 2 => "รอกเบทคาสติ้ง", 3 => "รอกทองเหลืองโบราณ", _ => "รอกมือ" };
            int tension = Mathf.RoundToInt((1 - Shop.RodTension) * 100), reel = Mathf.RoundToInt((Shop.ReelSpeed - 1) * 100);
            string bait = Shop.ActiveBait switch { "shrimp" => "ปลาหายาก/ดาวสูงเข้าบ่อยขึ้น", "bran" => "ปลากินเร็วขึ้น", "frog" => "ปลาตัวใหญ่ขึ้น", _ => "เหยื่อพื้นฐาน" };
            Ask($"อุปกรณ์ของฉัน\n" +
                $"คันเบ็ด: {Rod()} · เหวี่ยงไกล {Shop.CastDist:0} ม. · สายตึงช้าลง {tension}%\n" +
                $"รอก: {Reel()} · กรอสายเร็วขึ้น {reel}%\n" +
                $"เหยื่อที่ใช้: {Shop.BaitName(Shop.ActiveBait)} (เหลือ {(Shop.ActiveBait == "worm" ? "ไม่จำกัด" : Shop.BaitLeft(Shop.ActiveBait).ToString())}) · {bait}\n" +
                $"ไฟล่อใต้น้ำ: {(Shop.HasUwLight ? "มี · มองเห็นใต้น้ำชัดขึ้น ปลากินเร็วขึ้นนิด" : "ยังไม่มี (ร้านลุงส้ม)")}\n" +
                $"ปลาบึกยักษ์แม่โขง: {(Boss.GearReady ? "อุปกรณ์พร้อมสู้แล้ว" : "ต้องมีคันคาร์บอนเบา + รอกเบทคาสติ้งขึ้นไป")}",
                ("ปิด", null));
        }
    }
}
