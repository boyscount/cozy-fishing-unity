using System.Linq;
using UnityEngine;

namespace CozyFishing
{
    // รายการด่าน (ลำดับปลดล็อก) — บ่อทุ่งนาฟรี ด่านถัดไปซื้อทางไปด้วยเงินเก็บ
    public class StageDef
    {
        public string id, name, en, scene, desc, go, back;
        public int price;
        // ภาพประกอบด่าน (Resources/UI/Stages/<id>.jpg) — ใช้บนการ์ดเลือกด่านตอนเช้าและจอเดินทาง
        public Texture2D Art => Resources.Load<Texture2D>("UI/Stages/" + id);
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
            new StageDef { id = "temple", name = "วังปลาโบสถ์ร้าง", en = "Sunken Temple Pool", scene = "Temple", price = 600,
                desc = "วังน้ำลึกที่ท่วมโบสถ์เก่าไปครึ่งหลัง ช่อฟ้ายังโผล่พ้นน้ำ ปลาตัวโตว่ายผ่านหน้าต่างโบสถ์",
                go = "เดินลัดป่าไปตามทางอิฐเก่า เสียงระฆังลมดังแผ่ว ๆ มาจากที่ไหนสักแห่ง…", back = "เดินออกจากวัดร้าง หันกลับไปเห็นช่อฟ้าลับหายในม่านหมอก…" },
            new StageDef { id = "marsh", name = "หนองบัวซ่อนศาล", en = "Hidden Shrine Marsh", scene = "Marsh", price = 900,
                desc = "หนองบัวกว้างสุดตา ศาลเจ้าที่บนเสาต้นเดียวกลางน้ำ ผูกผ้าแพรเจ็ดสีไว้ไม่รู้ตั้งแต่เมื่อไร",
                go = "เดินลัดคันนาไปจนสุดทาง กลิ่นดอกบัวลอยมากับลม ผ้าแพรสีแดงโบกอยู่ไกล ๆ กลางหนอง…", back = "เดินกลับตามคันนา หันไปอีกทีศาลกลางหนองก็จมหายไปในหมอกแล้ว…" },
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
