using System.Runtime.InteropServices;
using UnityEngine;

namespace CozyFishing
{
    // ทดสอบ (?measurecast): วัดว่าจากจุดเกิดผู้เล่น เหวี่ยงลงน้ำได้ไกลสุดกี่เมตร ทุกทิศ และในทิศที่หันหน้าตอนเริ่ม (±45°)
    //   ใช้ตัดสินว่าด่านไหนน้ำแคบเกินระยะเหวี่ยงของคันขั้นสูงสุด · พิมพ์ลง console: [CastProbe] ...
    public static class CastProbe
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CozyTestNote(string msg);
#else
        static void CozyTestNote(string msg) { }
#endif
        public static void Run(IWorld w)
        {
            var p = w.Spawn;
            float best = 0, bestFwd = 0, bestYaw = 0;
            int waterDirs = 0;
            for (int deg = 0; deg < 360; deg += 3)
            {
                float a = deg * Mathf.Deg2Rad; var dir = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
                float far = 0;
                for (float d = 1.3f; d <= 80f; d += 0.5f)
                    if (w.CanCast(p.x + dir.x * d, p.z + dir.y * d)) far = d;
                if (far > 0) waterDirs++;
                if (far > best) { best = far; bestYaw = deg; }
                float off = Mathf.Abs(Mathf.DeltaAngle(deg, w.SpawnYaw));
                if (off <= 45 && far > bestFwd) bestFwd = far;
            }
            var note = $"[CastProbe] stage={w.StageId} max={best:0.0}m at {bestYaw:0}° · facing±45°={bestFwd:0.0}m · waterDirs={waterDirs}/120 · rodMax={Shop.CastDist:0} skillMax={30f * 1.21f:0}";
            Debug.Log(note); CozyTestNote(note);
        }
    }
}
