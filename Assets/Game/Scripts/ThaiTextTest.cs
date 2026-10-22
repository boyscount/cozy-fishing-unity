using UnityEngine;
using UnityEngine.UIElements;

// ทดสอบการวางสระ/วรรณยุกต์ภาษาไทยด้วย UI Toolkit + Advanced Text Generator (ICU/HarfBuzz)
public class ThaiTextTest : MonoBehaviour
{
    void Start()
    {
        var panel = ScriptableObject.CreateInstance<PanelSettings>();
        panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panel.referenceResolution = new Vector2Int(1080, 1920);
        panel.match = 0.5f;
        var doc = gameObject.AddComponent<UIDocument>();
        doc.panelSettings = panel;

        var font = Resources.Load<Font>("ThaiFont");
        var box = new VisualElement();
        box.style.position = Position.Absolute;
        box.style.left = 40; box.style.right = 40; box.style.top = 60;
        box.style.backgroundColor = new Color(1f, 0.97f, 0.92f, 0.94f);
        box.style.borderTopLeftRadius = box.style.borderTopRightRadius = box.style.borderBottomLeftRadius = box.style.borderBottomRightRadius = 36;
        box.style.paddingTop = box.style.paddingBottom = 24; box.style.paddingLeft = box.style.paddingRight = 32;
        foreach (var (text, size) in new[] { ("ตกปลาสบายใจ 🎣", 72), ("ปลาบู่ทอง · ปลาช่อน · ปลาตะเพียน · น้ำตก", 40), ("ที่นี่ปิ้งปลาให้ครอบครัว ซื้อสายไหม ขึ้นชิงช้าสวรรค์", 34) })
        {
            var l = new Label(text);
            l.style.fontSize = size;
            l.style.color = new Color(0.23f, 0.18f, 0.14f);
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            l.style.unityTextGenerator = TextGeneratorType.Advanced;
            if (font != null) l.style.unityFontDefinition = FontDefinition.FromFont(font);
            box.Add(l);
        }
        doc.rootVisualElement.Add(box);
    }
}
