using UnityEngine;

namespace CozyFishing
{
    public enum FishState { Free, Charging, Casting, Landed, Waiting, Bite, Strike, Fight, Caught, Retrieve, Snapped }

    // วงจรตกปลาครบ: กดค้างเล็ง → ปล่อยเหวี่ยง → ทุ่นลอยรอ (มีตอดเบา ๆ) → ทุ่นจม → (ปลาหายาก: ตีเบ็ด) → มินิเกมดึงปลา → ได้ปลา
    // ย้ายมาจาก game object ใน main.js ของเวอร์ชันเว็บ
    // ตัวนี้คุมสถานะและส่งต่อให้ชิ้นย่อยใน Gameplay/Fishing/: เหวี่ยง (FishingCast) · ปลากินเหยื่อ (FishingBite) · ไดหมึก (SquidJig)
    // · เหยื่อปลอม (LureRetrieve) · บอส (BossFight) · ได้ปลา/การ์ด (CatchFlow) · ทุ่นและสาย (FishingLook) · ท่าทาง (FisherPose) · HUD (FishingHudView) · บอททดสอบ (FishingBot)
    public class FishingController : MonoBehaviour
    {
        public PlayerController player;
        public Hud hud;
        public FishPreview preview;
        public StoryDirector director;
        public GameObject fishTemplate;      // โมเดลปลาจาก Blender (ซ่อนไว้ในฉาก)
        public GameObject bobber, aim;       // สร้างไว้ในฉากโดย SceneBuilder (WebGL ตัดโมดูล Physics/shader ที่ไม่ได้อ้างถึงออก)
        public LineRenderer line;
        public float armSign = 1f;           // กลับทิศหมุนแขนถ้าโมเดลนำเข้ามากลับด้าน

        public FishState State { get; private set; }
        public Vector3 BobberPos => bob;
        public bool LineOut => State >= FishState.Casting && State <= FishState.Fight;
        public bool Busy => State == FishState.Strike || State == FishState.Fight || State == FishState.Caught;
        /// วินาทีที่เหลือก่อนปลากินเหยื่อ (รอทุ่นอยู่) · -1 = ไม่ได้รอ — PondLife ใช้พาปลาว่ายมาหาเหยื่อ
        public float TimeToBite => State == FishState.Waiting ? bite.At - t : -1;
        /// ชนิดปลาที่สุ่มไว้ว่าจะติดเบ็ดรอบนี้ (ขยะ = null)
        public FishSpecies HookedSpecies => fish.IsJunk ? null : fish.species;
        /// มุมบุคคลที่หนึ่งถือปลาโชว์เอง (ไม่ให้ปลาลอยหมุนเหนือหัวแบบเดิม)
        public bool ExternalShowcase;
        public GameObject CaughtGo => caughtGo;
        public Vector3 CastTarget => cast.Target;
        public float CaughtCm => fish.cm;
        public RetrieveModel LureRun => lure.Run;
        public string CaughtName => fish.species?.name ?? fish.junk;
        public bool CaughtNew => catchFlow.IsNew;
        /// ฉากหลังกดเก็บ/ปล่อย (มุมบุคคลที่หนึ่งรับปลาไปจัดการต่อ) · คืน true = รับตัวปลาไปแล้ว ไม่ต้องทำลาย
        public static System.Func<GameObject, bool, bool> CatchOutro;
        public Vector3 AimPoint => aim != null && aim.activeSelf ? aim.transform.position : player.Pos + player.transform.forward * 8;
        bool paused;
        /// อยู่บ้าน/ระหว่างเดินทาง: หยุดระบบตกปลา ซ่อนทุ่นและสาย
        public bool Paused
        {
            get => paused;
            set { paused = value; bobber.SetActive(!value); line.enabled = !value; aim.SetActive(false); if (value) { player.CanWalk = false; player.CanTurn = false; player.Pose = 0; } }
        }

        // สถานะที่ชิ้นย่อยใช้ร่วมกัน
        internal IWorld world;
        internal readonly System.Random rng = GameUrl.Rng();
        internal float t, flight;            // t = วินาทีในสถานะนี้ · flight = 0..1 ทุ่นลอยออก/ลอยกลับ
        internal Vector3 from, bob;
        internal Catch fish;
        internal FightModel fight;
        internal StrikeModel strike;
        internal GameObject caughtGo;
        bool prevHeld;

        internal FishingCast cast;
        internal FishingBite bite;
        internal SquidJig jig;
        internal BossFight boss;
        internal LureRetrieve lure;
        CatchFlow catchFlow;
        FishingLook look;
        FisherPose pose;
        FishingHudView hudView;
        FishingBot bot;

        public void Init(IWorld w)
        {
            world = w;
            Bot = GameUrl.Value.Contains("bot") || System.Environment.CommandLine.Contains("-bot");
            look = new FishingLook(this);
            cast = new FishingCast(this);
            bite = new FishingBite(this);
            jig = new SquidJig(this);
            boss = new BossFight(this);
            lure = new LureRetrieve(this);
            catchFlow = new CatchFlow(this);
            pose = new FisherPose(this);
            hudView = new FishingHudView(this);
            bot = new FishingBot(this);
            aim.SetActive(false);
            if (Bot)   // ทดสอบ: ?lure=frog ใส่เหยื่อปลอมให้บอทลาก
            {
                var u = GameUrl.Value; int li = u.IndexOf("lure=");
                if (li >= 0) { var id = "lure_" + u.Substring(li + 5).Split('&')[0]; if (Shop.Get(id) != null) { Tackle.Give(id); Tackle.Equip(id); } }
            }
            look.ApplyTackle(); Tackle.Changed += look.ApplyTackle;
            Set(FishState.Free);
        }
        void OnDestroy() { if (look != null) Tackle.Changed -= look.ApplyTackle; }

        internal void Set(FishState s) { State = s; t = 0; }

        public static bool Bot;   // โหมดทดสอบอัตโนมัติ (FishingBot) · ตั้งใน Init (อ่าน URL ระหว่างโหลดฉากไม่ได้)
        // กลางคืน (GhostSystem ตั้ง): ผีปรากฏ = ปลาอาถรรพ์กินเบ็ด · ผีพรายอวนดึงสาย · สติต่ำ เกจแรงดึงแกว่ง
        public static float Haunt;
        public float GhostPull, GaugeWobble;
        /// ผีทำสายขาด (พรายหินจ้องตา)
        public void GhostSnap()
        {
            if (!(State == FishState.Waiting || State == FishState.Bite || State == FishState.Strike || State == FishState.Fight)) return;
            Roll(); Set(FishState.Snapped); from = bob; flight = 0;
        }

        public const float JigPeriod = SquidJig.Period, JigPeak = SquidJig.Peak;
        public bool Jigging => world != null && world.StageId == "squid";
        /// 0..1 ความสว่างไฟล่อหมึกตามจังหวะ (ยอด = จังหวะที่ควรแตะ) · SquidSea ใช้กะพริบไฟในฉาก
        public static float JigPulse => SquidJig.Pulse;

        /// เก็บสาย/ล้างปลาที่ค้างอยู่ทันที (ก่อนกลับบ้าน หรือเริ่มวันใหม่)
        public void ResetLine()
        {
            if (hud.CatchCardOpen) hud.CloseCard(true);
            catchFlow.Clear();
            if (preview != null) preview.StopLive();
            if (caughtGo) Destroy(caughtGo);
            caughtGo = null;
            prevHeld = false;
            Set(FishState.Free);
            if (player.rodTip) bob = player.rodTip.position + new Vector3(0, -0.55f, 0);
        }

        void Update()
        {
            if (paused) { hud.Power(-1); hud.Tension(false, 0, 0, 0, 0, false); hud.Strike(false, 0, 0, 0, 0, 0, 0); hud.Alert(false, default, false); return; }
            float dt = Time.deltaTime;
            bool held = GameInput.ActionHeld || (Bot && bot.Held());
            if (held && !prevHeld) OnDown();
            if (!held && prevHeld) OnUp();
            prevHeld = held;
            t += dt;
            var tip = player.rodTip.position;
            float now = WaterFx.Time;

            switch (State)
            {
                case FishState.Free:
                    bob = tip + new Vector3(Mathf.Sin(now * 2) * 0.05f, -0.55f, 0);
                    break;
                case FishState.Charging:
                    cast.ChargeStep(dt, tip, now);
                    break;
                case FishState.Casting:
                    if (cast.FlightStep(dt, tip)) Land();
                    break;
                case FishState.Landed:
                    if (t > 0.7f) StartRetrieve();
                    break;
                case FishState.Waiting:
                    if (Jigging && jig.HoldToReel(held, dt)) { StartRetrieve(); hud.Toast("เก็บสาย"); break; }
                    if (lure.Run != null) { lure.Step(dt, held); break; }
                    bite.WaitStep(now);
                    break;
                case FishState.Bite:
                    bob.y = world.WaterY - 0.2f + Mathf.Sin(t * 40) * 0.03f;
                    if (t > bite.End) { hud.Toast("ช้าไปนิด ปลากินเหยื่อหนีไป"); DuelLose(); Roll(); if (Lures.Active && !Jigging) { StartRetrieve(); break; } bite.Plan(); Set(FishState.Waiting); }   // เหยื่อปลอม: เก็บสายแล้วเหวี่ยงใหม่
                    break;
                case FishState.Strike:
                    strike.Step(dt);
                    bob.y = world.WaterY - 0.22f + Mathf.Sin(t * 35) * 0.04f;
                    if (strike.Time > 8f) { hud.Toast("ปลาเบื่อรอ ว่ายหนีไปแล้ว…"); DuelLose(); Roll(); StartRetrieve(); }
                    break;
                case FishState.Fight:
                    FightStep(dt, held);
                    break;
                case FishState.Retrieve:
                case FishState.Snapped:
                    if (State == FishState.Snapped && t < 1f) break;
                    flight += dt / 0.45f;
                    float kr = Mathf.Min(1, flight);
                    var end = tip + new Vector3(0, -0.55f, 0);
                    bob = Vector3.Lerp(from, end, kr) + Vector3.up * Mathf.Sin(kr * Mathf.PI) * 0.8f;
                    if (kr >= 1) Set(FishState.Free);
                    break;
                case FishState.Caught:
                    float kc = Mathf.Min(1, t / 0.7f);
                    var top = player.Pos + Vector3.up * 2.4f;
                    if (caughtGo && !ExternalShowcase)
                    {
                        caughtGo.transform.position = Vector3.Lerp(from, top, kc) + Vector3.up * Mathf.Sin(kc * Mathf.PI) * 1.2f;
                        caughtGo.transform.rotation = Quaternion.Euler(Mathf.Sin(t * 9) * 17, t * 140, Mathf.Sin(t * 12) * 14);
                    }
                    bob = caughtGo ? (ExternalShowcase ? caughtGo.transform.TransformPoint(0.48f, 0, 0) : caughtGo.transform.position) : top;   // ถือโชว์: สายผูกที่ปากปลา
                    if (t > (ExternalShowcase ? 2.2f : 0.9f) && !hud.CardOpen && caughtGo && !catchFlow.CardShown) { catchFlow.CardShown = true; catchFlow.ShowCard(); }   // มุมบุคคลที่หนึ่ง: ให้ชื่นชมปลาในมือก่อนการ์ดขึ้น
                    break;
            }
            if (State != FishState.Charging) aim.SetActive(false);
            look.SkillBubbles();
            look.Update(tip);
            pose.Update();
            hudView.Update();
        }

        // ---------- การกด ----------
        void OnDown()
        {
            switch (State)
            {
                case FishState.Free: cast.Charge = 0; Set(FishState.Charging); break;
                case FishState.Waiting: if (lure.Run != null) { lure.Tap(); break; } if (Jigging) { jig.Tap(); break; } StartRetrieve(); hud.Toast("เก็บสาย"); break;
                case FishState.Bite:
                    if (!fish.IsJunk && fish.species.stars >= 3 && !Bot) Cloud.SubmitQuick(Mathf.RoundToInt(t * 1000));   // กระดานมือไว (มิลลิวินาที น้อย = ไว)
                    if (!fish.IsJunk && fish.species.stars >= 3) { strike = new StrikeModel(fish.species.stars, rng, Tackle.StrikeWidthMul); Set(FishState.Strike); hud.Toast(fish.species.stars >= 4 ? "ตัวนี้ไม่ธรรมดา! ตีเบ็ดให้ตรงจังหวะ 3 ครั้ง" : "ปลาใหญ่! ตีเบ็ดให้ตรงจังหวะ 2 ครั้ง"); }
                    else StartFight();
                    break;
                case FishState.Strike:
                    if (strike.Press()) { WaterFx.AddRipple(bob.x, bob.z, 1.2f); if (strike.Done) { hud.Toast("ติดเบ็ดแล้ว! ระวังโซนเขียวเลื่อนไปมา"); StartFight(); } }
                    else if (strike.Failed) { hud.Toast("ปลาสะบัดหลุด! ต้องตีให้ตรงจังหวะ"); DuelLose(); Roll(); StartRetrieve(); }
                    else hud.Toast("พลาด! อีกครั้งเดียวนะ");
                    break;
            }
        }

        void OnUp() { if (State == FishState.Charging) cast.Release(); }

        public static float MaxCast => FishingCast.MaxCast;

        internal bool NearSpecial => new Vector2(bob.x - world.Special.x, bob.z - world.Special.z).magnitude < world.SpecialRadius;
        internal bool NearDock => world.OnSolid(player.Pos.x, player.Pos.z);   // ยืนบนท่าน้ำ
        public bool BossHooked => !fish.IsJunk && fish.species != null && fish.species.id == Boss.Id;

        void Roll() => bite.Roll();

        void Land()
        {
            bob = cast.Target;
            if (!cast.OnWater) { hud.Toast("เหวี่ยงไม่ถึงน้ำ ลองใหม่นะ"); Set(FishState.Landed); return; }
            Sfx.Plop();
            WaterFx.AddRipple(bob.x, bob.z, 1.2f);
            Roll(); bite.Plan();
            lure.Begin();
            Set(FishState.Waiting);
            if (NearSpecial) hud.Toast("จุดนี้ดูน่าสนใจ…");
        }

        internal void StartRetrieve() { lure.Clear(); flight = 0; from = bob; Set(FishState.Retrieve); }
        bool slipTold;

        void StartFight()
        {
            if (Bot && !fish.IsJunk && GameUrl.Value.Contains("fastcatch")) { catchFlow.OnCaught(); return; }   // ทดสอบ: ข้ามมินิเกมดึงปลา ดูฉากยกปลาโชว์เร็ว ๆ
            if (fish.IsJunk)
            {
                var bottle = fish.junk == Mystery.BottleJunk ? Mystery.OnBottle() : null;
                if (bottle != null) hud.ShowClue(bottle);
                else if (Story.S.time >= GhostSystem.NightFrom && rng.NextDouble() < 0.3)
                {
                    Story.S.amulets++; Story.Save();
                    hud.Toast(rng.NextDouble() < 0.5 ? $"ได้สายสิญจน์เก่าพันกิ่งไม้! ช่วยกันผีได้ (มี {Story.S.amulets} เส้น)" : $"ได้เหรียญขวัญถุงโบราณ! ช่วยกันผีได้ (มี {Story.S.amulets} ชิ้น)");
                }
                else hud.Toast($"ได้{fish.junk} …เก็บไปทิ้งให้น้ำสะอาด");
                Roll(); StartRetrieve(); return;
            }
            fight = new FightModel(fish.species, false, rng, Shop.RodTension * Tackle.LineTensionMul, Shop.ReelSpeed) { Drag = Tackle.HasDrag, ReleaseMul = Skills.ReleaseMul, SkillProgMul = Skills.ReelMul };
            dashWarnAt = 0;
            fight.OnDash += () => WaterFx.AddRipple(bob.x, bob.z, 1.2f);
            fight.OnSlip += () => { Sfx.Nibble(); if (!slipTold) { slipTold = true; hud.Toast("เบรกรอกคลายสายให้ สายไม่ขาด! ผ่อนนิ้วหน่อย"); } };
            from = bob;
            Set(FishState.Fight); Sfx.Strike();
        }

        float nextSplash, dashWarnAt;
        void FightStep(float dt, bool held)
        {
            if (Skills.DashWarning && fight.DashSoon && Time.time > dashWarnAt) { dashWarnAt = Time.time + 2.5f; hud.Toast("ระวัง! ปลากำลังจะพุ่ง ผ่อนนิ้วก่อน"); }   // ทักษะอ่านทางปลา
            if (BossHooked) boss.Step(dt);
            var r = fight.Step(dt, held);
            if (GhostPull > 0) fight.Tension = Mathf.Clamp01(fight.Tension + GhostPull * dt * (0.6f + Mathf.PerlinNoise(Time.time * 3, 0.5f)));   // มีอะไรดึงสายอยู่ใต้น้ำ
            float pr = Mathf.Clamp01(fight.Progress);
            var goal = player.Pos + player.Forward * 1.2f;
            float now = WaterFx.Time;
            float nx = Mathf.Lerp(from.x, goal.x, pr * 0.85f) + Mathf.Sin(now * 1.7f) * 0.5f * (1 - pr);
            float nz = Mathf.Lerp(from.z, goal.z, pr * 0.85f) + Mathf.Cos(now * 1.3f) * 0.4f * (1 - pr);
            if (world.CanCast(nx, nz)) { bob.x += (nx - bob.x) * Mathf.Min(1, dt * 3); bob.z += (nz - bob.z) * Mathf.Min(1, dt * 3); }
            bob.y = world.WaterY - 0.06f + Mathf.Sin(now * 14) * 0.03f;
            if (now > nextSplash) { WaterFx.AddRipple(bob.x, bob.z, 0.6f); nextSplash = now + 0.35f; }
            switch (r)
            {
                case FightResult.Caught: catchFlow.OnCaught(); break;
                case FightResult.Snapped: hud.Toast("สายขาด! ปลาหนีไปแล้ว "); Sfx.Snap(); DuelLose(); Roll(); Set(FishState.Snapped); from = bob; flight = 0; break;
                case FightResult.Slack: hud.Toast("สายหย่อนเกินไป ปลาหลุด…"); DuelLose(); Roll(); StartRetrieve(); break;
                case FightResult.Escaped: hud.Toast("ปลาว่ายหนีไปได้…"); DuelLose(); Roll(); StartRetrieve(); break;
            }
        }

        // ดวล: ตัวที่หลุด/ขาดนับเป็นหนึ่งตัว (น้ำหนัก 0) · ครบห้าตัวแล้วแสดงผล
        void DuelLose()
        {
            if (!Duel.Active || Duel.Done || fish.IsJunk) return;
            var m = Duel.Resolve(false, 0, fish.species.name);
            if (m.Length > 0) hud.Toast(m); else hud.ShowDuelResult();
        }

        // ปลาที่ตกได้/ปลาในบ่อ/ปลาโชว์ (FishFactory)
        public GameObject MakeFish(FishSpecies sp, float size) => FishFactory.Make(fishTemplate, sp, size);
    }
}
