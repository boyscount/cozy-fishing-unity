using System.Collections.Generic;
using UnityEngine;

namespace CozyFishing
{
    // ทอดแห (ถือแหแทนเบ็ด): ฝูงปลาเล็กว่ายวนในบ่อ บางช่วงรวมฝูงแน่นผุดน้ำ
    //   กดค้าง = แกว่งแห (เข็มแกว่งไปมา + วงเป้าเลื่อนออกไกลขึ้นแบบเหวี่ยงเบ็ด) → ปล่อยตอนเข็มเข้าช่องเขียว = แหกางเต็มวง
    //   แหตกครอบปลาตัวไหนได้ตัวนั้น → สาวแห 3 ครั้งตามจังหวะ (แตะผิดจังหวะ ปลาหลุดทีละตัว) → การ์ดสรุป เก็บใส่ข้อง/ปล่อย
    sealed class NetThrow
    {
        enum Phase { Swing, Fly, Sink, Haul, Return, Result }
        const float MinDist = 3f, MaxDist = 9.5f, SchoolFish = 9;

        readonly FishingController c;
        Phase phase; float pt;
        float charge, needle;
        CastNet.Grade grade; float radius;
        Vector3 target, netPos, netFrom; bool onWater;
        int pulls; float idle;
        readonly List<Swimmer> caught = new List<Swimmer>();

        // ฝูงปลา
        class Swimmer { public GameObject go; public FishSpecies sp; public float kg; public Vector2 off; public float phase, flee, jump = -1; public Vector3 pos, dir; }
        readonly List<Swimmer> school = new List<Swimmer>();
        Vector3 schoolPos, schoolGoal; float goalAt, respawnAt;
        string schoolStage;

        // ภาพ: วงเป้าสีทองบนน้ำ · ตัวแห (ซี่ + วงตาข่าย + ขอบตะกั่ว)
        LineRenderer ring, spokes, mesh, rim;

        public NetThrow(FishingController c) { this.c = c; }

        public bool Busy => c.State == FishState.Netting;
        public float Needle => needle;
        public bool Swinging => Busy && phase == Phase.Swing;
        public bool Hauling => Busy && phase == Phase.Haul;
        public int PullsDone => pulls;
        public int InNet => caught.Count;
        public float HaulTime => pt;
        public bool AimOnWater => onWater;
        /// ท่าตัวละคร (คลิปเหวี่ยงเบ็ด): แกว่ง = ง้างไปมาตามเข็ม · ปล่อย = สะบัดไปหน้า · สาวแห = ท่ากรอสาย
        public float PoseT => phase switch
        {
            Phase.Swing => Mathf.Lerp(0.07f, 0.165f, needle),
            Phase.Fly => Mathf.Lerp(0.165f, 0.3f, Mathf.Clamp01(pt / 0.35f)),
            Phase.Haul or Phase.Return => 0.52f + Mathf.Repeat(pt * 0.11f, 0.4f),
            _ => 0.3f,
        };
        public bool CanTurn => !Busy || phase == Phase.Swing;
        public bool HasSchool => school.Count >= 4;

        // ---------- เริ่ม/กด ----------
        public bool Begin()
        {
            if (CastNet.ThrowsLeft <= 0) { c.hud.Toast($"วันนี้ทอดแหครบ {CastNet.ThrowsPerDay} ครั้งแล้ว แหเปียกหนัก · สลับไปใช้เบ็ดได้"); return false; }
            phase = Phase.Swing; pt = 0; charge = 0; needle = 0;
            c.Set(FishState.Netting);
            return true;
        }

        public void Down()
        {
            if (phase != Phase.Haul) return;
            idle = 0;
            if (CastNet.PullOnBeat(pt))
            {
                pulls++; Sfx.Nibble(); WaterFx.AddRipple(netPos.x, netPos.z, 0.8f);
                if (pulls >= CastNet.Pulls) { phase = Phase.Return; pt = 0; netFrom = netPos; }
            }
            else if (caught.Count > 0)
            {
                var s = caught[caught.Count - 1]; caught.RemoveAt(caught.Count - 1); s.flee = 3f; school.Add(s);
                c.hud.Toast("สาวเร็วไป! ปลาหลุดไป 1 ตัว · แตะตอนวงสว่าง");
            }
        }

        public void Up()
        {
            if (phase != Phase.Swing) return;
            grade = CastNet.GradeOf(needle);
            if (!onWater) { c.hud.Toast("ทอดไม่ถึงน้ำ ลองหันไปทางน้ำแล้วทอดใหม่"); End(); return; }
            CastNet.UseThrow();
            radius = CastNet.FullRadius * CastNet.Open(grade);
            c.hud.NetPop(grade == CastNet.Grade.Perfect ? "PERFECT!" : grade == CastNet.Grade.Good ? "ดี!" : "แหพับ…", grade == CastNet.Grade.Perfect);
            netFrom = Hand; netPos = netFrom;
            phase = Phase.Fly; pt = 0;
        }

        Vector3 Hand => c.player.Pos + Vector3.up * 1.4f + c.player.Forward * 0.4f;

        // ---------- ทุกเฟรม ----------
        public void Step(float dt, bool held)
        {
            pt += dt;
            var world = c.world;
            switch (phase)
            {
                case Phase.Swing:
                    if (GameInput.PullPressed) { c.hud.Toast("เก็บแหไว้ก่อน ยังไม่เสียครั้งทอด"); End(); return; }   // ยกเลิกระหว่างแกว่ง (Q / คลิกขวา / ปุ่มบนจอ) · ยังไม่นับครั้ง
                    charge = Mathf.Min(1, charge + dt / 1.6f);
                    needle = CastNet.Needle(pt);
                    var f = c.player.Forward; float d = MinDist + charge * (MaxDist - MinDist);
                    target = new Vector3(c.player.Pos.x + f.x * d, world.WaterY + 0.03f, c.player.Pos.z + f.z * d);
                    if (FishingController.Bot && school.Count > 0) target = new Vector3(schoolPos.x, target.y, schoolPos.z);   // ทดสอบ: บอทเล็งกลางฝูง
                    onWater = world.CanCast(target.x, target.z);
                    bool hot = CastNet.GradeOf(needle) == CastNet.Grade.Perfect;
                    DrawRing(ring, target, CastNet.FullRadius, onWater ? (hot ? new Color(1f, 0.85f, 0.25f) : new Color(1f, 0.8f, 0.4f, 0.7f)) : new Color(1f, 0.45f, 0.35f));
                    ring.enabled = true; SetNet(false);
                    break;
                case Phase.Fly:
                {
                    float k = Mathf.Clamp01(pt / 0.75f);
                    netPos = Vector3.Lerp(netFrom, target, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 2.2f;
                    float rr = Mathf.Lerp(0.25f, radius, Mathf.SmoothStep(0, 1, k)); DrawNet(netPos, rr, rr * Mathf.Lerp(0.9f, 0.55f, k));   // กลางอากาศ: แหพองเป็นโดม ค่อย ๆ บานออก
                    ring.enabled = true;
                    if (k >= 1) { Sfx.Splash(); Splash(target, radius); sprayAt = Time.time; Trap(); phase = Phase.Sink; pt = 0; }
                    break;
                }
                case Phase.Sink:
                    netPos = target;
                    DrawNet(target + Vector3.down * Mathf.Min(0.12f, pt * 0.2f), radius * (1 - pt * 0.05f), radius * 0.6f * (1 - Mathf.Min(1, pt / 1.1f) * 0.55f));   // ครอบน้ำ: ตะกั่วจมขอบ โดมค่อย ๆ ยุบ
                    ring.enabled = false;
                    if (pt > 1.1f)
                    {
                        if (caught.Count == 0) { c.hud.Toast(grade == CastNet.Grade.Poor ? "แหพับ ครอบไม่โดนฝูง… ปล่อยตอนเข็มเข้าช่องเขียวนะ" : "แหว่างเปล่า… รอให้ฝูงปลาผุดน้ำ แล้วทอดให้ครอบกลางฝูง"); phase = Phase.Return; pt = 0; netFrom = netPos; pulls = CastNet.Pulls; break; }
                        c.hud.Toast($"ติดแห {caught.Count} ตัว! สาวแหตามจังหวะ {CastNet.Pulls} ครั้ง");
                        phase = Phase.Haul; pt = 0; pulls = 0; idle = 0; netFrom = target;
                    }
                    break;
                case Phase.Haul:
                {
                    idle += dt;
                    float k = pulls / (float)CastNet.Pulls;
                    var goal = Vector3.Lerp(netFrom, c.player.Pos + c.player.Forward * 1.2f, k * 0.8f); goal.y = world.WaterY - 0.05f;   // ขอบตะกั่วเรี่ยผิวน้ำ ยังเห็นแหถูกสาวเข้ามา
                    netPos = Vector3.Lerp(netPos, goal, Mathf.Min(1, dt * 4));
                    float hr = Mathf.Lerp(radius, 0.5f, k); DrawNet(netPos, hr, hr * 0.35f + 0.15f);   // สาวแห: ตะกั่วรูดเข้าหากัน
                    if (FishingController.Bot && CastNet.PullOnBeat(pt) && CastNet.PullPulse(pt) > 0.9f && idle > 0.4f) Down();
                    break;
                }
                case Phase.Return:
                {
                    float k = Mathf.Clamp01(pt / 0.7f);
                    netPos = Vector3.Lerp(netFrom, Hand, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.6f;
                    DrawNet(netPos + Vector3.down * 0.9f, Mathf.Lerp(0.45f, 0.18f, k), 0.9f);   // ยกขึ้นจากน้ำ: แหห้อยเป็นถุง
                    if (k >= 1) { SetNet(false); ShowResult(); }
                    break;
                }
            }
            // ปลาที่ติดแห ดิ้นอยู่ในแห
            for (int i = 0; i < caught.Count; i++)
            {
                var s = caught[i]; var p = netPos + new Vector3(Mathf.Cos(i * 2.4f) , 0, Mathf.Sin(i * 2.4f)) * 0.35f * Mathf.Min(1, radius);
                p.y = phase == Phase.Return ? netPos.y - 0.25f : Mathf.Min(netPos.y, world.WaterY - 0.18f);
                s.go.transform.SetPositionAndRotation(p, Quaternion.Euler(Mathf.Sin(Time.time * 17 + i) * 30, i * 70 + Time.time * 40, 0));
            }
        }

        // แหตกน้ำ: ปลาในวงติดแห · ตัวที่อยู่ใกล้ขอบตกใจว่ายหนี
        void Trap()
        {
            caught.Clear();
            for (int i = school.Count - 1; i >= 0; i--)
            {
                var s = school[i]; float dd = new Vector2(s.pos.x - target.x, s.pos.z - target.z).magnitude;
                if (dd <= radius) { caught.Add(s); school.RemoveAt(i); }
                else if (dd < radius + 2.5f) s.flee = 2.5f;
            }
        }

        void Splash(Vector3 p, float r)
        {
            WaterFx.AddRipple(p.x, p.z, 1.6f);
            for (int i = 0; i < 4; i++) { float a = i * Mathf.PI / 2 + 0.4f; WaterFx.AddRipple(p.x + Mathf.Cos(a) * r, p.z + Mathf.Sin(a) * r, 0.7f); }
        }

        void ShowResult()
        {
            if (caught.Count == 0) { End(); return; }
            phase = Phase.Result;
            var fish = new List<(FishSpecies, float)>(); foreach (var s in caught) fish.Add((s.sp, s.kg));
            var hauls = CastNet.Group(fish);
            bool anyNew = false;
            foreach (var s in caught) anyNew |= FishDex.Record(s.sp.id, s.kg);
            int lv = Skills.AddXp(5 + 3 * caught.Count);
            if (lv > 0) c.hud.Toast($"ระดับชาวประมง {Skills.FisherLevel}! ได้แต้มทักษะ (ดูที่หน้าเช้า ปุ่มทักษะ)");
            Sfx.Coin();
            c.hud.NetResult(hauls, anyNew, () =>
            {
                int dropped = CastNet.Keep(hauls);
                c.hud.Toast(dropped > 0 ? $"ข้องเต็ม ปล่อยปลาไป {dropped} กอง" : "เก็บปลาจากแหใส่ข้องแล้ว");
                c.director?.OnFishKept(); End();
            }, () => { c.hud.Toast("ปล่อยปลาเล็กคืนบ่อ ขอให้โตไว ๆ"); End(); });
        }

        void End()
        {
            foreach (var s in caught) if (s.go) Object.Destroy(s.go);
            caught.Clear();
            SetNet(false); if (ring) ring.enabled = false;
            if (school.Count < 4) { foreach (var s in school) if (s.go) Object.Destroy(s.go); school.Clear(); respawnAt = Time.time + 1.5f; }   // ฝูงบางแล้ว: ฝูงใหม่ว่ายเข้ามาแทน
            c.Set(FishState.Free);
        }

        /// เลิกทอดกลางคัน (กลับบ้าน/วันใหม่/สลับไปใช้เบ็ด)
        public void Clear(bool killSchool)
        {
            foreach (var s in caught) if (s.go) Object.Destroy(s.go);
            caught.Clear();
            SetNet(false); if (ring) ring.enabled = false;
            if (killSchool) { foreach (var s in school) if (s.go) Object.Destroy(s.go); school.Clear(); schoolStage = null; }
        }

        // ---------- ฝูงปลา (มีเฉพาะตอนถือแหในด่านที่ทอดได้) ----------
        public void SchoolStep(float dt)
        {
            var world = c.world;
            if (!CastNet.Using) { if (school.Count > 0 || schoolStage != null) Clear(true); return; }
            if (schoolStage != world.StageId) { Clear(true); schoolStage = world.StageId; respawnAt = 0; }
            if (school.Count == 0 && Time.time >= respawnAt && (!Busy || phase == Phase.Swing)) Spawn();   // ฝูงใหม่ว่ายเข้ามาได้แม้กำลังแกว่งแหอยู่ (ไม่งั้นทอดติด ๆ กันฝูงไม่มาเลย)
            if (school.Count == 0) return;
            if (Time.time > goalAt) { schoolGoal = WaterPoint(4f, 10f); goalAt = Time.time + Random.Range(6f, 10f); }
            var to = schoolGoal - schoolPos; to.y = 0;
            if (to.magnitude > 0.1f) schoolPos += to.normalized * Mathf.Min(to.magnitude, 0.55f * dt);
            // ฝูงรวมตัวแน่นเป็นช่วง ๆ (ราว 10 วิ) · ช่วงแน่นปลาผุดน้ำให้เห็น
            float tight = 0.5f + 0.5f * Mathf.Sin(Time.time * 0.6f);
            float spread = Mathf.Lerp(2.6f, 1.1f, tight);
            // ผิวน้ำยุบยิบเหนือฝูงตลอด (ถี่ขึ้นตอนรวมฝูงแน่น) · ช่วงแน่นมีปลากระโดดพ้นน้ำให้เห็นฝูงชัด ๆ
            if (Random.value < dt * Mathf.Lerp(2f, 6f, tight)) { var s0 = school[Random.Range(0, school.Count)]; WaterFx.AddRipple(s0.pos.x, s0.pos.z, Mathf.Lerp(0.35f, 0.6f, tight)); }
            if (tight > 0.7f && Random.value < dt * 0.9f) { var sj = school[Random.Range(0, school.Count)]; if (sj.jump < 0 && sj.flee <= 0) sj.jump = 0; }
            foreach (var s in school)
            {
                Vector3 goal;
                if (s.flee > 0) { s.flee -= dt; var away = s.pos - target; away.y = 0; goal = s.pos + (away.sqrMagnitude > 0.01f ? away.normalized : s.dir) * 3f; }
                else goal = schoolPos + new Vector3(s.off.x, 0, s.off.y) * spread + new Vector3(Mathf.Sin(Time.time * 0.9f + s.phase), 0, Mathf.Cos(Time.time * 0.7f + s.phase)) * 0.5f;
                var w = goal - s.pos; w.y = 0;
                var want = w.sqrMagnitude > 1e-4f ? w.normalized : s.dir;
                s.dir = Vector3.Slerp(s.dir, want, Mathf.Min(1, dt * 3f)).normalized;
                float spd = (s.flee > 0 ? 2.6f : 0.9f) * Mathf.Clamp01(w.magnitude);
                var next = s.pos + s.dir * spd * dt;
                if (world.CanCast(next.x, next.z)) s.pos = next;
                float y = world.WaterY - Mathf.Clamp(world.DepthAt(s.pos.x, s.pos.z) - 0.1f, 0.05f, 0.1f);   // ว่ายตื้นใกล้ผิวน้ำ มองจากฝั่งเห็นเงาฝูง
                var rot = Quaternion.LookRotation(s.dir) * Quaternion.Euler(0, -90, 0);   // หัวโมเดลอยู่ +X
                if (s.jump >= 0)
                {
                    s.jump += dt / 0.55f; float jk = Mathf.Clamp01(s.jump);
                    y = world.WaterY + Mathf.Sin(jk * Mathf.PI) * 0.45f; rot = rot * Quaternion.Euler(0, 0, Mathf.Lerp(40, -40, jk));
                    if (s.jump >= 1) { s.jump = -1; WaterFx.AddRipple(s.pos.x, s.pos.z, 0.7f); Sfx.Plop(); }
                }
                s.go.transform.SetPositionAndRotation(new Vector3(s.pos.x, y, s.pos.z), rot);
            }
        }

        void Spawn()
        {
            var pool = CastNet.Pool(c.world.StageId); if (pool.Count == 0) return;
            schoolPos = WaterPoint(5f, 10f); schoolGoal = WaterPoint(4f, 10f); goalAt = Time.time + 8f;
            int n = (int)SchoolFish - Random.Range(0, 3);
            for (int i = 0; i < n; i++)
            {
                var sp = CastNet.PickSpecies(pool, c.rng);
                var go = c.MakeFish(sp, 0.7f);   // ขยายเกินจริงนิด ให้มองเห็นฝูงจากฝั่ง (แบบ PondLife) go.name = "NetSchool_" + sp.id;
                foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var off = Random.insideUnitCircle;
                school.Add(new Swimmer { go = go, sp = sp, kg = CastNet.RollKg(sp, c.rng), off = off, phase = Random.value * 6f, pos = schoolPos + new Vector3(off.x, 0, off.y), dir = Vector3.forward });
            }
        }

        // จุดในน้ำห่างตัวผู้เล่น minD..maxD ที่รอบ ๆ เป็นน้ำ (แหกางเต็มวงไม่ติดตลิ่ง)
        Vector3 WaterPoint(float minD, float maxD)
        {
            var world = c.world; var p0 = c.player.Pos;
            for (int i = 0; i < 40; i++)
            {
                float a = Random.value * Mathf.PI * 2, d = Random.Range(minD, maxD);
                float x = p0.x + Mathf.Cos(a) * d, z = p0.z + Mathf.Sin(a) * d;
                if (world.CanCast(x, z) && world.CanCast(x + 1.8f, z) && world.CanCast(x - 1.8f, z) && world.CanCast(x, z + 1.8f) && world.CanCast(x, z - 1.8f)) return new Vector3(x, world.WaterY, z);
            }
            return school.Count > 0 ? schoolPos : new Vector3(p0.x + c.player.Forward.x * 6, world.WaterY, p0.z + c.player.Forward.z * 6);
        }

        // ---------- วาดแห ----------
        LineRenderer NewLine(string name, Color col, float width)
        {
            var lr = new GameObject(name).AddComponent<LineRenderer>();
            lr.sharedMaterial = Mat(col); lr.widthMultiplier = width; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.useWorldSpace = true;
            lr.numCapVertices = 0; lr.enabled = false; return lr;
        }
        Material Mat(Color col) { var m = new Material(c.line.sharedMaterial); m.SetColor("_BaseColor", col); return m; }

        // แห: ซี่โค้งตามโดม + ตาข่ายวนรอบ + ขอบ + ลูกตะกั่ว + เชือกจากมือถึงยอดแห + ละอองน้ำกระเซ็นตอนตกน้ำ
        LineRenderer rope, spray;
        Transform[] weights;
        float sprayAt = -9;
        const int Spokes = 22, Seg = 8, Turns = 7, PerTurn = 30, Weights = 18;

        void Ensure()
        {
            if (ring != null) return;
            var netCol = new Color(0.9f, 0.86f, 0.74f);
            ring = NewLine("NetAimRing", Color.white, 0.07f); ring.loop = true;
            spokes = NewLine("NetSpokes", netCol, 0.014f);
            mesh = NewLine("NetMesh", netCol, 0.012f);
            rim = NewLine("NetRim", new Color(0.32f, 0.3f, 0.27f), 0.045f); rim.loop = true;
            rope = NewLine("NetRope", new Color(0.62f, 0.5f, 0.32f), 0.025f);
            spray = NewLine("NetSpray", new Color(0.95f, 0.98f, 1f), 0.05f);
            // ลูกตะกั่ว: ใช้เมชทรงกลมจากชิ้นส่วนทุ่นในฉาก (WebGL ตัดโมดูล Physics → ไม่ใช้ CreatePrimitive)
            Mesh sphere = null;
            foreach (var mf in c.bobber.GetComponentsInChildren<MeshFilter>(true)) if (mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("Sphere")) { sphere = mf.sharedMesh; break; }
            weights = new Transform[sphere != null ? Weights : 0];
            var lead = Mat(new Color(0.22f, 0.21f, 0.2f));
            for (int i = 0; i < weights.Length; i++)
            {
                var g = new GameObject("NetLead"); g.AddComponent<MeshFilter>().sharedMesh = sphere;
                var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = lead; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                g.transform.localScale = Vector3.one * 0.09f; g.SetActive(false); weights[i] = g.transform;
            }
        }

        void DrawRing(LineRenderer lr, Vector3 c0, float r, Color col)
        {
            Ensure();
            const int n = 40; lr.positionCount = n;
            for (int i = 0; i < n; i++) { float a = i / (float)n * Mathf.PI * 2; lr.SetPosition(i, c0 + new Vector3(Mathf.Cos(a) * r, 0.02f, Mathf.Sin(a) * r)); }
            lr.sharedMaterial.SetColor("_BaseColor", col);
        }

        void SetNet(bool on)
        {
            if (spokes == null) return;
            spokes.enabled = mesh.enabled = rim.enabled = rope.enabled = on;
            if (!on) spray.enabled = false;
            foreach (var w in weights) w.gameObject.SetActive(on);
        }

        /// จุดบนผิวโดม: k = 0 ยอด → 1 ขอบ · a = มุมรอบวง · ขอบพลิ้วนิด ๆ ให้ดูเป็นผ้า
        Vector3 Dome(Vector3 c0, float r, float h, float k, float a)
        {
            float wob = 1 + 0.05f * Mathf.Sin(a * 5 + Time.time * 3) * k;
            float rad = r * Mathf.Sin(k * Mathf.PI * 0.5f) * wob, y = h * Mathf.Cos(k * Mathf.PI * 0.5f);
            return c0 + new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad);
        }

        /// c0 = กลางขอบตะกั่ว · r = รัศมีขอบ · h = ความสูงยอดโดมเหนือขอบ (ติดลบ = ยอดอยู่ต่ำกว่าขอบ)
        void DrawNet(Vector3 c0, float r, float h)
        {
            Ensure(); SetNet(true);
            // ซี่: ยอด → ตามโค้งลงขอบ → ย้อนกลับขึ้นยอด → ซี่ถัดไป (เส้นเดียวต่อเนื่อง)
            spokes.positionCount = Spokes * Seg * 2;
            int n = 0;
            for (int i = 0; i < Spokes; i++)
            {
                float a = i / (float)Spokes * Mathf.PI * 2;
                for (int j = 0; j < Seg; j++) spokes.SetPosition(n++, Dome(c0, r, h, j / (float)(Seg - 1), a));
                for (int j = Seg - 1; j >= 0; j--) spokes.SetPosition(n++, Dome(c0, r, h, j / (float)(Seg - 1), a));
            }
            // ตาข่าย: เส้นวนรอบจากยอดลงขอบ
            mesh.positionCount = Turns * PerTurn;
            for (int i = 0; i < Turns * PerTurn; i++)
            {
                float k = (i + 1) / (float)(Turns * PerTurn), a = i / (float)PerTurn * Mathf.PI * 2;
                mesh.SetPosition(i, Dome(c0, r, h, Mathf.Sqrt(k), a));
            }
            rim.positionCount = 36;
            for (int i = 0; i < 36; i++) rim.SetPosition(i, Dome(c0, r, h, 1, i / 36f * Mathf.PI * 2));
            for (int i = 0; i < weights.Length; i++) weights[i].position = Dome(c0, r, h, 1, i / (float)weights.Length * Mathf.PI * 2);
            // เชือกจากมือถึงยอดแห (หย่อนนิด ๆ)
            var top = c0 + Vector3.up * h; var hand = Hand;
            rope.positionCount = 12;
            for (int i = 0; i < 12; i++) { float k = i / 11f; rope.SetPosition(i, Vector3.Lerp(hand, top, k) + Vector3.down * Mathf.Sin(k * Mathf.PI) * 0.35f); }
            // ละอองน้ำ: ซี่ละอองพุ่งขึ้นรอบขอบแล้วยุบลง (0.6 วิหลังแหตกน้ำ) · เส้นที่ลากตามน้ำระหว่างซี่ = ฟองขาวรอบวง
            float st = (Time.time - sprayAt) / 0.6f;
            spray.enabled = st >= 0 && st < 1;
            if (spray.enabled)
            {
                const int sn = 28; spray.positionCount = sn * 3;
                float hgt = Mathf.Sin(st * Mathf.PI) * 0.7f, wy = c.world.WaterY + 0.02f;
                for (int i = 0; i < sn; i++)
                {
                    float a = i / (float)sn * Mathf.PI * 2, rr = r * (1.02f + 0.12f * st);
                    var b = new Vector3(target.x + Mathf.Cos(a) * rr, wy, target.z + Mathf.Sin(a) * rr);
                    float tall = hgt * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(i * 1.7f)));
                    spray.SetPosition(i * 3, b); spray.SetPosition(i * 3 + 1, b + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.25f * st + Vector3.up * tall); spray.SetPosition(i * 3 + 2, b);
                }
                spray.widthMultiplier = 0.06f * (1 - st);
            }
        }
    }
}
