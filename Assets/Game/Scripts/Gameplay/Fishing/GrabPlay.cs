using System.Collections.Generic;
using UnityEngine;

namespace CozyFishing
{
    // งมปลาด้วยมือเปล่า: โพรงริมตลิ่งรอบบ่อหลังบ้าน (วันละ 6 โพรง มีต้นอ้อปักไว้ ผิวน้ำผุดฟองเป็นระยะ)
    //   เดินไปยืนริมตลิ่งหน้าโพรง → กด = ลงงม · กล้องดำลงใต้น้ำมองเข้าโพรง (ใช้บรรยากาศใต้น้ำของ UnderwaterView)
    //   กดค้าง = ยื่นมือเข้าโพรง → สัมผัสบอกเป็นคำ (เดาเอาว่าเป็นอะไร) → กด = จับ / Q, คลิกขวา, ปุ่ม "ชักมือ" = ชักมือกลับ
    //   ปลา: บีบให้เข็มอยู่ในช่องเขียวจนครบ (บีบแรงไป = โดนเงี่ยง/ปลาดิ้นหลุด · หลวมไป = ลื่นหลุด)
    sealed class GrabPlay
    {
        public enum Phase { Dive, Reach, Feel, Grip, Done }
        class Hole { public Vector3 water, bank, cam, mouth, normal, inward, stand, side; public bool used; public GameObject marker; public float rippleAt; }

        readonly FishingController c;
        readonly List<Hole> holes = new List<Hole>();
        int holesDay = -1; string holesStage;
        Hole cur; HandGrab.Find find;
        public Phase phase; float pt;
        public float Reach, Grip, InZone, Shake;
        GameObject scene;   // ปากโพรง รากไม้ ตัวที่อยู่ในโพรง (ใต้น้ำ)
        Material baseMat;
        const float GripNeed = 2.4f, FeelTime = 7f, GripTime = 8f;

        public GrabPlay(FishingController c) { this.c = c; }

        public bool Busy => c.State == FishState.Grabbing;
        public string Feel => find?.feel;
        public bool IsFish => find != null && find.kind == HandGrab.Kind.Fish;
        public float GripProgress => InZone / GripNeed;

        // ---------- โพรงประจำวัน ----------
        Hole near;
        public bool NearHole => near != null;

        public void WorldStep(float dt)
        {
            var world = c.world;
            bool ok = HandGrab.StageOk(world.StageId) && !c.Paused;
            if (!ok) { near = null; if (holes.Count > 0 && world.StageId != holesStage) ClearHoles(); return; }
            if (holesDay != Story.S.day || holesStage != world.StageId) SpawnHoles();
            near = null; float best = 1.9f;
            foreach (var h in holes)
            {
                if (h.used) continue;
                float d = Vector2.Distance(new Vector2(c.player.Pos.x, c.player.Pos.z), new Vector2(h.bank.x, h.bank.z));
                if (d < best) { best = d; near = h; }
                if (Time.time > h.rippleAt) { h.rippleAt = Time.time + Random.Range(1.6f, 3.2f); WaterFx.AddRipple(h.water.x, h.water.z, 0.3f); }   // ฟองผุดหน้าโพรง
            }
            if (Raft.Aboard || CastNet.Using) near = null;
            if (FishingController.Bot && Time.frameCount % 60 == 0) CastProbe.Note($"grab holes={holes.Count} near={(near != null)} st={c.State} t={c.t:0.0} left={HandGrab.Left} p={c.player.Pos}");
            if (FishingController.Bot && GameUrl.Value.Contains("grabtest") && c.State == FishState.Free && near == null && c.t > 1f)   // ทดสอบ: พาไปยืนหน้าโพรงที่ยังไม่งม
                foreach (var h in holes) if (!h.used) { c.player.Pos = new Vector3(h.bank.x, world.StandY(h.bank.x, h.bank.z), h.bank.z); break; }
        }

        void SpawnHoles()
        {
            ClearHoles();
            holesDay = Story.S.day; holesStage = c.world.StageId;
            var world = c.world; var ctr = new Vector3(PaddyWorld.Pond.x, 0, PaddyWorld.Pond.y);
            var rng = new System.Random(Story.S.day * 7919 + 13);
            var order = new List<int>(); for (int i = 0; i < 28; i++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (order[i], order[j]) = (order[j], order[i]); }
            foreach (int i in order)
            {
                if (holes.Count >= HandGrab.PerDay) break;
                float a = i / 28f * Mathf.PI * 2; var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                // ขอบน้ำตามแนวรัศมี
                float rEdge = -1;
                for (float r = 4; r < 20; r += 0.2f) { var p = ctr + dir * r; if (!world.CanCast(p.x, p.z)) { rEdge = r; break; } }
                if (rEdge < 0) continue;
                Vector3 At(float r) => ctr + dir * r;
                var bank = At(rEdge + 0.7f);
                if (!world.Walkable(bank.x, bank.z) || world.OnSolid(bank.x, bank.z) || world.OnSolid(At(rEdge - 1).x, At(rEdge - 1).z)) continue;
                float rHole = -1, rCam = -1;
                for (float r = rEdge - 0.2f; r > rEdge - 6; r -= 0.1f) { var p = At(r); float dep = world.DepthAt(p.x, p.z); if (rHole < 0 && dep >= 0.62f) rHole = r; if (dep >= 0.8f && rHole > 0 && r < rHole - 0.6f) { rCam = r; break; } }   // โพรงลึกพอให้คุกเข่าไหล่จมน้ำ   // กล้องห่างปากโพรงพอเห็นทั้งโพรงและมือ
                if (rHole < 0 || rCam < 0) continue;
                var water = At(rHole);
                bool far = true; foreach (var h0 in holes) if ((h0.water - water).sqrMagnitude < 9f) far = false;
                if (!far) continue;
                var h = new Hole { water = water, bank = bank, inward = -dir };
                // ตัวละครลงไปยืนในน้ำหน้าโพรง (ห่างปากโพรง ~0.7 ม.) · กล้องใต้น้ำมองจากด้านข้าง เห็นแขนสองข้างเอื้อมเข้าโพรง
                h.stand = At(rHole - 0.62f); h.stand.y = world.GroundH(h.stand.x, h.stand.z);
                var side = Vector3.Cross(Vector3.up, dir);
                h.cam = At(rCam); h.cam.y = world.WaterY - 0.3f;
                foreach (float sgn in new[] { 1f, -1f })
                {
                    var sc = At(rHole - 1.1f) + side * sgn * 1.25f;
                    if (world.CanCast(sc.x, sc.z) && world.DepthAt(sc.x, sc.z) > 0.45f) { h.cam = new Vector3(sc.x, world.WaterY - 0.22f, sc.z); h.side = side * sgn; break; }
                }
                float gy = world.GroundH(water.x, water.z);
                var up = At(rHole + 0.3f); float slope = world.GroundH(up.x, up.z) - gy;   // ตลิ่งลาดขึ้นทางฝั่ง
                h.normal = (Vector3.up * 0.3f + h.inward * Mathf.Max(0.4f, slope / 0.3f)).normalized;
                h.mouth = new Vector3(water.x, gy + 0.05f, water.z);
                h.marker = Reeds(water);
                h.water.y = world.WaterY;
                holes.Add(h);
            }
        }

        void ClearHoles() { foreach (var h in holes) if (h.marker) Object.Destroy(h.marker); holes.Clear(); }

        Material Mat(Color col) { baseMat ??= Resources.Load<Material>("Rod/Cork"); var m = new Material(baseMat); m.SetColor("_BaseColor", col); return m; }
        Mesh Cyl => c.aim.GetComponentInChildren<MeshFilter>().sharedMesh;
        Mesh sphere;
        Mesh Sphere { get { if (sphere == null) foreach (var mf in c.bobber.GetComponentsInChildren<MeshFilter>(true)) if (mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("Sphere")) { sphere = mf.sharedMesh; break; } return sphere ?? Cyl; } }

        GameObject Part(Transform parent, Mesh mesh, Material m, Vector3 pos, Vector3 scale, Quaternion rot)
        {
            var g = new GameObject("GrabPart"); g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localRotation = rot; g.transform.localScale = scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        // ต้นอ้อสามต้นหน้าโพรง: บอกตำแหน่งโพรงจากฝั่ง
        GameObject Reeds(Vector3 at)
        {
            var root = new GameObject("GrabHoleReeds"); root.transform.position = new Vector3(at.x, c.world.WaterY, at.z);
            var m = Mat(new Color(0.36f, 0.5f, 0.22f)); var tip = Mat(new Color(0.48f, 0.36f, 0.2f));
            for (int i = 0; i < 3; i++)
            {
                var off = new Vector3(Mathf.Cos(i * 2.1f) * 0.18f, 0, Mathf.Sin(i * 2.1f) * 0.18f);
                var tilt = Quaternion.Euler(Mathf.Sin(i * 1.7f) * 10, 0, Mathf.Cos(i * 1.3f) * 10);
                Part(root.transform, Cyl, m, off + Vector3.up * 0.35f, new Vector3(0.025f, 0.38f, 0.025f), tilt);
                Part(root.transform, Cyl, tip, off + tilt * Vector3.up * 0.74f, new Vector3(0.05f, 0.07f, 0.05f), tilt);   // ดอกอ้อ
            }
            return root;
        }

        // ---------- ลงงม ----------
        public bool Begin()
        {
            if (near == null) return false;
            if (HandGrab.Left <= 0) { c.hud.Toast($"วันนี้งมครบ {HandGrab.PerDay} โพรงแล้ว พรุ่งนี้โพรงใหม่จะมีปลาเข้ามาอีก"); return false; }
            if (HandGrab.TooHurt) { c.hud.Toast("มือเจ็บเกินไปแล้ว วันนี้พักก่อน พรุ่งนี้แผลก็หาย"); return false; }
            if (FishingController.Bot) CastProbe.Note("grab begin");
            cur = near; find = HandGrab.Roll(c.rng); HandGrab.Use();
            phase = Phase.Dive; pt = 0; Reach = 0; Grip = 0.3f; InZone = 0; Shake = 0;
            var to = cur.water - c.player.Pos; c.player.Yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            UnderwaterView.GrabMode = true; UnderwaterView.GrabCam = cur.cam; UnderwaterView.GrabLook = Vector3.Lerp(cur.mouth, cur.stand, 0.3f) + Vector3.up * 0.12f;
            // มุมบุคคลที่หนึ่ง: ซ่อนตัวละครกับคันเบ็ด · แขนแยก (FPArms) ยื่นเข้าโพรงแทน
            if (arms == null && !armsFailed)
            {
                try { arms = FPArms.Create(c.player); }
                catch (System.Exception e) { armsFailed = true; CastProbe.Note("fparms create fail: " + e.GetType().Name + " " + e.Message); }
            }
            ShowBody(false);
            if (FishingController.Bot) CastProbe.Note("grab body hidden");
            BuildScene();
            if (FishingController.Bot) CastProbe.Note("grab scene ok");
            Sfx.Splash(); WaterFx.AddRipple(cur.water.x, cur.water.z, 1f);
            c.Set(FishState.Grabbing);
            return true;
        }

        void BuildScene()
        {
            scene = new GameObject("GrabScene"); var t = scene.transform;
            t.position = cur.mouth; t.rotation = Quaternion.FromToRotation(Vector3.up, cur.normal);   // แกน y ชี้ออกจากผนังตลิ่ง
            var dark = Mat(new Color(0.06f, 0.05f, 0.03f)); var mud = Mat(new Color(0.24f, 0.18f, 0.1f)); var root = Mat(new Color(0.3f, 0.22f, 0.13f));
            Part(t, Cyl, dark, Vector3.zero, new Vector3(0.42f, 0.02f, 0.3f), Quaternion.identity);                 // ปากโพรงมืด ๆ
            Part(t, Cyl, mud, new Vector3(0, -0.01f, 0), new Vector3(0.58f, 0.015f, 0.42f), Quaternion.identity);     // ขอบโคลนรอบปากโพรง
            for (int i = 0; i < 6; i++)   // รากไม้ห้อยรอบปากโพรง
            {
                float a = i * 1.05f + 0.3f; var p = new Vector3(Mathf.Cos(a) * 0.34f, 0.06f, Mathf.Sin(a) * 0.26f);
                Part(t, Cyl, root, p, new Vector3(0.018f, 0.18f + (i % 3) * 0.05f, 0.018f), Quaternion.Euler(Mathf.Sin(a) * 60, 0, Mathf.Cos(a) * 60 + 20));
            }
            var thing = new GameObject("GrabThing"); thing.transform.SetParent(t, false); thing.transform.localPosition = new Vector3(0, 0.05f, 0);
            var tt = thing.transform;
            switch (find.kind)
            {
                case HandGrab.Kind.Fish:
                {
                    var fish = c.MakeFish(find.sp, Mathf.Clamp(0.15f + find.cm / 200f, 0.18f, 0.34f)); fish.transform.SetParent(tt, false);   // ตัวเล็กพอดีปากโพรง โผล่แค่หัวครึ่งตัว
                    fish.transform.localPosition = new Vector3(0.08f, 0.03f, 0); fish.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    foreach (var r in fish.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    break;
                }
                case HandGrab.Kind.Crab:
                {
                    var shell = Mat(new Color(0.42f, 0.3f, 0.17f)); var leg = Mat(new Color(0.32f, 0.22f, 0.12f));
                    Part(tt, Sphere, shell, Vector3.zero, new Vector3(0.16f, 0.06f, 0.12f), Quaternion.identity);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        for (int i = 0; i < 3; i++) Part(tt, Cyl, leg, new Vector3(s * 0.11f, 0, -0.04f + i * 0.04f), new Vector3(0.012f, 0.06f, 0.012f), Quaternion.Euler(0, 0, s * 70));
                        Part(tt, Sphere, shell, new Vector3(s * 0.1f, 0.01f, 0.1f), new Vector3(0.06f, 0.04f, 0.05f), Quaternion.identity);   // ก้าม
                    }
                    break;
                }
                case HandGrab.Kind.Clam:
                    Part(tt, Sphere, Mat(new Color(0.22f, 0.21f, 0.16f)), Vector3.zero, new Vector3(0.16f, 0.05f, 0.1f), Quaternion.Euler(0, 30, 0)); break;
                case HandGrab.Kind.Thorn:
                {
                    var br = Mat(new Color(0.25f, 0.2f, 0.12f));
                    for (int i = 0; i < 3; i++) Part(tt, Cyl, br, new Vector3(0, 0.02f * i, 0), new Vector3(0.02f, 0.16f, 0.02f), Quaternion.Euler(90, i * 55, 0));
                    break;
                }
                case HandGrab.Kind.Glass:
                    Part(tt, Cyl, Mat(new Color(0.35f, 0.5f, 0.38f)), Vector3.zero, new Vector3(0.12f, 0.008f, 0.07f), Quaternion.Euler(10, 40, 0)); break;
                case HandGrab.Kind.Relic:
                    Part(tt, Cyl, Mat(new Color(0.55f, 0.32f, 0.18f)), Vector3.zero, new Vector3(0.05f, 0.015f, 0.07f), Quaternion.Euler(70, 0, 0)); break;
            }
            thing.SetActive(false);   // ยังมองไม่เห็น ต้องคลำก่อน
        }

        /// ปุ่มหลัก (จับ)
        public void Down()
        {
            if (phase == Phase.Feel) Take();
        }


        /// มือจริงของตัวละคร: ระยะยื่น 0 = มือแนบอก · 1 = ในปากโพรง · ปลาดิ้น = มือสั่นตาม
        bool armsFailed;
        FPArms arms; readonly List<Renderer> hidden = new List<Renderer>(); GameObject rodReal; bool bodyHidden;
        /// ซ่อน/คืนตัวละคร: จำเฉพาะชิ้นที่เปิดอยู่ตอนซ่อน (คันเบ็ดเหลี่ยมเดิมที่ HandIK ปิดไว้ต้องปิดต่อ)
        void ShowBody(bool on)
        {
            if (on == !bodyHidden) return;
            bodyHidden = !on;
            if (!on) { hidden.Clear(); foreach (var r in c.player.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; hidden.Add(r); } }
            else { foreach (var r in hidden) if (r) r.enabled = true; hidden.Clear(); }
            rodReal ??= GameObject.Find("RodReal");
            if (rodReal) rodReal.SetActive(on);
            if (arms != null) arms.Active = !on;
        }

        /// กล้องใต้น้ำหน้าปากโพรง + แขนสองข้างโผล่จากขอบล่างจอ เอื้อมเข้าหาโพรงตามระยะที่ยื่น · ปลาดิ้น = มือสั่น
        void Hands(float dt)
        {
            if (cur == null) return;
            var camP = cur.mouth + cur.inward * 0.95f + Vector3.up * 0.3f;   // inward ชี้ไปกลางบ่อ → กล้องอยู่ฝั่งน้ำ มองเข้าหาตลิ่ง
            camP.y = Mathf.Min(camP.y, c.world.WaterY - 0.07f);
            var look = cur.mouth + cur.normal * 0.05f;
            UnderwaterView.GrabCam = camP; UnderwaterView.GrabLook = look;
            var cam = Camera.main;
            if (cam != null && cam.nearClipPlane > 0.03f) { savedNear = cam.nearClipPlane; cam.nearClipPlane = 0.03f; }
            if (arms == null) return;
            var fw = (look - camP).normalized; var right = Vector3.Cross(Vector3.up, fw).normalized;
            float reach = phase == Phase.Dive ? 0 : phase == Phase.Reach ? Reach : 1;
            var inside = cur.mouth + cur.normal * 0.06f - cur.inward * 0.1f * reach;
            var start = camP + fw * 0.42f - Vector3.up * 0.26f;
            float sh = Mathf.Abs(Shake);
            var jit = new Vector3(Mathf.Sin(Time.time * 29), Mathf.Sin(Time.time * 23 + 1), Mathf.Cos(Time.time * 31)) * 0.03f * sh;
            var hl = Vector3.Lerp(start - right * 0.13f, inside - right * 0.07f, reach) + jit;
            var hr = Vector3.Lerp(start + right * 0.13f, inside + right * 0.07f, reach) - jit;
            try { arms.Pose(camP, fw, hl, hr, phase == Phase.Grip ? -1f : phase == Phase.Feel ? 0.2f : 1f, RollDeg); }
            catch (System.Exception e) { CastProbe.Note("fparms pose fail: " + e.GetType().Name + " " + e.Message); arms.Active = false; arms = null; armsFailed = true; }
        }
        public static float RollDeg = 180;   // หมุนข้อมือให้ฝ่ามือคว่ำลง (โมเดล T-pose ฝ่ามือหันลงพื้น หลังยกแขนไปหน้าจะหงาย)
        float savedNear = -1;

        public void Step(float dt, bool held)
        {
            pt += dt;
            Hands(dt);
            var thing = scene != null ? scene.transform.Find("GrabThing") : null;
            switch (phase)
            {
                case Phase.Dive:
                    if (pt > 0.7f) { phase = Phase.Reach; pt = 0; }
                    break;
                case Phase.Reach:
                    Reach = Mathf.Clamp01(Reach + (held ? dt / 1.3f : -dt * 0.6f));
                    if (held && Random.value < dt * 4f) UnderwaterView.Instance?.Silt(cur.mouth + cur.normal * 0.2f, 6);
                    if (FishingController.Bot) Reach = Mathf.Clamp01(Reach + dt / 1.3f);
                    if (GameInput.PullPressed) { c.hud.Toast("ขึ้นจากน้ำ ไว้ค่อยงมใหม่"); cur.used = false; Finish(false); break; }
                    if (Reach >= 1) { phase = Phase.Feel; pt = 0; Sfx.Nibble(); if (thing) thing.gameObject.SetActive(true); UnderwaterView.Instance?.Silt(cur.mouth, 18); }
                    break;
                case Phase.Feel:
                    Shake = find.kind == HandGrab.Kind.Fish || find.kind == HandGrab.Kind.Crab ? Mathf.Sin(Time.time * 30) * 0.5f : 0;
                    if (thing) thing.localPosition = new Vector3(Mathf.Sin(Time.time * 7) * 0.02f * Mathf.Abs(Shake), 0.05f, 0);
                    if (GameInput.PullPressed) { PullBack(); break; }
                    if (FishingController.Bot && pt > 1.2f) { if (find.kind == HandGrab.Kind.Thorn || find.kind == HandGrab.Kind.Glass || find.kind == HandGrab.Kind.Empty) PullBack(); else Take(); break; }
                    if (pt > FeelTime) { if (find.kind == HandGrab.Kind.Fish || find.kind == HandGrab.Kind.Crab) { c.hud.Toast($"มันมุดหนีเข้าโพรงลึกไปแล้ว…"); Finish(false); } else PullBack(); }
                    break;
                case Phase.Grip:
                {
                    // ปลาดิ้นเป็นพัก ๆ ดันเข็มขึ้นลง · กดค้าง = บีบแน่นขึ้น ปล่อย = คลายมือ
                    float thrash = (Mathf.PerlinNoise(Time.time * 1.7f, 3.1f) - 0.5f) * 1.6f + (Mathf.Sin(Time.time * 9) > 0.92f ? 0.9f : 0);
                    bool squeeze = held || (FishingController.Bot && Grip < (HandGrab.GripLow + HandGrab.GripHigh) * 0.5f);
                    Grip += (squeeze ? 0.9f : -0.75f) * dt + thrash * dt * (find.sp.id == "duk_na" ? 0.9f : 0.6f);
                    Grip = Mathf.Clamp(Grip, 0, 1);
                    Shake = thrash;
                    if (Grip >= HandGrab.GripLow && Grip <= HandGrab.GripHigh) InZone += dt;
                    if (thing) thing.localPosition = new Vector3(Mathf.Sin(Time.time * 23) * 0.03f, 0.05f + Mathf.Sin(Time.time * 17) * 0.02f, 0);
                    if (Random.value < dt * 3f) UnderwaterView.Instance?.Silt(cur.mouth + cur.normal * 0.1f, 8);
                    if (InZone >= GripNeed) { Caught(); break; }
                    if (Grip >= 0.98f)
                    {
                        if (find.sp.id == "duk_na") { HandGrab.Hurt(); c.hud.Toast("โอ๊ย! บีบแรงไปโดนเงี่ยงปักมือ ปลาดุกดิ้นหลุดไป"); }
                        else c.hud.Toast("บีบแรงไป ปลาดิ้นหลุดมือไป");
                        Sfx.Snap(); Finish(false); break;
                    }
                    if (Grip <= 0.02f && pt > 0.8f) { c.hud.Toast("มือหลวมไป ปลาลื่นหลุดไปแล้ว"); Finish(false); break; }
                    if (pt > GripTime) { c.hud.Toast("ปลาดิ้นจนหลุดมือ…"); Finish(false); }
                    break;
                }
            }
        }

        void PullBack()
        {
            switch (find.kind)
            {
                case HandGrab.Kind.Thorn: case HandGrab.Kind.Glass: c.hud.Toast("ดีที่ชักมือทัน ข้างในมีของคม ๆ"); break;
                case HandGrab.Kind.Empty: c.hud.Toast("โพรงนี้ว่างเปล่า"); break;
                case HandGrab.Kind.Fish: case HandGrab.Kind.Crab: c.hud.Toast($"ชักมือกลับ… {find.sp.name}มุดหนีเข้าโพรงลึกไปแล้ว"); break;
                default: c.hud.Toast("ชักมือกลับ ปล่อยไว้อย่างนั้นก่อน"); break;
            }
            Finish(false);
        }

        void Take()
        {
            switch (find.kind)
            {
                case HandGrab.Kind.Fish: phase = Phase.Grip; pt = 0; Grip = 0.35f; InZone = 0; Sfx.Strike(); c.hud.Toast("จับได้แล้ว! บีบให้เข็มอยู่ในช่องเขียว อย่าบีบแรงจนโดนเงี่ยง"); break;
                case HandGrab.Kind.Crab:
                    if (c.rng.NextDouble() < 0.35) { HandGrab.Hurt(); c.hud.Toast("โอ๊ย! โดนก้ามปูหนีบ… แต่จับได้"); }
                    Caught(); break;
                case HandGrab.Kind.Clam: Caught(); break;
                case HandGrab.Kind.Thorn: HandGrab.Hurt(); Sfx.Snap(); c.hud.Toast("โอ๊ย! กิ่งไผ่หนามทิ่มมือ"); Finish(false); break;
                case HandGrab.Kind.Glass: HandGrab.Hurt(); Sfx.Snap(); c.hud.Toast("โอ๊ย! เศษแก้วบาดมือ ระวังหน่อย"); Finish(false); break;
                case HandGrab.Kind.Relic:
                {
                    var clue = Mystery.OnBottle();
                    if (clue != null) { Sfx.Coin(); c.hud.ShowClue(clue); }
                    else { Story.S.amulets++; Story.Save(); Sfx.Coin(); c.hud.Toast($"ได้ตะกรุดโบราณจมโคลน! ช่วยกันผีได้ (มี {Story.S.amulets} ชิ้น)"); }
                    Finish(true); break;
                }
                default: c.hud.Toast("มีแต่โคลน…"); Finish(false); break;
            }
        }

        void Caught()
        {
            phase = Phase.Done;
            var f = find; bool isNew = FishDex.Record(f.sp.id, f.kg);
            int lv = Skills.AddXp(Skills.XpForCatch(f.sp.stars, false));
            if (lv > 0) c.hud.Toast($"ระดับชาวประมง {Skills.FisherLevel}! ได้แต้มทักษะ (ดูที่หน้าเช้า ปุ่มทักษะ)");
            string note = null;
            if (f.kind == HandGrab.Kind.Clam && c.rng.NextDouble() < HandGrab.PearlChance) { Story.S.money += HandGrab.PearlBaht; Story.Save(); note = $"แกะดูข้างใน เจอไข่มุกน้ำจืด! ขายได้ {Story.Baht(HandGrab.PearlBaht)}"; }
            Sfx.Splash();
            var haul = new List<CastNet.Haul> { new CastNet.Haul { sp = f.sp, count = 1, kg = f.kg } };
            c.hud.NetResult(haul, isNew, () =>
            {
                if (Story.BasketFull) c.hud.Toast("ข้องเต็มแล้ว ปล่อยคืนน้ำไปก่อน");
                else { Story.AddFish(new BasketFish { id = f.sp.id, kg = f.kg, cm = f.cm }); c.hud.Toast($"เก็บ{f.sp.name}ใส่ข้องแล้ว"); c.director?.OnFishKept(); }
                Finish(true);
            }, () => { c.hud.Toast("ปล่อยคืนน้ำ ขอให้โตไว ๆ"); Finish(true); }, $"งมได้{f.sp.name}!", note ?? "ของที่งมได้ใส่ข้องทีละตัว (ตัวละช่อง)");
        }

        void Release()
        {
            if (savedNear > 0 && Camera.main != null) { Camera.main.nearClipPlane = savedNear; savedNear = -1; }
            ShowBody(true);
        }

        void Finish(bool got)
        {
            Release();
            if (cur != null) { cur.used = true; Dim(cur); }
            if (scene) Object.Destroy(scene);
            scene = null; find = null;
            UnderwaterView.GrabMode = false;
            phase = Phase.Done;
            if (c.State == FishState.Grabbing) c.Set(FishState.Free);
        }

        void Dim(Hole h)
        {
            if (h.marker == null) return;
            var m = Mat(new Color(0.42f, 0.36f, 0.24f));
            foreach (var r in h.marker.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = m;   // งมแล้ว: ต้นอ้อหักโค้ง สีซีด
            h.marker.transform.localScale = new Vector3(1, 0.7f, 1);
        }

        /// เลิกกลางคัน (กลับบ้าน/วันใหม่)
        public void Clear()
        {
            if (c.State == FishState.Grabbing || (arms != null && arms.Active)) Release();
            if (scene) Object.Destroy(scene);
            scene = null; find = null; UnderwaterView.GrabMode = false;
            if (c.State == FishState.Grabbing) c.Set(FishState.Free);
        }
    }
}
