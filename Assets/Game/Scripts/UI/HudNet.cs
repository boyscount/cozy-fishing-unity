using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static CozyFishing.HudKit;

namespace CozyFishing
{
    // หน้าจอทอดแห: เกจแกว่งแห (ช่องเขียว = แหกางเต็มวง) · ป้าย PERFECT! · จังหวะสาวแห 3 ครั้ง · การ์ดสรุปปลาที่ติดแห · ปุ่มสลับ เบ็ด/แห
    public partial class Hud
    {
        VisualElement netRow, netGauge, netGood, netPerfect, netMark, netHaul, netBeat, netResultEl, netResultBody, netBtn;
        Button netCancel;
        VisualElement[] netKnots; Label netPopL, netCountL, netBtnBadge; float netPopUntil; bool netCanToggle;

        void BuildNet()
        {
            netRow = CenterRow(null, TouchUi ? 330 : 150);
            var box = Box(netRow, Paper, 24); Pad(box, 12, 22); MaxW(box, 620);
            // เกจแกว่งแห
            netGauge = new VisualElement { pickingMode = PickingMode.Ignore }; box.Add(netGauge);
            var gl = L(netGauge, "แกว่งแห · ปล่อยตอนเข็มเข้าช่องเขียว", 22, Ink); Shadow(gl);
            var bar = Bar(netGauge, 30, Track);
            netGood = Box(bar, new Color(0.45f, 0.78f, 0.4f, 0.55f), 0, false); Abs(netGood, null, null, 0, 0);
            netPerfect = Box(bar, new Color(0.4f, 0.95f, 0.45f), 0, false); Abs(netPerfect, null, null, 0, 0);
            netMark = Box(bar, Ink, 5, false); Abs(netMark, null, null, -2, -2); netMark.style.width = 12;
            float c0 = CastNet.ZoneCenter;
            netGood.style.left = Length.Percent((c0 - CastNet.GoodHalf) * 100); netGood.style.width = Length.Percent(CastNet.GoodHalf * 200);
            netPerfect.style.left = Length.Percent((c0 - CastNet.PerfectHalf) * 100); netPerfect.style.width = Length.Percent(CastNet.PerfectHalf * 200);
            // สาวแห: ปมเชือก 3 ปม + วงจังหวะ
            netHaul = new VisualElement { pickingMode = PickingMode.Ignore }; netHaul.style.flexDirection = FlexDirection.Row; netHaul.style.alignItems = Align.Center; netHaul.style.justifyContent = Justify.Center; box.Add(netHaul);
            netCountL = L(netHaul, "", 22, Ink); netCountL.style.marginRight = 14; netCountL.AddToClassList(TextLocalizer.NoLoc);
            netKnots = new VisualElement[CastNet.Pulls];
            for (int i = 0; i < netKnots.Length; i++)
            {
                var k = Box(netHaul, Track, 11, false); k.style.width = k.style.height = 22; k.style.marginRight = 8; netKnots[i] = k;
            }
            var ringEl = new VisualElement { pickingMode = PickingMode.Ignore }; ringEl.style.width = ringEl.style.height = 40; ringEl.style.marginLeft = 8;
            ringEl.style.borderTopWidth = ringEl.style.borderBottomWidth = ringEl.style.borderLeftWidth = ringEl.style.borderRightWidth = 2;
            ringEl.style.borderTopColor = ringEl.style.borderBottomColor = ringEl.style.borderLeftColor = ringEl.style.borderRightColor = new Color(1, 1, 1, 0.7f);
            Radius(ringEl, 20); ringEl.style.alignItems = Align.Center; ringEl.style.justifyContent = Justify.Center; netHaul.Add(ringEl);
            netBeat = new VisualElement { pickingMode = PickingMode.Ignore }; ringEl.Add(netBeat);
            netRow.style.display = DisplayStyle.None;

            // ป้าย PERFECT! / ดี! / แหพับ… กลางจอ
            var popRow = CenterRow(TouchUi ? 230 : 200, null);
            netPopL = L(popRow, "", 64, new Color(1f, 0.88f, 0.3f)); netPopL.style.unityFontDefinition = fontTitle; Shadow(netPopL);
            netPopL.style.display = DisplayStyle.None;
        }

        /// เรียกทุกเฟรมจาก FishingHudView
        internal void Net(bool show, NetThrow n)
        {
            if (netRow == null) { if (!show && netPopL == null) return; BuildNet(); }
            netPopL.style.display = Time.time < netPopUntil ? DisplayStyle.Flex : DisplayStyle.None;
            if (Time.time < netPopUntil) netPopL.style.scale = new Scale(Vector3.one * (1f + 0.25f * Mathf.Clamp01((netPopUntil - Time.time - 0.7f) / 0.3f)));
            bool swing = show && n != null && n.Swinging, haul = show && n != null && n.Hauling;
            netRow.style.display = swing || haul ? DisplayStyle.Flex : DisplayStyle.None;
            if (netCancel == null) { netCancel = new Button(() => GameInput.PullButton = true) { text = "ยกเลิก" }; WoodButton(netCancel, true); netCancel.style.position = Position.Absolute; netCancel.style.right = TouchUi ? 300 : 230; netCancel.style.bottom = TouchUi ? 70 : 50; netCancel.style.fontSize = 24; root.Add(netCancel); }
            netCancel.style.display = swing ? DisplayStyle.Flex : DisplayStyle.None;   // ยกเลิกทอดแหระหว่างแกว่ง
            if (!swing && !haul) return;
            netGauge.style.display = swing ? DisplayStyle.Flex : DisplayStyle.None;
            netHaul.style.display = haul ? DisplayStyle.Flex : DisplayStyle.None;
            if (swing)
            {
                netMark.style.left = Length.Percent(n.Needle * 100);
                bool hot = CastNet.GradeOf(n.Needle) == CastNet.Grade.Perfect;
                netMark.style.backgroundColor = hot ? new Color(1f, 0.9f, 0.3f) : Ink;
            }
            else
            {
                netCountL.text = Loc.T($"ติดแห {n.InNet} ตัว");
                for (int i = 0; i < netKnots.Length; i++) netKnots[i].style.backgroundColor = i < n.PullsDone ? new Color(0.95f, 0.75f, 0.3f) : Track;
                float r = CastNet.PullPulse(n.HaulTime), d = 8 + 26 * r;
                netBeat.style.width = netBeat.style.height = d; Radius(netBeat, d / 2);
                netBeat.style.backgroundColor = CastNet.PullOnBeat(n.HaulTime) ? new Color(1f, 0.85f, 0.3f) : new Color(1, 1, 1, 0.45f);
            }
        }

        public void NetPop(string text, bool big)
        {
            if (netPopL == null) BuildNet();
            netPopL.text = text; netPopL.style.fontSize = big ? 72 : 52; netPopUntil = Time.time + 1.1f;
            netPopL.style.color = big ? new Color(1f, 0.88f, 0.3f) : new Color(0.95f, 0.96f, 0.97f);
        }

        /// ปุ่มสลับ เบ็ด/แห (แถบไอคอนมุมขวาบน) · แสดงเฉพาะด่านที่ทอดแหได้ และมีแหแล้ว
        public void NetButton(bool show, bool usingNet, int left, bool canToggle)
        {
            netCanToggle = canToggle;
            if (netBtn == null)
            {
                if (!show) return;
                netBtn = IconButton("net", new Color(0.8f, 0.72f, 0.5f), () =>
                {
                    if (!netCanToggle) return;
                    CastNet.Toggle(); Sfx.Nibble();
                    Toast(CastNet.Using ? $"ถือแหแล้ว · วันนี้ทอดได้อีก {CastNet.ThrowsLeft} ครั้ง" : "กลับมาใช้เบ็ดแล้ว");
                }, out netBtnBadge);
                netBtnBadge.AddToClassList(TextLocalizer.NoLoc);   // ตั้งทุกเฟรม → แปลเอง
            }
            netBtn.style.display = show && !CardOpen && !ModalOpen ? DisplayStyle.Flex : DisplayStyle.None;
            netBtnBadge.text = Loc.T(usingNet ? $"แห {left}/{CastNet.ThrowsPerDay}" : "ใช้แห");
            netBtn.style.opacity = usingNet ? 1f : 0.85f;
        }

        // ---------- ปุ่มขึ้น/ลงแพ (บ่อหลังบ้าน) ----------
        VisualElement raftBtn; Label raftBadge; bool raftAboard, raftCanLand;
        public void RaftButton(bool show, bool aboard, bool canLand)
        {
            raftAboard = aboard; raftCanLand = canLand;
            if (raftBtn == null)
            {
                if (!show) return;
                raftBtn = IconButton("raft", new Color(0.75f, 0.62f, 0.38f), () =>
                {
                    var r = Raft.Current; if (r == null) return;
                    if (!raftAboard) r.Board();
                    else if (raftCanLand) r.Leave();
                    else Toast("ถ่อแพเข้าใกล้ฝั่งก่อนถึงจะลงได้");
                }, out raftBadge);
                raftBadge.AddToClassList(TextLocalizer.NoLoc);   // ตั้งทุกเฟรม → แปลเอง
            }
            raftBtn.style.display = show && !CardOpen && !ModalOpen ? DisplayStyle.Flex : DisplayStyle.None;
            raftBadge.text = Loc.T(!aboard ? "ขึ้นแพ" : canLand ? "ลงแพ" : "ถ่อเข้าฝั่ง");
            raftBtn.style.opacity = !aboard || canLand ? 1f : 0.7f;
        }

        // ---------- การ์ดสรุปปลาที่ติดแห ----------
        public bool NetResultOpen => netResultEl != null && netResultEl.style.display == DisplayStyle.Flex;

        public void NetResult(List<CastNet.Haul> hauls, bool anyNew, Action keep, Action release, string title = null, string note = null)
        {
            if (netResultEl == null)
            {
                netResultEl = Overlay(out var box, 640);
                Skin(box, "parchment", 20); box.style.paddingTop = 24; box.style.paddingBottom = 24; box.style.paddingLeft = 30; box.style.paddingRight = 30;
                netResultBody = new VisualElement { pickingMode = PickingMode.Ignore }; box.Add(netResultBody);
            }
            var ink = new Color(0.24f, 0.14f, 0.06f);
            netResultBody.Clear();
            int total = 0; float kg = 0; int baht = 0;
            foreach (var h in hauls) { total += h.count; kg += h.kg; baht += CastNet.ValueOf(h); }
            var t = L(netResultBody, title ?? $"ได้ปลาจากแห {total} ตัว!", 40, ink); t.style.unityFontDefinition = fontTitle;
            if (anyNew) L(netResultBody, "มีปลาชนิดใหม่ในสมุดปลา!", 22, new Color(0.7f, 0.35f, 0.05f)).style.marginTop = 2;
            foreach (var h in hauls)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore }; row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 10; netResultBody.Add(row);
                FishThumb(row, h.sp, 96, 56);
                var nm = L(row, $"{h.sp.name} ×{h.count}", 26, ink); nm.style.flexGrow = 1; nm.style.unityTextAlign = TextAnchor.MiddleLeft; nm.style.marginLeft = 10;
                L(row, $"{h.kg:0.00} กก. · {Story.Baht(CastNet.ValueOf(h))}", 22, new Color(0.42f, 0.3f, 0.17f), false);
            }
            var sum = L(netResultBody, $"รวม {kg:0.00} กก. · ขายได้ราว {Story.Baht(baht)}", 24, ink); sum.style.marginTop = 12;
            L(netResultBody, note ?? "ปลาจากแหใส่ข้องรวมเป็นกองละชนิด (ชนิดละช่อง)", 18, new Color(0.45f, 0.35f, 0.22f), false).style.marginTop = 4;
            var btns = new VisualElement { pickingMode = PickingMode.Ignore }; btns.style.flexDirection = FlexDirection.Row; btns.style.justifyContent = Justify.Center; btns.style.marginTop = 16; netResultBody.Add(btns);
            void Close() { Hide(netResultEl); GameInput.Blocked = ModalOpen || CardOpen; }
            var kb = new Button(() => { Close(); keep(); }) { text = "เก็บใส่ข้อง" }; WoodButton(kb, true); kb.style.marginRight = 12; btns.Add(kb);
            var rb = new Button(() => { Close(); release(); }) { text = "ปล่อยคืนบ่อ" }; WoodButton(rb, true); btns.Add(rb);
            Show(netResultEl); netResultEl.BringToFront(); GameInput.Blocked = true;
            TextLocalizer.Now();
            if (FishingController.Bot) netResultEl.schedule.Execute(() => { if (NetResultOpen) { Close(); keep(); } }).ExecuteLater(3500);   // ทดสอบ: บอทเก็บเอง
        }
    }
}
