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
        VisualElement timeIco, skyIco, viewBtn; Label dayTxt, tempTxt, windTxt;
        Label statusTxt, clockTxt, askTxt, fadeTxt, dialogWho, dialogSay, bagTitle;
        VisualElement dialogPortL, dialogPortR, dialogWhoIco, quest; Label questMain, questClue, questTitle; VisualElement questClueRow, questMainRow; bool questFolded;
        string lastQuest;
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
            casePanel = Overlay(out var cb, 900);
            Skin(cb, "clipboard", 40); cb.style.paddingTop = 76; cb.style.paddingBottom = 40; cb.style.paddingLeft = 58; cb.style.paddingRight = 58;
            var clip = new VisualElement { pickingMode = PickingMode.Ignore }; clip.style.width = 230; clip.style.height = 96;
            clip.style.backgroundImage = Resources.Load<Texture2D>("UI/Frames/clip"); Abs(clip, null, null, -18); clip.style.left = Length.Percent(50); clip.style.marginLeft = -115; cb.Add(clip);
            var cs = new ScrollView(ScrollViewMode.Vertical); cs.style.flexGrow = 1; cs.style.flexShrink = 1; cb.Add(cs);
            caseBody = new VisualElement(); cs.Add(caseBody);
            var row = Row(cb); row.style.marginTop = 12;
            var solve = new Button(() => { Hide(casePanel); director?.TrySolve(); }) { text = "ไขคดี!", name = "solve" }; WoodButton(solve, true); solve.style.unityBackgroundImageTintColor = new Color(1f, 0.62f, 0.32f); row.Add(solve);
            var close = new Button(() => Hide(casePanel)) { text = "ปิด" }; WoodButton(close, true); close.style.unityBackgroundImageTintColor = new Color(0.9f, 0.55f, 0.45f); close.style.marginLeft = 12; row.Add(close);
            cluePop = Overlay(out var pb, 680);
            clueBody = new VisualElement { pickingMode = PickingMode.Ignore }; clueBody.style.flexShrink = 0; pb.Add(clueBody);
            var ok = new Button(() => Hide(cluePop)) { text = "จดลงสมุดคดี" }; StyleButton(ok, new Color(0.32f, 0.26f, 0.42f)); ok.style.alignSelf = Align.Center; ok.style.marginTop = 14; pb.Add(ok);
        }

        void OpenCase()
        {
            if (CardOpen || ModalOpen) return;
            var c = Mystery.Current;
            caseBody.Clear();
            var ink = new Color(0.24f, 0.14f, 0.06f); var red = new Color(0.55f, 0.13f, 0.08f);
            var t = L(caseBody, c.title, 44, red); t.style.unityFontDefinition = fontTitle;
            var rule = new VisualElement { pickingMode = PickingMode.Ignore }; rule.style.height = 3; rule.style.width = Length.Percent(55); rule.style.alignSelf = Align.Center; rule.style.backgroundColor = new Color(0.55f, 0.13f, 0.08f, 0.6f); rule.style.marginBottom = 8; caseBody.Add(rule);
            // สรุปคดี + ภาพสถานที่แบบโพลารอยด์ปักหมุด
            var top = new VisualElement { pickingMode = PickingMode.Ignore }; top.style.flexDirection = FlexDirection.Row; top.style.alignItems = Align.FlexStart; caseBody.Add(top);
            var sum = L(top, Mystery.Solved ? (c.closed ?? "คดีปิดแล้ว") : c.summary, 23, ink, false); sum.style.unityTextAlign = TextAnchor.MiddleLeft; sum.style.flexShrink = 1; sum.style.flexGrow = 1;
            var pol = new VisualElement { pickingMode = PickingMode.Ignore }; pol.style.backgroundColor = new Color(0.97f, 0.95f, 0.9f); Pad(pol, 6, 6); pol.style.paddingBottom = 18; pol.style.marginLeft = 14; pol.style.flexShrink = 0; pol.style.rotate = new Rotate(4); top.Add(pol);
            var art = new VisualElement { pickingMode = PickingMode.Ignore }; art.style.width = 170; art.style.height = 120; art.style.backgroundImage = Stages.Get(c.stage).Art; art.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover); pol.Add(art);
            var pin = Ico(pol, "pin", 30); Abs(pin, 76, null, -14);
            // ตราประทับ "พบ" ตามจำนวนเบาะแส
            var stamps = new VisualElement { pickingMode = PickingMode.Ignore }; stamps.style.flexDirection = FlexDirection.Row; stamps.style.alignItems = Align.Center; stamps.style.justifyContent = Justify.Center; stamps.style.marginTop = 12; caseBody.Add(stamps);
            L(stamps, $"เบาะแส {Mystery.CluesFound}/{c.clues.Length}", 30, ink).style.marginRight = 12;
            foreach (var clue in c.clues) Ico(stamps, Mystery.HasClue(clue.id) ? "stamp_on" : "stamp_off", 52).style.marginLeft = 4;
            foreach (var clue in c.clues)
            {
                bool has = Mystery.HasClue(clue.id);
                var box = new VisualElement { pickingMode = PickingMode.Ignore }; Skin(box, "parchment", 18); Pad(box, 14, 22); box.style.marginTop = 10; caseBody.Add(box);
                box.style.unityBackgroundImageTintColor = has ? Color.white : new Color(0.7f, 0.8f, 0.88f);   // ยังไม่พบ = กระดาษหม่นอมฟ้า
                if (!has) { var qm = L(box, "?", 64, new Color(0.3f, 0.36f, 0.42f, 0.55f)); Abs(qm, null, 22, 0); }
                var n = L(box, has ? clue.name : "??? เบาะแสที่ยังไม่พบ", 26, has ? ink : new Color(0.2f, 0.26f, 0.32f)); n.style.unityTextAlign = TextAnchor.MiddleLeft;
                var d = L(box, has ? clue.found : "คำใบ้: " + clue.hint, 21, has ? new Color(0.35f, 0.24f, 0.12f) : new Color(0.25f, 0.3f, 0.36f), false); d.style.unityTextAlign = TextAnchor.MiddleLeft; d.style.marginTop = 2; d.style.marginRight = has ? 0 : 50;
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
            // นาฬิกา + วันที่ + อากาศ แบบไอคอน: ☀ 06:11  📅 2  /  ⛅ 28°  ༄ 1.7
            clockBox = Box(root, Paper, 12); Abs(clockBox, 24, null, 92); Pad(clockBox, 10, 20); Skin(clockBox, "wood", 18);
            VisualElement Row2() { var r = new VisualElement { pickingMode = PickingMode.Ignore }; r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; clockBox.Add(r); return r; }
            var r1 = Row2(); timeIco = Ico(r1, "sun", 26);
            clockTxt = L(r1, "06:00", 24, Ink); clockTxt.style.marginLeft = 6;
            Ico(r1, "calendar", 22).style.marginLeft = 14; dayTxt = L(r1, "1", 24, Ink); dayTxt.style.marginLeft = 5;
            var r2 = Row2(); r2.style.marginTop = 2; skyIco = Ico(r2, "partly", 26);
            tempTxt = L(r2, "28°", 22, Sub); tempTxt.style.marginLeft = 6;
            Ico(r2, "wind", 22).style.marginLeft = 14; windTxt = L(r2, "1.7", 22, Sub); windTxt.style.marginLeft = 5;
            BuildCase();
            BuildQuest();
            // ปุ่มสลับมุมกล้องตอนตกปลา: ตา = มุมบุคคลที่หนึ่ง · ตาขีดฆ่า = มุมกล้องหลังตัวละคร
            Label vl = null;
            viewBtn = IconButton(FirstPersonView.Enabled ? "eye" : "eye_off", new Color(0.55f, 0.75f, 0.85f), () =>
            {
                FirstPersonView.Enabled = !FirstPersonView.Enabled;
                SetIco(viewBtn.ElementAt(0).ElementAt(0), FirstPersonView.Enabled ? "eye" : "eye_off");
                Toast(FirstPersonView.Enabled ? "มุมมองบุคคลที่หนึ่ง (เห็นมือถือคันเบ็ด)" : "มุมกล้องจากด้านหลัง");
            }, out vl);
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
            morning = Overlay(out var mBox, 900);
            morning.style.backgroundImage = Resources.Load<Texture2D>("UI/Frames/backdrop"); morning.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            mBox.style.backgroundColor = new Color(0, 0, 0, 0.18f); mBox.style.borderTopWidth = mBox.style.borderBottomWidth = mBox.style.borderLeftWidth = mBox.style.borderRightWidth = 0;
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
            // แถวล่าง: ภาพตัวละคร (ซ้าย = คนอื่น · ขวา = พ่อ) + กล่องไม้ไผ่จักสาน
            var drow = new VisualElement { pickingMode = PickingMode.Ignore }; drow.style.flexDirection = FlexDirection.Row; drow.style.alignItems = Align.FlexEnd;
            MaxW(drow, 1040); drow.style.marginBottom = 24; dialogEl.Add(drow);
            VisualElement Portrait()
            {
                var p = new VisualElement { pickingMode = PickingMode.Ignore }; p.style.width = p.style.height = 190; p.style.flexShrink = 0; p.style.marginBottom = 20;
                drow.Add(p); return p;
            }
            dialogPortL = Portrait(); dialogPortL.style.marginRight = -34;
            var db = new VisualElement { pickingMode = PickingMode.Ignore }; db.style.flexGrow = 1; db.style.flexShrink = 1; drow.Add(db);
            Skin(db, "dialog", 26); db.style.paddingTop = 26; db.style.paddingBottom = 26; db.style.paddingLeft = 52; db.style.paddingRight = 44;
            dialogPortR = Portrait(); dialogPortR.style.marginLeft = -34;
            var whoRow = new VisualElement { pickingMode = PickingMode.Ignore }; whoRow.style.flexDirection = FlexDirection.Row; whoRow.style.alignItems = Align.Center; db.Add(whoRow);
            dialogWhoIco = Ico(whoRow, "magnifier", 30);
            dialogWho = L(whoRow, "", 34, new Color(1f, 0.78f, 0.38f)); dialogWho.style.unityTextAlign = TextAnchor.MiddleLeft; dialogWho.style.marginLeft = 8;
            dialogWho.style.unityFontDefinition = fontTitle;
            var rule = new VisualElement { pickingMode = PickingMode.Ignore }; rule.style.height = 2; rule.style.marginTop = 2; rule.style.marginBottom = 6; rule.style.width = Length.Percent(60);
            rule.style.backgroundColor = new Color(0.85f, 0.65f, 0.35f, 0.5f); db.Add(rule);
            dialogSay = L(db, "", 30, new Color(0.97f, 0.93f, 0.84f)); dialogSay.style.unityTextAlign = TextAnchor.MiddleLeft;
            var nx = L(db, "แตะเพื่อไปต่อ ▶", 20, new Color(0.85f, 0.75f, 0.6f), false); nx.style.unityTextAlign = TextAnchor.MiddleRight; nx.style.marginTop = 4;
            dialogEl.RegisterCallback<PointerDownEvent>(e => dialogNext = true);
            dialogEl.style.display = DisplayStyle.None;

            // จอมืดระหว่างเดินทาง (อยู่บนสุด)
            fadeEl = new VisualElement { pickingMode = PickingMode.Position }; Abs(fadeEl, 0, 0, 0, 0); fadeEl.style.backgroundColor = new Color(0.04f, 0.06f, 0.05f, 1);
            fadeEl.style.justifyContent = Justify.Center; fadeEl.style.alignItems = Align.Center; root.Add(fadeEl);
            fadeArt = new VisualElement { pickingMode = PickingMode.Ignore };
            fadeArt.style.width = 600; fadeArt.style.height = 400; fadeArt.style.maxWidth = Length.Percent(88); fadeArt.style.marginBottom = 28;
            fadeArt.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            Radius(fadeArt, 18); Border(fadeArt, 3, new Color(0.85f, 0.7f, 0.38f, 0.85f));
            fadeEl.Add(fadeArt); fadeArt.style.display = DisplayStyle.None;
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
            viewBtn.style.display = homeFishing && !CardOpen && !ModalOpen ? DisplayStyle.Flex : DisplayStyle.None;
            caseBtn.style.display = Mystery.Started && !CardOpen && !ModalOpen ? DisplayStyle.Flex : DisplayStyle.None;
            caseBtnTxt.text = Mystery.Solved ? "ปิดคดี" : Mystery.ReadyToSolve ? "ไขคดีได้!" : $"{Mystery.CluesFound}/{Mystery.Current.clues.Length}";
            caseBtnTxt.style.color = Mystery.ReadyToSolve && !Mystery.Solved ? new Color(1f, 0.82f, 0.35f) : Color.white;
            UpdateQuest();
        }
        public void SetStatus(string clock, int day, int n, int cap, int money, bool fishingNow)
        {
            var s = $"{clock}|{day}|{n}|{cap}|{money}|{fishingNow}";
            if (s == lastStatus) return;
            lastStatus = s;
            statusTxt.text = $"{n}/{cap}"; moneyLbl.text = Story.Baht(money);
            statusTxt.style.color = n >= cap ? Gold : Ink;
            var lk = Story.CurrentLuck;
            float temp = Mathf.Round(26 + 7 * Mathf.Sin(Mathf.InverseLerp(6, 18, Story.S.time) * Mathf.PI) - (lk.rain ? 4 : lk.sun < 0.6f ? 2 : 0));
            float wind = 1.2f + (1 - lk.sun) * 4.5f;
            SetIco(skyIco, lk.id == "great" ? "sun" : lk.id == "normal" ? "partly" : lk.id == "slow" ? "cloud" : "rain");
            SetIco(timeIco, Story.S.time >= 18.5f || Story.S.time < 5.5f ? "moon" : "sun");
            clockTxt.text = clock; dayTxt.text = day.ToString(); tempTxt.text = $"{temp:0}°"; windTxt.text = $"{wind:0.0}";
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
                var port = Resources.Load<Texture2D>("UI/Portraits/" + PortraitKey(who));
                bool right = who == "พ่อ";
                dialogPortL.style.backgroundImage = !right ? port : null; dialogPortR.style.backgroundImage = right ? port : null;
                dialogPortL.style.visibility = !right && port != null ? Visibility.Visible : Visibility.Hidden;
                dialogPortR.style.display = right && port != null ? DisplayStyle.Flex : DisplayStyle.None;
                SetIco(dialogWhoIco, who == "ลูก" ? "casebook" : who == "แม่" ? "basket" : "magnifier");
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

        static string PortraitKey(string who) => who switch
        {
            "แม่" => "mae", "พ่อ" => "pho", "ลูก" => "luk", "ลุงเปี๊ยก" => "lungpiak", "ยายอิ่ม" => "yai_im", "ผู้ใหญ่ทอง" => "phuyai_thong", _ => "",
        };

        // ---------- Quest Tracker (กระดาษเก่ามุมขวาบน · แตะเพื่อเปิดสมุดคดี) ----------
        void BuildQuest()
        {
            quest = new VisualElement { pickingMode = PickingMode.Position }; Abs(quest, null, 22, 136);
            quest.style.width = 250; Skin(quest, "parchment", 20); quest.style.paddingTop = 10; quest.style.paddingBottom = 12; quest.style.paddingLeft = 16; quest.style.paddingRight = 14;
            quest.style.opacity = 0.86f;
            root.Add(quest);
            var title = L(quest, "บันทึกภารกิจ ▾", 24, new Color(0.3f, 0.18f, 0.08f)); title.style.unityFontDefinition = fontTitle; questTitle = title;
            VisualElement QRow(string icon, out Label head, out Label body)
            {
                var r = new VisualElement { pickingMode = PickingMode.Ignore }; r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.FlexStart; r.style.marginTop = 6; quest.Add(r);
                Ico(r, icon, 22).style.marginTop = 2;
                var col = new VisualElement { pickingMode = PickingMode.Ignore }; col.style.flexShrink = 1; col.style.marginLeft = 6; r.Add(col);
                head = L(col, "", 17, new Color(0.55f, 0.12f, 0.08f)); head.style.unityTextAlign = TextAnchor.MiddleLeft;
                body = L(col, "", 16, new Color(0.24f, 0.16f, 0.08f), false); body.style.unityTextAlign = TextAnchor.MiddleLeft;
                return r;
            }
            questMainRow = QRow("pin", out var h1, out questMain); h1.text = "เควสหลัก";
            questClueRow = QRow("magnifier", out var h2, out questClue); h2.text = "เบาะแส";
            try { questFolded = PlayerPrefs.GetInt("abot.questFold", 0) == 1; } catch (Exception) { }
            // แตะหัวกระดาษ = พับ/กาง · แตะเนื้อหา = เปิดสมุดคดี
            title.pickingMode = PickingMode.Position;
            title.RegisterCallback<PointerDownEvent>(e => { questFolded = !questFolded; try { PlayerPrefs.SetInt("abot.questFold", questFolded ? 1 : 0); } catch (Exception) { } lastQuest = null; e.StopPropagation(); });
            quest.RegisterCallback<PointerDownEvent>(e => { if (Mystery.Started) OpenCase(); });
        }

        void UpdateQuest()
        {
            bool show = !CardOpen && !ModalOpen && fightBox.style.display != DisplayStyle.Flex && !FirstPersonView.Active && !(director != null && director.fishing != null && director.fishing.LineOut);   // สายอยู่ในน้ำ: ซ่อน ไม่ทับเกจระยะสาย
            quest.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;
            var g = Story.CurrentGoal;
            string main = g == null ? "ทำครบทุกเป้าหมายแล้ว" : Story.CanPayGoal ? $"{g.text} · เงินพอแล้ว!" : g.text;
            string clue = null;
            if (Mystery.Started)
            {
                var c = Mystery.Current;
                if (Mystery.Solved) clue = Mystery.HasNext ? "ปิดคดีแล้ว · พรุ่งนี้เช้าตามแผนที่ยายอิ่มไปจุดต่อไป" : "ตามรอยยายอิ่มครบทุกบทแล้ว";
                else if (Mystery.ReadyToSolve) clue = "เบาะแสครบแล้ว! แตะเพื่อเปิดสมุดคดีแล้วไขปริศนา";
                else
                {
                    Clue next = null; foreach (var x in c.clues) if (!Mystery.HasClue(x.id)) { next = x; break; }
                    var where = c.stage != Story.S.stage ? $"ไปที่{Stages.Get(c.stage).name} · " : "";
                    var hint = next?.hint ?? "";
                    if (hint.Length > 46) hint = hint.Substring(0, 44) + "…";   // สั้นพอ 2 บรรทัด · รายละเอียดเต็มอยู่ในสมุดคดี
                    clue = $"{where}{hint} ({Mystery.CluesFound}/{c.clues.Length})";
                }
            }
            var key = main + "|" + clue + "|" + questFolded;
            if (key == lastQuest) return;
            lastQuest = key;
            questMain.text = main;
            questTitle.text = questFolded ? "บันทึกภารกิจ ▸" : "บันทึกภารกิจ ▾";
            questMainRow.style.display = questFolded ? DisplayStyle.None : DisplayStyle.Flex;
            questClueRow.style.display = clue != null && !questFolded ? DisplayStyle.Flex : DisplayStyle.None;
            questClue.text = clue ?? "";
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
            var brown = new Color(0.24f, 0.14f, 0.06f); var cream = new Color(1f, 0.93f, 0.78f);
            var title = L(morningBody, $"เช้าวันที่ {s.day}", 50, cream); title.style.unityFontDefinition = fontTitle; Shadow(title);
            // ข่าวหมู่บ้าน: แผ่นกระดาษประกาศ (อากาศ · ราคาตลาด)
            VisualElement Notice(string text, Color col)
            {
                var n = new VisualElement { pickingMode = PickingMode.Ignore }; Skin(n, "parchment", 20); Pad(n, 12, 30); n.style.marginTop = 8; n.style.alignSelf = Align.Center;
                morningBody.Add(n); L(n, text, 26, col); return n;
            }
            Notice(Story.CurrentLuck.text, brown);
            var hot = s.hot != null ? FishDatabase.Get(s.hot) : null;
            if (hot != null) Notice($"วันนี้แม่ค้ารับซื้อ{(FishDex.Has(hot.id) ? hot.name : "ปลาลึกลับ")}ราคาพิเศษ {Story.Baht(Story.PriceOf(hot.id))}/กก.", new Color(0.55f, 0.2f, 0.05f));
            // ป้ายไม้: เงินเก็บ · ความสุขครอบครัว
            var stats = Row(morningBody); stats.style.marginTop = 10;
            VisualElement Plaque(string icon, string text, Color col)
            {
                var pl = new VisualElement { pickingMode = PickingMode.Ignore }; Skin(pl, "wood", 18); Pad(pl, 10, 22); pl.style.flexDirection = FlexDirection.Row; pl.style.alignItems = Align.Center;
                pl.style.marginLeft = pl.style.marginRight = 6; stats.Add(pl); Ico(pl, icon, 32); L(pl, text, 26, col).style.marginLeft = 8; return pl;
            }
            Plaque("coin", $"เงินเก็บ {Story.Baht(s.money)}", Money);
            Plaque("heart", $"ครอบครัว {(s.happy >= 75 ? "มีความสุข" : s.happy >= 40 ? "สบายดี" : "หิวโหย")} {s.happy}%", cream);
            // เป้าหมายครอบครัว: กรอบไม้ไผ่บนกระดาษ + ภาพสเก็ตช์ของเป้าหมาย
            var g = Story.CurrentGoal;
            var gb = new VisualElement { pickingMode = PickingMode.Ignore }; Skin(gb, "bamboo_paper", 26); gb.style.paddingTop = 22; gb.style.paddingBottom = 24; gb.style.paddingLeft = 30; gb.style.paddingRight = 26;
            gb.style.marginTop = 12; gb.style.flexDirection = FlexDirection.Row; gb.style.alignItems = Align.Center; morningBody.Add(gb);
            var gcol = new VisualElement { pickingMode = PickingMode.Ignore }; gcol.style.flexGrow = 1; gcol.style.flexShrink = 1; gb.Add(gcol);
            if (g != null)
            {
                var gl = L(gcol, $"เป้าหมาย: {g.text}", 30, brown); gl.style.unityTextAlign = TextAnchor.MiddleLeft; gl.style.unityFontDefinition = fontTitle;
                var bar = Bar(gcol, 24, new Color(0.3f, 0.22f, 0.14f, 0.5f)); var fill = Box(bar, new Color(0.9f, 0.5f, 0.15f), 12, false); fill.style.height = Length.Percent(100);
                fill.style.width = Length.Percent(Mathf.Clamp01((float)s.money / g.cost) * 100);
                var gr = Row(gcol); gr.style.justifyContent = Justify.SpaceBetween; gr.style.marginTop = 8; gr.style.alignItems = Align.Center;
                L(gr, $"{Story.Baht(s.money)} / {Story.Baht(g.cost)}", 24, brown);
                if (Story.CanPayGoal) { var pb = new Button(() => onMorningPay?.Invoke()) { text = s.goal == 0 ? "จ่ายค่าซ่อมหลังคา" : "จ่ายเลย" }; WoodButton(pb, true); gr.Add(pb); }
                else if (s.goal >= 2 && !Mystery.AllSolved) L(gr, "(ตามรอยแผนที่ยายอิ่มให้ครบทุกคดีก่อน)", 20, new Color(0.45f, 0.3f, 0.18f), false);
                var sk = new VisualElement { pickingMode = PickingMode.Ignore }; sk.style.width = 180; sk.style.height = 120; sk.style.flexShrink = 0; sk.style.marginLeft = 12;
                sk.style.backgroundImage = Resources.Load<Texture2D>("UI/Icons/" + (s.goal == 0 ? "goal_roof" : s.goal == 1 ? "goal_bike" : "goal_fair")); gb.Add(sk);
            }
            else L(gcol, "ทำครบทุกเป้าหมายแล้ว! ตกปลาสบาย ๆ ได้ตลอดไป", 26, brown);
            if (!Story.RoofFixed) { var lk = L(morningBody, "หลังคาบ้านยังรั่วอยู่ เมื่อคืนต้องเอาถังรองน้ำฝน…", 22, cream, false); lk.style.marginTop = 8; Shadow(lk); }
            // เลือกที่ตกปลา: การ์ดกระดาษ + ภาพโพลารอยด์ปักหมุด · ความยาก · แว่นขยายถ้ามีคดีค้างที่ด่านนั้น
            var q = L(morningBody, "วันนี้จะไปตกปลาที่ไหน?", 34, cream); q.style.marginTop = 18; q.style.unityFontDefinition = fontTitle; Shadow(q);
            for (int si = 0; si < Stages.All.Length; si++)
            {
                var st = Stages.All[si];
                bool open = Stages.Unlocked(st.id);
                var row = new VisualElement { pickingMode = PickingMode.Ignore }; Skin(row, "parchment", 20); Pad(row, 14, 20);
                row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 10; morningBody.Add(row);
                var polaroid = new VisualElement { pickingMode = PickingMode.Ignore }; polaroid.style.backgroundColor = new Color(0.97f, 0.95f, 0.9f); Pad(polaroid, 6, 6); polaroid.style.paddingBottom = 16;
                polaroid.style.flexShrink = 0; polaroid.style.marginRight = 14; polaroid.style.rotate = new Rotate(si % 2 == 0 ? -3 : 2); row.Add(polaroid);
                var art = new VisualElement { pickingMode = PickingMode.Ignore }; art.style.width = 168; art.style.height = 108;
                art.style.backgroundImage = st.Art; art.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                art.style.unityBackgroundImageTintColor = open ? Color.white : new Color(0.45f, 0.42f, 0.4f); polaroid.Add(art);
                var pin = Ico(polaroid, "pin", 30); Abs(pin, 78, null, -14);
                var info = new VisualElement { pickingMode = PickingMode.Ignore }; info.style.flexGrow = 1; info.style.flexShrink = 1; info.style.marginRight = 10; row.Add(info);
                var nm = L(info, st.name, 32, open ? brown : new Color(0.45f, 0.36f, 0.28f)); nm.style.unityTextAlign = TextAnchor.MiddleLeft; nm.style.unityFontDefinition = fontTitle;
                var ds = L(info, open ? st.desc : $"{st.desc} · ค่าเดินทางครั้งแรก {Story.Baht(st.price)}", 18, new Color(0.32f, 0.22f, 0.12f), false); ds.style.unityTextAlign = TextAnchor.MiddleLeft;
                var tags = new VisualElement { pickingMode = PickingMode.Ignore }; tags.style.flexDirection = FlexDirection.Row; tags.style.alignItems = Align.Center; tags.style.marginTop = 4; info.Add(tags);
                int lvl = 1 + Mathf.RoundToInt(si * 4f / Mathf.Max(1, Stages.All.Length - 1));
                L(tags, "ความยาก", 18, new Color(0.6f, 0.35f, 0.1f)).style.marginRight = 4;
                for (int k = 0; k < 5; k++) Ico(tags, k < lvl ? "star_on" : "star_off", 20);
                if (Mystery.Started && !Mystery.Solved && Mystery.Current.stage == st.id)
                {
                    Ico(tags, "magnifier", 24).style.marginLeft = 12;
                    L(tags, $"มีคดีค้าง ({Mystery.CluesFound}/{Mystery.Current.clues.Length})", 18, new Color(0.55f, 0.12f, 0.08f)).style.marginLeft = 4;
                }
                var id = st.id;
                if (open)
                {
                    var bt = new Button(() => { Hide(morning); onMorningGo?.Invoke(id); }) { text = "ไปตกที่นี่" };
                    WoodButton(bt, true); row.Add(bt);
                }
                else
                {
                    bool can = s.money >= st.price;
                    var bt = new Button(() => { if (Stages.Buy(id)) { Toast($"เปิดทางไป{Stages.Get(id).name}แล้ว!"); RefreshMorning(); } }) { text = $"ซื้อทาง {Story.Baht(st.price)}" };
                    WoodButton(bt, can); bt.SetEnabled(can); row.Add(bt);
                }
            }
        }

        // ปุ่มป้ายไม้เก่า (หน้าเช้า)
        void WoodButton(Button b, bool active)
        {
            StyleButton(b, Color.clear);
            Skin(b, "plank", 22); b.style.color = active ? new Color(0.22f, 0.12f, 0.04f) : new Color(0.4f, 0.34f, 0.28f);
            b.style.unityBackgroundImageTintColor = active ? Color.white : new Color(0.7f, 0.68f, 0.65f);
            Pad(b, 14, 30); b.style.fontSize = 26;
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
