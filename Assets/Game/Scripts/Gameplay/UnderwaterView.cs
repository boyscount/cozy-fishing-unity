using UnityEngine;

namespace CozyFishing
{
    // มองใต้น้ำ: ทุ่นลอยรอปลา → กล้องดำลงไปข้างเหยื่อ เห็นสายเบ็ด เหยื่อ ปลาว่ายมาดู/งับ (PondLife) · ผิวน้ำด้านล่างพลิ้ว หมอกเขียวขุ่น ตะกอนลอย
    // · ไส้เดือนเกี่ยวตะขอเหล็ก · สายหน้าเบ็ดสั่น (แรงขึ้นตอนปลาตอด) ฟองอากาศเกาะสาย · ลำแสงแดดลอดผิวน้ำ · เศษใบไม้จม
    // · สาหร่ายหางกระรอกรอบเหยื่อ ฟองผุดจากโคลน · ขอนไม้ตะไคร่กับกล่องเหล็กสนิมจมอยู่ใกล้ ๆ (ย้ายตามจุดดำทุกครั้ง)
    // ปลาติดเบ็ด (ตีเบ็ด/สู้ปลา) → กล้องโผล่กลับขึ้นผิวน้ำ · น้ำตื้นเกินไปไม่ดำ
    public class UnderwaterView : MonoBehaviour
    {
        public static bool Enabled
        {
            get { try { return PlayerPrefs.GetInt("abot.uw", 1) == 1; } catch (System.Exception) { return true; } }
            set { try { PlayerPrefs.SetInt("abot.uw", value ? 1 : 0); PlayerPrefs.Save(); } catch (System.Exception) { } }
        }
        IWorld world; FishingController fishing; CameraRig rig;
        System.Collections.Generic.List<GameObject> unders = new System.Collections.Generic.List<GameObject>(); GameObject bait; ParticleSystem motes; LineRenderer leader;
        ParticleSystem rays, leaves, lineBubbles, mudBubbles; GameObject bed; Transform[] weeds; Vector3 bedAt = new Vector3(9999, 0, 0);
        bool active, noSpot; float enterT;
        Color fogColor; FogMode savedMode; float savedDensity, savedStart, savedEnd;

        public void Init(IWorld w, FishingController f, CameraRig r)
        {
            world = w; fishing = f; rig = r;
            // ผิวน้ำมองจากใต้น้ำ: แผ่นใหญ่ระดับผิวน้ำตามกล้องไป (mesh ผิวน้ำของบางด่านเล็ก มองไกลแล้วทะลุเห็นฟ้า) · ตลิ่งที่สูงกว่าน้ำบังส่วนเกินเอง
            Material waterMat = null;
            foreach (var mr in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (mr.sharedMaterial != null && mr.sharedMaterial.shader != null && mr.sharedMaterial.shader.name == "CozyFishing/Water") { waterMat = mr.sharedMaterial; break; }
            if (waterMat != null)
            {
                var under = new GameObject("WaterUnderside"); unders.Add(under);
                var q = new Mesh { name = "UnderQuad" };
                q.SetVertices(new[] { new Vector3(-200, 0, -200), new Vector3(-200, 0, 200), new Vector3(200, 0, 200), new Vector3(200, 0, -200) });
                q.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0); q.RecalculateNormals(); q.bounds = new Bounds(Vector3.zero, new Vector3(400, 1, 400));
                under.AddComponent<MeshFilter>().sharedMesh = q;
                var ur = under.AddComponent<MeshRenderer>(); ur.sharedMaterial = new Material(Resources.Load<Material>("Water/WaterUnder")); ur.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var deep = waterMat.HasProperty("_Deep") ? waterMat.GetColor("_Deep") : new Color(0.2f, 0.35f, 0.3f);
                var shallow = waterMat.HasProperty("_Shallow") ? waterMat.GetColor("_Shallow") : new Color(0.6f, 0.7f, 0.6f);
                fogColor = Color.Lerp(deep, shallow, 0.35f);
                ur.sharedMaterial.SetColor("_Tint", Color.Lerp(shallow, Color.white, 0.3f));
                under.SetActive(false);
            }
            // เหยื่อ (ไส้เดือนสีชมพูอมแดง) + สายหน้าเบ็ดจากทุ่นลงมา
            bait = new GameObject("Bait");
            var worm = new GameObject("Worm"); worm.transform.SetParent(bait.transform, false);
            worm.AddComponent<MeshFilter>().sharedMesh = WormMesh();
            var wormMat = new Material(Resources.Load<Material>("Water/Bait")); wormMat.SetColor("_BaseColor", new Color(0.62f, 0.3f, 0.26f)); wormMat.SetFloat("_Smoothness", 0.85f);
            worm.AddComponent<MeshRenderer>().sharedMaterial = wormMat;
            var hook = new GameObject("Hook"); hook.transform.SetParent(bait.transform, false);
            hook.AddComponent<MeshFilter>().sharedMesh = HookMesh();
            hook.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Rod/Metal");
            bait.SetActive(false);
            leader = new GameObject("Leader").AddComponent<LineRenderer>();
            leader.sharedMaterial = Resources.Load<Material>("Water/Leader"); leader.widthMultiplier = 0.003f; leader.positionCount = 8; leader.enabled = false;
            leader.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // ตะกอนลอยในน้ำรอบกล้อง
            var pg = new GameObject("Motes"); motes = pg.AddComponent<ParticleSystem>();
            var main = motes.main; main.startLifetime = 4; main.startSpeed = 0.02f; main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.02f);
            main.startColor = new Color(0.9f, 1f, 0.85f, 0.5f); main.maxParticles = 300; main.simulationSpace = ParticleSystemSimulationSpace.World; main.playOnAwake = false;
            var em = motes.emission; em.rateOverTime = 60;
            var sh = motes.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(3, 1.2f, 3);
            var noise = motes.noise; noise.enabled = true; noise.strength = 0.05f; noise.frequency = 0.3f;
            pg.GetComponent<ParticleSystemRenderer>().sharedMaterial = Resources.Load<Material>("Water/Mote");
            motes.Stop();
            // ลำแสงแดดลอดผิวน้ำ: อนุภาคยืดยาวแนวตั้ง จาง ๆ ค่อย ๆ ปรากฏแล้วหาย
            rays = MakePs("SunRays", 0.7f, new Vector2(7f, 10f), new Color(0.9f, 1f, 0.85f, 0.03f), new Vector3(9, 0.1f, 6), 8);   // บาง ๆ ไม่กี่ลำ (ซ้อนกันมากจะกลายเป็นหมอกขาว)
            var rm = rays.main; rm.startSize3D = true; rm.startSizeX = new ParticleSystem.MinMaxCurve(0.25f, 0.6f); rm.startSizeY = new ParticleSystem.MinMaxCurve(3f, 5f); rm.startSizeZ = 1;
            rm.startSpeed = 0; rm.startRotation = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
            rays.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.VerticalBillboard;
            // เศษใบไม้เน่า/ตะกอนชิ้นใหญ่จมช้า ๆ
            leaves = MakePs("LeafBits", 6, new Vector2(6f, 9f), new Color(0.35f, 0.28f, 0.16f, 0.85f), new Vector3(3, 0.2f, 3), 120);
            var lm = leaves.main; lm.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.04f); lm.startSpeed = 0; lm.gravityModifier = 0.004f; lm.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var ln = leaves.noise; ln.enabled = true; ln.strength = 0.06f; ln.frequency = 0.4f;
            // ฟองอากาศจิ๋วเกาะสาย ลอยขึ้นช้า ๆ · ฟองผุดจากโคลน
            lineBubbles = MakePs("LineBubbles", 4, new Vector2(1.5f, 2.5f), new Color(0.9f, 1f, 1f, 0.7f), new Vector3(0.01f, 0.01f, 0.4f), 60);
            var bm = lineBubbles.main; bm.startSize = new ParticleSystem.MinMaxCurve(0.004f, 0.01f); bm.startSpeed = 0; bm.gravityModifier = -0.012f;
            mudBubbles = MakePs("MudBubbles", 1.2f, new Vector2(2.5f, 3.5f), new Color(0.9f, 1f, 1f, 0.75f), new Vector3(0.3f, 0.02f, 0.3f), 40);
            var mb = mudBubbles.main; mb.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.025f); mb.startSpeed = 0; mb.gravityModifier = -0.03f;
            var mbb = mudBubbles.emission; mbb.SetBursts(new[] { new ParticleSystem.Burst(0, 3, 6, 6, 2.2f) });
            BuildBed();
        }

        ParticleSystem MakePs(string name, float rate, Vector2 life, Color col, Vector3 box, int max)
        {
            var go = new GameObject(name); var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y); m.startColor = col; m.maxParticles = max; m.simulationSpace = ParticleSystemSimulationSpace.World; m.playOnAwake = false;
            var e = ps.emission; e.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = box;
            var c = ps.colorOverLifetime; c.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.25f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) });
            c.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Resources.Load<Material>("Water/Mote");
            return ps;
        }

        // ---------- พื้นท้องน้ำรอบเหยื่อ: สาหร่ายหางกระรอก ขอนไม้ตะไคร่ กล่องเหล็กสนิม ----------
        void BuildBed()
        {
            bed = new GameObject("UnderBed");
            Material Lit(Color c, float smooth) { var m = new Material(Resources.Load<Material>("Water/Bait")); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); return m; }
            var weedMat = Lit(new Color(0.16f, 0.3f, 0.12f), 0.3f);
            weeds = new Transform[9];
            for (int i = 0; i < weeds.Length; i++)
            {
                var w = new GameObject("Hornwort"); w.transform.SetParent(bed.transform, false);
                w.AddComponent<MeshFilter>().sharedMesh = WeedMesh(i);
                w.AddComponent<MeshRenderer>().sharedMaterial = weedMat; weeds[i] = w.transform;
            }
            var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder); log.name = "SunkenLog"; Destroy(log.GetComponent<Collider>());
            log.transform.SetParent(bed.transform, false); log.transform.localScale = new Vector3(0.28f, 1.3f, 0.28f); log.transform.localRotation = Quaternion.Euler(84, 30, 0);
            log.GetComponent<Renderer>().sharedMaterial = Lit(new Color(0.2f, 0.17f, 0.1f), 0.15f);
            var moss = GameObject.CreatePrimitive(PrimitiveType.Sphere); moss.name = "Moss"; Destroy(moss.GetComponent<Collider>());
            moss.transform.SetParent(log.transform, false); moss.transform.localScale = new Vector3(1.1f, 0.5f, 1.1f); moss.transform.localPosition = new Vector3(0, 0.2f, 0.12f);
            moss.GetComponent<Renderer>().sharedMaterial = Lit(new Color(0.2f, 0.32f, 0.12f), 0.1f);
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = "RustyBox"; Destroy(box.GetComponent<Collider>());
            box.transform.SetParent(bed.transform, false); box.transform.localScale = new Vector3(0.34f, 0.2f, 0.26f); box.transform.localRotation = Quaternion.Euler(8, 25, -14);
            box.GetComponent<Renderer>().sharedMaterial = Lit(new Color(0.42f, 0.2f, 0.08f), 0.2f);
            var lid = GameObject.CreatePrimitive(PrimitiveType.Cube); lid.name = "Lid"; Destroy(lid.GetComponent<Collider>());
            lid.transform.SetParent(box.transform, false); lid.transform.localScale = new Vector3(1.04f, 0.15f, 1.04f); lid.transform.localPosition = new Vector3(0.05f, 0.6f, -0.05f); lid.transform.localRotation = Quaternion.Euler(0, 0, 22);
            lid.GetComponent<Renderer>().sharedMaterial = Lit(new Color(0.32f, 0.16f, 0.07f), 0.25f);
            bed.SetActive(false);
        }

        // วางพื้นท้องน้ำรอบเหยื่อใหม่เมื่อดำที่จุดใหม่ (สาหร่ายยืนบนพื้นจริง สูงไม่เกินผิวน้ำ)
        void PlaceBed(Vector3 b, Vector3 cam)
        {
            if ((new Vector2(b.x - bedAt.x, b.z - bedAt.z)).sqrMagnitude < 1f) return;
            bedAt = b;
            var rnd = new System.Random((int)(b.x * 73 + b.z * 31));
            float R(float a, float c) => a + (float)rnd.NextDouble() * (c - a);
            var toCam = cam - b; toCam.y = 0; toCam = toCam.sqrMagnitude > 1e-4f ? toCam.normalized : Vector3.back;
            var side = Vector3.Cross(Vector3.up, toCam);
            Vector3 Ground(Vector3 p) { p.y = world.GroundH(p.x, p.z); return p; }
            for (int i = 0; i < weeds.Length; i++)
            {
                // ด้านหลังเหยื่อ (ไกลจากกล้อง) และด้านข้าง ไม่บังหน้ากล้อง
                var p = Ground(b - toCam * R(0.4f, 2.4f) + side * R(-2f, 2f));
                float room = world.WaterY - p.y - 0.1f;
                if (room < 0.3f) { weeds[i].gameObject.SetActive(false); continue; }
                weeds[i].gameObject.SetActive(true);
                weeds[i].position = p; weeds[i].rotation = Quaternion.Euler(0, R(0, 360), 0);
                float h = Mathf.Min(room, R(0.6f, 1.6f)); weeds[i].localScale = new Vector3(1, h, 1);
            }
            var log = bed.transform.Find("SunkenLog"); log.position = Ground(b - toCam * 1.6f + side * 1.1f) + Vector3.up * 0.1f;
            var box = bed.transform.Find("RustyBox"); box.position = Ground(b - toCam * 1.1f - side * 0.9f) + Vector3.up * 0.06f;
            mudBubbles.transform.position = Ground(b - toCam * 0.8f + side * 0.3f);
        }

        // ไส้เดือน: ท่อเรียวโค้งเป็นรูปตัว S มีปล้อง (ยาว ~7 ซม.)
        static Mesh WormMesh()
        {
            var v = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
            const int N = 22, S = 8;
            for (int i = 0; i <= N; i++)
            {
                float u = i / (float)N;
                var c = new Vector3(Mathf.Sin(u * Mathf.PI * 2.2f) * 0.012f, -u * 0.07f + 0.02f, Mathf.Cos(u * Mathf.PI * 1.3f) * 0.006f);
                float r = 0.0042f * Mathf.Sqrt(Mathf.Sin(Mathf.Clamp(u, 0.03f, 0.97f) * Mathf.PI)) * (1 + 0.12f * Mathf.Sin(u * 90));   // ปล้อง
                for (int k = 0; k <= S; k++) { float a = k / (float)S * Mathf.PI * 2; v.Add(c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r)); }
                if (i > 0) for (int k = 0; k < S; k++) { int a0 = (i - 1) * (S + 1) + k, b0 = a0 + S + 1; t.AddRange(new[] { a0, b0, a0 + 1, a0 + 1, b0, b0 + 1 }); }
            }
            var m = new Mesh { name = "Worm" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        // ตะขอเบ็ด: ก้านตรง + โค้งตัว J + เงี่ยง (ท่อลวดเล็ก)
        static Mesh HookMesh()
        {
            var pts = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i <= 6; i++) pts.Add(new Vector3(0, 0.03f - i * 0.008f, 0));
            for (int i = 1; i <= 10; i++) { float a = i / 10f * Mathf.PI; pts.Add(new Vector3((1 - Mathf.Cos(a)) * 0.007f, -0.018f - Mathf.Sin(a) * 0.009f, 0)); }
            pts.Add(new Vector3(0.0135f, -0.008f, 0)); pts.Add(new Vector3(0.011f, -0.012f, 0));   // ปลายแหลม + เงี่ยง
            var v = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>(); const int S = 6;
            for (int i = 0; i < pts.Count; i++)
            {
                var tg = (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
                var a1 = Vector3.Cross(tg, Vector3.forward).normalized; var a2 = Vector3.forward;
                float r = i == pts.Count - 1 ? 0.0002f : 0.0009f;
                for (int k = 0; k <= S; k++) { float a = k / (float)S * Mathf.PI * 2; v.Add(pts[i] + (a1 * Mathf.Cos(a) + a2 * Mathf.Sin(a)) * r); }
                if (i > 0) for (int k = 0; k < S; k++) { int a0 = (i - 1) * (S + 1) + k, b0 = a0 + S + 1; t.AddRange(new[] { a0, a0 + 1, b0, a0 + 1, b0 + 1, b0 }); }
            }
            var m = new Mesh { name = "Hook" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        // สาหร่ายหางกระรอก: ลำต้นคดเล็กน้อย + ใบเข็มเป็นวงรอบข้อ (สูง 1 หน่วย ปรับด้วย scale.y)
        static Mesh WeedMesh(int seed)
        {
            var rnd = new System.Random(seed * 17 + 3);
            var v = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
            // สองหน้าแยกจุดยอดกัน: ถ้าใช้จุดร่วม normal ของสองหน้าหักล้างเป็นศูนย์ → แสงเป็น NaN แล้ว bloom ลามขาวทั้งจอ
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
                int j = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); t.AddRange(new[] { j, j + 2, j + 1, j, j + 3, j + 2 });
            }
            int stems = rnd.Next(2, 4);
            for (int s = 0; s < stems; s++)
            {
                var basePos = new Vector3((float)rnd.NextDouble() * 0.2f - 0.1f, 0, (float)rnd.NextDouble() * 0.2f - 0.1f);
                float bend = (float)rnd.NextDouble() * 0.15f;
                for (int j = 0; j < 14; j++)
                {
                    float y0 = j / 14f, y1 = (j + 1) / 14f;
                    var p0 = basePos + new Vector3(Mathf.Sin(y0 * 3 + s) * bend, y0, 0); var p1 = basePos + new Vector3(Mathf.Sin(y1 * 3 + s) * bend, y1, 0);
                    Quad(p0 + Vector3.left * 0.004f, p1 + Vector3.left * 0.004f, p1 + Vector3.right * 0.004f, p0 + Vector3.right * 0.004f);
                    for (int k = 0; k < 6; k++)   // วงใบเข็ม
                    {
                        float a = k / 6f * Mathf.PI * 2 + j; var dir = new Vector3(Mathf.Cos(a), 0.35f, Mathf.Sin(a)) * (0.05f * (1 - y0 * 0.5f));
                        Quad(p0 + Vector3.up * 0.003f, p0 + dir, p0 + dir + Vector3.up * 0.006f, p0 + Vector3.up * 0.009f);
                    }
                }
            }
            var m = new Mesh { name = "Hornwort" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        bool Want()
        {
            if (!Enabled || unders.Count == 0 || noSpot) return false;
            var s = fishing.State;
            if (s != FishState.Waiting && s != FishState.Bite) return false;
            var b = fishing.BobberPos;
            return world.DepthAt(b.x, b.z) > 0.32f;   // ดำลงใต้น้ำได้แม้น้ำค่อนข้างตื้น (เห็นภาพใต้น้ำบ่อยขึ้น)
        }

        Vector3 Bait => fishing.BobberPos + Vector3.down * 0.45f;

        void Update()
        {
            if (fishing == null) return;
            if (fishing.State != FishState.Waiting && fishing.State != FishState.Bite) noSpot = false;
            bool want = Want();
            if (want && !active) Enter(); else if (!want && active) Exit();
            if (!active) return;
            var bob = fishing.BobberPos; var b = Bait;
            var d = bob - fishing.player.Pos; d.y = 0; d = d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, d);
            // กล้องอยู่ใต้น้ำข้าง ๆ เหยื่อ: ลองหลายทิศรอบเหยื่อ (เริ่มจากฝั่งผู้เล่น) เอาจุดแรกที่จมน้ำจริงและไม่มุดดิน
            if (!FindCam(b, d, right, out var cam)) { noSpot = true; Exit(); return; }   // ตรงนี้น้ำแคบ/ตื้น ดำไม่ได้ ไว้รอบหน้า
            rig.Override(cam, b + Vector3.up * 0.04f);
            bool nibble = fishing.State == FishState.Bite;
            bait.transform.position = b + Vector3.up * 0.03f;
            bait.transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 2.6f) * 10, Time.time * 12, Mathf.Sin(Time.time * 4.1f) * 14);
            var worm = bait.transform.GetChild(0); worm.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 5) * 18, 0, Mathf.Sin(Time.time * 3.3f) * 22);   // ไส้เดือนดิ้น
            // สายหน้าเบ็ด: ตึงเกือบตรง สั่นเบา ๆ ตามกระแสน้ำ · ปลาตอดแล้วสั่นแรง
            float amp = nibble ? 0.012f : 0.003f, fq = nibble ? 38f : 6f;
            for (int i = 0; i < leader.positionCount; i++)
            {
                float u = i / (leader.positionCount - 1f);
                var off = new Vector3(Mathf.Sin(Time.time * fq + u * 9), 0, Mathf.Cos(Time.time * fq * 0.8f + u * 7)) * amp * Mathf.Sin(u * Mathf.PI);
                leader.SetPosition(i, Vector3.Lerp(bob, b + Vector3.up * 0.05f, u) + off);
            }
            var camT = Camera.main ? Camera.main.transform : null;
            motes.transform.position = camT ? camT.position + camT.forward * 1.2f : b;
            lineBubbles.transform.position = Vector3.Lerp(bob, b, 0.5f); lineBubbles.transform.rotation = Quaternion.LookRotation(b - bob);
            var sh = lineBubbles.shape; sh.scale = new Vector3(0.01f, 0.01f, (b - bob).magnitude);
            if (camT != null)
            {
                var fw = camT.forward; fw.y = 0; fw = fw.sqrMagnitude > 1e-4f ? fw.normalized : Vector3.forward;
                rays.transform.position = new Vector3(camT.position.x, world.WaterY - 1.6f, camT.position.z) + fw * 5.5f;
                leaves.transform.position = camT.position + camT.forward * 1.4f + Vector3.up * 0.4f;
            }
            PlaceBed(b, cam);
            fishing.hud.DepthReading = Mathf.Max(0, world.DepthAt(bob.x, bob.z));
            foreach (var u in unders) u.transform.position = new Vector3(cam.x, world.WaterY, cam.z);
        }

        bool FindCam(Vector3 b, Vector3 d, Vector3 right, out Vector3 cam)
        {
            float top = world.WaterY - 0.15f;
            for (int i = 0; i < 12; i++)
            {
                float ang = (i % 2 == 0 ? 1 : -1) * (i / 2) * 30f;              // 0, ±30, ±60, … องศา จากแนวผู้เล่น→เหยื่อ
                var dir = Quaternion.Euler(0, ang, 0) * (-d * 1.5f + right * 0.8f);
                for (float k = 1f; k > 0.45f; k -= 0.15f)
                {
                    cam = b + dir * k;
                    float floor = world.GroundH(cam.x, cam.z) + 0.15f;
                    if (floor < top - 0.05f) { cam.y = Mathf.Clamp(b.y + 0.05f, floor, top); return true; }
                }
            }
            cam = b; return false;
        }

        void LateUpdate()
        {
            if (!active) return;
            // DayCycle ตั้งสีหมอกทุกเฟรม → ทับด้วยหมอกใต้น้ำหลังจากนั้น · ค่อย ๆ หนาขึ้นตอนเพิ่งดำ
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = Mathf.Lerp(0.05f, 0.42f, Mathf.Clamp01((Time.time - enterT) / 0.5f));
            RenderSettings.fogColor = fogColor * Mathf.Lerp(0.9f, 1.15f, Mathf.Clamp01(world.WaterY));
        }

        void Enter()
        {
            active = true; enterT = Time.time;
            savedMode = RenderSettings.fogMode; savedDensity = RenderSettings.fogDensity; savedStart = RenderSettings.fogStartDistance; savedEnd = RenderSettings.fogEndDistance;
            foreach (var u in unders) u.SetActive(true); bait.SetActive(true); leader.enabled = true; motes.Play();
            bedAt = new Vector3(9999, 0, 0); bed.SetActive(true);
            rays.Play(); leaves.Play(); lineBubbles.Play(); mudBubbles.Play();
        }

        void Exit()
        {
            active = false;
            RenderSettings.fogMode = savedMode; RenderSettings.fogDensity = savedDensity; RenderSettings.fogStartDistance = savedStart; RenderSettings.fogEndDistance = savedEnd;
            foreach (var u in unders) u.SetActive(false); bait.SetActive(false); leader.enabled = false; motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            bed.SetActive(false); fishing.hud.DepthReading = -1;
            foreach (var ps in new[] { rays, leaves, lineBubbles, mudBubbles }) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            rig.ClearOverride();
        }
    }
}
