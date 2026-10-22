using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CozyFishing
{
    // ลำดับเรื่องราวของแต่ละวัน (เทียบ openMap/goFishing/goHome/evening ของ main.js):
    //   เช้า (หน้าบ้าน: ดวง/ราคาตลาด/เป้าหมาย) → ออกไปตกปลา (นาฬิกาเดิน 6 โมง–6 โมงเย็น) → ข้องเต็ม/ค่ำ/กดกลับบ้าน
    //   → มื้อเย็นที่บ้าน (เลือกปิ้งหรือขาย) → บทสนทนาครอบครัว → จ่ายซ่อมหลังคา → นอน → เช้าวันใหม่
    public class StoryDirector : MonoBehaviour
    {
        public FishingController fishing;
        public PlayerController player;
        public CameraRig rig;
        public Hud hud;
        public DayCycle sky;
        public GameObject roofLeak;                 // หยดน้ำ/ถัง/แอ่งน้ำใต้หลังคารั่ว (หายเมื่อซ่อมแล้ว)
        // ตัวละครยืนข้างบ้าน (ไม่บังกล้อง) · กล้องถอยสูงมองเห็นทั้งบ้าน หลังคารั่ว และตัวละคร
        public Vector3 homeStand = new Vector3(-7.3f, 0, -7.4f), homeCam = new Vector3(-3.2f, 4.6f, -1.6f), homeLook = new Vector3(-9.6f, 1.2f, -8.0f);

        readonly System.Random rng = new System.Random();
        bool warned, fullAsked, busy;
        float saveT;
        IWorld world;

        public void Begin(IWorld w)
        {
            world = w;
            // ทดสอบ: ?bot&day2 ข้ามไปเช้าวันที่ 2 (เปิดคดี) ทันทีเมื่อเริ่มเซฟใหม่
            if (FishingController.Bot && Application.absoluteURL.Contains("day2") && Story.S.day == 1 && Story.Phase == StoryPhase.New) { Story.S.day = 2; Story.Phase = StoryPhase.Morning; }
            // หลายด่าน: บ้านอยู่ในฉากบ่อทุ่งนา · ตกปลาด่านอื่นโหลดฉากของด่านนั้น
            bool home = roofLeak != null;
            if (Story.Phase == StoryPhase.Fishing && Story.S.stage != w.StageId) { SceneManager.LoadScene(Stages.Current.scene); return; }
            if (Story.Phase != StoryPhase.Fishing && !home) { SceneManager.LoadScene("Paddy"); return; }
            switch (Story.Phase)
            {
                case StoryPhase.Fishing: StartCoroutine(ResumeFishing()); break;
                case StoryPhase.Evening: StartCoroutine(Evening(false)); break;
                case StoryPhase.Morning: StartCoroutine(Morning("เช้าวันที่ " + Story.S.day, false)); break;
                default: StartCoroutine(Morning("บ้านไม้หลังเล็ก ริมทุ่งนา…", true)); break;
            }
        }

        float Speed => FishingController.Bot ? 10f : 1f;    // โหมดทดสอบ: วันผ่านเร็ว 10 เท่า

        void Update()
        {
            hud.SetStatus(Story.FmtTime(Story.S.time), Story.S.day, Story.S.basket.Count, Story.Capacity, Story.S.money, Story.Phase == StoryPhase.Fishing);
            if (Story.Phase != StoryPhase.Fishing || busy || hud.CardOpen || hud.ModalOpen) return;
            Story.S.time = Mathf.Min(Story.S.time + Time.deltaTime * Story.HoursPerSec * Speed, Story.DayEnd + 0.25f);   // ระหว่างสู้ปลาตอนหกโมงเย็น เวลาไม่เลยไปจนมืด
            sky.Apply(Story.S.time, Story.CurrentLuck.sun);
            if (Story.S.time >= 17 && !warned) { warned = true; hud.Toast("ใกล้ค่ำแล้ว อีกชั่วโมงต้องกลับบ้าน"); }
            if (Story.S.time >= Story.DayEnd && !fishing.Busy)
            {
                Story.S.time = Story.DayEnd;
                hud.Toast("หกโมงเย็นแล้ว กลับบ้านกันเถอะ");
                GoHome();
                return;
            }
            if ((saveT += Time.deltaTime) > 5) { saveT = 0; Story.Save(); }
        }

        // ---------- ระหว่างตกปลา ----------
        /// ระบบตกปลาเรียกหลังเก็บปลาลงข้อง (ข้องเพิ่งเต็ม → ถามครั้งเดียวต่อวัน)
        public void OnFishKept()
        {
            if (!Story.BasketFull || fullAsked) return;
            fullAsked = true;
            hud.Ask($"ได้ปลาเต็มข้อง {Story.S.basket.Count} ตัวแล้ว (ขายได้ราว {Story.Baht(Story.BasketValue)}) จะทำอะไรต่อดี?",
                ("กลับบ้าน", GoHome),
                ("ตกต่อ (ได้ตัวใหม่แลกตัวที่ถูกสุดได้)", () => hud.Toast("ตกต่อได้เลย! ได้ตัวใหม่จะเลือกแลกกับตัวที่ถูกสุด")));
        }

        /// ปุ่ม "กลับบ้าน" บนจอ
        public void RequestHome()
        {
            if (Story.Phase != StoryPhase.Fishing || busy) return;
            if (fishing.Busy) { hud.Toast("ดึงปลาตัวนี้ขึ้นมาก่อนนะ!"); return; }
            if (Story.S.time < 15) hud.Ask("ยังไม่ค่ำเลย จะกลับบ้านเลยไหม?", ("กลับบ้าน", GoHome), ("ตกต่อ", null));
            else GoHome();
        }

        void GoHome() { if (!busy) StartCoroutine(Evening(true)); }

        IEnumerator ResumeFishing()
        {
            busy = true;
            yield return hud.Fade(Stages.Get(world.StageId).name + " · " + Story.FmtTime(Story.S.time), () => SetupFishing(false));
            hud.Toast($"{Stages.Get(world.StageId).name}\n{Story.CurrentLuck.text}");
            busy = false;
        }

        void SetupFishing(bool reset)
        {
            fishing.Paused = false;
            fishing.ResetLine();
            if (reset) { player.Pos = world.Spawn; player.Pos.y = world.StandY(player.Pos.x, player.Pos.z); player.Yaw = world.SpawnYaw; }
            rig.ClearOverride();
            rig.Snap();
            if (roofLeak) roofLeak.SetActive(!Story.RoofFixed);
            sky.Apply(Story.S.time, Story.CurrentLuck.sun);
            Story.Phase = StoryPhase.Fishing;
        }

        // ---------- เช้า ----------
        IEnumerator Morning(string title, bool intro)
        {
            busy = true;
            yield return hud.Fade(title, () => ShowHome(6.2f));
            if (intro) yield return hud.Dialog(Story.Intro);
            // สืบคดี: เช้าวันที่ 2 ลุงเปี๊ยกมาเล่าเรื่องแสงทองใต้กอบัว
            if (!Mystery.Started && Story.S.day >= 2)
            {
                yield return hud.Dialog(Mystery.Current.intro);
                Mystery.Start();
                hud.Toast("เปิดคดีใหม่! ดู \"สมุดคดี\" มุมขวาบน");
            }
            else if (Mystery.ReadyToSolve) yield return hud.Dialog(new[] { ("ลูก", "พ่อ เบาะแสครบแล้วนะ! เปิดสมุดคดีแล้วไขปริศนากันเถอะ") });
            Story.Phase = StoryPhase.Morning;
            string dest = null;
            hud.ShowMorning(id => dest = id, PayGoal);
            while (dest == null) yield return null;
            warned = false; fullAsked = false;
            Story.S.time = Story.DayStart;
            Story.S.stage = dest;
            var st = Stages.Get(dest);
            if (dest == world.StageId)
            {
                yield return hud.Fade(st.go, () => SetupFishing(true));
                hud.Toast($"{st.name}\n{Story.CurrentLuck.text}");
                busy = false;
            }
            else
            {
                Story.Phase = StoryPhase.Fishing;
                yield return hud.Fade(st.go, () => SceneManager.LoadScene(st.scene));
            }
        }

        void PayGoal()
        {
            var g = Story.CurrentGoal;
            if (g == null || !Story.PayGoal()) return;
            if (roofLeak) roofLeak.SetActive(!Story.RoofFixed);
            StartCoroutine(PaidLines(g));
        }
        IEnumerator PaidLines(Goal g) { yield return hud.Dialog(new[] { ("แม่", g.done), ("ลูก", "เย้! ขอบคุณนะพ่อ") }); hud.RefreshMorning(); }

        // ---------- ไขคดี (เรียกจากสมุดคดี) ----------
        public void TrySolve()
        {
            var c = Mystery.Current;
            if (!Mystery.ReadyToSolve) return;
            var opts = new (string, System.Action)[c.options.Length + 1];
            for (int i = 0; i < c.options.Length; i++)
            {
                int k = i;
                opts[i] = (c.options[i], () => StartCoroutine(Answer(k)));
            }
            opts[c.options.Length] = ("ขอคิดดูก่อน", null);
            hud.Ask(c.question, opts);
        }

        IEnumerator Answer(int option)
        {
            if (!Mystery.Answer(option)) { yield return hud.Dialog(new[] { ("ลูก", "อืม… หนูว่ายังไม่ใช่นะพ่อ"), ("ลูก", Mystery.Current.wrong) }); yield break; }
            yield return hud.Dialog(Mystery.Current.solved);
            hud.Toast($"ไขคดีสำเร็จ! ได้ตะกร้าแห่งวันพรุ่งนี้ (ข้องใส่ได้ 14 ตัว) + {Story.Baht(Mystery.Current.rewardMoney)}");
        }

        // ---------- มื้อเย็น ----------
        IEnumerator Evening(bool travel)
        {
            busy = true;
            fishing.ResetLine();
            fishing.Paused = true;
            Story.Phase = StoryPhase.Evening;
            if (roofLeak == null)   // ตกปลาด่านอื่นอยู่ → เดินทางกลับฉากบ้าน (บ่อทุ่งนา)
            {
                Story.S.stage = "paddy";
                yield return hud.Fade(Stages.Get(world.StageId).back, () => SceneManager.LoadScene("Paddy"));
                yield break;
            }
            yield return hud.Fade(travel ? Stages.Get(Story.S.stage).back : "กลับถึงบ้านแล้ว…", () => ShowHome(Mathf.Max(Story.S.time, 17.6f)));
            Story.S.stage = "paddy";
            bool[] choice = null;
            hud.ShowEvening(c => choice = c);
            while (choice == null) yield return null;
            var r = Story.SettleEvening(choice);
            var lines = new System.Collections.Generic.List<(string, string)>(Story.EveningLines[r.mood]);
            if (r.earned > 0) lines.Add(("แม่", $"ปลาที่เหลือเอาไปขายตลาดได้ {Story.Baht(r.earned)} (มีเงินเก็บ {Story.Baht(Story.S.money)})"));
            if (!Story.RoofFixed) lines.Add(("แม่", Story.S.money >= Story.Goals[0].cost ? "เงินพอซ่อมหลังคาแล้วนะพ่อ! พรุ่งนี้เช้าจ่ายช่างได้เลย" : $"หลังคายังรั่วอยู่เลย เก็บเงินอีก {Story.Baht(Story.Goals[0].cost - Story.S.money)} ก็ซ่อมได้แล้ว"));
            if (Story.S.happy < 30) lines.Add(("แม่", "ช่วงนี้ลูกกินไม่ค่อยอิ่มเลย… พรุ่งนี้แบ่งปลามาปิ้งบ้างนะพ่อ"));
            // ผ่าท้องปลาที่ปิ้ง: อาจเจอเบาะแส · ปิ้งครบเงื่อนไขปลดล็อกความลับของปลา
            var clue = Mystery.OnGrill(r.grilledFish);
            if (clue != null) { lines.Add(("ลูก", "พ่อ! แม่! ดูนี่สิ เจออะไรในท้องปลาด้วย")); lines.Add(("แม่", clue.found)); }
            foreach (var sec in Mystery.CheckGrillSecrets(r.grilledFish)) lines.Add(("ลูก", $"หนูรู้ความลับของ{FishDatabase.Get(sec.species)?.name}แล้ว! {sec.text}"));
            if (Mystery.ReadyToSolve) lines.Add(("ลูก", "เบาะแสครบห้าอย่างแล้ว! เปิดสมุดคดีแล้วไขปริศนากันเถอะพ่อ"));
            yield return hud.Dialog(lines.ToArray());
            bool sleep = false;
            hud.Ask("ได้เวลาพักผ่อนแล้ว", ("นอนหลับ", () => sleep = true));
            while (!sleep) yield return null;
            Story.Sleep(rng);
            StartCoroutine(Morning($"ราตรีสวัสดิ์…  เช้าวันที่ {Story.S.day}", false));
        }

        // ตัวละครยืนหน้าบ้าน กล้องมองบ้านไม้ (เห็นหลังคารั่ว) แสงตามช่วงเวลา
        void ShowHome(float hour)
        {
            fishing.Paused = true;
            fishing.ResetLine();
            player.Pos = new Vector3(homeStand.x, 0, homeStand.z);
            player.Pos.y = world.StandY(player.Pos.x, player.Pos.z);
            player.Yaw = Quaternion.LookRotation(new Vector3(homeCam.x, 0, homeCam.z) - homeStand).eulerAngles.y + 35;   // หันมาทางกล้องเฉียง ๆ
            player.Pose = 0;
            rig.Override(homeCam, homeLook);
            if (roofLeak) roofLeak.SetActive(!Story.RoofFixed);
            Story.S.time = hour;
            sky.Apply(hour >= 12 ? Mathf.Max(hour, 18.55f) : hour, Story.CurrentLuck.sun);   // มื้อเย็น: แดดอัสดงส้มต่ำ
        }
    }
}
