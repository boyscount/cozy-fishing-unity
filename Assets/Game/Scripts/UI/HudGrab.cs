using UnityEngine;
using UnityEngine.UIElements;
using static CozyFishing.HudKit;

namespace CozyFishing
{
    // หน้าจองมปลา: มือสองข้างยื่นจากขอบล่าง (เลื่อนขึ้นตามระยะที่ยื่นเข้าโพรง สั่นตอนปลาดิ้น) · ป้ายสัมผัส · เกจบีบ · ปุ่ม "ชักมือ" · งมได้อีก/แผลที่มือ
    public partial class Hud
    {
        VisualElement grabEl, grabHandL, grabHandR, grabGauge, grabGood, grabMark, grabProg;
        Label grabFeel, grabTip, grabInfo; Button grabPull;

        void BuildGrab()
        {
            grabEl = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(grabEl, 0, 0, 0, 0); root.Add(grabEl);
            VisualElement Hand(string tex, bool left)
            {
                var h = new VisualElement { pickingMode = PickingMode.Ignore }; h.style.position = Position.Absolute; h.style.bottom = 0;
                h.style.width = Length.Percent(TouchUi ? 34 : 26); h.style.height = Length.Percent(52);
                if (left) h.style.left = Length.Percent(14); else h.style.right = Length.Percent(14);
                h.style.backgroundImage = Resources.Load<Texture2D>("UI/Grab/" + tex); h.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                h.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Bottom);
                grabEl.Add(h); return h;
            }
            grabHandL = Hand("hand_l", true); grabHandR = Hand("hand_r", false);
            // ป้ายสัมผัสกลางจอ
            var top = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(top, 0, 0, TouchUi ? 330 : 290, null); top.style.alignItems = Align.Center; grabEl.Add(top);   // ต่ำกว่าแถบข้อความแจ้งเตือนด้านบน
            var box = Box(top, Paper, 22); Pad(box, 12, 26); MaxW(box, 720);
            grabFeel = L(box, "", 30, new Color(1f, 0.92f, 0.72f)); grabFeel.AddToClassList(TextLocalizer.NoLoc); Shadow(grabFeel);
            grabTip = L(box, "", 20, Ink, false); grabTip.AddToClassList(TextLocalizer.NoLoc); grabTip.style.marginTop = 6;
            // เกจบีบ (เฉพาะตอนจับปลา)
            grabGauge = new VisualElement { pickingMode = PickingMode.Ignore }; box.Add(grabGauge);
            var bar = Bar(grabGauge, 28, Track);
            grabGood = Box(bar, new Color(0.4f, 0.9f, 0.45f, 0.8f), 0, false); Abs(grabGood, null, null, 0, 0);
            var red = Box(bar, new Color(0.95f, 0.3f, 0.25f, 0.7f), 0, false); Abs(red, null, 0, 0, 0); red.style.width = Length.Percent(6);
            grabMark = Box(bar, Ink, 5, false); Abs(grabMark, null, null, -2, -2); grabMark.style.width = 12;
            var pb = Bar(grabGauge, 8, Track); grabProg = Box(pb, Gold, 4, false); grabProg.style.height = Length.Percent(100);
            grabInfo = L(top, "", 18, Ink, false); grabInfo.AddToClassList(TextLocalizer.NoLoc); Shadow(grabInfo); grabInfo.style.marginTop = 6;
            // ปุ่มชักมือ (ซ้ายของปุ่มหลัก)
            grabPull = new Button(() => GameInput.PullButton = true) { text = TouchUi ? "ชักมือ" : "ชักมือ (Q)" };
            WoodButton(grabPull, true); grabPull.style.position = Position.Absolute; grabPull.style.right = TouchUi ? 300 : 230; grabPull.style.bottom = TouchUi ? 70 : 50;
            grabPull.style.fontSize = 26; grabEl.Add(grabPull);
            grabEl.style.display = DisplayStyle.None;
        }

        /// เรียกทุกเฟรมจาก FishingHudView
        internal void Grab(bool show, GrabPlay g)
        {
            if (grabEl == null) { if (!show) return; BuildGrab(); }
            grabEl.style.display = show && g != null && !NetResultOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show || g == null) return;
            grabHandL.style.display = grabHandR.style.display = DisplayStyle.None;   // ใช้มือจริงของตัวละคร (HandIK) แทนภาพวาด
            var ph = g.phase;
            float reach = ph == GrabPlay.Phase.Reach ? g.Reach : ph == GrabPlay.Phase.Dive ? 0 : 1;
            float sh = Mathf.Abs(g.Shake);
            float y = Mathf.Lerp(62, 6, reach);
            grabHandL.style.translate = new Translate(Length.Percent(Mathf.Sin(Time.time * 31) * sh * 3), Length.Percent(y + Mathf.Sin(Time.time * 1.3f) * 1.5f));
            grabHandR.style.translate = new Translate(Length.Percent(Mathf.Sin(Time.time * 27 + 1) * sh * 3), Length.Percent(y + Mathf.Sin(Time.time * 1.1f + 1) * 1.5f));
            grabHandL.style.rotate = new Rotate(Mathf.Lerp(-6, 8, reach)); grabHandR.style.rotate = new Rotate(Mathf.Lerp(6, -8, reach));
            string feel = ph switch
            {
                GrabPlay.Phase.Dive => "ก้มลงงมใต้น้ำ…",
                GrabPlay.Phase.Reach => "คลำเข้าไปในโพรง…",
                _ => g.Feel ?? "",
            };
            grabFeel.text = Loc.T(feel);
            string key = TouchUi ? "ปุ่มหลัก" : "E/คลิก";
            grabTip.text = Loc.T(ph switch
            {
                GrabPlay.Phase.Reach => $"กดค้าง {key} เพื่อยื่นมือเข้าโพรง",
                GrabPlay.Phase.Feel => TouchUi ? "จับ = ปุ่มหลัก · ไม่แน่ใจ = ชักมือ" : "จับ = E/คลิกซ้าย · ไม่แน่ใจ = ชักมือ (Q/คลิกขวา)",
                GrabPlay.Phase.Grip => $"กดค้างบีบ ปล่อยคลาย ให้เข็มอยู่ในช่องเขียว",
                _ => "",
            });
            bool grip = ph == GrabPlay.Phase.Grip;
            grabGauge.style.display = grip ? DisplayStyle.Flex : DisplayStyle.None;
            if (grip)
            {
                grabGood.style.left = Length.Percent(HandGrab.GripLow * 100); grabGood.style.width = Length.Percent((HandGrab.GripHigh - HandGrab.GripLow) * 100);
                grabMark.style.left = Length.Percent(g.Grip * 100);
                bool ok = g.Grip >= HandGrab.GripLow && g.Grip <= HandGrab.GripHigh;
                grabMark.style.backgroundColor = ok ? new Color(1f, 0.9f, 0.3f) : g.Grip > HandGrab.GripHigh ? new Color(1f, 0.45f, 0.4f) : Ink;
                grabProg.style.width = Length.Percent(Mathf.Clamp01(g.GripProgress) * 100);
            }
            grabPull.style.display = ph == GrabPlay.Phase.Reach || ph == GrabPlay.Phase.Feel ? DisplayStyle.Flex : DisplayStyle.None;
            string wounds = new string('●', HandGrab.Wound) + new string('○', HandGrab.MaxWound - HandGrab.Wound);
            grabInfo.text = Loc.T($"งมได้อีก {HandGrab.Left} โพรงวันนี้ · แผลที่มือ {wounds}");
        }
    }
}
