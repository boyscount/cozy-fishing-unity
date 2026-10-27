using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // แถบลากเหยื่อปลอม (บนจอระหว่างลาก): ชื่อเหยื่อ · แถบ "ปลาสนใจ" · จุดจังหวะ (โตเต็มวง = แตะได้) · คำติชมล่าสุด (ดี! / เร็วไป / ช้าไป)
    public partial class Hud
    {
        VisualElement lureRow, lureBox, lureFill, lureBeat; Label lureName, lureSay;

        void BuildLure()
        {
            lureRow = CenterRow(null, TouchUi ? 380 : 110);   // ล่างจอเหนือคำแนะนำ (บนจอเป็นที่ของข้อความแจ้งเตือน)
            lureBox = Box(lureRow, Paper, 22); Pad(lureBox, 12, 20); MaxW(lureBox, 620);
            var head = new VisualElement { pickingMode = PickingMode.Ignore }; head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center; head.style.justifyContent = Justify.SpaceBetween; lureBox.Add(head);
            lureName = L(head, "", 22, Ink); lureName.style.unityTextAlign = TextAnchor.MiddleLeft;
            lureSay = L(head, "", 22, Gold); lureSay.style.unityTextAlign = TextAnchor.MiddleRight;
            var row = new VisualElement { pickingMode = PickingMode.Ignore }; row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 8; lureBox.Add(row);
            var lbl = L(row, "ปลาสนใจ", 20, Ink); lbl.style.marginRight = 10; lbl.style.flexShrink = 0;
            var bar = Bar(row, 22, Track); bar.style.flexGrow = 1;
            lureFill = Box(bar, Green, 11, false); lureFill.style.height = Length.Percent(100); lureFill.style.width = 0;
            var ring = new VisualElement { pickingMode = PickingMode.Ignore }; ring.style.width = ring.style.height = 34; ring.style.marginLeft = 12; ring.style.flexShrink = 0;
            ring.style.borderTopWidth = ring.style.borderBottomWidth = ring.style.borderLeftWidth = ring.style.borderRightWidth = 2;
            ring.style.borderTopColor = ring.style.borderBottomColor = ring.style.borderLeftColor = ring.style.borderRightColor = new Color(1, 1, 1, 0.6f);
            Radius(ring, 17); ring.style.alignItems = Align.Center; ring.style.justifyContent = Justify.Center; row.Add(ring);
            lureBeat = new VisualElement { pickingMode = PickingMode.Ignore }; Radius(lureBeat, 14); ring.Add(lureBeat);
            lureRow.style.display = DisplayStyle.None;
        }

        /// เรียกทุกเฟรมจาก FishingController
        public void Lure(bool show, RetrieveModel run)
        {
            if (lureRow == null) { if (!show) return; BuildLure(); }
            lureRow.style.display = show && run != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show || run == null) return;
            var it = Shop.Get(run.def.id);
            lureName.text = it != null ? it.name : "";
            lureFill.style.width = Length.Percent(Mathf.Clamp01(run.Interest) * 100);
            lureFill.style.backgroundColor = run.Interest > 0.75f ? Gold : Green;
            float r = run.Readiness, d = 6 + 22 * r;
            lureBeat.style.width = lureBeat.style.height = d; Radius(lureBeat, d / 2);
            lureBeat.style.backgroundColor = r >= 1 ? new Color(1f, 0.85f, 0.3f) : new Color(1, 1, 1, 0.5f);
            bool fresh = run.Time - run.FeedbackAt < 1.2f;
            lureSay.text = fresh ? run.Feedback : "";
        }
    }
}
