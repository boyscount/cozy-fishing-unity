using System;

namespace CozyFishing
{
    // เหยื่อปลอม (ร้านลุงส้ม แท็บ "ทุ่น สาย เบ็ด" หมวดเหยื่อปลอม): ใส่แล้วไม่ต้องรอทุ่นจม ต้องลากเหยื่อให้ปลาสนใจเอง
    //   แต่ละแบบมีจังหวะลากของตัวเอง ลากถูกจังหวะ แถบ "ปลาสนใจ" เต็ม = ปลาฮุบเหยื่อ (เข้าช่วงวัดเบ็ดตามปกติ)
    //   ใช้ซ้ำได้ไม่หมด · แทนเหยื่อสด (กุ้ง/รำ/กบ ไม่ถูกใช้ตอนใส่เหยื่อปลอม) · ด่านไดหมึกใช้ไม่ได้
    public enum LureStyle { TapPause, Hold, HoldRelease, QuickTap }

    public class LureDef
    {
        public string id; public LureStyle style;
        public string hint;            // บอกวิธีลากบนจอ
        public float gapMin, gapMax;   // TapPause/QuickTap: ช่วงห่างระหว่างแตะที่ดี (วิ) · HoldRelease: ช่วงกดค้างที่ดี
        public float step;             // ระยะที่เหยื่อเข้าหาตัวต่อการแตะ (ม.) หรือต่อวินาทีที่กดค้าง
    }

    public static class Lures
    {
        public const string None = "lure_none";
        public static readonly LureDef[] Defs =
        {
            new LureDef { id = "lure_frog", style = LureStyle.TapPause, gapMin = 0.7f, gapMax = 1.35f, step = 0.55f, hint = "แตะ… รอ… แตะ (ให้กบกระโดดเป็นจังหวะ)" },
            new LureDef { id = "lure_spinner", style = LureStyle.Hold, step = 0.9f, hint = "กดค้างกรอสม่ำเสมอ ใบหมุนจะวาวล่อปลา" },
            new LureDef { id = "lure_spoon", style = LureStyle.HoldRelease, gapMin = 0.8f, gapMax = 1.8f, step = 1.3f, hint = "กดค้างกรอเร็ว แล้วปล่อยให้ช้อนพลิ้วจม" },
            new LureDef { id = "lure_swim", style = LureStyle.QuickTap, gapMin = 0.35f, gapMax = 0.7f, step = 0.28f, hint = "แตะถี่ ๆ สม่ำเสมอ ให้ปลายางส่ายหาง" },
        };
        public static LureDef Get(string id) => Array.Find(Defs, d => d.id == id);
        public static LureDef Equipped => Get(Tackle.Equipped(Tackle.Lure));
        /// ใส่เหยื่อปลอมอยู่ และด่านนี้ใช้ได้ (ด่านไดหมึกใช้วิธีไดหมึกแทน)
        public static bool Active => Equipped != null && Story.S.stage != "squid";

        /// น้ำหนักการสุ่มพันธุ์ปลาเมื่อใส่เหยื่อปลอม: ปลาล่าเหยื่อ/ปลาใหญ่ชอบ ปลาเล็กกินไม่เข้า
        public static float Weight(FishSpecies s)
        {
            var l = Active ? Equipped.id : null;
            return l switch
            {
                "lure_frog" => s.kgMax >= 1.5f ? 2.5f : s.kgMax < 0.3f ? 0.2f : 1f,     // กบยาง: ปลาช่อน ชะโด ปลานักล่า
                "lure_spinner" => s.dash >= 1.0f ? 2f : s.kgMax < 0.2f ? 0.4f : 1f,      // สปินเนอร์: ปลาว่องไว
                "lure_spoon" => s.stars >= 3 ? 2.2f : s.stars <= 1 ? 0.6f : 1f,          // ช้อน: ปลาดาวสูง
                "lure_swim" => s.kgMax >= 5f ? 3f : s.kgMax < 0.5f ? 0.3f : 1f,          // ปลายาง: ปลาตัวโต
                _ => 1f,
            };
        }
    }

    /// มินิเกมลากเหยื่อ (ตรรกะล้วน ทดสอบแยกได้): Step ทุกเฟรม → ระยะที่เหยื่อเคลื่อน + แถบปลาสนใจ
    public class RetrieveModel
    {
        public readonly LureDef def;
        public float Interest, Time, Moved;           // Moved = ระยะที่เหยื่อเคลื่อนเข้าหาตัวในเฟรมนี้
        public bool Struck => Interest >= threshold;
        public string Feedback;                       // "ดี!" / "เร็วไป" / "ช้าไป" ล่าสุด
        public float FeedbackAt = -9;
        readonly float windowMul, gainMul, threshold;
        float lastTap = -1, holdStart = -1, heldFor;

        public RetrieveModel(LureDef def, float windowMul = 1f, float gainMul = 1f, float threshold = 1f)
        {
            this.def = def; this.windowMul = windowMul; this.gainMul = gainMul; this.threshold = threshold;
        }

        float Lo => def.gapMin - (def.gapMax - def.gapMin) * (windowMul - 1) * 0.5f;
        float Hi => def.gapMax + (def.gapMax - def.gapMin) * (windowMul - 1) * 0.5f;
        void Say(string s) { Feedback = s; FeedbackAt = Time; }

        /// พร้อมแตะครั้งถัดไปแค่ไหน (0 = เพิ่งแตะ, 1 = อยู่ในช่วงที่ดี) — ใช้วาดจุดจังหวะบนจอ
        public float Readiness
        {
            get
            {
                if (def.style == LureStyle.Hold) return heldFor > 0 ? 1 : 0;
                float since = def.style == LureStyle.HoldRelease ? (holdStart >= 0 ? Time - holdStart : 0) : (lastTap < 0 ? Hi : Time - lastTap);
                return since < Lo ? since / Lo : since <= Hi ? 1f : Math.Max(0, 1 - (since - Hi));
            }
        }

        public void Step(float dt, bool held, bool tapped)
        {
            Time += dt; Moved = 0;
            switch (def.style)
            {
                case LureStyle.TapPause:
                case LureStyle.QuickTap:
                    if (tapped)
                    {
                        Moved = def.step;
                        if (lastTap < 0) { Interest += 0.06f * gainMul; Say("ดี!"); }
                        else
                        {
                            float gap = Time - lastTap;
                            if (gap < Lo) { Interest -= 0.12f; Say("เร็วไป"); }
                            else if (gap <= Hi) { Interest += (def.style == LureStyle.QuickTap ? 0.09f : 0.2f) * gainMul; Say("ดี!"); }
                            else { Interest += 0.03f * gainMul; Say("ช้าไป"); }
                        }
                        lastTap = Time;
                    }
                    else if (lastTap >= 0 && Time - lastTap > Hi + 0.6f) Interest -= 0.1f * dt;   // หยุดนานเกิน ปลาเลิกสนใจ
                    break;
                case LureStyle.Hold:
                    if (held) { heldFor += dt; Moved = def.step * dt; Interest += (heldFor > 0.6f ? 0.16f : 0.05f) * gainMul * dt; }   // กรอนิ่ง ๆ ต่อเนื่องถึงได้ผล
                    else { if (heldFor > 0) Say("กรอต่อเนื่องอย่าหยุด"); heldFor = 0; Interest -= 0.12f * dt; }
                    break;
                case LureStyle.HoldRelease:
                    if (held) { if (holdStart < 0) holdStart = Time; Moved = def.step * dt; }
                    else if (holdStart >= 0)
                    {
                        float d = Time - holdStart; holdStart = -1;
                        if (d < Lo) { Interest -= 0.08f; Say("กรอสั้นไป"); }
                        else if (d <= Hi) { Interest += 0.26f * gainMul; Say("ดี! ช้อนพลิ้ว"); }
                        else { Interest -= 0.12f; Say("กรอนานไป"); }
                    }
                    break;
            }
            Interest = Math.Max(0, Math.Min(threshold, Interest));
        }

        /// บอทอัตโนมัติ: กดตามจังหวะที่ดีของเหยื่อแต่ละแบบ
        public bool BotHold(float t)
        {
            switch (def.style)
            {
                case LureStyle.Hold: return true;
                case LureStyle.HoldRelease: return holdStart < 0 ? t % 2.2f < 0.1f : Time - holdStart < (def.gapMin + def.gapMax) / 2;
                default:
                    float mid = (def.gapMin + def.gapMax) / 2;
                    return lastTap < 0 || Time - lastTap >= mid;
            }
        }
    }
}
