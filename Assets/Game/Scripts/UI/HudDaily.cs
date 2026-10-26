using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // ภารกิจรายวัน: ปุ่มปฏิทินบนแถบไอคอน (ป้าย "1/3") → แผงปลาเด่นวันนี้ + ภารกิจ 3 อย่าง + โบนัสครบชุด · รีเซ็ตเที่ยงคืนเวลาไทย (Daily.cs)
    public partial class Hud
    {
        VisualElement dailyBtn, daily, dailyPanel, dailyBody; Label dailyBadge, dailyInfo; float dailyBadgeAt; int dailyVer = -1;

        void BuildDaily()
        {
            dailyBtn = IconButton("calendar", new Color(0.9f, 0.75f, 0.4f), OpenDaily, out dailyBadge);
            daily = new VisualElement { pickingMode = PickingMode.Position }; Abs(daily, 0, 0, 0, 0); root.Add(daily);
            daily.style.backgroundColor = new Color(0.03f, 0.02f, 0.01f, 0.82f); daily.style.alignItems = Align.Center; daily.style.justifyContent = Justify.Center;
            daily.style.display = DisplayStyle.None;
            daily.RegisterCallback<PointerDownEvent>(e => { if (e.target == daily) CloseDaily(); });   // แตะพื้นหลังปิด
            var panel = new VisualElement { pickingMode = PickingMode.Position }; GoldFrame(panel, 3); panel.style.backgroundColor = new Color(0.12f, 0.08f, 0.04f, 0.97f);
            panel.style.width = Length.Percent(Narrow ? 94 : 56); panel.style.maxWidth = 900; panel.style.maxHeight = Length.Percent(92); Pad(panel, 14, 20); daily.Add(panel); dailyPanel = panel;
            var trow = new VisualElement { pickingMode = PickingMode.Ignore }; trow.style.flexDirection = FlexDirection.Row; trow.style.alignItems = Align.Center; trow.style.justifyContent = Justify.Center; panel.Add(trow);
            Ico(trow, "calendar", Narrow ? 36 : 46).style.marginRight = 10;
            var title = L(trow, "ภารกิจประจำวัน", Narrow ? 28 : 34, ShopGold); title.style.unityFontDefinition = fontTitle;
            dailyInfo = L(panel, "", 16, new Color(0.9f, 0.84f, 0.7f), false); dailyInfo.style.marginTop = 2;
            var sv = new ScrollView(ScrollViewMode.Vertical); sv.style.flexGrow = 1; sv.style.flexShrink = 1; sv.style.marginTop = 8; sv.verticalScrollerVisibility = ScrollerVisibility.Hidden; panel.Add(sv);
            dailyBody = new VisualElement(); sv.Add(dailyBody);
            var close = new Button(CloseDaily) { text = "ปิด" };
            StyleButton(close, Color.clear); GoldFrame(close, 2); close.style.alignSelf = Align.Center; close.style.marginTop = 10; close.style.fontSize = 22; close.style.color = new Color(1f, 0.9f, 0.75f); Pad(close, 6, 28);
            Hoverable(close, new Color(0.3f, 0.22f, 0.08f, 0.9f)); panel.Add(close);
            if (GameUrl.Value.Contains("opendaily")) root.schedule.Execute(() => { if (!dailyTested && Time.time > 8 && !CardOpen && !ModalOpen && Story.Phase == StoryPhase.Fishing) { dailyTested = true; OpenDaily(); } }).Every(500);   // ทดสอบ
            if (GameUrl.Value.Contains("openboard")) root.schedule.Execute(() => { if (!boardTested && Time.time > 8 && !CardOpen && !ModalOpen && Story.Phase == StoryPhase.Fishing) { boardTested = true; OpenBoard(); } }).Every(500);   // ทดสอบ: เปิดกระดานปลาใหญ่
            if (GameUrl.Value.Contains("openward")) root.schedule.Execute(() => { if (!wardTested && Time.time > 8 && !CardOpen && !ModalOpen && Story.Phase == StoryPhase.Fishing) { wardTested = true; OpenWardrobe(); } }).Every(500);   // ทดสอบ: เปิดตู้แต่งตัว
            if (GameUrl.Value.Contains("opengear")) root.schedule.Execute(() => { if (!gearTested && Time.time > 8 && !CardOpen && !ModalOpen && Story.Phase == StoryPhase.Fishing) { gearTested = true; ShowGear(); } }).Every(500);   // ทดสอบ: เปิดหน้าอุปกรณ์
        }
        bool dailyTested, boardTested, wardTested, gearTested;

        void CloseDaily() { daily.style.display = DisplayStyle.None; GameInput.Blocked = ModalOpen || CardOpen; }

        public void OpenDaily()
        {
            daily.style.display = DisplayStyle.Flex; daily.BringToFront(); GameInput.Blocked = true;
            dailyPanel.style.width = Length.Percent(Narrow ? 94 : 56);
            var st = Daily.S; var left = Daily.UntilReset; dailyVer = Daily.Version;
            dailyInfo.text = $"เป้าใหม่ทุกวัน · เหลือเวลาอีก {(int)left.TotalHours} ชม. {left.Minutes} นาที";
            dailyBody.Clear();
            // ปลาเด่นวันนี้: รูป ชื่อ ด่าน ช่วงเวลา รางวัล
            var sp = FishDatabase.Get(st.featured);
            if (sp != null)
            {
                var card = new VisualElement(); card.style.flexDirection = FlexDirection.Row; card.style.alignItems = Align.Center; Pad(card, 10, 12); Radius(card, 10);
                card.style.backgroundColor = st.featDone ? new Color(0.2f, 0.3f, 0.12f, 0.9f) : new Color(0.38f, 0.26f, 0.07f, 0.9f); GoldFrame(card, 2); dailyBody.Add(card);
                var ph = new VisualElement { pickingMode = PickingMode.Ignore }; ph.style.width = Narrow ? 110 : 150; ph.style.height = Narrow ? 62 : 84; ph.style.flexShrink = 0; ph.style.marginRight = 12;
                if (sp.Photo != null) { ph.style.backgroundImage = sp.Photo; ph.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain); }
                card.Add(ph);
                var col = new VisualElement(); col.style.flexGrow = 1; col.style.flexShrink = 1; card.Add(col);
                var tag = new VisualElement(); tag.style.flexDirection = FlexDirection.Row; tag.style.alignItems = Align.Center; col.Add(tag);
                Ico(tag, "star_on", 20).style.marginRight = 4;
                L(tag, "ปลาเด่นวันนี้", 17, new Color(1f, 0.85f, 0.45f));
                var nm = L(col, sp.name, Narrow ? 26 : 30, Color.white); nm.style.unityFontDefinition = fontTitle; nm.style.unityTextAlign = TextAnchor.MiddleLeft;
                var where = L(col, $"{Stages.Get(sp.stage).name} · {Daily.TimeHint(sp.time)}", 16, new Color(0.92f, 0.86f, 0.74f), false); where.style.unityTextAlign = TextAnchor.MiddleLeft; where.style.whiteSpace = WhiteSpace.Normal;
                var rw = L(card, st.featDone ? "ได้แล้ว!" : $"รางวัล\n{Story.Baht(Daily.FeaturedReward(sp))}", Narrow ? 18 : 21, st.featDone ? new Color(0.7f, 1f, 0.6f) : ShopGold);
                rw.style.flexShrink = 0; rw.style.marginLeft = 8; rw.style.unityTextAlign = TextAnchor.MiddleCenter;
            }
            // ภารกิจ 3 อย่าง: ข้อความ แถบความคืบหน้า รางวัล
            foreach (var q in st.quests)
            {
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 8; Pad(row, 8, 12); Radius(row, 8);
                row.style.backgroundColor = q.done ? new Color(0.16f, 0.26f, 0.1f, 0.85f) : new Color(0f, 0f, 0f, 0.28f); dailyBody.Add(row);
                Ico(row, q.done ? "stamp_on" : "stamp_off", 34).style.marginRight = 10;
                var col = new VisualElement(); col.style.flexGrow = 1; col.style.flexShrink = 1; row.Add(col);
                var t = L(col, Daily.Text(q), 19, new Color(0.97f, 0.92f, 0.82f), false); t.style.unityTextAlign = TextAnchor.MiddleLeft; t.style.whiteSpace = WhiteSpace.Normal;
                var barRow = new VisualElement(); barRow.style.flexDirection = FlexDirection.Row; barRow.style.alignItems = Align.Center; barRow.style.marginTop = 4; col.Add(barRow);
                var bar = new VisualElement(); bar.style.height = 10; bar.style.flexGrow = 1; bar.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f); Radius(bar, 5); barRow.Add(bar);
                var fill = new VisualElement(); fill.style.height = Length.Percent(100); fill.style.width = Length.Percent(Daily.Fraction(q) * 100); Radius(fill, 5);
                fill.style.backgroundColor = q.done ? new Color(0.5f, 0.85f, 0.35f) : new Color(0.95f, 0.72f, 0.3f); bar.Add(fill);
                L(barRow, Daily.Progress(q), 15, new Color(0.9f, 0.84f, 0.7f), false).style.marginLeft = 8;
                var rw = L(row, q.done ? "สำเร็จ" : Story.Baht(q.reward), 20, q.done ? new Color(0.7f, 1f, 0.6f) : ShopGold); rw.style.flexShrink = 0; rw.style.marginLeft = 10; rw.style.minWidth = 60; rw.style.unityTextAlign = TextAnchor.MiddleRight;
            }
            var bonus = L(dailyBody, st.allDone ? $"ครบทุกภารกิจแล้ว! ได้โบนัส {Story.Baht(Daily.AllBonus)} · พรุ่งนี้มาใหม่นะ" : $"ทำครบ 3 ภารกิจ รับโบนัสเพิ่ม {Story.Baht(Daily.AllBonus)}", 18, st.allDone ? new Color(0.7f, 1f, 0.6f) : new Color(1f, 0.85f, 0.5f));
            bonus.style.marginTop = 10; bonus.style.whiteSpace = WhiteSpace.Normal;
        }

        // เรียกทุกเฟรมจาก Update: แสดงเฉพาะตอนตกปลา · ป้ายใต้ปุ่มอัปเดตวินาทีละครั้ง
        void UpdateDailyBtn(bool play)
        {
            if (dailyBtn == null) return;
            if (Shown(daily) && dailyVer != Daily.Version) OpenDaily();
            dailyBtn.style.display = play ? DisplayStyle.Flex : DisplayStyle.None;
            if (!play || Time.unscaledTime < dailyBadgeAt) return;
            dailyBadgeAt = Time.unscaledTime + 1;
            var st = Daily.S;
            dailyBadge.text = st.allDone && (st.featDone || st.featured == null) ? "ครบแล้ว" : $"{Daily.DoneCount}/{st.quests.Count}";
        }
    }
}
