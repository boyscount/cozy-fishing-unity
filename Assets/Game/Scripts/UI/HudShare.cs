using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // แชร์ปลาใหญ่: ปุ่ม "แชร์" บนการ์ดปลา → ซ่อนปุ่ม/ตัวเลขทั้งหมด เหลือภาพปลาที่ยกโชว์ + กรอบไม้ไผ่ ชื่อปลา ดาว น้ำหนัก ราคา ด่าน ชื่อเกม ลิงก์
    //   ถ่ายภาพหน้าจอ → มือถือเปิดหน้าต่างแชร์ของเครื่อง · คอมดาวน์โหลดเป็นภาพ (CozyShare.jslib)
    public partial class Hud
    {
        const string GameUrlPublic = "https://boyscount.github.io/cozy-fishing-unity/";
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CozyShareImage(byte[] png, int len, string title, string text, string url);
#else
        static void CozyShareImage(byte[] png, int len, string title, string text, string url) { System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, "share.png"), png); Debug.Log("[Share] saved share.png"); }
#endif
        VisualElement shareFrame, shareBtn; Label shareName, shareStats, shareStage; VisualElement[] shareStars; bool sharing;

        void AddShareButton(VisualElement btns)
        {
            var b = new Button(() => { if (!sharing) StartCoroutine(ShareCatch()); }) { text = "แชร์" };
            StyleButton(b, new Color(0.25f, 0.45f, 0.62f)); b.style.fontSize = 28; b.style.marginLeft = 14; Pad(b, 14, 24);
            b.style.minWidth = 170; b.style.flexShrink = 0; b.style.whiteSpace = WhiteSpace.NoWrap;   // ไม่ให้ถูกบีบจนไอคอนทับคำ · ที่ไม่พอขึ้นแถวใหม่แทน
            Skin(b, "plank", 22); b.style.unityBackgroundImageTintColor = new Color(0.7f, 0.86f, 1f); b.style.color = new Color(0.06f, 0.14f, 0.24f);
            b.style.paddingLeft = 64; var ic = Ico(b, "camera_share", 38); Abs(ic, 16, null, 11);
            btns.Add(b); shareBtn = b;
        }

        void BuildShareFrame()
        {
            shareFrame = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(shareFrame, 0, 0, 0, 0); root.Add(shareFrame);
            shareFrame.style.display = DisplayStyle.None;
            // กรอบไม้ไผ่รอบภาพ
            var border = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(border, 14, 14, 14, 14); shareFrame.Add(border);
            border.style.borderTopWidth = border.style.borderBottomWidth = border.style.borderLeftWidth = border.style.borderRightWidth = 10;
            border.style.borderTopColor = border.style.borderBottomColor = border.style.borderLeftColor = border.style.borderRightColor = new Color(0.62f, 0.45f, 0.2f);
            Radius(border, 18);
            // ป้ายบน: ชื่อปลา + ดาว
            var top = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(top, 0, 0, 34, null); top.style.alignItems = Align.Center; shareFrame.Add(top);
            var plate = new VisualElement { pickingMode = PickingMode.Ignore }; Skin(plate, "parchment", 20); Pad(plate, 10, 34); plate.style.alignItems = Align.Center; top.Add(plate);
            var srow = new VisualElement { pickingMode = PickingMode.Ignore }; srow.style.flexDirection = FlexDirection.Row; plate.Add(srow);
            shareStars = new VisualElement[4]; for (int i = 0; i < 4; i++) { shareStars[i] = Ico(srow, "star_off", 30); shareStars[i].style.marginLeft = 2; }
            shareName = L(plate, "", 58, new Color(0.24f, 0.12f, 0.04f)); shareName.style.unityFontDefinition = fontTitle;
            // ป้ายล่าง: น้ำหนัก ราคา ด่าน + ชื่อเกม ลิงก์
            var bot = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(bot, 0, 0, null, 34); bot.style.alignItems = Align.Center; shareFrame.Add(bot);
            var bp = new VisualElement { pickingMode = PickingMode.Ignore }; bp.style.backgroundColor = new Color(0.08f, 0.05f, 0.02f, 0.78f); Radius(bp, 16); Pad(bp, 12, 30); bp.style.alignItems = Align.Center; bot.Add(bp);
            shareStats = L(bp, "", 34, new Color(1f, 0.86f, 0.5f)); shareStats.style.unityFontDefinition = fontTitle;
            shareStage = L(bp, "", 20, new Color(0.95f, 0.9f, 0.8f));
            var brand = new VisualElement { pickingMode = PickingMode.Ignore }; brand.style.flexDirection = FlexDirection.Row; brand.style.alignItems = Align.Center; brand.style.marginTop = 6; bp.Add(brand);
            Ico(brand, "basket", 28).style.marginRight = 8;
            L(brand, "ตะกร้าแห่งวันพรุ่งนี้ · boyscount.github.io/cozy-fishing-unity", 18, new Color(0.9f, 0.82f, 0.62f));
        }

        IEnumerator ShareCatch()
        {
            sharing = true;
            if (shareFrame == null) BuildShareFrame();
            string fish = cardName.text, kg = cardKg.text, price = cardPrice.text;
            int stars = cardStarCount;
            shareName.text = fish;
            for (int i = 0; i < 4; i++) SetIco(shareStars[i], i < stars ? "star_on" : "star_off");
            shareStats.text = $"{kg} · {price}";
            var st = Stages.Current; shareStage.text = $"ได้ที่ {st.name}";
            // ซ่อน UI ทุกอย่าง เหลือแค่กรอบแชร์ แล้วถ่ายภาพ · ใช้ visibility ไม่ใช่ display เพราะจอยกับปุ่มตีเบ็ดถูกตั้ง display ใหม่ทุกเฟรม
            var hidden = new System.Collections.Generic.List<(VisualElement e, StyleEnum<Visibility> v)>();
            foreach (var c in root.Children()) { if (c == shareFrame) continue; hidden.Add((c, c.style.visibility)); c.style.visibility = Visibility.Hidden; }
            var fc = Cosmetics.Equipped(Cosmetics.Card).color;   // กรอบการ์ดตามของแต่งที่เลือก
            var bd = shareFrame.ElementAt(0); bd.style.borderTopColor = bd.style.borderBottomColor = bd.style.borderLeftColor = bd.style.borderRightColor = fc;
            shareFrame.style.display = DisplayStyle.Flex; shareFrame.BringToFront();
            yield return null; yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            foreach (var (e, v) in hidden) e.style.visibility = v;
            shareFrame.style.display = DisplayStyle.None;
            var png = tex.EncodeToPNG(); Destroy(tex);
            CozyShareImage(png, png.Length, $"ได้{fish} {kg}!", $"ได้{fish} หนัก {kg} ที่{st.name} 🎣 มาตกปลาด้วยกัน ใน ตะกร้าแห่งวันพรุ่งนี้", GameUrlPublic);
            Sfx.Coin();
            sharing = false;
        }
    }
}
