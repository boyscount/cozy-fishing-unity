using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // หน้าจอของโหมดเนื้อเรื่อง: แถบเวลา/ข้อง/เงิน · ปุ่มกลับบ้าน · หน้าต่างถาม · จอมืดระหว่างเดินทาง · กล่องบทสนทนา
    // · เช้าที่บ้าน (ดวง ราคาตลาด เป้าหมายซ่อมหลังคา) · มื้อเย็น (เลือกปิ้ง/ขายทีละตัว) · ข้องปลา (ดู/ปล่อยคืนน้ำ)
    public partial class Hud
    {
        public StoryDirector director;
        VisualElement statusBox, clockBox, homeBtn, ask, askBox, askBtns, fadeEl, dialogEl, morning, morningBody, evening, eveList, eveSum, bag, bagList;
        VisualElement fadeArt;
        Label statusTxt, clockTxt, askTxt, fadeTxt, dialogWho, dialogSay, bagTitle;
        Action<string> onMorningGo; Action onMorningPay;
        Action<bool[]> onEveningDone;
        bool[] eveGrill;
        bool dialogNext;
        public bool ModalOpen => Shown(ask) || Shown(fadeEl) || Shown(dialogEl) || Shown(morning) || Shown(evening) || Shown(bag) || Shown(casePanel) || Shown(cluePop);
        VisualElement caseBtn, casePanel, caseBody, cluePop, clueBody;
        Label caseBtnTxt;

        // ---------- สมุดคดี ----------
        void BuildCase()
        {
            caseBtn = IconButton("casebook", new Color(0.62f, 0.5f, 0.85f), OpenCase, out caseBtnTxt);
            casePanel = Overlay(out var cb, 860);
            var cs = new ScrollView(ScrollViewMode.Vertical); cs.style.flexGrow = 1; cs.style.flexShrink = 1; cb.Add(cs);
            caseBody = new VisualElement(); cs.Add(caseBody);
            var row = Row(cb); row.style.marginTop = 12;
            var solve = new Button(() => { Hide(casePanel); director?.TrySolve(); }) { text = "ไขคดี!", name = "solve" }; StyleButton(solve, new Color(0.85f, 0.45f, 0.2f)); row.Add(solve);
            var close = new Button(() => Hide(casePanel)) { text = "ปิด" }; StyleButton(close, Gray); close.style.marginLeft = 10; row.Add(close);
            cluePop = Overlay(out var pb, 680);
            clueBody = new VisualElement { pickingMode = PickingMode.Ignore }; clueBody.style.flexShrink = 0; pb.Add(clueBody);
            var ok = new Button(() => Hide(cluePop)) { text = "จดลงสมุดคดี" }; StyleButton(ok, new Color(0.32f, 0.26f, 0.42f)); ok.style.alignSelf = Align.Center; ok.style.marginTop = 14; pb.Add(ok);
        }

        void OpenCase()
        {
            if (CardOpen || ModalOpen) return;
            var c = Mystery.Current;
            caseBody.Clear();
            L(caseBody, c.title, 38, new Color(0.78f, 0.7f, 1f));
            var sum = L(caseBody, Mystery.Solved ? "คดีปิดแล้ว · ตะกร้าแห่งวันพรุ่งนี้อยู่กับครอบครัวเรา" : c.summary, 24, Ink, false); sum.style.marginTop = 6; sum.style.unityTextAlign = TextAnchor.MiddleLeft;
            L(caseBody, $"เบาะแส {Mystery.CluesFound}/{c.clues.Length}", 28, Ink).style.marginTop = 12;
            foreach (var clue in c.clues)
            {
                bool has = Mystery.HasClue(clue.id);
                var box = Box(caseBody, has ? Glass2 : Glass3, 20, false); Pad(box, 10, 16); box.style.marginTop = 8;
                var n = L(box, has ? clue.name : "??? เบาะแสที่ยังไม่พบ", 26, has ? Ink : Sub); n.style.unityTextAlign = TextAnchor.MiddleLeft;
                var d = L(box, has ? clue.found : "คำใบ้: " + clue.hint, 22, Sub, false); d.style.unityTextAlign = TextAnchor.MiddleLeft; d.style.marginTop = 2;
            }
            casePanel.Q<Button>("solve").style.display = Mystery.ReadyToSolve ? DisplayStyle.Flex : DisplayStyle.None;
            Show(casePanel);
        }

        /// ป๊อปอัป "พบเบาะแส!" (เช่น เก็บขวดจดหมายขึ้นมาได้)
        public void ShowClue(Clue c)
        {
            clueBody.Clear();
            L(clueBody, "พบเบาะแส!", 40, new Color(0.78f, 0.7f, 1f));
            L(clueBody, c.name, 30, Ink).style.marginTop = 6;
            var d = L(clueBody, c.found, 26, Ink, false); d.style.marginTop = 10;
            L(clueBody, $"เบาะแส {Mystery.CluesFound}/{Mystery.Current.clues.Length}", 22, Sub).style.marginTop = 10;
            Show(cluePop);
            if (FishingController.Bot) StartCoroutine(BotClick(cluePop, 2.5f, () => Hide(cluePop)));
        }
        static bool Shown(VisualElement e) => e != null && e.style.display == DisplayStyle.Flex;

        void BuildStory()
        {
            // แถบซ้ายบน: ข้อง + เงิน (แตะเพื่อดูข้อง) · ใต้ลงมา: นาฬิกา + วันที่
            statusBox = countBoxRef; statusTxt = count;
            statusBox.pickingMode = PickingMode.Position;
            statusBox.RegisterCallback<PointerDownEvent>(e => OpenBag());
            clockBox = Box(root, Paper, 12); Abs(clockBox, 24, null, 78); Pad(clockBox, 6, 18);
            clockTxt = L(clockBox, "06:00", 22, Ink); clockTxt.style.unityTextAlign = TextAnchor.MiddleLeft;
            BuildCase();
            homeBtn = IconButton("home", new Color(0.85f, 0.55f, 0.3f), () => director?.RequestHome(), out var hl);
            hl.text = "กลับบ้าน";

            // หน้าต่างถาม (ปุ่มตอบได้หลายปุ่ม)
            ask = Overlay(out askBox, 640);
            askTxt = L(askBox, "", 30, Ink);
            askBtns = new VisualElement { pickingMode = PickingMode.Ignore }; askBtns.style.marginTop = 18; askBtns.style.flexShrink = 0; askBox.Add(askBtns);

            // ข้องปลา
            bag = Overlay(out var bagBox, 760);
            bagTitle = L(bagBox, "", 32, Ink);
            var bs = new ScrollView(ScrollViewMode.Vertical); bs.style.flexGrow = 1; bs.style.flexShrink = 1; bs.style.marginTop = 10; bagBox.Add(bs);
            bagList = new VisualElement(); bs.Add(bagList);
            var bclose = new Button(() => Hide(bag)) { text = "ปิด" }; StyleButton(bclose, Gray); bclose.style.alignSelf = Align.Center; bclose.style.marginTop = 12; bagBox.Add(bclose);

            // เช้าที่บ้าน
            morning = Overlay(out var mBox, 820);
            var ms = new ScrollView(ScrollViewMode.Vertical); ms.style.flexGrow = 1; ms.style.flexShrink = 1; mBox.Add(ms);
            morningBody = new VisualElement(); ms.Add(morningBody);

            // มื้อเย็น
            evening = Overlay(out var eBox, 820);
            L(eBox, "มื้อเย็นที่บ้าน · ปลาตัวไหนจะปิ้ง ตัวไหนจะขาย?", 30, Ink);
            var es = new ScrollView(ScrollViewMode.Vertical); es.style.flexGrow = 1; es.style.flexShrink = 1; es.style.marginTop = 10; eBox.Add(es);
            eveList = new VisualElement(); es.Add(eveList);
            eveSum = new VisualElement { pickingMode = PickingMode.Ignore }; eveSum.style.flexShrink = 0; eveSum.style.marginTop = 10; eBox.Add(eveSum);
            var eb = Row(eBox); eb.style.marginTop = 12;
            var ga = new Button(() => { for (int i = 0; i < eveGrill.Length; i++) eveGrill[i] = true; FillEvening(); }) { text = "ปิ้งหมด" }; StyleButton(ga, new Color(0.85f, 0.45f, 0.2f)); eb.Add(ga);
            var sa = new Button(() => { for (int i = 0; i < eveGrill.Length; i++) eveGrill[i] = false; FillEvening(); }) { text = "ขายหมด" }; StyleButton(sa, new Color(0.62f, 0.5f, 0.15f)); sa.style.marginLeft = 10; eb.Add(sa);
            var ok = new Button(() => { Hide(evening); onEveningDone?.Invoke(eveGrill); }) { text = "ตกลง" }; StyleButton(ok, Green); ok.style.marginLeft = 10; eb.Add(ok);

            // กล่องบทสนทนา (ล่างจอ แตะ/กด Space ไปต่อ)
            dialogEl = new VisualElement { pickingMode = PickingMode.Position }; Abs(dialogEl, 0, 0, 0, 0); dialogEl.style.justifyContent = Justify.FlexEnd; dialogEl.style.alignItems = Align.Center; root.Add(dialogEl);
            var db = Box(dialogEl, Paper, 30); Pad(db, 16, 28); MaxW(db, 900); db.style.marginBottom = 30;
            dialogWho = L(db, "", 26, Gold); dialogWho.style.unityTextAlign = TextAnchor.MiddleLeft;
            dialogSay = L(db, "", 30, Ink); dialogSay.style.unityTextAlign = TextAnchor.MiddleLeft; dialogSay.style.marginTop = 4;
            var nx = L(db, "แตะเพื่อไปต่อ ▶", 20, Sub, false); nx.style.unityTextAlign = TextAnchor.MiddleRight;
            dialogEl.RegisterCallback<PointerDownEvent>(e => dialogNext = true);
            dialogEl.style.display = DisplayStyle.None;

            // จอมืดระหว่างเดินทาง (อยู่บนสุด)
            fadeEl = new VisualElement { pickingMode = PickingMode.Position }; Abs(fadeEl, 0, 0, 0, 0); fadeEl.style.backgroundColor = new Color(0.04f, 0.06f, 0.05f, 1);
            fadeEl.style.justifyContent = Justify.Center; fadeEl.style.alignItems = Align.Center; root.Add(fadeEl);
            fadeArt = new VisualElement { pickingMode = PickingMode.Ignore };
            fadeArt.style.width = 600; fadeArt.style.height = 400; fadeArt.style.maxWidth = Length.Percent(88); fadeArt.style.marginBottom = 28;
            fadeArt.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            Radius(fadeArt, 18); Border(fadeArt, 3, new Color(0.85f, 0.7f, 0.38f, 0.85f));
            fadeEl.Add(fadeArt);
            fadeTxt = L(fadeEl, "", 40, new Color(0.96f, 0.93f, 0.85f)); fadeTxt.style.maxWidth = 900;
            // เริ่มฉากด้วยจอมืดเสมอ (ทุกฉากเรียก Fade(fromBlack) ตอนเริ่ม) — ไม่ให้เห็นฉากแวบก่อนจอเดินทาง
            fadeEl.style.display = DisplayStyle.Flex; fadeEl.style.opacity = 1;
        }

        VisualElement Overlay(out VisualElement box, float maxW)
        {
            var o = new VisualElement { pickingMode = PickingMode.Position }; Abs(o, 0, 0, 0, 0); o.style.backgroundColor = new Color(0.08f, 0.12f, 0.1f, 0.55f);
            o.style.justifyContent = Justify.Center; o.style.alignItems = Align.Center; root.Add(o);
            box = Box(o, Paper, 40); Pad(box, 24, 30); MaxW(box, maxW); box.style.maxHeight = Length.Percent(92); box.pickingMode = PickingMode.Position;
            o.style.display = DisplayStyle.None;
            return o;
        }
        static VisualElement Row(VisualElement parent)
        {
            var r = new VisualElement { pickingMode = PickingMode.Ignore }; r.style.flexDirection = FlexDirection.Row; r.style.justifyContent = Justify.Center; r.style.flexWrap = Wrap.Wrap; r.style.flexShrink = 0;
            parent.Add(r); return r;
        }
        void Show(VisualElement e) { e.style.display = DisplayStyle.Flex; e.BringToFront(); fadeEl?.BringToFront(); GameInput.Blocked = true; }
        void Hide(VisualElement e) { e.style.display = DisplayStyle.None; GameInput.Blocked = ModalOpen || CardOpen; }

        // ---------- แถบสถานะ ----------
        string lastStatus;
        bool homeFishing;
        void UpdateStoryUi()
        {
            homeBtn.style.display = homeFishing && !CardOpen && !ModalOpen && fightBox.style.display != DisplayStyle.Flex ? DisplayStyle.Flex : DisplayStyle.None;   // สู้ปลาอยู่: ซ่อนไม่ให้ทับเกจ
            caseBtn.style.display = Mystery.Started && !CardOpen && !ModalOpen ? DisplayStyle.Flex : DisplayStyle.None;
            caseBtnTxt.text = Mystery.Solved ? "ปิดคดี" : Mystery.ReadyToSolve ? "ไขคดีได้!" : $"{Mystery.CluesFound}/{Mystery.Current.clues.Length}";
            caseBtnTxt.style.color = Mystery.ReadyToSolve && !Mystery.Solved ? new Color(1f, 0.82f, 0.35f) : Color.white;
        }
        public void SetStatus(string clock, int day, int n, int cap, int money, bool fishingNow)
        {
            var s = $"{clock}|{day}|{n}|{cap}|{money}|{fishingNow}";
            if (s == lastStatus) return;
            lastStatus = s;
            statusTxt.text = $"ข้อง {n}/{cap} · {Story.Baht(money)}";
            var lk = Story.CurrentLuck;
            float temp = Mathf.Round(26 + 7 * Mathf.Sin(Mathf.InverseLerp(6, 18, Story.S.time) * Mathf.PI) - (lk.rain ? 4 : lk.sun < 0.6f ? 2 : 0));
            float wind = 1.2f + (1 - lk.sun) * 4.5f;
            string sky = lk.id == "great" ? "ฟ้าใส" : lk.id == "normal" ? "มีเมฆบางส่วน" : lk.id == "slow" ? "ฟ้าครึ้ม" : "ฝนพรำ";
            clockTxt.text = $"{clock} น. · วันที่ {day}\n{sky} · {temp:0}°C · ลม {wind:0.0} ม./วิ";
            clockTxt.style.color = Story.S.time >= 17 ? Gold : Ink;
            homeFishing = fishingNow;
        }

        // ---------- หน้าต่างถาม ----------
        public void Ask(string text, params (string label, Action act)[] buttons)
        {
            askTxt.text = text;
            askBtns.Clear();
            for (int i = 0; i < buttons.Length; i++)
            {
                var (label, act) = buttons[i];
                var b = new Button(() => { Hide(ask); act?.Invoke(); }) { text = label };
                StyleButton(b, i == 0 ? Green : Gray);
                b.style.marginTop = 8; b.style.alignSelf = Align.Stretch;
                askBtns.Add(b);
            }
            Show(ask);
            if (FishingController.Bot) StartCoroutine(BotClick(ask, 1.2f, () => { Hide(ask); buttons[0].act?.Invoke(); }));
        }

        // โหมดทดสอบ: ?bot&stage=rapids เปิดทางและไปด่านนั้นทันที
        static string BotStage()
        {
            var url = Application.absoluteURL;
            foreach (var st in Stages.All)
                if (url.Contains("stage=" + st.id)) { if (!Stages.Unlocked(st.id)) Story.S.unlocked.Add(st.id); return st.id; }
            return "paddy";
        }

        IEnumerator BotClick(VisualElement panel, float delay, Action click)
        {
            yield return new WaitForSeconds(delay);
            if (Shown(panel)) click();
        }

        // ---------- จอมืด: มืดลง → ทำงานระหว่างมืด → สว่างขึ้น ----------
        public IEnumerator Fade(string text, Action during, Texture2D art = null, bool fromBlack = false)
        {
            fadeTxt.text = text;
            fadeArt.style.backgroundImage = art; fadeArt.style.display = art != null ? DisplayStyle.Flex : DisplayStyle.None;
            fadeEl.style.display = DisplayStyle.Flex; fadeEl.BringToFront();
            GameInput.Blocked = true;
            if (!fromBlack) for (float a = 0; a < 1; a += Time.deltaTime / 0.5f) { fadeEl.style.opacity = a; yield return null; }   // fromBlack: เพิ่งโหลดฉากใหม่ ห้ามโชว์ฉากก่อนจอมืด
            fadeEl.style.opacity = 1;
            during?.Invoke();
            yield return new WaitForSeconds(1.1f);
            while (StageLoader.Loading) yield return null;   // ดาวน์โหลดด่าน: จอมืดค้างไว้จนฉากใหม่ขึ้น (ฮัดนี้จะถูกลบไปพร้อมฉากเก่า)
            for (float a = 1; a > 0; a -= Time.deltaTime / 0.6f) { fadeEl.style.opacity = a; yield return null; }
            fadeEl.style.display = DisplayStyle.None;
            GameInput.Blocked = ModalOpen || CardOpen;
        }

        /// ข้อความบนจอมืดระหว่างดาวน์โหลดด่าน
        public void FadeProgress(string text, float p)
        {
            if (fadeTxt == null) return;
            fadeEl.style.display = DisplayStyle.Flex; fadeEl.style.opacity = 1;
            fadeTxt.text = p < 1 ? $"{text}\n<size=70%>กำลังโหลดด่าน {Mathf.RoundToInt(p * 100)}%</size>" : text;
        }

        // ---------- บทสนทนา ----------
        public IEnumerator Dialog((string who, string say)[] lines)
        {
            Show(dialogEl);
            foreach (var (who, say) in lines)
            {
                dialogWho.text = who; dialogSay.text = say;
                dialogNext = false;
                float shown = 0;
                yield return null;
                while (!dialogNext)
                {
                    shown += Time.deltaTime;
                    var k = Keyboard.current;
                    if (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame)) dialogNext = true;
                    if (FishingController.Bot && shown > 1.4f) dialogNext = true;
                    yield return null;
                }
            }
            Hide(dialogEl);
        }

        // ---------- เช้าที่บ้าน ----------
        public void ShowMorning(Action<string> go, Action pay)
        {
            onMorningGo = go; onMorningPay = pay;
            RefreshMorning();
            Show(morning);
            if (FishingController.Bot) StartCoroutine(BotClick(morning, 2.5f, () => { Hide(morning); onMorningGo?.Invoke(BotStage()); }));
        }

        public void RefreshMorning()
        {
            var s = Story.S;
            morningBody.Clear();
            L(morningBody, $"เช้าวันที่ {s.day}", 44, Ink);
            L(morningBody, Story.CurrentLuck.text, 28, new Color(0.55f, 0.8f, 1f)).style.marginTop = 4;
            var stats = Row(morningBody); stats.style.marginTop = 12;
            L(Stat(stats), $"เงินเก็บ {Story.Baht(s.money)}", 28, Money);
            L(Stat(stats), $"ครอบครัว {(s.happy >= 75 ? "มีความสุข" : s.happy >= 40 ? "สบายดี" : "หิวโหย")} {s.happy}%", 28, Ink);
            var hot = s.hot != null ? FishDatabase.Get(s.hot) : null;
            if (hot != null)
            {
                var hb = Box(morningBody, new Color(0.32f, 0.2f, 0.08f, 0.9f), 22, false); Pad(hb, 10, 18); hb.style.marginTop = 12;
                L(hb, $"วันนี้แม่ค้ารับซื้อ{(FishDex.Has(hot.id) ? hot.name : "ปลาลึกลับ")}ราคาพิเศษ {Story.Baht(Story.PriceOf(hot.id))}/กก.", 26, Money);
            }
            var g = Story.CurrentGoal;
            var gb = Box(morningBody, Glass2, 22, false); Pad(gb, 12, 18); gb.style.marginTop = 12;
            if (g != null)
            {
                var gl = L(gb, $"เป้าหมาย: {g.text}", 28, Ink); gl.style.unityTextAlign = TextAnchor.MiddleLeft;
                var bar = Bar(gb, 24, Track); var fill = Box(bar, Gold, 12, false); fill.style.height = Length.Percent(100);
                fill.style.width = Length.Percent(Mathf.Clamp01((float)s.money / g.cost) * 100);
                var gr = Row(gb); gr.style.justifyContent = Justify.SpaceBetween; gr.style.marginTop = 8; gr.style.alignItems = Align.Center;
                L(gr, $"{Story.Baht(s.money)} / {Story.Baht(g.cost)}", 24, Ink);
                if (Story.CanPayGoal) { var pb = new Button(() => onMorningPay?.Invoke()) { text = s.goal == 0 ? "จ่ายค่าซ่อมหลังคา" : "จ่ายเลย" }; StyleButton(pb, new Color(0.85f, 0.45f, 0.2f)); gr.Add(pb); }
                else if (s.goal >= 2) L(gr, "(ต้องปลดล็อกที่ตกปลาครบทุกด่านก่อน)", 20, Sub, false);
            }
            else L(gb, "ทำครบทุกเป้าหมายแล้ว! ตกปลาสบาย ๆ ได้ตลอดไป", 26, Ink);
            if (!Story.RoofFixed) L(morningBody, "หลังคาบ้านยังรั่วอยู่ เมื่อคืนต้องเอาถังรองน้ำฝน…", 22, Sub, false).style.marginTop = 8;
            // เลือกที่ตกปลาวันนี้: ด่านที่เปิดแล้วไปได้เลย · ด่านถัดไปซื้อทางด้วยเงินเก็บ
            L(morningBody, "วันนี้จะไปตกปลาที่ไหน?", 28, Ink).style.marginTop = 16;
            foreach (var st in Stages.All)
            {
                bool open = Stages.Unlocked(st.id);
                var row = Box(morningBody, Glass2, 10, false); Pad(row, 8, 12); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 8;
                var art = new VisualElement { pickingMode = PickingMode.Ignore };
                art.style.width = 150; art.style.height = 100; art.style.flexShrink = 0; art.style.marginRight = 12;
                art.style.backgroundImage = st.Art; art.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                art.style.unityBackgroundImageTintColor = open ? Color.white : new Color(0.45f, 0.45f, 0.45f);
                Radius(art, 8); row.Add(art);
                var info = new VisualElement { pickingMode = PickingMode.Ignore }; info.style.flexGrow = 1; info.style.flexShrink = 1; info.style.marginRight = 10; row.Add(info);
                var nm = L(info, st.name, 28, open ? Ink : Sub); nm.style.unityTextAlign = TextAnchor.MiddleLeft;
                var ds = L(info, open ? st.desc : $"{st.desc} · ค่าเดินทางครั้งแรก {Story.Baht(st.price)}", 18, Sub, false); ds.style.unityTextAlign = TextAnchor.MiddleLeft;
                var id = st.id;
                if (open)
                {
                    var b = new Button(() => { Hide(morning); onMorningGo?.Invoke(id); }) { text = "ไปตกที่นี่" };
                    StyleButton(b, Gold); row.Add(b);
                }
                else
                {
                    bool can = s.money >= st.price;
                    var b = new Button(() => { if (Stages.Buy(id)) { Toast($"เปิดทางไป{Stages.Get(id).name}แล้ว!"); RefreshMorning(); } }) { text = $"ซื้อทาง {Story.Baht(st.price)}" };
                    StyleButton(b, can ? Green : Gray); b.SetEnabled(can); row.Add(b);
                }
            }
        }

        // ---------- มื้อเย็น ----------
        public void ShowEvening(Action<bool[]> done)
        {
            onEveningDone = done;
            eveGrill = Story.DefaultGrill();
            FillEvening();
            Show(evening);
            if (FishingController.Bot) StartCoroutine(BotClick(evening, 2.5f, () => { Hide(evening); onEveningDone?.Invoke(eveGrill); }));
        }

        void FillEvening()
        {
            eveList.Clear();
            var b = Story.S.basket;
            if (b.Count == 0) L(eveList, "วันนี้ข้องว่างเปล่า…", 28, Sub);
            float fed = 0; int earn = 0;
            for (int i = 0; i < b.Count; i++)
            {
                int idx = i;
                var f = b[i]; var sp = FishDatabase.Get(f.id);
                if (eveGrill[i]) fed += f.kg; else earn += Story.Value(f);
                var row = Box(eveList, Glass2, 20, false); Pad(row, 6, 12); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginBottom = 8;
                FishThumb(row, sp, 120, 70);
                var info = new VisualElement { pickingMode = PickingMode.Ignore }; info.style.flexGrow = 1; info.style.marginLeft = 8; row.Add(info);
                var n = L(info, sp?.name ?? f.id, 26, Ink); n.style.unityTextAlign = TextAnchor.MiddleLeft;
                var d = L(info, $"{f.kg:0.00} กก. · ขายได้ {Story.Baht(Story.Value(f))}{(Story.S.hot == f.id ? " (ราคาพิเศษ)" : "")}", 20, Sub, false); d.style.unityTextAlign = TextAnchor.MiddleLeft;
                var g = new Button(() => { eveGrill[idx] = true; FillEvening(); }) { text = "ปิ้ง" }; StyleButton(g, eveGrill[i] ? new Color(0.85f, 0.45f, 0.2f) : Gray); g.style.fontSize = 24; row.Add(g);
                var s = new Button(() => { eveGrill[idx] = false; FillEvening(); }) { text = "ขาย" }; StyleButton(s, !eveGrill[i] ? new Color(0.62f, 0.5f, 0.15f) : Gray); s.style.fontSize = 24; s.style.marginLeft = 6; row.Add(s);
            }
            eveSum.Clear();
            var r1 = Row(eveSum); r1.style.justifyContent = Justify.SpaceBetween;
            L(r1, "ความอิ่มของครอบครัว", 24, Ink); L(r1, $"{fed:0.00} / {Story.NeedKg} กก.", 24, Ink);
            var bar = Bar(eveSum, 20, Track); var fill = Box(bar, new Color(0.85f, 0.45f, 0.2f), 10, false); fill.style.height = Length.Percent(100);
            fill.style.width = Length.Percent(Mathf.Clamp01(fed / Story.NeedKg) * 100);
            var r2 = Row(eveSum); r2.style.justifyContent = Justify.SpaceBetween; r2.style.marginTop = 6;
            L(r2, "ขายตลาดได้", 24, Ink); L(r2, Story.Baht(earn), 24, Money);
        }

        void FishThumb(VisualElement parent, FishSpecies sp, float w, float h)
        {
            var img = new VisualElement { pickingMode = PickingMode.Ignore }; img.style.width = w; img.style.height = h; img.style.flexShrink = 0;
            img.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            if (preview != null && sp != null) img.style.backgroundImage = Background.FromRenderTexture(preview.Icon(sp, false));
            parent.Add(img);
        }

        // ---------- ข้องปลา: ดูปลาที่ได้วันนี้ · ปล่อยคืนน้ำเพื่อเว้นช่อง ----------
        void OpenBag()
        {
            if (CardOpen || ModalOpen || Story.Phase != StoryPhase.Fishing) return;
            FillBag();
            Show(bag);
        }
        void FillBag()
        {
            bagList.Clear();
            var b = Story.S.basket;
            bagTitle.text = $"ข้องปลา {b.Count}/{Story.Capacity} · ขายได้ราว {Story.Baht(Story.BasketValue)}";
            if (b.Count == 0) L(bagList, "ข้องยังว่างอยู่ ไปตกปลากันเถอะ!", 26, Sub);
            for (int i = 0; i < b.Count; i++)
            {
                int idx = i; var f = b[i]; var sp = FishDatabase.Get(f.id);
                var row = Box(bagList, Glass2, 20, false); Pad(row, 6, 12); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginBottom = 8;
                FishThumb(row, sp, 110, 64);
                var info = new VisualElement { pickingMode = PickingMode.Ignore }; info.style.flexGrow = 1; info.style.marginLeft = 8; row.Add(info);
                var n = L(info, sp?.name ?? f.id, 26, Ink); n.style.unityTextAlign = TextAnchor.MiddleLeft;
                var d = L(info, $"{f.kg:0.00} กก. · {f.cm} ซม. · {Story.Baht(Story.Value(f))}", 20, Sub, false); d.style.unityTextAlign = TextAnchor.MiddleLeft;
                var rel = new Button(() => { var old = Story.Release(idx); Toast($"ปล่อย{FishDatabase.Get(old.id)?.name}คืนน้ำแล้ว"); FillBag(); }) { text = "ปล่อยคืนน้ำ" };
                StyleButton(rel, new Color(0.3f, 0.55f, 0.7f)); rel.style.fontSize = 22; row.Add(rel);
            }
        }
    }
}
