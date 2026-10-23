// ฟ้าไล่สีเรียบ ๆ สำหรับวันครึ้มหมอกจัด: ขอบฟ้าสีหมอก → กลางฟ้าเทาอมฟ้าเข้มขึ้นนิด · มีปื้นเมฆจาง ๆ เลื่อนช้า ๆ · ไม่มีดวงอาทิตย์
Shader "CozyFishing/SkyGradient"
{
    Properties
    {
        _Horizon ("Horizon", Color) = (0.62,0.65,0.66,1)
        _Zenith ("Zenith", Color) = (0.42,0.46,0.5,1)
        _Ground ("Below Horizon", Color) = (0.5,0.53,0.54,1)
        _Clouds ("Cloud Contrast", Range(0,0.3)) = 0.08
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Horizon, _Zenith, _Ground;
                float _Clouds;
            CBUFFER_END
            float4 _CozyWind;
            struct A { float4 positionOS : POSITION; };
            struct Vy { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };
            Vy vert(A v) { Vy o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.dir = v.positionOS.xyz; return o; }
            float h2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float n2(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f); return lerp(lerp(h2(i), h2(i + float2(1, 0)), f.x), lerp(h2(i + float2(0, 1)), h2(i + float2(1, 1)), f.x), f.y); }
            half4 frag(Vy i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float up = saturate(d.y);
                half3 col = d.y >= 0 ? lerp(_Horizon.rgb, _Zenith.rgb, pow(up, 0.6)) : lerp(_Horizon.rgb, _Ground.rgb, saturate(-d.y * 4));
                // เมฆครึ้มเป็นปื้น ๆ บนท้องฟ้า (ฉายลงระนาบเหนือหัว)
                float2 uv = d.xz / max(d.y + 0.15, 0.05) * 0.6 + _CozyWind.x * 0.004;
                float c = n2(uv * 2) * 0.6 + n2(uv * 5.3) * 0.3 + n2(uv * 11) * 0.1;
                col *= 1 + (c - 0.5) * _Clouds * smoothstep(0.02, 0.3, up);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
