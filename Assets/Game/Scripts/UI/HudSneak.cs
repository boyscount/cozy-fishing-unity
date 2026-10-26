using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CozyFishing
{
    // แอบออกไปตกปลาตอนดึก: กระดานไม้ปักหมุด · คานหัวมีจันทร์เสี้ยว · แต่ละด่านเป็นแผ่นป้ายกระดาษ มีไอคอนผีตามความดุ + ตราประทับ ดุน้อย/ดุ/ดุมาก
    public partial class Hud
    {
        VisualElement sneak, sneakList;
        Action<string> onSneak;

        void BuildSneak()
        {
            sneak = Overlay(out var box, 820);
            box.style.borderTopWidth = box.style.borderBottomWidth = box.style.borderLeftWidth = box.style.borderRightWidth = 0;
            box.style.borderTopLeftRadius = box.style.borderTopRightRadius = box.style.borderBottomLeftRadius = box.style.borderBottomRightRadius = 0;
            box.style.backgroundColor = Color.clear; box.style.backgroundImage = TitleTex("board");
            box.style.unitySliceLeft = box.style.unitySliceRight = box.style.unitySliceTop = box.style.unitySliceBottom = 96;
            box.style.paddingTop = 50; box.style.paddingBottom = 44; box.style.paddingLeft = 46; box.style.paddingRight = 70; box.style.overflow = Overflow.Visible;
            // คานไม้หัวกระดาน: จันทร์เสี้ยว + หัวข้อ
            var beam = new VisualElement { pickingMode = PickingMode.Ignore }; beam.style.flexDirection = FlexDirection.Row; beam.style.alignItems = Align.Center; beam.style.alignSelf = Align.Center;
            beam.style.backgroundImage = TitleTex("beam"); beam.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
            beam.style.paddingTop = 26; beam.style.paddingBottom = 30; beam.style.paddingLeft = 60; beam.style.paddingRight = 60; box.Add(beam);
            var moon = Ico(beam, "moon", 44); moon.style.unityBackgroundImageTintColor = new Color(0.95f, 0.8f, 0.4f); moon.style.marginRight = 12;
            var t = L(beam, "แอบไปตกปลาที่ไหนดี? (ตกได้ถึงตี 5 ครึ่ง)", 34, new Color(0.2f, 0.1f, 0.03f)); t.style.unityFontDefinition = fontTitle; t.style.flexShrink = 1;
            t.style.textShadow = new TextShadow { offset = new Vector2(0, 1.5f), blurRadius = 0.5f, color = new Color(1f, 0.86f, 0.62f, 0.6f) };
            var sv = new ScrollView(ScrollViewMode.Vertical); sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden; sv.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            sv.style.flexGrow = 1; sv.style.flexShrink = 1; sv.style.marginTop = 10; box.Add(sv);
            sneakList = new VisualElement(); sv.Add(sneakList);
            var rope = new VisualElement { pickingMode = PickingMode.Ignore }; Abs(rope, null, 24, 60, 60); rope.style.width = 30;
            rope.style.backgroundImage = TitleTex("rope_v"); rope.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100)); box.Add(rope);
            var stay = new Button(() => { Hide(sneak); onSneak?.Invoke(""); }) { text = "ไม่ไปแล้ว นอนดีกว่า" }; WoodButton(stay, true);
            stay.style.alignSelf = Align.Center; stay.style.marginTop = 12; stay.style.unityBackgroundImageTintColor = new Color(0.85f, 0.8f, 0.72f); box.Add(stay);
        }

        /// เลือกด่านสำหรับแอบออกไปตกปลาตอนดึก · ""= ไม่ไป
        public void ShowSneakPicker(List<(string id, string name, int hazard)> spots, Action<string> pick)
        {
            if (sneak == null) BuildSneak();
            onSneak = pick;
            sneakList.Clear();
            bool n = Narrow;
            foreach (var (id, name, hz) in spots)
            {
                string sid = id;
                var row = new Button(() => { Hide(sneak); onSneak?.Invoke(sid); }) { text = "" };
                StyleButton(row, Color.clear);
                row.style.borderTopWidth = row.style.borderBottomWidth = row.style.borderLeftWidth = row.style.borderRightWidth = 0;
                row.style.backgroundImage = TitleTex("note"); row.style.unitySliceLeft = row.style.unitySliceRight = row.style.unitySliceTop = row.style.unitySliceBottom = 34;
                row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 6;
                row.style.paddingTop = row.style.paddingBottom = n ? 14 : 16; row.style.paddingLeft = 30; row.style.paddingRight = 26;
                // ภาพปกด่านเล็ก ๆ แบบรูปโพลารอยด์ (เดิมมีแต่ชื่อ ไม่มีภาพประกอบ)
                var pol = new VisualElement { pickingMode = PickingMode.Ignore }; pol.style.backgroundColor = new Color(0.97f, 0.95f, 0.9f); Pad(pol, 4, 4); pol.style.paddingBottom = 9;
                pol.style.flexShrink = 0; pol.style.marginRight = 14; pol.style.rotate = new Rotate(sneakList.childCount % 2 == 0 ? -3 : 2); row.Add(pol);
                var art = new VisualElement { pickingMode = PickingMode.Ignore }; art.style.width = n ? 84 : 112; art.style.height = n ? 54 : 72;
                art.style.backgroundImage = Stages.Get(sid).Art; art.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover); art.style.unityBackgroundImageTintColor = new Color(0.62f, 0.66f, 0.8f); pol.Add(art);   // ย้อมโทนกลางคืน
                var nm = L(row, $"{name} · {(hz >= 3 ? "ผีดุมาก" : hz == 2 ? "ผีดุ" : "ผีดุน้อย")}", n ? 22 : 28, new Color(0.22f, 0.12f, 0.05f));
                nm.style.flexGrow = 1; nm.style.flexShrink = 1; nm.style.unityTextAlign = TextAnchor.MiddleLeft; nm.style.unityFontDefinition = fontTitle;
                // ไอคอนผีตามความดุ (ดุมากแดงเข้ม)
                var tint = hz >= 3 ? new Color(0.62f, 0.1f, 0.08f) : hz == 2 ? new Color(0.72f, 0.36f, 0.08f) : new Color(0.25f, 0.3f, 0.36f);
                for (int k = 0; k < hz; k++) { var g = Ico(row, "ghost", n ? 26 : 32); g.style.unityBackgroundImageTintColor = tint; g.style.marginLeft = 2; g.style.flexShrink = 0; }
                // ตราประทับความดุ
                var stamp = L(row, hz >= 3 ? "ดุมาก" : hz == 2 ? "ดุ" : "ดุน้อย", n ? 18 : 22, new Color(1f, 0.95f, 0.85f));
                stamp.style.flexShrink = 0; stamp.style.marginLeft = 10; stamp.style.paddingLeft = stamp.style.paddingRight = 10; stamp.style.paddingTop = stamp.style.paddingBottom = 2;
                stamp.style.backgroundColor = hz >= 3 ? new Color(0.55f, 0.08f, 0.06f, 0.9f) : hz == 2 ? new Color(0.78f, 0.4f, 0.08f, 0.9f) : new Color(0.24f, 0.5f, 0.22f, 0.9f);
                stamp.style.borderTopLeftRadius = stamp.style.borderTopRightRadius = stamp.style.borderBottomLeftRadius = stamp.style.borderBottomRightRadius = 4;
                stamp.style.rotate = new Rotate(-6); stamp.style.unityFontDefinition = fontTitle;
                sneakList.Add(row);
            }
            if (fadeEl != null) { fadeEl.style.display = DisplayStyle.None; fadeEl.style.opacity = 0; }   // จอดำค้างไม่บังกระดาน
            Show(sneak);
        }
    }
}
