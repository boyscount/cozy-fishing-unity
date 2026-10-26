using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CozyFishing
{
    // บรรยากาศแฟนตาซีทุกด่าน (ไม่ต้องสร้างฉากใหม่):
    //  · ปรับภาพตามเวลา (Volume ลำดับสูงกว่าของฉาก แก้เฉพาะค่าที่ตั้ง): เช้า/เย็นอุ่นทอง bloom ฟุ้ง · กลางคืนแสงจันทร์น้ำเงิน เกรนฟิล์ม · สติต่ำภาพเหลื่อมสี
    //  · ลำแสงทแยงจากทิศดวงอาทิตย์ (god rays) · ดวงอาทิตย์เรืองแสงบนฟ้า · ละอองเกสรระยิบในแดด → กลางคืนเป็นหิ่งห้อย · หมอกบางลอยเหนือน้ำตอนรุ่งสาง/กลางคืน
    public class Atmosphere : MonoBehaviour
    {
        IWorld world; Camera cam; bool cave, swamp, manor, gloom, raft;
        Light fillLamp; Transform playerT;   // ด่านมืด: ตะเกียงติดตัวส่องรอบตัวละครเสมอ
        SphericalHarmonicsL2 baseProbe; bool probeSaved;   // แสงรอบทิศที่อบไว้จากท้องฟ้าของฉาก (ด่านมืดอบจากฟ้ามืดจึงดำสนิท)
        /// ความสว่างที่ผู้เล่นตั้ง (หน้าตั้งค่า): -1 มืดลง · 0 ปกติ · 1 สว่าง · 2 สว่างมาก
        public static int Brightness
        {
            get { try { return PlayerPrefs.GetInt("cozy.bright", 0); } catch (System.Exception) { return 0; } }
            set { try { PlayerPrefs.SetInt("cozy.bright", value); PlayerPrefs.Save(); } catch (System.Exception) { } }
        }
        Bloom bloom; SplitToning split; ColorAdjustments color; Vignette vig; FilmGrain grain; ChromaticAberration chroma; DepthOfField dof;
        Mesh beamMesh; Material beamMat, sunMat, mistMat, moteMat; Transform sun; ParticleSystem motes;
        Transform[] mist; Vector3[] mistOff;
        const int Beams = 11;
        Vector3[] beamOff; float[] beamW, beamPh;
        Material lanternMat, candleMat; Transform[] lanterns; Vector3[] lanternOff; float[] lanternPh;
        Transform[] krathongs; Vector3[] krathongPos; float[] krathongPh;

        public void Init(IWorld w, Camera c)
        {
            probeSaved = false; world = w; cam = c; cave = w.StageId == "cave"; swamp = w.StageId == "mangrove"; manor = w.StageId == "headman"; gloom = w.StageId == "bend"; raft = w.StageId == "houseboat";
            // ---------- ปรับภาพ ----------
            var vol = gameObject.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 50;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>(); vol.profile = prof;
            bloom = prof.Add<Bloom>(); split = prof.Add<SplitToning>(); color = prof.Add<ColorAdjustments>();
            vig = prof.Add<Vignette>(); grain = prof.Add<FilmGrain>(); chroma = prof.Add<ChromaticAberration>();
            grain.type.Override(FilmGrainLookup.Thin1);
            dof = prof.Add<DepthOfField>(); dof.mode.Override(DepthOfFieldMode.Off);
            var sh = Shader.Find("CozyFishing/NightSky");
            if (sh == null || cam == null) return;
            var rnd = new System.Random(w.StageId.GetHashCode());
            float Rn() => (float)rnd.NextDouble();
            // ---------- ลำแสง ----------
            beamMat = new Material(sh) { mainTexture = BeamTex(), renderQueue = 3010 }; beamMat.SetFloat("_Twinkle", 0.4f);
            beamMesh = new Mesh { name = "GodRays" }; beamMesh.MarkDynamic();
            var bg = new GameObject("GodRays"); bg.AddComponent<MeshFilter>().sharedMesh = beamMesh;
            var br = bg.AddComponent<MeshRenderer>(); br.sharedMaterial = beamMat; br.shadowCastingMode = ShadowCastingMode.Off;
            beamOff = new Vector3[Beams]; beamW = new float[Beams]; beamPh = new float[Beams];
            for (int i = 0; i < Beams; i++) { float a = Rn() * Mathf.PI * 2, r = 4 + Rn() * 18; beamOff[i] = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); beamW[i] = 0.6f + Rn() * 1.8f; beamPh[i] = Rn(); }
            // ---------- ดวงอาทิตย์เรืองแสง ----------
            sunMat = new Material(sh) { mainTexture = GlowTex(new Color(1f, 0.92f, 0.75f), new Color(1f, 0.6f, 0.3f)) };
            sun = Quad("SunGlow", sunMat).transform;
            // ---------- ละอองแสง / หิ่งห้อย ----------
            moteMat = new Material(sh) { mainTexture = DotTex() }; moteMat.SetFloat("_Twinkle", 3f);
            var mg = new GameObject("Motes"); motes = mg.AddComponent<ParticleSystem>();
            var main = motes.main; main.maxParticles = 220; main.simulationSpace = ParticleSystemSimulationSpace.World; main.startLifetime = new ParticleSystem.MinMaxCurve(4, 8);
            main.startSpeed = 0.05f; main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f); main.gravityModifier = -0.004f;
            var em = motes.emission; em.rateOverTime = 30;
            var shape = motes.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(18, 4, 18);
            var noise = motes.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.25f;
            var col = motes.colorOverLifetime; col.enabled = true;
            col.color = new Gradient { colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, alphaKeys = new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) } };
            var pr = mg.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = moteMat; pr.shadowCastingMode = ShadowCastingMode.Off;
            // ---------- โคมลอย (ยี่เป็ง): ลอยขึ้นฟ้าช้า ๆ ไกลออกไป ----------
            lanternMat = new Material(sh) { mainTexture = GlowTex(new Color(1f, 0.85f, 0.5f), new Color(1f, 0.45f, 0.12f)) }; lanternMat.SetFloat("_Twinkle", 5f);
            lanterns = new Transform[18]; lanternOff = new Vector3[18]; lanternPh = new float[18];
            for (int i = 0; i < lanterns.Length; i++)
            {
                lanterns[i] = Quad("KhomLoi", lanternMat).transform;
                float a = Rn() * Mathf.PI * 2, r = 45 + Rn() * 90;
                lanternOff[i] = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); lanternPh[i] = Rn();
                var mf = lanterns[i].GetComponent<MeshFilter>(); var m = Object.Instantiate(mf.sharedMesh);
                var cs = m.colors; for (int k = 0; k < cs.Length; k++) cs[k] = new Color(1, 1, 1, 0.05f + Rn() * 0.9f); m.colors = cs; mf.sharedMesh = m;   // กะพริบไม่พร้อมกัน
            }
            // ---------- กระทง: ใบตองพับ ดอกดาวเรือง เทียนจุด ลอยน้ำรอบตัว ----------
            candleMat = new Material(sh) { mainTexture = GlowTex(new Color(1f, 0.9f, 0.6f), new Color(1f, 0.55f, 0.2f)) }; candleMat.SetFloat("_Twinkle", 9f);
            var leaf = new Material(Resources.Load<Material>("Rod/Cork")); leaf.SetColor("_BaseColor", new Color(0.22f, 0.42f, 0.16f));
            var marigold = new Material(leaf); marigold.SetColor("_BaseColor", new Color(1f, 0.55f, 0.08f));
            krathongs = new Transform[7]; krathongPos = new Vector3[7]; krathongPh = new float[7];
            for (int i = 0; i < krathongs.Length; i++)
            {
                var k = new GameObject("Krathong").transform; krathongs[i] = k; krathongPh[i] = Rn() * 10;
                var b = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(b.GetComponent<Collider>()); b.transform.SetParent(k, false);
                b.transform.localScale = new Vector3(0.32f, 0.04f, 0.32f); b.GetComponent<Renderer>().sharedMaterial = leaf;
                var top = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(top.GetComponent<Collider>()); top.transform.SetParent(k, false);
                top.transform.localPosition = Vector3.up * 0.07f; top.transform.localScale = new Vector3(0.2f, 0.1f, 0.2f); top.GetComponent<Renderer>().sharedMaterial = marigold;
                var fl = Quad("Candle", candleMat).transform; fl.SetParent(k, false); fl.localPosition = Vector3.up * 0.2f; fl.localScale = Vector3.one * 0.28f;
                krathongPos[i] = new Vector3((Rn() - 0.5f) * 30, 0, (Rn() - 0.5f) * 30);
                k.gameObject.SetActive(false);
            }
            // ---------- หมอกเหนือน้ำ ----------
            mistMat = new Material(sh) { mainTexture = MistTex(), renderQueue = 3005 };
            mist = new Transform[16]; mistOff = new Vector3[16];
            for (int i = 0; i < mist.Length; i++)
            {
                var q = Quad("Mist", mistMat, true); mist[i] = q.transform;
                mistOff[i] = new Vector3((Rn() - 0.5f) * 50, 0.25f + Rn() * 0.5f, (Rn() - 0.5f) * 50);
                q.transform.localScale = new Vector3(6 + Rn() * 8, 1, 4 + Rn() * 5);
            }
        }

        GameObject Quad(string name, Material m, bool flat = false)
        {
            var q = new GameObject(name); q.AddComponent<MeshFilter>().sharedMesh = flat ? FlatQuad() : UpQuad();
            q.AddComponent<MeshRenderer>();
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return q;
        }
        static Mesh flatQuad;
        static Mesh FlatQuad()
        {
            if (flatQuad != null) return flatQuad;
            var c0 = new Color(1, 1, 1, 0);   // a = 0 → ไม่กระพริบ
            flatQuad = new Mesh { vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f) },
                uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) }, colors = new[] { c0, c0, c0, c0 } };
            flatQuad.triangles = new[] { 0, 2, 1, 0, 3, 2 }; flatQuad.RecalculateBounds(); return flatQuad;
        }
        static Mesh upQuad;
        static Mesh UpQuad()
        {
            if (upQuad != null) return upQuad;
            var c0 = new Color(1, 1, 1, 0);
            upQuad = new Mesh { vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) },
                uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) }, colors = new[] { c0, c0, c0, c0 } };
            upQuad.triangles = new[] { 0, 2, 1, 0, 3, 2 }; upQuad.RecalculateBounds(); return upQuad;
        }

        void LateUpdate()
        {
            if (cam == null || bloom == null) return;
            float night = DayCycle.Night, golden = DayCycle.Golden, clear = DayCycle.Clear, day = Mathf.Clamp01(1 - night);
            float fear = Mathf.Clamp01((100 - GhostSystem.Sanity) / 100f) * night;
            var cp = cam.transform.position;
            bool under = cp.y < world.WaterY - 0.05f;
            // ---------- ปรับภาพตามเวลา ----------
            bloom.intensity.Override(0.5f + golden * 0.45f * clear + night * 0.55f);
            bloom.threshold.Override(Mathf.Lerp(1.0f, 0.8f, night));
            bloom.scatter.Override(0.72f);
            bloom.tint.Override(Color.Lerp(Color.Lerp(Color.white, new Color(1f, 0.82f, 0.62f), golden), new Color(0.7f, 0.82f, 1f), night));
            var grey = new Color(0.5f, 0.5f, 0.5f);
            split.shadows.Override(Color.Lerp(Color.Lerp(grey, new Color(0.38f, 0.5f, 0.6f), 0.55f + golden * 0.25f), new Color(0.3f, 0.4f, 0.65f), night));   // เงาอมฟ้าเขียว
            split.highlights.Override(Color.Lerp(Color.Lerp(grey, new Color(0.72f, 0.58f, 0.4f), 0.45f + golden * 0.4f), new Color(0.55f, 0.58f, 0.66f), night));   // ไฮไลต์อุ่น
            split.balance.Override(-10);
            color.saturation.Override(Mathf.Lerp(12 + golden * 8, -14, night) - fear * 25 - (swamp ? 16 : 0));
            color.colorFilter.Override(Color.Lerp(Color.Lerp(swamp ? new Color(0.9f, 0.96f, 0.84f) : Color.white, new Color(1f, 0.93f, 0.85f), golden * 0.6f), new Color(0.82f, 0.88f, 1f), night));
            if (swamp) color.contrast.Override(14);   // แสงเงาตัดกันทึม ๆ (chiaroscuro)
            if (raft) { color.contrast.Override(12); color.saturation.Override(-18 - fear * 25); vig.intensity.Override(0.3f + night * 0.12f + fear * 0.15f); }   // แพสุสาน: หม่น คอนทราสต์สูง
            if (gloom) { color.contrast.Override(12); color.colorFilter.Override(new Color(0.82f, 0.88f, 1f)); vig.intensity.Override(0.34f + night * 0.12f + fear * 0.15f); }   // คุ้งน้ำมืด: น้ำเงินดำ อึดอัด
            if (manor)   // ท่าน้ำผู้ใหญ่: โทนน้ำเงิน-ดำ แสงตะเกียงส้มสลัว เปียกชื้นลึกลับ
            {
                color.contrast.Override(10); color.colorFilter.Override(Color.Lerp(new Color(0.84f, 0.9f, 1.06f), new Color(0.78f, 0.86f, 1.05f), night));
                split.highlights.Override(new Color(0.78f, 0.56f, 0.34f)); split.shadows.Override(new Color(0.28f, 0.38f, 0.62f)); split.balance.Override(-25);
                vig.intensity.Override(0.32f + night * 0.12f + fear * 0.15f);
            }
            vig.intensity.Override(0.22f + night * 0.14f + fear * 0.15f + (swamp ? (under ? 0.2f : 0.12f) : 0)); vig.smoothness.Override(swamp && under ? 0.6f : 0.45f);
            vig.color.Override(swamp && under ? new Color(0.06f, 0.05f, 0.02f) : Color.black);
            // ใต้น้ำป่าเลน: เบลอทุกอย่างที่ไกลเกินช่วงเหยื่อ (น้ำขุ่นมองเห็นได้สั้น)
            if (swamp && under) { dof.mode.Override(DepthOfFieldMode.Gaussian); dof.gaussianStart.Override(1.2f); dof.gaussianEnd.Override(4.5f); dof.gaussianMaxRadius.Override(1f); }
            else dof.mode.Override(DepthOfFieldMode.Off);
            grain.intensity.Override(night * 0.28f + fear * 0.3f);
            chroma.intensity.Override(0.04f + fear * 0.55f);
            // ---------- ด่านมืด/กลางคืน: เพิ่มความสว่างให้เล่นบนจอมือถือได้ (ยังคงอารมณ์หลอน) ----------
            float lift = (cave ? 0.55f : gloom ? 0.5f : swamp ? 0.35f : manor ? 0.35f : raft ? 0.2f : 0f) + night * 0.35f;
            // ด่านมืดตอนกลางวัน: ยกพื้นความสว่างขึ้น ให้เห็นพื้นและน้ำชัด (ความมืดจริงจังเก็บไว้กลางคืน) · ใต้น้ำ: สว่างขึ้นทุกด่านให้เห็นเหยื่อ
            bool darkStage = cave || gloom || swamp || manor || raft;
            float dayFloor = darkStage ? day * 0.3f : 0f, underLift = under ? 0.35f : 0f;
            color.postExposure.Override(0.35f + (under ? lift * 0.5f : lift) + dayFloor + underLift + Brightness * 0.3f);   // ฐาน 0.35 = ค่าเดิมของฉาก
            // แสงจันทร์รอบทิศขั้นต่ำ: ambientIntensity ไม่มีผลหลังอบแล้ว จึงเติมสีฟ้าอ่อนลงใน ambient probe ตรง ๆ ให้เงามืดไม่ดำสนิท
            if (!probeSaved) { baseProbe = RenderSettings.ambientProbe; probeSaved = true; }
            float floorK = (cave || gloom ? 0.16f : swamp || manor || raft ? 0.1f : 0f) + night * 0.12f + (darkStage ? day * 0.12f : 0f);
            var probe = baseProbe; probe.AddAmbientLight(new Color(0.42f, 0.5f, 0.72f) * floorK); RenderSettings.ambientProbe = probe;
            if (fillLamp == null && (cave || gloom || swamp || manor || raft))
            {
                fillLamp = new GameObject("FillLantern").AddComponent<Light>(); fillLamp.type = LightType.Point; fillLamp.shadows = LightShadows.None;
                fillLamp.color = new Color(1f, 0.72f, 0.45f);
                var pc = FindAnyObjectByType<PlayerController>(); if (pc != null) playerT = pc.transform;
            }
            if (fillLamp != null)
            {
                var pp = playerT != null ? playerT.position : cp;
                fillLamp.transform.position = pp + Vector3.up * 1.8f;
                fillLamp.range = cave || gloom ? 11f : 8f; fillLamp.intensity = (cave || gloom ? 2.2f : 1.4f) * (under ? 0.4f : 1f);
            }
            if (beamMat == null) return;
            // ---------- ลำแสง: เช้า/เย็นชัดสุด กลางวันจาง กลางคืนเป็นแสงจันทร์ฟ้าจาง ๆ ----------
            float beamA = cave || under ? 0 : (0.05f + golden * 0.16f) * day * clear + night * 0.05f;
            if (swamp && !under) beamA = Mathf.Max(beamA, 0.15f * day);   // แดดลอดยอดโกงกางเป็นลำทั้งวันแม้ฟ้าครึ้ม
            if (raft && !under) beamA = Mathf.Max(beamA, 0.12f * day);    // แสงเช้าสาดผ่านหมอกระหว่างแพร้าง
            if ((world.StageId == "marsh" || world.StageId == "temple") && !under) beamA = Mathf.Max(beamA, 0.11f * day);   // ลำแสงสีทองลอดม่านหมอกเหนือหนอง/วังน้ำ
            var axis = night > 0.5f ? -(DayCycle.MoonRot * Vector3.forward) : DayCycle.SunDir;   // ทิศไปหาแหล่งแสง
            if (axis.y < 0.12f) axis = new Vector3(axis.x, 0.12f, axis.z).normalized;
            var bc = Color.Lerp(Color.Lerp(new Color(1f, 0.95f, 0.8f), new Color(1f, 0.72f, 0.42f), golden), new Color(0.55f, 0.68f, 1f), night);
            beamMat.SetFloat("_Fade", beamA);
            var v = new Vector3[Beams * 4]; var uv = new Vector2[Beams * 4]; var cl = new Color[Beams * 4]; var tri = new int[Beams * 6];
            float L = 26;
            for (int i = 0; i < Beams; i++)
            {
                var basePt = new Vector3(cp.x, world.WaterY, cp.z) + beamOff[i];
                var mid = basePt + axis * (L * 0.45f);
                var toCam = (cp - mid).normalized;
                var side = Vector3.Cross(axis, toCam).normalized * beamW[i];
                var a0 = basePt - axis * 2f; var a1 = basePt + axis * L;
                v[i * 4] = a0 - side; v[i * 4 + 1] = a0 + side; v[i * 4 + 2] = a1 + side; v[i * 4 + 3] = a1 - side;
                uv[i * 4] = new Vector2(0, 0); uv[i * 4 + 1] = new Vector2(1, 0); uv[i * 4 + 2] = new Vector2(1, 1); uv[i * 4 + 3] = new Vector2(0, 1);
                float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * 0.25f + beamPh[i] * 6.28f);   // ลำแสงค่อย ๆ จางเข้าออก (เมฆบังผ่าน)
                var c = bc * pulse; c.a = 0; for (int k = 0; k < 4; k++) cl[i * 4 + k] = c;
                tri[i * 6] = i * 4; tri[i * 6 + 1] = i * 4 + 2; tri[i * 6 + 2] = i * 4 + 1; tri[i * 6 + 3] = i * 4; tri[i * 6 + 4] = i * 4 + 3; tri[i * 6 + 5] = i * 4 + 2;
            }
            beamMesh.Clear(); beamMesh.vertices = v; beamMesh.uv = uv; beamMesh.colors = cl; beamMesh.triangles = tri;
            beamMesh.bounds = new Bounds(cp, Vector3.one * 200);
            // ---------- ดวงอาทิตย์เรืองแสง ----------
            float sunA = cave || under ? 0 : day * Mathf.Lerp(0.35f, 1f, golden) * Mathf.Lerp(0.15f, 1f, clear) * Mathf.Clamp01(DayCycle.SunDir.y * 6 + 0.3f);
            sunMat.SetFloat("_Fade", sunA);
            sun.position = cp + DayCycle.SunDir * 235; sun.rotation = Quaternion.LookRotation(DayCycle.SunDir);
            sun.localScale = Vector3.one * Mathf.Lerp(38, 70, golden);
            sun.gameObject.SetActive(sunA > 0.01f);
            // ---------- ละออง: กลางวันเกสรระยิบสีทอง · กลางคืนหิ่งห้อยเหลืองเขียวกะพริบ ----------
            motes.transform.position = cp + cam.transform.forward * 5 + Vector3.down * 0.5f;
            var main = motes.main;
            main.startColor = night > 0.5f ? new ParticleSystem.MinMaxGradient(new Color(0.75f, 1f, 0.35f), new Color(1f, 0.9f, 0.4f)) : new ParticleSystem.MinMaxGradient(new Color(1f, 0.92f, 0.7f) * 0.7f, new Color(1f, 1f, 0.9f) * 0.5f);
            main.startSize = night > 0.5f ? new ParticleSystem.MinMaxCurve(0.05f, 0.11f) : new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
            moteMat.SetFloat("_Fade", under ? 0 : night > 0.5f ? 1.2f : 0.5f + golden * 0.6f);
            // ---------- โคมลอย + กระทง (กลางคืน) ----------
            float lanternA = cave || under ? 0 : Mathf.Clamp01((night - 0.4f) / 0.6f);
            lanternMat.SetFloat("_Fade", lanternA * 0.9f); candleMat.SetFloat("_Fade", lanternA * 1.1f);
            for (int i = 0; i < lanterns.Length; i++)
            {
                bool on = lanternA > 0.01f; if (lanterns[i].gameObject.activeSelf != on) lanterns[i].gameObject.SetActive(on);
                if (!on) continue;
                float life = Mathf.Repeat(Time.time * 0.012f + lanternPh[i], 1f);   // ลอยขึ้นจากขอบฟ้าถึงสูงลิบ แล้ววนใหม่
                var o = lanternOff[i] + new Vector3(Mathf.Sin(Time.time * 0.05f + i) * 4, 4 + life * 70, life * 12);
                lanterns[i].position = new Vector3(cp.x, world.WaterY, cp.z) + o;
                lanterns[i].rotation = Quaternion.LookRotation(lanterns[i].position - cp);
                lanterns[i].localScale = Vector3.one * (2.6f * (1 - life * 0.5f));
            }
            for (int i = 0; i < krathongs.Length; i++)
            {
                var k = krathongs[i];
                var p = krathongPos[i]; p.x += Mathf.Sin(Time.time * 0.03f + krathongPh[i]) * 3; p.z += Time.time * 0.08f;   // ลอยตามน้ำช้า ๆ
                float wx = Mathf.Repeat(p.x + 15, 30) - 15, wz = Mathf.Repeat(p.z + 15, 30) - 15;
                float x = cp.x + wx, z = cp.z + wz;
                bool on = lanternA > 0.01f && world.DepthAt(x, z) > 0.25f;
                if (k.gameObject.activeSelf != on) k.gameObject.SetActive(on);
                if (!on) continue;
                k.position = new Vector3(x, world.WaterY + 0.02f + Mathf.Sin(Time.time * 1.3f + krathongPh[i]) * 0.015f, z);
                k.GetChild(2).rotation = Quaternion.LookRotation(k.GetChild(2).position - cp);   // แสงเทียนหันหากล้อง
            }
            // ---------- หมอกเหนือน้ำ: รุ่งสาง (ก่อน 8 โมง) และกลางคืน ----------
            float hour = Story.S != null ? Story.S.time % 24 : 12;
            float mistA = under ? 0 : Mathf.Max(Mathf.Clamp01((8.2f - hour) / 2f) * (hour > 4 ? 1 : 0), night * 0.8f) * 0.07f;
            if (swamp && !under) mistA = Mathf.Max(mistA, 0.06f);   // ป่าเลนชื้นจัด หมอกลอยต่ำทั้งวัน
            if (manor && !under) mistA = Mathf.Max(mistA, 0.05f);   // ท่าน้ำ: ไอหมอกเรี่ยผิวน้ำตลอด
            if (gloom && !under) mistA = Mathf.Max(mistA, 0.07f);   // คุ้งน้ำมืด: หมอกหนาทั้งวันทั้งคืน
            mistMat.SetFloat("_Fade", mistA);
            for (int i = 0; i < mist.Length; i++)
            {
                bool on = mistA > 0.003f; if (mist[i].gameObject.activeSelf != on) mist[i].gameObject.SetActive(on);
                if (!on) continue;
                var o = mistOff[i]; o.x += Time.time * 0.25f;   // หมอกลอยช้า ๆ ตามลม
                float wx = Mathf.Repeat(o.x + 25, 50) - 25, wz = Mathf.Repeat(o.z + 25, 50) - 25;
                mist[i].position = new Vector3(cp.x + wx, world.WaterY + o.y, cp.z + wz);
            }
        }

        // ---------- ภาพที่สร้างเอง ----------
        static Texture2D BeamTex()
        {
            const int W = 64, H = 128; var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[W * H];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                float u = x / (W - 1f) * 2 - 1, v = y / (H - 1f);
                float across = Mathf.Exp(-u * u * 4.5f) * (0.75f + 0.25f * Mathf.PerlinNoise(x * 0.3f, 3.1f));   // เส้นแสงเป็นริ้ว ๆ
                float along = Mathf.SmoothStep(0, 1, v / 0.25f) * Mathf.SmoothStep(0, 1, (1 - v) / 0.55f);
                px[y * W + x] = new Color(1, 1, 1, across * along);
            }
            t.SetPixels(px); t.Apply(); return t;
        }
        static Texture2D GlowTex(Color core, Color halo)
        {
            const int S = 128; var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S * 2 - 1, v = (y + 0.5f) / S * 2 - 1, r = Mathf.Sqrt(u * u + v * v);
                var c = core * Mathf.Clamp01((0.08f - r) * 40 + 1) * 2.2f + halo * (Mathf.Exp(-r * 7f) * 0.9f + Mathf.Exp(-r * 2.2f) * 0.25f);
                c *= 1 - Mathf.SmoothStep(0.85f, 1f, r); c.a = 1; px[y * S + x] = c;
            }
            t.SetPixels(px); t.Apply(); return t;
        }
        static Texture2D DotTex()
        {
            const int S = 32; var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S * 2 - 1, v = (y + 0.5f) / S * 2 - 1, r2 = u * u + v * v;
                px[y * S + x] = new Color(1, 1, 1, Mathf.Clamp01(Mathf.Exp(-r2 * 10f) + Mathf.Exp(-r2 * 3f) * 0.3f) * (1 - Mathf.Clamp01(r2)));
            }
            t.SetPixels(px); t.Apply(); return t;
        }
        static Texture2D MistTex()
        {
            const int S = 128; var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S * 2 - 1, v = (y + 0.5f) / S * 2 - 1, r = Mathf.Sqrt(u * u + v * v);
                float n = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.65f + Mathf.PerlinNoise(x * 0.12f + 7, y * 0.12f + 3) * 0.35f;
                float a = Mathf.Clamp01(1 - r) * Mathf.SmoothStep(0.25f, 0.8f, n);
                px[y * S + x] = new Color(0.85f, 0.9f, 1f, a);
            }
            t.SetPixels(px); t.Apply(); return t;
        }
    }
}
