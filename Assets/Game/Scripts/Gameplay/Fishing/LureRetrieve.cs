using UnityEngine;

namespace CozyFishing
{
    // เหยื่อปลอม: มินิเกมลากเหยื่อ (ตรรกะอยู่ใน Lures.cs/RetrieveModel) · ไม่มีปลากินเองตามเวลา ต้องลากให้ปลาสนใจจนฮุบ
    sealed class LureRetrieve
    {
        readonly FishingController c;
        public RetrieveModel Run;
        bool tap; float ripple;

        public LureRetrieve(FishingController c) { this.c = c; }

        public void Tap() => tap = true;
        public void Clear() => Run = null;

        /// เหยื่อตกน้ำ: ใส่เหยื่อปลอมอยู่ (และไม่ใช่ด่านไดหมึก) → เริ่มลาก
        public void Begin()
        {
            Run = null;
            if (Lures.Active && !c.Jigging)
            {
                Run = new RetrieveModel(Lures.Equipped, Skills.RetrieveWindow, Skills.RetrieveGain, Skills.RetrieveThreshold);
                c.bite.At = float.MaxValue; c.bite.ClearNibbles();   // ไม่มีปลากินเองตามเวลา ต้องลากให้ปลาสนใจ
                c.hud.Toast(Lures.Equipped.hint);
            }
        }

        public void Step(float dt, bool held)
        {
            var player = c.player; var world = c.world;
            Run.Step(dt, held, tap); tap = false;
            if (Run.Moved > 0)
            {
                var to = new Vector3(player.Pos.x - c.bob.x, 0, player.Pos.z - c.bob.z);
                float dist = to.magnitude;
                if (dist > 0.01f) c.bob += to / dist * Mathf.Min(dist, Run.Moved);
                if (Time.time > ripple) { ripple = Time.time + 0.25f; WaterFx.AddRipple(c.bob.x, c.bob.z, 0.45f); }
            }
            c.bob.y = world.WaterY - 0.04f + Mathf.Sin(c.t * 6) * 0.02f;
            if (Run.Struck)   // ปลาฮุบเหยื่อ → ช่วงวัดเบ็ดตามปกติ
            {
                Run = null; c.bite.At = c.t;
                c.hud.Toast("ปลาฮุบเหยื่อแล้ว! กดเลย!");
                return;
            }
            bool canCastHere = world.CanCast(c.bob.x, c.bob.z);
            if (new Vector2(player.Pos.x - c.bob.x, player.Pos.z - c.bob.z).magnitude < 2.6f || !canCastHere)
            {
                c.hud.Toast("ลากถึงฝั่งแล้ว ไม่มีปลาตาม ลองจังหวะใหม่นะ");
                c.StartRetrieve();
            }
        }
    }
}
