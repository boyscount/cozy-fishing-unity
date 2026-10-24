using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // ฉากเปิดด่าน: แถบดำบน-ล่างแบบภาพยนตร์ · การ์ดชื่อด่าน (ค่อย ๆ จางเข้า) · ปุ่ม "แตะเพื่อข้าม"
    public partial class Hud
    {
        VisualElement introEl, introTop, introBot, introCardEl; Label introName, introDesc, introFish, introSkip;
        float introBars;

        public void IntroOpen(string name, string desc, string fish, Action skip)
        {
            if (introEl == null)
            {
                introEl = new VisualElement { pickingMode = PickingMode.Position }; Abs(introEl, 0, 0, 0, 0); root.Add(introEl);
                introTop = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(introTop, 0, 0, 0); introTop.style.backgroundColor = Color.black; introEl.Add(introTop);
                introBot = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(introBot, 0, 0, null, 0); introBot.style.backgroundColor = Color.black; introEl.Add(introBot);
                introCardEl = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(introCardEl, 0, 0, null, 0); introCardEl.style.alignItems = Align.Center; introEl.Add(introCardEl);
                introCardEl.style.paddingBottom = 10;
                VisualElement Ornament()
                {
                    // ── ◆ ── เส้นทองคู่ ปลายเรียว กลางเป็นข้าวหลามตัด (ลายประจำยามอย่างง่าย)
                    var row = new VisualElement { pickingMode = PickingMode.Ignore }; row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 2; row.style.marginBottom = 2;
                    var gold = new Color(0.9f, 0.72f, 0.38f, 0.95f);
                    VisualElement Line() { var l = new VisualElement { pickingMode = PickingMode.Ignore }; l.style.width = 150; l.style.height = 5; l.style.borderTopWidth = l.style.borderBottomWidth = 1.5f; l.style.borderTopColor = l.style.borderBottomColor = gold; row.Add(l); return l; }
                    Line();
                    var dia = new VisualElement { pickingMode = PickingMode.Ignore }; dia.style.width = dia.style.height = 12; dia.style.marginLeft = dia.style.marginRight = 10;
                    dia.style.backgroundColor = gold; dia.style.rotate = new Rotate(45); row.Add(dia);
                    Line();
                    introCardEl.Add(row); return row;
                }
                Ornament();
                introName = L(introCardEl, "", 64, new Color(1f, 0.9f, 0.68f)); introName.style.unityFontDefinition = fontTitle; Shadow(introName);
                Ornament();
                introDesc = L(introCardEl, "", 24, new Color(0.95f, 0.92f, 0.85f), false); introDesc.style.maxWidth = Length.Percent(70); Shadow(introDesc);
                introFish = L(introCardEl, "", 20, new Color(1f, 0.78f, 0.45f)); introFish.style.marginTop = 6; Shadow(introFish);
                introSkip = L(introEl, "แตะเพื่อข้าม ▶", 18, new Color(1, 1, 1, 0.7f), false); Abs(introSkip, null, 24, 18);
            }
            Action fire = skip;
            introEl.RegisterCallback<PointerDownEvent>(e => { fire?.Invoke(); fire = null; });
            introName.text = name; introDesc.text = desc; introFish.text = string.IsNullOrEmpty(fish) ? "" : "ปลาเด่น: " + fish;
            introCardEl.style.opacity = 0; introBars = 0;
            introEl.style.display = DisplayStyle.Flex; introEl.BringToFront();
            introEl.schedule.Execute(StepIntro).Every(16).Until(() => introEl.style.display == DisplayStyle.None);
        }
        bool introCardOn;
        public void IntroCard(bool on) { introCardOn = on; }
        void StepIntro()
        {
            float h = root.layout.height; if (float.IsNaN(h)) return;
            introBars = Mathf.MoveTowards(introBars, 1, 0.03f);
            float bar = h * 0.11f * Mathf.SmoothStep(0, 1, introBars);
            introTop.style.height = bar; introBot.style.height = bar;
            introCardEl.style.bottom = bar + 12;
            float o = introCardEl.resolvedStyle.opacity;
            introCardEl.style.opacity = Mathf.MoveTowards(o, introCardOn ? 1 : 0, 0.025f);
        }
        // ฉากภาพยนตร์: ซ่อน HUD อื่นทั้งหมด (ยกเว้นแผ่นฉากเปิด / จอมืด) แล้วคืนตามเดิม
        public void Cinematic(bool on)
        {
            foreach (var c in root.Children())
                if (c != introEl && c != fadeEl) c.style.visibility = on ? Visibility.Hidden : StyleKeyword.Null;
        }

        public void IntroClose()
        {
            if (introEl == null) return;
            introEl.style.display = DisplayStyle.None; introCardOn = false;
        }
    }
}
