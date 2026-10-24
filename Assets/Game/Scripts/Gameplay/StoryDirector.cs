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
        bool warned, fullAsked, busy, nightAsked;
        bool sneaked; int prayDay = -1;   // แอบออกไปตกปลาตอนดึก (กลับมาเข้านอนเลย) · สวดมนต์ได้คืนละครั้ง
        public static bool NightMode;   // เลือกตกปลากลางคืนต่อ (ถึงเที่ยงคืน)
        const float NightEnd = 23.9f;
        float saveT;
        IWorld world;

        public static bool TitleDone;   // แสดงหน้าแรกครั้งเดียวต่อการเปิดเกม (กลับหน้าแรกได้จากหน้าเช้า)
        public void Begin(IWorld w)
        {
            world = w;
            if (!TitleDone && !FishingController.Bot) { StartCoroutine(Title()); return; }
            // ทดสอบ: ?bot&day2 ข้ามไปเช้าวันที่ 2 (เปิดคดี) ทันทีเมื่อเริ่มเซฟใหม่
            if (FishingController.Bot && Application.absoluteURL.Contains("day2") && Story.S.day == 1 && Story.Phase == StoryPhase.New) { Story.S.day = 2; Story.Phase = StoryPhase.Morning; }
            // หลายด่าน: บ้านอยู่ในฉากบ่อทุ่งนา · ตกปลาด่านอื่นโหลดฉากของด่านนั้น
            bool home = roofLeak != null;
            if (Story.Phase == StoryPhase.Fishing && Story.S.stage != w.StageId)
            {
                var st = Stages.Current;
                StageLoader.Go(st.scene, p => hud.FadeProgress(st.name, p), _ => { Story.S.stage = w.StageId; Begin(w); });   // โหลดไม่ได้ → ตกปลาบ่อบ้านแทน
                return;
            }
            if (Story.Phase != StoryPhase.Fishing && !home) { StageLoader.Go("Paddy"); return; }
            switch (Story.Phase)
            {
                case StoryPhase.Fishing: StartCoroutine(ResumeFishing()); break;
                case StoryPhase.Evening: StartCoroutine(Evening(false)); break;
                case StoryPhase.Morning: StartCoroutine(Morning("เช้าวันที่ " + Story.S.day, false, true)); break;
                default: StartCoroutine(Morning("บ้านไม้หลังเล็ก ริมทุ่งนา…", true, true)); break;
            }
        }

        // ---------- หน้าแรก ----------
        bool testDex;
        IEnumerator Title()
        {
            fishing.Paused = true;
            while (true)
            {
                string pick = null;
                string info = Story.HasStorySave ? $"คดีที่ {Mystery.M.chapter + 1} · วันที่ {Story.S.day}" : null;
                hud.ShowTitle(info, k => pick = k);
                if (!testDex && Application.absoluteURL.Contains("opendex")) { testDex = true; pick = "dex"; }
                while (pick == null) yield return null;
                if (pick == "dex") { hud.OpenDexFromTitle(); continue; }
                if (pick == "settings") { hud.ShowSettings(); continue; }
                if (pick == "new" && Story.HasStorySave)
                {
                    bool? ok = null;
                    hud.Ask("เริ่มการเดินทางใหม่? ความคืบหน้าเนื้อเรื่อง เงิน และคดีจะเริ่มต้นใหม่ (สมุดสะสมปลายังอยู่)", ("เริ่มใหม่", () => ok = true), ("ยกเลิก", () => ok = false));
                    while (ok == null) yield return null;
                    if (ok == false) continue;
                }
                if (pick == "new") { Story.Reset(); Mystery.ResetStory(); }
                if (pick == "free") Story.EnterFreePlay();
                break;
            }
            hud.HideTitle();
            TitleDone = true;
            Begin(world);
        }

        float Speed => FishingController.Bot ? 10f : 1f;    // โหมดทดสอบ: วันผ่านเร็ว 10 เท่า

        void Update()
        {
            hud.SetStatus(Story.FmtTime(Story.S.time), Story.S.day, Story.S.basket.Count, Story.Capacity, Story.S.money, Story.Phase == StoryPhase.Fishing);
            if (Story.Phase != StoryPhase.Fishing || busy || hud.CardOpen || hud.ModalOpen) return;
            Story.S.time = Mathf.Min(Story.S.time + Time.deltaTime * Story.HoursPerSec * Speed, (NightMode ? NightEnd : Story.DayEnd) + 0.25f);   // ระหว่างสู้ปลาตอนหกโมงเย็น เวลาไม่เลยไปจนมืด
            sky.Apply(Story.S.time, Story.CurrentLuck.sun);
            if (Story.S.time >= 17 && !warned && !NightMode) { warned = true; hud.Toast("ใกล้ค่ำแล้ว อีกชั่วโมงจะมืด"); }
            if (!NightMode && Story.S.time >= Story.DayEnd && !fishing.Busy && !nightAsked)
            {
                nightAsked = true;
                Story.S.time = Story.DayEnd;
                if (FishingController.Bot && !Application.absoluteURL.Contains("night")) { hud.Toast("หกโมงเย็นแล้ว กลับบ้านกันเถอะ"); GoHome(); return; }
                hud.Ask("หกโมงเย็นแล้ว… กลับบ้าน หรือจะตกปลากลางคืนต่อ? กลางคืนปลาอาถรรพ์กินเบ็ด แต่ระวังวิญญาณที่สิงอยู่แถวนี้!",
                    ("กลับบ้าน", GoHome), ("ตกปลากลางคืน (ถึงเที่ยงคืน)", () => { NightMode = true; hud.Toast("กดปุ่มไฟฉาย (หรือ F) ส่องไล่ผี · สติหมดจะหมดสติ!"); }));
                return;
            }
            if (NightMode && Story.S.time >= NightEnd && !fishing.Busy)
            {
                Story.S.time = NightEnd;
                hud.Toast("เที่ยงคืนแล้ว… กลับบ้านก่อนที่จะสายเกินไป");
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

        IEnumerator PlayIntro()
        {
            fishing.Paused = true;
            yield return StageIntro.Play(world, rig, hud, fishing);
            fishing.Paused = false;
        }

        /// สติหมด: หมดสติ ตื่นมาที่บ้าน ปลาในข้องหายไปครึ่งหนึ่ง
        public void Faint(string ghost)
        {
            if (busy) return;
            int lose = Story.S.basket.Count / 2 + (Story.S.basket.Count % 2);
            for (int i = 0; i < lose && Story.S.basket.Count > 0; i++) Story.S.basket.RemoveAt(Random.Range(0, Story.S.basket.Count));
            Story.Save();
            hud.Toast($"{ghost}ยืนอยู่ตรงหน้า… ทุกอย่างมืดลง (หมดสติ ปลาหายไป {lose} ตัว)");
            NightMode = false;
            StartCoroutine(Evening(true));
        }

        IEnumerator ResumeFishing()
        {
            busy = true;
            yield return hud.Fade(Stages.Get(world.StageId).name + " · " + Story.FmtTime(Story.S.time), () => SetupFishing(false), Stages.Get(world.StageId).Art, true);
            // เพิ่งเดินทางมาถึงด่าน (เช้า) ครั้งแรก → ฉากเปิดด่านแบบตัวอย่างหนัง
            if (Story.S.time <= Story.DayStart + 0.4f && StageIntro.ShouldPlay(world.StageId)) yield return PlayIntro();
            hud.Toast($"{Stages.Get(world.StageId).name}\n{Story.CurrentLuck.text}");
            busy = false;
        }

        void SetupFishing(bool reset)
        {
            fishing.Paused = false;
            fishing.ResetLine();
            if (reset) { player.Pos = world.Spawn; player.Pos.y = world.StandY(player.Pos.x, player.Pos.z); player.Yaw = world.SpawnYaw; }
            if (reset) { NightMode = false; nightAsked = false; }
            if (FishingController.Bot && Application.absoluteURL.Contains("night") && Story.S.time < 19) { Story.S.time = 19.5f; NightMode = true; nightAsked = true; }
            rig.ClearOverride();
            rig.Snap();
            if (roofLeak) roofLeak.SetActive(!Story.RoofFixed);
            sky.Apply(Story.S.time, Story.CurrentLuck.sun);
            Story.Phase = StoryPhase.Fishing;
        }

        // ---------- เช้า ----------
        IEnumerator Morning(string title, bool intro, bool fromBlack = false)
        {
            busy = true;
            yield return hud.Fade(title, () => ShowHome(6.2f), null, fromBlack);
            if (intro) yield return hud.Dialog(Story.Intro);
            // สืบคดี: เช้าวันที่ 2 ลุงเปี๊ยกมาเล่าเรื่องแสงทองใต้กอบัว · ปิดคดีแล้วเช้าถัดมาเปิดบทต่อไปตามแผนที่ยายอิ่ม
            if (!Story.FreePlay) Mystery.NextChapter();
            if (Story.FreePlay) { }
            else if (!Mystery.Started && Story.S.day >= 2)
            {
                var nc = Mystery.Current;
                yield return hud.Dialog(nc.intro);
                Mystery.Start();
                if (nc.stage != "paddy" && !Stages.Unlocked(nc.stage))
                    yield return hud.Dialog(new[] { ("แม่", $"แต่ทางไป{Stages.Get(nc.stage).name}ต้องใช้เงิน {Story.Baht(Stages.Get(nc.stage).price)} นะพ่อ เก็บเงินอีกหน่อยแล้วค่อยไปกัน") });
                hud.Toast($"เปิดคดีใหม่! ({Stages.Get(nc.stage).name}) ดู \"สมุดคดี\" มุมขวาบน");
            }
            else if (Mystery.ReadyToSolve) yield return hud.Dialog(new[] { ("ลูก", "พ่อ เบาะแสครบแล้วนะ! เปิดสมุดคดีแล้วไขปริศนากันเถอะ") });
            else if (Mystery.Started && !Mystery.Solved && Mystery.Current.stage != "paddy" && Story.S.day % 3 == 0)
                yield return hud.Dialog(new[] { ("ลูก", $"พ่อ อย่าลืมคดี \"{Mystery.Current.title}\" นะ เบาะแสอยู่ที่{Stages.Get(Mystery.Current.stage).name}") });
            Story.Phase = StoryPhase.Morning;
            string dest = null;
            hud.ShowMorning(id => dest = id, PayGoal);
            while (dest == null) yield return null;
            if (dest == "__title")
            {
                busy = false; Story.ExitFreePlay(); TitleDone = false;
                Begin(world); yield break;
            }
            warned = false; fullAsked = false;
            Story.S.time = Story.DayStart;
            Story.S.stage = dest;
            var st = Stages.Get(dest);
            if (dest == world.StageId)
            {
                yield return hud.Fade(st.go, () => SetupFishing(true), st.Art);
                if (StageIntro.ShouldPlay(dest)) yield return PlayIntro();
                hud.Toast($"{st.name}\n{Story.CurrentLuck.text}");
                busy = false;
            }
            else
            {
                Story.Phase = StoryPhase.Fishing;
                yield return hud.Fade(st.go, () => StageLoader.Go(st.scene, p => hud.FadeProgress(st.go, p), _ => TravelFailed()), st.Art);
            }
        }

        // ดาวน์โหลดด่านไม่สำเร็จ (เน็ตหลุด) → ตกปลาบ่อหลังบ้านไปก่อน
        void TravelFailed()
        {
            Story.S.stage = world.StageId;
            SetupFishing(true);
            hud.Toast("ทางไปด่านนั้นขาด (โหลดไม่สำเร็จ) วันนี้ตกบ่อหลังบ้านไปก่อนนะ");
            busy = false;
        }

        void PayGoal()
        {
            var g = Story.CurrentGoal;
            if (g == null || !Story.PayGoal()) return;
            if (roofLeak) roofLeak.SetActive(!Story.RoofFixed);
            StartCoroutine(PaidLines(g));
        }
        IEnumerator PaidLines(Goal g)
        {
            if (g == Story.Goals[Story.Goals.Length - 1])
                // ตอนจบ: ทั้งครอบครัวพายายอิ่มกับผู้ใหญ่ทองไปเที่ยวงานวัด
                yield return hud.Dialog(new[]
                {
                    ("แม่", g.done),
                    ("ยายอิ่ม", "ชิงช้าสวรรค์สูงขนาดนี้ ยายมองเห็นทุ่งนาบ้านเราด้วยแหละหลาน"),
                    ("ผู้ใหญ่ทอง", "ขอบคุณที่พาแม่ผมกลับมานะครับ ถ้าไม่มีพวกคุณ ลายสานพวกนั้นคงจมอยู่ใต้น้ำไปตลอดกาล"),
                    ("ลูก", "พ่อ… ตะกร้าเก้าลายใส่ขนมสายไหมได้เต็มเลย!"),
                    ("พ่อ", "ตะกร้าไม่ได้วิเศษหรอกลูก แต่ใครที่ตั้งใจหาปลาเลี้ยงคนที่รัก ตะกร้าของเขาจะไม่ว่างเปล่าในวันพรุ่งนี้"),
                    ("ลูก", "เย้! ขอบคุณนะพ่อ"),
                });
            else yield return hud.Dialog(new[] { ("แม่", g.done), ("ลูก", "เย้! ขอบคุณนะพ่อ") });
            hud.RefreshMorning();
        }

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
            var c = Mystery.Current;
            yield return hud.Dialog(c.solved);
            hud.Toast(c.rewardBigBasket ? $"ไขคดีสำเร็จ! ได้ตะกร้าแห่งวันพรุ่งนี้ (ข้องใส่ได้ 14 ตัว) + {Story.Baht(c.rewardMoney)}"
                : Mystery.AllSolved ? $"ไขคดีสุดท้ายสำเร็จ! + {Story.Baht(c.rewardMoney)} · ปลดล็อกเป้าหมาย \"พาครอบครัวไปเที่ยวงานวัด\""
                : $"ไขคดีสำเร็จ! ได้ลายสานใหม่ + {Story.Baht(c.rewardMoney)} · พรุ่งนี้เช้าตามแผนที่ไปจุดต่อไป");
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
                yield return hud.Fade(Stages.Get(world.StageId).back, () => StageLoader.Go("Paddy"), Stages.Get("paddy").Art);
                yield break;
            }
            yield return hud.Fade(travel ? Stages.Get(Story.S.stage).back : "กลับถึงบ้านแล้ว…", () => ShowHome(Mathf.Max(Story.S.time, 17.6f)), Stages.Get("paddy").Art, !travel);   // !travel = เพิ่งโหลดฉากบ้านมาจากด่านอื่น
            Story.S.stage = "paddy";
            if (sneaked)
            {
                // กลับจากแอบตกปลาตอนดึก: ย่องเข้ามุ้งนอนเลย ปลาที่ได้เก็บไว้ในข้องไว้ปิ้ง/ขายมื้อเย็นพรุ่งนี้
                sneaked = false; NightMode = false;
                if (Story.S.basket.Count > 0) hud.Toast($"ย่องกลับเข้ามุ้ง… ปลา {Story.S.basket.Count} ตัวในข้องเก็บไว้มื้อเย็นพรุ่งนี้");
                Story.Sleep(rng);
                StartCoroutine(Morning($"ราตรีสวัสดิ์…  เช้าวันที่ {Story.S.day}", false));
                yield break;
            }
            if (Story.FreePlay)
            {
                var fr = Story.SettleEvening(new bool[Story.S.basket.Count]);
                if (fr.earned > 0) hud.Toast($"ขายปลาได้ {Story.Baht(fr.earned)}");
                Story.Sleep(rng);
                StartCoroutine(Morning("ตกปลาอิสระ · วันใหม่", false));
                yield break;
            }
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
            if (Mystery.ReadyToSolve) lines.Add(("ลูก", "เบาะแสครบแล้ว! เปิดสมุดคดีแล้วไขปริศนากันเถอะพ่อ"));
            yield return hud.Dialog(lines.ToArray());
            // ก่อนนอน: ห้องนอนมีมุ้ง ตะเกียง · เลือกนอน / สวดมนต์ / แอบออกไปตกปลา / เซฟ
            Story.S.time = Mathf.Max(Story.S.time, 21.5f);
            while (true)
            {
                string pick = null;
                hud.ShowBedtime(prayDay != Story.S.day, k => pick = k);
                while (pick == null) yield return null;
                if (pick == "pray")
                {
                    prayDay = Story.S.day; Story.S.amulets++; Story.S.happy = Mathf.Min(100, Story.S.happy + 2); Story.Save();
                    hud.Toast($"ตรวจกลอนประตูหน้าต่าง จุดธูปสวดมนต์แผ่เมตตาให้ดวงวิญญาณ… จิตใจสงบ (ได้สายสิญจน์ 1 เส้น · มี {Story.S.amulets})");
                    continue;
                }
                if (pick == "save") { Story.Save(); Mystery.Save(); hud.Toast("บันทึกสมุดคดีแล้ว (เซฟเกม)"); continue; }
                hud.HideBed();
                if (pick == "sneak") { StartCoroutine(SneakOut()); yield break; }
                break;
            }
            Story.Sleep(rng);
            StartCoroutine(Morning($"ราตรีสวัสดิ์…  เช้าวันที่ {Story.S.day}", false));
        }

        // แอบออกจากบ้านตอนดึก: ตกปลาบ่อหลังบ้าน 3 ทุ่มครึ่ง–เที่ยงคืน (ผีออกเต็มที่)
        IEnumerator SneakOut()
        {
            busy = true;
            yield return hud.Fade("แอบย่องออกจากบ้านตอนทุกคนหลับ… ระวังตัวด้วย", () =>
            {
                Story.S.stage = world.StageId; Story.S.time = 21.5f;
                SetupFishing(true);
                NightMode = true; nightAsked = true; warned = true; fullAsked = false; sneaked = true;
            }, Stages.Get(world.StageId).Art);
            hud.Toast("กดปุ่มไฟฉาย (หรือ F) ส่องไล่ผี · สติหมดจะหมดสติ!");
            busy = false;
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
