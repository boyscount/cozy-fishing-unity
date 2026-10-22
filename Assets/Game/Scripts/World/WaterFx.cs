using UnityEngine;

namespace CozyFishing
{
    // ค่าที่ shader ทุกตัวใช้ร่วมกัน: เวลา/แรงลม (_CozyWind) และวงคลื่นบนผิวน้ำ (_CozyRipples)
    public class WaterFx : MonoBehaviour
    {
        static readonly Vector4[] ripples = new Vector4[6];
        static int next;
        public static float Time;
        static readonly int WindId = Shader.PropertyToID("_CozyWind"), RipId = Shader.PropertyToID("_CozyRipples"), DimId = Shader.PropertyToID("_CozyDim");

        public static void AddRipple(float x, float z, float strength = 1f)
        {
            ripples[next] = new Vector4(x, z, Time, strength);
            next = (next + 1) % ripples.Length;
        }

        void Awake()
        {
            for (int i = 0; i < ripples.Length; i++) ripples[i] = new Vector4(0, 0, -100, 1);
            Shader.SetGlobalFloat(DimId, 1);
        }

        void Update()
        {
            Time += UnityEngine.Time.deltaTime;
            Shader.SetGlobalVector(WindId, new Vector4(Time, 0.8f + 0.35f * Mathf.Sin(Time * 0.23f), 0, 0));
            Shader.SetGlobalVectorArray(RipId, ripples);
        }
    }
}
