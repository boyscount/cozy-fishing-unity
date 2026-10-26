using UnityEngine;

namespace CozyFishing
{
    // แม่น้ำโขง: ลูกไฟพญานาค (บั้งไฟพญานาค) ลอยขึ้นจากกลางแม่น้ำเป็นระยะ ไร้เสียง สีแดงอมส้ม
    //   ปกติทุก 10–20 วิ · บอสพร้อมโผล่ (สัปดาห์นี้ยังไม่มีใครจับ) ถี่ขึ้น · ปลาบึกยักษ์ติดเบ็ด ลอยขึ้นทีละหลายลูก
    public class MekongFx : MonoBehaviour
    {
        IWorld world; FishingController fishing; ParticleSystem balls; float nextAt;

        public static void Build(IWorld w, FishingController f) => new GameObject("MekongFx").AddComponent<MekongFx>().Init(w, f);

        void Init(IWorld w, FishingController f)
        {
            world = w; fishing = f; nextAt = Time.time + 4;
            balls = new GameObject("NagaFireballs").AddComponent<ParticleSystem>(); balls.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = balls.main; m.startLifetime = 7f; m.startSpeed = 0; m.startSize = 0.55f; m.maxParticles = 40; m.simulationSpace = ParticleSystemSimulationSpace.World; m.playOnAwake = false; m.loop = false;
            var e = balls.emission; e.enabled = false;
            var v = balls.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World;
            v.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f); v.y = new ParticleSystem.MinMaxCurve(3.2f, 4.6f); v.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            var c = balls.colorOverLifetime; c.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.55f, 0.2f), 0), new GradientColorKey(new Color(1f, 0.25f, 0.12f), 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.08f), new GradientAlphaKey(1, 0.75f), new GradientAlphaKey(0, 1) });
            c.color = g;
            var s = balls.sizeOverLifetime; s.enabled = true; s.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1f, 1, 0.55f));
            var r = balls.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Resources.Load<Material>("Water/Mote"); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var trail = balls.trails; trail.enabled = true; trail.lifetime = 0.35f; trail.widthOverTrail = 0.4f; trail.dieWithParticles = true;
            r.trailMaterial = r.sharedMaterial;
        }

        void Update()
        {
            if (Time.time < nextAt) return;
            bool hooked = fishing != null && fishing.BossHooked && fishing.LineOut;
            int n = hooked ? Random.Range(2, 5) : 1;
            nextAt = Time.time + (hooked ? Random.Range(2.5f, 4.5f) : Boss.Eligible ? Random.Range(7f, 13f) : Random.Range(12f, 22f));
            for (int i = 0; i < n; i++)
            {
                var ep = new ParticleSystem.EmitParams { applyShapeToPosition = false, position = new Vector3(Random.Range(-22f, 22f), world.WaterY + 0.2f, Random.Range(14f, 40f)) };
                ep.startColor = new Color(1f, 0.5f, 0.2f) * 2.2f; ep.startSize = Random.Range(0.45f, 0.75f);
                balls.Emit(ep, 1);
            }
        }
    }
}
