using UnityEngine;

namespace CozyFishing
{
    // ด่านเรือไดหมึก: หมึกไม่กินเบ็ดเอง ต้อง "ไดหมึก" แตะตามจังหวะไฟเขียวกะพริบ ให้หมึกเข้ามาในวงแสง · กดค้าง = เก็บสาย
    sealed class SquidJig
    {
        public const float Period = 1.6f, Peak = 0.2f;
        /// 0..1 ความสว่างไฟล่อหมึกตามจังหวะ (ยอด = จังหวะที่ควรแตะ)
        public static float Pulse => 1 - Mathf.Clamp01(Mathf.Abs(Mathf.Repeat(Time.time, Period) - Peak) / 0.3f);

        readonly FishingController c;
        int streak; float held;

        public SquidJig(FishingController c) { this.c = c; }

        public void ResetStreak() => streak = 0;

        /// กดค้างเกิน 0.8 วิ = เก็บสาย
        public bool HoldToReel(bool down, float dt)
        {
            held = down ? held + dt : 0;
            if (held > 0.8f) { held = 0; return true; }
            return false;
        }

        public void Tap()
        {
            var hud = c.hud; var bite = c.bite;
            bool good = Mathf.Abs(Mathf.Repeat(Time.time, Period) - Peak) < 0.24f;
            WaterFx.AddRipple(c.bob.x, c.bob.z, good ? 0.9f : 0.5f); c.bob.y -= 0.1f;
            if (good)
            {
                streak++;
                bite.At = Mathf.Max(c.t + 0.6f, bite.At - (3.2f + streak * 0.9f));
                hud.Toast(streak >= 3 ? "หมึกรุมเข้ามาในแสงไฟแล้ว!" : streak == 2 ? "ดีมาก! เห็นเงาหมึกวนใต้เรือ" : "จังหวะดี! หมึกเริ่มเข้ามาใกล้");
                if (streak >= 2) Sfx.Nibble();
            }
            else { streak = 0; bite.At = Mathf.Max(c.t + 0.6f, bite.At - 0.4f); hud.Toast("ยังไม่ตรงจังหวะไฟ ลองแตะตอนไฟเขียวสว่างสุด"); }
        }
    }
}
