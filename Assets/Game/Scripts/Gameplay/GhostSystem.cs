using System.Collections.Generic;
using UnityEngine;

namespace CozyFishing
{
    // ตกปลากลางคืน: วิญญาณผีไทยประจำด่าน (ด่านละตน ไม่ซ้ำกัน) ปรากฏเป็นระยะ ขยับเข้าหาผู้เล่นตามนิสัยของตน
    //  · เกจสติ: ผียิ่งใกล้สติยิ่งลด · ตะเกียงกระพริบ · เสียงหัวใจเต้น · ขอบจอแดง · เกจแรงดึงแกว่ง · สติหมด = หมดสติ ตื่นที่บ้าน เสียปลาครึ่งข้อง
    //  · ไฟฉาย (ปุ่ม/คีย์ F): ส่องตรงผีค้างไว้ ~1 วิ ไล่ไปได้ · แบตหมดได้ · สายสิญจน์ (ตกได้กลางคืน) ลดการเสียสติ และช่วยไว้ได้ครั้งหนึ่งตอนสติหมด
    //  · ผีปรากฏอยู่ = ปลา 3–4 ดาว/ปลาตำนานกินเบ็ดบ่อยขึ้นมาก (FishingController.Haunt)
    [DefaultExecutionOrder(1000)]   // หลังกล้อง: สั่นกล้องทับได้
    public class GhostSystem : MonoBehaviour
    {
        public const float NightFrom = 18.6f;
        public static float Sanity = 100, Battery = 100;
        public static bool Active => instance != null && instance.ghost != null && instance.state != S.Hidden;
        static GhostSystem instance;

        class Def { public GhostBodies.Kind kind; public string name, nearLine, legend; public Color color; public float dark, dist, stop, speed, fear; public bool water; }
        static readonly Dictionary<string, Def> Defs = new Dictionary<string, Def>
        {
            { "rapids", new Def { kind = GhostBodies.Kind.Drowned, name = "พรายหินกินคน", nearLine = "มีอะไรคลานอยู่ตามซอกหิน… อย่าสบตามันนาน", legend = "คนหาปลาที่จมหายในแก่งหินเมื่อหลายสิบปีก่อน… ยังรอใครสักคนมาแทนที่", color = new Color(0.78f, 0.88f, 0.9f), dist = 13, stop = 3.5f, speed = 0.9f, fear = 1f } },
            { "temple", new Def { kind = GhostBodies.Kind.Preta, name = "เปรตใต้น้ำ", nearLine = "เงาสูงเท่ายอดเจดีย์ก้มหน้าลงมา… น้ำกระเพื่อมแรง", legend = "อดีตคนเฝ้าวัดที่ขโมยของถวายพระ ต้องหิวโหยอยู่ใต้น้ำชั่วกัปชั่วกัลป์", color = new Color(0.86f, 0.8f, 0.7f), dist = 22, stop = 12, speed = 1.2f, fear = 1.1f, water = true } },
            { "marsh", new Def { kind = GhostBodies.Kind.LotusMaiden, name = "ผีสไบขาดริมหนอง", nearLine = "เสียงสะอื้นแว่วมาจากดงบัว… มันขยับเข้ามาทุกครั้งที่กระพริบตา", legend = "หญิงสาวที่รอคนรักริมหนองจนสิ้นใจ สไบขาดวิ่นยังลอยอยู่กลางดงบัว", color = new Color(0.9f, 0.92f, 1f), dist = 15, stop = 3, speed = 0, fear = 1f, water = true } },
            { "houseboat", new Def { kind = GhostBodies.Kind.NetFiend, name = "ผีพรายอวน", nearLine = "ฟองอากาศผุดรอบเรือ… มีอะไรดึงสายเบ็ดอยู่ใต้น้ำ", legend = "ชาวเรือที่ติดอวนของตัวเองจมน้ำตาย… มันอยากได้ใครสักคนลงไปอยู่ด้วย", color = new Color(0.35f, 0.55f, 0.45f), dark = 1, dist = 13, stop = 2.5f, speed = 1.4f, fear = 0.9f, water = true } },
            { "bend", new Def { kind = GhostBodies.Kind.RootSiren, name = "นางพรายน้ำรากไม้", nearLine = "หิ่งห้อยดับทีละดวง… มีเสียงเรียกชื่อพ่อจากในความมืด", legend = "เสียงหวานเรียกชื่อคนที่ได้ยิน ใครเดินตามเสียงเข้าไปในรากไม้ ไม่เคยได้กลับออกมา", color = new Color(0.6f, 0.95f, 0.75f), dist = 12, stop = 4, speed = 0.35f, fear = 1f, water = true } },
            { "cave", new Def { kind = GhostBodies.Kind.Headless, name = "ผีถ้ำหัวขาด", nearLine = "เสียงโซ่ตรวนลากหินดังก้องถ้ำ… แสงในน้ำดับลงเป็นทาง", legend = "ทาสที่ถูกตัดหัวเพราะเห็นที่ซ่อนสมบัติในถ้ำ ยังเดินตามหาหัวของตัวเองอยู่", color = new Color(0.55f, 0.8f, 1f), dist = 15, stop = 4, speed = 0.7f, fear = 1.1f, water = true } },
            { "mangrove", new Def { kind = GhostBodies.Kind.MudWraith, name = "สัมภเวสีโคลนดูด", nearLine = "จ๊วบ… จ๊วบ… เสียงดูดโคลนใกล้เข้ามาเรื่อย ๆ", legend = "คนหาหอยที่จมโคลนตอนน้ำขึ้น ไม่มีใครทำบุญไปให้ จึงวนเวียนขอส่วนบุญ", color = new Color(0.75f, 0.7f, 0.6f), dist = 13, stop = 1.4f, speed = 2.2f, fear = 1f } },
            { "paddy", new Def { kind = GhostBodies.Kind.LotusMaiden, name = "นางตานีใต้กอกล้วย", nearLine = "ใบตองไหวทั้งที่ไม่มีลม… เสียงผู้หญิงหัวเราะคิกคักหลังกอกล้วย", legend = "นางไม้ในกอกล้วยตานีหลังบ้าน ไม่ทำร้ายใคร… ถ้าไม่มีใครลบหลู่", color = new Color(0.7f, 0.95f, 0.7f), dist = 14, stop = 4, speed = 0, fear = 0.7f } },
            { "headman", new Def { kind = GhostBodies.Kind.Hanging, name = "วิญญาณพรานปลาผูกคอตาย", nearLine = "เชือกเสียดขื่อดังเอี๊ยด… อาด… ร่างนั้นหย่อนลงมาใกล้ขึ้น", legend = "พรานปลาที่ติดหนี้ผู้ใหญ่คนก่อน ผูกคอใต้ชายคาท่าน้ำในคืนเดือนมืด", color = new Color(0.86f, 0.86f, 0.8f), dist = 0, stop = 0, speed = 0, fear = 1.2f } },
        };

        public static int NightHazard(string stage)
        {
            if (!Defs.TryGetValue(stage, out var d)) return 0;
            float k = d.fear + d.speed * 0.25f;
            return d.fear >= 1.2f || k >= 1.25f ? 3 : k >= 0.9f ? 2 : 1;
        }

        enum S { Hidden, Appearing, Approach, Near, Leaving }
        IWorld world; FishingController fishing; Hud hud; StoryDirector director;
        Def def; Transform ghost; Material mat; S state = S.Hidden;
        float timer, cooldown = 8, alpha, stare, expose, warpT, nightAnnounced;
        Vector3 pos; bool stoleOrSnapped;
        Light lantern, flash, aura; AudioSource heart;
        readonly List<ParticleSystem> dimPs = new List<ParticleSystem>(); readonly List<Light> dimLights = new List<Light>(); readonly List<float> dimLightBase = new List<float>();
        public static bool FlashOn;
        // ร่างมนุษย์: กระดูกที่ใช้ดัดท่าให้ผิดธรรมชาติ (คอหัก หลังค่อม หัวกระตุก) · ผมยาวปิดหน้า · เชือกแขวนคอ
        Animator gAnim; Transform hHead, hNeck, hChest, hSpine; Material hairMat; LineRenderer rope;
        Vector3 shownPos; float stutterT, twitchT = 1, twitchLeft; Vector3 twitchAxis; float twitchDeg; Vector3 baseScale = Vector3.one;
        // ฉากผีปรากฏ: แฟลชขาว + สโลโมชั่น + กล้องซูมเข้าหาผี + ภาพย้อนอดีต (ชื่อ/ตำนาน) · คืนละครั้งเต็ม ครั้งต่อไปสั้น ๆ
        float cineT = -1, cineLen, cineFov; Camera cineCam; bool revealedTonight, cineRestore; Vector3 cineBasePos; Quaternion cineBaseRot; AudioSource sting;

        public void Init(IWorld w, FishingController f, StoryDirector d)
        {
            instance = this; world = w; fishing = f; hud = f.hud; director = d;
            Time.timeScale = 1;   // กันค้างสโลโมชั่นข้ามฉาก
            Defs.TryGetValue(w.StageId, out def);
            // ตะเกียงติดตัว (กลางคืน) + ไฟฉายจากกล้อง
            lantern = new GameObject("Lantern").AddComponent<Light>(); lantern.type = LightType.Point; lantern.color = new Color(1f, 0.72f, 0.4f); lantern.range = 7; lantern.shadows = LightShadows.None; lantern.enabled = false;
            flash = new GameObject("Flashlight").AddComponent<Light>(); flash.type = LightType.Spot; flash.color = new Color(0.95f, 0.97f, 1f); flash.range = 22; flash.spotAngle = 34; flash.intensity = 6; flash.shadows = LightShadows.None; flash.enabled = false;
            // เสียงหัวใจเต้น (สร้างเสียงเอง: ตุบ-ตุบ ความถี่ต่ำ)
            heart = gameObject.AddComponent<AudioSource>(); heart.loop = true; heart.volume = 0; heart.playOnAwake = false; heart.spatialBlend = 0;
            int sr = 22050; var data = new float[sr];
            for (int i = 0; i < sr; i++)
            {
                float tt = i / (float)sr, s = 0;
                foreach (var (at, amp) in new[] { (0f, 1f), (0.24f, 0.7f) }) { float u = tt - at; if (u >= 0 && u < 0.16f) s += Mathf.Sin(u * 2 * Mathf.PI * 52) * Mathf.Exp(-u * 26) * amp; }
                data[i] = s * 0.9f;
            }
            var clip = AudioClip.Create("Heartbeat", sr, 1, sr, false); clip.SetData(data, 0); heart.clip = clip;
            sting = gameObject.AddComponent<AudioSource>(); sting.playOnAwake = false; sting.spatialBlend = 0; sting.volume = 0.8f; sting.clip = StingClip();
            if (FishingController.Bot && Application.absoluteURL.Contains("ghost")) cooldown = 2;   // ทดสอบ: ผีโผล่เร็ว
            if (def == null) return;
            mat = new Material(Shader.Find("CozyFishing/Ghost")); mat.SetColor("_Color", def.color); mat.SetFloat("_Dark", def.dark); mat.SetFloat("_Alpha", 0);
            mat.SetFloat("_FeetFade", def.kind == GhostBodies.Kind.MudWraith || def.kind == GhostBodies.Kind.Hanging ? 0 : def.kind == GhostBodies.Kind.Preta ? 3f : 0.9f);
            mat.SetFloat("_Wobble", 0.006f); mat.SetFloat("_Glitch", def.kind == GhostBodies.Kind.Headless || def.kind == GhostBodies.Kind.Drowned ? 0.9f : 0.6f);
            var eyeMat = new Material(Resources.Load<Material>("Water/Leader"));
            eyeMat.SetColor("_BaseColor", def.kind == GhostBodies.Kind.NetFiend || def.kind == GhostBodies.Kind.MudWraith || def.kind == GhostBodies.Kind.Preta ? new Color(2.6f, 0.3f, 0.2f) : new Color(1.1f, 0.12f, 0.08f));   // ตาแดงหม่น
            var body = BuildHuman(eyeMat) ?? BuildLowPoly(eyeMat);
            ghost = body.transform;
            // แสงหลอนรอบตัวผี: ส่องพื้น/น้ำรอบ ๆ ให้รู้ว่ามีอะไรอยู่ตรงนั้น
            aura = new GameObject("GhostAura").AddComponent<Light>(); aura.transform.SetParent(ghost, false); aura.transform.localPosition = Vector3.up * (def.kind == GhostBodies.Kind.Preta ? 4f : 1f);
            aura.type = LightType.Point; aura.color = def.dark > 0 ? new Color(0.4f, 0.8f, 0.5f) : def.color; aura.range = def.kind == GhostBodies.Kind.Preta ? 10 : 5; aura.intensity = 0; aura.shadows = LightShadows.None;
            body.SetActive(false);
            // ของในฉากที่ผีทำให้หรี่ลง: หิ่งห้อย (คุ้งน้ำมืด) · แสงน้ำเรืองในถ้ำ
            foreach (var ps in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                if (ps.name.Contains("Firefl") || ps.name.Contains("WaterGlow")) dimPs.Add(ps);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.name.Contains("GlowLight")) { dimLights.Add(l); dimLightBase.Add(l.intensity); }
        }

        GameObject BuildLowPoly(Material eyeMat)
        {
            var body = new GameObject("Ghost_" + def.kind);
            body.AddComponent<MeshFilter>().sharedMesh = GhostBodies.Build(def.kind, world.StageId.GetHashCode(), out var eyes);
            var mr = body.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach (var e in eyes)
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(eye.GetComponent<Collider>());
                eye.transform.SetParent(body.transform, false); eye.transform.localPosition = e; eye.transform.localScale = Vector3.one * 0.035f;
                eye.GetComponent<Renderer>().sharedMaterial = eyeMat;
            }
            return body;
        }

        // ร่างผีจากโมเดลคน (โครงกระดูกเดียวกับตัวเอก): ถอดคันเบ็ด/หมวก · ผิวศพ · ผมดำยาว (ผีผู้หญิง) · ตาเรืองเล็ก ๆ
        GameObject BuildHuman(Material eyeMat)
        {
            var srcAnim = fishing.player.animator;
            if (srcAnim == null || !srcAnim.isHuman) return null;
            var src = srcAnim.gameObject;
            var go = Instantiate(src); go.name = "Ghost_" + def.kind;
            go.transform.SetParent(null, false);
            baseScale = src.transform.lossyScale;
            go.transform.localScale = baseScale;
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is SkinnedMeshRenderer smr)
                {
                    var ms = new Material[smr.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = mat;
                    smr.sharedMaterials = ms; smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; smr.updateWhenOffscreen = true;
                }
                else Destroy(r.gameObject);   // คันเบ็ด หมวก ของติดมือ
            }
            gAnim = go.GetComponent<Animator>();
            gAnim.applyRootMotion = false; gAnim.speed = 0.3f; gAnim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            hHead = gAnim.GetBoneTransform(HumanBodyBones.Head); hNeck = gAnim.GetBoneTransform(HumanBodyBones.Neck);
            hChest = gAnim.GetBoneTransform(HumanBodyBones.Chest); hSpine = gAnim.GetBoneTransform(HumanBodyBones.Spine);
            if (hHead == null) { Destroy(go); return null; }
            var F = go.transform.forward; var Rt = go.transform.right;
            // ผม: ผีผู้หญิงผมยาวดำปิดหน้าถึงอก · ผีจมน้ำผมเปียกสั้น · ผีโคลน/พรายอวนผมยุ่ง
            float hairLen = def.kind == GhostBodies.Kind.LotusMaiden || def.kind == GhostBodies.Kind.RootSiren ? 0.85f
                : def.kind == GhostBodies.Kind.MudWraith ? 0.5f : 0;
            if (hairLen > 0)
            {
                hairMat = new Material(mat); hairMat.SetFloat("_FeetFade", 0); hairMat.SetFloat("_Dark", 1); hairMat.SetFloat("_Cull", 0); hairMat.SetColor("_Color", def.color * 0.5f);
                var hair = new GameObject("Hair"); hair.transform.SetParent(hHead, false);
                hair.AddComponent<MeshFilter>().sharedMesh = HairMesh(hHead, F, Rt, hairLen, hairLen > 0.6f ? 1f : 0.7f);
                var hr = hair.AddComponent<MeshRenderer>(); hr.sharedMaterial = hairMat; hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // ตาเรืองเล็ก ๆ ลอดผม (ผีหัวขาดไม่มีตา)
            if (def.kind != GhostBodies.Kind.Headless)
                foreach (var sd in new[] { -1f, 1f })
                {
                    var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(eye.GetComponent<Collider>());
                    eye.transform.position = hHead.position + Vector3.up * 0.07f + F * 0.115f + Rt * 0.033f * sd;
                    eye.transform.SetParent(hHead, true); eye.transform.localScale = Vector3.one * 0.012f / Mathf.Max(0.001f, hHead.lossyScale.x);
                    eye.GetComponent<Renderer>().sharedMaterial = eyeMat;
                }
            // เชือกแขวนคอ (วิญญาณพรานปลา)
            if (def.kind == GhostBodies.Kind.Hanging)
            {
                rope = new GameObject("Rope").AddComponent<LineRenderer>(); rope.transform.SetParent(go.transform, false);
                rope.sharedMaterial = new Material(Resources.Load<Material>("Water/Leader")); rope.sharedMaterial.SetColor("_BaseColor", new Color(0.12f, 0.09f, 0.06f));
                rope.widthMultiplier = 0.025f; rope.positionCount = 2; rope.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // สัดส่วน: เปรตผอมสูงลิบ · ที่เหลือสูงกว่าคนนิดหนึ่ง
            go.transform.localScale = Vector3.Scale(baseScale, def.kind == GhostBodies.Kind.Preta ? new Vector3(1.5f, 5f, 1.5f) : Vector3.one * 1.06f);
            return go;
        }

        // ผมยาว: เส้นผมเป็นริบบิ้นห้อยจากรอบศีรษะ เส้นด้านหน้าห้อยปิดหน้าลงมาถึงอก (สร้างในพิกัดของกระดูกหัว)
        static Mesh HairMesh(Transform head, Vector3 F, Vector3 R, float len, float frontCover)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var tri = new List<int>(); var rnd = new System.Random(5);
            float Rn() => (float)rnd.NextDouble();
            var H = head.position + Vector3.up * 0.08f;
            const int count = 170, seg = 10;
            for (int sIdx = 0; sIdx < count; sIdx++)
            {
                float a = sIdx / (float)count * Mathf.PI * 2 + Rn() * 0.15f;
                var radial = Mathf.Cos(a) * R + Mathf.Sin(a) * F;
                float fr = Vector3.Dot(radial, F);
                float L = len * (0.8f + 0.4f * Rn()) * (fr > 0.2f ? frontCover : 1f);
                var root = H + radial * 0.092f + Vector3.up * 0.045f * (1 - Mathf.Abs(fr) * 0.5f);
                var side = Vector3.Cross(Vector3.up, radial).normalized;
                int b0 = v.Count;
                for (int k = 0; k <= seg; k++)
                {
                    float u = k / (float)seg;
                    var p = root + radial * (0.04f * Mathf.Sin(Mathf.Min(1, u * 3) * 1.4f)) + Vector3.down * (L * u) + side * (Rn() - 0.5f) * 0.03f * u;
                    if (fr > 0.2f) p += F * 0.035f * Mathf.Min(1, u * 2.5f);   // ด้านหน้าห้อยคลุมหน้า ไม่จมเข้าไปในหัว
                    float ww = 0.016f * (1 - u * 0.6f);
                    v.Add(head.InverseTransformPoint(p - side * ww)); v.Add(head.InverseTransformPoint(p + side * ww));
                    var nn = head.InverseTransformDirection(radial); n.Add(nn); n.Add(nn);
                    if (k > 0) { int i = b0 + k * 2; tri.Add(i - 2); tri.Add(i - 1); tri.Add(i); tri.Add(i - 1); tri.Add(i + 1); tri.Add(i); }
                }
            }
            var m = new Mesh(); m.SetVertices(v); m.SetNormals(n); m.SetTriangles(tri, 0); m.RecalculateBounds(); return m;
        }

        // ดัดท่าหลัง Animator: คอหัก หลังค่อม หัวกระตุกเป็นพัก ๆ (หมุนรอบแกนของตัวผีในโลก ไม่ขึ้นกับแกนกระดูกของโมเดล)
        void PoseGhost()
        {
            if (gAnim == null || state == S.Hidden) return;
            var Rt = ghost.right; var F = ghost.forward;
            void Rot(Transform b, Vector3 axis, float deg) { if (b != null) b.rotation = Quaternion.AngleAxis(deg, axis) * b.rotation; }
            switch (def.kind)
            {
                case GhostBodies.Kind.Drowned: Rot(hSpine, Rt, 30); Rot(hChest, Rt, 15); Rot(hHead, Rt, -45); Rot(hHead, F, 18); break;   // ค่อมต่ำ เงยหน้าจ้อง
                case GhostBodies.Kind.Preta: Rot(hChest, Rt, 22); Rot(hHead, Rt, 35); if (hHead) hHead.localScale = Vector3.one * 0.55f; break;   // ก้มมองลงมา หัวเล็ก
                case GhostBodies.Kind.LotusMaiden: Rot(hNeck, F, 22); Rot(hHead, Rt, 24); Rot(hHead, F, 12); break;   // ก้มหน้า ผมปิด เอียงคอ
                case GhostBodies.Kind.NetFiend: Rot(hChest, Rt, 18); Rot(hHead, Rt, 20); Rot(hHead, F, -16); break;
                case GhostBodies.Kind.RootSiren: Rot(hNeck, F, 40); Rot(hHead, F, 32); Rot(hHead, Rt, 8); break;   // คอหักพับข้าง
                case GhostBodies.Kind.Headless: if (hHead) hHead.localScale = Vector3.zero; Rot(hSpine, Rt, 8); break;
                case GhostBodies.Kind.MudWraith: Rot(hHead, Rt, -75); Rot(hChest, Rt, -20); break;   // ร่างคลาน: เงยหน้าขึ้นมองตรง ๆ
                case GhostBodies.Kind.Hanging: Rot(hNeck, F, 45); Rot(hHead, F, 20); Rot(hHead, Rt, 30); break;   // คอพับ
            }
            // หัวกระตุก: นิ่ง… แล้วสะบัดแรงวูบหนึ่ง
            twitchT -= Time.deltaTime;
            if (twitchT <= 0) { twitchT = Random.Range(0.7f, 2.6f); twitchLeft = 0.13f; twitchAxis = Random.value < 0.5f ? Rt : F; twitchDeg = Random.Range(-28f, 28f); }
            if (twitchLeft > 0) { twitchLeft -= Time.deltaTime; Rot(hHead, twitchAxis, twitchDeg); }
            // บางขณะร่างหยุดนิ่งสนิทเหมือนภาพค้าง
            gAnim.speed = Mathf.PerlinNoise(Time.time * 0.7f, 4.2f) > 0.62f ? 0 : 0.3f;
            if (rope != null && hNeck != null) { rope.SetPosition(0, hNeck.position); rope.SetPosition(1, hNeck.position + Vector3.up * 3.2f); }
        }

        static bool IsNight => Story.Phase == StoryPhase.Fishing && (Story.S.time >= NightFrom || Story.S.time < 5f);

        void Update()
        {
            if (fishing == null) return;
            bool night = IsNight && !hud.ModalOpen;
            float dt = Time.deltaTime;
            var cam = Camera.main ? Camera.main.transform : null;
            // ตะเกียง/ไฟฉาย
            lantern.enabled = IsNight;
            if (lantern.enabled)
            {
                var p = fishing.player.Pos; lantern.transform.position = p + Vector3.up * 1.3f + fishing.player.Forward * 0.35f;
                float fl = 1 - Fear * 0.7f * Mathf.PerlinNoise(Time.time * 9, 0.3f);   // ผีใกล้ = ตะเกียงวูบวาบ
                lantern.intensity = 1.6f * fl;
            }
            if (!IsNight) FlashOn = false;
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame && IsNight) FlashOn = !FlashOn;
            if (FlashOn && Battery <= 0) { FlashOn = false; hud.Toast("ไฟฉายแบตหมด…"); }
            flash.enabled = FlashOn && cam != null;
            if (flash.enabled) { flash.transform.SetPositionAndRotation(cam.position, cam.rotation); Battery = Mathf.Max(0, Battery - dt * 4f); }

            if (cineT >= 0 && (hud.ModalOpen || hud.CardOpen)) cineT = cineLen;   // มีหน้าต่างเด้งขึ้น → จบฉากผีทันที
            if (cineRestore && cineCam != null) { cineRestore = false; cineCam.transform.SetPositionAndRotation(cineBasePos, cineBaseRot); }   // คืนตำแหน่งกล้องก่อนกล้องหลักคำนวณเฟรมนี้
            if (!IsNight) revealedTonight = false;
            if (!IsNight) { if (state != S.Hidden) Hide(); Sanity = Mathf.MoveTowards(Sanity, 100, dt * 10); Battery = 100; Publish(false, cam); return; }
            if (nightAnnounced == 0 && def != null) { nightAnnounced = 1; hud.Toast($"ค่ำแล้ว… ระวังให้ดี ที่นี่มี{def.name}"); }
            if (def == null) { Sanity = Mathf.MoveTowards(Sanity, 100, dt * 2); Publish(night, cam); return; }   // บ่อหลังบ้าน: ไม่มีผี
            Step(dt, cam);
            // เกจสติ
            float drain = Fear * 7f * def.fear * (Story.S.amulets > 0 ? 0.55f : 1f);
            if (state == S.Hidden) Sanity = Mathf.Min(100, Sanity + dt * 2.5f); else Sanity -= drain * dt;
            if (Sanity <= 0)
            {
                if (Story.S.amulets > 0) { Story.S.amulets--; Story.Save(); Sanity = 45; hud.Toast("สายสิญจน์ขาดสะบั้น… แต่ช่วยดึงสติไว้ได้"); Leave(30); }
                else { Sanity = 100; Hide(); cooldown = 20; director?.Faint(def.name); }
            }
            Publish(night, cam);
        }

        float Fear
        {
            get
            {
                if (def == null || ghost == null || state == S.Hidden) return 0;
                var p = fishing.player.Pos;
                float range = def.kind == GhostBodies.Kind.Preta ? 30 : 16;
                float d = def.kind == GhostBodies.Kind.Hanging ? Vector3.Distance(p + Vector3.up * 1.5f, pos + Vector3.up * 1.4f) : Vector2.Distance(new Vector2(p.x, p.z), new Vector2(pos.x, pos.z));
                return Mathf.Clamp01(1 - d / range) * alpha;
            }
        }

        void Publish(bool night, Transform cam)
        {
            float fear = Fear;
            FishingController.Haunt = Active ? 1 : 0;
            fishing.GaugeWobble = fear;
            fishing.GhostPull = def != null && def.kind == GhostBodies.Kind.NetFiend && fear > 0.55f ? 0.4f : 0;
            heart.volume = Mathf.Lerp(heart.volume, night ? Mathf.Clamp01(fear * 1.2f + (100 - Sanity) / 250f) : 0, Time.deltaTime * 3);
            heart.pitch = 0.85f + fear * 0.7f + (100 - Sanity) / 300f;
            if (heart.volume > 0.02f && !heart.isPlaying) heart.Play(); else if (heart.volume <= 0.02f && heart.isPlaying) heart.Stop();
            string warn = def != null && (state == S.Approach || state == S.Near) ? $"ระวัง! สติกำลังลดลง… ({def.name}กำลังขยับเข้ามา)" : null;
            hud.Night(IsNight, Sanity, fear, warn, FlashOn, Battery);
            // หิ่งห้อย/แสงน้ำเรืองหรี่ตามผี (นางพรายราก · ผีหัวขาด)
            bool dims = def != null && (def.kind == GhostBodies.Kind.RootSiren || def.kind == GhostBodies.Kind.Headless);
            foreach (var ps in dimPs) { var em = ps.emission; em.rateOverTimeMultiplier = dims ? Mathf.Max(0.05f, 1 - fear * 1.4f) * 1 : 1; }
            for (int i = 0; i < dimLights.Count; i++) dimLights[i].intensity = dimLightBase[i] * (dims ? Mathf.Max(0.1f, 1 - fear * 1.3f) : 1);
        }

        // ---------- พฤติกรรมผี ----------
        void Step(float dt, Transform cam)
        {
            var p = fishing.player.Pos;
            timer += dt;
            switch (state)
            {
                case S.Hidden:
                    cooldown -= dt;
                    if (cooldown <= 0) Appear(p, cam);
                    break;
                case S.Appearing:
                    alpha = Mathf.MoveTowards(alpha, 1, dt / 1.8f);
                    if (alpha >= 1) { state = S.Approach; timer = 0; }
                    break;
                case S.Approach:
                    Move(p, dt);
                    if (Dist(p) <= def.stop + 0.3f || timer > 22) { state = S.Near; timer = 0; hud.Toast(def.nearLine); }
                    break;
                case S.Near:
                    Move(p, dt);
                    NearEffects(p, cam, dt);
                    if (timer > 9) Leave(Random.Range(18f, 32f));
                    break;
                case S.Leaving:
                    alpha = Mathf.MoveTowards(alpha, 0, dt / 1.4f);
                    if (alpha <= 0) Hide();
                    break;
            }
            if (state == S.Hidden) return;
            // ไฟฉายส่องตรงผี ~1 วิ = ไล่ไปได้
            if (FlashOn && cam != null && state != S.Leaving)
            {
                var to = pos + Vector3.up * 1f - cam.position;
                if (Vector3.Angle(cam.forward, to) < 20 && to.magnitude < 24) { expose += dt; if (expose > 1f) { hud.Toast($"ไล่{def.name}ไปได้แล้ว!"); Leave(40); } }
                else expose = Mathf.Max(0, expose - dt);
            }
            var want = pos + Vector3.up * (def.kind == GhostBodies.Kind.LotusMaiden ? 0.25f + Mathf.Sin(Time.time * 1.3f) * 0.06f
                : gAnim != null && def.kind == GhostBodies.Kind.NetFiend ? -0.7f : gAnim != null && def.kind == GhostBodies.Kind.MudWraith ? 0.22f : 0);
            stutterT -= dt;
            if (state == S.Appearing || stutterT <= 0 || (shownPos - want).sqrMagnitude > 9) { shownPos = want; stutterT = state == S.Approach ? Random.Range(0.06f, 0.32f) : 0; }
            ghost.position = shownPos;
            var look = p - pos; look.y = 0;
            if (look.sqrMagnitude > 0.01f && def.kind != GhostBodies.Kind.Hanging) ghost.rotation = Quaternion.Slerp(ghost.rotation, Quaternion.LookRotation(look), dt * 2);
            if (def.kind == GhostBodies.Kind.Hanging) ghost.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 0.9f) * 6, Time.time * 8, Mathf.Cos(Time.time * 0.7f) * 5);   // แกว่งตามลม
            if (def.kind == GhostBodies.Kind.Preta) ghost.rotation *= Quaternion.Euler(Mathf.Lerp(0, 18, state == S.Near ? Mathf.Clamp01(timer / 4) : 0), 0, 0);   // ก้มหน้าลงมา
            if (gAnim != null && def.kind == GhostBodies.Kind.MudWraith) ghost.rotation = Quaternion.LookRotation(look.sqrMagnitude > 0.01f ? look : ghost.forward) * Quaternion.Euler(80, 0, 0);   // คลานราบไปกับโคลน
            mat.SetFloat("_Alpha", alpha); if (hairMat) hairMat.SetFloat("_Alpha", alpha);
            if (aura) aura.intensity = alpha * (1.6f + Mathf.PerlinNoise(Time.time * 3, 7) * 0.8f);
        }

        float Dist(Vector3 p) => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(pos.x, pos.z));

        void Appear(Vector3 p, Transform cam)
        {
            var fwd = cam != null ? cam.forward : fishing.player.Forward; fwd.y = 0; fwd = fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward;
            if (def.kind == GhostBodies.Kind.Hanging)
            {
                var sp = world.Special; pos = new Vector3(sp.x, world.WaterY + 2.4f, sp.z);   // ใต้ชายคาท่าน้ำเรือนผู้ใหญ่ ค่อย ๆ หย่อนลง
            }
            else
            {
                pos = p + fwd * def.dist;
                for (int i = 0; i < 40; i++)
                {
                    var dir = Quaternion.Euler(0, Random.Range(-40f, 40f), 0) * fwd;   // โผล่ในมุมกล้องให้เห็น
                    var c = p + dir * def.dist * Random.Range(0.85f, 1.1f);
                    float g = world.GroundH(c.x, c.z), wy = world.WaterY;
                    bool ok = def.water ? wy - g > 0.25f : g > wy - 0.3f && g < wy + 0.9f;
                    if (ok) { pos = c; break; }
                }
                pos.y = def.water ? world.WaterY - (def.kind == GhostBodies.Kind.Preta ? 0.3f : def.kind == GhostBodies.Kind.NetFiend ? 0.25f : 0) : world.GroundH(pos.x, pos.z);
            }
            ghost.gameObject.SetActive(true); ghost.position = pos;
            alpha = 0; state = S.Appearing; timer = 0; stare = 0; expose = 0; warpT = 0; stoleOrSnapped = false;
            StartReveal(cam, !revealedTonight);
        }

        void StartReveal(Transform cam, bool full)
        {
            if (cam == null || hud.ModalOpen || hud.CardOpen || cineT >= 0) return;
            revealedTonight = true;
            cineCam = cam.GetComponent<Camera>(); if (cineCam == null) return;
            cineLen = full ? 3.6f : 1.5f; cineT = 0; cineFov = cineCam.fieldOfView;
            alpha = 1; mat.SetFloat("_Alpha", 1); if (hairMat) hairMat.SetFloat("_Alpha", 1); ghost.position = shownPos = pos;
            Time.timeScale = full ? 0.18f : 0.3f; GameInput.Blocked = true;
            hud.GhostReveal(full ? def.name : null, full ? def.legend : null, cineLen, def.dark > 0 ? new Color(0.5f, 1f, 0.6f) : def.color);
            sting.Play();
        }

        void LateUpdate()
        {
            PoseGhost();
            PretaShake();
            if (cineT < 0 || cineCam == null) return;
            cineT += Time.unscaledDeltaTime;
            var c = cineCam.transform;
            cineBasePos = c.position; cineBaseRot = c.rotation; cineRestore = true;   // กล้องหลักวางไว้ตรงนี้ → เฟรมหน้าคืนให้ก่อน (ไม่ให้กล้องไหลตาม)
            float w = Mathf.SmoothStep(0, 1, Mathf.Clamp01(cineT / 0.45f)) * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((cineT - (cineLen - 0.55f)) / 0.55f)));
            float hgt = def.kind == GhostBodies.Kind.Preta ? 4f : def.kind == GhostBodies.Kind.Hanging ? 0.6f : 1.25f;
            var target = hHead != null ? hHead.position + Vector3.up * 0.04f : ghost.position + Vector3.up * hgt;
            var dir = target - cineBasePos;
            c.position = cineBasePos + dir * 0.3f * w + Random.insideUnitSphere * 0.015f * w;   // ดันกล้องเข้าหา + สั่นนิด ๆ
            c.rotation = Quaternion.Slerp(cineBaseRot, Quaternion.LookRotation(dir), w);
            cineCam.fieldOfView = Mathf.Lerp(cineFov, cineFov * 0.62f, w);
            if (cineT >= cineLen)
            {
                cineT = -1; Time.timeScale = 1; cineCam.fieldOfView = cineFov;
                GameInput.Blocked = hud.ModalOpen || hud.CardOpen;
            }
        }

        void OnDisable() { if (cineT >= 0) Time.timeScale = 1; }

        // เสียงตกใจ: ตูม! ต่ำ ๆ + เสียงแหลมไถลลง + ซ่าแตก ๆ
        static AudioClip StingClip()
        {
            int sr = 22050, n = sr * 2; var d = new float[n]; var r = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float boom = Mathf.Sin(2 * Mathf.PI * (48 - t * 12) * t) * Mathf.Exp(-t * 2.2f);
                float shriek = Mathf.Sin(2 * Mathf.PI * (1400 - t * 520) * t + Mathf.Sin(t * 38) * 3) * Mathf.Exp(-t * 1.6f) * 0.22f;
                float hiss = ((float)r.NextDouble() * 2 - 1) * Mathf.Exp(-t * 5f) * 0.25f;
                d[i] = Mathf.Clamp((boom * 0.9f + shriek + hiss) * Mathf.Min(1, t * 200), -1, 1);
            }
            var c = AudioClip.Create("GhostSting", n, 1, sr, false); c.SetData(d, 0); return c;
        }

        void Move(Vector3 p, float dt)
        {
            if (def.kind == GhostBodies.Kind.Hanging) { pos.y = Mathf.MoveTowards(pos.y, world.WaterY + 0.25f, dt * 0.08f); return; }   // ยิ่งดึกยิ่งหย่อนลงมา
            var to = p - pos; to.y = 0; float d = to.magnitude;
            if (d <= def.stop) return;
            to /= d;
            float step;
            if (def.kind == GhostBodies.Kind.LotusMaiden)
            {
                warpT += dt;   // วาร์ปเข้ามาทีละช่วงกระพริบตา
                if (warpT < 2.4f) return;
                warpT = 0; step = Mathf.Min(2.2f, d - def.stop); alpha = 0.25f;
            }
            else if (def.kind == GhostBodies.Kind.Drowned) step = def.speed * dt * (Mathf.Sin(Time.time * 3.1f) > 0 ? 1.6f : 0.15f);   // คลานกระตุก ๆ
            else step = def.speed * dt;
            var n = pos + to * Mathf.Min(step, d - def.stop);
            if (def.water && world.WaterY - world.GroundH(n.x, n.z) < 0.15f) return;   // ผีน้ำไม่ขึ้นบก
            pos.x = n.x; pos.z = n.z;
            if (!def.water) pos.y = Mathf.Max(world.GroundH(pos.x, pos.z), world.WaterY - 0.1f);
            if (def.kind == GhostBodies.Kind.NetFiend) pos.y = world.WaterY - Mathf.Lerp(0.25f, 0.7f, Mathf.Clamp01(1 - d / 6));   // เข้าใกล้แล้วดำลงใต้เรือ
            if (def.kind == GhostBodies.Kind.LotusMaiden) alpha = Mathf.MoveTowards(alpha, 1, dt * 2);
        }

        void NearEffects(Vector3 p, Transform cam, float dt)
        {
            if (cam == null) return;
            switch (def.kind)
            {
                case GhostBodies.Kind.Drowned:   // จ้องตานานเกินไป สายขาด
                    var to = pos + Vector3.up * 0.7f - cam.position;
                    if (fishing.LineOut && Vector3.Angle(cam.forward, to) < 14) stare += dt; else stare = Mathf.Max(0, stare - dt * 0.5f);
                    if (stare > 2.6f && !stoleOrSnapped) { stoleOrSnapped = true; fishing.GhostSnap(); hud.Toast("สบตาพรายหินนานเกินไป… สายเบ็ดขาดผึง!"); Leave(25); }
                    break;
                case GhostBodies.Kind.MudWraith:   // คลานถึงตัว ดึงปลาจากข้องไปหนึ่งตัว
                    if (Dist(p) < 1.8f && !stoleOrSnapped)
                    {
                        stoleOrSnapped = true;
                        if (Story.S.basket.Count > 0) { var i = Random.Range(0, Story.S.basket.Count); var f = Story.S.basket[i]; Story.S.basket.RemoveAt(i); Story.Save(); hud.Toast($"มือเปื้อนโคลนดึง{FishDatabase.Get(f.id)?.name}ออกจากข้องไปแล้ว!"); }
                        else hud.Toast("มือเปื้อนโคลนคว้าข้องว่างเปล่า… แล้วจมหายลงเลน");
                        Leave(25);
                    }
                    break;
            }
        }

        void PretaShake()
        {
            // เปรตกวาดน้ำ: กล้องโคลงเคลงเหมือนเรือโดนคลื่น
            if (def == null || state == S.Hidden || def.kind != GhostBodies.Kind.Preta || Camera.main == null) return;
            float amp = Fear * 0.06f;
            Camera.main.transform.position += new Vector3(Mathf.Sin(Time.time * 2.3f), Mathf.Sin(Time.time * 3.1f) * 0.6f, Mathf.Cos(Time.time * 1.9f)) * amp;
        }

        void Leave(float cool) { if (state == S.Hidden) return; state = S.Leaving; cooldown = cool; }
        void Hide() { state = S.Hidden; alpha = 0; if (ghost) ghost.gameObject.SetActive(false); if (mat) mat.SetFloat("_Alpha", 0); if (cooldown <= 0) cooldown = 15; }

        void OnDestroy() { if (instance == this) instance = null; FishingController.Haunt = 0; }
    }
}
