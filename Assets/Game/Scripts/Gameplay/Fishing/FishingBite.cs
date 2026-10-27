using System.Collections.Generic;
using UnityEngine;

namespace CozyFishing
{
    // ปลาตัวไหนจะกินเหยื่อ (สุ่มตามด่าน/เวลา/คดี/ดวล/บอส) และกินเมื่อไร (ตอดเบา ๆ ก่อน แล้วทุ่นจม)
    sealed class FishingBite
    {
        readonly FishingController c;
        public float At, End;
        readonly List<float> nibbles = new List<float>();
        bool nibbling;

        public FishingBite(FishingController c) { this.c = c; }

        bool Bot => FishingController.Bot;

        public void Roll()
        {
            // สืบคดี: ระหว่างตามหาเบาะแส ปลาที่พาเบาะแส/ขวดจดหมาย มีโอกาสโผล่มากขึ้นเมื่ออยู่ในเงื่อนไข
            if (Duel.Active && !Duel.Done) { c.fish = Duel.Next; return; }   // ดวล: ปลาชุดเดียวกับเพื่อน
            var o = Mystery.Override(c.world.StageId, Story.S.time, c.NearSpecial, c.NearDock, c.rng);
            if (o.HasValue) { c.fish = o.Value; return; }
            RollNormal();
            BossRoll();
        }

        // ดวงประจำวัน: ฟ้าครึ้ม/ฝน = ขยะบ่อยขึ้น · วันฝนหนักบางวันปลาแทบไม่กินเหยื่อ
        void RollNormal()
        {
            var world = c.world; var rng = c.rng;
            c.fish = Bot && GameUrl.Value.Contains("rarefish") ? FishRoller.Roll(world.StageId, true, 3f, 0f, Story.S.time, rng, 1f)   // ทดสอบ/ถ่ายคลิป: ปลาหายากขึ้นบ่อย
                : Story.S.caught == 0 ? FishRoller.Roll(world.StageId, false, 1f, 0f, Story.S.time, rng)   // ตัวแรก: ปลาจริงเสมอ
                : Story.S.dead && rng.NextDouble() < 0.85
                ? FishRoller.Roll(world.StageId, false, 0, 1f, Story.S.time, rng)
                : FishRoller.Roll(world.StageId, c.NearSpecial, world.DepthAt(c.bob.x, c.bob.z), Story.JunkChance, Story.S.time, rng, FishingController.Haunt);
        }

        // แม่น้ำโขง: บอสไม่ได้มาจากการสุ่มปกติ (chance 0) · มีโอกาสโผล่เมื่อพร้อม (สัปดาห์นี้ยังไม่มีใครในเครื่องนี้จับได้ + อุปกรณ์ถึง)
        void BossRoll()
        {
            var rng = c.rng;
            bool force = Bot && GameUrl.Value.Contains("boss");   // ทดสอบ: บอสโผล่ทุกครั้ง ไม่ต้องมีอุปกรณ์
            if (c.world.StageId != Boss.Stage || c.fish.IsJunk || (!Boss.Eligible && !force)) return;
            if (!force && rng.NextDouble() > (Boss.Prayed ? 0.13 : 0.08) * (c.NearSpecial ? 2 : 1)) return;
            var sp = FishDatabase.Get(Boss.Id); if (sp == null) return;
            float k = (float)rng.NextDouble();
            c.fish = new Catch { species = sp, kg = (float)System.Math.Round(sp.kgMin + (sp.kgMax - sp.kgMin) * k, 1), cm = (int)(sp.cmMin + (sp.cmMax - sp.cmMin) * k) };
        }

        public void ClearNibbles() => nibbles.Clear();

        public void Plan()
        {
            var rng = c.rng; float t = c.t;
            At = t + (Bot && GameUrl.Value.Contains("fastcatch") ? 1.5f : (Story.S.caught == 0 ? 1.2f + (float)rng.NextDouble() * 1.3f : (2.2f + (float)rng.NextDouble() * 5.5f) * Story.BiteMul * Shop.BiteMul));   // ตัวแรกของเกม: กินเร็ว ติดใจตั้งแต่เหวี่ยงแรก
            if (c.Jigging && !(Bot && GameUrl.Value.Contains("fastcatch"))) { At = t + 18f + (float)rng.NextDouble() * 10f; c.jig.ResetStreak(); }   // ไม่ไดหมึก = แทบไม่มีหมึกเข้ามา
            nibbles.Clear();
            int n = rng.Next(3) + Tackle.ExtraNibbles;
            for (int i = 0; i < n; i++) nibbles.Add(At - 0.6f - (float)rng.NextDouble() * 1.8f);
            nibbles.Sort();
        }

        /// ทุ่นลอยรอ (มีตอด) · ถึงเวลาแล้วทุ่นจม → เข้าสถานะ Bite
        public void WaitStep(float now)
        {
            var world = c.world; float t = c.t;
            float y = world.WaterY + 0.02f + Mathf.Sin(now * 2.2f) * 0.015f;
            if (nibbles.Count > 0 && t > nibbles[0])
            {
                float kk = (t - nibbles[0]) / 0.25f;
                if (kk < 1) { y -= Mathf.Sin(kk * Mathf.PI) * 0.07f * Tackle.NibbleDepth; if (!nibbling) { nibbling = true; WaterFx.AddRipple(c.bob.x, c.bob.z, 0.5f); } }
                else { nibbles.RemoveAt(0); nibbling = false; }
            }
            c.bob.y = y;
            if (t > At)
            {
                c.Set(FishState.Bite); Sfx.Bite();
                End = c.fish.IsJunk ? 1.2f : c.fish.species.win * Tackle.BiteWindowMul;   // ทุ่นไว/ทุ่นโฟมในน้ำเชี่ยว/ทุ่นไฟตอนมืด: เห็นจมนานขึ้น
                WaterFx.AddRipple(c.bob.x, c.bob.z, 1.5f);
            }
        }
    }
}
