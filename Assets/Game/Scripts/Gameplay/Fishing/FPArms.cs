using UnityEngine;

namespace CozyFishing
{
    // แขนมุมบุคคลที่หนึ่งสำหรับงมปลา: แขน+มือที่ตัดจากโมเดลตัวละคร (Resources/FPArms/fp_arms · ทำใน Blender จาก remy_idle)
    //   วางไหล่ไว้ใต้กล้อง · ยื่นมือไปหาเป้าด้วย IK สองข้อ (ไหล่→ศอก→ข้อมือ) ที่คำนวณเอง · งอนิ้วตามจังหวะ (คลำ = แบ · บีบปลา = กำ)
    //   วัสดุยืมจากตัวละครในฉาก (ผิว/เสื้อ) ไม่ต้องมีเท็กซ์เจอร์ซ้ำ
    sealed class FPArms
    {
        class Side { public Transform up, lo, hand; public Transform[][] fingers; public float a, b; public int sign; }
        readonly GameObject go;
        readonly Side L, R;
        float scale = 1;
        Transform[] all; Quaternion[] rest;   // ท่าเริ่มต้น (T-pose) — คืนทุกเฟรมก่อนคำนวณ ไม่ให้นิ้ว/ข้อหมุนสะสม

        FPArms(GameObject g)
        {
            go = g;
            L = Bind("Left", 1); R = Bind("Right", -1);
            if (L.up != null && R.up != null)
            {
                float w = Vector3.Distance(L.up.position, R.up.position);
                if (w > 0.001f) { scale = 0.36f / w; go.transform.localScale *= scale; }   // ไหล่ห่างกันราว 36 ซม.
            }
            Measure(L); Measure(R);
            all = go.GetComponentsInChildren<Transform>(true); rest = new Quaternion[all.Length];
            for (int i = 0; i < all.Length; i++) rest[i] = all[i].localRotation;
        }

        public static FPArms Create(PlayerController player)
        {
            var prefab = Resources.Load<GameObject>("FPArms/fp_arms");
            if (prefab == null) return null;
            var g = Object.Instantiate(prefab); g.name = "FPArms";
            foreach (var r in g.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                r.updateWhenOffscreen = true; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // วัสดุผิว/เสื้อจากตัวละครในฉาก (ชื่อเมชเดียวกัน Body / Tops)
                foreach (var src in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (src.name == r.name) { r.sharedMaterials = src.sharedMaterials; break; }
            }
            g.SetActive(false);
            return new FPArms(g);
        }

        Side Bind(string side, int sign)
        {
            Transform F(string n) { foreach (var t in go.GetComponentsInChildren<Transform>(true)) if (t.name.EndsWith(":" + n) || t.name == n) return t; return null; }
            var s = new Side { up = F(side + "Arm"), lo = F(side + "ForeArm"), hand = F(side + "Hand"), sign = sign };
            string[] names = { "Thumb", "Index", "Middle", "Ring", "Pinky" };
            s.fingers = new Transform[names.Length][];
            for (int i = 0; i < names.Length; i++) s.fingers[i] = new[] { F(side + "Hand" + names[i] + "1"), F(side + "Hand" + names[i] + "2"), F(side + "Hand" + names[i] + "3") };
            return s;
        }

        static void Measure(Side s)
        {
            if (s.up == null || s.lo == null || s.hand == null) return;
            s.a = Vector3.Distance(s.up.position, s.lo.position); s.b = Vector3.Distance(s.lo.position, s.hand.position);
        }

        public bool Active { get => go.activeSelf; set { if (go.activeSelf != value) go.SetActive(value); } }

        /// วางแขน: ไหล่ใต้กล้อง · มือไปที่ handL/handR · curl 1 = แบนิ้ว, -1 = กำ · roll = หมุนข้อมือให้ฝ่ามือคว่ำ
        public void Pose(Vector3 camPos, Vector3 camFwd, Vector3 handL, Vector3 handR, float curl, float roll = 0)
        {
            if (L.up == null || R.up == null) return;
            for (int i = 0; i < all.Length; i++) all[i].localRotation = rest[i];
            var fw = camFwd; fw.y = 0; fw = fw.sqrMagnitude > 1e-4f ? fw.normalized : Vector3.forward;
            go.transform.rotation = Quaternion.LookRotation(fw, Vector3.up);
            var rgt = Vector3.Cross(Vector3.up, fw);
            if (Vector3.Dot(L.up.position - R.up.position, rgt) > 0) go.transform.rotation = Quaternion.LookRotation(-fw, Vector3.up);   // โมเดลหันกลับด้าน: หมุน 180° ให้ไหล่ซ้ายอยู่ซ้ายจอ
            // ไหล่ทั้งสองอยู่ใต้กล้อง ~18 ซม. ถอยหลังนิด → แขนโผล่จากขอบล่างจอ
            var mid = (L.up.position + R.up.position) * 0.5f;
            go.transform.position += camPos - Vector3.up * 0.2f - fw * 0.08f - mid;
            // มือเอื้อมไม่ถึงเป้า: เลื่อนไหล่ไปหน้าเท่าที่ขาด (ไม่เกิน 25 ซม. ไหล่ยังอยู่ใต้กล้อง ไม่โผล่ให้เห็น)
            float need = Mathf.Max(Vector3.Distance(L.up.position, handL) - (L.a + L.b) * 0.92f, Vector3.Distance(R.up.position, handR) - (R.a + R.b) * 0.92f);
            if (need > 0) go.transform.position += fw * Mathf.Min(need, 0.25f);
            var right = Vector3.Cross(Vector3.up, fw);
            float lo = -1;   // ไหล่ซ้ายอยู่ซ้ายจอเสมอ (หมุนโมเดลไว้แล้ว)
            Solve(L, handL, L.up.position + right * (0.35f * lo) - Vector3.up * 0.25f, curl, roll);
            Solve(R, handR, R.up.position - right * (0.35f * lo) - Vector3.up * 0.25f, curl, -roll);
        }

        // IK สองข้อ: ศอกงอไปทาง hint (ด้านนอกล่าง) · ข้อมือชี้ตามแขนแล้วก้มลงนิด · นิ้วงอรอบแกนข้างมือ
        void Solve(Side s, Vector3 target, Vector3 hint, float curl, float roll)
        {
            var sh = s.up.position; var d = target - sh;
            float dist = Mathf.Clamp(d.magnitude, 0.05f, (s.a + s.b) * 0.999f); d = d.normalized;
            float cosA = Mathf.Clamp((s.a * s.a + dist * dist - s.b * s.b) / (2 * s.a * dist), -1, 1), ang = Mathf.Acos(cosA);
            var n = Vector3.Cross(d, hint - sh); if (n.sqrMagnitude < 1e-6f) n = Vector3.up; n.Normalize();
            var bend = Vector3.Cross(n, d).normalized;
            if (Vector3.Dot(bend, hint - sh) < 0) bend = -bend;
            var elbow = sh + d * (s.a * cosA) + bend * (s.a * Mathf.Sin(ang));
            s.up.rotation = Quaternion.FromToRotation(s.lo.position - sh, elbow - sh) * s.up.rotation;
            var wrist = sh + d * dist;
            s.lo.rotation = Quaternion.FromToRotation(s.hand.position - s.lo.position, wrist - s.lo.position) * s.lo.rotation;
            // ข้อมือ: นิ้วกลางชี้ไปข้างหน้าตามแขน ก้มลงเล็กน้อย
            var mid1 = s.fingers[2][0];
            if (mid1 != null)
            {
                var want = ((wrist - elbow).normalized + Vector3.down * 0.35f).normalized;
                s.hand.rotation = Quaternion.FromToRotation(mid1.position - s.hand.position, want) * s.hand.rotation;
                s.hand.rotation = Quaternion.AngleAxis(roll, want) * s.hand.rotation;
            }
            // นิ้ว: แกนงอ = แนวข้างมือ (นิ้วชี้→นิ้วก้อย)
            var idx = s.fingers[1][0]; var pk = s.fingers[4][0];
            if (idx == null || pk == null) return;
            var side = (pk.position - idx.position).normalized;
            float bendDeg = Mathf.Lerp(8f, 70f, (1 - curl) * 0.5f);
            for (int f = 0; f < s.fingers.Length; f++)
                for (int j = 0; j < 3; j++)
                {
                    var t = s.fingers[f][j]; if (t == null) continue;
                    float k = f == 0 ? 0.5f : 1f;   // นิ้วโป้งงอน้อยกว่า
                    t.rotation = Quaternion.AngleAxis(-bendDeg * k * s.sign, side) * t.rotation;   // งอเข้าหาฝ่ามือ
                }
        }

        public string Debug(Vector3 tl) => $"reach={L.a + L.b:0.00} dist={Vector3.Distance(L.up.position, tl):0.00} Lx={Vector3.Dot(L.up.position - R.up.position, go.transform.right):0.00} scale={scale:0.000}";
        public void Destroy() { if (go) Object.Destroy(go); }
    }
}
