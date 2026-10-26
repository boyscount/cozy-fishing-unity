using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // กระดาน "ปลาใหญ่ประจำสัปดาห์": แท็บด่านที่เปิดแล้ว · 10 อันดับแรก (ชื่อ ปลา น้ำหนัก) + อันดับของเรา · รีเซ็ตทุกสัปดาห์ (ตั้งในหน้า Unity Cloud)
    public partial class Hud
    {
        VisualElement board, boardList, boardTabs, boardEvent; Label boardInfo; string boardStage = "paddy";

        void BuildBoard()
        {
            board = new VisualElement { pickingMode = PickingMode.Position }; Abs(board, 0, 0, 0, 0); root.Add(board);
            board.style.backgroundColor = new Color(0.03f, 0.02f, 0.01f, 0.82f); board.style.alignItems = Align.Center; board.style.justifyContent = Justify.Center;
            board.style.display = DisplayStyle.None;
            var panel = new VisualElement { pickingMode = PickingMode.Position }; GoldFrame(panel, 3); panel.style.backgroundColor = new Color(0.12f, 0.08f, 0.04f, 0.97f);
            panel.style.width = Length.Percent(Narrow ? 94 : 60); panel.style.maxHeight = Length.Percent(90); Pad(panel, 16, 20); board.Add(panel);
            var trow = new VisualElement { pickingMode = PickingMode.Ignore }; trow.style.flexDirection = FlexDirection.Row; trow.style.alignItems = Align.Center; trow.style.justifyContent = Justify.Center; panel.Add(trow);
            Ico(trow, "trophy", Narrow ? 40 : 52).style.marginRight = 10;
            var title = L(trow, "ปลาใหญ่ประจำสัปดาห์", Narrow ? 30 : 36, ShopGold); title.style.unityFontDefinition = fontTitle;
            Ico(trow, "trophy", Narrow ? 40 : 52).style.marginLeft = 10;
            boardInfo = L(panel, "", 17, new Color(0.9f, 0.84f, 0.7f), false); boardInfo.style.marginTop = 2;
            boardEvent = new VisualElement(); boardEvent.style.marginTop = 8; panel.Add(boardEvent);
            boardTabs = new VisualElement(); boardTabs.style.flexDirection = FlexDirection.Row; boardTabs.style.flexWrap = Wrap.Wrap; boardTabs.style.marginTop = 10; panel.Add(boardTabs);
            var sv = new ScrollView(ScrollViewMode.Vertical); sv.style.flexGrow = 1; sv.style.flexShrink = 1; sv.style.marginTop = 10; sv.verticalScrollerVisibility = ScrollerVisibility.Hidden; panel.Add(sv);
            boardList = new VisualElement(); sv.Add(boardList);
            var close = new Button(() => { board.style.display = DisplayStyle.None; GameInput.Blocked = ModalOpen || CardOpen; }) { text = "ปิด" };
            StyleButton(close, Color.clear); GoldFrame(close, 2); close.style.alignSelf = Align.Center; close.style.marginTop = 12; close.style.fontSize = 22; close.style.color = new Color(1f, 0.9f, 0.75f); Pad(close, 6, 28);
            Hoverable(close, new Color(0.3f, 0.22f, 0.08f, 0.9f)); panel.Add(close);
        }

        public void OpenBoard()
        {
            if (board == null) BuildBoard();
            board.style.display = DisplayStyle.Flex; board.BringToFront(); GameInput.Blocked = true;
            FillBoardEvent();
            if (Weekly.CanJoin) boardStage = Weekly.Stage.id;   // เปิดที่แท็บด่านเทศกาลก่อน
            boardTabs.Clear();
            foreach (var st in Stages.All)
            {
                if (!Stages.Unlocked(st.id)) continue;
                var id = st.id;
                var tb = new Button(() => { boardStage = id; FillBoard(); }) { text = id == Weekly.Stage.id ? st.name + " · เทศกาล" : st.name }; tb.name = id; StyleButton(tb, Color.clear); GoldFrame(tb, 1);
                tb.style.fontSize = 16; tb.style.color = new Color(0.95f, 0.88f, 0.72f); Pad(tb, 4, 10); tb.style.marginRight = 5; tb.style.marginTop = 4;
                Hoverable(tb, new Color(0.38f, 0.28f, 0.1f, 0.95f)); boardTabs.Add(tb);
            }
            {   // แท็บมือไว: เวลาตีเบ็ดปลา ★3 ขึ้นไปที่เร็วที่สุดของแต่ละคน
                var qb = new Button(() => { boardStage = "_quick"; FillBoard(); }) { text = "มือไว ⚡" }; qb.name = "_quick"; StyleButton(qb, Color.clear); GoldFrame(qb, 1);
                qb.text = "มือไว"; qb.style.fontSize = 16; qb.style.color = new Color(0.95f, 0.88f, 0.72f); Pad(qb, 4, 10); qb.style.marginRight = 5; qb.style.marginTop = 4;
                Hoverable(qb, new Color(0.38f, 0.28f, 0.1f, 0.95f)); boardTabs.Add(qb);
            }
            if (boardStage != "_quick" && !Stages.Unlocked(boardStage)) boardStage = "paddy";
            FillBoard();
        }

        // ป้ายเทศกาลปลาใหญ่: ด่าน · เวลาที่เหลือ · ตัวหนักสุดของเรา · รางวัลสามขั้น (เหรียญทองแดง/เงิน/ทอง)
        void FillBoardEvent()
        {
            boardEvent.Clear();
            var box = new VisualElement(); GoldFrame(box, 2); Radius(box, 10); Pad(box, 10, 14); box.style.backgroundColor = new Color(0.36f, 0.22f, 0.05f, 0.92f); boardEvent.Add(box);
            var head = new VisualElement(); head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center; head.style.flexWrap = Wrap.Wrap; box.Add(head);
            var t = L(head, $"เทศกาลปลาใหญ่สัปดาห์นี้: {Weekly.Stage.name}", Narrow ? 22 : 26, new Color(1f, 0.88f, 0.5f)); t.style.unityFontDefinition = fontTitle; t.style.marginRight = 12;
            var left = Weekly.Left;
            L(head, left.TotalDays >= 1 ? $"เหลืออีก {(int)left.TotalDays} วัน {left.Hours} ชม." : $"เหลืออีก {left.Hours} ชม. {left.Minutes} นาที", 17, new Color(0.95f, 0.88f, 0.75f), false);
            var info = Weekly.CanJoin
                ? (Weekly.Best > 0 ? $"ตัวหนักสุดของคุณที่ด่านนี้สัปดาห์นี้: {Weekly.Best:0.00} กก." : "ยังไม่ได้ตกปลาที่ด่านนี้ในสัปดาห์นี้ — ไปลุยกัน!")
                : "ด่านนี้ยังไม่เปิดในเนื้อเรื่อง · ร่วมได้ในโหมดตกปลาอิสระ (หน้าแรก)";
            var il = L(box, info, 17, new Color(0.97f, 0.92f, 0.82f), false); il.style.marginTop = 4; il.style.whiteSpace = WhiteSpace.Normal; il.style.unityTextAlign = TextAnchor.MiddleLeft;
            var tiers = new VisualElement(); tiers.style.flexDirection = FlexDirection.Row; tiers.style.flexWrap = Wrap.Wrap; tiers.style.marginTop = 6; box.Add(tiers);
            for (int i = 0; i < 3; i++)
            {
                bool got = Weekly.Got(i);
                var c = new VisualElement(); c.style.flexDirection = FlexDirection.Row; c.style.alignItems = Align.Center; Pad(c, 4, 10); Radius(c, 8); c.style.marginRight = 8; c.style.marginTop = 4;
                c.style.backgroundColor = got ? new Color(0.25f, 0.42f, 0.12f, 0.95f) : new Color(0f, 0f, 0f, 0.3f); tiers.Add(c);
                var md = Ico(c, "medal_" + (3 - i), 30); md.style.marginRight = 6; if (!got) md.style.opacity = 0.55f;
                L(c, $"{Weekly.TierName[i]} {Weekly.Tier(i):0.0} กก. · {(got ? "ได้แล้ว" : Story.Baht(Weekly.TierReward[i]))}", 16, got ? new Color(0.8f, 1f, 0.7f) : new Color(0.95f, 0.88f, 0.72f), false);
            }
        }

        async void FillBoard()
        {
            foreach (var tb in boardTabs.Children())
            {   // แท็บด่านที่เลือก: พื้นทองเข้ม ขอบทองสว่าง ตัวขาว · แท็บอื่นโปร่ง ขอบทองจาง
                bool on = tb.name == boardStage;
                tb.style.backgroundColor = on ? new Color(0.55f, 0.4f, 0.12f, 1f) : (StyleColor)Color.clear;
                tb.style.borderTopColor = tb.style.borderBottomColor = tb.style.borderLeftColor = tb.style.borderRightColor = on ? new Color(1f, 0.85f, 0.4f) : new Color(ShopGold.r, ShopGold.g, ShopGold.b, 0.85f);
                tb.style.color = on ? Color.white : new Color(0.95f, 0.88f, 0.72f);
                tb.style.unityFontStyleAndWeight = on ? FontStyle.Bold : FontStyle.Normal;
            }
            boardList.Clear();
            if (boardStage == "_quick") boardInfo.text = Cloud.Ready ? "เวลาตีเบ็ดที่ไวที่สุด ตั้งแต่ทุ่นจมจนกด (ปลา 3 ดาวขึ้นไป) · รีเซ็ตทุกสัปดาห์" : "ออฟไลน์อยู่ · ต่อเน็ตแล้วเปิดใหม่อีกครั้ง";
            else boardInfo.text = Cloud.Ready ? $"คุณคือ \"{Cloud.PlayerName.Split('#')[0]}\" · ปลาตัวที่หนักที่สุดของแต่ละคนในสัปดาห์นี้" : "ออฟไลน์อยู่ · ต่อเน็ตแล้วเปิดใหม่อีกครั้ง";
            if (!Cloud.Ready) return;
            var wait = L(boardList, "กำลังโหลด…", 20, new Color(0.9f, 0.85f, 0.7f));
            string asked = boardStage;
            bool quick = asked == "_quick";
            var rows = quick ? await Cloud.TopBoard(Cloud.QuickBoard) : await Cloud.Top(asked);
            if (asked != boardStage || board.style.display == DisplayStyle.None) return;
            boardList.Clear();
            if (rows == null) { L(boardList, "ยังโหลดกระดานไม่ได้ ลองใหม่อีกครั้ง", 20, new Color(1f, 0.7f, 0.6f)); return; }
            if (rows.Count == 0) { L(boardList, "ยังไม่มีใครตกได้ในสัปดาห์นี้ — เป็นคนแรกเลย!", 20, new Color(0.9f, 0.85f, 0.7f)); return; }
            foreach (var r in rows)
            {
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 4; Pad(row, 6, 10);
                row.style.backgroundColor = r.me ? new Color(0.35f, 0.26f, 0.08f, 0.9f) : new Color(0f, 0f, 0f, 0.25f); Radius(row, 8);
                if (r.rank <= 3) { var md = Ico(row, "medal_" + r.rank, 40); md.style.marginRight = 4; md.style.width = 40; }   // เหรียญทอง เงิน ทองแดง
                else { var rk = L(row, r.rank.ToString(), 22, new Color(0.9f, 0.85f, 0.7f)); rk.style.width = 44; }
                var nm = L(row, r.me ? r.name + " (คุณ)" : r.name, 19, new Color(0.97f, 0.92f, 0.82f), false); nm.style.flexGrow = 1; nm.style.flexShrink = 1; nm.style.unityTextAlign = TextAnchor.MiddleLeft;
                var sp = FishDatabase.Get(r.fish);
                if (sp != null && sp.Photo != null)   // รูปปลาเล็ก ๆ หน้าชื่อ
                {
                    var ph = new VisualElement { pickingMode = PickingMode.Ignore }; ph.style.width = Narrow ? 46 : 60; ph.style.height = Narrow ? 26 : 34; ph.style.marginRight = 6; ph.style.flexShrink = 0;
                    ph.style.backgroundImage = sp.Photo; ph.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain); row.Add(ph);
                }
                var fish = L(row, sp?.name ?? "", 17, new Color(0.85f, 0.8f, 0.68f), false); fish.style.marginRight = 10; fish.style.flexShrink = 1;
                L(row, quick ? $"{r.kg / 1000.0:0.000} วิ" : $"{r.kg:0.00} กก.", 19, ShopGold).style.flexShrink = 0;
                boardList.Add(row);
            }
        }
    }
}
