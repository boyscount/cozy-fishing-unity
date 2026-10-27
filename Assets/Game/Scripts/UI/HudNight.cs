using UnityEngine;
using UnityEngine.UIElements;
using static CozyFishing.HudKit;

namespace CozyFishing
{
    // หน้าจอกลางคืน: เกจสติ (หัวใจ ← แถบ → สมอง) ใต้นาฬิกา · ขอบจอแดงตามความกลัว · คำเตือนผีกลางจอ · ปุ่มไฟฉาย (แสดงแบต)
    public partial class Hud
    {
        VisualElement nightBox, sanityFill, vignette, flashBtn, ghostIco; Label nightWarn, flashBadge, amuletTxt;
        bool nightBuilt;

        void BuildNight()
        {
            nightBuilt = true;
            vignette = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(vignette, 0, 0, 0, 0);
            vignette.style.backgroundImage = Resources.Load<Texture2D>("UI/Frames/vignette"); vignette.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            vignette.style.opacity = 0; root.Insert(1, vignette);   // ใต้ปุ่มทั้งหมด
            nightBox = Box(root, Paper, 12); Abs(nightBox, 24, null, 176); Pad(nightBox, 8, 16); Skin(nightBox, "wood", 18);
            nightBox.style.flexDirection = FlexDirection.Row; nightBox.style.alignItems = Align.Center;
            Ico(nightBox, "heart", 30);
            var track = new VisualElement { pickingMode = PickingMode.Ignore }; track.style.width = 150; track.style.height = 16; track.style.marginLeft = 6; track.style.marginRight = 6;
            track.style.backgroundColor = new Color(0.12f, 0.06f, 0.05f, 0.85f); Radius(track, 8); nightBox.Add(track);
            sanityFill = new VisualElement { pickingMode = PickingMode.Ignore }; sanityFill.style.height = Length.Percent(100); Radius(sanityFill, 8); track.Add(sanityFill);
            Ico(nightBox, "brain", 30);
            amuletTxt = L(nightBox, "", 20, new Color(1f, 0.85f, 0.55f)); amuletTxt.style.marginLeft = 8;
            // ผีกำลังเข้าใกล้: ไอคอนผีข้างแถบสติ เต้นแรงขึ้นตามความกลัว (แทนข้อความเตือนยาว ๆ ที่ขึ้นค้างจนรำคาญ)
            ghostIco = Ico(nightBox, "ghost", 34); ghostIco.style.marginLeft = 8; ghostIco.style.display = DisplayStyle.None;
            // คำเตือนผี: ต่อใต้กล่องข้อความแจ้ง (ไม่ลอยทับกัน) · พื้นมืดโปร่ง ๆ ให้อ่านออกทุกฉาก
            nightWarn = L(toastRow, "", 26, new Color(1f, 0.55f, 0.48f)); nightWarn.style.unityFontDefinition = fontTitle; Shadow(nightWarn);
            nightWarn.style.backgroundColor = new Color(0.05f, 0.02f, 0.02f, 0.5f); Pad(nightWarn, 6, 18); nightWarn.style.marginTop = 8;
            nightWarn.style.borderTopLeftRadius = nightWarn.style.borderTopRightRadius = nightWarn.style.borderBottomLeftRadius = nightWarn.style.borderBottomRightRadius = 14;
            nightWarn.style.maxWidth = Length.Percent(88); nightWarn.style.whiteSpace = WhiteSpace.Normal; nightWarn.style.unityTextAlign = TextAnchor.MiddleCenter;
            flashBtn = IconButton("flashlight", new Color(0.9f, 0.85f, 0.5f), () => { if (Story.S.time >= GhostSystem.NightFrom) GhostSystem.FlashOn = !GhostSystem.FlashOn; }, out flashBadge);
            nightBox.style.display = DisplayStyle.None; nightWarn.style.display = DisplayStyle.None; flashBtn.style.display = DisplayStyle.None;
        }

        // ---------- ฉากผีปรากฏ: แฟลชขาว → ภาพย้อนอดีตสีซีเปียสั่นไหว + ชื่อผีและตำนาน (ใช้เวลาจริง เพราะเกมสโลโมชั่นอยู่) ----------
        VisualElement revealEl, revealFlash, revealTint; Label revealName, revealLegend;
        public void GhostReveal(string name, string legend, float len, Color glow)
        {
            if (revealEl == null)
            {
                revealEl = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(revealEl, 0, 0, 0, 0); root.Add(revealEl);
                revealTint = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(revealTint, 0, 0, 0, 0); revealEl.Add(revealTint);
                revealTint.style.backgroundImage = Resources.Load<Texture2D>("UI/Frames/vignette"); revealTint.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                // ชื่อผี + ตำนาน: กลางจอค่อนบน (ไม่ทับจอย/ปุ่มด้านล่าง) · กว้างไม่เกินช่วงระหว่างปุ่ม
                var col = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(col, 0, 0, null, null); col.style.top = Length.Percent(36); col.style.alignItems = Align.Center; revealEl.Add(col);
                revealName = L(col, "", 68, Color.white); revealName.style.unityFontDefinition = fontTitle; Shadow(revealName);
                revealLegend = L(col, "", 26, new Color(0.95f, 0.88f, 0.75f)); revealLegend.style.maxWidth = Length.Percent(66); revealLegend.style.marginTop = 6; Shadow(revealLegend);
                revealLegend.style.unityTextAlign = TextAnchor.MiddleCenter; revealLegend.style.whiteSpace = WhiteSpace.Normal;
                revealLegend.style.backgroundColor = new Color(0, 0, 0, 0.35f); Pad(revealLegend, 6, 16);
                revealFlash = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(revealFlash, 0, 0, 0, 0); revealFlash.style.backgroundColor = Color.white; revealEl.Add(revealFlash);
            }
            revealName.text = name ?? ""; revealLegend.text = ""; revealName.style.fontSize = Narrow ? 40 : 54;   // ชื่อผีอย่างเดียว · ตำนานไปอ่านในสมุดคดี (ระหว่างเล่นข้อความยาวรำคาญ)
            revealLegend.style.display = DisplayStyle.None;
            revealName.style.color = Color.Lerp(glow, new Color(1f, 0.35f, 0.3f), 0.55f);
            toastUntil = 0;   // ซ่อนข้อความแจ้ง (ชื่อด่าน/อากาศ) ไม่ให้ทับชื่อผี
            revealEl.BringToFront(); revealEl.style.display = DisplayStyle.Flex;
            StartCoroutine(RevealRun(len, name != null));
        }
        System.Collections.IEnumerator RevealRun(float len, bool full)
        {
            float t = 0;
            while (t < len)
            {
                t += Time.unscaledDeltaTime;
                revealFlash.style.opacity = Mathf.Clamp01(1 - t / 0.28f) * (full ? 1 : 0.7f);                     // แฟลชขาววาบ
                float hold = Mathf.Clamp01(t / 0.35f) * Mathf.Clamp01((len - t) / 0.5f);
                float flick = 0.82f + 0.18f * Mathf.PerlinNoise(t * 22, 3.3f);                                     // ฟิล์มเก่ากระพริบ
                revealTint.style.unityBackgroundImageTintColor = new Color(0.25f, 0.12f, 0.05f, 1);
                revealTint.style.backgroundColor = new Color(0.42f, 0.28f, 0.12f, 0.28f * hold * flick);             // ซีเปียย้อนอดีต
                revealTint.style.opacity = hold;
                float txt = full ? Mathf.Clamp01((t - 0.3f) / 0.4f) * Mathf.Clamp01((len - t) / 0.45f) : 0;
                revealName.style.opacity = txt * flick; revealLegend.style.opacity = txt * 0.95f;
                revealName.style.scale = new Scale(Vector3.one * (1.12f - 0.12f * Mathf.Clamp01(t / len)));       // ชื่อค่อย ๆ หดเข้า
                revealName.style.translate = new Translate((Mathf.PerlinNoise(t * 30, 1) - 0.5f) * 6, 0);           // สั่นเหมือนภาพหลอน
                yield return null;
            }
            revealEl.style.display = DisplayStyle.None;
        }

        // ---------- ได้ปลา: แฟลชกล้อง + ภาพหยุดในกรอบโพลารอยด์ "ได้ปลา!" (ใช้เวลาจริง ระหว่างเกมหยุดภาพ) ----------
        VisualElement photoEl, photoFlash, photoFrame; Label photoCap, photoStamp;
        public void PhotoFlash(string name, bool isNew)
        {
            if (photoEl == null)
            {
                photoEl = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(photoEl, 0, 0, 0, 0); root.Add(photoEl);
                photoEl.style.justifyContent = Justify.Center; photoEl.style.alignItems = Align.Center;
                photoFrame = new VisualElement { pickingMode = PickingMode.Ignore }; photoEl.Add(photoFrame);
                photoFrame.style.width = Length.Percent(74); photoFrame.style.height = Length.Percent(78);
                Border(photoFrame, 22, new Color(0.97f, 0.95f, 0.9f)); photoFrame.style.borderBottomWidth = 96;
                photoFrame.style.rotate = new Rotate(-2.5f);
                photoCap = L(photoEl, "", 46, new Color(0.22f, 0.13f, 0.05f)); photoCap.style.unityFontDefinition = fontTitle;
                photoCap.style.position = Position.Absolute; photoCap.style.bottom = Length.Percent(12.5f); photoCap.style.left = 0; photoCap.style.right = 0;
                photoStamp = L(photoEl, "ปลาใหม่!", 40, new Color(0.8f, 0.12f, 0.08f)); photoStamp.style.unityFontDefinition = fontTitle;
                photoStamp.style.position = Position.Absolute; photoStamp.style.right = Length.Percent(17); photoStamp.style.top = Length.Percent(16);
                photoStamp.style.rotate = new Rotate(14); Border(photoStamp, 4, new Color(0.8f, 0.12f, 0.08f)); Pad(photoStamp, 2, 14); Radius(photoStamp, 8);
                photoFlash = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(photoFlash, 0, 0, 0, 0); photoFlash.style.backgroundColor = Color.white; photoEl.Add(photoFlash);
            }
            photoCap.text = $"ได้{name}!"; photoStamp.style.display = isNew ? DisplayStyle.Flex : DisplayStyle.None;
            photoEl.BringToFront(); photoEl.style.display = DisplayStyle.Flex;
            StartCoroutine(PhotoRun());
        }
        System.Collections.IEnumerator PhotoRun()
        {
            for (float t = 0; t < 1.25f; t += Time.unscaledDeltaTime)
            {
                photoFlash.style.opacity = Mathf.Clamp01(1 - t / 0.22f);
                float f = Mathf.Clamp01(t / 0.12f) * Mathf.Clamp01((1.25f - t) / 0.35f);
                photoFrame.style.opacity = f; photoCap.style.opacity = f; photoStamp.style.opacity = f;
                photoFrame.style.scale = new Scale(Vector3.one * (1.06f - 0.06f * Mathf.Clamp01(t / 0.3f)));
                yield return null;
            }
            photoEl.style.display = DisplayStyle.None;
        }

        /// GhostSystem เรียกทุกเฟรม
        public void Night(bool night, float sanity, float fear, string warn, bool flashOn, float battery)
        {
            if (!nightBuilt) BuildNight();
            bool show = night && !CardOpen && !ModalOpen;
            nightBox.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            flashBtn.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            float s = Mathf.Clamp01(sanity / 100f);
            sanityFill.style.width = Length.Percent(s * 100);
            sanityFill.style.backgroundColor = Color.Lerp(new Color(0.85f, 0.15f, 0.12f), new Color(0.55f, 0.8f, 0.45f), s);
            amuletTxt.text = Story.S.amulets > 0 ? $"สายสิญจน์ ×{Story.S.amulets}" : "";
            // ขอบจอแดง: ตามความกลัว + เต้นตามจังหวะหัวใจเมื่อสติต่ำ
            float pulse = s < 0.4f ? (0.5f + 0.5f * Mathf.Sin(Time.time * (6 + (1 - s) * 6))) * (0.4f - s) : 0;
            vignette.style.opacity = night ? Mathf.Clamp01(fear * 0.75f + pulse * 1.4f + (1 - s) * 0.25f) : 0;
            nightWarn.style.display = DisplayStyle.None;
            bool near = show && !string.IsNullOrEmpty(warn);
            ghostIco.style.display = near ? DisplayStyle.Flex : DisplayStyle.None;
            if (near)
            {
                float beat = Mathf.Abs(Mathf.Sin(Time.time * (3f + fear * 6f)));
                ghostIco.style.scale = new Scale(Vector3.one * (1f + beat * (0.15f + fear * 0.25f)));
                ghostIco.style.unityBackgroundImageTintColor = Color.Lerp(new Color(1f, 0.85f, 0.85f), new Color(1f, 0.25f, 0.2f), Mathf.Clamp01(fear * 1.4f) * (0.5f + 0.5f * beat));
            }
            flashBadge.text = $"{Mathf.RoundToInt(battery)}%";
            flashBtn.ElementAt(0).style.unityBackgroundImageTintColor = flashOn ? new Color(1.3f, 1.25f, 0.8f) : Color.white;
        }
    }
}
