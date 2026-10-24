using System;
using UnityEngine;

namespace CozyFishing
{
    // ตัวเลือกทดสอบ/ถ่ายคลิป (?bot&stage=… ) — บนเว็บอ่านจาก URL · ในเอดิเตอร์/เครื่องอัดคลิปอ่านจากตัวแปรแวดล้อม COZY_FLAGS
    public static class GameUrl
    {
        static string extra;
        static int seed = 1;
        /// สุ่มทั่วไป · ถ่ายคลิป (COZY_PROMO) ใช้ค่าตั้งต้นคงที่ ผลออกมาซ้ำได้
        public static System.Random Rng() => Environment.GetEnvironmentVariable("COZY_PROMO") != null ? new System.Random(seed++ * 7919) : new System.Random();
        public static string Value
        {
            get
            {
                if (extra == null) { try { extra = Environment.GetEnvironmentVariable("COZY_FLAGS") ?? ""; } catch (Exception) { extra = ""; } }
                var u = Application.absoluteURL ?? "";
                return extra.Length == 0 ? u : u + "?" + extra;
            }
        }
    }
}
