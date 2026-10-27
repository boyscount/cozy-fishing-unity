// มองทะลุสิ่งบัง (ใต้น้ำ): วาดเฉพาะส่วนที่ถูกหิน/ขอนไม้/เสาบังอยู่ เป็นเส้นขอบเรืองแสงสีฟ้าอมเขียว
//   ZTest Greater = วาดเฉพาะพิกเซลที่อยู่หลังของอื่น · บวกแสง (One One) ไม่ทับสีเดิม · ขอบสว่างกว่ากลาง (rim) ให้เห็นรูปร่าง
//   อยู่ใน Resources เพื่อให้ติดไปกับ build (โหลดด้วย Resources.Load<Shader>("Shaders/XRay"))
Shader "CozyFishing/XRay"
{
    Properties
    {
        _Color ("Color", Color) = (0.35, 1.0, 0.85, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+100" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "XRay"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZTest Greater
            ZWrite Off
            Blend One One
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; };
            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 p = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(p);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = GetWorldSpaceViewDir(p);
                return o;
            }
            half4 frag (Varyings i) : SV_Target
            {
                float rim = 1 - saturate(abs(dot(normalize(i.normalWS), normalize(i.viewWS))));
                float pulse = 0.85 + 0.15 * sin(_Time.y * 4);
                return _Color * (0.18 + pow(rim, 2) * 1.3) * pulse;
            }
            ENDHLSL
        }
    }
}
