using UnityEngine;

namespace CozyFishing
{
    // ใส่ของแต่งตัวที่เลือกไว้ให้ตัวละครกับเบ็ดในฉาก: สีหมวก/แถบหมวก · สีเสื้อ · สีทุ่น · สีสายเบ็ด (อัปเดตทันทีเมื่อเปลี่ยนในตู้แต่งตัว)
    public class CosmeticLook : MonoBehaviour
    {
        PlayerController player; FishingController fishing; MaterialPropertyBlock mpb;

        public static void Build(PlayerController p, FishingController f)
        {
            var c = new GameObject("CosmeticLook").AddComponent<CosmeticLook>(); c.player = p; c.fishing = f; c.mpb = new MaterialPropertyBlock();
            Cosmetics.Changed += c.Apply; c.Apply();
        }
        void OnDestroy() => Cosmetics.Changed -= Apply;

        void Tint(Renderer r, Color c, int sub = -1)
        {
            if (r == null) return;
            if (sub < 0) r.GetPropertyBlock(mpb); else r.GetPropertyBlock(mpb, sub);
            mpb.SetColor("_BaseColor", c); mpb.SetColor("_Color", c);
            if (sub < 0) r.SetPropertyBlock(mpb); else r.SetPropertyBlock(mpb, sub);
        }

        public void Apply()
        {
            if (this == null) return;
            var hat = Cosmetics.Equipped(Cosmetics.Hat); var shirt = Cosmetics.Equipped(Cosmetics.Shirt);
            var bob = Cosmetics.Equipped(Cosmetics.Bobber); var line = Cosmetics.Equipped(Cosmetics.Line);
            if (player != null)
                foreach (var r in player.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var n = mats[i] != null ? mats[i].name : "";
                        if (n.Contains("HatBand")) Tint(r, hat.color2, i);
                        else if (n.Contains("Hat")) Tint(r, hat.color, i);
                        else if (n.Contains("Top") || n.Contains("Shirt")) Tint(r, shirt.color, i);   // เสื้อ (โมเดล Mixamo: Remy_Top)
                    }
                }
            if (fishing != null && fishing.bobber != null)
                foreach (var r in fishing.bobber.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.name == "Stick") Tint(r, bob.color);
                    else if (r.name == "Glow") Tint(r, bob.color2 * 1.4f);
                    else if (bob.id == "bob_gold" || bob.id == "bob_naga") Tint(r, Color.Lerp(Color.white, bob.id == "bob_gold" ? bob.color : new Color(0.75f, 0.85f, 0.5f), 0.7f));
                }
            if (fishing != null && fishing.line != null) { fishing.line.startColor = fishing.line.endColor = line.color; Tint(fishing.line, line.color); }
        }
    }
}
