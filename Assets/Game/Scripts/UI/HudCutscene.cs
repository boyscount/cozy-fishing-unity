using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // คัตซีน (Timeline): แถบดำบน-ล่าง · บทพูดใต้จอ (ชื่อคนพูด + ประโยค) · ชื่อเรื่องกลางจอ · "แตะเพื่อข้าม"
    public partial class Hud
    {
        VisualElement cutEl, cutTop, cutBot, cutSubBox; Label cutWho, cutText, cutTitle, cutSkip;
        float cutBarsOn;

        void EnsureCutscene()
        {
            if (cutEl != null) return;
            cutEl = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(cutEl, 0, 0, 0, 0); root.Add(cutEl);
            cutTop = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(cutTop, 0, 0, 0); cutTop.style.backgroundColor = Color.black; cutEl.Add(cutTop);
            cutBot = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(cutBot, 0, 0, null, 0); cutBot.style.backgroundColor = Color.black; cutEl.Add(cutBot);
            cutSubBox = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(cutSubBox, 0, 0, null, 0); cutSubBox.style.alignItems = Align.Center; cutEl.Add(cutSubBox);
            cutWho = L(cutSubBox, "", 24, new Color(1f, 0.8f, 0.45f)); Shadow(cutWho);
            cutText = L(cutSubBox, "", 30, Color.white); Shadow(cutText);
            cutText.style.maxWidth = Length.Percent(86); cutText.style.unityTextAlign = TextAnchor.MiddleCenter; cutText.style.whiteSpace = WhiteSpace.Normal;
            cutTitle = L(cutEl, "", 64, new Color(1f, 0.88f, 0.6f)); Abs(cutTitle, 0, 0, null, null); cutTitle.style.top = Length.Percent(38);
            cutTitle.style.unityTextAlign = TextAnchor.MiddleCenter; cutTitle.style.unityFontDefinition = fontTitle; cutTitle.style.whiteSpace = WhiteSpace.Normal; Shadow(cutTitle);
            cutSkip = L(cutEl, "แตะเพื่อข้าม ▸", 18, new Color(1, 1, 1, 0.6f)); Abs(cutSkip, null, 18, 12); cutSkip.style.display = DisplayStyle.None;
            cutEl.style.display = DisplayStyle.None;
        }

        /// เปิด/ปิดโหมดคัตซีน: ซ่อน HUD อื่น + แถบดำ (bars) · skip = แสดงป้าย "แตะเพื่อข้าม"
        public void CutsceneMode(bool on, bool bars, bool skip)
        {
            EnsureCutscene();
            if (bars) Cinematic(on);
            cutEl.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            cutEl.style.visibility = Visibility.Visible; cutEl.BringToFront();
            cutBarsOn = on && bars ? 1 : 0;
            float h = cutBarsOn * 64;
            cutTop.style.height = h; cutBot.style.height = h;
            cutSubBox.style.bottom = h + 14;
            cutSkip.text = Loc.T("แตะเพื่อข้าม ▸"); cutSkip.style.display = skip ? DisplayStyle.Flex : DisplayStyle.None;
            if (!on) CutsceneCaption(null, null, 0, false);
        }

        /// บทพูด (title = false) หรือชื่อเรื่องกลางจอ (title = true) · alpha = ความชัด (Timeline เฟดให้)
        public void CutsceneCaption(string who, string text, float alpha, bool title)
        {
            if (cutEl == null) { if (alpha <= 0) return; EnsureCutscene(); cutEl.style.display = DisplayStyle.Flex; cutEl.BringToFront(); }
            bool show = alpha > 0.001f && !string.IsNullOrEmpty(text);
            cutTitle.text = show && title ? text : ""; cutTitle.style.opacity = show && title ? alpha : 0;
            cutWho.text = show && !title ? who ?? "" : ""; cutText.text = show && !title ? text : "";
            cutWho.style.display = string.IsNullOrEmpty(cutWho.text) ? DisplayStyle.None : DisplayStyle.Flex;
            cutSubBox.style.opacity = show && !title ? alpha : 0;
        }
    }
}
