using System;

namespace CozyFishing
{
    // ต้นไม้ทักษะชาวประมง: ได้ค่าประสบการณ์ทุกครั้งที่จับปลา · เลเวลอัปได้แต้มทักษะ 1 แต้ม · เรียนทีละขั้นในแต่ละสาย (ต้องเรียนขั้นก่อนหน้า)
    //   4 สาย × 3 ขั้น: ตีเบ็ด · ลากเหยื่อ · สู้ปลา · อ่านน้ำ — ทุกขั้นมีผลจริงตอนตกปลา
    public class SkillNode { public string name, desc; }
    public class SkillBranch { public string id, name, icon; public SkillNode[] nodes; }

    public static class Skills
    {
        public const string Cast = "cast", Retrieve = "retrieve", Fight = "fight", Read = "read";
        public static readonly SkillBranch[] Branches =
        {
            new SkillBranch { id = Cast, name = "ตีเบ็ด", icon = "cast", nodes = new[] {
                new SkillNode { name = "แขนแข็งแรง", desc = "เหวี่ยงได้ไกลขึ้น 7%" },
                new SkillNode { name = "เหวี่ยงลื่นไหล", desc = "เหวี่ยงได้ไกลขึ้นรวม 14%" },
                new SkillNode { name = "มือเหวี่ยงครู", desc = "เหวี่ยงได้ไกลขึ้นรวม 21%" } } },
            new SkillBranch { id = Retrieve, name = "ลากเหยื่อ", icon = "reel", nodes = new[] {
                new SkillNode { name = "จับจังหวะ", desc = "ช่วงจังหวะลากเหยื่อปลอมที่ดีกว้างขึ้น 30%" },
                new SkillNode { name = "เหยื่อมีชีวิต", desc = "ปลาสนใจเหยื่อปลอมเร็วขึ้น 25%" },
                new SkillNode { name = "นักล่อตัวยง", desc = "ปลาฮุบเหยื่อปลอมเมื่อแถบสนใจถึง 85%" } } },
            new SkillBranch { id = Fight, name = "สู้ปลา", icon = "hook", nodes = new[] {
                new SkillNode { name = "อ่านทางปลา", desc = "เตือนก่อนปลาจะพุ่งสุดแรง ให้ผ่อนสายทัน" },
                new SkillNode { name = "ผ่อนเป็น", desc = "ปล่อยนิ้วแล้วสายหย่อนเร็วขึ้น 25%" },
                new SkillNode { name = "แขนเหล็ก", desc = "กรอปลาเข้าหาตัวเร็วขึ้น 15%" } } },
            new SkillBranch { id = Read, name = "อ่านน้ำ", icon = "eye", nodes = new[] {
                new SkillNode { name = "ตาเหยี่ยว", desc = "เห็นฟองอากาศผุดตรงจุดที่ปลาชุม" },
                new SkillNode { name = "รู้ใจปลา", desc = "ปลากินเหยื่อเร็วขึ้น 10%" },
                new SkillNode { name = "ปราชญ์สายน้ำ", desc = "ปลาหายากและปลาดาวสูงเข้าบ่อยขึ้น 25%" } } },
        };

        static StoryState S => Story.S;
        public static int Level(string branch) => branch switch { Cast => S.skCast, Retrieve => S.skRetrieve, Fight => S.skFight, _ => S.skRead };
        static void SetLevel(string branch, int v) { switch (branch) { case Cast: S.skCast = v; break; case Retrieve: S.skRetrieve = v; break; case Fight: S.skFight = v; break; default: S.skRead = v; break; } }

        // ---------- ค่าประสบการณ์ / ระดับ ----------
        /// ค่าประสบการณ์ที่ต้องใช้จากระดับนี้ไประดับถัดไป
        public static int Need(int level) => 80 + 60 * level;
        public static int FisherLevel => S.fLevel + 1;
        /// ได้ค่าประสบการณ์ · คืนค่าจำนวนระดับที่เพิ่มขึ้น (0 = ยังไม่อัป)
        public static int AddXp(int xp)
        {
            S.xp += xp; int up = 0;
            while (S.xp >= Need(S.fLevel)) { S.xp -= Need(S.fLevel); S.fLevel++; S.skillPts++; up++; }
            Story.Save();
            return up;
        }
        public static int XpForCatch(int stars, bool lure) => 10 + 10 * stars + (lure ? 10 : 0);

        public static bool CanLearn(string branch) => S.skillPts > 0 && Level(branch) < 3;
        public static bool Learn(string branch)
        {
            if (!CanLearn(branch)) return false;
            S.skillPts--; SetLevel(branch, Level(branch) + 1); Story.Save();
            return true;
        }

        // ---------- ผลของทักษะ ----------
        public static float CastMul => 1f + 0.07f * Level(Cast);
        public static float RetrieveWindow => Level(Retrieve) >= 1 ? 1.3f : 1f;
        public static float RetrieveGain => Level(Retrieve) >= 2 ? 1.25f : 1f;
        public static float RetrieveThreshold => Level(Retrieve) >= 3 ? 0.85f : 1f;
        public static bool DashWarning => Level(Fight) >= 1;
        public static float ReleaseMul => Level(Fight) >= 2 ? 1.25f : 1f;
        public static float ReelMul => Level(Fight) >= 3 ? 1.15f : 1f;
        public static bool SeeBubbles => Level(Read) >= 1;
        public static float BiteMul => Level(Read) >= 2 ? 0.9f : 1f;
        public static float RareMul => Level(Read) >= 3 ? 1.25f : 1f;
    }
}
