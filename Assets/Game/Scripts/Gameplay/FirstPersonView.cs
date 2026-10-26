using System.Collections;
using UnityEngine;

namespace CozyFishing
{
    // มุมบุคคลที่หนึ่ง (แบบเกมตกปลาซิม): เล็ง/เหวี่ยง/ตีเบ็ด/สู้ปลา กล้องอยู่ที่ตาตัวละคร เห็นมือถือคันเบ็ดกับสายพุ่งไปที่ทุ่น
    // ได้ปลา → ปลากระโดดพ้นน้ำสโลโมชั่น · แฟลชหยุดภาพเป็นรูปโพลารอยด์ · ปลาห้อยแกว่งที่ปลายสายกลางจอ (การ์ดอยู่ข้าง)
    //   เก็บ → ตัดไปแคร่ไม้ไผ่ ปลาทุกตัวของวันนี้เรียงบนใบตอง ตัวใหม่ดิ้นตึก ๆ · ปล่อย → ปลาตกลงน้ำดังจ๋อม
    // รอทุ่นจมใช้ UnderwaterView · เดินเล่นใช้กล้องปกติ · ปิด/เปิดได้ที่ปุ่มรูปตา (จำไว้ใน PlayerPrefs)
    public class FirstPersonView : MonoBehaviour
    {
        const string PrefKey = "abot.fp";
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(PrefKey, 1) == 1;
            set { PlayerPrefs.SetInt(PrefKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }
        public static bool Active { get; private set; }

        FishingController fishing; CameraRig rig; PlayerController player; Hud hud;
        Transform headBone; HandIK ik; Camera cam;
        bool active; Vector3 camPos; Quaternion camRot; float savedNear, savedFov, showT;
        Vector3 showFrom; float showK, showLen;
        int phase; float phaseT; Vector3 blendPos; Quaternion blendRot; bool slowMo;
        ParticleSystem splash, drips;

        public void Init(FishingController f, CameraRig r)
        {
            fishing = f; rig = r; player = f.player; hud = f.hud;
            cam = r.GetComponent<Camera>();
            if (player.animator != null)
            {
                headBone = player.animator.GetBoneTransform(HumanBodyBones.Head);
                ik = player.animator.GetComponent<HandIK>() ?? player.animator.gameObject.AddComponent<HandIK>();
                ik.rodTip = player.rodTip;
            }
            FishingController.CatchOutro = (go, kept) => { StartCoroutine(kept ? Platter(go) : DropBack(go)); return true; };
        }

        void OnDestroy() { if (slowMo) Time.timeScale = 1; FishingController.CatchOutro = null; }

        bool Want()
        {
            if (!Enabled || headBone == null) return false;
            var s = fishing.State;
            // ง้าง (Charging) ใช้กล้องหลังตัวละคร: ท่าง้างยกสองแขนบังหน้า · ปล่อยเหวี่ยงแล้วค่อยเข้ามุมสายตา
            return s == FishState.Casting || s == FishState.Strike || s == FishState.Fight || s == FishState.Caught;
        }

        // ตำแหน่งตา (เหวี่ยงอยู่: ยื่นหน้าอีกนิด ให้แขนที่สะบัดอยู่ข้างหลังกล้อง)
        Vector3 Eye
        {
            get
            {
                var s = fishing.State;
                bool windup = s == FishState.Charging || s == FishState.Casting;
                float portrait = 1 - Mathf.Clamp01((cam.aspect - 0.6f) / 1.0f);   // จอแนวตั้ง: ยื่นพ้นไหล่อีกนิด (มุมแคบ ไหล่บังครึ่งจอ)
                return headBone.position + player.transform.forward * (windup ? 0.42f + portrait * 0.18f : 0.16f) + Vector3.up * (windup ? 0.1f : 0.05f);   // เหวี่ยง: ลำตัวโน้มไปหน้า กล้องต้องยื่นพ้นไหล่ตัวเอง
            }
        }

        void Update()
        {
            if (fishing == null) return;
            bool want = Want();
            if (want && !active) Enter(); else if (!want && active) Exit();
            if (ik != null)
            {
                // กรอสาย (สู้ปลา/เก็บสาย): มือซ้ายจับมือหมุนรอก · กดค้าง = หมุนเร็ว
                var st = fishing.State;
                ik.crank = st == FishState.Fight || st == FishState.Retrieve;
                ik.crankSpeed = st == FishState.Retrieve || GameInput.ActionHeld || FishingController.Bot ? 2.2f : 0.4f;
            }
            if (!active) return;
            cam.fieldOfView = Fov(cam.aspect);   // หมุนจอระหว่างเล่นได้
            float dt = Time.deltaTime;
            var s = fishing.State;
            var fwd = player.transform.forward; fwd.y = 0; fwd.Normalize();
            Vector3 look = s switch
            {
                FishState.Charging => fishing.AimPoint + Vector3.up * 0.4f,
                FishState.Casting => fishing.CastTarget + Vector3.up * 0.5f,   // มองจุดที่ทุ่นจะตก ไม่แหงนตามทุ่นขึ้นฟ้า
                FishState.Caught => Eye + fwd * 3f + Vector3.down * 0.35f,
                _ => fishing.BobberPos + Vector3.up * 0.25f,
            };
            camPos = Vector3.Lerp(camPos, Eye, Mathf.Min(1, dt * 14));
            var want3 = Quaternion.LookRotation((look - camPos).sqrMagnitude > 1e-4f ? look - camPos : fwd, Vector3.up);
            camRot = Quaternion.Slerp(camRot, want3, Mathf.Min(1, dt * (s == FishState.Fight ? 4f : 7f)));
            // สู้ปลา: กล้องสั่นตามแรงดึงนิด ๆ
            var shake = s == FishState.Fight ? new Vector3(Mathf.PerlinNoise(Time.time * 9, 0) - 0.5f, Mathf.PerlinNoise(0, Time.time * 9) - 0.5f, 0) * 0.012f : Vector3.zero;
            rig.Override(camPos + camRot * shake, camPos + camRot * Vector3.forward, false);

            hud.Showcase3D = s == FishState.Caught;
            if (s == FishState.Caught && fishing.CaughtGo != null) Showcase(dt);
            else if (ik != null) ik.weight = Mathf.MoveTowards(ik.weight, 0, dt * 4);
        }

        // ได้ปลา: 0 กระโดดพ้นน้ำ (สโลโมชั่น) → 1 หยุดภาพ (แฟลช + กรอบโพลารอยด์) → 2 ห้อยแกว่งที่ปลายสายกลางจอ
        void Showcase(float dt)
        {
            var g = fishing.CaughtGo.transform; var c = cam.transform;
            if (!fishing.ExternalShowcase)
            {
                fishing.ExternalShowcase = true; showT = 0; phase = 0; phaseT = 0; showFrom = g.position;
                g.localScale = Vector3.one * Mathf.Clamp(fishing.CaughtCm / 100f * 1.1f + 0.12f, 0.24f, 0.95f);
                Splash(showFrom, 1f);
                Time.timeScale = 0.3f; slowMo = true;
            }
            float udt = Time.unscaledDeltaTime;
            showT += udt; phaseT += udt;
            if (ik != null) ik.weight = Mathf.MoveTowards(ik.weight, 0, udt * 4);   // ไม่ใช้มือจับปลาแล้ว
            float len = g.lossyScale.x;                                  // โมเดลยาว 1 หน่วย → scale = ความยาวจริง (ม.)
            float side = Mathf.Clamp01((cam.aspect - 0.6f) / 1.0f);     // จอแนวตั้ง: ปลาอยู่กลางจอครึ่งบน (การ์ดอยู่ล่าง)
            // ระยะให้ปลายาว ~45% ของความสูงจอเสมอ · ปากปลาล็อกตำแหน่งบนจอ: กระโดด/ถ่ายรูป = กลางจอ · ห้อย = กลางพื้นที่ว่างซ้ายของการ์ด (จอตั้ง: กลางบน)
            float dist = Mathf.Max(0.42f, len / (Mathf.Lerp(0.6f, 0.9f, side) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)));
            float vx = phase >= 2 ? Mathf.Lerp(0.5f, 0.23f, side) : 0.5f, vy = phase >= 2 ? Mathf.Lerp(0.88f, 0.8f, side) : 0.76f;
            var anchor = cam.ViewportToWorldPoint(new Vector3(vx, vy, dist));   // ปากปลา (ปลายสาย)
            var hangRot = Quaternion.LookRotation(-c.forward, c.right);  // หัวปลา (+X) ชี้ขึ้น · ด้านข้างหันหากล้อง
            var flyRot = Quaternion.LookRotation(-c.forward, -c.up);     // ปลาตัวนอน หัวไปทางขวา
            Vector3 Mouth(Quaternion r) => r * Vector3.right * (0.48f * len);
            switch (phase)
            {
                case 0:   // พุ่งพ้นน้ำเป็นโค้ง ตัวบิดดิ้น ละอองน้ำตามมา
                {
                    float k = Mathf.Clamp01(phaseT / 0.95f);
                    var peak = anchor - Mouth(hangRot) - c.up * 0.05f;
                    g.rotation = Quaternion.Slerp(flyRot, hangRot, Mathf.SmoothStep(0, 1, k)) * Quaternion.Euler(0, Mathf.Sin(showT * 26) * 30 * (1 - k * 0.6f), 0);
                    g.position = Vector3.Lerp(showFrom, peak, Mathf.SmoothStep(0, 1, k)) + Vector3.up * Mathf.Sin(k * Mathf.PI) * (0.5f + len * 0.8f);
                    if (k >= 0.86f)
                    {
                        // เดิมหยุดภาพ + แฟลชกรอบโพลารอยด์ "ได้ปลา!" (ผู้เล่นไม่ชอบ บังจอ) → ไปห้อยแกว่งที่ปลายสายต่อเลย
                        phase = 2; phaseT = 0; Time.timeScale = 1; slowMo = false; blendPos = g.position; blendRot = g.rotation;
                    }
                    break;
                }
                case 1:
                    if (phaseT > 0.85f) { phase = 2; phaseT = 0; Time.timeScale = 1; slowMo = false; blendPos = g.position; blendRot = g.rotation; }
                    break;
                default:   // ห้อยปลายสาย แกว่งเป็นลูกตุ้ม บิดตัวช้า ๆ · ดิ้นแรงเป็นพัก ๆ · น้ำหยดจากหาง
                {
                    float flop = Mathf.Repeat(showT, 1.9f) < 0.32f ? Mathf.Sin(showT * 34) * 26 : 0;
                    var rot = Quaternion.AngleAxis(Mathf.Sin(showT * 2.1f) * 9 + flop * 0.4f, c.forward) * Quaternion.AngleAxis(Mathf.Sin(showT * 1.3f) * 28, c.up) * hangRot * Quaternion.Euler(0, flop, 0);
                    var pos = anchor - Mouth(rot);
                    float kb = Mathf.SmoothStep(0, 1, Mathf.Clamp01(phaseT / 0.35f));
                    g.position = Vector3.Lerp(blendPos, pos, kb); g.rotation = Quaternion.Slerp(blendRot, rot, kb);
                    Drips(g.TransformPoint(-0.45f, 0, 0));
                    break;
                }
            }
        }

        // ---------- ละอองน้ำ / หยดน้ำ ----------
        ParticleSystem MakePs(string name, int max)
        {
            var go = new GameObject(name); var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main; main.maxParticles = max; main.simulationSpace = ParticleSystemSimulationSpace.World; main.playOnAwake = false; main.loop = false;
            main.startColor = new Color(0.85f, 0.95f, 1f, 0.85f); main.gravityModifier = 1.2f;
            var em = ps.emission; em.rateOverTime = 0;
            var sh = ps.shape; sh.enabled = false;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Resources.Load<Material>("Water/Mote");
            return ps;
        }
        void Splash(Vector3 at, float power)
        {
            if (splash == null) splash = MakePs("CatchSplash", 300);
            splash.transform.position = at;
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < 90; i++)
            {
                var d = new Vector3(Random.Range(-1f, 1f), Random.Range(1.2f, 3.2f), Random.Range(-1f, 1f)) * power;
                ep.position = at + new Vector3(d.x, 0, d.z) * 0.08f; ep.velocity = d * Random.Range(0.7f, 1.4f);
                ep.startSize = Random.Range(0.015f, 0.06f); ep.startLifetime = Random.Range(0.6f, 1.3f);
                splash.Emit(ep, 1);
            }
            WaterFx.AddRipple(at.x, at.z, 1.4f * power);
        }
        float dripT;
        void Drips(Vector3 at)
        {
            if (drips == null) drips = MakePs("FishDrips", 80);
            if ((dripT -= Time.deltaTime) > 0) return;
            dripT = Random.Range(0.08f, 0.22f);
            var ep = new ParticleSystem.EmitParams { position = at + Random.insideUnitSphere * 0.02f, velocity = Vector3.down * 0.3f, startSize = Random.Range(0.012f, 0.025f), startLifetime = 0.9f };
            drips.Emit(ep, 1);
        }

        // ---------- ปล่อย: ปลาหลุดจากสายตกลงน้ำ ----------
        IEnumerator DropBack(GameObject go)
        {
            if (slowMo) { Time.timeScale = 1; slowMo = false; }
            var t = go.transform; var start = t.position; var end = new Vector3(showFrom.x, showFrom.y, showFrom.z);
            if (end == Vector3.zero) end = start + Vector3.down * 2;
            for (float k = 0; k < 1; k += Time.deltaTime / 0.55f)
            {
                if (go == null) yield break;
                t.position = Vector3.Lerp(start, end, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.4f;
                t.rotation *= Quaternion.Euler(0, 0, 540 * Time.deltaTime);
                yield return null;
            }
            Splash(end, 0.6f);
            Destroy(go);
        }

        // ---------- เก็บ: แคร่ไม้ไผ่ ปลาทุกตัวของวันนี้เรียงบนใบตอง ตัวใหม่ดิ้นอยู่ท้ายแถว ----------
        static Texture2D leafTex;
        IEnumerator Platter(GameObject newFish)
        {
            if (slowMo) { Time.timeScale = 1; slowMo = false; }
            GameInput.Blocked = true;
            var fwd = player.Forward; fwd.y = 0; fwd = fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, fwd);
            var root = new GameObject("Platter").transform;
            var center = player.Pos + right * 1.0f + fwd * 0.35f;
            root.position = center; root.rotation = Quaternion.LookRotation(fwd);
            var basket = Story.S.basket;
            int n = Mathf.Max(1, basket.Count);
            int cols = n <= 4 ? n : n <= 8 ? 4 : 5, rows = (n + cols - 1) / cols;
            float W = Mathf.Max(0.8f, cols * 0.4f), D = Mathf.Max(0.62f, rows * 0.4f), H = 0.42f;
            // แคร่: ลำไม้ไผ่เรียงตามขวาง + ขา 4 ขา
            var bamboo = new Material(Resources.Load<Material>("Rod/Cork")); bamboo.SetColor("_BaseColor", new Color(0.78f, 0.64f, 0.36f));
            var dark = new Material(bamboo); dark.SetColor("_BaseColor", new Color(0.45f, 0.33f, 0.18f));
            void Cyl(Vector3 lp, Vector3 scale, Quaternion rot, Material m)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(c.GetComponent<Collider>());
                c.transform.SetParent(root, false); c.transform.localPosition = lp; c.transform.localRotation = rot; c.transform.localScale = scale;
                c.GetComponent<Renderer>().sharedMaterial = m;
            }
            int slats = Mathf.RoundToInt(D / 0.075f);
            for (int i = 0; i < slats; i++) Cyl(new Vector3(0, H, -D / 2 + (i + 0.5f) * D / slats), new Vector3(0.068f, W / 2 + 0.06f, 0.068f), Quaternion.Euler(0, 0, 90), i % 3 == 0 ? dark : bamboo);
            foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 })
                Cyl(new Vector3(sx * (W / 2 - 0.06f), H / 2, sz * (D / 2 - 0.05f)), new Vector3(0.07f, H / 2, 0.07f), Quaternion.identity, dark);
            foreach (var sz in new[] { -1, 1 }) Cyl(new Vector3(0, H - 0.05f, sz * (D / 2 - 0.05f)), new Vector3(0.06f, W / 2, 0.06f), Quaternion.Euler(0, 0, 90), dark);
            // ใบตองปูทับ
            if (leafTex == null) leafTex = LeafTexture();
            var leafMat = new Material(Resources.Load<Material>("Rod/Cork")); leafMat.SetColor("_BaseColor", Color.white); leafMat.SetTexture("_BaseMap", leafTex);
            int leaves = Mathf.Max(2, Mathf.CeilToInt(W / 0.42f));
            for (int i = 0; i < leaves; i++)
            {
                var lf = new GameObject("BananaLeaf"); lf.transform.SetParent(root, false);
                lf.transform.localPosition = new Vector3(-W / 2 + (i + 0.5f) * W / leaves, H + 0.04f + i * 0.002f, 0);
                lf.transform.localRotation = Quaternion.Euler(0, (i % 2 == 0 ? 4 : -5), 0);
                lf.AddComponent<MeshFilter>().sharedMesh = LeafMesh(W / leaves * 1.35f, D * 1.15f);
                lf.AddComponent<MeshRenderer>().sharedMaterial = leafMat;
            }
            // ปลาทั้งวัน: นอนตะแคงบนใบตอง (ตัวใหม่ = ตัวสุดท้ายที่ชนิดเดียวกับปลาที่เพิ่งได้)
            int newIdx = -1;
            for (int i = basket.Count - 1; i >= 0; i--) if (newFish != null && newFish.name.EndsWith(basket[i].id)) { newIdx = i; break; }
            if (newIdx < 0) newIdx = basket.Count - 1;
            Transform fresh = null; Vector3 freshPos = default; Quaternion lie = Quaternion.LookRotation(Vector3.up, right);
            for (int i = 0; i < basket.Count; i++)
            {
                var bf = basket[i]; var sp = FishDatabase.Get(bf.id);
                GameObject f = i == newIdx && newFish != null ? newFish : (sp != null ? fishing.MakeFish(sp, 1) : null);
                if (f == null) continue;
                f.transform.SetParent(null);
                f.transform.localScale = Vector3.one * Mathf.Clamp(bf.cm / 100f * 1.1f + 0.1f, 0.22f, 0.45f);
                int r = i / cols, cc = i % cols;
                var lp = new Vector3(-W / 2 + (cc + 0.5f) * W / cols, H + 0.075f, D / 2 - (r + 0.5f) * D / rows);
                var wp = root.TransformPoint(lp);
                f.transform.SetPositionAndRotation(wp, lie * Quaternion.Euler(0, 0, (i % 2 == 0 ? 8 : -8)));
                f.transform.SetParent(root, true);
                if (i == newIdx) { fresh = f.transform; freshPos = wp; }
            }
            if (newFish != null && newFish.transform.parent != root) Destroy(newFish);
            bool skip = false;
            hud.IntroOpen("แคร่ปลาวันนี้", $"{basket.Count}/{Story.Capacity} ตัว · ขายได้ราว {Story.Baht(Story.BasketValue)}", "", () => skip = true);
            hud.IntroCard(true); hud.Cinematic(true);
            // กล้องมองลงมาที่แคร่ · ปลาตัวใหม่ดิ้นตึก ๆ บนใบตอง
            float cd = (Mathf.Max(W / Mathf.Max(0.5f, cam.aspect), D) + 0.35f) / (2 * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));   // ระยะพอดีเห็นทั้งแคร่
            var camPos = center + Vector3.up * (H + cd * 0.88f) - fwd * (cd * 0.45f);
            var look = center + Vector3.up * H;
            float dur = FishingController.Bot ? 1.6f : 2.8f;
            for (float t = 0; t < dur && !skip; t += Time.deltaTime)
            {
                rig.Override(camPos + (look - camPos) * Mathf.SmoothStep(0, 0.12f, t / dur), look, false);
                if (fresh != null)
                {
                    float hop = Mathf.Max(0, Mathf.Sin(t * 9)) * 0.045f * Mathf.Clamp01(1.6f - t);
                    fresh.position = freshPos + Vector3.up * hop;
                    fresh.rotation = lie * Quaternion.Euler(Mathf.Sin(t * 23) * 18 * Mathf.Clamp01(1.8f - t), 0, Mathf.Sin(t * 17) * 22 * Mathf.Clamp01(1.8f - t));
                }
                yield return null;
            }
            Destroy(root.gameObject);
            hud.IntroClose(); hud.Cinematic(false);
            rig.ClearOverride(); rig.Snap();
            GameInput.Blocked = hud.ModalOpen || hud.CardOpen;
        }

        // ใบตอง: ยาวรี ปลายมน ก้านกลาง · โค้งลงข้างเล็กน้อย (พิกัดท้องถิ่น ยาวตามแกน Z)
        static Mesh LeafMesh(float w, float l)
        {
            const int N = 14; var v = new Vector3[(N + 1) * 3]; var uv = new Vector2[v.Length]; var tri = new System.Collections.Generic.List<int>();
            for (int i = 0; i <= N; i++)
            {
                float u = i / (float)N, z = (u - 0.5f) * l, hw = w * 0.5f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Lerp(0.04f, 0.96f, u)), 0.45f);
                v[i * 3] = new Vector3(-hw, -0.015f, z); v[i * 3 + 1] = new Vector3(0, 0.005f, z); v[i * 3 + 2] = new Vector3(hw, -0.015f, z);
                uv[i * 3] = new Vector2(0, u); uv[i * 3 + 1] = new Vector2(0.5f, u); uv[i * 3 + 2] = new Vector2(1, u);
                if (i > 0) for (int k = 0; k < 2; k++)
                {
                    int a = (i - 1) * 3 + k, b = i * 3 + k;
                    tri.Add(a); tri.Add(b); tri.Add(a + 1); tri.Add(a + 1); tri.Add(b); tri.Add(b + 1);
                }
            }
            var m = new Mesh { vertices = v, uv = uv }; m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        static Texture2D LeafTexture()
        {
            const int Wt = 128, Ht = 256; var t = new Texture2D(Wt, Ht, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[Wt * Ht];
            for (int y = 0; y < Ht; y++) for (int x = 0; x < Wt; x++)
            {
                float u = x / (float)(Wt - 1), vv = y / (float)(Ht - 1), d = Mathf.Abs(u - 0.5f) * 2;
                var c = Color.Lerp(new Color(0.34f, 0.56f, 0.16f), new Color(0.16f, 0.36f, 0.09f), d * 0.8f);
                float vein = Mathf.Pow(Mathf.Abs(Mathf.Sin((vv * 46 + d * 3.5f) * Mathf.PI)), 18) * 0.12f;   // เส้นใบขนานเฉียงออกจากก้าน
                c += new Color(vein, vein * 1.2f, vein * 0.5f);
                if (d < 0.05f) c = Color.Lerp(new Color(0.72f, 0.78f, 0.42f), c, d / 0.05f);   // ก้านกลางสีอ่อน
                c *= 0.9f + 0.1f * Mathf.PerlinNoise(u * 8, vv * 20);
                px[y * Wt + x] = new Color(c.r, c.g, c.b, 1);
            }
            t.SetPixels(px); t.Apply(); return t;
        }

        void LateUpdate()
        {
            // หลัง Animator: ย่อหัวให้เล็กจนมองไม่เห็น (ผม หมวก ติดไปด้วย) ไม่บังกล้องที่ตา
            if (active && headBone != null) headBone.localScale = Vector3.one * 0.001f;
        }

        void Enter()
        {
            active = Active = true;
            camPos = Eye; camRot = cam.transform.rotation;
            savedNear = cam.nearClipPlane; savedFov = cam.fieldOfView;
            cam.nearClipPlane = 0.03f; cam.fieldOfView = Fov(cam.aspect);
        }

        /// มุมมองแนวตั้ง 55° · จอแนวตั้ง (มือถือ/iPad ตั้ง) ขยายให้มุมกว้างแนวนอนไม่ต่ำกว่า ~56° — ไม่งั้นเห็นแต่มือกับปลาเต็มจอ
        static float Fov(float aspect) => Mathf.Clamp(2f * Mathf.Atan(Mathf.Tan(28f * Mathf.Deg2Rad) / Mathf.Max(0.3f, aspect)) * Mathf.Rad2Deg, 55f, 100f);

        void Exit()
        {
            active = Active = false;
            if (headBone != null) headBone.localScale = Vector3.one;
            cam.nearClipPlane = savedNear; cam.fieldOfView = savedFov;
            if (ik != null) ik.weight = 0;
            if (slowMo) { Time.timeScale = 1; slowMo = false; }
            fishing.ExternalShowcase = false; hud.Showcase3D = false;
            rig.ClearOverride(); rig.Snap();   // กลับมุมปกติทันที ไม่ให้กล้องลอยผ่านหัวตัวละคร
        }
    }
}
