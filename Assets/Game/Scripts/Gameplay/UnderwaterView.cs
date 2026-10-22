using UnityEngine;

namespace CozyFishing
{
    // มองใต้น้ำ: ทุ่นลอยรอปลา → กล้องดำลงไปข้างเหยื่อ เห็นสายเบ็ด เหยื่อ ปลาว่ายมาดู/งับ (PondLife) · ผิวน้ำด้านล่างพลิ้ว หมอกเขียวขุ่น ตะกอนลอย
    // ปลาติดเบ็ด (ตีเบ็ด/สู้ปลา) → กล้องโผล่กลับขึ้นผิวน้ำ · น้ำตื้นเกินไปไม่ดำ
    public class UnderwaterView : MonoBehaviour
    {
        public static bool Enabled = true;
        IWorld world; FishingController fishing; CameraRig rig;
        System.Collections.Generic.List<GameObject> unders = new System.Collections.Generic.List<GameObject>(); GameObject bait; ParticleSystem motes; LineRenderer leader;
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
            bait = GameObject.CreatePrimitive(PrimitiveType.Capsule); bait.name = "Bait";
            Destroy(bait.GetComponent<Collider>());
            bait.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>("Water/Bait");
            bait.transform.localScale = new Vector3(0.025f, 0.045f, 0.025f);
            bait.SetActive(false);
            leader = new GameObject("Leader").AddComponent<LineRenderer>();
            leader.sharedMaterial = Resources.Load<Material>("Water/Leader"); leader.widthMultiplier = 0.004f; leader.positionCount = 2; leader.enabled = false;
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
        }

        bool Want()
        {
            if (!Enabled || unders.Count == 0 || noSpot) return false;
            var s = fishing.State;
            if (s != FishState.Waiting && s != FishState.Bite) return false;
            var b = fishing.BobberPos;
            return world.DepthAt(b.x, b.z) > 0.55f;
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
            bait.transform.position = b; bait.transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 3) * 25, Time.time * 20, 90);
            leader.SetPosition(0, bob); leader.SetPosition(1, b);
            motes.transform.position = Camera.main ? Camera.main.transform.position + Camera.main.transform.forward * 1.2f : b;
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
        }

        void Exit()
        {
            active = false;
            RenderSettings.fogMode = savedMode; RenderSettings.fogDensity = savedDensity; RenderSettings.fogStartDistance = savedStart; RenderSettings.fogEndDistance = savedEnd;
            foreach (var u in unders) u.SetActive(false); bait.SetActive(false); leader.enabled = false; motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            rig.ClearOverride();
        }
    }
}
