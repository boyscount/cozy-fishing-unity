using UnityEngine;

namespace CozyFishing
{
    // แสงตามเวลาของวัน: ดวงอาทิตย์ขึ้นตะวันออก (6 โมง) ตกตะวันตก (6 โมงเย็น) · เช้า/เย็นแดดส้มนุ่ม · ดวงไม่ดี (ฟ้าครึ้ม/ฝน) แสงหม่นลง
    public class DayCycle : MonoBehaviour
    {
        public Light sun;
        float baseIntensity, baseAmbient;
        Color baseColor, baseFog, baseSkyTint;
        Material sky; float baseSkyExp;   // สำเนาวัสดุฟ้าของฉาก (ไม่แก้ไฟล์วัสดุจริง)
        bool ready;
        /// ความมืดกลางคืน 0..1 และความใสของฟ้า 0..1 (ฟ้าครึ้ม/ฝน = น้อย) — NightSky ใช้จางดาว/พระจันทร์
        public static float Night, Clear = 1;
        /// แดดเช้า/เย็นต่ำใกล้ขอบฟ้า 0..1 · ทิศไปหาดวงอาทิตย์ (Atmosphere ใช้วาดลำแสง/ดวงอาทิตย์เรืองแสง)
        public static float Golden; public static Vector3 SunDir = Vector3.up;
        /// ทิศแสงจันทร์ (NightSky ตั้งให้ตรงกับพระจันทร์บนฟ้า)
        public static Quaternion MoonRot = Quaternion.Euler(55, 200, 0);

        void Awake() => Capture();
        void Capture()
        {
            if (ready || sun == null) return;
            baseIntensity = sun.intensity; baseColor = sun.color;
            baseAmbient = RenderSettings.ambientIntensity; baseFog = RenderSettings.fogColor;
            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
            {
                sky = new Material(RenderSettings.skybox); RenderSettings.skybox = sky;
                baseSkyExp = sky.GetFloat("_Exposure"); baseSkyTint = sky.HasProperty("_Tint") ? sky.GetColor("_Tint") : Color.gray;
            }
            ready = true;
        }

        public void Apply(float hour, float luckSun)
        {
            Capture();
            if (!ready) return;
            float night = hour >= 12 ? Mathf.Clamp01((hour - 18.6f) / 1.1f) : Mathf.Clamp01((5.2f - hour) / 1f);
            Night = night; Clear = Mathf.Lerp(0.2f, 1f, Mathf.InverseLerp(0.3f, 0.9f, luckSun));
            if (hour > 18.6f) hour = 18.6f;   // คำนวณแสงเย็นสุดท้ายแล้วค่อยไล่เข้ากลางคืน
            float t = Mathf.InverseLerp(5.3f, 18.9f, hour);
            float elev = Mathf.Sin(t * Mathf.PI) * 62f + 3f;
            float az = Mathf.Lerp(95f, 255f, t);
            sun.transform.rotation = Quaternion.Euler(elev, az, 0);
            float low = 1 - Mathf.Clamp01((elev - 3f) / 32f);            // ใกล้ขอบฟ้า → แสงส้ม
            SunDir = -sun.transform.forward; Golden = low * (1 - night);
            float day = Mathf.Clamp01((elev - 3f) / 22f);
            float cloud = Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(0.35f, 1f, luckSun));
            sun.color = Color.Lerp(baseColor, new Color(1f, 0.55f, 0.28f), low * 0.95f);   // golden hour
            sun.intensity = baseIntensity * Mathf.Lerp(0.5f, 1f, day) * cloud * (1 + low * 0.15f * (1 - Mathf.Clamp01((3.5f - elev) / 3f)));
            sun.shadowStrength = Mathf.Lerp(0.35f, 0.85f, cloud);
            RenderSettings.ambientIntensity = baseAmbient * Mathf.Lerp(0.5f, 1f, day) * Mathf.Lerp(0.75f, 1f, cloud);
            var dusk = new Color(0.9f, 0.62f, 0.48f);
            var gray = new Color(0.68f, 0.72f, 0.75f);
            RenderSettings.fogColor = Color.Lerp(Color.Lerp(baseFog, gray, 1 - cloud), dusk, low * 0.55f) * Mathf.Lerp(0.6f, 1f, day);
            if (night > 0)
            {
                // แสงจันทร์: ส่องจากสูงทางทิศใต้ สีฟ้าเย็น จาง ๆ · ฟ้ามืด หมอกน้ำเงินเข้ม
                sun.transform.rotation = Quaternion.Slerp(sun.transform.rotation, MoonRot, night);
                sun.color = Color.Lerp(sun.color, new Color(0.55f, 0.66f, 0.95f), night);
                sun.intensity = Mathf.Lerp(sun.intensity, baseIntensity * 0.3f, night);   // แสงจันทร์สว่างพอเห็นฉากบนจอมือถือ
                sun.shadowStrength = Mathf.Lerp(sun.shadowStrength, 0.5f, night);
                RenderSettings.ambientIntensity = Mathf.Lerp(RenderSettings.ambientIntensity, baseAmbient * 0.5f, night);
                RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, new Color(0.09f, 0.12f, 0.18f), night);
            }
            // ท้องฟ้า: กลางคืนมืดลงเป็นน้ำเงินเข้ม (เดิมฟ้ายังสว่างเมฆขาวทั้งที่สี่ทุ่ม)
            if (sky != null)
            {
                sky.SetFloat("_Exposure", baseSkyExp * Mathf.Lerp(Mathf.Lerp(0.55f, 1f, day), 0.07f, night));
                if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", Color.Lerp(Color.Lerp(baseSkyTint, baseSkyTint * new Color(1.15f, 0.85f, 0.75f), low * 0.6f), new Color(0.22f, 0.28f, 0.45f, baseSkyTint.a), night));
            }
        }
    }
}
