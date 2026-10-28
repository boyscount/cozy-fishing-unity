using UnityEngine;

namespace CozyFishing
{
    // แพไม้ไผ่ที่บ่อหลังบ้าน (ลุงส้มให้ยืมพร้อมแห): จอดข้างท่าน้ำ · เดินไปใกล้ ๆ แล้วกด "ขึ้นแพ"
    //   บนแพ: จอย/WASD = ถ่อแพ (มีแรงเฉื่อย ลอยไปต่ออีกนิดแล้วหยุด) · ตกเบ็ด/ทอดแหจากแพได้ตามปกติ
    //   แพไม่เกยตลิ่ง (เช็กรอบแพว่ายังเป็นน้ำ ชนแล้วเด้งออกเบา ๆ) · ใกล้ฝั่งกด "ลงแพ" = ขึ้นฝั่งตรงนั้น แพจอดรอที่เดิม
    [DefaultExecutionOrder(-50)]   // LateUpdate ก่อนกล้อง: ตัวละครกับแพขยับพร้อมกัน กล้องไม่ตามช้าไปหนึ่งเฟรม
    public class Raft : MonoBehaviour
    {
        public static Raft Current;
        public static bool Aboard => Current != null && Current.aboard;

        const float Accel = 2.2f, MaxSpeed = 1.7f, Drag = 1.1f, Foot = 1.35f, BoardDist = 2.6f, DeckY = 0.14f;
        FishingController c;
        IWorld world;
        Transform raft;
        Vector3 pos, vel, moor; float yaw, moorYaw;
        bool aboard;
        public Vector3 Pos => pos;

        public void Init(FishingController fc)
        {
            c = fc; world = fc.world; Current = this;
            Build();
            FindMooring();
            ResetToDock();
        }
        void OnDestroy() { if (Current == this) Current = null; if (raft) Destroy(raft.gameObject); }

        public bool Available => CastNet.Owned;
        public bool Near => Available && !aboard && Vector2.Distance(new Vector2(c.player.Pos.x, c.player.Pos.z), new Vector2(pos.x, pos.z)) < BoardDist;

        // ---------- ขึ้น/ลง ----------
        public void Board()
        {
            if (!Near || c.State != FishState.Free) return;
            aboard = true; c.player.Riding = true; vel = Vector3.zero;
            Sfx.Plop(); WaterFx.AddRipple(pos.x, pos.z, 1f);
            c.hud.Toast("ขึ้นแพแล้ว · จอย/WASD ถ่อแพไปหาฝูงปลา");
        }

        /// จุดขึ้นฝั่งใกล้แพ (ไม่มี = ยังลงไม่ได้)
        public bool LandingPoint(out Vector3 p)
        {
            p = default; float best = 99;
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2;
                for (float d = Foot + 0.2f; d <= Foot + 1.6f; d += 0.35f)
                {
                    float x = pos.x + Mathf.Cos(a) * d, z = pos.z + Mathf.Sin(a) * d;
                    if (!world.Walkable(x, z)) continue;
                    if (d < best) { best = d; p = new Vector3(x, world.StandY(x, z), z); }
                    break;
                }
            }
            return best < 99;
        }

        public void Leave()
        {
            if (!aboard || c.State != FishState.Free || !LandingPoint(out var p)) return;
            aboard = false; c.player.Riding = false; vel = Vector3.zero;
            c.player.Pos = p; c.player.transform.position = p;
            c.hud.Toast("ขึ้นฝั่งแล้ว แพจอดรอตรงนี้");
        }

        /// วันใหม่/กลับบ้าน: ลงแพ แพกลับไปจอดข้างท่าน้ำ
        public void ResetToDock()
        {
            if (aboard && c != null) { aboard = false; c.player.Riding = false; }
            pos = moor; yaw = moorYaw; vel = Vector3.zero;
            Place();
        }

        // ---------- ทุกเฟรม ----------
        void Update()
        {
            if (raft == null) return;
            bool show = Available && !c.Paused;
            if (raft.gameObject.activeSelf != show) raft.gameObject.SetActive(show);
            if (!show) { if (aboard) ResetToDock(); return; }
            float dt = Time.deltaTime;
            if (FishingController.Bot && GameUrl.Value.Contains("raft")) BotStep();
            if (aboard && c.State == FishState.Free)
            {
                var mv = botMove.sqrMagnitude > 0 ? botMove : GameInput.Move;
                if (mv.sqrMagnitude > 0.0025f)
                {
                    var dir = Quaternion.Euler(0, CameraRig.ViewYaw, 0) * new Vector3(mv.x, 0, mv.y);
                    vel += dir * Accel * dt;
                    if (Random.value < dt * 3f) WaterFx.AddRipple(pos.x - dir.x * Foot, pos.z - dir.z * Foot, 0.4f);   // ไม้ถ่อจ้วงน้ำท้ายแพ
                }
            }
            vel -= vel * Mathf.Min(1, Drag * dt);
            if (vel.magnitude > MaxSpeed) vel = vel.normalized * MaxSpeed;
            if (vel.sqrMagnitude > 1e-5f)
            {
                var next = pos + vel * dt;
                if (Fits(next.x, next.z)) pos = next;
                else if (Fits(next.x, pos.z)) { pos.x = next.x; vel.z *= -0.3f; }
                else if (Fits(pos.x, next.z)) { pos.z = next.z; vel.x *= -0.3f; }
                else vel *= -0.3f;   // ชนตลิ่ง เด้งออกเบา ๆ
                if (vel.sqrMagnitude > 0.04f) yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(vel.x, vel.z) * Mathf.Rad2Deg, Mathf.Min(1, dt * 0.8f));   // หัวแพค่อย ๆ หันตามทางที่ไป
            }
            Place();
        }

        // ทดสอบ (?bot&raft): ขึ้นแพเอง ถ่อออกกลางบ่อ 5 วิ แล้วให้บอททอดแห/ตกเบ็ดจากแพ
        Vector2 botMove; float botT;
        void BotStep()
        {
            botT += Time.deltaTime;
            if (!aboard && botT > 2f && c.State == FishState.Free && Available) { c.player.Pos = new Vector3(pos.x, c.player.Pos.y, pos.z - 1f); Board(); botT = 2f; }
            if (!aboard) return;
            var to = new Vector3(PaddyWorld.Pond.x - pos.x, 0, PaddyWorld.Pond.y - pos.z);
            var local = Quaternion.Euler(0, -CameraRig.ViewYaw, 0) * to.normalized;
            botMove = botT < 7f && to.magnitude > 2f ? new Vector2(local.x, local.z) : Vector2.zero;
        }

        void LateUpdate()
        {
            if (!aboard) return;
            var p = DeckTop;
            c.player.Pos = p; c.player.transform.position = p;
        }

        Vector3 DeckTop => new Vector3(pos.x, world.WaterY + DeckY + Bob, pos.z);
        float Bob => Mathf.Sin(Time.time * 1.3f) * 0.02f;

        void Place()
        {
            if (raft == null) return;
            raft.SetPositionAndRotation(new Vector3(pos.x, world.WaterY + Bob, pos.z),
                Quaternion.Euler(Mathf.Sin(Time.time * 1.1f) * 1.2f, yaw, Mathf.Sin(Time.time * 0.9f + 1) * 1.5f));
        }

        /// รอบแพยังเป็นน้ำทั้งหมด (ไม่เกยตลิ่ง ไม่ทับท่าน้ำ)
        bool Fits(float x, float z)
        {
            if (!world.CanCast(x, z)) return false;
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2;
                if (!world.CanCast(x + Mathf.Cos(a) * Foot, z + Mathf.Sin(a) * Foot)) return false;
            }
            return true;
        }

        void FindMooring()
        {
            // ข้างท่าน้ำ (ฝั่งซ้ายของท่า · ฝั่งขวามีเรือพายจอดอยู่) แล้วค่อยหาไกลออกไปทางกลางบ่อ
            var sp = world.Spawn;
            Vector3[] tries = { new Vector3(-5.1f, 0, 2.6f), new Vector3(-3.2f, 0, 6f), new Vector3(-5.4f, 0, 4f), new Vector3(-1.2f, 0, 6.4f) };
            foreach (var t in tries) if (Fits(t.x, t.z)) { moor = t; moorYaw = 0; return; }
            for (float d = 3; d < 14; d += 0.5f)
            {
                var t = new Vector3(sp.x, 0, sp.z + d);
                if (Fits(t.x, t.z)) { moor = t; moorYaw = 0; return; }
            }
            moor = new Vector3(PaddyWorld.Pond.x, 0, PaddyWorld.Pond.y);
        }

        // ---------- แพไม้ไผ่ (ไม้ไผ่ 8 ลำวางเรียง + ไม้ขวาง 2 ลำ) ----------
        void Build()
        {
            var cyl = c.aim.GetComponentInChildren<MeshFilter>().sharedMesh;   // ทรงกระบอกจากวงเล็งในฉาก
            var baseMat = Resources.Load<Material>("Rod/Cork");
            Material Mat(Color col) { var m = new Material(baseMat); m.SetColor("_BaseColor", col); return m; }
            var bamboo = Mat(new Color(0.74f, 0.6f, 0.34f)); var dark = Mat(new Color(0.5f, 0.38f, 0.2f)); var rope = Mat(new Color(0.55f, 0.46f, 0.3f));
            raft = new GameObject("BambooRaft").transform;
            void Log(Vector3 p, Vector3 s, Vector3 rot, Material m)
            {
                var g = new GameObject("Bamboo"); g.transform.SetParent(raft, false);
                g.transform.localPosition = p; g.transform.localRotation = Quaternion.Euler(rot); g.transform.localScale = s;
                g.AddComponent<MeshFilter>().sharedMesh = cyl; g.AddComponent<MeshRenderer>().sharedMaterial = m;
            }
            for (int i = 0; i < 8; i++)
            {
                float x = -0.7f + i * 0.2f, len = 1.3f + (i % 3) * 0.04f;
                Log(new Vector3(x, 0.03f, (i % 2) * 0.05f), new Vector3(0.18f, len, 0.18f), new Vector3(90, 0, 0), i % 3 == 1 ? dark : bamboo);
            }
            foreach (float z in new[] { -0.95f, 0.95f })
            {
                Log(new Vector3(0, 0.13f, z), new Vector3(0.1f, 0.85f, 0.1f), new Vector3(0, 0, 90), dark);
                for (int i = 0; i < 4; i++) Log(new Vector3(-0.6f + i * 0.4f, 0.13f, z), new Vector3(0.06f, 0.06f, 0.06f), Vector3.zero, rope);   // ปมเชือกมัด
            }
            Log(new Vector3(0.62f, 0.2f, 0), new Vector3(0.05f, 1.6f, 0.05f), new Vector3(90, 8, 0), dark);   // ไม้ถ่อวางบนแพ
            foreach (var r in raft.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }
}
