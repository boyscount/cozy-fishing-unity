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
        bool paused;
        /// อยู่บ้าน/ระหว่างเดินทาง: หยุดระบบตกปลา ซ่อนทุ่นและสาย
        public bool Paused
        {
            get => paused;
            set { paused = value; bobber.SetActive(!value); line.enabled = !value; aim.SetActive(false); if (value) { player.CanWalk = false; player.CanTurn = false; player.Pose = 0; } }
        }

        IWorld world;
        readonly System.Random rng = new System.Random();
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
            Bot = Application.absoluteURL.Contains("bot") || System.Environment.CommandLine.Contains("-bot");
            line.positionCount = linePts.Length;
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
        float botNext;
        bool BotHeld()
        {
            switch (State)
            {
                case FishState.Free: return t > 1.2f && t < 1.2f + 0.9f;            // ง้าง ~0.9 วิ แล้วปล่อย
                case FishState.Charging: return charge < 0.7f;
                case FishState.Bite: return t > 0.25f;                              // รอเห็นทุ่นจมแป๊บหนึ่งค่อยกด
                case FishState.Strike:
                    bool inZone = Mathf.Abs(strike.Pos - strike.Center) < strike.Width * 0.35f;
                    if (inZone && Time.time > botNext) { botNext = Time.time + 0.35f; return true; }
                    return false;
                case FishState.Fight: return fight.Tension < Mathf.Min(0.8f, fight.ZoneCenter + fight.ZoneWidth * 0.3f);
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
                    charge = Mathf.Min(1, charge + dt / 1.25f);
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
                        Set(FishState.Bite);
                        biteEnd = fish.IsJunk ? 1.2f : fish.species.win;
                        WaterFx.AddRipple(bob.x, bob.z, 1.5f);
                    }
                    break;
                case FishState.Bite:
                    bob.y = world.WaterY - 0.2f + Mathf.Sin(t * 40) * 0.03f;
                    if (t > biteEnd) { hud.Toast("ช้าไปนิด ปลากินเหยื่อหนีไป"); Roll(); NewBite(); Set(FishState.Waiting); }
                    break;
                case FishState.Strike:
                    strike.Step(dt);
                    bob.y = world.WaterY - 0.22f + Mathf.Sin(t * 35) * 0.04f;
                    if (strike.Time > 8f) { hud.Toast("ปลาเบื่อรอ ว่ายหนีไปแล้ว…"); Roll(); StartRetrieve(); }
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
                    if (caughtGo)
                    {
                        caughtGo.transform.position = Vector3.Lerp(from, top, kc) + Vector3.up * Mathf.Sin(kc * Mathf.PI) * 1.2f;
                        caughtGo.transform.rotation = Quaternion.Euler(Mathf.Sin(t * 9) * 17, t * 140, Mathf.Sin(t * 12) * 14);
                    }
                    bob = caughtGo ? caughtGo.transform.position : top;
                    if (t > 0.9f && !hud.CardOpen && caughtGo && !cardShown) { cardShown = true; ShowCard(); }
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
                case FishState.Waiting: StartRetrieve(); hud.Toast("เก็บสาย"); break;
                case FishState.Bite:
                    if (!fish.IsJunk && fish.species.stars >= 3) { strike = new StrikeModel(fish.species.stars, rng); Set(FishState.Strike); hud.Toast(fish.species.stars >= 4 ? "ตัวนี้ไม่ธรรมดา! ตีเบ็ดให้ตรงจังหวะ 3 ครั้ง" : "ปลาใหญ่! ตีเบ็ดให้ตรงจังหวะ 2 ครั้ง"); }
                    else StartFight();
                    break;
                case FishState.Strike:
                    if (strike.Press()) { WaterFx.AddRipple(bob.x, bob.z, 1.2f); if (strike.Done) { hud.Toast("ติดเบ็ดแล้ว! ระวังโซนเขียวเลื่อนไปมา"); StartFight(); } }
                    else if (strike.Failed) { hud.Toast("ปลาสะบัดหลุด! ต้องตีให้ตรงจังหวะ"); Roll(); StartRetrieve(); }
                    else hud.Toast("พลาด! อีกครั้งเดียวนะ");
                    break;
            }
        }

        void OnUp() { if (State == FishState.Charging) Cast(); }

        Vector3 CastPoint(float c) { var f = player.Forward; float d = 1.3f + c * 8.7f; return new Vector3(player.Pos.x + f.x * d, 0, player.Pos.z + f.z * d); }

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
        void Roll()
        {
            // สืบคดี: ระหว่างตามหาเบาะแส ปลาที่พาเบาะแส/ขวดจดหมาย มีโอกาสโผล่มากขึ้นเมื่ออยู่ในเงื่อนไข
            var o = Mystery.Override(Story.S.time, NearSpecial, NearDock, rng);
            if (o.HasValue) { fish = o.Value; return; }
            RollNormal();
        }
        void RollNormal() => fish = Story.S.dead && rng.NextDouble() < 0.85
            ? FishRoller.Roll(world.StageId, false, 0, 1f, Story.S.time, rng)
            : FishRoller.Roll(world.StageId, NearSpecial, world.DepthAt(bob.x, bob.z), Story.JunkChance, Story.S.time, rng);

        void Land()
        {
            bob = target;
            if (!onWater) { hud.Toast("เหวี่ยงไม่ถึงน้ำ ลองใหม่นะ"); Set(FishState.Landed); return; }
            WaterFx.AddRipple(bob.x, bob.z, 1.2f);
            Roll(); NewBite();
            Set(FishState.Waiting);
            if (NearSpecial) hud.Toast("จุดนี้ดูน่าสนใจ…");
        }

        void NewBite()
        {
            biteAt = t + (2.2f + (float)rng.NextDouble() * 5.5f) * Story.BiteMul;
            nibbles.Clear();
            int n = rng.Next(3);
            for (int i = 0; i < n; i++) nibbles.Add(biteAt - 0.6f - (float)rng.NextDouble() * 1.8f);
            nibbles.Sort();
        }

        void StartRetrieve() { flight = 0; from = bob; Set(FishState.Retrieve); }

        void StartFight()
        {
            if (fish.IsJunk)
            {
                var bottle = fish.junk == Mystery.BottleJunk ? Mystery.OnBottle() : null;
                if (bottle != null) hud.ShowClue(bottle);
                else hud.Toast($"ได้{fish.junk} …เก็บไปทิ้งให้น้ำสะอาด");
                Roll(); StartRetrieve(); return;
            }
            fight = new FightModel(fish.species, false, rng);
            fight.OnDash += () => WaterFx.AddRipple(bob.x, bob.z, 1.2f);
            from = bob;
            Set(FishState.Fight);
        }

        float nextSplash;
        void FightStep(float dt, bool held)
        {
            var r = fight.Step(dt, held);
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
                case FightResult.Snapped: hud.Toast("สายขาด! ปลาหนีไปแล้ว "); Roll(); Set(FishState.Snapped); from = bob; flight = 0; break;
                case FightResult.Slack: hud.Toast("สายหย่อนเกินไป ปลาหลุด…"); Roll(); StartRetrieve(); break;
                case FightResult.Escaped: hud.Toast("ปลาว่ายหนีไปได้…"); Roll(); StartRetrieve(); break;
            }
        }

        void OnCaught()
        {
            WaterFx.AddRipple(bob.x, bob.z, 1.6f);
            caughtGo = MakeFish(fish.species, Mathf.Min(2.4f, 0.55f + fish.cm / 80f));
            caughtGo.transform.position = bob;
            from = bob;
            caughtIsNew = FishDex.Record(fish.species.id, fish.kg);
            caughtClue = Mystery.OnCatch(fish.species.id, Story.S.time, NearSpecial, NearDock, rng);
            caughtSecret = Mystery.CheckCatchSecret(fish.species.id, fish.kg, Story.S.time, Story.S.luck);
            // ข้องเต็ม → ยังไม่ใส่ รอให้ผู้เล่นเลือกแลกหรือปล่อยในการ์ด
            var f = new BasketFish { id = fish.species.id, kg = fish.kg, cm = fish.cm };
            pending = f;   // ยังไม่ใส่ข้อง รอผู้เล่นกด "เก็บ" หรือ "ปล่อย" ในการ์ด
            Set(FishState.Caught);
        }

        BasketFish pending;
        Clue caughtClue;
        FishSecret caughtSecret;

        void ShowCard()
        {
            var img = preview != null ? preview.Live(fish.species) : null;
            string clueNote = caughtClue != null ? $"พบเบาะแส! {caughtClue.name} — {caughtClue.found}" : null;
            if (caughtClue != null) hud.Toast($"เบาะแส {Mystery.CluesFound}/{Mystery.Current.clues.Length} · บันทึกลงสมุดคดีแล้ว");
            if (caughtSecret != null) hud.Toast($"ค้นพบความลับของ{fish.species.name}! ดูในสมุดปลา");
            if (Story.BasketFull)
            {
                int ci = Story.CheapestIndex(); var c = Story.S.basket[ci];
                bool better = Story.Value(pending) > Story.Value(c);
                hud.ShowCatch(fish, img, caughtIsNew, $"เก็บ (แลกกับ{FishDatabase.Get(c.id)?.name} {Story.Baht(Story.Value(c))})", () => KeepPending(true),
                    "ปล่อย", () => KeepPending(false), clueNote ?? (better ? "ตัวนี้ราคาดีกว่าตัวที่ถูกสุดในข้อง ควรแลก!" : "ตัวนี้ราคาน้อยกว่าทุกตัวในข้อง ปล่อยคืนน้ำดีกว่า"));
            }
            else hud.ShowCatch(fish, img, caughtIsNew, $"เก็บใส่ข้อง ({Story.S.basket.Count + 1}/{Story.Capacity})",
                () => { Story.AddFish(pending); pending = null; EndCatch(); director?.OnFishKept(); },
                "ปล่อย", () => { pending = null; hud.Toast("ปล่อยคืนน้ำ ขอให้โตไว ๆ นะ"); EndCatch(); }, clueNote);
        }

        void KeepPending(bool keep)
        {
            if (keep && pending != null && !Story.BasketFull) { Story.AddFish(pending); hud.Toast($"เก็บ{FishDatabase.Get(pending.id)?.name}ใส่ข้องแล้ว"); }
            else if (keep && pending != null) { var old = Story.SwapCheapest(pending); hud.Toast($"เก็บ{FishDatabase.Get(pending.id)?.name} · ปล่อย{FishDatabase.Get(old.id)?.name}คืนน้ำ"); }
            else hud.Toast("ปล่อยคืนน้ำ ขอให้โตไว ๆ นะ");
            pending = null;
            EndCatch();
        }

        void EndCatch()
        {
            cardShown = false;
            if (preview != null) preview.StopLive();
            if (caughtGo) Destroy(caughtGo);
            caughtGo = null;
            Set(FishState.Free);
        }

        // ---------- ภาพ ----------
        void UpdateVisuals(Vector3 tip)
        {
            bool hide = State == FishState.Snapped && t < 1f;
            bobber.SetActive(!hide && State != FishState.Caught);
            line.enabled = !hide;
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
            line.SetPositions(linePts);
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
                case FishState.Free: hud.Hint(touch ? "กดค้างปุ่มเหวี่ยงเพื่อเล็ง · ลากจอเพื่อหมุนกล้อง" : "คลิกค้าง / กด E ค้าง เพื่อเล็ง · ปล่อยเพื่อเหวี่ยง\nWASD เดิน · Space กระโดด · คลิกขวาลาก หมุนกล้อง · ล้อเมาส์ ซูม"); hud.ActionLabel("เหวี่ยง", false); break;
                case FishState.Charging: hud.Hint(""); hud.ActionLabel("ปล่อย!", false); break;
                case FishState.Casting: case FishState.Landed: case FishState.Retrieve: hud.Hint(""); hud.ActionLabel("…", false); break;
                case FishState.Waiting: hud.Hint("รอให้ทุ่นจม… (กดเพื่อเก็บสาย)"); hud.ActionLabel("เก็บสาย", false); break;
                case FishState.Bite: hud.Hint("ทุ่นจม! กดเลย!"); hud.ActionLabel("ดึง!", true); break;
                case FishState.Strike: hud.Hint("กดตอนเข็มอยู่ในโซนทอง!"); hud.ActionLabel("ตีเบ็ด!", true); break;
                case FishState.Fight: hud.Hint(touch ? "กดค้างกรอสาย · ปล่อยเมื่อใกล้แดง" : "กดค้างเพื่อกรอสาย · ปล่อยเมื่อเข็มใกล้สีแดง"); hud.ActionLabel("กรอสาย", false); break;
                default: hud.Hint(""); break;
            }
            hud.Power(State == FishState.Charging ? charge : -1);
            hud.LineDistance(LineOut || State == FishState.Bite ? Vector2.Distance(new Vector2(player.Pos.x, player.Pos.z), new Vector2(bob.x, bob.z)) : -1);
            hud.Tension(State == FishState.Fight, fight?.Tension ?? 0, fight?.Progress ?? 0, fight?.ZoneCenter ?? 0.5f, fight?.ZoneWidth ?? 0.6f, State == FishState.Fight && fight.Tension > 0.85f);
            hud.Strike(State == FishState.Strike, strike?.Pos ?? 0, strike?.Center ?? 0, strike?.Width ?? 0, strike?.Hits ?? 0, strike?.Need ?? 0, strike?.Misses ?? 0);
            hud.Alert(State == FishState.Bite, player.Pos + Vector3.up * 2.3f, !fish.IsJunk && fish.species != null && fish.species.stars >= 3);
        }

        // ---------- สร้างวัตถุ ----------
        // ปลาที่ตกได้: โคลนโมเดลจาก Blender แล้วย้อมสีตามชนิด
        public GameObject MakeFish(FishSpecies sp, float size)
        {
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
