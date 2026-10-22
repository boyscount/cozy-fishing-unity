// ผิวน้ำใสแบบการ์ตูน: สีตามความลึก (จาก depth texture) · ฟองริมฝั่ง · คลื่น · เส้นแสงระยิบ · แสงแดดสะท้อน · วงคลื่น
// เทียบ makeWater() ของเวอร์ชันเว็บ — ต้องเปิด Depth Texture ใน URP asset
Shader "CozyFishing/Water"
{
    Properties
    {
        _Shallow ("Shallow", Color) = (0.61,0.81,0.54,1)
        _Deep ("Deep", Color) = (0.24,0.48,0.36,1)
        _DepthScale ("Depth Scale", Float) = 1.9
        _AlphaMin ("Alpha Shallow", Range(0,1)) = 0.3
        _AlphaMax ("Alpha Deep", Range(0,1)) = 0.86
        _Waves ("Wave Height", Float) = 0.015
        _Glint ("Sun Glint", Range(0,2)) = 0.8
        _FoamWidth ("Shore Foam Width", Float) = 0.12
        _Flow ("Flow (xy)", Vector) = (0.05,0.04,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Water"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Shallow, _Deep, _Flow;
                float _DepthScale, _AlphaMin, _AlphaMax, _Waves, _Glint, _FoamWidth;
            CBUFFER_END
            float4 _CozyWind;
            float4 _CozyRipples[6];   // xy = ตำแหน่ง, z = เวลาเริ่ม, w = แรง (ตั้งจาก WaterRipples)
            float _CozyDim;           // กลางคืน: หรี่สีน้ำ (1 = กลางวัน)

            float wv(float2 p, float t)
            {
                return sin(dot(p, float2(0.21, 0.13)) + t * 0.9) * 0.5 + sin(dot(p, float2(-0.12, 0.27)) + t * 1.3) * 0.35
                     + sin(dot(p, float2(0.47, 0.39)) + t * 2.1) * 0.18 + sin(dot(p, float2(-0.71, 0.52)) + t * 2.9) * 0.09;
            }
            float h2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float n2(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f); return lerp(lerp(h2(i), h2(i + float2(1,0)), f.x), lerp(h2(i + float2(0,1)), h2(i + float2(1,1)), f.x), f.y); }

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float4 screenPos : TEXCOORD1; float fog : TEXCOORD2; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                ws.y += wv(ws.xz, _CozyWind.x) * _Waves * 2;
                o.positionWS = ws;
                o.positionCS = TransformWorldToHClip(ws);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _CozyWind.x;
                float2 p = i.positionWS.xz;
                float2 suv = i.screenPos.xy / i.screenPos.w;
                float sceneZ = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float waterZ = LinearEyeDepth(i.positionCS.z, _ZBufferParams);
                float depth = max(0, sceneZ - waterZ);

                float e = 0.35, A = _Waves * 2;
                float dx = (wv(p + float2(e, 0), t) - wv(p - float2(e, 0), t)) / (2 * e) * A;
                float dz = (wv(p + float2(0, e), t) - wv(p - float2(0, e), t)) / (2 * e) * A;
                float r1 = n2(p * 1.7 + t * 0.35), r2 = n2(p * 1.7 + float2(0.37, 0) + t * 0.35), r3 = n2(p * 1.7 + float2(0, 0.37) + t * 0.35);
                float3 N = normalize(float3(-dx - (r2 - r1) * 0.35, 1, -dz - (r3 - r1) * 0.35));
                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
                Light light = GetMainLight();

                float k = smoothstep(0, _DepthScale, depth);
                float3 col = lerp(_Shallow.rgb, _Deep.rgb, k);
                float a = lerp(_AlphaMin, _AlphaMax, k);
                float ndl = dot(N, light.direction);
                col *= lerp(0.86, 1.0, smoothstep(0.80, 0.86, ndl)) + 0.06 * smoothstep(0.95, 0.99, ndl);
                // เส้นแสงสะท้อนแบบการ์ตูน
                float nn = n2(p * 0.9 + _Flow.xy * t) * 0.6 + n2(p * 1.9 - t * 0.09 + _Flow.xy * t * 1.5) * 0.4;
                float line_ = 1 - smoothstep(0, 0.035, abs(nn - 0.5));
                col = lerp(col, float3(0.9, 1, 0.97), line_ * 0.45); a = max(a, line_ * 0.55);
                // ฟองริมฝั่ง (จากความลึกจริงของฉาก)
                float fz = _FoamWidth + 0.05 * sin(p.x * 3 + p.y * 2 + t * 1.5) + n2(p * 3 + t * 0.3) * 0.07;
                float foam = smoothstep(fz, fz - 0.03, depth);
                // วงคลื่นจากทุ่น / ปลากระโดด
                float rip = 0;
                [unroll] for (int j = 0; j < 6; j++)
                {
                    float age = t - _CozyRipples[j].z;
                    if (age > 0 && age < 2.2)
                    {
                        float rr = age * 1.1 * _CozyRipples[j].w, d = length(p - _CozyRipples[j].xy);
                        rip += smoothstep(0.05, 0, abs(d - rr)) * (1 - age / 2.2) * _CozyRipples[j].w;
                        rip += smoothstep(0.04, 0, abs(d - rr * 0.6)) * (1 - age / 2.2) * 0.6;
                    }
                }
                foam = max(foam, min(rip, 1));
                float dim = _CozyDim > 0 ? _CozyDim : 1;
                col *= dim;
                col = lerp(col, (float3)max(dim, 0.5), foam); a = max(a, foam * 0.95);
                // ฟ้าสะท้อน + แสงแดดระยิบ
                float fr = pow(1 - saturate(dot(V, N)), 3);
                col = lerp(col, unity_FogColor.rgb * 0.8 + 0.2, fr * 0.55); a = lerp(a, 1, fr * 0.45);
                float3 R = reflect(-V, N);
                float sp = saturate(dot(R, light.direction));
                float glint = step(0.985, sp) * _Glint + smoothstep(0.9, 1, sp) * 0.18 * _Glint;
                col = lerp(col, light.color * 0.5 + 0.5, saturate(glint)); a = max(a, saturate(glint));
                col = MixFog(col, i.fog);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
