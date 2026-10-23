using UnityEngine;

namespace CozyFishing
{
    // แสงตามเวลาของวัน: ดวงอาทิตย์ขึ้นตะวันออก (6 โมง) ตกตะวันตก (6 โมงเย็น) · เช้า/เย็นแดดส้มนุ่ม · ดวงไม่ดี (ฟ้าครึ้ม/ฝน) แสงหม่นลง
    public class DayCycle : MonoBehaviour
    {
        public Light sun;
        float baseIntensity, baseAmbient;
        Color baseColor, baseFog;
        bool ready;

        void Awake() => Capture();
        void Capture()
        {
            if (ready || sun == null) return;
            baseIntensity = sun.intensity; baseColor = sun.color;
            baseAmbient = RenderSettings.ambientIntensity; baseFog = RenderSettings.fogColor;
            ready = true;
        }

        public void Apply(float hour, float luckSun)
        {
            Capture();
            if (!ready) return;
            float t = Mathf.InverseLerp(5.3f, 18.9f, hour);
            float elev = Mathf.Sin(t * Mathf.PI) * 62f + 3f;
            float az = Mathf.Lerp(95f, 255f, t);
            sun.transform.rotation = Quaternion.Euler(elev, az, 0);
            float low = 1 - Mathf.Clamp01((elev - 3f) / 32f);            // ใกล้ขอบฟ้า → แสงส้ม
            float day = Mathf.Clamp01((elev - 3f) / 22f);
            float cloud = Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(0.35f, 1f, luckSun));
            sun.color = Color.Lerp(baseColor, new Color(1f, 0.55f, 0.28f), low * 0.95f);   // golden hour
            sun.intensity = baseIntensity * Mathf.Lerp(0.5f, 1f, day) * cloud * (1 + low * 0.15f * (1 - Mathf.Clamp01((3.5f - elev) / 3f)));
            sun.shadowStrength = Mathf.Lerp(0.35f, 0.85f, cloud);
            RenderSettings.ambientIntensity = baseAmbient * Mathf.Lerp(0.5f, 1f, day) * Mathf.Lerp(0.75f, 1f, cloud);
            var dusk = new Color(0.9f, 0.62f, 0.48f);
            var gray = new Color(0.68f, 0.72f, 0.75f);
            RenderSettings.fogColor = Color.Lerp(Color.Lerp(baseFog, gray, 1 - cloud), dusk, low * 0.55f) * Mathf.Lerp(0.6f, 1f, day);
        }
    }
}
