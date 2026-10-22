using UnityEngine;

namespace CozyFishing
{
    // มือของตัวละครตอนตกปลา (ต้องเปิด IK Pass ที่เลเยอร์ — CharacterSetup):
    //  · มือขวากำรอบด้ามคันเบ็ดจริง ๆ (งอนิ้วด้วยกล้ามเนื้อ Humanoid) + คันเบ็ดใหม่ (ด้ามไม้ก๊อก รอกสปินนิ่ง ปลายเรียว) วางกลางกำมือ ไม่ทะลุฝ่ามือ
    //  · มือซ้ายเอื้อมไปกำสายเหนือปากปลาตอนยกโชว์ (IK + งอนิ้ว)
    [RequireComponent(typeof(Animator))]
    public class HandIK : MonoBehaviour
    {
        public Vector3 target;
        public float weight;
        public Transform rodTip;            // ปลายคันเดิม (สายเบ็ดผูกตรงนี้) — คันใหม่ยืดไปให้ถึงจุดนี้
        Animator anim;
        HumanPoseHandler hph; HumanPose pose;
        int[] rightFingers, leftFingers, rightThumb, leftThumb;
        Transform rod, blank, reel;

        void Awake() => anim = GetComponent<Animator>();

        void Start()
        {
            if (anim.avatar == null || !anim.isHuman) { enabled = false; return; }
            hph = new HumanPoseHandler(anim.avatar, anim.transform);
            rightFingers = Muscles("Right", false); leftFingers = Muscles("Left", false);
            rightThumb = Muscles("Right", true); leftThumb = Muscles("Left", true);
            BuildRod();
        }

        static int[] Muscles(string side, bool thumb)
        {
            var l = new System.Collections.Generic.List<int>();
            for (int i = 0; i < HumanTrait.MuscleCount; i++)
            {
                var n = HumanTrait.MuscleName[i];
                if (n.StartsWith(side) && n.Contains("Stretched") && n.Contains("Thumb") == thumb && (thumb || n.Contains("Index") || n.Contains("Middle") || n.Contains("Ring") || n.Contains("Little"))) l.Add(i);
            }
            return l.ToArray();
        }

        void OnAnimatorIK(int layer)
        {
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
            if (weight > 0.001f) anim.SetIKPosition(AvatarIKGoal.LeftHand, target);
        }

        void LateUpdate()
        {
            if (hph == null) return;
            // งอนิ้ว: -1 = กำแน่น · มือขวากำด้ามคันตลอด · มือซ้ายกำตามน้ำหนัก IK (ตอนจับปลา)
            hph.GetHumanPose(ref pose);
            foreach (var i in rightFingers) pose.muscles[i] = -0.95f;
            foreach (var i in rightThumb) pose.muscles[i] = -0.45f;
            foreach (var i in leftFingers) pose.muscles[i] = Mathf.Lerp(pose.muscles[i], -0.9f, weight);
            foreach (var i in leftThumb) pose.muscles[i] = Mathf.Lerp(pose.muscles[i], -0.5f, weight);
            hph.SetHumanPose(ref pose);
            PlaceRod();
        }

        // จุดกลางกำมือขวา: เฉลี่ยข้อมือ ข้อนิ้วกลางทั้งสามข้อ และปลายนิ้วโป้ง (โพรงที่ด้ามคันผ่าน)
        Vector3 Grip()
        {
            var hand = anim.GetBoneTransform(HumanBodyBones.RightHand).position;
            Vector3 P(HumanBodyBones b) { var t = anim.GetBoneTransform(b); return t ? t.position : hand; }
            return (hand + P(HumanBodyBones.RightMiddleProximal) + P(HumanBodyBones.RightMiddleIntermediate) + P(HumanBodyBones.RightMiddleDistal) + P(HumanBodyBones.RightThumbDistal)) / 5f;
        }

        void PlaceRod()
        {
            if (rod == null || rodTip == null) return;
            var grip = Grip(); var tip = rodTip.position;
            var dir = tip - grip; float len = dir.magnitude;
            if (len < 0.2f) return;
            dir /= len;
            var butt = grip - dir * 0.16f;                         // ด้ามโผล่หลังกำมือนิดหน่อย
            rod.SetPositionAndRotation(butt, Quaternion.LookRotation(dir, Vector3.up));
            blank.localScale = new Vector3(1, 1, len + 0.16f);
            reel.localPosition = new Vector3(0, -0.035f, 0.26f);   // รอกห้อยใต้คันหน้ากำมือ
        }

        // ---------- คันเบ็ดสร้างด้วยโค้ด (ยาว 1 หน่วยตามแกน Z · ยืดตามระยะถึงปลายคันจริง) ----------
        void BuildRod()
        {
            var oldRod = rodTip != null ? rodTip.parent : null;
            if (oldRod != null) foreach (var r in oldRod.GetComponentsInChildren<Renderer>()) r.enabled = false;   // ซ่อนคันเหลี่ยมแบบเดิม
            rod = new GameObject("RodReal").transform;
            blank = new GameObject("Blank").transform; blank.SetParent(rod, false);
            blank.gameObject.AddComponent<MeshFilter>().sharedMesh = RodMesh();
            blank.gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { Resources.Load<Material>("Rod/Cork"), Resources.Load<Material>("Rod/Metal"), Resources.Load<Material>("Rod/Blank") };
            reel = new GameObject("Reel").transform; reel.SetParent(rod, false);
            reel.gameObject.AddComponent<MeshFilter>().sharedMesh = ReelMesh();
            reel.gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { Resources.Load<Material>("Rod/Metal"), Resources.Load<Material>("Rod/Blank") };
        }

        // ช่วงตามความยาว (0..1): ก้นด้าม · ด้ามไม้ก๊อก · ที่ล็อกรอก (โลหะ) · ด้ามหน้า · ตัวคันเรียวไปปลาย
        static Mesh RodMesh()
        {
            var v = new System.Collections.Generic.List<Vector3>();
            var tris = new[] { new System.Collections.Generic.List<int>(), new System.Collections.Generic.List<int>(), new System.Collections.Generic.List<int>() };
            (float z, float r, int sub)[] prof = {
                (0f, 0.006f, 1), (0.004f, 0.017f, 1), (0.012f, 0.017f, 0), (0.13f, 0.015f, 0), (0.135f, 0.012f, 1), (0.2f, 0.012f, 1),
                (0.205f, 0.013f, 0), (0.27f, 0.011f, 0), (0.275f, 0.0075f, 2), (0.6f, 0.0045f, 2), (1f, 0.0014f, 2) };
            const int S = 10;
            int prev = -1;
            for (int k = 0; k < prof.Length; k++)
            {
                int start = v.Count;
                for (int i = 0; i <= S; i++) { float a = i / (float)S * Mathf.PI * 2; v.Add(new Vector3(Mathf.Cos(a) * prof[k].r, Mathf.Sin(a) * prof[k].r, prof[k].z)); }
                if (prev >= 0)
                    for (int i = 0; i < S; i++) { int a = prev + i, b = start + i; tris[prof[k].sub].AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 }); }
                prev = start;
            }
            var m = new Mesh { name = "Rod" }; m.SetVertices(v); m.subMeshCount = 3;
            for (int s = 0; s < 3; s++) m.SetTriangles(tris[s], s);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        // รอกสปินนิ่งเล็ก ๆ: ขาจับ · ตัวรอก · หลอดสาย · มือหมุน
        static Mesh ReelMesh()
        {
            var v = new System.Collections.Generic.List<Vector3>(); var metal = new System.Collections.Generic.List<int>(); var dark = new System.Collections.Generic.List<int>();
            void Cyl(Vector3 c, Vector3 axis, float r, float h, System.Collections.Generic.List<int> tri)
            {
                var q = Quaternion.FromToRotation(Vector3.forward, axis.normalized); const int S = 12; int s0 = v.Count;
                for (int e = 0; e < 2; e++) for (int i = 0; i <= S; i++) { float a = i / (float)S * Mathf.PI * 2; v.Add(c + q * new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, (e - 0.5f) * h)); }
                for (int i = 0; i < S; i++) { int a = s0 + i, b = s0 + S + 1 + i; tri.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 }); }
                int c0 = v.Count; v.Add(c - q * Vector3.forward * h * 0.5f); int c1 = v.Count; v.Add(c + q * Vector3.forward * h * 0.5f);
                for (int i = 0; i < S; i++) { tri.AddRange(new[] { c0, s0 + i + 1, s0 + i }); tri.AddRange(new[] { c1, s0 + S + 1 + i, s0 + S + 2 + i }); }
            }
            Cyl(new Vector3(0, 0.012f, 0), Vector3.up, 0.004f, 0.03f, metal);                 // ขา
            Cyl(new Vector3(0, -0.012f, 0), Vector3.forward, 0.022f, 0.035f, dark);            // ตัวรอก
            Cyl(new Vector3(0, -0.012f, 0.03f), Vector3.forward, 0.018f, 0.024f, metal);       // หลอดสาย
            Cyl(new Vector3(0.03f, -0.012f, 0), Vector3.right, 0.003f, 0.03f, metal);          // แกนมือหมุน
            Cyl(new Vector3(0.045f, -0.03f, 0), Vector3.right, 0.006f, 0.012f, dark);          // ปุ่มมือหมุน
            var m = new Mesh { name = "Reel" }; m.SetVertices(v); m.subMeshCount = 2;
            m.SetTriangles(metal, 0); m.SetTriangles(dark, 1); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        void OnDestroy() { if (rod) Destroy(rod.gameObject); }
    }
}
