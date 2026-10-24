using System.Collections;
using System.Linq;
using UnityEngine;

namespace CozyFishing
{
    // ฉากเปิดด่านแบบตัวอย่างหนัง (เล่นครั้งแรกที่มาถึงด่าน · แตะเพื่อข้าม) — คำนวณจากข้อมูลด่านทั้งหมด ด่านใหม่ได้ฉากเปิดเองโดยไม่ต้องวางกล้อง
    //  1 กล้องบินโฉบสูงวนรอบจุดเด่นของด่าน (world.Special) ลดระดับลงมาเฉียดผิวน้ำ
    //  2 เลียดผิวน้ำ: ปลาประจำด่านกระโดดพ้นน้ำทีละตัว · เงาดำปลาตำนานว่ายผ่านใต้ผิวน้ำ
    //  3 แถบดำแบบภาพยนตร์ + ชื่อด่าน + คำโปรย + ปลาเด่น → ตัดเข้าเกม
    public static class StageIntro
    {
        static string Key(string id) => "abot.intro." + id;
        public static bool ShouldPlay(string id)
        {
            var url = GameUrl.Value ?? "";
            if (url.Contains("intro")) return true;            // ทดสอบ / อัดวิดีโอโปรโมต: ?intro
            if (FishingController.Bot) return false;
            try { return PlayerPrefs.GetInt(Key(id), 0) == 0; } catch (System.Exception) { return false; }
        }
        static void MarkSeen(string id) { try { PlayerPrefs.SetInt(Key(id), 1); PlayerPrefs.Save(); } catch (System.Exception) { } }

        public static bool Playing;   // ถ่ายคลิปโปรโมต: อัดช่วงฉากเปิดด่าน
        public static IEnumerator Play(IWorld w, CameraRig rig, Hud hud, FishingController fishing)
        {
            Playing = true;
            var st = Stages.Get(w.StageId);
            MarkSeen(w.StageId);
            var all = FishDatabase.ForStage(w.StageId).ToList();
            var stars = all.Where(s => !s.rare).OrderByDescending(s => s.stars).Take(3).ToList();
            var legend = all.FirstOrDefault(s => s.rare) ?? all.OrderByDescending(s => s.stars).FirstOrDefault();
            bool skip = false;
            hud.IntroOpen(st.name, st.desc, string.Join(" · ", stars.Select(s => s.name)), () => skip = true);
            // ดนตรีไทยประจำด่าน: ระนาดเอก ฉิ่งฉับ ฆ้อง (ทำนองต่างกันทุกด่าน)
            var music = new GameObject("IntroMusic").AddComponent<AudioSource>();
            music.clip = ThaiMusic.Ranat(w.StageId.GetHashCode(), 13f); music.volume = 0.75f; music.spatialBlend = 0; music.Play();
            GameInput.Blocked = true; hud.Cinematic(true);

            // ---------- จุดอ้างอิงจากข้อมูลด่าน ----------
            var focus = w.Special; if (focus == Vector3.zero) focus = w.Spawn + Quaternion.Euler(0, w.SpawnYaw, 0) * Vector3.forward * 8;
            focus.y = w.WaterY;
            var spawn = w.Spawn; spawn.y = w.WaterY;
            var away = spawn - focus; away.y = 0; if (away.sqrMagnitude < 1) away = Vector3.back * 10; away.Normalize();
            float R = Mathf.Clamp(Vector3.Distance(spawn, focus) + 6, 10, 26);

            // ---------- ช็อต 1: บินโฉบวนรอบจุดเด่น ลดระดับลง ----------
            float t = 0, T1 = 7f;
            while (t < T1 && !skip)
            {
                float k = t / T1, e = Mathf.SmoothStep(0, 1, k);
                var dir = Quaternion.Euler(0, Mathf.Lerp(-110, 25, e), 0) * -away;   // วนจากด้านข้างไปจบฝั่งตรงข้ามผู้เล่น
                var pos = focus + dir * Mathf.Lerp(R * 1.8f, R * 0.9f, e) + Vector3.up * Mathf.Lerp(R * 1.1f, 3.2f, e);
                rig.Override(pos, focus + Vector3.up * Mathf.Lerp(0f, 1.2f, e), false);
                t += Time.deltaTime; yield return null;
            }

            // ---------- ช็อต 2: เลียดผิวน้ำ ปลากระโดด · เงาปลาตำนาน ----------
            var side = Vector3.Cross(Vector3.up, away);
            var camA = focus - away * 7f + side * 3f + Vector3.up * 0.7f;
            var camB = focus - away * 4.5f - side * 1.5f + Vector3.up * 0.5f;
            GameObject shadow = null;
            if (legend != null)
            {
                shadow = fishing.MakeFish(legend, 1);
                shadow.transform.localScale = Vector3.one * 2.2f;
                var dark = new Material(Resources.Load<Material>("Rod/Cork")); dark.SetColor("_BaseColor", new Color(0.02f, 0.025f, 0.03f));
                foreach (var r in shadow.GetComponentsInChildren<Renderer>()) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = dark; r.sharedMaterials = ms; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            }
            var jumpers = stars.Select(s => fishing.MakeFish(s, 1)).ToList();
            foreach (var j in jumpers) { j.transform.localScale = Vector3.one * 0.55f; j.SetActive(false); }
            t = 0; float T2 = 5.5f; int lastJump = -1;
            hud.IntroCard(true);
            while (t < T2 && !skip)
            {
                float k = t / T2;
                var cam = Vector3.Lerp(camA, camB, Mathf.SmoothStep(0, 1, k));
                rig.Override(cam, focus + Vector3.up * 0.5f + away * 2f, false);
                // ปลากระโดดทีละตัว เป็นโค้งพ้นน้ำ
                for (int i = 0; i < jumpers.Count; i++)
                {
                    float jt = (t - 0.4f - i * 1.4f) / 1.1f;
                    var j = jumpers[i];
                    bool on = jt > 0 && jt < 1; if (j.activeSelf != on) j.SetActive(on);
                    var start = focus - away * 1.2f + side * (i - 1) * 2.2f;
                    if (on)
                    {
                        if (lastJump < i) { lastJump = i; WaterFx.AddRipple(start.x, start.z, 1.2f); }
                        var p = start + side * jt * 1.6f + Vector3.up * (Mathf.Sin(jt * Mathf.PI) * 1.3f - 0.1f);
                        j.transform.position = p;
                        j.transform.rotation = Quaternion.LookRotation(-away, Vector3.up) * Quaternion.Euler(0, 90, Mathf.Lerp(50, -50, jt));
                        if (jt > 0.92f) WaterFx.AddRipple(p.x, p.z, 1f);
                    }
                }
                if (shadow != null)
                {
                    var sp = focus - away * 2.2f + side * Mathf.Lerp(5, -5, k) + Vector3.down * 0.35f;
                    shadow.transform.SetPositionAndRotation(sp, Quaternion.LookRotation(-side, Vector3.up) * Quaternion.Euler(0, 90, Mathf.Sin(t * 3) * 8));
                }
                t += Time.deltaTime; yield return null;
            }
            foreach (var j in jumpers) Object.Destroy(j);
            if (shadow != null) Object.Destroy(shadow);
            // ---------- ตัดเข้าเกม ----------
            hud.IntroClose(); hud.Cinematic(false);
            if (music != null) { music.Stop(); Object.Destroy(music.gameObject); }
            rig.ClearOverride(); rig.Snap();
            GameInput.Blocked = hud.ModalOpen || hud.CardOpen;
            Playing = false;
        }
    }
}
