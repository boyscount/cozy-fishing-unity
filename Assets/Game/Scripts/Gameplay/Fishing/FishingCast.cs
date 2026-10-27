using UnityEngine;

namespace CozyFishing
{
    // เล็ง + เหวี่ยง: กดค้างง้าง (วงเล็งบนผิวน้ำ) → ปล่อย → ทุ่นพุ่งเป็นโค้งไปตกจุดเล็ง
    sealed class FishingCast
    {
        public const float ThrowDelay = 0.16f;   // จากปล่อยปุ่มถึงทุ่นพุ่งออก (ตรงกับเฟรมแขนสะบัดของท่า Mixamo)
        public static float MaxCast => Shop.CastDist;   // ง้างเต็มแรง: คันไม้ไผ่ 18 ม. · คันจากร้านไกลขึ้นถึง 30 ม.

        readonly FishingController c;
        readonly Renderer aimRenderer;
        readonly MaterialPropertyBlock aimMpb;
        public float Charge;
        public Vector3 Target;
        public bool OnWater;
        float flightDur;

        public FishingCast(FishingController c)
        {
            this.c = c;
            aimRenderer = c.aim.GetComponentInChildren<Renderer>();
            aimMpb = new MaterialPropertyBlock();
        }

        Vector3 CastPoint(float k) { var p = c.player; var f = p.Forward; float d = 1.3f + k * (MaxCast - 1.3f); return new Vector3(p.Pos.x + f.x * d, 0, p.Pos.z + f.z * d); }

        public void ChargeStep(float dt, Vector3 tip, float now)
        {
            var world = c.world; var aim = c.aim;
            Charge = Mathf.Min(1, Charge + dt / 1.6f);   // ระยะไกลขึ้น → ง้างนานขึ้นนิด กะระยะใกล้ได้แม่นเหมือนเดิม
            c.bob = tip + new Vector3(0, -0.55f, 0);
            var p = CastPoint(Charge);
            bool w = world.CanCast(p.x, p.z);
            aim.SetActive(true);
            aim.transform.position = new Vector3(p.x, (w ? world.WaterY : world.GroundH(p.x, p.z)) + 0.04f, p.z);
            aim.transform.localScale = new Vector3(0.8f, 0.005f, 0.8f) * (1 + Mathf.Sin(now * 8) * 0.08f);   // วงแบน ๆ บนผิวน้ำ (y คงบางไว้)
            aimMpb.SetColor("_BaseColor", w ? Color.white : new Color(1f, 0.54f, 0.44f));
            aimRenderer.SetPropertyBlock(aimMpb);
        }

        public void Release()
        {
            var world = c.world;
            Target = CastPoint(Charge);
            OnWater = world.CanCast(Target.x, Target.z);
            Target.y = OnWater ? world.WaterY + 0.03f : world.OnSolid(Target.x, Target.z) ? world.StandY(Target.x, Target.z) + 0.05f : world.GroundH(Target.x, Target.z) + 0.08f;
            c.from = c.player.rodTip.position;
            flightDur = 0.5f + Vector3.Distance(c.from, Target) * 0.045f;
            c.flight = 0;
            c.Set(FishState.Casting);
        }

        /// ทุ่นลอยกลางอากาศ · คืน true เมื่อตกถึงจุดเล็ง
        public bool FlightStep(float dt, Vector3 tip)
        {
            // ช่วงแรกแขนสะบัดไปข้างหน้า ทุ่นยังติดปลายคัน แล้วค่อยพุ่งออกพร้อมจังหวะปล่อยสาย
            if (c.t < ThrowDelay) { c.bob = tip + new Vector3(0, -0.3f, 0); c.from = tip; return false; }
            c.flight += dt / flightDur;
            float k = Mathf.Min(1, c.flight);
            c.bob = Vector3.Lerp(c.from, Target, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * (1.5f + Vector3.Distance(c.from, Target) * 0.22f);
            return k >= 1;
        }
    }
}
