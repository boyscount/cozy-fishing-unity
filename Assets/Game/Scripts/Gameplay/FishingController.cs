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
        public GameObject fishTemplate;      // โมเดลปลาจาก Blender (ซ่อนไว้ในฉาก)
        public GameObject bobber, aim;       // สร้างไว้ในฉากโดย SceneBuilder (WebGL ตัดโมดูล Physics/shader ที่ไม่ได้อ้างถึงออก)
        public LineRenderer line;
        public float armSign = 1f;           // กลับทิศหมุนแขนถ้าโมเดลนำเข้ามากลับด้าน

        public FishState State { get; private set; }
        public Vector3 BobberPos => bob;
        public bool LineOut => State >= FishState.Casting && State <= FishState.Fight;
        public int Caught { get; private set; }
        public int Money { get; private set; }

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
            line.positionCount = linePts.Length;
            aimRenderer = aim.GetComponentInChildren<Renderer>();
            aimMpb = new MaterialPropertyBlock();
            aim.SetActive(false);
            Set(FishState.Free);
        }

        void Set(FishState s) { State = s; t = 0; }

        void Update()
        {
            float dt = Time.deltaTime;
            bool held = GameInput.ActionHeld;
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
                    aim.transform.localScale = Vector3.one * (1 + Mathf.Sin(now * 8) * 0.08f);
                    aimMpb.SetColor("_BaseColor", w ? Color.white : new Color(1f, 0.54f, 0.44f));
                    aimRenderer.SetPropertyBlock(aimMpb);
                    break;
                case FishState.Casting:
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
                    if (t > 0.9f && !hud.CardOpen && caughtGo) hud.ShowCatch(fish, EndCatch);
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
        void Roll() => fish = FishRoller.Roll(world.StageId, NearSpecial, world.DepthAt(bob.x, bob.z), 0.08f, 10f, rng);

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
            biteAt = t + 2.2f + (float)rng.NextDouble() * 5.5f;
            nibbles.Clear();
            int n = rng.Next(3);
            for (int i = 0; i < n; i++) nibbles.Add(biteAt - 0.6f - (float)rng.NextDouble() * 1.8f);
            nibbles.Sort();
        }

        void StartRetrieve() { flight = 0; from = bob; Set(FishState.Retrieve); }

        void StartFight()
        {
            if (fish.IsJunk) { hud.Toast($"ได้{fish.junk} …เก็บไปทิ้งให้น้ำสะอาด"); Roll(); StartRetrieve(); return; }
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
            Caught++;
            Money += Mathf.RoundToInt(fish.kg * fish.species.price);
            Set(FishState.Caught);
        }

        void EndCatch()
        {
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
            player.ArmLTarget = State == FishState.Caught ? -150 * armSign : State == FishState.Fight ? (GameInput.ActionHeld ? -52 + Mathf.Sin(now * 18) * 20 : -34) * armSign : float.NaN;
        }

        void UpdateHud()
        {
            bool touch = hud.TouchUi;
            switch (State)
            {
                case FishState.Free: hud.Hint(touch ? "กดค้างปุ่มเหวี่ยง เพื่อเล็ง · ปล่อยเพื่อเหวี่ยง" : "กดค้าง Space / คลิกค้าง เพื่อเล็ง · ปล่อยเพื่อเหวี่ยงเบ็ด"); hud.ActionLabel("เหวี่ยง", false); break;
                case FishState.Charging: hud.Hint(""); hud.ActionLabel("ปล่อย!", false); break;
                case FishState.Waiting: hud.Hint("รอให้ทุ่นจม… (กดเพื่อเก็บสาย)"); hud.ActionLabel("เก็บสาย", false); break;
                case FishState.Bite: hud.Hint("ทุ่นจม! กดเลย!"); hud.ActionLabel("ดึง!", true); break;
                case FishState.Strike: hud.Hint("กดตอนเข็มอยู่ในโซนทอง!"); hud.ActionLabel("ตีเบ็ด!", true); break;
                case FishState.Fight: hud.Hint(touch ? "กดค้างกรอสาย · ปล่อยเมื่อใกล้แดง" : "กดค้างเพื่อกรอสาย · ปล่อยเมื่อเข็มใกล้สีแดง"); hud.ActionLabel("กรอสาย", false); break;
                default: hud.Hint(""); break;
            }
            hud.Power(State == FishState.Charging ? charge : -1);
            hud.Tension(State == FishState.Fight, fight?.Tension ?? 0, fight?.Progress ?? 0, fight?.ZoneCenter ?? 0.5f, fight?.ZoneWidth ?? 0.6f, State == FishState.Fight && fight.Tension > 0.85f);
            hud.Strike(State == FishState.Strike, strike?.Pos ?? 0, strike?.Center ?? 0, strike?.Width ?? 0, strike?.Hits ?? 0, strike?.Need ?? 0, strike?.Misses ?? 0);
            hud.Alert(State == FishState.Bite, player.Pos + Vector3.up * 2.3f, !fish.IsJunk && fish.species != null && fish.species.stars >= 3);
            hud.SetCount(Caught, Money);
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
