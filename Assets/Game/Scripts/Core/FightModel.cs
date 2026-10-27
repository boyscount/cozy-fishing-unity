using System;

namespace CozyFishing
{
    public enum FightResult { Ongoing, Caught, Snapped, Slack, Escaped }

    // มินิเกมดึงปลา: กดค้าง = กรอสาย (แรงตึงขึ้น), ปล่อย = ผ่อนสาย — รักษาเข็มไว้ในโซนเขียว
    // ปลา ★3 ขึ้นไป โซนเขียวแคบลงและเลื่อนไปมา — ย้ายมาจาก fightUpdate() ของเวอร์ชันเว็บ
    public class FightModel
    {
        public float Tension = 0.45f, Progress = 0.22f, Slack, Time;
        public float ZoneCenter = 0.5f, ZoneWidth = 0.6f;
        public bool Dashing { get; private set; }
        public float PullBoost, ReelTensionMul = 1f, ReelProgMul = 1f;   // บอส: ตั้งทุกเฟรมตามช่วงการสู้
        public bool Drag;                 // เบรกรอก (Tackle): สายตึงเกือบขาด รอกคลายสายให้ แลกกับปลาได้ระยะคืน
        public bool Slipping { get; private set; }
        public event Action OnSlip;
        readonly FishSpecies sp;
        readonly float rodK;
        readonly Random rng;
        float nextDash, dashEnd;
        public event Action OnDash;

        readonly float reelK;
        public FightModel(FishSpecies species, bool fiberRod, Random rng, float rodTension = 1f, float reelSpeed = 1f)
        {
            sp = species; this.rng = rng;
            rodK = (fiberRod ? 0.8f : 1f) * rodTension; reelK = reelSpeed;
            nextDash = 1f + (float)rng.NextDouble() * 1.5f;
        }

        public bool InZone => Math.Abs(Tension - ZoneCenter) < ZoneWidth / 2;

        public FightResult Step(float dt, bool reeling)
        {
            Time += dt;
            if (Time > nextDash)
            {
                dashEnd = Time + 0.4f + (float)rng.NextDouble() * 0.7f;
                nextDash = dashEnd + 0.8f + (float)rng.NextDouble() * 2.2f / (0.6f + sp.dash);
                OnDash?.Invoke();
            }
            Dashing = Time < dashEnd;
            var pull = (Dashing ? sp.dash : sp.pull * (0.6f + 0.4f * (float)Math.Sin(Time * 2.3f))) + PullBoost;
            if (sp.stars >= 3)
            {
                ZoneWidth = sp.stars >= 4 ? 0.36f : 0.48f;
                var c = 0.5f + (float)Math.Sin(Time * (sp.stars >= 4 ? 0.9f : 0.6f)) * 0.2f + (Dashing ? (float)Math.Sin(Time * 6) * 0.05f : 0f);
                ZoneCenter = Math.Max(ZoneWidth / 2 + 0.04f, Math.Min(0.92f - ZoneWidth / 2, c));
            }
            if (reeling)
            {
                Tension += (0.35f + pull * 0.5f) * rodK * ReelTensionMul * dt;
                Progress += sp.prog * reelK * ReelProgMul * dt * (InZone ? 1f : 0.35f) - (Dashing ? sp.dash * 0.06f * dt : 0f);
            }
            else
            {
                Tension -= (0.55f - pull * 0.25f) * dt;
                Progress -= pull * 0.03f * dt;
            }
            Tension = Math.Max(0, Tension);
            bool slip = Drag && Tension > 0.9f;
            if (slip) { Tension = Math.Max(0.86f, Tension - 1.4f * dt); Progress -= 0.05f * dt; if (!Slipping) OnSlip?.Invoke(); }
            Slipping = slip;
            Slack = Tension < 0.1f ? Slack + dt : 0;
            if (Tension >= 1) return FightResult.Snapped;
            if (Slack > 2f) return FightResult.Slack;
            if (Progress <= 0) return FightResult.Escaped;
            if (Progress >= 1) return FightResult.Caught;
            return FightResult.Ongoing;
        }
    }

    // ตีเบ็ดปลาหายาก: กดตอนเข็มอยู่ในโซนทอง (★3 = 2 ครั้ง, ★4 = 3 ครั้ง) พลาดได้ 1 ครั้ง
    public class StrikeModel
    {
        public float Pos = 0.05f, Center, Width, Speed, Time;
        public int Hits, Need, Misses;
        int dir = 1;
        readonly Random rng;

        public StrikeModel(int stars, Random rng, float widthMul = 1f)
        {
            this.rng = rng;
            Speed = stars >= 4 ? 1.35f : 0.95f;
            Width = (stars >= 4 ? 0.15f : 0.22f) * widthMul;
            Need = stars >= 4 ? 3 : 2;
            Center = 0.3f + (float)rng.NextDouble() * 0.4f;
        }

        public bool Done => Hits >= Need;
        public bool Failed => Misses >= 2 || Time > 8f;

        public void Step(float dt)
        {
            Time += dt;
            Pos += dir * Speed * dt;
            if (Pos > 1) { Pos = 1; dir = -1; } else if (Pos < 0) { Pos = 0; dir = 1; }
        }

        // คืนค่า true ถ้าตีโดน
        public bool Press()
        {
            if (Math.Abs(Pos - Center) <= Width / 2 + 0.01f)
            {
                Hits++;
                Center = 0.15f + (float)rng.NextDouble() * 0.7f; Speed *= 1.2f; Width *= 0.85f;
                return true;
            }
            Misses++;
            return false;
        }
    }
}
