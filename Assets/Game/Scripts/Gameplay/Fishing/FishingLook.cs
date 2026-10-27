using UnityEngine;

namespace CozyFishing
{
    // ภาพทุ่นและสายเบ็ด: ทุ่นตามอุปกรณ์ที่ใส่ · สายโค้งจากปลายคันถึงทุ่น/ปากปลา
    sealed class FishingLook
    {
        readonly FishingController c;
        readonly Vector3[] linePts = new Vector3[24];
        readonly float lineBase = 0.014f;
        float lineW0 = 1, bubbleAt;

        public FishingLook(FishingController c)
        {
            this.c = c;
            c.line.positionCount = linePts.Length; lineBase = c.line.widthMultiplier;
        }

        /// ทุ่นที่ใส่อยู่ (ลูก V_<ชื่อ> ใต้ Bobber ที่ SceneBuilder สร้างไว้) + ความหนาสาย
        public void ApplyTackle()
        {
            if (c == null || c.bobber == null) return;
            string want = Lures.Active ? "V_" + Tackle.Equipped(Tackle.Lure) : "V_" + Tackle.Equipped(Tackle.Float).Replace("float_", "");
            bool found = false;
            foreach (Transform t in c.bobber.transform) if (t.name == want) found = true;
            foreach (Transform t in c.bobber.transform) if (t.name.StartsWith("V_")) t.gameObject.SetActive(found ? t.name == want : t.name == "V_feather");
            lineW0 = lineBase * Tackle.LineWidth;
        }

        /// ทักษะตาเหยี่ยว: ฟองอากาศผุดตรงจุดปลาชุม
        public void SkillBubbles()
        {
            if (!(Skills.SeeBubbles && (c.State == FishState.Free || c.State == FishState.Waiting) && Time.time > bubbleAt)) return;
            var rng = c.rng; var world = c.world;
            bubbleAt = Time.time + 1.6f + (float)rng.NextDouble();
            var sp = world.Special; float a = (float)rng.NextDouble() * 6.28f, r = (float)rng.NextDouble() * world.SpecialRadius * 0.7f;
            WaterFx.AddRipple(sp.x + Mathf.Cos(a) * r, sp.z + Mathf.Sin(a) * r, 0.35f);
        }

        public void Update(Vector3 tip)
        {
            var State = c.State; float t = c.t; var bob = c.bob; var line = c.line; var caughtGo = c.CaughtGo;
            bool hide = State == FishState.Snapped && t < 1f;
            c.bobber.SetActive(!hide && State != FishState.Caught);
            line.enabled = !hide;   // ยกปลาโชว์: สายตึงจากปลายคันถึงปากปลา
            c.bobber.transform.position = bob;
            c.bobber.transform.rotation = Quaternion.Euler(0, 0, State == FishState.Fight ? Mathf.Sin(WaterFx.Time * 20) * 17 : 0);
            float sag = State == FishState.Waiting || State == FishState.Bite ? 0.9f : State == FishState.Fight ? 0.05f + (1 - c.fight.Tension) * 0.6f : State == FishState.Casting ? 0.4f : 0.05f;
            var b = bob + Vector3.up * (State == FishState.Caught ? 0.1f : 0.22f);
            var mid = (tip + b) / 2 - Vector3.up * sag;
            for (int i = 0; i < linePts.Length; i++)
            {
                float u = i / (linePts.Length - 1f), v = 1 - u;
                linePts[i] = v * v * tip + 2 * v * u * mid + u * u * b;
            }
            if (State == FishState.Caught && c.ExternalShowcase && caughtGo)
            {   // ยกปลาโชว์มุมสายตา: สายผูกที่ปาก (จุดบนสุดของตัวปลาที่ห้อยอยู่) แล้วตรงขึ้นไปหาปลายคันเหนือจอ · ไม่ลากเส้นขาวผ่านข้างตัวปลาลงไปล่างจอ
                var rr = caughtGo.GetComponentInChildren<Renderer>();
                if (rr != null)
                {
                    var bd = rr.bounds; var mouth = new Vector3(bd.center.x, bd.max.y - bd.size.y * 0.02f, bd.center.z);
                    for (int i = 0; i < linePts.Length; i++) linePts[i] = mouth + Vector3.up * (4f * (1 - i / (linePts.Length - 1f)));
                }
            }
            line.SetPositions(linePts);
            line.widthMultiplier = State == FishState.Caught ? lineW0 * 0.2f : lineW0;   // ปลาห้อยอยู่ใกล้กล้องมาก สายปกติจะดูเป็นแท่งหนา
        }
    }
}
