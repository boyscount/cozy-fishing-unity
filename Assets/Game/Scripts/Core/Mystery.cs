using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace CozyFishing
{
    // สืบคดีลึกลับ + ความลับของปลา
    //  · คดี: เบาะแสซ่อนอยู่ในปลา (ปลาที่ติดเบ็ดตามเงื่อนไขเวลา/จุด/อากาศ · ของในท้องปลาตอนปิ้ง · ขวดจดหมายที่ลอยมา)
    //    ครบทุกเบาะแส → ตอบคำถามไขคดี → รางวัล + เปิดเรื่องถัดไป
    //  · ความลับของปลา: ปลาแต่ละชนิดมีความลับ 1 ข้อ ปลดล็อกเมื่อทำตามเงื่อนไข (ตกได้หลายตัว ตกตอนฝน ตกตัวใหญ่ ปิ้ง ฯลฯ) แสดงในสมุดปลา
    public enum ClueSource { Catch, Grill, Bottle }

    public class Clue
    {
        public string id, name, found, hint;
        public ClueSource source;
        public string species;                 // ปลาชนิดที่พาเบาะแสมา (ว่าง = ชนิดใดก็ได้)
        public float hourFrom = 0, hourTo = 24;
        public bool nearSpecial, nearDock;
        public float chance = 1f;
        public float boost;                    // ระหว่างตามหาเบาะแสนี้ เพิ่มโอกาสเจอปลาชนิดนี้ (เมื่ออยู่ในเงื่อนไข)
    }

    public class Case
    {
        public string id, title, summary;
        public (string who, string say)[] intro, solved;
        public Clue[] clues;
        public string stage = "paddy";
        public string question; public string[] options; public int answer;
        public string wrong;
        public int rewardMoney; public bool rewardBigBasket;
    }

    // ความลับของปลา: kind = catch (ตกได้ n ตัว) · rain (ตกในวันฝน) · grill (ปิ้ง n ตัว) · heavy (ตัวหนัก ≥ kg) · dawn (ก่อน 7 โมง) · dusk (หลัง 5 โมงเย็น)
    public class FishSecret { public string species, text, hint, kind = "catch"; public int n = 1; public float kg; }

    [Serializable]
    public class MysteryState
    {
        public int v = 1;
        public bool started, solved;
        public int wrong;
        public List<string> clues = new List<string>(), secrets = new List<string>();
        public List<string> counterKeys = new List<string>();
        public List<int> counterVals = new List<int>();
    }

    public static class Mystery
    {
        const string Key = "abot.mystery.v1";
        public const string BottleJunk = "ขวดแก้วมีกระดาษม้วนอยู่ข้างใน";

        // ---------- คดีที่ 1 ----------
        public static readonly Case Case1 = new Case
        {
            id = "golden_shadow",
            title = "คดีที่ 1 · เงาทองใต้กอบัว",
            summary = "สามสิบปีก่อน ยายอิ่ม — แม่ค้าปลาคนแรกของหมู่บ้าน — หายไปในคืนที่มีแสงสีทองวาบใต้กอบัว ตำนานเล่าว่ายายสาน \"ตะกร้าแห่งวันพรุ่งนี้\" ไว้ใบหนึ่ง ใครได้ไปจะไม่มีวันอดอยาก แต่ไม่มีใครเคยเห็นมันอีกเลย",
            intro = new[]
            {
                ("ลุงเปี๊ยก", "เมื่อคืนลุงเดินผ่านบ่อ เห็นแสงสีทองวาบใต้กอบัวมุมบ่อ… เหมือนคืนที่ยายอิ่มหายไปเมื่อสามสิบปีก่อนไม่มีผิด"),
                ("พ่อ", "ยายอิ่มที่สานตะกร้าเก่งที่สุดในหมู่บ้านน่ะเหรอลุง"),
                ("ลุงเปี๊ยก", "ใช่ ว่ากันว่ายายสาน \"ตะกร้าแห่งวันพรุ่งนี้\" ไว้ใบหนึ่ง ใส่ปลาเท่าไหร่ก็ไม่เต็ม ใครได้ไปจะไม่มีวันอดอยาก"),
                ("ลุงเปี๊ยก", "ปลาในบ่อมันรู้นะ… ลองสังเกตปลาที่ติดเบ็ดดี ๆ บางตัวอาจพาอะไรมาบอกเรา"),
                ("ลูก", "พ่อ! เรามาเป็นนักสืบกันเถอะ หนูจะจดเบาะแสลงสมุดคดีให้"),
            },
            clues = new[]
            {
                new Clue { id = "scale", name = "เกล็ดสีทองแปลกตา", source = ClueSource.Catch, nearSpecial = true, chance = 0.55f,
                    found = "มีเกล็ดสีทองแผ่นใหญ่ติดอยู่ที่ตัวปลา ไม่ใช่เกล็ดของปลาตัวนี้แน่ ๆ… ใหญ่เกินกว่าปลาในบ่อทั่วไป",
                    hint = "ลุงเปี๊ยกเห็นแสงทองที่กอบัวมุมบ่อ ลองตกปลาแถวนั้นดู" },
                new Clue { id = "ring", name = "ห่วงทองเหลืองสลัก \"อ.อ.\"", source = ClueSource.Grill, species = "salit",
                    found = "ตอนแม่ผ่าท้องปลาสลิด เจอห่วงทองเหลืองเล็ก ๆ สลักตัวอักษร \"อ.อ.\" — อิ่ม อ่อนละมุน ชื่อเต็มของยายอิ่ม!",
                    hint = "ปลาบางตัวกลืนของแปลก ๆ ลงท้อง ลองเอาปลาสลิดกลับไปปิ้งที่บ้าน" },
                new Clue { id = "letter", name = "จดหมายในขวดแก้ว", source = ClueSource.Bottle, nearDock = true, hourFrom = 6, hourTo = 9, chance = 0.6f,
                    found = "ลายมือเก่าซีด: \"ถ้าใครพบจดหมายนี้ ขอให้ตามปลาที่ตื่นเช้ากว่าใคร มันจะพาเจ้าไปหาสิ่งที่ยายฝากไว้\"",
                    hint = "ของที่ลอยน้ำมักไปติดแถวท่าน้ำ ลองเหวี่ยงเบ็ดจากท่าไม้ตอนเช้าตรู่" },
                new Clue { id = "ribbon", name = "ริบบิ้นสานบนครีบปลาชะโด", source = ClueSource.Catch, species = "chado", hourFrom = 6, hourTo = 9.5f, chance = 1f, boost = 0.3f,
                    found = "ครีบหลังของชะโดผูกเชือกสานเส้นเล็ก ลายดอกบัวสามดอกเรียงกัน มีลูกศรชี้ไปทาง \"ท่าน้ำ\"",
                    hint = "จดหมายบอกให้ตามปลาที่ตื่นเช้ากว่าใคร… ชะโดชอบออกล่าตอนเช้า" },
                new Clue { id = "song", name = "ลูกปัดไม้ไผ่กับเพลงกล่อม", source = ClueSource.Catch, species = "butong", hourFrom = 15.5f, hourTo = 18.5f, nearSpecial = true, chance = 1f, boost = 0.22f,
                    found = "ปลาบู่ทองคาบลูกปัดไม้ไผ่ แม่จำได้ทันที — เพลงกล่อมของยายอิ่ม: \"บู่ทองเอ๋ย พาหนูไปนอนใต้ไม้ที่สาม\"",
                    hint = "แสงทองใต้กอบัว… คงเป็นปลาบู่ทองในตำนาน มันออกมาตอนเย็นใกล้กอบัว" },
            },
            question = "ตะกร้าแห่งวันพรุ่งนี้ของยายอิ่มซ่อนอยู่ที่ไหน?",
            options = new[] { "ใต้กอบัวหนาแน่นมุมบ่อ ที่เห็นแสงสีทอง", "ใต้เสาไม้ต้นที่สามของท่าน้ำ", "ในโพรงรากต้นตาลหลังเถียงนา" },
            answer = 1,
            wrong = "ลองอ่านเบาะแสอีกครั้ง… ลูกศรบนริบบิ้นชี้ไปทางไหน แล้วเพลงกล่อมพูดถึง \"ไม้ที่สาม\" ของอะไร?",
            solved = new[]
            {
                ("พ่อ", "ลูกศรชี้ไปทางท่าน้ำ เพลงกล่อมบอก \"ไม้ที่สาม\"… เสาต้นที่สามของท่าน้ำ!"),
                ("ลูก", "เจอแล้ว! ตะกร้าสานเก่า ๆ ห่อผ้ายางไว้ใต้เสาท่าน้ำ ข้างในมีเหรียญเก่ากับจดหมายอีกฉบับ"),
                ("แม่", "จดหมายเขียนว่า \"ตะกร้านี้ไม่ได้วิเศษหรอก แต่ใครที่ตั้งใจหาปลาเลี้ยงคนที่รัก ตะกร้าของเขาจะไม่ว่างเปล่าในวันพรุ่งนี้\""),
                ("ลุงเปี๊ยก", "ยายอิ่ม… ไม่ได้หายไปไหนหรอก ยายย้ายไปอยู่กับลูกสาวที่ลำธารป่าลึก จดหมายมีแผนที่ไปหาด้วยนี่"),
                ("ลูก", "ตะกร้าใบนี้ใส่ปลาได้ตั้ง 14 ตัวแน่ะพ่อ! แล้วเราไปตามหายายอิ่มที่ป่าลึกกันนะ"),
            },
            rewardMoney = 500, rewardBigBasket = true,
        };

        // ---------- ความลับของปลา (ด่านทุ่งนา) ----------
        public static readonly FishSecret[] Secrets =
        {
            new FishSecret { species = "kradi", kind = "catch", n = 5, hint = "ตกปลากระดี่ให้ได้ 5 ตัว", text = "ฝูงกระดี่จะว่ายวนเป็นวงกลมก่อนฝนตกเสมอ ชาวนารุ่นก่อนดูปลากระดี่แทนพยากรณ์อากาศ" },
            new FishSecret { species = "mo", kind = "rain", hint = "ตกปลาหมอในวันฝนพรำ", text = "ปลาหมอปีนคันนาได้จริง! มันใช้แผ่นปิดเหงือกที่มีหนามยันพื้น เดินข้ามไปบ่ออื่นตอนฝนตก" },
            new FishSecret { species = "salit", kind = "grill", n = 3, hint = "เอาปลาสลิดกลับไปปิ้ง 3 ตัว", text = "ตัวผู้ปลาสลิดก่อ \"หวอด\" ฟองน้ำลายลอยบนผิวน้ำ แล้วเฝ้าไข่เองจนลูกฟัก" },
            new FishSecret { species = "lai", kind = "heavy", kg = 1f, hint = "ตกปลาไหลนาตัวหนัก 1 กก. ขึ้นไป", text = "ปลาไหลนาหายใจทางผิวหนังได้ หน้าแล้งมันฝังตัวในโคลนลึก รอดได้เป็นเดือนจนฝนแรกมา" },
            new FishSecret { species = "chado", kind = "dawn", hint = "ตกปลาชะโดก่อน 7 โมงเช้า", text = "พ่อแม่ชะโดช่วยกันเฝ้าฝูงลูกสีส้มแดง ใครว่ายเข้าใกล้ฝูงจะพุ่งชนทันที" },
            new FishSecret { species = "butong", kind = "catch", hint = "ตกปลาบู่ทองให้ได้สักครั้ง", text = "ลายบนหลังบู่ทองตัวนี้คล้ายตัวอักษรเก่า อ่านได้ว่า \"อิ่ม\"… ปลาในตำนานที่คอยเฝ้าความลับของบ่อ" },
            // แก่งหินตาปลา
            new FishSecret { species = "siw_rapid", kind = "catch", n = 8, hint = "ตกปลาซิวหางกรรไกรให้ได้ 8 ตัว", text = "ฝูงซิวหางกรรไกรกระพือหางพร้อมกันเป็นสัญญาณเตือนภัย แล้วหักเลี้ยวพร้อมกันทั้งฝูงในพริบตา" },
            new FishSecret { species = "kho", kind = "grill", n = 2, hint = "เอาปลาค้อกลับไปปิ้ง 2 ตัว", text = "ใต้ท้องปลาค้อมีปากดูด ใช้เกาะหินกลางน้ำเชี่ยวไว้ไม่ให้ถูกกระแสพัดไป" },
            new FishSecret { species = "kang", kind = "dusk", hint = "ตกปลาก้างหลัง 5 โมงเย็น", text = "พ่อปลาก้างอมไข่ไว้ในปากจนลูกฟักเป็นตัว ช่วงนั้นมันไม่กินอะไรเลยหลายวัน" },
            new FishSecret { species = "wian_kaeng", kind = "heavy", kg = 2.5f, hint = "ตกปลาเวียนหนัก 2.5 กก. ขึ้นไป", text = "ทุกต้นฤดูฝน ปลาเวียนตัวใหญ่ว่ายทวนน้ำกระโดดข้ามแก่งขึ้นไปวางไข่ ชาวบ้านเรียกว่า \"ปลาขึ้นแก่ง\"" },
            new FishSecret { species = "mangkong", kind = "rain", hint = "ตกปลามังกงในวันฝนพรำ", text = "ปลามังกงรับรสด้วยหนวดทั้งแปดเส้น หาอาหารเจอได้แม้น้ำขุ่นจนมองไม่เห็นอะไรเลย" },
            new FishSecret { species = "wian_tong", kind = "catch", hint = "ตกปลาเวียนตาทองให้ได้สักครั้ง", text = "ดวงตาสีทองของปลาเวียนเผือกสะท้อนแสงเหมือนหินตาปลา… หรือว่าตาบนหินคือรูปสลักของมันเมื่อนานมาแล้ว?" },
            // วังปลาโบสถ์ร้าง
            new FishSecret { species = "kaemcham", kind = "catch", n = 8, hint = "ตกปลาแก้มช้ำให้ได้ 8 ตัว", text = "รอยแดงที่แก้มปลาแก้มช้ำเข้มขึ้นช่วงผสมพันธุ์ ตัวที่แดงที่สุดจะได้ว่ายนำหน้าฝูง" },
            new FishSecret { species = "sangkawat", kind = "dawn", hint = "ตกปลาสังกะวาดก่อน 7 โมงเช้า", text = "แต่ก่อนพระบิณฑบาตกลับมาจะโปรยข้าวที่ท่าน้ำทุกเช้า ปลาสังกะวาดยังมารอเวลาเดิมทุกวัน ทั้งที่ไม่มีใครมาแล้ว" },
            new FishSecret { species = "kot", kind = "grill", n = 2, hint = "เอาปลากดกลับไปปิ้ง 2 ตัว", text = "ปลากดส่งเสียงอี๊ด ๆ ได้ด้วยการเสียดสีกระดูกครีบอก ชาวบ้านเลยเรียกมันว่า \"ปลากดร้องไห้\"" },
            new FishSecret { species = "sawai", kind = "rain", hint = "ตกปลาสวายในวันฝนพรำ", text = "ปลาสวายมีกระเพาะลมช่วยหายใจ ขึ้นมาฮุบอากาศได้ จึงรอดในน้ำที่ออกซิเจนน้อยหลังฝนตกหนัก" },
            new FishSecret { species = "thepho", kind = "heavy", kg = 5f, hint = "ตกปลาเทโพหนัก 5 กก. ขึ้นไป", text = "ปลาเทโพตัวใหญ่ว่ายวนตามเข็มนาฬิการอบโบสถ์ทุกค่ำ เหมือนคนเวียนเทียน… สามรอบพอดี" },
            new FishSecret { species = "buekthao", kind = "catch", hint = "ตกปลาบึกเฒ่าให้ได้สักครั้ง", text = "บนหลังปลาบึกเฒ่ามีรอยแผลเป็นรูปกลีบบัวเหมือนลายปูนปั้นหน้าบันโบสถ์ ราวกับมันเคยว่ายผ่านหน้าต่างโบสถ์ก่อนน้ำจะท่วม" },
            // หนองบัวซ่อนศาล
            new FishSecret { species = "siwkhwan", kind = "catch", n = 8, hint = "ตกปลาซิวข้างขวานให้ได้ 8 ตัว", text = "ปลาซิวข้างขวานวางไข่ติดใต้ใบบัว ตัวผู้กับตัวเมียพลิกตัวหงายท้องพร้อมกันตอนปล่อยไข่" },
            new FishSecret { species = "motan", kind = "grill", n = 2, hint = "เอาปลาหมอตาลกลับไปปิ้ง 2 ตัว", text = "ที่ปลาหมอตาลจูบกันไม่ใช่ความรัก แต่คือการดวลแย่งอาณาเขต ใครถอยก่อนคือแพ้" },
            new FishSecret { species = "krasong", kind = "dusk", hint = "ตกปลากระสงหลัง 5 โมงเย็น", text = "ปลากระสงพ่อแม่ช่วยกันเฝ้าลูกเป็นฝูงสีส้ม ใครเข้าใกล้กอกกที่ลูกอยู่จะโดนพุ่งชนทันที" },
            new FishSecret { species = "salat", kind = "dawn", hint = "ตกปลาสลาดก่อน 7 โมงเช้า", text = "ปลาสลาดฮุบอากาศจากผิวน้ำได้ เสียง \"จ๋อม\" แผ่ว ๆ ในหนองยามเช้ามืดส่วนใหญ่คือเสียงของมัน" },
            new FishSecret { species = "raet", kind = "heavy", kg = 4f, hint = "ตกปลาแรดหนัก 4 กก. ขึ้นไป", text = "ปลาแรดตัวผู้ก่อรังจากเศษกกและใบบัวขนาดเท่าลูกมะพร้าว ซ่อนไว้ใต้เสาศาลกลางหนอง" },
            new FishSecret { species = "chonphueak", kind = "catch", hint = "ตกปลาช่อนเผือกเจ้าที่ให้ได้สักครั้ง", text = "ครีบหลังของปลาช่อนเผือกมีเส้นแดงเจ็ดเส้นเรียงกัน… เท่ากับผ้าแพรเจ็ดสีที่ผูกเสาศาลพอดี" },
            // หลืบแพสุสาน
            new FishSecret { species = "siwnuat", kind = "catch", n = 8, hint = "ตกปลาซิวหนวดยาวให้ได้ 8 ตัว", text = "ปลาซิวหนวดยาวกระโดดพ้นน้ำได้ไกลหลายช่วงตัวเวลาหนีปลาใหญ่ ชาวแพเรียกมันว่า \"ปลาบิน\"" },
            new FishSecret { species = "soikhao", kind = "grill", n = 3, hint = "เอาปลาสร้อยขาวกลับไปปิ้ง 3 ตัว", text = "ทุกต้นหนาว ปลาสร้อยขาวนับล้านตัวว่ายทวนน้ำพร้อมกันจนผิวน้ำเป็นประกายเงินยาวเป็นกิโล" },
            new FishSecret { species = "khayaenglai", kind = "dusk", hint = "ตกปลาแขยงข้างลายหลัง 5 โมงเย็น", text = "ปลาแขยงส่งเสียงจี๊ด ๆ ได้ด้วยการถูเงี่ยงครีบอก คืนเงียบ ๆ ใต้แพจะได้ยินพวกมันคุยกัน" },
            new FishSecret { species = "krathung", kind = "dawn", hint = "ตกปลากระทุงเหวก่อน 7 โมงเช้า", text = "ปลากระทุงเหววางไข่ติดกับรากผักตบลอยน้ำ ไข่มีเส้นใยเหนียวพันไว้ไม่ให้กระแสน้ำพัดหลุด" },
            new FishSecret { species = "khaokhao", kind = "heavy", kg = 6f, hint = "ตกปลาเค้าขาวหนัก 6 กก. ขึ้นไป", text = "ปลาเค้าขาวตัวใหญ่ชอบอยู่ใต้แพร้างที่มีเงามืด ยิ่งแพเก่ายิ่งมีปลาเค้าอาศัย" },
            new FishSecret { species = "kraho", kind = "catch", hint = "ตกปลากระโห้ผีแพให้ได้สักครั้ง", text = "บนเกล็ดปลากระโห้ตัวนี้มีรอยสลักเลขเรือนแพเลือนลาง… เลขเดียวกับป้ายบนเรือนแพที่จมอยู่ลึกที่สุด" },
            // คุ้งน้ำมืดป่าสนเถา
            new FishSecret { species = "paenkaeo", kind = "catch", n = 8, hint = "ตกปลาแป้นแก้วให้ได้ 8 ตัว", text = "ปลาแป้นแก้วตัวใสจนเห็นหัวใจเต้น มันใช้ความใสพรางตัวในเงามืดให้ปลาใหญ่มองไม่เห็น" },
            new FishSecret { species = "kradimuk", kind = "dawn", hint = "ตกปลากระดี่มุกก่อน 7 โมงเช้า", text = "ตัวผู้ปลากระดี่มุกสร้างหวอดฟองใต้ใบไม้ลอยน้ำ แล้วเฝ้าไข่ไม่ยอมไปไหนจนลูกฟัก" },
            new FishSecret { species = "mochang", kind = "grill", n = 2, hint = "เอาปลาหมอช้างเหยียบกลับไปปิ้ง 2 ตัว", text = "ชื่อหมอช้างเหยียบมาจากตัวที่แบนแต๊ดแต๋เหมือนโดนช้างเหยียบ ทั้งที่ความจริงมันแบนไว้มุดซอกรากไม้" },
            new FishSecret { species = "lot", kind = "rain", hint = "ตกปลาหลดในวันฝนพรำ", text = "ปลาหลดหายใจอากาศได้ หน้าแล้งมันมุดลงโคลนลึกแล้วรอจนฝนแรกปลุกให้ตื่น" },
            new FishSecret { species = "tonglai", kind = "heavy", kg = 4f, hint = "ตกปลาตองลายหนัก 4 กก. ขึ้นไป", text = "ปลาตองลายส่งเสียงฮุบอากาศดัง \"ป๊อก\" กลางดึก ชาวบ้านเล่าว่าเป็นเสียงผีพายเรือในคุ้งน้ำ" },
            new FishSecret { species = "chonnguhao", kind = "catch", hint = "ตกปลาช่อนงูเห่าให้ได้สักครั้ง", text = "จุดตาสีส้มที่โคนหางปลาช่อนงูเห่า เรืองแสงจาง ๆ ในความมืด… เหมือนแสงหิ่งห้อยที่ลอยตามมันไปทั้งคุ้ง" },
        };

        public static MysteryState M { get; private set; } = Load();
        public static Case Current => Case1;

        static MysteryState Load()
        {
            try
            {
                var json = PlayerPrefs.GetString(Key, "");
                if (!string.IsNullOrEmpty(json)) { var d = JsonUtility.FromJson<MysteryState>(json); if (d != null && d.v == 1) return d; }
            }
            catch (Exception) { }
            return new MysteryState();
        }
        public static void Save() { try { PlayerPrefs.SetString(Key, JsonUtility.ToJson(M)); PlayerPrefs.Save(); } catch (Exception) { } }
        public static void UseForTests(MysteryState s) => M = s;

        // ---------- คดี ----------
        public static bool Started => M.started;
        public static bool Solved => M.solved;
        public static void Start() { M.started = true; Save(); }
        public static bool HasClue(string id) => M.clues.Contains(id);
        public static int CluesFound => Current.clues.Count(c => HasClue(c.id));
        public static bool ReadyToSolve => M.started && !M.solved && CluesFound == Current.clues.Length;
        static bool Hunting => M.started && !M.solved;

        static bool Fits(Clue c, float hour, bool nearSpecial, bool nearDock) =>
            hour >= c.hourFrom && hour < c.hourTo && (!c.nearSpecial || nearSpecial) && (!c.nearDock || nearDock);

        /// ตอนสุ่มปลา: ระหว่างตามหาเบาะแส อาจบังคับให้เจอปลาที่พาเบาะแส (เพิ่มโอกาส) หรือขวดจดหมาย · คืน null = สุ่มตามปกติ
        public static Catch? Override(string stage, float hour, bool nearSpecial, bool nearDock, Random rng)
        {
            if (!Hunting || stage != Current.stage) return null;
            foreach (var c in Current.clues)
            {
                if (HasClue(c.id) || !Fits(c, hour, nearSpecial, nearDock)) continue;
                if (c.source == ClueSource.Bottle && rng.NextDouble() < c.chance * 0.5f) return new Catch { junk = BottleJunk };
                if (c.source == ClueSource.Catch && c.boost > 0 && !string.IsNullOrEmpty(c.species) && rng.NextDouble() < c.boost)
                {
                    var sp = FishDatabase.Get(c.species);
                    if (sp == null) continue;
                    float t = (float)rng.NextDouble();
                    return new Catch { species = sp, kg = (float)Math.Round(sp.kgMin + (sp.kgMax - sp.kgMin) * t, 2), cm = (int)Math.Round(sp.cmMin + (sp.cmMax - sp.cmMin) * t) };
                }
            }
            return null;
        }

        /// ได้ปลาขึ้นมา: ปลาตัวนี้พาเบาะแสมาไหม · คืนเบาะแสที่เพิ่งพบ (หรือ null)
        public static Clue OnCatch(string stage, string species, float hour, bool nearSpecial, bool nearDock, Random rng)
        {
            if (!Hunting || stage != Current.stage) return null;
            // เบาะแสที่ผูกกับชนิดปลาก่อน (เช่น บู่ทองใกล้กอบัว ต้องได้เพลงกล่อม ไม่ใช่เกล็ดทองทั่วไป)
            foreach (var c in Current.clues.OrderBy(c => string.IsNullOrEmpty(c.species) ? 1 : 0))
            {
                if (c.source != ClueSource.Catch || HasClue(c.id) || !Fits(c, hour, nearSpecial, nearDock)) continue;
                if (!string.IsNullOrEmpty(c.species) && c.species != species) continue;
                if (rng.NextDouble() >= c.chance) continue;
                return Found(c);
            }
            return null;
        }

        /// เก็บขวดจดหมายขึ้นมาได้
        public static Clue OnBottle() => Hunting ? Current.clues.Where(c => c.source == ClueSource.Bottle && !HasClue(c.id)).Select(Found).FirstOrDefault() : null;

        /// มื้อเย็น: ผ่าท้องปลาที่ปิ้ง
        public static Clue OnGrill(IEnumerable<BasketFish> grilled)
        {
            var list = grilled.ToList();
            foreach (var f in list) Count("grill:" + f.id);
            Clue got = null;
            if (Hunting)
                foreach (var c in Current.clues)
                    if (c.source == ClueSource.Grill && !HasClue(c.id) && list.Any(f => string.IsNullOrEmpty(c.species) || f.id == c.species)) { got = Found(c); break; }
            Save();
            return got;
        }

        static Clue Found(Clue c) { M.clues.Add(c.id); Save(); return c; }

        /// ตอบคำถามไขคดี · true = ถูก (ได้รางวัล)
        public static bool Answer(int option)
        {
            if (!ReadyToSolve) return false;
            if (option != Current.answer) { M.wrong++; Save(); return false; }
            M.solved = true;
            Story.S.money += Current.rewardMoney;
            if (Current.rewardBigBasket) Story.S.bigBasket = true;
            Story.Save();
            Save();
            return true;
        }

        // ---------- ความลับของปลา ----------
        static int Counter(string k) { int i = M.counterKeys.IndexOf(k); return i >= 0 ? M.counterVals[i] : 0; }
        static void Count(string k) { int i = M.counterKeys.IndexOf(k); if (i >= 0) M.counterVals[i]++; else { M.counterKeys.Add(k); M.counterVals.Add(1); } }
        public static FishSecret SecretOf(string species) => Secrets.FirstOrDefault(s => s.species == species);
        public static bool SecretKnown(string species) => M.secrets.Contains(species);

        /// ทุกครั้งที่ได้ปลา: นับสถิติแล้วเช็กเงื่อนไขความลับ · คืนความลับที่เพิ่งปลดล็อก (หรือ null)
        public static FishSecret CheckCatchSecret(string species, float kg, float hour, string luck)
        {
            Count("catch:" + species);
            if (luck == "bad") Count("rain:" + species);
            if (hour < 7) Count("dawn:" + species);
            if (hour >= 17) Count("dusk:" + species);
            var def = SecretOf(species);
            if (def != null && def.kind == "heavy" && kg >= def.kg) Count("heavy:" + species);
            var s = TryUnlock(species);
            Save();
            return s;
        }
        /// หลังปิ้ง (OnGrill นับไว้แล้ว) เช็กความลับของปลาที่ปิ้ง
        public static List<FishSecret> CheckGrillSecrets(IEnumerable<BasketFish> grilled)
        {
            var got = new List<FishSecret>();
            foreach (var id in grilled.Select(f => f.id).Distinct()) { var s = TryUnlock(id); if (s != null) got.Add(s); }
            Save();
            return got;
        }

        static FishSecret TryUnlock(string species)
        {
            var s = SecretOf(species);
            if (s == null || SecretKnown(species)) return null;
            bool ok = s.kind switch
            {
                "catch" => Counter("catch:" + species) >= s.n,
                "rain" => Counter("rain:" + species) >= s.n,
                "grill" => Counter("grill:" + species) >= s.n,
                "heavy" => Counter("heavy:" + species) >= 1,
                "dawn" => Counter("dawn:" + species) >= 1,
                "dusk" => Counter("dusk:" + species) >= 1,
                _ => false,
            };
            if (!ok) return null;
            M.secrets.Add(species);
            return s;
        }
    }
}
