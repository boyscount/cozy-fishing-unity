using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // ป้ายไม้หน้าแรก: เศษไม้เฉือนขอบดิบ เชือกพัน หมุดสนิม + ของประกอบเล่าเรื่อง (แผนที่ · กุญแจ · เหยื่อ · ภาพถ่ายปลา)
    // ชี้/แตะ: แสงตะเกียงวิบวับบนเนื้อไม้ · ฝุ่นละอองลอยจากหลังป้าย · เสียงไม้กระทบ
    public partial class Hud
    {
        class SignProp { public VisualElement e; public float x, y, size; public bool fromRight; }
        class Sign { public Button b; public VisualElement plank, glow; public Label main, sub; public VisualElement icon; public List<SignProp> props = new List<SignProp>(); public bool small, hot; public float t; }
        readonly List<Sign> signs = new List<Sign>();
        AudioSource uiAudio; AudioClip knockClip, clackClip;
        FontDefinition fontSign;

        internal static Texture2D TitleTex(string n) => Resources.Load<Texture2D>("UI/Title/" + n);

        Sign MakeSign(string main, string sub, string plankTex, System.Action click, bool small = false)
        {
            if (fontSign.font == null) { var charm = Resources.Load<Font>("Fonts/Charm-Bold"); fontSign = charm != null ? FontDefinition.FromFont(charm) : fontTitle; }
            var s = new Sign { small = small };
            var b = s.b = new Button(() => { PlayUi(clackClip, 0.7f); click(); }) { text = "" };
            StyleButton(b, Color.clear); b.style.backgroundImage = StyleKeyword.None;
            b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 0;
            b.style.overflow = Overflow.Visible; b.style.flexShrink = 0;
            b.style.paddingTop = b.style.paddingBottom = b.style.paddingLeft = b.style.paddingRight = 0;
            b.style.justifyContent = Justify.Center; b.style.alignItems = Align.Center;
            // ภาพป้ายมีขอบว่างรอบ (เงา/เชือกล้น) → ขยายออกนอกกรอบปุ่มเท่าขอบนั้น
            var tex = TitleTex(plankTex);
            var pl = s.plank = new VisualElement { pickingMode = PickingMode.Ignore }; pl.style.position = Position.Absolute;
            if (tex != null)
            {
                float ex = 40f / (tex.width - 80), ey = 40f / (tex.height - 80);
                pl.style.left = pl.style.right = Length.Percent(-ex * 100); pl.style.top = pl.style.bottom = Length.Percent(-ey * 100);
                pl.style.backgroundImage = tex; pl.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
            }
            b.Add(pl);
            var gl = s.glow = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(gl, 0, 0, 0, 0);
            gl.style.backgroundImage = TitleTex("glow"); gl.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
            gl.style.opacity = 0; b.Add(gl);
            // ตัวหนังสือสลักไม้: สีน้ำตาลเข้ม + ขอบสว่างด้านล่าง (เหมือนร่องแกะ)
            s.main = new Label(main) { pickingMode = PickingMode.Ignore }; s.main.style.unityFontDefinition = fontSign;
            s.main.style.color = new Color(0.2f, 0.1f, 0.03f); s.main.style.unityTextAlign = TextAnchor.MiddleCenter;
            s.main.style.textShadow = new TextShadow { offset = new Vector2(0, 1.5f), blurRadius = 0.5f, color = new Color(1f, 0.86f, 0.62f, 0.55f) };
            s.main.style.marginTop = s.main.style.marginBottom = 0; s.main.style.paddingTop = s.main.style.paddingBottom = 0;
            b.Add(s.main);
            if (sub != null)
            {
                s.sub = new Label(sub) { pickingMode = PickingMode.Ignore }; s.sub.style.unityFontDefinition = fontBold;
                s.sub.style.color = new Color(0.17f, 0.08f, 0.02f); s.sub.style.unityTextAlign = TextAnchor.MiddleCenter;
                s.sub.style.marginTop = -2; b.Add(s.sub);
            }
            b.RegisterCallback<PointerEnterEvent>(e => { if (!s.hot) { s.hot = true; PlayUi(knockClip, 0.45f); Dust(s); } });
            b.RegisterCallback<PointerLeaveEvent>(e => s.hot = false);
            signs.Add(s);
            return s;
        }

        SignProp Prop(Sign s, string tex, float x, float y, float size, bool fromRight = false, float opacity = 1)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore }; e.style.position = Position.Absolute;
            e.style.backgroundImage = TitleTex(tex); e.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain); e.style.opacity = opacity;
            s.b.Insert(tex == "stamp_red" ? 2 : s.b.childCount, e);   // ตราแดงประทับบนเนื้อไม้ (ใต้ตัวหนังสือ) · ของอื่นวางทับ
            var p = new SignProp { e = e, x = x, y = y, size = size, fromRight = fromRight }; s.props.Add(p); return p;
        }

        // วางขนาดป้าย/ของประกอบตามความสูงป้าย H (ของประกอบใช้หน่วย H) · edgeK < 1 = หดของที่ล้นออกซ้าย/ขวา (จอตั้ง)
        void SizeSign(Sign s, float w, float h, float edgeK)
        {
            s.b.style.width = w; s.b.style.height = h; s.b.style.minHeight = h;
            float fm = s.small ? h * 0.52f : h * (s.sub != null ? (edgeK < 1 ? 0.3f : 0.38f) : 0.44f);
            if (Loc.English) fm *= 0.92f;
            // อังกฤษ: ตัวเขียนลายมือ (Charm) · ไทย: ตัวหนาอ่านง่ายแบบป้ายไม้ (Mali Bold) — Charm ไทยเส้นบางเกินที่ขนาดป้าย
            s.main.style.unityFontDefinition = Loc.English ? fontSign : fontBold;
            s.main.style.fontSize = fm; s.main.style.marginTop = s.sub != null ? -h * 0.06f : 0;
            if (s.sub != null) s.sub.style.fontSize = Mathf.Max(11, h * (edgeK < 1 ? 0.15f : 0.18f));
            float padL = s.icon != null ? h * 0.75f : h * 0.42f;
            s.b.style.paddingLeft = padL; s.b.style.paddingRight = h * 0.42f;
            if (s.icon != null) { float isz = h * 0.82f; s.icon.style.width = s.icon.style.height = isz; s.icon.style.left = h * 0.12f; s.icon.style.top = (h - isz) / 2; }
            foreach (var p in s.props)
            {
                float sz = p.size * h, x = p.x * h, y = p.y * h;
                if (edgeK < 1 && p.x < 0)   // จอแคบ: ของประกอบเล็กลง ชิดมุมป้าย ไม่ทับตัวหนังสือ/ไม่ล้นจอ
                {
                    sz *= 0.62f; y = p.y * h * 0.62f - h * 0.12f;
                    x = p.fromRight ? -sz * 0.62f : -sz * 0.42f;
                }
                p.e.style.width = p.e.style.height = sz; p.e.style.left = p.fromRight ? w + x : x; p.e.style.top = y;
            }
        }

        // ---------- ลูกเล่นตอนชี้: แสงตะเกียงวิบวับ + เอียง/ขยายนิด ๆ ----------
        void TickSigns()
        {
            float dt = 0.033f;
            foreach (var s in signs)
            {
                s.t = Mathf.MoveTowards(s.t, s.hot ? 1 : 0, dt * 6);
                float fl = Mathf.PerlinNoise(Time.unscaledTime * 7f, s.GetHashCode() * 0.001f);
                s.glow.style.opacity = s.t * (0.28f + 0.38f * fl);
                float k = 1 + 0.035f * s.t; s.b.style.scale = new Scale(new Vector3(k, k, 1));
                s.b.style.rotate = new Rotate(new Angle(-0.6f * s.t));
                s.plank.style.unityBackgroundImageTintColor = Color.Lerp(Color.white, new Color(1.0f, 0.93f, 0.8f), s.t * fl);
            }
        }

        // ฝุ่นละอองลอยขึ้นจากหลังป้าย
        void Dust(Sign s)
        {
            var host = s.b.parent; if (host == null) return;
            var r = s.b.layout; var tex = TitleTex("mote");
            for (int i = 0; i < 9; i++)
            {
                var m = new VisualElement { pickingMode = PickingMode.Ignore }; m.style.position = Position.Absolute;
                float sz = Random.Range(3f, 8f); m.style.width = m.style.height = sz; m.style.backgroundImage = tex;
                float x = r.x + Random.Range(0.05f, 0.95f) * r.width, y = r.y + Random.Range(0.1f, 0.9f) * r.height;
                float vx = Random.Range(-14f, 14f), vy = Random.Range(-40f, -16f), life = Random.Range(0.6f, 1.2f), age = 0;
                m.style.left = x; m.style.top = y; host.Insert(0, m);   // อยู่หลังป้าย
                IVisualElementScheduledItem it = null;
                it = m.schedule.Execute(() =>
                {
                    age += 0.033f; x += vx * 0.033f; y += vy * 0.033f; vy += 6 * 0.033f;
                    m.style.left = x; m.style.top = y; m.style.opacity = Mathf.Sin(Mathf.Clamp01(age / life) * Mathf.PI) * 0.85f;
                    if (age >= life) { it.Pause(); m.RemoveFromHierarchy(); }
                }).Every(33);
            }
        }

        // ---------- เสียง: ไม้กระทบกัน (สังเคราะห์) ----------
        void PlayUi(AudioClip c, float vol)
        {
            if (c == null) return;
            if (uiAudio == null) { uiAudio = gameObject.AddComponent<AudioSource>(); uiAudio.playOnAwake = false; uiAudio.spatialBlend = 0; }
            uiAudio.pitch = Random.Range(0.93f, 1.07f); uiAudio.PlayOneShot(c, vol);
        }
        static AudioClip WoodClip(string name, float baseHz, int hits, float gap)
        {
            const int sr = 44100; int n = (int)(sr * (0.22f + gap * hits)); var d = new float[n];
            var rng = new System.Random(name.GetHashCode());
            float[] f = { baseHz, baseHz * 2.32f, baseHz * 4.1f, baseHz * 6.7f }; float[] dec = { 28, 42, 70, 110 }; float[] amp = { 1, 0.55f, 0.3f, 0.15f };
            for (int hIdx = 0; hIdx < hits; hIdx++)
            {
                int o = (int)(hIdx * gap * sr); float g = hIdx == 0 ? 1 : 0.6f;
                for (int i = 0; o + i < n; i++)
                {
                    float t = i / (float)sr, v = 0;
                    for (int k = 0; k < 4; k++) v += amp[k] * Mathf.Sin(2 * Mathf.PI * f[k] * t) * Mathf.Exp(-dec[k] * t);
                    v += ((float)rng.NextDouble() * 2 - 1) * Mathf.Exp(-t * 400) * 0.6f;   // เสียงคลิกตอนกระทบ
                    d[o + i] += v * g * 0.32f;
                }
            }
            var c = AudioClip.Create(name, n, 1, sr, false); c.SetData(d, 0); return c;
        }
        void InitSignAudio() { knockClip = WoodClip("WoodKnock", 520, 1, 0); clackClip = WoodClip("WoodClack", 330, 2, 0.07f); }
    }
}
