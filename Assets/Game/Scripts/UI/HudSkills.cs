using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // หน้าต้นไม้ทักษะ (ปุ่ม "ทักษะ" หน้าเช้า): ระดับชาวประมง + แถบค่าประสบการณ์ · 4 สาย สายละ 3 ขั้น เรียนทีละขั้นด้วยแต้มทักษะ
    public partial class Hud
    {
        VisualElement skillsEl, skillsBody; Label skillsHead, skillsSub; VisualElement skillsXp;

        public void ShowSkills()
        {
            if (skillsEl == null)
            {
                skillsEl = new VisualElement { pickingMode = PickingMode.Position }; Abs(skillsEl, 0, 0, 0, 0); root.Add(skillsEl);
                skillsEl.style.backgroundColor = new Color(0.03f, 0.02f, 0.01f, 0.85f); skillsEl.style.alignItems = Align.Center; skillsEl.style.justifyContent = Justify.Center;
                var panel = new VisualElement { pickingMode = PickingMode.Position }; panel.name = "panel"; GoldFrame(panel, 3); panel.style.backgroundColor = new Color(0.12f, 0.08f, 0.04f, 0.97f);
                panel.style.maxWidth = 1000; panel.style.maxHeight = Length.Percent(94); Pad(panel, 12, 16); skillsEl.Add(panel);
                var head = new VisualElement(); head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center; head.style.justifyContent = Justify.SpaceBetween; panel.Add(head);
                skillsHead = L(head, "", 28, ShopGold); skillsHead.style.unityFontDefinition = fontTitle; skillsHead.style.unityTextAlign = TextAnchor.MiddleLeft;
                var close = new Button(() => { skillsEl.style.display = DisplayStyle.None; GameInput.Blocked = ModalOpen || CardOpen; RefreshMorning(); }) { text = "ปิด" };
                StyleButton(close, Color.clear); GoldFrame(close, 2); close.style.fontSize = 20; close.style.color = new Color(1f, 0.9f, 0.75f); Pad(close, 4, 18);
                Hoverable(close, new Color(0.3f, 0.22f, 0.08f, 0.9f)); head.Add(close);
                skillsSub = L(panel, "", 17, new Color(0.92f, 0.85f, 0.7f), false); skillsSub.style.unityTextAlign = TextAnchor.MiddleLeft; skillsSub.style.marginTop = 4;
                var xpBar = new VisualElement(); xpBar.style.height = 12; xpBar.style.marginTop = 6; xpBar.style.backgroundColor = new Color(0, 0, 0, 0.45f); Radius(xpBar, 6); panel.Add(xpBar);
                skillsXp = new VisualElement(); skillsXp.style.height = Length.Percent(100); skillsXp.style.backgroundColor = new Color(0.95f, 0.7f, 0.28f); Radius(skillsXp, 6); xpBar.Add(skillsXp);
                var sv = new ScrollView(ScrollViewMode.Vertical); sv.style.flexGrow = 1; sv.style.flexShrink = 1; sv.style.marginTop = 10; sv.verticalScrollerVisibility = ScrollerVisibility.Hidden; sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden; panel.Add(sv);
                skillsBody = new VisualElement(); skillsBody.style.flexDirection = FlexDirection.Row; skillsBody.style.flexWrap = Wrap.Wrap; sv.Add(skillsBody);
            }
            skillsEl.Q("panel").style.width = Length.Percent(Narrow ? 96 : 80);
            skillsEl.style.display = DisplayStyle.Flex; skillsEl.BringToFront(); GameInput.Blocked = true;
            FillSkills();
        }

        void FillSkills()
        {
            var s = Story.S;
            skillsHead.text = $"ทักษะชาวประมง · ระดับ {Skills.FisherLevel}";
            skillsSub.text = (s.skillPts > 0 ? $"มีแต้มทักษะ {s.skillPts} แต้ม แตะขั้นที่สว่างเพื่อเรียน" : "ยังไม่มีแต้มทักษะ ตกปลาต่อเพื่อเลื่อนระดับ") + $" · ค่าประสบการณ์ {s.xp}/{Skills.Need(s.fLevel)}";
            skillsXp.style.width = Length.Percent(Mathf.Clamp01(s.xp / (float)Skills.Need(s.fLevel)) * 100);
            skillsBody.Clear();
            foreach (var br in Skills.Branches)
            {
                int lv = Skills.Level(br.id);
                var col = new VisualElement(); col.style.flexGrow = 1; col.style.flexBasis = Narrow ? Length.Percent(46) : Length.Percent(23); col.style.minWidth = 0;
                col.style.marginRight = 6; col.style.marginBottom = 10; Pad(col, 8, 8); Radius(col, 10); col.style.backgroundColor = new Color(0, 0, 0, 0.3f); skillsBody.Add(col);
                var top = new VisualElement(); top.style.flexDirection = FlexDirection.Row; top.style.alignItems = Align.Center; col.Add(top);
                var ic = Ico(top, br.icon, 30); ic.style.marginRight = 6; ic.style.flexShrink = 0;
                var t = L(top, $"{br.name} {lv}/3", 21, new Color(1f, 0.93f, 0.8f)); t.style.unityFontDefinition = fontTitle; t.style.unityTextAlign = TextAnchor.MiddleLeft;
                for (int i = 0; i < br.nodes.Length; i++)
                {
                    var n = br.nodes[i]; bool have = i < lv, next = i == lv, can = next && s.skillPts > 0;
                    string id = br.id;
                    var b = new Button(() => { if (Skills.Learn(id)) { Sfx.Coin(); Toast("เรียนทักษะใหม่แล้ว!"); FillSkills(); } }) { text = "" };
                    StyleButton(b, have ? new Color(0.2f, 0.36f, 0.16f, 0.95f) : can ? new Color(0.45f, 0.32f, 0.1f, 0.95f) : new Color(0.16f, 0.13f, 0.1f, 0.95f));
                    b.style.flexDirection = FlexDirection.Column; b.style.alignItems = Align.FlexStart; b.style.marginTop = 6; Pad(b, 8, 10); Radius(b, 8);
                    if (can) GoldFrame(b, 2);
                    b.SetEnabled(can);
                    var nm = L(b, (have ? "✓ " : "") + n.name, 18, have ? new Color(0.75f, 1f, 0.7f) : can ? new Color(1f, 0.9f, 0.6f) : new Color(0.75f, 0.7f, 0.62f));
                    nm.style.unityTextAlign = TextAnchor.MiddleLeft;
                    var ds = L(b, n.desc + (can ? " · แตะเพื่อเรียน (1 แต้ม)" : ""), 15, new Color(0.88f, 0.82f, 0.7f), false); ds.style.unityTextAlign = TextAnchor.MiddleLeft; ds.style.whiteSpace = WhiteSpace.Normal;
                    b.style.opacity = have || can ? 1f : 0.7f;
                    col.Add(b);
                }
            }
        }
    }
}
