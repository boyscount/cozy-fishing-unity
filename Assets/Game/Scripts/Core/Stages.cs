using System.Linq;

namespace CozyFishing
{
    // รายการด่าน (ลำดับปลดล็อก) — บ่อทุ่งนาฟรี ด่านถัดไปซื้อทางไปด้วยเงินเก็บ
    public class StageDef
    {
        public string id, name, en, scene, desc, go, back;
        public int price;
    }

    public static class Stages
    {
        public static readonly StageDef[] All =
        {
            new StageDef { id = "paddy", name = "บ่อปลาทุ่งนา", en = "Rice-Field Pond", scene = "Paddy", price = 0,
                desc = "บ่อใหญ่กลางทุ่งข้าวหลังบ้าน ปลาบ้าน ๆ และตำนานบู่ทองใต้กอบัว",
                go = "เดินตามคันนาไปบ่อปลาหลังบ้าน…", back = "เดินลัดทุ่งข้าวกลับบ้าน…" },
            new StageDef { id = "rapids", name = "แก่งหินตาปลา", en = "Fish-Eye Rock Rapids", scene = "Rapids", price = 300,
                desc = "แก่งน้ำเชี่ยวกลางหุบเขา โขดหินยักษ์มีรอยเหมือนดวงตาปลาจ้องมองสายน้ำ",
                go = "เดินขึ้นเขาตามเสียงน้ำเชี่ยว ละอองน้ำเย็นปะทะหน้า…", back = "เดินลงเขากลับบ้าน เสียงแก่งค่อย ๆ เงียบหายไป…" },
        };

        public static StageDef Get(string id) => All.FirstOrDefault(s => s.id == id) ?? All[0];
        public static bool Unlocked(string id) => id == "paddy" || Story.S.unlocked.Contains(id);
        public static bool Buy(string id)
        {
            var s = Get(id);
            if (Unlocked(id) || Story.S.money < s.price) return false;
            Story.S.money -= s.price; Story.S.unlocked.Add(id); Story.Save();
            return true;
        }
        public static StageDef Current => Get(string.IsNullOrEmpty(Story.S.stage) ? "paddy" : Story.S.stage);
    }
}
