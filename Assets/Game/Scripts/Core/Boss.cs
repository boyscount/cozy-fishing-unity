using System;
using UnityEngine;

namespace CozyFishing
{
    // บอสแม่น้ำโขง: ปลาบึกยักษ์ตัวเดียวต่อสัปดาห์ (รอบเดียวกับกระดานรายสัปดาห์) · ต้องมีคันและรอกขั้นสองขึ้นไป (โหมดอิสระไม่ต้อง)
    //   ไหว้ศาลริมแก่ง (ค่าธูปเทียนพวงมาลัย) ก่อนลงเบ็ด = บอสโผล่บ่อยขึ้นนิด และไม่มีเงาพญานาคกวนตอนสู้
    //   สู้สามช่วง (FishingController): ดิ่งลึก → พุ่งขึ้นผิวน้ำ → ว่ายเข้าแก่ง (ต้องผ่อนสาย) · ได้แล้วเลือกปล่อยคืนแม่น้ำ (รับพร) หรือเก็บ
    public static class Boss
    {
        public const string Stage = "mekong", Id = "buekyak";
        public const int OfferingCost = 50, Blessing = 1500;
        const string Key = "cozy.boss.v1";

        [Serializable] class State { public string caughtWeek = ""; public int prayedDay = -1; public int released, kept; }
        static State s;
        static State S
        {
            get
            {
                if (s == null) { try { var j = PlayerPrefs.GetString(Key, ""); if (j.Length > 0) s = JsonUtility.FromJson<State>(j); } catch (Exception) { s = null; } }
                return s ??= new State();
            }
        }
        static void Save() { try { PlayerPrefs.SetString(Key, JsonUtility.ToJson(s)); PlayerPrefs.Save(); } catch (Exception) { } Cloud.MarkDirty(); }
        public static void Reload() { s = null; }

        public static bool CaughtThisWeek => S.caughtWeek == Weekly.WeekKey;
        public static bool GearReady => Story.FreePlay || (Story.S.rod >= 2 && Story.S.reel >= 2);
        public static bool Eligible => !CaughtThisWeek && GearReady;
        public static bool Prayed => S.prayedDay == Story.S.day;
        /// ข้อความบอกสถานะบอสตอนเข้าด่าน
        public static string Status => CaughtThisWeek ? "ปลาบึกยักษ์ของสัปดาห์นี้ถูกจับไปแล้ว… รอสัปดาห์หน้า"
            : !GearReady ? "ต้องมีคันคาร์บอนเบาและรอกเบทคาสติ้งขึ้นไป ถึงจะสู้ปลาบึกยักษ์ไหว (ร้านลุงส้ม)"
            : "ลูกไฟพญานาคลอยขึ้นถี่ ๆ แปลว่าปลาบึกยักษ์อยู่ใกล้";

        public static bool Pray()
        {
            if (Prayed) return true;
            if (!Story.FreePlay && Story.S.money < OfferingCost) return false;
            if (!Story.FreePlay) { Story.S.money -= OfferingCost; Story.Save(); }
            S.prayedDay = Story.S.day; Save(); return true;
        }

        public static void MarkCaught() { S.caughtWeek = Weekly.WeekKey; Save(); }
        public static void OnRelease()
        {
            S.released++; Save(); Cosmetics.Earn(15, "ปล่อยปลาบึกยักษ์");
            Story.S.money += Blessing; Story.S.earned += Blessing; Story.S.happy = Mathf.Min(100, Story.S.happy + 10); Story.Save();
        }
        public static void OnKeep() { S.kept++; Save(); Cosmetics.Earn(5, "จับปลาบึกยักษ์"); }
    }
}
