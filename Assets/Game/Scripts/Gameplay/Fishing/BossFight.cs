using UnityEngine;

namespace CozyFishing
{
    // บอสสามช่วงตามความคืบหน้า: ดิ่งลึก (ดึงหนัก) → พุ่งขึ้นผิวน้ำ (กระชากเป็นจังหวะ) → ว่ายเข้าแก่ง (ช่วงวิ่งต้องผ่อนสาย ไม่งั้นสายตึงพรวด)
    sealed class BossFight
    {
        readonly FishingController c;
        int phase; float runAt, runEnd, spikeAt;

        public BossFight(FishingController c) { this.c = c; }

        /// ช่วงสาม บอสกำลังวิ่งเข้าแก่ง (บอททดสอบผ่อนสายตอนนี้)
        public bool Running => phase == 3 && c.fight.Time < runEnd;
        public void Reset() => phase = 0;

        public void Step(float dt)
        {
            var fight = c.fight; var hud = c.hud; var rng = c.rng; var bob = c.bob;
            float p = fight.Progress;
            int ph = Mathf.Max(phase, p < 0.4f ? 1 : p < 0.7f ? 2 : 3);   // ช่วงเดินหน้าอย่างเดียว (ไม่สลับไปมาเมื่อความคืบหน้าแกว่งรอบเส้นแบ่ง)
            if (ph != phase)
            {
                phase = ph;
                hud.Toast(ph == 1 ? "ปลาบึกยักษ์ดิ่งลงลึก! ค่อย ๆ กรอ อย่าให้สายตึงเกิน" : ph == 2 ? "มันพุ่งขึ้นผิวน้ำ! ระวังกระชาก" : "มันว่ายเข้าแก่ง! ผ่อนสายตอนมันวิ่ง");
                CameraRig.Impulse(0.6f); runAt = fight.Time + 2.5f; spikeAt = fight.Time + 1.5f;
            }
            fight.PullBoost = ph == 1 ? 0.18f : ph == 2 ? 0.05f : 0.08f;
            fight.ReelTensionMul = 1f; fight.ReelProgMul = 1f;
            if (ph == 2 && fight.Time > spikeAt) { spikeAt = fight.Time + 2.2f + (float)rng.NextDouble() * 1.8f; fight.Tension = Mathf.Min(0.95f, fight.Tension + 0.16f); WaterFx.AddRipple(bob.x, bob.z, 2.2f); Sfx.Splash(); CameraRig.Impulse(0.4f); }
            if (ph == 3)
            {
                if (fight.Time > runAt && fight.Time > runEnd) { runEnd = fight.Time + 2.2f; runAt = runEnd + 2.5f + (float)rng.NextDouble() * 2f; hud.Toast("ผ่อนสาย! มันวิ่งเข้าแก่ง"); }
                bool running = fight.Time < runEnd;
                if (running) { fight.ReelTensionMul = 2.4f; fight.ReelProgMul = -0.35f; }
            }
            if (!Boss.Prayed && Mathf.Repeat(fight.Time, 7f) < dt) { hud.Toast("เงาพญานาคใต้น้ำ… สายสะบัดแรง"); fight.Tension = Mathf.Min(0.95f, fight.Tension + 0.12f); }   // ไม่ไหว้ศาล
            c.GaugeWobble = Mathf.Max(c.GaugeWobble, Boss.Prayed ? 0 : 0.5f);
        }
    }
}
