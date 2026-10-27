using UnityEngine;

namespace CozyFishing
{
    // โหมดทดสอบอัตโนมัติ (?bot ใน URL หรือ -bot): เล่นเองครบวง เหวี่ยง → รอ → ดึง → ตีเบ็ด → สู้ปลา → ปิดการ์ด
    sealed class FishingBot
    {
        readonly FishingController c;
        float next;

        public FishingBot(FishingController c) { this.c = c; }

        /// บอทกดปุ่มค้างอยู่ไหมในเฟรมนี้
        public bool Held()
        {
            float t = c.t; var fight = c.fight; var strike = c.strike;
            switch (c.State)
            {
                case FishState.Waiting:   // ทดสอบ: บอทไดหมึกตามจังหวะไฟ
                    if (c.Jigging && FishingController.JigPulse > 0.85f && Time.time > next) { next = Time.time + 0.8f; return true; }
                    return false;
                case FishState.Free: return t > 1.2f && t < 1.2f + 0.9f;            // ง้าง ~0.9 วิ แล้วปล่อย
                case FishState.Charging: return c.cast.Charge < 0.45f;   // บอททดสอบเหวี่ยงราว 9 ม. เท่าเดิม
                case FishState.Bite: return t > 0.25f;                              // รอเห็นทุ่นจมแป๊บหนึ่งค่อยกด
                case FishState.Strike:
                    bool inZone = Mathf.Abs(strike.Pos - strike.Center) < strike.Width * 0.35f;
                    if (inZone && Time.time > next) { next = Time.time + 0.35f; return true; }
                    return false;
                case FishState.Fight: if (c.BossHooked && c.boss.Running) return false;   // บอทผ่อนสายตอนบอสวิ่งเข้าแก่ง
                    return fight.Tension < Mathf.Min(0.8f, fight.ZoneCenter + fight.ZoneWidth * 0.3f);
                case FishState.Caught: if (c.hud.CatchCardOpen && t > 6f) c.hud.CloseCard(false); return false;
                default: return false;
            }
        }
    }
}
