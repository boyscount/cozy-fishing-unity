using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CozyFishing
{
    // ชีวิตในบ่อ: ฝูงปลาจริง (โมเดลจากภาพถ่าย) ว่ายวนใต้ผิวน้ำ มองทะลุน้ำเห็นจากฝั่ง
    //  · เหวี่ยงทุ่นลงน้ำ → ปลาใกล้ ๆ ตกใจพุ่งหนี
    //  · ก่อนปลากินเหยื่อ → ปลาชนิดที่จะติดเบ็ดว่ายเข้ามาวนรอบเหยื่อ แล้วพุ่งงับตอนทุ่นจม · ติดเบ็ดแล้วตัวนี้หายไป (กลายเป็นปลาบนสาย)
    // สร้างตอนเริ่มฉาก (GameRoot) ไม่ต้องอบลงฉาก
    public class PondLife : MonoBehaviour
    {
        class Swimmer
        {
            public GameObject go; public FishSpecies sp; public Vector3 pos, target; public float speed, depth, flee, len; public Vector3 dir = Vector3.forward;
        }

        IWorld world; FishingController fishing;
        readonly List<Swimmer> school = new List<Swimmer>();
        Swimmer biter;
        FishState lastState;
        readonly System.Random rng = GameUrl.Rng();
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        public Vector3 BaitPos => fishing.BobberPos + Vector3.down * 0.45f;   // เหยื่อห้อยใต้ทุ่น

        public void Init(IWorld w, FishingController f)
        {
            world = w; fishing = f;
            var species = FishDatabase.ForStage(w.StageId).Where(s => s.Photo != null && !s.rare).ToList();
            if (species.Count == 0) return;
            int n = 14;
            for (int i = 0; i < n; i++)
            {
                // ปลาธรรมดาเยอะกว่าปลาหายาก (ตามโอกาสติดเบ็ด)
                var sp = Pick(species);
                if (!RandomWater(w.Spawn, 22, 0.45f, out var p)) continue;
                school.Add(Make(sp, p));
            }
        }

        FishSpecies Pick(List<FishSpecies> l)
        {
            float tot = l.Sum(s => s.chance), r = R(0, tot);
            foreach (var s in l) { r -= s.chance; if (r <= 0) return s; }
            return l[0];
        }

        Swimmer Make(FishSpecies sp, Vector3 p)
        {
            float len = Mathf.Clamp(R(sp.cmMin, sp.cmMax) / 100f * 1.4f, 0.2f, 1.2f);   // ขยายนิดให้มองเห็นจากฝั่ง
            var go = fishing.MakeFish(sp, len / 1.15f);
            go.name = "Swim_" + sp.id;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var mpb = new MaterialPropertyBlock(); mpb.SetFloat("_Phase", R(0, 6.28f)); mpb.SetFloat("_Wiggle", 0.05f); r.SetPropertyBlock(mpb);
            }
            var s = new Swimmer { go = go, sp = sp, pos = p, target = p, len = len, speed = R(0.25f, 0.6f), depth = R(0.15f, 0.4f) };   // ว่ายตื้น ๆ ให้มองเห็นจากฝั่ง
            NewTarget(s);
            return s;
        }

        bool RandomWater(Vector3 around, float radius, float minDepth, out Vector3 p)
        {
            for (int t = 0; t < 40; t++)
            {
                var c = around + new Vector3(R(-radius, radius), 0, R(-radius, radius));
                if (world.CanCast(c.x, c.z) && world.DepthAt(c.x, c.z) > minDepth) { p = new Vector3(c.x, world.WaterY, c.z); return true; }
            }
            p = around; return false;
        }

        void NewTarget(Swimmer s)
        {
            if (RandomWater(s.pos, 6, 0.4f, out var p)) s.target = p;
        }

        void Update()
        {
            if (fishing == null || school.Count == 0) return;
            var st = fishing.State;
            float dt = Time.deltaTime;
            // ทุ่นตกน้ำ: ปลารอบ ๆ ตกใจหนี
            if (st == FishState.Waiting && lastState != FishState.Waiting && lastState != FishState.Bite)
                foreach (var s in school)
                    if ((s.pos - fishing.BobberPos).sqrMagnitude < 9)
                    {
                        var away = s.pos - fishing.BobberPos; away.y = 0;
                        s.target = s.pos + away.normalized * 5; s.flee = 1.4f;
                    }
            // ปลากินแล้วหนีไป (ตีเบ็ดช้า) เกมสุ่มปลาตัวใหม่ → ปล่อยตัวเดิมไป ให้ชนิดที่จะติดจริงว่ายเข้ามาแทน
            if (biter != null && st == FishState.Waiting && biter.sp != fishing.HookedSpecies) biter = null;
            // ใกล้เวลาปลากิน: ปลาชนิดที่จะติดเบ็ดว่ายมาที่เหยื่อ
            if (st == FishState.Waiting && biter == null && fishing.TimeToBite > 0 && fishing.TimeToBite < 4.5f && fishing.HookedSpecies != null)
            {
                var sp = fishing.HookedSpecies;
                biter = school.Where(s => s.sp == sp && s.go.activeSelf).OrderBy(s => (s.pos - fishing.BobberPos).sqrMagnitude).FirstOrDefault();
                if (biter == null && sp.Photo != null && RandomWater(fishing.BobberPos, 5, 0.4f, out var p0)) { biter = Make(sp, p0); school.Add(biter); }
            }
            // ติดเบ็ด/ตกได้/เก็บสาย: ปลาตัวที่งับเหยื่อหายไปเป็นปลาบนสาย แล้วค่อยกลับมาว่ายที่อื่น
            if (biter != null && (st == FishState.Strike || st == FishState.Fight || st == FishState.Caught)) { biter.go.SetActive(false); }
            if (biter != null && (st == FishState.Free || st == FishState.Retrieve || st == FishState.Charging))
            {
                if (!biter.go.activeSelf && RandomWater(world.Spawn, 20, 0.45f, out var p1)) { biter.pos = p1; biter.go.SetActive(true); }
                biter = null;
            }
            lastState = st;

            foreach (var s in school)
            {
                if (!s.go.activeSelf) continue;
                Vector3 goal = s.target; float spd = s.speed;
                if (s == biter && (st == FishState.Waiting || st == FishState.Bite))
                {
                    var bait = BaitPos;
                    if (st == FishState.Bite) { goal = bait; spd = 1.6f; }                                  // งับ!
                    else
                    {
                        float a = Time.time * 0.8f + s.len * 10;                                            // วนดูเหยื่อ
                        goal = bait + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (0.5f + s.len);
                        spd = 0.35f;
                    }
                }
                else if (s.flee > 0) { s.flee -= dt; spd *= 3.5f; }
                var to = goal - s.pos; to.y = 0;
                if (to.magnitude < 0.3f && s != biter) NewTarget(s);
                var want = to.sqrMagnitude > 1e-4f ? to.normalized : s.dir;
                s.dir = Vector3.Slerp(s.dir, want, Mathf.Min(1, dt * 2.2f)).normalized;
                var next = s.pos + s.dir * spd * dt;
                if (world.CanCast(next.x, next.z) && world.DepthAt(next.x, next.z) > 0.3f) s.pos = next; else NewTarget(s);
                float d = Mathf.Min(s.depth, world.DepthAt(s.pos.x, s.pos.z) - 0.12f);
                if (s == biter && (st == FishState.Waiting || st == FishState.Bite)) d = 0.45f;
                float y = world.WaterY - Mathf.Max(0.12f, d) + Mathf.Sin(Time.time * 0.7f + s.len * 20) * 0.03f;
                s.go.transform.SetPositionAndRotation(new Vector3(s.pos.x, y, s.pos.z), Quaternion.LookRotation(s.dir) * Quaternion.Euler(0, -90, 0));   // หัวโมเดลอยู่ +X
            }
        }

        void OnDestroy() { foreach (var s in school) if (s.go) Destroy(s.go); }
    }
}
