using System.Collections.Generic;
using UnityEngine;

namespace CozyFishing
{
    public enum FishState { Free, Charging, Casting, Landed, Waiting, Bite, Strike, Fight, Caught, Retrieve, Snapped }

    // วงจรตกปลาครบ: กดค้างเล็ง → ปล่อยเหวี่ยง → ทุ่นลอยรอ (มีตอดเบา ๆ) → ทุ่นจม → (ปลาหายาก: ตีเบ็ด) → มินิเกมดึงปลา → ได้ปลา
    // ย้ายมาจาก game object ใน main.js ของเวอร์ชันเว็บ
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
        public float TimeToBite => State == FishState.Waiting ? biteAt - t : -1;
        /// ชนิดปลาที่สุ่มไว้ว่าจะติดเบ็ดรอบนี้ (ขยะ = null)
        public FishSpecies HookedSpecies => fish.IsJunk ? null : fish.species;
        /// มุมบุคคลที่หนึ่งถือปลาโชว์เอง (ไม่ให้ปลาลอยหมุนเหนือหัวแบบเดิม)
        public bool ExternalShowcase;
        public GameObject CaughtGo => caughtGo;
        public Vector3 CastTarget => target;
        public float CaughtCm => fish.cm;
        public string CaughtName => fish.species?.name ?? fish.junk;
        public bool CaughtNew => caughtIsNew;
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

        IWorld world;
        readonly System.Random rng = GameUrl.Rng();
        float t, charge, flight, flightDur, biteAt, biteEnd;
        Vector3 target, from, bob;
        bool onWater, prevHeld, nibbling;
        readonly List<float> nibbles = new List<float>();
        Catch fish;
        FightModel fight;
        StrikeModel strike;
        GameObject caughtGo;
        Renderer aimRenderer;
        MaterialPropertyBlock aimMpb;
        readonly Vector3[] linePts = new Vector3[24];

        public void Init(IWorld w)
        {
            world = w;
            Bot = GameUrl.Value.Contains("bot") || System.Environment.CommandLine.Contains("-bot");
            line.positionCount = linePts.Length; lineW0 = line.widthMultiplier;
            aimRenderer = aim.GetComponentInChildren<Renderer>();
            aimMpb = new MaterialPropertyBlock();
            aim.SetActive(false);
            Set(FishState.Free);
        }

        const float ThrowDelay = 0.16f;   // จากปล่อยปุ่มถึงทุ่นพุ่งออก (ตรงกับเฟรมแขนสะบัดของท่า Mixamo)
        float reelT;
        bool caughtIsNew, cardShown;

        void Set(FishState s) { State = s; t = 0; }

        // ---------- โหมดทดสอบอัตโนมัติ (?bot ใน URL หรือ -bot): เล่นเองครบวง เหวี่ยง → รอ → ดึง → ตีเบ็ด → สู้ปลา → ปิดการ์ด ----------
        public static bool Bot;   // ตั้งใน Init (อ่าน URL ระหว่างโหลดฉากไม่ได้)
        // กลางคืน (GhostSystem ตั้ง): ผีปรากฏ = ปลาอาถรรพ์กินเบ็ด · ผีพรายอวนดึงสาย · สติต่ำ เกจแรงดึงแกว่ง
        public static float Haunt;
        public float GhostPull, GaugeWobble;
        /// ผีทำสายขาด (พรายหินจ้องตา)
        public void GhostSnap()
        {
            if (!(State == FishState.Waiting || State == FishState.Bite || State == FishState.Strike || State == FishState.Fight)) return;
            Roll(); Set(FishState.Snapped); from = bob; flight = 0;
        }
        float botNext;

        // ---------- ด่านเรือไดหมึก: หมึกไม่กินเบ็ดเอง ต้อง "ไดหมึก" แตะตามจังหวะไฟเขียวกะพริบ ให้หมึกเข้ามาในวงแสง · กดค้าง = เก็บสาย ----------
        public const float JigPeriod = 1.6f, JigPeak = 0.2f;
        public bool Jigging => world != null && world.StageId == "squid";
        /// 0..1 ความสว่างไฟล่อหมึกตามจังหวะ (ยอด = จังหวะที่ควรแตะ) · SquidSea ใช้กะพริบไฟในฉาก
        public static float JigPulse => 1 - Mathf.Clamp01(Mathf.Abs(Mathf.Repeat(Time.time, JigPeriod) - JigPeak) / 0.3f);
        int jigStreak; float jigHeld;
        void Jig()
        {
            bool good = Mathf.Abs(Mathf.Repeat(Time.time, JigPeriod) - JigPeak) < 0.24f;
            WaterFx.AddRipple(bob.x, bob.z, good ? 0.9f : 0.5f); bob.y -= 0.1f;
            if (good)
            {
                jigStreak++;
                biteAt = Mathf.Max(t + 0.6f, biteAt - (3.2f + jigStreak * 0.9f));
                hud.Toast(jigStreak >= 3 ? "หมึกรุมเข้ามาในแสงไฟแล้ว!" : jigStreak == 2 ? "ดีมาก! เห็นเงาหมึกวนใต้เรือ" : "จังหวะดี! หมึกเริ่มเข้ามาใกล้");
                if (jigStreak >= 2) Sfx.Nibble();
            }
            else { jigStreak = 0; biteAt = Mathf.Max(t + 0.6f, biteAt - 0.4f); hud.Toast("ยังไม่ตรงจังหวะไฟ ลองแตะตอนไฟเขียวสว่างสุด"); }
        }

        bool BotHeld()
        {
            switch (State)
            {
                case FishState.Waiting:   // ทดสอบ: บอทไดหมึกตามจังหวะไฟ
                    if (Jigging && JigPulse > 0.85f && Time.time > botNext) { botNext = Time.time + 0.8f; return true; }
                    return false;
                case FishState.Free: return t > 1.2f && t < 1.2f + 0.9f;            // ง้าง ~0.9 วิ แล้วปล่อย
                case FishState.Charging: return charge < 0.45f;   // บอททดสอบเหวี่ยงราว 9 ม. เท่าเดิม
                case FishState.Bite: return t > 0.25f;                              // รอเห็นทุ่นจมแป๊บหนึ่งค่อยกด
                case FishState.Strike:
                    bool inZone = Mathf.Abs(strike.Pos - strike.Center) < strike.Width * 0.35f;
                    if (inZone && Time.time > botNext) { botNext = Time.time + 0.35f; return true; }
                    return false;
                case FishState.Fight: if (BossHooked && bossPhase == 3 && fight.Time < bossRunEnd) return false;   // บอทผ่อนสายตอนบอสวิ่งเข้าแก่ง
                    return fight.Tension < Mathf.Min(0.8f, fight.ZoneCenter + fight.ZoneWidth * 0.3f);
                case FishState.Caught: if (hud.CatchCardOpen && t > 6f) hud.CloseCard(false); return false;
                default: return false;
            }
        }

        /// เก็บสาย/ล้างปลาที่ค้างอยู่ทันที (ก่อนกลับบ้าน หรือเริ่มวันใหม่)
        public void ResetLine()
        {
            if (hud.CatchCardOpen) hud.CloseCard(true);
            pending = null; cardShown = false;
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
            bool held = GameInput.ActionHeld || (Bot && BotHeld());
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
                    charge = Mathf.Min(1, charge + dt / 1.6f);   // ระยะไกลขึ้น → ง้างนานขึ้นนิด กะระยะใกล้ได้แม่นเหมือนเดิม
                    bob = tip + new Vector3(0, -0.55f, 0);
                    var p = CastPoint(charge);
                    bool w = world.CanCast(p.x, p.z);
                    aim.SetActive(true);
                    aim.transform.position = new Vector3(p.x, (w ? world.WaterY : world.GroundH(p.x, p.z)) + 0.04f, p.z);
                    aim.transform.localScale = new Vector3(0.8f, 0.005f, 0.8f) * (1 + Mathf.Sin(now * 8) * 0.08f);   // วงแบน ๆ บนผิวน้ำ (y คงบางไว้)
                    aimMpb.SetColor("_BaseColor", w ? Color.white : new Color(1f, 0.54f, 0.44f));
                    aimRenderer.SetPropertyBlock(aimMpb);
                    break;
                case FishState.Casting:
                    // ช่วงแรกแขนสะบัดไปข้างหน้า ทุ่นยังติดปลายคัน แล้วค่อยพุ่งออกพร้อมจังหวะปล่อยสาย
                    if (t < ThrowDelay) { bob = tip + new Vector3(0, -0.3f, 0); from = tip; break; }
                    flight += dt / flightDur;
                    float k = Mathf.Min(1, flight);
                    bob = Vector3.Lerp(from, target, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * (1.5f + Vector3.Distance(from, target) * 0.22f);
                    if (k >= 1) Land();
                    break;
                case FishState.Landed:
                    if (t > 0.7f) StartRetrieve();
                    break;
                case FishState.Waiting:
                    if (Jigging) { jigHeld = held ? jigHeld + dt : 0; if (jigHeld > 0.8f) { jigHeld = 0; StartRetrieve(); hud.Toast("เก็บสาย"); break; } }
                    float y = world.WaterY + 0.02f + Mathf.Sin(now * 2.2f) * 0.015f;
                    if (nibbles.Count > 0 && t > nibbles[0])
                    {
                        float kk = (t - nibbles[0]) / 0.25f;
                        if (kk < 1) { y -= Mathf.Sin(kk * Mathf.PI) * 0.07f; if (!nibbling) { nibbling = true; WaterFx.AddRipple(bob.x, bob.z, 0.5f); } }
                        else { nibbles.RemoveAt(0); nibbling = false; }
                    }
                    bob.y = y;
                    if (t > biteAt)
                    {
                        Set(FishState.Bite); Sfx.Bite();
                        biteEnd = fish.IsJunk ? 1.2f : fish.species.win;
                        WaterFx.AddRipple(bob.x, bob.z, 1.5f);
                    }
                    break;
                case FishState.Bite:
                    bob.y = world.WaterY - 0.2f + Mathf.Sin(t * 40) * 0.03f;
                    if (t > biteEnd) { hud.Toast("ช้าไปนิด ปลากินเหยื่อหนีไป"); DuelLose(); Roll(); NewBite(); Set(FishState.Waiting); }
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
                    if (t > (ExternalShowcase ? 2.2f : 0.9f) && !hud.CardOpen && caughtGo && !cardShown) { cardShown = true; ShowCard(); }   // มุมบุคคลที่หนึ่ง: ให้ชื่นชมปลาในมือก่อนการ์ดขึ้น
                    break;
            }
            if (State != FishState.Charging) aim.SetActive(false);
            UpdateVisuals(tip);
            UpdatePose();
            UpdateHud();
        }

        // ---------- การกด ----------
        void OnDown()
        {
            switch (State)
            {
                case FishState.Free: charge = 0; Set(FishState.Charging); break;
                case FishState.Waiting: if (Jigging) { Jig(); break; } StartRetrieve(); hud.Toast("เก็บสาย"); break;
                case FishState.Bite:
                    if (!fish.IsJunk && fish.species.stars >= 3 && !Bot) Cloud.SubmitQuick(Mathf.RoundToInt(t * 1000));   // กระดานมือไว (มิลลิวินาที น้อย = ไว)
                    if (!fish.IsJunk && fish.species.stars >= 3) { strike = new StrikeModel(fish.species.stars, rng); Set(FishState.Strike); hud.Toast(fish.species.stars >= 4 ? "ตัวนี้ไม่ธรรมดา! ตีเบ็ดให้ตรงจังหวะ 3 ครั้ง" : "ปลาใหญ่! ตีเบ็ดให้ตรงจังหวะ 2 ครั้ง"); }
                    else StartFight();
                    break;
                case FishState.Strike:
                    if (strike.Press()) { WaterFx.AddRipple(bob.x, bob.z, 1.2f); if (strike.Done) { hud.Toast("ติดเบ็ดแล้ว! ระวังโซนเขียวเลื่อนไปมา"); StartFight(); } }
                    else if (strike.Failed) { hud.Toast("ปลาสะบัดหลุด! ต้องตีให้ตรงจังหวะ"); DuelLose(); Roll(); StartRetrieve(); }
                    else hud.Toast("พลาด! อีกครั้งเดียวนะ");
                    break;
            }
        }

        void OnUp() { if (State == FishState.Charging) Cast(); }

        public static float MaxCast => Shop.CastDist;   // ง้างเต็มแรง: คันไม้ไผ่ 18 ม. · คันจากร้านไกลขึ้นถึง 30 ม.
        Vector3 CastPoint(float c) { var f = player.Forward; float d = 1.3f + c * (MaxCast - 1.3f); return new Vector3(player.Pos.x + f.x * d, 0, player.Pos.z + f.z * d); }

        void Cast()
        {
            target = CastPoint(charge);
            onWater = world.CanCast(target.x, target.z);
            target.y = onWater ? world.WaterY + 0.03f : world.OnSolid(target.x, target.z) ? world.StandY(target.x, target.z) + 0.05f : world.GroundH(target.x, target.z) + 0.08f;
            from = player.rodTip.position;
            flightDur = 0.5f + Vector3.Distance(from, target) * 0.045f;
            flight = 0;
            Set(FishState.Casting);
        }

        bool NearSpecial => new Vector2(bob.x - world.Special.x, bob.z - world.Special.z).magnitude < world.SpecialRadius;
        // ดวงประจำวัน: ฟ้าครึ้ม/ฝน = ขยะบ่อยขึ้น · วันฝนหนักบางวันปลาแทบไม่กินเหยื่อ
        bool NearDock => world.OnSolid(player.Pos.x, player.Pos.z);   // ยืนบนท่าน้ำ
        public bool BossHooked => !fish.IsJunk && fish.species != null && fish.species.id == Boss.Id;
        int bossPhase; float bossRunAt, bossRunEnd, bossSpikeAt;
        // แม่น้ำโขง: บอสไม่ได้มาจากการสุ่มปกติ (chance 0) · มีโอกาสโผล่เมื่อพร้อม (สัปดาห์นี้ยังไม่มีใครในเครื่องนี้จับได้ + อุปกรณ์ถึง)
        void BossRoll()
        {
            bool force = Bot && GameUrl.Value.Contains("boss");   // ทดสอบ: บอสโผล่ทุกครั้ง ไม่ต้องมีอุปกรณ์
            if (world.StageId != Boss.Stage || fish.IsJunk || (!Boss.Eligible && !force)) return;
            if (!force && rng.NextDouble() > (Boss.Prayed ? 0.13 : 0.08) * (NearSpecial ? 2 : 1)) return;
            var sp = FishDatabase.Get(Boss.Id); if (sp == null) return;
            float k = (float)rng.NextDouble();
            fish = new Catch { species = sp, kg = (float)System.Math.Round(sp.kgMin + (sp.kgMax - sp.kgMin) * k, 1), cm = (int)(sp.cmMin + (sp.cmMax - sp.cmMin) * k) };
        }
        void Roll()
        {
            // สืบคดี: ระหว่างตามหาเบาะแส ปลาที่พาเบาะแส/ขวดจดหมาย มีโอกาสโผล่มากขึ้นเมื่ออยู่ในเงื่อนไข
            if (Duel.Active && !Duel.Done) { fish = Duel.Next; return; }   // ดวล: ปลาชุดเดียวกับเพื่อน
            var o = Mystery.Override(world.StageId, Story.S.time, NearSpecial, NearDock, rng);
            if (o.HasValue) { fish = o.Value; return; }
            RollNormal();
            BossRoll();
        }
        void RollNormal() => fish = Bot && GameUrl.Value.Contains("rarefish") ? FishRoller.Roll(world.StageId, true, 3f, 0f, Story.S.time, rng, 1f)   // ทดสอบ/ถ่ายคลิป: ปลาหายากขึ้นบ่อย
            : Story.S.caught == 0 ? FishRoller.Roll(world.StageId, false, 1f, 0f, Story.S.time, rng)   // ตัวแรก: ปลาจริงเสมอ
            : Story.S.dead && rng.NextDouble() < 0.85
            ? FishRoller.Roll(world.StageId, false, 0, 1f, Story.S.time, rng)
            : FishRoller.Roll(world.StageId, NearSpecial, world.DepthAt(bob.x, bob.z), Story.JunkChance, Story.S.time, rng, Haunt);

        void Land()
        {
            bob = target;
            if (!onWater) { hud.Toast("เหวี่ยงไม่ถึงน้ำ ลองใหม่นะ"); Set(FishState.Landed); return; }
            Sfx.Plop();
            WaterFx.AddRipple(bob.x, bob.z, 1.2f);
            Roll(); NewBite();
            Set(FishState.Waiting);
            if (NearSpecial) hud.Toast("จุดนี้ดูน่าสนใจ…");
        }

        void NewBite()
        {
            biteAt = t + (Bot && GameUrl.Value.Contains("fastcatch") ? 1.5f : (Story.S.caught == 0 ? 1.2f + (float)rng.NextDouble() * 1.3f : (2.2f + (float)rng.NextDouble() * 5.5f) * Story.BiteMul * Shop.BiteMul));   // ตัวแรกของเกม: กินเร็ว ติดใจตั้งแต่เหวี่ยงแรก
            if (Jigging && !(Bot && GameUrl.Value.Contains("fastcatch"))) { biteAt = t + 18f + (float)rng.NextDouble() * 10f; jigStreak = 0; }   // ไม่ไดหมึก = แทบไม่มีหมึกเข้ามา
            nibbles.Clear();
            int n = rng.Next(3);
            for (int i = 0; i < n; i++) nibbles.Add(biteAt - 0.6f - (float)rng.NextDouble() * 1.8f);
            nibbles.Sort();
        }

        void StartRetrieve() { flight = 0; from = bob; Set(FishState.Retrieve); }

        void StartFight()
        {
            if (Bot && !fish.IsJunk && GameUrl.Value.Contains("fastcatch")) { OnCaught(); return; }   // ทดสอบ: ข้ามมินิเกมดึงปลา ดูฉากยกปลาโชว์เร็ว ๆ
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
            fight = new FightModel(fish.species, false, rng, Shop.RodTension, Shop.ReelSpeed);
            fight.OnDash += () => WaterFx.AddRipple(bob.x, bob.z, 1.2f);
            from = bob;
            Set(FishState.Fight); Sfx.Strike();
        }

        float nextSplash;
        // บอสสามช่วงตามความคืบหน้า: ดิ่งลึก (ดึงหนัก) → พุ่งขึ้นผิวน้ำ (กระชากเป็นจังหวะ) → ว่ายเข้าแก่ง (ช่วงวิ่งต้องผ่อนสาย ไม่งั้นสายตึงพรวด)
        void BossFight(float dt, bool held)
        {
            float p = fight.Progress;
            int ph = Mathf.Max(bossPhase, p < 0.4f ? 1 : p < 0.7f ? 2 : 3);   // ช่วงเดินหน้าอย่างเดียว (ไม่สลับไปมาเมื่อความคืบหน้าแกว่งรอบเส้นแบ่ง)
            if (ph != bossPhase)
            {
                bossPhase = ph;
                hud.Toast(ph == 1 ? "ปลาบึกยักษ์ดิ่งลงลึก! ค่อย ๆ กรอ อย่าให้สายตึงเกิน" : ph == 2 ? "มันพุ่งขึ้นผิวน้ำ! ระวังกระชาก" : "มันว่ายเข้าแก่ง! ผ่อนสายตอนมันวิ่ง");
                CameraRig.Impulse(0.6f); bossRunAt = fight.Time + 2.5f; bossSpikeAt = fight.Time + 1.5f;
            }
            fight.PullBoost = ph == 1 ? 0.18f : ph == 2 ? 0.05f : 0.08f;
            fight.ReelTensionMul = 1f; fight.ReelProgMul = 1f;
            if (ph == 2 && fight.Time > bossSpikeAt) { bossSpikeAt = fight.Time + 2.2f + (float)rng.NextDouble() * 1.8f; fight.Tension = Mathf.Min(0.95f, fight.Tension + 0.16f); WaterFx.AddRipple(bob.x, bob.z, 2.2f); Sfx.Splash(); CameraRig.Impulse(0.4f); }
            if (ph == 3)
            {
                if (fight.Time > bossRunAt && fight.Time > bossRunEnd) { bossRunEnd = fight.Time + 2.2f; bossRunAt = bossRunEnd + 2.5f + (float)rng.NextDouble() * 2f; hud.Toast("ผ่อนสาย! มันวิ่งเข้าแก่ง"); }
                bool running = fight.Time < bossRunEnd;
                if (running) { fight.ReelTensionMul = 2.4f; fight.ReelProgMul = -0.35f; }
            }
            if (!Boss.Prayed && Mathf.Repeat(fight.Time, 7f) < dt) { hud.Toast("เงาพญานาคใต้น้ำ… สายสะบัดแรง"); fight.Tension = Mathf.Min(0.95f, fight.Tension + 0.12f); }   // ไม่ไหว้ศาล
            GaugeWobble = Mathf.Max(GaugeWobble, Boss.Prayed ? 0 : 0.5f);
        }

        void FightStep(float dt, bool held)
        {
            if (BossHooked) BossFight(dt, held);
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
                case FightResult.Caught: OnCaught(); break;
                case FightResult.Snapped: hud.Toast("สายขาด! ปลาหนีไปแล้ว "); Sfx.Snap(); DuelLose(); Roll(); Set(FishState.Snapped); from = bob; flight = 0; break;
                case FightResult.Slack: hud.Toast("สายหย่อนเกินไป ปลาหลุด…"); DuelLose(); Roll(); StartRetrieve(); break;
                case FightResult.Escaped: hud.Toast("ปลาว่ายหนีไปได้…"); DuelLose(); Roll(); StartRetrieve(); break;
            }
        }

        void OnCaught()
        {
            WaterFx.AddRipple(bob.x, bob.z, 1.6f);
            caughtGo = MakeFish(fish.species, Mathf.Min(2.4f, 0.55f + fish.cm / 80f));
            caughtGo.transform.position = bob;
            from = bob;
            if (BossHooked) { Boss.MarkCaught(); bossPhase = 0; }
            if (Duel.Active && !Duel.Done) { var dm = Duel.Resolve(true, fish.kg, fish.species.name); if (dm.Length > 0) hud.Toast(dm); }
            caughtIsNew = FishDex.Record(fish.species.id, fish.kg);
            Cloud.SubmitFish(world.StageId, fish.species.id, fish.kg);   // กระดานปลาใหญ่ประจำสัปดาห์ของด่านนี้ (ออนไลน์เท่านั้น)
            if (Shop.ActiveBait != "worm" && Shop.UseBait()) hud.Toast("เหยื่อหมดแล้ว กลับไปใช้ไส้เดือน");
            caughtClue = Mystery.OnCatch(world.StageId, fish.species.id, Story.S.time, NearSpecial, NearDock, rng);
            caughtSecret = Mystery.CheckCatchSecret(fish.species.id, fish.kg, Story.S.time, Story.S.luck);
            dailyMsgs = Daily.OnCatch(world.StageId, fish.species, fish.kg);
            dailyMsgs.AddRange(Weekly.OnCatch(world.StageId, fish.species, fish.kg));   // เทศกาลปลาใหญ่ประจำสัปดาห์   // ภารกิจรายวัน/ปลาเด่น: รางวัลเข้ากระเป๋าทันที แจ้งในการ์ด
            // ข้องเต็ม → ยังไม่ใส่ รอให้ผู้เล่นเลือกแลกหรือปล่อยในการ์ด
            var f = new BasketFish { id = fish.species.id, kg = fish.kg, cm = fish.cm };
            pending = f;   // ยังไม่ใส่ข้อง รอผู้เล่นกด "เก็บ" หรือ "ปล่อย" ในการ์ด
            Set(FishState.Caught); Sfx.Splash();
        }

        BasketFish pending;
        Clue caughtClue;
        FishSecret caughtSecret;
        System.Collections.Generic.List<string> dailyMsgs;

        void ShowCard()
        {
            var img = preview != null ? preview.Live(fish.species) : null;
            string clueNote = caughtClue != null ? $"พบเบาะแส! {caughtClue.name} — {caughtClue.found}" : null;
            string dailyNote = dailyMsgs != null && dailyMsgs.Count > 0 ? string.Join("\n", dailyMsgs) : null;
            if (dailyNote != null) { Sfx.Coin(); clueNote = clueNote != null ? clueNote + "\n" + dailyNote : dailyNote; }
            if (caughtClue != null) hud.Toast($"เบาะแส {Mystery.CluesFound}/{Mystery.Current.clues.Length} · บันทึกลงสมุดคดีแล้ว");
            if (caughtSecret != null) hud.Toast($"ค้นพบความลับของ{fish.species.name}! ดูในสมุดปลา");
            if (BossHooked && pending != null)
            {
                hud.ShowCatch(fish, img, caughtIsNew, "เก็บ (ขายได้ราคาสูง)", () => { Boss.OnKeep(); KeepPending(true); },
                    $"ปล่อยคืนแม่น้ำ (รับพร {Story.Baht(Boss.Blessing)})", () => { Boss.OnRelease(); pending = null; hud.Toast("ปลาบึกยักษ์ว่ายกลับสู่แม่น้ำโขง… ลูกไฟพญานาคลอยขึ้นเป็นสาย"); EndCatch(false); },
                    "ปลาบึกยักษ์แห่งแม่น้ำโขง! ปล่อยคืนแม่น้ำ = พญานาคให้พร ครอบครัวมีความสุข · เก็บ = ขายได้เงินก้อนใหญ่");
                return;
            }
            if (Story.BasketFull)
            {
                int ci = Story.CheapestIndex(); var c = Story.S.basket[ci];
                bool better = Story.Value(pending) > Story.Value(c);
                hud.ShowCatch(fish, img, caughtIsNew, $"เก็บ (แลกกับ{FishDatabase.Get(c.id)?.name} {Story.Baht(Story.Value(c))})", () => KeepPending(true),
                    "ปล่อย", () => KeepPending(false), clueNote ?? (better ? "ตัวนี้ราคาดีกว่าตัวที่ถูกสุดในข้อง ควรแลก!" : "ตัวนี้ราคาน้อยกว่าทุกตัวในข้อง ปล่อยคืนน้ำดีกว่า"));
            }
            else hud.ShowCatch(fish, img, caughtIsNew, $"เก็บใส่ข้อง ({Story.S.basket.Count + 1}/{Story.Capacity})",
                () => { Story.AddFish(pending); pending = null; EndCatch(true); director?.OnFishKept(); },
                "ปล่อย", () => { pending = null; hud.Toast("ปล่อยคืนน้ำ ขอให้โตไว ๆ นะ"); EndCatch(false); }, clueNote);
        }

        void KeepPending(bool keep)
        {
            if (keep && pending != null && !Story.BasketFull) { Story.AddFish(pending); hud.Toast($"เก็บ{FishDatabase.Get(pending.id)?.name}ใส่ข้องแล้ว"); }
            else if (keep && pending != null) { var old = Story.SwapCheapest(pending); hud.Toast($"เก็บ{FishDatabase.Get(pending.id)?.name} · ปล่อย{FishDatabase.Get(old.id)?.name}คืนน้ำ"); }
            else hud.Toast("ปล่อยคืนน้ำ ขอให้โตไว ๆ นะ");
            pending = null;
            EndCatch(keep);
        }

        // ดวล: ตัวที่หลุด/ขาดนับเป็นหนึ่งตัว (น้ำหนัก 0) · ครบห้าตัวแล้วแสดงผล
        void DuelLose()
        {
            if (!Duel.Active || Duel.Done || fish.IsJunk) return;
            var m = Duel.Resolve(false, 0, fish.species.name);
            if (m.Length > 0) hud.Toast(m); else hud.ShowDuelResult();
        }

        void EndCatch(bool kept)
        {
            cardShown = false;
            if (Duel.Done && Duel.Active) hud.ShowDuelResult();
            if (preview != null) preview.StopLive();
            bool taken = caughtGo && ExternalShowcase && !fish.IsJunk && CatchOutro != null && CatchOutro(caughtGo, kept);
            if (caughtGo && !taken) Destroy(caughtGo);
            caughtGo = null;
            Set(FishState.Free);
        }

        // ---------- ภาพ ----------
        float lineW0 = 1;
        void UpdateVisuals(Vector3 tip)
        {
            bool hide = State == FishState.Snapped && t < 1f;
            bobber.SetActive(!hide && State != FishState.Caught);
            line.enabled = !hide;   // ยกปลาโชว์: สายตึงจากปลายคันถึงปากปลา
            bobber.transform.position = bob;
            bobber.transform.rotation = Quaternion.Euler(0, 0, State == FishState.Fight ? Mathf.Sin(WaterFx.Time * 20) * 17 : 0);
            float sag = State == FishState.Waiting || State == FishState.Bite ? 0.9f : State == FishState.Fight ? 0.05f + (1 - fight.Tension) * 0.6f : State == FishState.Casting ? 0.4f : 0.05f;
            var b = bob + Vector3.up * (State == FishState.Caught ? 0.1f : 0.22f);
            var c = (tip + b) / 2 - Vector3.up * sag;
            for (int i = 0; i < linePts.Length; i++)
            {
                float u = i / (linePts.Length - 1f), v = 1 - u;
                linePts[i] = v * v * tip + 2 * v * u * c + u * u * b;
            }
            if (State == FishState.Caught && ExternalShowcase && caughtGo)
            {   // ยกปลาโชว์มุมสายตา: สายผูกที่ปาก (จุดบนสุดของตัวปลาที่ห้อยอยู่) แล้วตรงขึ้นไปหาปลายคันเหนือจอ · ไม่ลากเส้นขาวผ่านข้างตัวปลาลงไปล่างจอ
                var rr = caughtGo.GetComponentInChildren<Renderer>();
                if (rr != null)
                {
                    var bd = rr.bounds; var mouth = new Vector3(bd.center.x, bd.max.y - bd.size.y * 0.02f, bd.center.z);
                    for (int i = 0; i < linePts.Length; i++) linePts[i] = mouth + Vector3.up * (4f * (1 - i / (linePts.Length - 1f)));
                }
            }
            line.SetPositions(linePts);
            line.widthMultiplier = State == FishState.Caught ? lineW0 * 0.2f : lineW0;   // ปลาห้อยอยู่ใกล้กล้องมาก สายปกติจะดูเป็นแท่งหนา
        }

        void UpdatePose()
        {
            player.CanWalk = State == FishState.Free;
            player.CanTurn = State == FishState.Free || State == FishState.Charging;
            float now = WaterFx.Time;
            float a = State switch
            {
                FishState.Charging => -80 * charge,
                FishState.Casting => Mathf.Min(31, -80 + t * 800),
                FishState.Waiting or FishState.Bite or FishState.Landed => 20 + Mathf.Sin(now * 1.5f),
                FishState.Strike => -34 + Mathf.Sin(now * 25) * 6,
                FishState.Fight => -20 + Mathf.Sin(now * 30) * 2.3f * fight.Tension,
                FishState.Caught => -150,
                _ => 0,
            };
            player.ArmRTarget = a * armSign;
            // ท่าตัวละคร Mixamo: คลิป "เหวี่ยงเบ็ด+กรอสาย" 8.8 วิ — ง้าง 0.06→0.165 · สะบัดไปหน้า 0.165→0.30 (ปล่อยสาย ~0.215) · กรอสาย 0.52→0.92
            switch (State)
            {
                case FishState.Charging: player.Pose = 1; player.CastT = Mathf.Lerp(0.06f, 0.165f, Mathf.SmoothStep(0, 1, charge)); break;
                case FishState.Casting: player.Pose = 1; player.CastT = Mathf.Lerp(0.165f, 0.3f, Mathf.Clamp01(t / (ThrowDelay * 2.6f))); break;
                case FishState.Fight: case FishState.Retrieve: case FishState.Snapped:
                    player.Pose = 1; reelT += Time.deltaTime * (State == FishState.Fight && !GameInput.ActionHeld ? 0.03f : 0.11f);
                    player.CastT = 0.52f + Mathf.Repeat(reelT, 0.4f); break;
                case FishState.Free: player.Pose = 0; break;
                default: player.Pose = 3; break;
            }
            player.ArmLTarget = State == FishState.Caught ? -150 * armSign : State == FishState.Fight ? (GameInput.ActionHeld ? -52 + Mathf.Sin(now * 18) * 20 : -34) * armSign : float.NaN;
        }

        void UpdateHud()
        {
            bool touch = hud.TouchUi;
            switch (State)
            {
                case FishState.Free: hud.Hint(touch ? "กดค้างปุ่มเหวี่ยงเพื่อเล็ง · ลากจอเพื่อหมุนกล้อง" : "คลิกค้าง / กด E ค้าง เพื่อเล็ง · ปล่อยเพื่อเหวี่ยง\nWASD เดิน · Space กระโดด · คลิกขวาลาก หมุนกล้อง · ล้อเมาส์ ซูม"); hud.ActionLabel("", false, "cast"); break;
                case FishState.Charging: hud.Hint(""); hud.ActionLabel("", true, "cast"); break;
                case FishState.Casting: case FishState.Landed: case FishState.Retrieve: hud.Hint(""); hud.ActionLabel("", false, "wait"); break;
                case FishState.Waiting:
                    if (Jigging) { hud.Hint("แตะตอนไฟเขียวสว่าง ล่อหมึกเข้าวงแสง · กดค้างเก็บสาย"); hud.ActionLabel("", JigPulse > 0.5f, "reelin"); }   // วงปุ่มสว่างตามจังหวะไฟ
                    else { hud.Hint("รอให้ทุ่นจม… (กดเพื่อเก็บสาย)"); hud.ActionLabel("", false, "reelin"); }
                    break;
                case FishState.Bite: hud.Hint("ทุ่นจม! กดเลย!"); hud.ActionLabel("ดึง!", true, "hook"); break;
                case FishState.Strike: hud.Hint("กดตอนเข็มอยู่ในโซนทอง!"); hud.ActionLabel("ตีเบ็ด!", true, "strike"); break;
                case FishState.Fight: hud.Hint(touch ? "กดค้างกรอสาย · ปล่อยเมื่อใกล้แดง" : "กดค้างเพื่อกรอสาย · ปล่อยเมื่อเข็มใกล้สีแดง"); hud.ActionLabel("", false, "reel"); break;
                default: hud.Hint(""); break;
            }
            hud.Power(State == FishState.Charging ? charge : -1);
            hud.LineDistance(LineOut || State == FishState.Bite ? Vector2.Distance(new Vector2(player.Pos.x, player.Pos.z), new Vector2(bob.x, bob.z)) : -1);
            hud.Tension(State == FishState.Fight, Mathf.Clamp01((fight?.Tension ?? 0) + GaugeWobble * 0.18f * Mathf.Sin(Time.time * 11) * Mathf.PerlinNoise(Time.time * 4, 2)), fight?.Progress ?? 0, fight?.ZoneCenter ?? 0.5f, fight?.ZoneWidth ?? 0.6f, State == FishState.Fight && fight.Tension > 0.85f);
            hud.Strike(State == FishState.Strike, strike?.Pos ?? 0, strike?.Center ?? 0, strike?.Width ?? 0, strike?.Hits ?? 0, strike?.Need ?? 0, strike?.Misses ?? 0);
            hud.Alert(State == FishState.Bite, player.Pos + Vector3.up * 2.3f, !fish.IsJunk && fish.species != null && fish.species.stars >= 3);
        }

        // ---------- สร้างวัตถุ ----------
        // วัสดุปลาจากภาพถ่าย (หนึ่งชิ้นต่อชนิด ใช้ซ้ำ) · ปลาหายากเรืองแสงทองจาง ๆ
        static readonly Dictionary<string, Material> photoMats = new Dictionary<string, Material>();
        public static Material PhotoMat(FishSpecies sp)
        {
            if (photoMats.TryGetValue(sp.id, out var m) && m != null) return m;
            var baseMat = Resources.Load<Material>("Fish/FishPhoto");
            m = new Material(baseMat) { name = "FishPhoto_" + sp.id };
            m.SetTexture("_BaseMap", sp.Photo);
            m.SetFloat("_Wiggle", 0.07f); m.SetFloat("_SwimSpeed", 13f);
            if (sp.glow) m.SetColor("_EmissionColor", new Color(0.16f, 0.11f, 0.02f));
            photoMats[sp.id] = m;
            return m;
        }
        // ปลาที่ตกได้: โคลนโมเดลจาก Blender แล้วย้อมสีตามชนิด
        public GameObject MakeFish(FishSpecies sp, float size)
        {
            var mesh = sp.Photo != null ? sp.PhotoMesh : null;
            if (mesh != null)
            {
                var pg = new GameObject("Fish_" + sp.id);
                pg.AddComponent<MeshFilter>().sharedMesh = mesh;
                pg.AddComponent<MeshRenderer>().sharedMaterial = PhotoMat(sp);
                pg.transform.localScale = Vector3.one * size * 1.15f;
                return pg;
            }
            var g = Instantiate(fishTemplate);
            g.SetActive(true);
            g.transform.localScale = new Vector3(sp.shape[0], sp.shape[1], sp.shape[2]) * size;
            var mpb = new MaterialPropertyBlock();
            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var n = mats[i] ? mats[i].name : "";
                    if (n.Contains("Eye")) continue;
                    mpb.Clear();
                    mpb.SetColor("_BaseColor", n.Contains("Fin") ? sp.FinColor : Color.Lerp(sp.TopColor, sp.BellyColor, 0.25f));
                    if (sp.glow) mpb.SetColor("_EmissionColor", new Color(0.35f, 0.23f, 0f));
                    r.SetPropertyBlock(mpb, i);
                }
            }
            return g;
        }
    }
}
